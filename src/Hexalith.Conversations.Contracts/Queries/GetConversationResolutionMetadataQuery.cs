namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>Requests exact-resource metadata solely for internal Projects equality scoring.</summary>
public sealed record GetConversationResolutionMetadataQuery
{
    /// <summary>The supported query discriminator.</summary>
    public const string QueryType = "Hexalith.Conversations.Queries.GetConversationResolutionMetadata.v1";
}
