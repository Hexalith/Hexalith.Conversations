using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Requests a qualification-only receipt for one exact source publication.</summary>
/// <param name="Signal">The original source-atomic approved publication.</param>
/// <param name="DeliveryAttemptId">The independently persisted delivery attempt.</param>
/// <param name="TargetVersion">The exact installed receiver target.</param>
public sealed record AcceptConformanceDeletion(ConversationDeletionSignal Signal, string DeliveryAttemptId, string TargetVersion);
