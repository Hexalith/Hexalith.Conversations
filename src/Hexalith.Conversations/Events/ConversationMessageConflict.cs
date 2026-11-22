using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>The message identity or mutation key is already bound to different intent.</summary>
public sealed record ConversationMessageConflict : IRejectionEvent;
