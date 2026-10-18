using Hexalith.Conversations.MinimalModule.Contracts;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.MinimalModule.Server;

/// <summary>Conversation aggregate: a command handler and replay-safe state, discovered by the platform host.</summary>
public sealed class ConversationAggregate : EventStoreAggregate<ConversationState>
{
    /// <summary>Handles <see cref="CreateConversation"/>; creating an existing conversation is a no-op.</summary>
    /// <param name="command">The command.</param>
    /// <param name="state">The current state, or <see langword="null"/> before creation.</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(CreateConversation command, ConversationState? state)
        => state?.Created == true
            ? DomainResult.NoOp()
            : DomainResult.Success(new IEventPayload[] { new ConversationCreated(command.ConversationId, command.Title) });
}

/// <summary>Replay-safe conversation state.</summary>
public sealed class ConversationState
{
    /// <summary>Gets a value indicating whether the conversation exists.</summary>
    public bool Created { get; private set; }

    /// <summary>Gets the conversation title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Applies <see cref="ConversationCreated"/>.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ConversationCreated e)
    {
        Created = true;
        Title = e.Title;
    }
}
