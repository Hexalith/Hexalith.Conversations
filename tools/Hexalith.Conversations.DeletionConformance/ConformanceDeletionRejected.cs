using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Content-free typed non-acceptance; it carries no acknowledgement.</summary>
/// <param name="Outcome">The closed denial, conflict or unavailable result.</param>
public sealed record ConformanceDeletionRejected(ConversationAgentsOutcome Outcome) : IRejectionEvent;
