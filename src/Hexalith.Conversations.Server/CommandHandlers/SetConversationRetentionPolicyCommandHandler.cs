// <copyright file="SetConversationRetentionPolicyCommandHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Validation;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.CommandHandlers;

/// <summary>
/// Handles governed retention policy commands behind tenant, governance, audit, and idempotency gates.
/// </summary>
public sealed class SetConversationRetentionPolicyCommandHandler
{
    private readonly IConversationGovernanceAuditService _auditService;
    private readonly IdempotentConversationCommandExecutor? _idempotencyExecutor;
    private readonly IConversationRejectionTelemetry? _telemetry;
    private readonly IConversationTenantAccessService _tenantAccessService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetConversationRetentionPolicyCommandHandler"/> class.
    /// </summary>
    /// <param name="tenantAccessService">The tenant access boundary.</param>
    /// <param name="auditService">The governance audit boundary.</param>
    /// <param name="idempotencyExecutor">The optional idempotency executor.</param>
    /// <param name="telemetry">The optional rejection telemetry.</param>
    public SetConversationRetentionPolicyCommandHandler(
        IConversationTenantAccessService tenantAccessService,
        IConversationGovernanceAuditService auditService,
        IdempotentConversationCommandExecutor? idempotencyExecutor = null,
        IConversationRejectionTelemetry? telemetry = null)
    {
        _tenantAccessService = tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _idempotencyExecutor = idempotencyExecutor;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Executes the retention policy command after fail-closed authorization and audit checks.
    /// </summary>
    /// <param name="command">The public retention policy command.</param>
    /// <param name="callerPrincipalId">The caller principal or user identifier.</param>
    /// <param name="loadStateAsync">Loads current conversation state only after tenant access is allowed.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <param name="trustedTenantId">The trusted request tenant context.</param>
    /// <param name="routeTenantId">The route tenant context, when present.</param>
    /// <param name="idempotencyTenantId">The idempotency tenant context, when present.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A retention policy event, idempotency replay, or typed rejection.</returns>
    public async ValueTask<DomainResult> HandleAsync(
        SetConversationRetentionPolicyCommand? command,
        string? callerPrincipalId,
        Func<CancellationToken, ValueTask<ConversationState?>> loadStateAsync,
        string eventId,
        TenantId? trustedTenantId = null,
        TenantId? routeTenantId = null,
        TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loadStateAsync);

        ConversationRejectedDomainEvent? schemaRejection = SetConversationRetentionPolicyBoundary.ValidateSchemaShape(command);
        if (schemaRejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { schemaRejection });
        }

