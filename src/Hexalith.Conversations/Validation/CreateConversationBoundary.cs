// <copyright file="CreateConversationBoundary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Validation;

/// <summary>
/// Maps public create-conversation contracts into the domain aggregate.
/// </summary>
public static class CreateConversationBoundary
{
    /// <summary>
    /// Dispatches a public create-conversation command through the domain aggregate.
    /// </summary>
    /// <param name="command">The public create-conversation command.</param>
    /// <param name="conversationId">The assigned Conversations-owned identity.</param>
    /// <param name="createdAt">The deterministic creation timestamp supplied by the boundary.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <param name="state">The current conversation state, when any.</param>
    /// <returns>A domain result containing one creation event or one typed rejection.</returns>
    public static DomainResult Dispatch(
        CreateConversationCommand? command,
        ConversationId? conversationId,
        DateTimeOffset createdAt,
        string eventId,
        ConversationState? state = null)
    {
        if (command is null)
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new ConversationRejectedDomainEvent(ConversationErrorCode.CommandValidationFailed, "command_missing"),
            });
        }

        CreateConversation domainCommand = new(command, conversationId, createdAt, eventId);
        return ConversationAggregate.Handle(domainCommand, state);
    }
}
