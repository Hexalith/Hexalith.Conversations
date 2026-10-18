using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.MinimalModule.Contracts;

/// <summary>Event raised when a conversation is created.</summary>
/// <param name="ConversationId">The conversation aggregate identifier.</param>
/// <param name="Title">The conversation title.</param>
public sealed record ConversationCreated(string ConversationId, string Title) : IEventPayload;