        if (trustedTenantId is null)
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new ConversationRejectedDomainEvent(
                    ConversationErrorCode.TenantBindingMissing,
                    "tenant_binding_missing",
                    command!.Metadata.SchemaVersion,
                    eventId,
                    CausationId: null),
            });
        }

        return await ConversationTenantAccessGuard.RunAsync(
            _tenantAccessService,
            ConversationTenantAccessRequirement.Governance,
            trustedTenantId,
            callerPrincipalId,
            decision => DomainResult.Rejection(new IRejectionEvent[]
            {
                decision.ToRejection(command!.Metadata.SchemaVersion, eventId, causationId: null),
            }),
            async guardedCancellationToken =>
            {
                ConversationRejectedDomainEvent? semanticRejection =
                    SetConversationRetentionPolicyBoundary.ValidateSemanticShape(command!, eventId);
                if (semanticRejection is not null)
                {
                    return DomainResult.Rejection(new IRejectionEvent[] { semanticRejection });
                }

                if (_idempotencyExecutor is null)
                {
                    return await ExecuteRetentionMutationAsync(
                        command!,
                        trustedTenantId,
                        loadStateAsync,
                        eventId,
                        guardedCancellationToken).ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(command!.Metadata.IdempotencyKey))
                {
                    return DomainResult.Rejection(new IRejectionEvent[]
                    {
                        new ConversationRejectedDomainEvent(
                            ConversationErrorCode.IdempotencyKeyMissing,
                            "idempotency_key_missing",
                            command.Metadata.SchemaVersion,
                            eventId,
                            CausationId: null),
                    });
                }

                ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(command, command.ConversationId);
                string auditHandle = ConversationAuditHandle.FromServerBoundary(fingerprint, eventId);

                return await _idempotencyExecutor.ExecuteAsync(
                    fingerprint,
                    command.OperationTimestamp,
                    eventId,
                    causationId: null,
                    mutationCancellationToken => ExecuteRetentionMutationAsync(
                        command,
                        trustedTenantId,
                        loadStateAsync,
                        eventId,
                        mutationCancellationToken),
                    result => ToIdempotencyOutcome(command, result, auditHandle),
                    guardedCancellationToken).ConfigureAwait(false);
            },
            routeTenantId,
            commandTenantId: command!.Metadata.TenantId,
            aggregateTenantId: null,
            projectionTenantId: null,
            idempotencyTenantId: idempotencyTenantId,
            telemetry: _telemetry,
            correlationId: eventId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<DomainResult> ExecuteRetentionMutationAsync(
        SetConversationRetentionPolicyCommand command,
        TenantId grantedTenantId,
        Func<CancellationToken, ValueTask<ConversationState?>> loadStateAsync,
        string eventId,
        CancellationToken cancellationToken)
    {
        ConversationState? state;
        try
        {
            state = await loadStateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Rejection(command, ConversationErrorCode.TenantProjectionStale, "tenant_projection_stale", eventId);
        }

        if (state is not null && state.IsCreated && state.TenantId is { } stateTenantId && stateTenantId != grantedTenantId)
        {
            return Rejection(command, ConversationErrorCode.TenantIsolationViolation, "tenant_isolation_violation", eventId);
        }

        ConversationRejectedDomainEvent? preAuditRejection =
            SetConversationRetentionPolicyBoundary.ValidateStateBeforeAudit(command, eventId, state);
        if (preAuditRejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { preAuditRejection });
        }

        ConversationGovernanceAuditResult auditResult = await ConversationGovernanceAuditGate
            .RecordRequiredAsync(
                token => _auditService.RecordRetentionPolicyChangeAsync(command, OperationKindFor(state), eventId, token),
                cancellationToken)
            .ConfigureAwait(false);

        if (auditResult.Status != ConversationGovernanceAuditStatus.Succeeded || auditResult.Evidence is null)
        {
            if (auditResult.Status == ConversationGovernanceAuditStatus.AuditUnavailable)
            {
                _telemetry?.RecordCommandRejection(
                    ConversationCommandRejectionClass.AuditUnavailable,
                    ConversationTenantAccessRequirement.Governance,
                    isRetryable: ConversationErrorCode.IsRetryable(ConversationErrorCode.AuditSinkUnavailable),
                    eventId);
            }

            return AuditFailure(command, auditResult.Status, eventId);
        }

        ConversationRejectedDomainEvent? auditPairingRejection =
            SetConversationRetentionPolicyBoundary.ValidateAuditEvidenceProvided(command, auditResult.Evidence);
        if (auditPairingRejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { auditPairingRejection });
        }

        return SetConversationRetentionPolicyBoundary.DispatchValidated(command, auditResult.Evidence, eventId, state);
    }

    private static GovernanceOperationKind OperationKindFor(ConversationState? state)
        => state?.ActiveRetentionPolicy is null
            ? GovernanceOperationKind.SetRetentionPolicy
            : GovernanceOperationKind.ReplaceRetentionPolicy;

    private static DomainResult AuditFailure(
        SetConversationRetentionPolicyCommand command,
        ConversationGovernanceAuditStatus status,
        string eventId)
        => status switch
        {
            ConversationGovernanceAuditStatus.PolicyBlocked => Rejection(
                command,
                ConversationErrorCode.CommandValidationFailed,
                "retention_policy_blocked",
                eventId),
            ConversationGovernanceAuditStatus.UnsafeEvidence => Rejection(
                command,
                ConversationErrorCode.AuditPairingRequired,
                "audit_evidence_unsafe",
                eventId),
            ConversationGovernanceAuditStatus.Uncertain => Rejection(
                command,
                ConversationErrorCode.IdempotencyOutcomeUnknown,
                "audit_pairing_uncertain",
                eventId),
            _ => Rejection(command, ConversationErrorCode.AuditSinkUnavailable, "audit_unavailable", eventId),
        };

    private static DomainResult Rejection(
        SetConversationRetentionPolicyCommand command,
        ConversationErrorCode code,
        string reasonCode,
        string eventId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new ConversationRejectedDomainEvent(
                code,
                reasonCode,
                command.Metadata.SchemaVersion,
                eventId,
                CausationId: null),
        });

    private static ConversationIdempotencyOutcome ToIdempotencyOutcome(
        SetConversationRetentionPolicyCommand command,
        DomainResult result,
        string auditHandle)
    {
        if (result.IsSuccess)
        {
            return ConversationIdempotencyOutcome.Success(
                command.Metadata.SchemaVersion,
                command.Metadata.TenantId,
                ConversationCommandType.SetConversationRetentionPolicyCommand,
                command.ConversationId,
                messageId: null,
                participantPartyId: null,
                fileId: null,
                auditHandle,
                auditHandle);
        }

        if (result.IsRejection)
        {
            ConversationRejectedDomainEvent? rejection = result.Events.OfType<ConversationRejectedDomainEvent>().FirstOrDefault();
            if (rejection is not null)
            {
                return ConversationIdempotencyOutcome.Rejection(
                    command.Metadata.SchemaVersion,
                    command.Metadata.TenantId,
                    ConversationCommandType.SetConversationRetentionPolicyCommand,
                    command.ConversationId,
                    rejection.Code,
                    rejection.ReasonCode,
                    ConversationErrorCode.IsRetryable(rejection.Code),
                    auditHandle,
                    auditHandle);
            }
        }

        return ConversationIdempotencyOutcome.Uncertain(
            command.Metadata.SchemaVersion,
            command.Metadata.TenantId,
            ConversationCommandType.SetConversationRetentionPolicyCommand,
            command.ConversationId,
            auditHandle,
            auditHandle);
    }
}
