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
}
