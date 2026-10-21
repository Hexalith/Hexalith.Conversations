namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>Observed metadata; neither a currentness certificate nor association authority.</summary>
/// <param name="Metadata">The authorized isolated subset, or null when evaluation was unavailable.</param>
/// <param name="Incomplete">Whether observations or scoring-label coverage were unavailable.</param>
public sealed record ConversationAdvisoryMetadataResult(ConversationResolutionMetadataV1? Metadata, bool Incomplete);
