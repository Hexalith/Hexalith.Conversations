// <copyright file="ConversationProjectionReadStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Production <see cref="IConversationProjectionReadStore"/> that reads persisted conversation read models
/// through the shared EventStore <see cref="IReadModelStore"/> by stable, tenant-scoped key.
/// </summary>
/// <remarks>
/// This closes the production read-store binding deferred from Story 2.3: the five query/governance services
/// that require <see cref="IConversationProjectionReadStore"/> now resolve from the real host. The read side
/// is intentionally thin — authorization, freshness gating, and the fail-closed shapes stay in
/// <see cref="ConversationProjectionReadService"/> and <see cref="Queries.ConversationQueryHandler"/>, which
/// authorize before any store read. A different tenant resolves to a different key, so cross-tenant reads are
/// impossible by construction.
/// </remarks>
public sealed class ConversationProjectionReadStore : IConversationProjectionReadStore
{
    private readonly IReadModelStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionReadStore"/> class.
    /// </summary>
    /// <param name="store">The shared read-model store.</param>
    public ConversationProjectionReadStore(IReadModelStore store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <inheritdoc/>
    public async ValueTask<ConversationProjectedReadModels?> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);

        ReadModelEntry<ConversationProjectedReadModels> entry = await _store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(tenantId, conversationId),
                cancellationToken)
            .ConfigureAwait(false);

        ConversationProjectedReadModels? models = entry.Value;
        if (models is null)
        {
            return null;
        }

        // Preserve the higher-level poison guard: return an identity-mismatched pair so the read service can
        // classify it without consulting a key derived from the poisoned payload.
        if (models.Summary.TenantId != tenantId
            || models.Detail.TenantId != tenantId
            || models.Summary.ConversationId != conversationId
            || models.Detail.ConversationId != conversationId)
        {
            return models;
        }

        ReadModelEntry<ConversationProjectionIndexReadModel> indexEntry = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);
        ConversationSummaryProjectionV1? indexedSummary = indexEntry.Value?.Summaries.SingleOrDefault(
            summary => summary.TenantId == tenantId && summary.ConversationId == conversationId);

        if (indexedSummary is null
            || !SameGeneration(models.Summary.Freshness, models.Detail.Freshness)
            || !SameGeneration(indexedSummary.Freshness, models.Detail.Freshness))
        {
            throw new ConversationProjectionConsistencyException();
        }

        return models;
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        ReadModelEntry<ConversationProjectionIndexReadModel> entry = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);

        if (entry.Value is null)
        {
            throw new ConversationProjectionConsistencyException();
        }

        IReadOnlyList<ConversationSummaryProjectionV1> summaries = entry.Value.Summaries;
        foreach (ConversationSummaryProjectionV1 summary in summaries)
        {
            if (summary.TenantId != tenantId)
            {
                throw new ConversationProjectionConsistencyException();
            }

            ReadModelEntry<ConversationProjectedReadModels> detailEntry = await _store
                .GetAsync<ConversationProjectedReadModels>(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ConversationProjectionReadModelKeys.ConversationKey(tenantId, summary.ConversationId),
                    cancellationToken)
                .ConfigureAwait(false);
            ConversationProjectedReadModels? models = detailEntry.Value;
            if (models is null
                || models.Summary.TenantId != tenantId
                || models.Detail.TenantId != tenantId
                || models.Summary.ConversationId != summary.ConversationId
                || models.Detail.ConversationId != summary.ConversationId
                || !SameGeneration(summary.Freshness, models.Summary.Freshness)
                || !SameGeneration(summary.Freshness, models.Detail.Freshness))
            {
                throw new ConversationProjectionConsistencyException();
            }
        }

        return summaries;
    }

    private static bool SameGeneration(ProjectionFreshnessV1 first, ProjectionFreshnessV1 second)
        => first.ProjectionCursor == second.ProjectionCursor
            && first.LastAppliedEventPosition == second.LastAppliedEventPosition
            && first.LastAppliedEventTimestamp.UtcTicks == second.LastAppliedEventTimestamp.UtcTicks
            && first.ProjectionGeneratedAt.UtcTicks == second.ProjectionGeneratedAt.UtcTicks;
}
