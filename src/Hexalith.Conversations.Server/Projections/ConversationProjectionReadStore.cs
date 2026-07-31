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
/// <para>
/// This closes the production read-store binding deferred from Story 2.3: the five query/governance services
/// that require <see cref="IConversationProjectionReadStore"/> now resolve from the real host. The read side
/// is intentionally thin — authorization, freshness gating, and the fail-closed shapes stay in
/// <see cref="ConversationProjectionReadService"/> and <see cref="Queries.ConversationQueryHandler"/>, which
/// authorize before any store read. A different tenant resolves to a different key, so cross-tenant reads are
/// impossible by construction.
/// </para>
/// <para>
/// Cross-key generation consistency is scoped to the conversation, never to the tenant. A conversation whose
/// detail key, index entry and dispatch ledger disagree is withheld and reported through
/// <see cref="ValidatePageAsync"/>; unrelated conversations in the same tenant stay readable. Listing reads
/// the index once and verifies only the requested page, so the cost of a list is proportional to the page and
/// not to the tenant's conversation count.
/// A completed ledger is a bounded redelivery-window guard rather than permanent projection state: after it
/// expires, an internally matching detail/index generation remains readable. Any ledger that is still present
/// must be completed and identity-consistent, so an in-flight or poisoned dispatch continues to fail closed.
/// </para>
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

        // A misfiled or poisoned record is returned unvalidated on purpose: the caller's poison guard maps it
        // to Forbidden/PoisonEvent, which is a stronger and more specific signal than a generation conflict.
        // Every caller must apply that guard — see ConversationProjectionReadService.ProjectionMatchesRequest.
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

        // Nothing persisted for this conversation. A pending dispatch marker alone must not make an
        // unbuilt conversation distinguishable from one that never existed: the caller maps null to the same
        // non-disclosing shape it uses for an unknown identifier.
        if (models is null && indexed.Length == 0)
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
        ConversationProjectionDispatchLedger? ledger = await ReadLedgerAsync(
            dispatchReference,
            tenantId,
            conversationId,
            cancellationToken).ConfigureAwait(false);
        if (dispatchReference.IsPending
            || !string.Equals(models.DispatchId, dispatchReference.DispatchId, StringComparison.Ordinal)
            || dispatchReference.LastAppliedEventPosition != indexedSummary.Freshness.LastAppliedEventPosition
            || !SameSummary(indexedSummary, models.Summary)
            || !SameGeneration(models.Summary.Freshness, models.Detail.Freshness)
            || (ledger is not null
                && ledger.ProjectionGeneratedAt.UtcTicks != models.Detail.Freshness.ProjectionGeneratedAt.UtcTicks))
        {
            throw new ConversationProjectionConsistencyException();
        }

        return models;
    }

    /// <inheritdoc/>
    public async ValueTask<ConversationProjectionIndexSnapshot> ListAsync(
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

        // A tenant that has never had a conversation has no index key. That is an empty tenant, not a
        // cross-key inconsistency, and it must read as an empty current page.
        //
        // DISCLOSED LIMITATION (pass-10 review, decision D3). An index key that once existed and was then
        // erased or evicted is indistinguishable from a tenant that never had one, so destroyed state is
        // served as an empty CURRENT page rather than as Rebuilding or Unavailable. Deriving the difference
        // from surviving per-conversation detail keys was evaluated and is not implementable against this
        // seam: IReadModelStore exposes only GetAsync/SaveAsync/TrySaveAsync and IReadModelBulkStore only
        // GetManyAsync(keys), so a tenant's detail keys can only be discovered from the index itself — the
        // very key that is missing. Closing this needs a durable tenant-index write at first use, which is
        // out of Story 6.2's scope; it is disclosed in the AC6 release evidence instead.
        if (entry.Value is null)
        {
            return ConversationProjectionIndexSnapshot.Empty;
        }

        IReadOnlyList<ConversationSummaryProjectionV1> summaries = entry.Value.Summaries;

        // The index naming one conversation twice is structural corruption of the index itself: no page taken
        // from it can be trusted, so this is the one tenant-scoped failure that remains.
        if (summaries.Select(summary => summary.ConversationId.Value).Distinct(StringComparer.Ordinal).Count() != summaries.Count)
        {
            throw new ConversationProjectionConsistencyException();
        }

        IReadOnlyDictionary<string, ConversationProjectionDispatchReference> dispatches = entry.Value.Dispatches;
        Dictionary<string, long> positions = summaries.ToDictionary(
            summary => summary.ConversationId.Value,
            summary => summary.Freshness.LastAppliedEventPosition,
            StringComparer.Ordinal);

        // A dispatch reference the summaries do not reflect means an accepted conversation may be missing from
        // every page. Callers must not report such a page as current, but they may still return the rows they
        // hold: an omission degrades freshness, it does not invalidate unrelated conversations.
        bool hasIncompleteDispatch = dispatches.Any(pair =>
            !positions.TryGetValue(pair.Key, out long position)
            || position != pair.Value.LastAppliedEventPosition
            || pair.Value.IsPending
            || string.IsNullOrWhiteSpace(pair.Value.DispatchId));

        return new ConversationProjectionIndexSnapshot
        {
            Summaries = summaries,
            Dispatches = dispatches,
            HasIncompleteDispatch = hasIncompleteDispatch,
        };
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlySet<string>> ValidatePageAsync(
        TenantId tenantId,
        ConversationProjectionIndexSnapshot snapshot,
        IReadOnlyList<ConversationSummaryProjectionV1> page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(page);

        HashSet<string> inconsistent = new(StringComparer.Ordinal);
        if (page.Count == 0)
        {
            return inconsistent;
        }

        List<ConversationSummaryProjectionV1> verifiable = [];
        Dictionary<string, ConversationProjectionDispatchReference> references = new(StringComparer.Ordinal);
        foreach (ConversationSummaryProjectionV1 summary in page)
        {
            string conversationId = summary.ConversationId.Value;
            if (summary.TenantId != tenantId
                || !snapshot.Dispatches.TryGetValue(conversationId, out ConversationProjectionDispatchReference? reference)
                || reference is null
                || string.IsNullOrWhiteSpace(reference.DispatchId)
                || reference.IsPending
                || reference.LastAppliedEventPosition != summary.Freshness.LastAppliedEventPosition)
            {
                _ = inconsistent.Add(conversationId);
                continue;
            }

            verifiable.Add(summary);
            references[conversationId] = reference;
        }

        if (verifiable.Count == 0)
        {
            return inconsistent;
        }

        string[] detailKeys = [.. verifiable.Select(summary =>
            ConversationProjectionReadModelKeys.ConversationKey(tenantId, summary.ConversationId))];
        IReadOnlyDictionary<string, ConversationProjectedReadModels?> details = await BulkReadAsync<ConversationProjectedReadModels>(
            detailKeys,
            cancellationToken).ConfigureAwait(false);

        string[] ledgerKeys = [.. references.Values
            .Select(reference => ConversationProjectionReadModelKeys.DispatchLedgerKey(reference.DispatchId))
            .Distinct(StringComparer.Ordinal)];
        IReadOnlyDictionary<string, ConversationProjectionDispatchLedger?> ledgers = await BulkReadAsync<ConversationProjectionDispatchLedger>(
            ledgerKeys,
            cancellationToken).ConfigureAwait(false);

        for (int index = 0; index < verifiable.Count; index++)
        {
            ConversationSummaryProjectionV1 summary = verifiable[index];
            string conversationId = summary.ConversationId.Value;
            ConversationProjectionDispatchReference reference = references[conversationId];
            ConversationProjectedReadModels? models = details[detailKeys[index]];
            ConversationProjectionDispatchLedger? ledger = ledgers[
                ConversationProjectionReadModelKeys.DispatchLedgerKey(reference.DispatchId)];
            if (models is null
                || (ledger is not null
                    && (ledger.Status != ConversationProjectionDispatchStatus.Completed
                        || !string.Equals(ledger.DispatchId, reference.DispatchId, StringComparison.Ordinal)
                        || ledger.TenantId != tenantId
                        || ledger.ConversationId != summary.ConversationId
                        || string.IsNullOrWhiteSpace(ledger.RequestFingerprint)))
                || models.Summary.TenantId != tenantId
                || models.Detail.TenantId != tenantId
                || models.Summary.ConversationId != summary.ConversationId
                || models.Detail.ConversationId != summary.ConversationId
                || !string.Equals(models.DispatchId, reference.DispatchId, StringComparison.Ordinal)
                || !SameSummary(summary, models.Summary)
                || !SameGeneration(summary.Freshness, models.Detail.Freshness)
                || (ledger is not null
                    && ledger.ProjectionGeneratedAt.UtcTicks != summary.Freshness.ProjectionGeneratedAt.UtcTicks))
            {
                _ = inconsistent.Add(conversationId);
            }
        }

        return inconsistent;
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
            HashSet<string> requested = new(chunk, StringComparer.Ordinal);
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
                if (!requested.Contains(item.Key))
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

    private async Task<ConversationProjectionDispatchLedger?> ReadLedgerAsync(
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
        if (ledger is not null
            && (ledger.Status != ConversationProjectionDispatchStatus.Completed
            || !string.Equals(ledger.DispatchId, reference.DispatchId, StringComparison.Ordinal)
            || ledger.TenantId != tenantId
            || ledger.ConversationId != conversationId
            || string.IsNullOrWhiteSpace(ledger.RequestFingerprint)))
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
