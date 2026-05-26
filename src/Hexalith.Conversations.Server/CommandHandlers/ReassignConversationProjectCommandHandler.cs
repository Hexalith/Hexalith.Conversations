// <copyright file="ReassignConversationProjectCommandHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Validation;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.CommandHandlers;

/// <summary>
/// Handles project assignment changes behind the tenant access and idempotency boundaries.
/// </summary>
public sealed class ReassignConversationProjectCommandHandler
{
    private readonly IdempotentConversationCommandExecutor? _idempotencyExecutor;
    private readonly IConversationTenantAccessService _tenantAccessService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReassignConversationProjectCommandHandler"/> class without idempotency storage.
    /// </summary>
    /// <param name="tenantAccessService">The tenant access boundary.</param>
    public ReassignConversationProjectCommandHandler(IConversationTenantAccessService tenantAccessService)
        : this(tenantAccessService, idempotencyExecutor: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReassignConversationProjectCommandHandler"/> class.
    /// </summary>
    /// <param name="tenantAccessService">The tenant access boundary.</param>
    /// <param name="idempotencyExecutor">The optional idempotency executor.</param>
    public ReassignConversationProjectCommandHandler(
        IConversationTenantAccessService tenantAccessService,
        IdempotentConversationCommandExecutor? idempotencyExecutor)
    {
        _tenantAccessService = tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));
        _idempotencyExecutor = idempotencyExecutor;
    }

    /// <summary>
    /// Checks tenant access, then loads state and dispatches the command to the aggregate.
    /// </summary>
    /// <param name="command">The public project reassignment command.</param>
    /// <param name="callerPrincipalId">The caller principal or user identifier.</param>
    /// <param name="loadStateAsync">Loads the current conversation state only after tenant access is allowed.</param>
    /// <param name="changedAt">The deterministic project-change timestamp.</param>
    /// <param name="eventId">The deterministic event identity.</param>
    /// <param name="trustedTenantId">The trusted request tenant context.</param>
    /// <param name="routeTenantId">The route tenant context, when present.</param>
    /// <param name="idempotencyTenantId">The idempotency tenant context, when present.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A project-changed event, idempotency replay, no-op, or typed rejection.</returns>
    public async ValueTask<DomainResult> HandleAsync(
        ReassignConversationProjectCommand? command,
        string? callerPrincipalId,
        Func<CancellationToken, ValueTask<ConversationState?>> loadStateAsync,
        DateTimeOffset changedAt,
        string eventId,
        TenantId? trustedTenantId = null,
        TenantId? routeTenantId = null,
        TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loadStateAsync);

        ConversationRejectedDomainEvent? schemaRejection = ReassignConversationProjectBoundary.ValidateSchemaShape(command);
        if (schemaRejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { schemaRejection });
        }

        ArgumentNullException.ThrowIfNull(command);

        if (trustedTenantId is null)
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new ConversationRejectedDomainEvent(
                    ConversationErrorCode.TenantBindingMissing,
                    "tenant_binding_missing",
                    command!.Metadata.SchemaVersion,
                    CorrelationId: eventId,
                    CausationId: null),
            });
        }

        TenantId grantedTenantId = trustedTenantId;

        return await ConversationTenantAccessGuard.RunAsync(
            _tenantAccessService,
            ConversationTenantAccessRequirement.Write,
            grantedTenantId,
            callerPrincipalId,
            decision => DomainResult.Rejection(new IRejectionEvent[]
            {
                decision.ToRejection(
                    command!.Metadata.SchemaVersion,
                    correlationId: eventId,
                    causationId: null),
            }),
            async guardedCancellationToken =>
            {
                ConversationRejectedDomainEvent? semanticRejection =
                    ReassignConversationProjectBoundary.ValidateSemanticShape(command!, changedAt, eventId);
                if (semanticRejection is not null)
                {
                    return DomainResult.Rejection(new IRejectionEvent[] { semanticRejection });
                }

                ValueTask<DomainResult> ExecuteMutationAsync(CancellationToken mutationCancellationToken)
                    => ExecuteProjectMutationAsync(
                        command!,
                        grantedTenantId,
                        loadStateAsync,
                        changedAt,
                        eventId,
                        mutationCancellationToken);

                if (_idempotencyExecutor is not null)
                {
                    if (string.IsNullOrWhiteSpace(command!.Metadata.IdempotencyKey))
                    {
                        return DomainResult.Rejection(new IRejectionEvent[]
                        {
                            new ConversationRejectedDomainEvent(
                                ConversationErrorCode.IdempotencyKeyMissing,
                                "idempotency_key_missing",
                                command.Metadata.SchemaVersion,
                                CorrelationId: eventId,
                                CausationId: null),
                        });
                    }

                    ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(command!, command.ConversationId);
                    string auditHandle = ConversationAuditHandle.FromServerBoundary(fingerprint, eventId);

                    return await _idempotencyExecutor.ExecuteAsync(
                        fingerprint,
                        changedAt,
                        correlationId: eventId,
                        causationId: null,
                        ExecuteMutationAsync,
                        result => ToIdempotencyOutcome(command!, result, auditHandle),
                        guardedCancellationToken).ConfigureAwait(false);
                }

                return await ExecuteMutationAsync(guardedCancellationToken).ConfigureAwait(false);
            },
            routeTenantId,
            commandTenantId: command!.Metadata.TenantId,
            aggregateTenantId: null,
            projectionTenantId: null,
            idempotencyTenantId: idempotencyTenantId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<DomainResult> ExecuteProjectMutationAsync(
        ReassignConversationProjectCommand command,
        TenantId grantedTenantId,
        Func<CancellationToken, ValueTask<ConversationState?>> loadStateAsync,
        DateTimeOffset changedAt,
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
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new ConversationRejectedDomainEvent(
                    ConversationErrorCode.TenantProjectionStale,
                    "tenant_projection_stale",
                    command.Metadata.SchemaVersion,
                    CorrelationId: eventId,
                    CausationId: null),
            });
        }

        if (state is not null && state.IsCreated && state.TenantId is { } stateTenantId && stateTenantId != grantedTenantId)
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new ConversationRejectedDomainEvent(
                    ConversationErrorCode.TenantIsolationViolation,
                    "tenant_isolation_violation",
                    command.Metadata.SchemaVersion,
                    CorrelationId: eventId,
                    CausationId: null),
            });
        }

        return ReassignConversationProjectBoundary.DispatchValidated(command, changedAt, eventId, state);
    }

    private static ConversationIdempotencyOutcome ToIdempotencyOutcome(
        ReassignConversationProjectCommand command,
        DomainResult result,
        string auditHandle)
    {
        if (result.IsSuccess)
        {
            ConversationProjectChangedDomainEvent? changed = result.Events
                .OfType<ConversationProjectChangedDomainEvent>()
                .FirstOrDefault();
            if (changed is null)
            {
                return ConversationIdempotencyOutcome.Uncertain(
                    command.Metadata.SchemaVersion,
                    command.Metadata.TenantId,
                    ConversationCommandType.ReassignConversationProjectCommand,
                    command.ConversationId,
                    auditHandle,
                    auditHandle);
            }

            return ConversationIdempotencyOutcome.Success(
                command.Metadata.SchemaVersion,
                command.Metadata.TenantId,
                ConversationCommandType.ReassignConversationProjectCommand,
                command.ConversationId,
                messageId: null,
                participantPartyId: null,
                fileId: null,
                auditHandle,
                auditHandle);
        }

        if (result.IsRejection)
        {
            ConversationRejectedDomainEvent? rejection = result.Events
                .OfType<ConversationRejectedDomainEvent>()
                .FirstOrDefault();
            if (rejection is null)
            {
                return ConversationIdempotencyOutcome.Uncertain(
                    command.Metadata.SchemaVersion,
                    command.Metadata.TenantId,
                    ConversationCommandType.ReassignConversationProjectCommand,
                    command.ConversationId,
                    auditHandle,
                    auditHandle);
            }

            return ConversationIdempotencyOutcome.Rejection(
                command.Metadata.SchemaVersion,
                command.Metadata.TenantId,
                ConversationCommandType.ReassignConversationProjectCommand,
                command.ConversationId,
                rejection.Code,
                rejection.ReasonCode,
                ConversationErrorCode.IsRetryable(rejection.Code),
                auditHandle,
                auditHandle);
        }

        return ConversationIdempotencyOutcome.NoOp(
            command.Metadata.SchemaVersion,
            command.Metadata.TenantId,
            ConversationCommandType.ReassignConversationProjectCommand,
            command.ConversationId,
            auditHandle,
            auditHandle);
    }
}
