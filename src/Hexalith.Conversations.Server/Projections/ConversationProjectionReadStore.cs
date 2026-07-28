// <copyright file="ConversationProjectionReadStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

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
    private const int BulkReadChunkSize = 100;
    private const int BulkReadParallelism = 8;
    private static readonly JsonSerializerOptions ComparisonJsonOptions = new(JsonSerializerDefaults.Web);
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
        if (models is not null
            && (models.Summary.TenantId != tenantId
                || models.Detail.TenantId != tenantId
                || models.Summary.ConversationId != conversationId
                || models.Detail.ConversationId != conversationId))
        {
            return models;
        }

        ReadModelEntry<ConversationProjectionIndexReadModel> indexEntry = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);

        ConversationSummaryProjectionV1[] indexed = [.. (indexEntry.Value?.Summaries ?? [])
            .Where(summary => summary.ConversationId == conversationId)];
        ConversationProjectionDispatchReference? dispatchReference = null;
        bool hasDispatch = indexEntry.Value?.Dispatches.TryGetValue(
            conversationId.Value,
            out dispatchReference) == true;

        if (models is null && indexed.Length == 0 && !hasDispatch)
        {
            return null;
        }

        if (models is null
            || indexed.Length != 1
            || !hasDispatch
            || dispatchReference is null)
        {
            throw new ConversationProjectionConsistencyException();
        }

        ConversationSummaryProjectionV1 indexedSummary = indexed[0];
        ConversationProjectionDispatchLedger ledger = await ReadCompletedLedgerAsync(
            dispatchReference,
            tenantId,
            conversationId,
            cancellationToken).ConfigureAwait(false);
        if (!string.Equals(models.DispatchId, dispatchReference.DispatchId, StringComparison.Ordinal)
            || dispatchReference.LastAppliedEventPosition != indexedSummary.Freshness.LastAppliedEventPosition
            || !SameSummary(indexedSummary, models.Summary)
            || !SameGeneration(models.Summary.Freshness, models.Detail.Freshness)
            || ledger.ProjectionGeneratedAt.UtcTicks != models.Detail.Freshness.ProjectionGeneratedAt.UtcTicks)
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
        if (summaries.Select(summary => summary.ConversationId.Value).Distinct(StringComparer.Ordinal).Count() != summaries.Count
            || entry.Value.Dispatches.Count != summaries.Count)
        {
            throw new ConversationProjectionConsistencyException();
        }

        string[] detailKeys = [.. summaries.Select(summary =>
            ConversationProjectionReadModelKeys.ConversationKey(tenantId, summary.ConversationId))];
        IReadOnlyDictionary<string, ConversationProjectedReadModels?> details = await BulkReadAsync<ConversationProjectedReadModels>(
            detailKeys,
            cancellationToken).ConfigureAwait(false);

        ConversationProjectionDispatchReference[] references = new ConversationProjectionDispatchReference[summaries.Count];
        for (int index = 0; index < summaries.Count; index++)
        {
            ConversationSummaryProjectionV1 summary = summaries[index];
            if (summary.TenantId != tenantId
                || !entry.Value.Dispatches.TryGetValue(
                    summary.ConversationId.Value,
                    out ConversationProjectionDispatchReference? reference)
                || reference is null
                || string.IsNullOrWhiteSpace(reference.DispatchId)
                || reference.LastAppliedEventPosition != summary.Freshness.LastAppliedEventPosition)
            {
                throw new ConversationProjectionConsistencyException();
            }

            references[index] = reference;
        }

        string[] ledgerKeys = [.. references
            .Select(reference => ConversationProjectionReadModelKeys.DispatchLedgerKey(reference.DispatchId))
            .Distinct(StringComparer.Ordinal)];
        IReadOnlyDictionary<string, ConversationProjectionDispatchLedger?> ledgers = await BulkReadAsync<ConversationProjectionDispatchLedger>(
            ledgerKeys,
            cancellationToken).ConfigureAwait(false);

        for (int index = 0; index < summaries.Count; index++)
        {
            ConversationSummaryProjectionV1 summary = summaries[index];
            ConversationProjectionDispatchReference reference = references[index];
            string detailKey = detailKeys[index];
            string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey(reference.DispatchId);
            ConversationProjectedReadModels? models = details[detailKey];
            ConversationProjectionDispatchLedger? ledger = ledgers[ledgerKey];
            if (models is null
                || ledger is null
                || ledger.Status != ConversationProjectionDispatchStatus.Completed
                || !string.Equals(ledger.DispatchId, reference.DispatchId, StringComparison.Ordinal)
                || ledger.TenantId != tenantId
                || ledger.ConversationId != summary.ConversationId
                || string.IsNullOrWhiteSpace(ledger.RequestFingerprint)
                || models.Summary.TenantId != tenantId
                || models.Detail.TenantId != tenantId
                || models.Summary.ConversationId != summary.ConversationId
                || models.Detail.ConversationId != summary.ConversationId
                || !string.Equals(models.DispatchId, reference.DispatchId, StringComparison.Ordinal)
                || !SameSummary(summary, models.Summary)
                || !SameGeneration(summary.Freshness, models.Detail.Freshness)
                || ledger.ProjectionGeneratedAt.UtcTicks != summary.Freshness.ProjectionGeneratedAt.UtcTicks)
            {
                throw new ConversationProjectionConsistencyException();
            }
        }

        return summaries;
    }

    private async Task<IReadOnlyDictionary<string, TValue?>> BulkReadAsync<TValue>(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
        where TValue : class
    {
        if (_store is not IReadModelBulkStore bulkStore)
        {
            throw new InvalidOperationException("The configured read-model store does not support bounded bulk reads.");
        }

        var values = new Dictionary<string, TValue?>(keys.Count, StringComparer.Ordinal);
        foreach (string[] chunk in keys.Chunk(BulkReadChunkSize))
        {
            IReadOnlyList<ReadModelBulkEntry<TValue>> entries = await bulkStore
                .GetManyAsync<TValue>(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    chunk,
                    BulkReadParallelism,
                    cancellationToken)
                .ConfigureAwait(false);
            if (entries.Count != chunk.Length
                || entries.Select(static item => item.Key).Distinct(StringComparer.Ordinal).Count() != entries.Count)
            {
                throw new ConversationProjectionConsistencyException();
            }

            foreach (ReadModelBulkEntry<TValue> item in entries)
            {
                if (!chunk.Contains(item.Key, StringComparer.Ordinal))
                {
                    throw new ConversationProjectionConsistencyException();
                }

                values[item.Key] = item.Value;
            }
        }

        if (values.Count != keys.Count)
        {
            throw new ConversationProjectionConsistencyException();
        }

        return values;
    }

    private async Task<ConversationProjectionDispatchLedger> ReadCompletedLedgerAsync(
        ConversationProjectionDispatchReference reference,
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken)
    {
        ReadModelEntry<ConversationProjectionDispatchLedger> entry = await _store
            .GetAsync<ConversationProjectionDispatchLedger>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey(reference.DispatchId),
                cancellationToken)
            .ConfigureAwait(false);
        ConversationProjectionDispatchLedger? ledger = entry.Value;
        if (ledger is null
            || ledger.Status != ConversationProjectionDispatchStatus.Completed
            || !string.Equals(ledger.DispatchId, reference.DispatchId, StringComparison.Ordinal)
            || ledger.TenantId != tenantId
            || ledger.ConversationId != conversationId
            || string.IsNullOrWhiteSpace(ledger.RequestFingerprint))
        {
            throw new ConversationProjectionConsistencyException();
        }

        return ledger;
    }

    private static bool SameGeneration(ProjectionFreshnessV1 first, ProjectionFreshnessV1 second)
        => first == second;

    private static bool SameSummary(ConversationSummaryProjectionV1 first, ConversationSummaryProjectionV1 second)
        => JsonElement.DeepEquals(
            JsonSerializer.SerializeToElement(first, ComparisonJsonOptions),
            JsonSerializer.SerializeToElement(second, ComparisonJsonOptions));
}
