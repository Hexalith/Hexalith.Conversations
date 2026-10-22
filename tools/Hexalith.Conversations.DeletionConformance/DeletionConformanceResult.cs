using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>A typed conformance result; only an independently read persisted acceptance has an acknowledgement.</summary>
/// <param name="Outcome">The safe typed result.</param>
/// <param name="Acknowledgement">The original persisted receipt, if available.</param>
public sealed record DeletionConformanceResult(ConversationAgentsOutcome Outcome, ConversationDeletionAcknowledgement? Acknowledgement = null);
