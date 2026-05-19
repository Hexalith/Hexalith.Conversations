// <copyright file="AddParticipantCommandHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Validation;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.CommandHandlers;

/// <summary>
/// Handles add-participant commands after command-time Party validation.
/// </summary>
/// <param name="participantDirectory">The participant directory validation boundary.</param>
public sealed class AddParticipantCommandHandler(IParticipantDirectory participantDirectory)
{
    private readonly IParticipantDirectory _participantDirectory =
        participantDirectory ?? throw new ArgumentNullException(nameof(participantDirectory));

    /// <summary>
    /// Validates the participant Party reference, then dispatches the command to the aggregate.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <param name="addedAt">The deterministic participant-added timestamp.</param>
    /// <param name="eventId">The deterministic event identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A participant-added event or a typed content-safe rejection.</returns>
    public async ValueTask<DomainResult> HandleAsync(
        AddParticipantCommand? command,
        ConversationState? state,
        DateTimeOffset addedAt,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        ConversationRejectedDomainEvent? shapeRejection = AddParticipantBoundary.ValidateCommandShape(command, addedAt, eventId);
        if (shapeRejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { shapeRejection });
        }

        ParticipantDirectoryValidation validation = await _participantDirectory
            .ValidateParticipantAsync(command!.Metadata.TenantId, command.ParticipantPartyId, cancellationToken)
            .ConfigureAwait(false);

        if (validation.Status != ParticipantDirectoryValidationStatus.Valid)
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                ToRejection(validation.Status, command),
            });
        }

        return AddParticipantBoundary.DispatchValidated(command, addedAt, eventId, state);
    }

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
            : new ConversationRejectedDomainEvent(
                ConversationErrorCode.ParticipantValidationUnavailable,
                $"participant_validation_{status.ToString().ToLowerInvariant()}",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
}
