namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>The authorized subset used solely for internal equality scoring.</summary>
/// <param name="ConversationId">The owner identity.</param>
/// <param name="ProjectId">The native Project membership, if any.</param>
/// <param name="SafeLabel">The exact bounded owner-classified scoring label, if any.</param>
/// <param name="LifecycleState">The native lifecycle.</param>
public sealed record ConversationResolutionMetadataV1(
    string ConversationId, string? ProjectId, string? SafeLabel, string LifecycleState);
