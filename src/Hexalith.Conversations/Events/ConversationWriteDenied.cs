using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>The authenticated principal lacks the requested human Party or write access.</summary>
public sealed record ConversationWriteDenied : IRejectionEvent;
