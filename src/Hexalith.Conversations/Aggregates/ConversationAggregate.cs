// <copyright file="ConversationAggregate.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Validation;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using PublicCreateConversationCommand = Hexalith.Conversations.Contracts.Commands.CreateConversationCommand;
using PublicConversationCommandMetadata = Hexalith.Conversations.Contracts.Commands.ConversationCommandMetadata;
using PublicAddParticipantCommand = Hexalith.Conversations.Contracts.Commands.AddParticipantCommand;
using PublicSetConversationRetentionPolicyCommand = Hexalith.Conversations.Contracts.Governance.SetConversationRetentionPolicyCommand;

namespace Hexalith.Conversations.Aggregates;

/// <summary>
/// Handles tenant-scoped conversation domain commands.
/// </summary>
public sealed class ConversationAggregate : EventStoreAggregate<ConversationState>
{
    /// <summary>
    /// Creates a conversation when the command is valid and the state has not already been created.
    /// </summary>
    /// <param name="command">The create-conversation domain command.</param>
    /// <param name="state">The current conversation state, when any.</param>
    /// <returns>A domain result containing one creation event or one typed rejection.</returns>
    public static DomainResult Handle(CreateConversation command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = CreateConversationValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        PublicCreateConversationCommand publicCommand = command.PublicCommand;
        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;

        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            ConversationEventType.ConversationCreated,
            commandMetadata.TenantId,
            command.ConversationId!,
            commandMetadata.CorrelationId,
            command.CreatedAt,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        ConversationCreatedDomainEvent created = new(
            eventMetadata,
            publicCommand.BusinessReference,
            publicCommand.ProjectId,
            publicCommand.FolderId,
            publicCommand.Label,
            commandMetadata.IdempotencyKey,
            publicCommand.ProviderCorrelation);

        return DomainResult.Success(new IEventPayload[] { created });
    }

    /// <summary>
    /// Adds a validated participant when the conversation state permits new membership.
    /// </summary>
    /// <param name="command">The add-participant domain command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one participant-added event or one typed rejection.</returns>
    public static DomainResult Handle(AddParticipant command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = AddParticipantValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        PublicAddParticipantCommand publicCommand = command.PublicCommand;
        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;

        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            ConversationEventType.ParticipantAdded,
            commandMetadata.TenantId,
            publicCommand.ConversationId,
            commandMetadata.CorrelationId,
            command.AddedAt,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        ParticipantAddedDomainEvent added = new(
            eventMetadata,
            publicCommand.ParticipantPartyId,
            publicCommand.ParticipantType,
            publicCommand.ParticipantRole);

        return DomainResult.Success(new IEventPayload[] { added });
    }

    /// <summary>
    /// Sets or replaces a governed retention policy when the current conversation state permits it.
    /// </summary>
    /// <param name="command">The retention policy domain command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one retention event or one typed rejection.</returns>
    public static DomainResult Handle(SetConversationRetentionPolicy command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = SetConversationRetentionPolicyValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        PublicSetConversationRetentionPolicyCommand publicCommand = command.PublicCommand;
        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;
        bool replacing = state!.ActiveRetentionPolicy is not null;

        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            replacing ? ConversationEventType.RetentionPolicyReplaced : ConversationEventType.RetentionPolicySet,
            commandMetadata.TenantId,
            publicCommand.ConversationId,
            commandMetadata.CorrelationId,
            publicCommand.OperationTimestamp,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        if (replacing)
        {
            RetentionPolicyReplacedDomainEvent replaced = new(
                eventMetadata,
                publicCommand.PolicyReference,
                state.ActiveRetentionPolicy!.PolicyReference,
                publicCommand.Rationale,
                command.AuditEvidence,
                commandMetadata.IdempotencyKey);
            return DomainResult.Success(new IEventPayload[] { replaced });
        }

        RetentionPolicySetDomainEvent set = new(
            eventMetadata,
            publicCommand.PolicyReference,
            publicCommand.Rationale,
            command.AuditEvidence,
            commandMetadata.IdempotencyKey);
        return DomainResult.Success(new IEventPayload[] { set });
    }
}
