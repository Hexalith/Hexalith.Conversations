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
/// <param name="store">The isolated projection read seam.</param>
public sealed class GetConversationResolutionMetadataDomainQueryHandler(
    ConversationResolutionMetadataAuthorization authorization,
    IConversationProjectionReadStore store) : IDomainQueryHandler
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

            cancellationToken.ThrowIfCancellationRequested();
            return Success(new(metadata.Metadata, metadata.Freshness, metadata.Freshness.ProjectionCursor,
                final.PermissionVersion, final.AuthorizedAt));
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
