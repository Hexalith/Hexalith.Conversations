namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>Requests committed, owner-authorized metadata for advisory Project scoring only.</summary>
public sealed record GetConversationAdvisoryMetadataQuery
{
    /// <summary>The supported advisory query discriminator.</summary>
    public const string QueryType = "Hexalith.Conversations.Queries.GetConversationAdvisoryMetadata.v1";
}
