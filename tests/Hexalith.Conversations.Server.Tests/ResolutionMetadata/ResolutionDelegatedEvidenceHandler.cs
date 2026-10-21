using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionDelegatedEvidenceHandler(ConversationResolutionNativeEvidence evidence,
    ConversationResolutionMetadataReadModel metadata) : IDomainQueryHandler
{
    public string Domain => "conversations";
    public string QueryType => GetConversationResolutionMetadataQuery.QueryType;
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(await evidence.ReadProjectionAsync(metadata, cancellationToken)), QueryType);
}
