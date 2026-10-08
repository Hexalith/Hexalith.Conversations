using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Current authenticated receiver outcome. Absent is an authoritative exact negative; Unavailable/unknown never permits retry.</summary>
/// <param name="Outcome">Closed typed receiver outcome.</param>
/// <param name="Acknowledgement">Receiver-issued exact persisted receipt, never gateway admission alone.</param>
public sealed record ConversationDeletionReceiverResult(ConversationAgentsOutcome Outcome, ConversationDeletionAcknowledgement? Acknowledgement = null);
