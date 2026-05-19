// <copyright file="AddParticipantCommandHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Validation;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.CommandHandlers;

/// <summary>
/// Handles add-participant commands after command-time Party validation.
/// </summary>
/// <param name="participantDirectory">The participant directory validation boundary.</param>
/// <param name="tenantAccessService">The tenant access boundary.</param>
public sealed class AddParticipantCommandHandler(
    IParticipantDirectory participantDirectory,
    IConversationTenantAccessService tenantAccessService)
{
    private readonly IParticipantDirectory _participantDirectory =
        participantDirectory ?? throw new ArgumentNullException(nameof(participantDirectory));

    private readonly IConversationTenantAccessService _tenantAccessService =
        tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));

    /// <summary>
    /// Checks tenant access, loads state, validates the participant Party reference, then dispatches the command to the aggregate.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <param name="callerPrincipalId">The caller principal or user identifier.</param>
    /// <param name="loadStateAsync">Loads the current conversation state only after tenant access is allowed.</param>
    /// <param name="addedAt">The deterministic participant-added timestamp.</param>
    /// <param name="eventId">The deterministic event identity.</param>
    /// <param name="trustedTenantId">The trusted request tenant context. Required: the caller boundary (auth middleware) must supply a non-null trusted tenant; a null value is treated as a missing tenant binding.</param>
    /// <param name="routeTenantId">The route tenant context, when present.</param>
    /// <param name="idempotencyTenantId">The idempotency tenant context, when present.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A participant-added event or a typed content-safe rejection.</returns>
    public async ValueTask<DomainResult> HandleAsync(
        AddParticipantCommand? command,
        string? callerPrincipalId,
        Func<CancellationToken, ValueTask<ConversationState?>> loadStateAsync,
        DateTimeOffset addedAt,
        string eventId,
        TenantId? trustedTenantId = null,
        TenantId? routeTenantId = null,
        TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loadStateAsync);

        // D4 hybrid: cheap schema-shape check (public vocabulary) runs before the tenant
        // access guard. Semantic shape (party id, type, role, conversation id, event id,
        // timestamp) is deferred to post-authorization so its rejection vocabulary is not
        // fingerprintable by cross-tenant probes.
        ConversationRejectedDomainEvent? schemaRejection = AddParticipantBoundary.ValidateSchemaShape(command);
        if (schemaRejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { schemaRejection });
        }

        // F4: do not fall back to the caller-controlled command body when the trusted
        // tenant binding is absent. A missing trusted tenant fails closed with the safe
        // tenant-binding rejection without recording caller-supplied correlation values
        // into the tenant audit trail.
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
                // F4: use the deterministic boundary-provided eventId as the safe correlation
                // token on the denial path; do not propagate caller-controlled correlation or
                // causation ids into the durable rejection event of the granted tenant.
                decision.ToRejection(
                    command!.Metadata.SchemaVersion,
                    correlationId: eventId,
                    causationId: null),
            }),
            async guardedCancellationToken =>
            {
                // D4: semantic shape validation runs first inside the guarded path so a
                // semantically invalid command never triggers an aggregate load.
                ConversationRejectedDomainEvent? semanticRejection = AddParticipantBoundary.ValidateSemanticShape(command!, addedAt, eventId);
                if (semanticRejection is not null)
                {
                    return DomainResult.Rejection(new IRejectionEvent[] { semanticRejection });
                }

                // F2: convert state-load infrastructure exceptions to a typed fail-closed
                // rejection so caller boundaries never see raw EventStore / stream vocabulary.
                ConversationState? state;
                try
                {
                    state = await loadStateAsync(guardedCancellationToken).ConfigureAwait(false);
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
                            command!.Metadata.SchemaVersion,
                            CorrelationId: eventId,
                            CausationId: null),
                    });
                }

                // F3: aggregate tenant cross-check after load. A loaded state whose
                // TenantId disagrees with the granted tenant cannot proceed even if every
                // other binding matched, because EventStore aggregate ownership is the
                // ultimate tenant-isolation invariant.
                if (state is not null && state.IsCreated && state.TenantId is { } stateTenantId && stateTenantId != grantedTenantId)
                {
                    return DomainResult.Rejection(new IRejectionEvent[]
                    {
                        new ConversationRejectedDomainEvent(
                            ConversationErrorCode.TenantIsolationViolation,
                            "tenant_isolation_violation",
                            command!.Metadata.SchemaVersion,
                            CorrelationId: eventId,
                            CausationId: null),
                    });
                }

                return await HandleAfterTenantAccessAsync(
                    command!,
                    state,
                    addedAt,
                    eventId,
                    guardedCancellationToken).ConfigureAwait(false);
            },
            routeTenantId,
            commandTenantId: command!.Metadata.TenantId,
            aggregateTenantId: null,
            projectionTenantId: null,
            idempotencyTenantId: idempotencyTenantId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<DomainResult> HandleAfterTenantAccessAsync(
        AddParticipantCommand command,
        ConversationState? state,
        DateTimeOffset addedAt,
        string eventId,
        CancellationToken cancellationToken)
    {
        ParticipantDirectoryValidation? validation;
        try
        {
            validation = await _participantDirectory
                .ValidateParticipantAsync(command.Metadata.TenantId, command.ParticipantPartyId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Honor caller cancellation explicitly; do not surface as a fail-closed rejection.
            throw;
        }
        catch (Exception)
        {
            // Fail-closed for any directory failure (provider exception, transient infrastructure).
            // The typed Conversations rejection keeps content safety guarantees regardless of the
            // underlying provider error type.
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                ParticipantValidationUnavailableRejection(command),
            });
        }

        if (validation is null)
        {
            // A misbehaving directory implementation must still fail closed rather than NRE
            // on the subsequent Status access.
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                ParticipantValidationUnavailableRejection(command),
            });
        }

        if (validation.Status != ParticipantDirectoryValidationStatus.Valid)
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                ToRejection(validation.Status, command),
            });
        }

        // Honor cancellation requested while the directory call was in-flight before
        // committing the aggregate dispatch.
        cancellationToken.ThrowIfCancellationRequested();

        return AddParticipantBoundary.DispatchValidated(command, addedAt, eventId, state);
    }

    private static ConversationRejectedDomainEvent ParticipantValidationUnavailableRejection(AddParticipantCommand command)
        => new(
            ConversationErrorCode.ParticipantValidationUnavailable,
            "participant_validation_unavailable",
            command.Metadata.SchemaVersion,
            command.Metadata.CorrelationId,
            command.Metadata.CausationId);

    private static ConversationRejectedDomainEvent ToRejection(
        ParticipantDirectoryValidationStatus status,
        AddParticipantCommand command)
        => status == ParticipantDirectoryValidationStatus.TenantMismatch
            ? new ConversationRejectedDomainEvent(
                ConversationErrorCode.TenantContextMismatch,
                "participant_tenant_mismatch",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId)
            : ParticipantValidationUnavailableRejection(command);
}
