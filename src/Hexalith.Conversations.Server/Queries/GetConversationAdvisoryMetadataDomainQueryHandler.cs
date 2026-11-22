using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Http;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>Reads only committed isolated metadata between two ordinary owner Read decisions.</summary>
/// <param name="access">The ordinary owner authorization service.</param>
/// <param name="accessor">The SDK-verified exact caller and request.</param>
/// <param name="storeOptions">The provider-local component binding; legacy direct callers use the SDK default.</param>
/// <param name="store">The SDK compacted read-model store.</param>
public sealed class GetConversationAdvisoryMetadataDomainQueryHandler(
    IConversationTenantAccessService access, IHttpContextAccessor accessor, IReadModelStore store, ConversationProjectionStoreOptions storeOptions) : IDomainQueryHandler
{
    private readonly ConversationProjectionStoreOptions _storeOptions = storeOptions ?? throw new ArgumentNullException(nameof(storeOptions));

    /// <summary>Preserves the existing direct-construction API using the SDK default.</summary>
    /// <param name="access">The owner authorization service.</param>
    /// <param name="accessor">The SDK-verified caller context.</param>
    /// <param name="store">The SDK read-model store.</param>
    public GetConversationAdvisoryMetadataDomainQueryHandler(IConversationTenantAccessService access, IHttpContextAccessor accessor, IReadModelStore store)
        : this(access, accessor, store, ConversationProjectionStoreOptions.Legacy)
    {
    }

    /// <inheritdoc/>
    public string Domain => "conversations";

    /// <inheritdoc/>
    public string QueryType => GetConversationAdvisoryMetadataQuery.QueryType;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Verified(query)) return QueryResult.Failure("safe-denial");
        var observation = await ReadAuthorizedAsync(new(query.TenantId), new(query.AggregateId), query.UserId,
            cancellationToken, requireMatchingGeneration: false).ConfigureAwait(false);
        return observation.Result is { } result ? Success(result) : QueryResult.Failure("safe-denial");
    }

    /// <summary>Composes bounded detail from the authorized isolated metadata and its freshness.</summary>
    internal async Task<ConversationDetailResult> ReadDetailAsync(GetConversationQuery query, CancellationToken cancellationToken)
    {
        var observation = await ReadAuthorizedAsync(query.TenantId, query.ConversationId, query.CallerPrincipalId,
            cancellationToken, requireMatchingGeneration: true)
            .ConfigureAwait(false);
        if (observation.Result is null)
        {
            return ConversationDetailResult.Hidden(query.SchemaVersion);
        }

        if (observation.Result is not { Incomplete: false, Metadata: { } metadata } || observation.Freshness is not { } freshness)
        {
            return ConversationDetailResult.Unavailable(query.SchemaVersion);
        }

        var detail = new ConversationDetailsV1(freshness.ProjectionContractSchemaVersion, query.TenantId, query.ConversationId,
            freshness, metadata.LifecycleState, metadata.SafeLabel, ProjectId: metadata.ProjectId is null ? null : new ProjectId(metadata.ProjectId));
        return ConversationDetailResult.Visible(query.SchemaVersion, detail, "Classified advisory evidence is available.")
            with { ReadPurpose = ConversationResolutionScoringLabel.SupportedPurpose };
    }

    private async Task<(ConversationAdvisoryMetadataResult? Result, ProjectionFreshnessV1? Freshness)> ReadAuthorizedAsync(
        TenantId tenant, ConversationId resource, string actor, CancellationToken cancellationToken, bool requireMatchingGeneration)
    {
        try
        {
            if (!await AllowedAsync(tenant, actor, cancellationToken).ConfigureAwait(false))
                return default;

            var observation = await ReadCommittedEvidenceAsync(tenant, resource, cancellationToken, requireMatchingGeneration).ConfigureAwait(false);
            if (!await AllowedAsync(tenant, actor, cancellationToken).ConfigureAwait(false))
                return default;

            cancellationToken.ThrowIfCancellationRequested();
            return observation;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return Unavailable(); }
    }

    private async Task<(ConversationAdvisoryMetadataResult? Result, ProjectionFreshnessV1? Freshness)> ReadCommittedEvidenceAsync(
        TenantId tenant, ConversationId resource, CancellationToken cancellationToken, bool requireMatchingGeneration)
    {
        try
        {
            // The optional isolated fence rejects known pending rebuilds without requiring native-head proof.
            string proofKey = ConversationProjectionReadModelKeys.ResolutionProofKey(tenant, resource);
            var before = await store.GetCompactedAsync<ConversationResolutionMetadataProof>(
                _storeOptions.StateStoreName, proofKey, cancellationToken).ConfigureAwait(false);
            if (Pending(before)) return Unavailable();
            var entry = await store.GetCompactedAsync<ConversationResolutionMetadataReadModel>(
                _storeOptions.StateStoreName,
                ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenant, resource), cancellationToken).ConfigureAwait(false);
            if (entry.Value is null)
            {
                var summaries = await store.GetCompactedAsync<ConversationProjectionIndexReadModel>(
                    _storeOptions.StateStoreName,
                    ConversationProjectionReadModelKeys.TenantIndexKey(tenant), cancellationToken).ConfigureAwait(false);
                return summaries.Value?.Summaries?.Any(summary => summary.TenantId == tenant && summary.ConversationId == resource) == true
                    ? Unavailable() : default;
            }
            var value = entry.Value;
            if (string.IsNullOrEmpty(entry.ETag) || value.TenantId != tenant || value.ConversationId != resource
                || value.Metadata is null || value.Metadata.ConversationId != resource.Value || value.Freshness is null
                || value.Freshness.ProjectionContractSchemaVersion != SchemaVersion.Current
                || value.Freshness.FreshnessState != ProjectionTrustState.Current && value.Freshness.FreshnessState != ProjectionTrustState.Stale
                || value.NativeSourceProof is { Erased: true }
                || !Canonical(value.DispatchId)
                || value.Metadata.LifecycleState is not ("Initializing" or "Open" or "Closed" or "Archived")
                || value.Metadata.ProjectId is not null && !Canonical(value.Metadata.ProjectId)) return Unavailable();
            var after = await store.GetCompactedAsync<ConversationResolutionMetadataProof>(
                _storeOptions.StateStoreName, proofKey, cancellationToken).ConfigureAwait(false);
            if (Pending(after) || before.ETag != after.ETag || before.Value != after.Value) return Unavailable();
            if (requireMatchingGeneration && before.Value?.Current is { } generation && !generation.Matches(value)) return Unavailable();

            bool classified = value.IsLabelComplete
                && value.LabelPurpose == ConversationResolutionScoringPolicy.Purpose
                && value.LabelClassification == ConversationResolutionScoringPolicy.Classification
                && ConversationResolutionScoringPolicy.CanClassify(value.Metadata.SafeLabel, value.LabelPurpose);
            // Missing legacy classification omits the label; it cannot be inferred from label text.
            var metadata = value.Metadata with { SafeLabel = classified ? value.Metadata.SafeLabel : null };
            cancellationToken.ThrowIfCancellationRequested();
            return (new(metadata, !classified), value.Freshness);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return Unavailable(); }
    }

    private async Task<bool> AllowedAsync(TenantId tenant, string actor, CancellationToken token)
    {
        var decision = await access.CheckAccessAsync(ConversationTenantAccessRequirement.Read, tenant, actor,
            routeTenantId: tenant, aggregateTenantId: tenant, projectionTenantId: tenant, cancellationToken: token).ConfigureAwait(false);

        // Every non-allowed decision, including an unhealthy or unavailable tenant projection, is the unknown-tenant
        // safe denial; the internal retry hint never reaches the delegated caller. Only a thrown exception is unavailable.
        return decision.IsAllowed && decision.Requirement == ConversationTenantAccessRequirement.Read
            && decision.TenantId == tenant && decision.CallerPrincipalId == actor;
    }

    private bool Verified(QueryEnvelope query)
    {
        VerifiedQueryCaller? verified = VerifiedQueryCaller.Get(accessor.HttpContext);
        OriginatingQueryContext? origin = verified?.OriginatingContext;
        if (verified is null || !ReferenceEquals(verified.Query, query) || origin is null
            || query.CallerAuthority?.Origin != "delegated-owner-query" || query.Domain != Domain || query.QueryType != QueryType
            || query.AuthenticatedWorkloadId != "projects" || !query.IsDelegated || query.IsGlobalAdmin
            || !Canonical(query.TenantId) || !Canonical(query.AggregateId) || !Canonical(query.UserId)
            || query.OriginalActorId != query.UserId || origin.OriginalActorId != query.UserId
            || !Canonical(origin.AuthenticatedWorkloadId)
            || origin.IsDelegated && !Canonical(origin.DelegationId) || !origin.IsDelegated && origin.DelegationId is not null
            || origin.Scopes is { Count: > 0 } && !origin.Scopes.Contains("projects.read", StringComparer.Ordinal)
            || query.Paging is not null || query.EntityId is not null && query.EntityId != query.AggregateId
            || query.Payload is not { Length: <= 8192 }) return false;
        try
        {
            using JsonDocument payload = JsonDocument.Parse(query.Payload);
            return payload.RootElement.ValueKind == JsonValueKind.Object && !payload.RootElement.EnumerateObject().Any();
        }
        catch (JsonException) { return false; }
    }

    private static bool Pending(ReadModelEntry<ConversationResolutionMetadataProof> entry)
        => entry.Value is not null && (string.IsNullOrEmpty(entry.ETag) || entry.Value.Current is null
            || entry.Value.Current.IsPending || entry.Value.Barrier is not null);
    private static bool Canonical(string? value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim();
    private static (ConversationAdvisoryMetadataResult Result, ProjectionFreshnessV1? Freshness) Unavailable() => (new(null, true), null);
    private static QueryResult Success(ConversationAdvisoryMetadataResult result)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, ConversationsJsonContext.Default.ConversationAdvisoryMetadataResult));
}
