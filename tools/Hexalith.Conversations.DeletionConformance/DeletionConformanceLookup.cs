using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Exact independent lookup of one logical source and receiver target; transport attempts are not receipt identities.</summary>
/// <param name="Signal">The complete immutable source binding.</param>
/// <param name="DeliveryAttemptId">The current independent transport attempt.</param>
/// <param name="TargetVersion">The exact current or retained target.</param>
public sealed record DeletionConformanceLookup(ConversationDeletionSignal Signal, string DeliveryAttemptId, string TargetVersion);
