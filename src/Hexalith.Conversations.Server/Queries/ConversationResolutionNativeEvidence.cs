using System.Text.Json;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>Uses only the existing signed bound-owner SDK and isolated content-free selected-owner key.</summary>
/// <param name="client">The existing delegated SDK transport.</param>
/// <param name="store">The supported read-model store.</param>
public sealed class ConversationResolutionNativeEvidence(DelegatedOwnerQueryClient client, IReadModelStore store)
    : IConversationResolutionNativeEvidence
{
    /// <inheritdoc/>
    public async Task<TenantReadAuthorityResult?> ReadTenantAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        QueryResult result = await client.InvokeAsync(new(query.TenantId, "tenants", query.TenantId,
            ResolveTenantReadAuthority.QueryType, Payload: JsonSerializer.SerializeToElement(new ResolveTenantReadAuthority(), JsonSerializerOptions.Web)),
            cancellationToken).ConfigureAwait(false);
        if (!result.Success) return null;
        TenantReadAuthorityResult? evidence = result.GetPayload().Deserialize<TenantReadAuthorityResult>(JsonSerializerOptions.Web);
        return evidence?.TenantId == query.TenantId && evidence.PrincipalId == query.UserId
            && evidence.NativeProof is { TenantId: "system", Domain: "tenants", Erased: false }
            && evidence.NativeProof.AggregateId == query.TenantId ? evidence : null;
    }

    /// <inheritdoc/>
    public async Task<ProjectionCurrentProof?> ReadProjectionAsync(ConversationResolutionMetadataReadModel metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var selected = await store.GetAsync<ConversationResolutionNativeSelection>(ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(metadata.TenantId, metadata.ConversationId), cancellationToken).ConfigureAwait(false);
        ProjectionOwnerSelection? selection = selected.Value?.ProjectionOwner;
        if (string.IsNullOrEmpty(selected.ETag) || selection is null || selection.OwnerGeneration != metadata.Digest()
            || selection.TenantId != metadata.TenantId.Value || selection.AggregateId != metadata.ConversationId.Value
            || selection.Domain != ConversationProjectionHandler.ConversationDomain
            || selection.ProjectionName != ConversationAsyncProjectionHandler.ConversationReadModelProjectionType
            || selection.AppliedSequence != metadata.SourceHeadSequence || selection.MessageId != metadata.SourceHeadMessageId
            || selection.FreshnessVersion != metadata.Freshness.ProjectionCursor
            // A coordinated legacy rebuild receipt can carry a rich original manifest before terminal
            // compaction. This strict query must refuse before asking any proof reader to load that row.
            || selection.ReceiptProjectionName is not null) return null;
        QueryResult result = await client.InvokeAsync(new(selection.TenantId, selection.Domain, selection.AggregateId,
            InspectProjectionCurrentQuery.QueryType, Payload: JsonSerializer.SerializeToElement(new InspectProjectionCurrentQuery(selection), JsonSerializerOptions.Web)),
            cancellationToken).ConfigureAwait(false);
        if (!result.Success) return null;
        ProjectionCurrentProof? proof = result.GetPayload().Deserialize<ProjectionCurrentProof>(JsonSerializerOptions.Web);
        return proof?.Owner.Selection == selection && proof.Source.TenantId == selection.TenantId
            && proof.Source.Domain == selection.Domain && proof.Source.AggregateId == selection.AggregateId
            && proof.Source.HeadSequence == selection.AppliedSequence && proof.Source.HeadMessageId == selection.MessageId ? proof : null;
    }
}
