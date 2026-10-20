using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>A current authorized subset, or empty unavailable evidence.</summary>
/// <param name="Metadata">The subset, only with complete current evidence.</param>
/// <param name="Freshness">The native projection freshness.</param>
/// <param name="ResourceVersion">The native resource cursor.</param>
/// <param name="PermissionVersion">The native permission revision and watermark tuple.</param>
/// <param name="AuthorizedAt">The actual final owner verification instant.</param>
public sealed record ConversationResolutionMetadataResultV1(
    ConversationResolutionMetadataV1? Metadata,
    ProjectionFreshnessV1? Freshness,
    string? ResourceVersion, string? PermissionVersion, DateTimeOffset? AuthorizedAt);
