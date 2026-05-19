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
    /// <param name="trustedTenantId">The trusted request tenant context.</param>
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

        ConversationRejectedDomainEvent? shapeRejection = AddParticipantBoundary.ValidateCommandShape(command, addedAt, eventId);
        if (shapeRejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { shapeRejection });
        }

        return await ConversationTenantAccessGuard.RunAsync(
            _tenantAccessService,
            ConversationTenantAccessRequirement.Write,
            trustedTenantId ?? command!.Metadata.TenantId,
            callerPrincipalId,
            decision => DomainResult.Rejection(new IRejectionEvent[]
            {
                decision.ToRejection(
                    command!.Metadata.SchemaVersion,
                    command.Metadata.CorrelationId,
                    command.Metadata.CausationId),
            }),
            async guardedCancellationToken =>
            {
                ConversationState? state = await loadStateAsync(guardedCancellationToken).ConfigureAwait(false);
                return await HandleAfterTenantAccessAsync(
                    command!,
                    state,
                    addedAt,
                    eventId,
                    guardedCancellationToken).ConfigureAwait(false);
            },
            routeTenantId,
            command!.Metadata.TenantId,
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
