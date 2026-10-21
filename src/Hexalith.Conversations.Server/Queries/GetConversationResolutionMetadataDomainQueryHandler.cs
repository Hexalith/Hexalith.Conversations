using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>Serves isolated, owner-authorized metadata solely for internal Projects equality scoring.</summary>
/// <param name="authorization">The fixed-purpose exact-resource authorization boundary.</param>
/// <param name="native">The strict native source and permission gate; absence withholds metadata.</param>
/// <param name="store">The isolated projection read seam.</param>
public sealed class GetConversationResolutionMetadataDomainQueryHandler(
    ConversationResolutionMetadataAuthorization authorization,
    IConversationProjectionReadStore store,
    IConversationResolutionNativeEvidence? native = null) : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => "conversations";

    /// <inheritdoc/>
    public string QueryType => GetConversationResolutionMetadataQuery.QueryType;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (!authorization.IsVerifiedScoringQuery(query))
        {
            return QueryResult.Failure("safe-denial");
        }

        try
        {
            TenantId tenant = new(query.TenantId);
            ConversationId resource = new(query.AggregateId);
            var first = await authorization.CheckAsync(tenant, resource, query.UserId, ConversationResolutionMetadataAuthorization.ScoringPurpose, cancellationToken).ConfigureAwait(false);
            if (first.Denied)
            {
                return QueryResult.Failure("safe-denial");
            }

            if (first.PermissionVersion is null)
            {
                return Unavailable();
            }

            var tenantBefore = native is null ? null : await native.ReadTenantAsync(query, cancellationToken).ConfigureAwait(false);
            if (tenantBefore is null) return Unavailable();
            if (!tenantBefore.Allowed) return QueryResult.Failure("safe-denial");
            if (authorization.UsesNativeEvidence && first.PermissionVersion != JsonSerializer.Serialize(tenantBefore.NativeProof, JsonSerializerOptions.Web))
                return Unavailable();
            ConversationResolutionMetadataReadModel? metadata = await store.ReadResolutionMetadataAsync(tenant, resource, cancellationToken)
                .ConfigureAwait(false);
            if (metadata is null)
            {
                return QueryResult.Failure("safe-denial");
            }

            if (!Usable(metadata, tenant, resource))
            {
                return Unavailable();
            }

            var resourceBefore = await native!.ReadProjectionAsync(metadata, cancellationToken).ConfigureAwait(false);
            if (resourceBefore is null || metadata.NativeSourceProof is null || resourceBefore.Source != metadata.NativeSourceProof) return Unavailable();
            var final = await authorization.CheckAsync(tenant, resource, query.UserId, ConversationResolutionMetadataAuthorization.ScoringPurpose, cancellationToken).ConfigureAwait(false);
            if (final.Denied)
            {
                return QueryResult.Failure("safe-denial");
            }

            if (final.PermissionVersion is null || first.PermissionVersion != final.PermissionVersion)
            {
                return Unavailable();
            }

            // Re-read after the final policy check; no metadata can be borrowed from an earlier generation.
            ConversationResolutionMetadataReadModel? confirmed = await store.ReadResolutionMetadataAsync(tenant, resource, cancellationToken)
                .ConfigureAwait(false);
            if (confirmed is null || confirmed != metadata || !Usable(confirmed, tenant, resource)
                || !await authorization.ConfirmVersionAsync(tenant, final.PermissionVersion, cancellationToken).ConfigureAwait(false))
            {
                return Unavailable();
            }

            var resourceAfter = await native!.ReadProjectionAsync(confirmed, cancellationToken).ConfigureAwait(false);
            // Native permission is sampled after the final awaited resource I/O. Revoke/regrant cannot
            // borrow the earlier Allowed decision, even if it restores the same role at a new native prefix.
            var tenantAfter = await native.ReadTenantAsync(query, cancellationToken).ConfigureAwait(false);
            if (tenantAfter is { Allowed: false }) return QueryResult.Failure("safe-denial");
            if (tenantAfter is null || tenantAfter.NativeProof != tenantBefore.NativeProof
                || tenantAfter.TenantId != query.TenantId || tenantAfter.PrincipalId != query.UserId
                || resourceAfter != resourceBefore
                || authorization.UsesNativeEvidence && final.PermissionVersion != JsonSerializer.Serialize(tenantAfter.NativeProof, JsonSerializerOptions.Web))
                return Unavailable();
            cancellationToken.ThrowIfCancellationRequested();
            return Success(new(metadata.Metadata, metadata.Freshness,
                JsonSerializer.Serialize(resourceAfter.Source, JsonSerializerOptions.Web),
                JsonSerializer.Serialize(tenantAfter.NativeProof, JsonSerializerOptions.Web), tenantAfter.VerifiedAt));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable();
        }
    }

    private static bool Usable(ConversationResolutionMetadataReadModel value, TenantId tenant, ConversationId resource)
        => value.TenantId == tenant && value.ConversationId == resource && value.IsWellFormed()
            && value.IsLabelComplete && value.Freshness.AllowsTrustBearingDecision();

    private static QueryResult Unavailable() => Success(new(null, null, null, null, null));
    private static QueryResult Success(ConversationResolutionMetadataResultV1 result)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, ConversationsJsonContext.Default.ConversationResolutionMetadataResultV1),
            GetConversationResolutionMetadataQuery.QueryType);
}
