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
    private readonly IReadModelStore _store;
    private readonly ConversationProjectionStoreOptions _storeOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionReadStore"/> class.
    /// </summary>
    /// <param name="store">The shared read-model store.</param>
    public ConversationProjectionReadStore(IReadModelStore store)
        : this(store, ConversationProjectionStoreOptions.Legacy)
    {
    }

    /// <summary>Initializes a reader with the provider's enrolled component binding.</summary>
    /// <param name="store">The SDK store.</param>
    /// <param name="storeOptions">The immutable enrolled component binding.</param>
    public ConversationProjectionReadStore(IReadModelStore store, ConversationProjectionStoreOptions storeOptions)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _storeOptions = storeOptions ?? throw new ArgumentNullException(nameof(storeOptions));
    }

    /// <inheritdoc/>
    public async ValueTask<ConversationResolutionMetadataReadModel?> ReadResolutionMetadataAsync(
        TenantId tenantId, ConversationId conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ReadResolutionMetadataCoreAsync(tenantId, conversationId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            throw new ConversationProjectionConsistencyException();
        }
    }

    private async ValueTask<ConversationResolutionMetadataReadModel?> ReadResolutionMetadataCoreAsync(
        TenantId tenantId, ConversationId conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);
        string proofKey = ConversationProjectionReadModelKeys.ResolutionProofKey(tenantId, conversationId);
        ReadModelEntry<ConversationResolutionMetadataProof> before = await _store
            .GetCompactedAsync<ConversationResolutionMetadataProof>(_storeOptions.StateStoreName,
                proofKey, cancellationToken).ConfigureAwait(false);
        // The outer proof rejects pending/invalidated observations before touching an envelope-wrapped
        // metadata key. Retained live manifests contain only this bounded subset and a content-free fence.
        if (before.Value is not null && string.IsNullOrEmpty(before.ETag)
            || before.Value is { Current: null } || before.Value?.Current?.IsPending == true || before.Value?.Barrier is not null)
        {
            throw new ConversationProjectionConsistencyException();
        }

        ReadModelEntry<ConversationResolutionMetadataReadModel> metadata = await _store
            .GetCompactedAsync<ConversationResolutionMetadataReadModel>(_storeOptions.StateStoreName,
                ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenantId, conversationId), cancellationToken)
            .ConfigureAwait(false);
        ReadModelEntry<ConversationResolutionMetadataProof> after;
        ConversationResolutionGeneration? generation = before.Value?.Current;
        if (generation is null && metadata.Value is null)
        {
            after = await _store.GetCompactedAsync<ConversationResolutionMetadataProof>(
                _storeOptions.StateStoreName, proofKey, cancellationToken).ConfigureAwait(false);
            if (after.Value is null && after.ETag == before.ETag)
            {
                return null;
            }

            throw new ConversationProjectionConsistencyException();
        }

        if (generation is null || generation.NativeSourceProof is null || generation.IsPending || metadata.Value is null || string.IsNullOrEmpty(metadata.ETag)
            || generation.TenantId != tenantId || generation.ConversationId != conversationId
            || !generation.Matches(metadata.Value))
        {
            throw new ConversationProjectionConsistencyException();
        }

        ReadModelEntry<ConversationProjectionDispatchLedger> ledger = await _store
            .GetCompactedAsync<ConversationProjectionDispatchLedger>(_storeOptions.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey(generation.DispatchId), cancellationToken)
            .ConfigureAwait(false);
        if (ledger.Value is not null && (ledger.Value.Status != ConversationProjectionDispatchStatus.Completed
            || ledger.Value.TenantId != tenantId || ledger.Value.ConversationId != conversationId
            || ledger.Value.DispatchId != generation.DispatchId
            || ledger.Value.RequestFingerprint != generation.RequestFingerprint
            || ledger.Value.ProjectionGeneratedAt != generation.Freshness.ProjectionGeneratedAt))
        {
            throw new ConversationProjectionConsistencyException();
        }

        after = await _store.GetCompactedAsync<ConversationResolutionMetadataProof>(
            _storeOptions.StateStoreName, proofKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(after.ETag) || before.ETag != after.ETag || before.Value != after.Value)
        {
            throw new ConversationProjectionConsistencyException();
        }

        return metadata.Value;
    }

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
                _storeOptions.StateStoreName,
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

        ConversationProjectionIndexLookupResult? indexLookup;
        ConversationProjectionIndexLookupTarget? previousTarget = ConversationProjectionIndexLookupScope.Begin(conversationId.Value);
        try
        {
            ReadModelEntry<ConversationProjectionIndexLookupResult> indexEntry = await _store
                .GetAsync<ConversationProjectionIndexLookupResult>(
                    _storeOptions.StateStoreName,
                    ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                    cancellationToken)
                .ConfigureAwait(false);
            indexLookup = indexEntry.Value;
        }
        finally
        {
            ConversationProjectionIndexLookupScope.End(previousTarget);
        }

        int matchCount = indexLookup?.MatchCount ?? 0;
        ConversationSummaryProjectionV1? indexedSummary = indexLookup?.MatchedSummary;
        ConversationProjectionDispatchReference? dispatchReference = indexLookup?.DispatchReference;

        // Nothing persisted for this conversation. A pending dispatch marker alone must not make an
        // unbuilt conversation distinguishable from one that never existed: the caller maps null to the same
        // non-disclosing shape it uses for an unknown identifier.
        if (models is null && matchCount == 0)
        {
            return null;
        }

        if (models is null
            || matchCount != 1
            || indexedSummary is null
            || dispatchReference is null)
        {
            throw new ConversationProjectionConsistencyException();
        }

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
                _storeOptions.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);

        // AD-7: the version 2 index is the tenant's only lifecycle record. TenantCreated initializes it Ready, so a
        // missing record for an authorized tenant is unavailable state, never an empty current page. Reads map
        // what is persisted and never write or infer a lifecycle.
        if (entry.Value is null)
        {
            return ConversationProjectionIndexSnapshot.Missing;
        }

        ConversationProjectionIndexReadModel index = entry.Value;
        IReadOnlyList<ConversationSummaryProjectionV1> summaries = index.Summaries;

        // Single-pass duplicate detection + position map shared across lifecycle classification and incomplete-dispatch checks.
        // The index naming one conversation twice is structural corruption of the index itself: no page taken
        // from it can be trusted, so this is the one tenant-scoped failure that remains.
        Dictionary<string, long> positions = new(summaries.Count, StringComparer.Ordinal);
        for (int i = 0; i < summaries.Count; i++)
        {
            ConversationSummaryProjectionV1 summary = summaries[i];
            if (!positions.TryAdd(summary.ConversationId.Value, summary.Freshness.LastAppliedEventPosition))
            {
                throw new ConversationProjectionConsistencyException();
            }
        }

        ConversationProjectionIndexReadState readState = ClassifyLifecycle(index, positions);
        IReadOnlyDictionary<string, ConversationProjectionDispatchReference> dispatches = index.Dispatches;

        // A dispatch reference the summaries do not reflect means an accepted conversation may be missing from
        // every page. Callers must not report such a page as current, but they may still return the rows they
        // hold: an omission degrades freshness, it does not invalidate unrelated conversations.
        bool hasIncompleteDispatch = false;
        foreach (KeyValuePair<string, ConversationProjectionDispatchReference> pair in dispatches)
        {
            ConversationProjectionDispatchReference reference = pair.Value;
            if (!positions.TryGetValue(pair.Key, out long position)
                || position != reference.LastAppliedEventPosition
                || reference.IsPending
                || string.IsNullOrWhiteSpace(reference.DispatchId))
            {
                hasIncompleteDispatch = true;
                break;
            }
        }

        return new ConversationProjectionIndexSnapshot
        {
            Summaries = summaries,
            Dispatches = dispatches,
            HasIncompleteDispatch = hasIncompleteDispatch,
            ReadState = readState,
        };
    }

    /// <summary>
    /// Maps one persisted tenant index to its read state without writing (AD-7).
    /// </summary>
    /// <param name="index">The persisted index.</param>
    /// <returns>The read state of a well-formed record.</returns>
    /// <exception cref="ConversationProjectionConsistencyException">
    /// Thrown for a contradictory lifecycle record: an unknown schema version or lifecycle, lifecycle fields on a
    /// version 1 record, a malformed watermark, or a summary ahead of its non-regressing watermark.
    /// </exception>
    internal static ConversationProjectionIndexReadState ClassifyLifecycle(ConversationProjectionIndexReadModel index)
    {
        ArgumentNullException.ThrowIfNull(index);
        if (!TryValidateLifecycleHeader(index, out ConversationProjectionIndexLifecycle lifecycle, out IReadOnlyDictionary<string, long> watermarks))
        {
            return ConversationProjectionIndexReadState.Rebuilding;
        }

        foreach (KeyValuePair<string, long> pair in watermarks)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 1)
            {
                throw new ConversationProjectionConsistencyException();
            }
        }

        bool gap = false;
        HashSet<string> listed = new(index.Summaries.Count, StringComparer.Ordinal);
        for (int i = 0; i < index.Summaries.Count; i++)
        {
            ConversationSummaryProjectionV1 summary = index.Summaries[i];
            string conversationId = summary.ConversationId.Value;
            _ = listed.Add(conversationId);
            long position = summary.Freshness.LastAppliedEventPosition;
            if (!watermarks.TryGetValue(conversationId, out long watermark))
            {
                gap = true;
                continue;
            }

            if (position > watermark)
            {
                throw new ConversationProjectionConsistencyException();
            }

            if (position < watermark)
            {
                gap = true;
            }
        }

        foreach (string watermarkId in watermarks.Keys)
        {
            if (!listed.Contains(watermarkId))
            {
                gap = true;
                break;
            }
        }

        return FinalizeLifecycleReadState(lifecycle, gap);
    }

    private static ConversationProjectionIndexReadState ClassifyLifecycle(
        ConversationProjectionIndexReadModel index,
        Dictionary<string, long> positions)
    {
        if (!TryValidateLifecycleHeader(index, out ConversationProjectionIndexLifecycle lifecycle, out IReadOnlyDictionary<string, long> watermarks))
        {
            return ConversationProjectionIndexReadState.Rebuilding;
        }

        bool gap = false;
        int coveredSummaryCount = 0;
        foreach (KeyValuePair<string, long> pair in watermarks)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 1)
            {
                throw new ConversationProjectionConsistencyException();
            }

            if (!positions.TryGetValue(pair.Key, out long position))
            {
                // Watermark covers a conversation not currently listed in the index.
                gap = true;
                continue;
            }

            coveredSummaryCount++;
            if (position > pair.Value)
            {
                // Watermarks never regress and move in the same compare-and-swap as their summary.
                throw new ConversationProjectionConsistencyException();
            }

            // Source-position gap: the index once published a newer position than it now holds.
            if (position < pair.Value)
            {
                gap = true;
            }
        }

        // Lifecycle gap: at least one listed summary is not covered by any watermark.
        if (coveredSummaryCount != positions.Count)
        {
            gap = true;
        }

        return FinalizeLifecycleReadState(lifecycle, gap);
    }

    private static bool TryValidateLifecycleHeader(
        ConversationProjectionIndexReadModel index,
        out ConversationProjectionIndexLifecycle lifecycle,
        out IReadOnlyDictionary<string, long> watermarks)
    {
        if (index.SchemaVersion is 0 or 1)
        {
            // A version 1 record has no lifecycle or v2 generation token; one that carries them contradicts itself.
            if (index.Lifecycle is not null || index.Watermarks is not null || index.GenerationToken is not null)
            {
                throw new ConversationProjectionConsistencyException();
            }

            lifecycle = default;
            watermarks = null!;
            return false;
        }

        if (!index.IsLifecycleRecord
            || index.Lifecycle is not { } parsedLifecycle
            || !Enum.IsDefined(parsedLifecycle)
            || index.Watermarks is not { } parsedWatermarks)
        {
            throw new ConversationProjectionConsistencyException();
        }

        lifecycle = parsedLifecycle;
        watermarks = parsedWatermarks;
        return true;
    }

    private static ConversationProjectionIndexReadState FinalizeLifecycleReadState(
        ConversationProjectionIndexLifecycle lifecycle,
        bool gap)
        => lifecycle == ConversationProjectionIndexLifecycle.Rebuilding
            ? ConversationProjectionIndexReadState.Rebuilding
            : gap
                ? ConversationProjectionIndexReadState.GapDetected
                : ConversationProjectionIndexReadState.Ready;

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

        List<ConversationSummaryProjectionV1> verifiable = new(page.Count);
        List<ConversationProjectionDispatchReference> verifiableReferences = new(page.Count);
        for (int index = 0; index < page.Count; index++)
        {
            ConversationSummaryProjectionV1 summary = page[index];
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
            verifiableReferences.Add(reference);
        }

        if (verifiable.Count == 0)
        {
            return inconsistent;
        }

        int verifiableCount = verifiable.Count;
        string[] detailKeys = new string[verifiableCount];
        string[] rowLedgerKeys = new string[verifiableCount];
        HashSet<string> seenLedgerKeys = new(verifiableCount, StringComparer.Ordinal);
        List<string> uniqueLedgerKeys = new(verifiableCount);
        for (int index = 0; index < verifiableCount; index++)
        {
            detailKeys[index] = ConversationProjectionReadModelKeys.ConversationKey(tenantId, verifiable[index].ConversationId);
            string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey(verifiableReferences[index].DispatchId);
            rowLedgerKeys[index] = ledgerKey;
            if (seenLedgerKeys.Add(ledgerKey))
            {
                uniqueLedgerKeys.Add(ledgerKey);
            }
        }

        // Observe page details BEFORE reading dispatch ledgers. During restoration of a missing detail after
        // ledger expiry, reading ledgers in parallel could observe the expired (absent) ledger before redelivery
        // recreates it pending and then observe the restored detail before dispatch completion, falsely certifying
        // Current. Reading details first guarantees that if a restored detail is observed, the subsequent ledger
        // read observes at least the pending ledger (step 1 precedes detail step 5).
        IReadOnlyDictionary<string, ConversationPageValidationRecord?> details = await BulkReadAsync<ConversationPageValidationRecord>(
            detailKeys,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyDictionary<string, ConversationProjectionDispatchLedger?> ledgers = await BulkReadAsync<ConversationProjectionDispatchLedger>(
            uniqueLedgerKeys,
            cancellationToken).ConfigureAwait(false);

        for (int index = 0; index < verifiableCount; index++)
        {
            ConversationSummaryProjectionV1 summary = verifiable[index];
            string conversationId = summary.ConversationId.Value;
            ConversationProjectionDispatchReference reference = verifiableReferences[index];
            ConversationPageValidationRecord? models = details[detailKeys[index]];
            ConversationProjectionDispatchLedger? ledger = ledgers[rowLedgerKeys[index]];
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
        if (keys.Count <= BulkReadChunkSize)
        {
            await BulkReadChunkAsync(bulkStore, keys, values, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            foreach (string[] chunk in keys.Chunk(BulkReadChunkSize))
            {
                await BulkReadChunkAsync(bulkStore, chunk, values, cancellationToken).ConfigureAwait(false);
            }
        }

        if (values.Count != keys.Count)
        {
            throw new ConversationProjectionConsistencyException();
        }

        return values;
    }

    private async Task BulkReadChunkAsync<TValue>(
        IReadModelBulkStore bulkStore,
        IReadOnlyList<string> chunk,
        Dictionary<string, TValue?> values,
        CancellationToken cancellationToken)
        where TValue : class
    {
        HashSet<string> requested = new(chunk.Count, StringComparer.Ordinal);
        for (int index = 0; index < chunk.Count; index++)
        {
            _ = requested.Add(chunk[index]);
        }

        IReadOnlyList<ReadModelBulkEntry<TValue>> entries = await bulkStore
            .GetManyAsync<TValue>(
                _storeOptions.StateStoreName,
                chunk,
                BulkReadParallelism,
                cancellationToken)
            .ConfigureAwait(false);
        if (entries.Count != chunk.Count)
        {
            throw new ConversationProjectionConsistencyException();
        }

        for (int index = 0; index < entries.Count; index++)
        {
            ReadModelBulkEntry<TValue> item = entries[index];
            if (!requested.Contains(item.Key) || !values.TryAdd(item.Key, item.Value))
            {
                throw new ConversationProjectionConsistencyException();
            }
        }
    }

    private async Task<ConversationProjectionDispatchLedger?> ReadLedgerAsync(
        ConversationProjectionDispatchReference reference,
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken)
    {
        ReadModelEntry<ConversationProjectionDispatchLedger> entry = await _store
            .GetAsync<ConversationProjectionDispatchLedger>(
                _storeOptions.StateStoreName,
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
        => first.SchemaVersion == second.SchemaVersion
            && first.TenantId == second.TenantId
            && first.ConversationId == second.ConversationId
            && first.Freshness == second.Freshness
            && first.Freshness.LastAppliedEventTimestamp.EqualsExact(second.Freshness.LastAppliedEventTimestamp)
            && first.Freshness.ProjectionGeneratedAt.EqualsExact(second.Freshness.ProjectionGeneratedAt)
            && first.LifecycleState == second.LifecycleState
            && first.Label == second.Label
            && first.BusinessReference == second.BusinessReference
            && first.ProjectId == second.ProjectId
            && first.FolderId == second.FolderId
            && first.ParticipantPartyIds.SequenceEqual(second.ParticipantPartyIds)
            && first.MessageCount == second.MessageCount
            && first.FileReferenceCount == second.FileReferenceCount
            && SameProviderCorrelation(first.ProviderCorrelation, second.ProviderCorrelation)
            && first.SearchTrustPreview == second.SearchTrustPreview;

    private static bool SameProviderCorrelation(ProviderCorrelationMetadata? first, ProviderCorrelationMetadata? second)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first is null || second is null || first.ProviderName != second.ProviderName
            || first.ProviderType != second.ProviderType || first.MetadataSchemaVersion != second.MetadataSchemaVersion
            || first.ProviderSessionReference != second.ProviderSessionReference
            || first.ProviderResponseReference != second.ProviderResponseReference)
        {
            return false;
        }

        var firstData = first.ExtensionData;
        var secondData = second.ExtensionData;
        if (ReferenceEquals(firstData, secondData)) return true;
        if (firstData is null || secondData is null || firstData.Count != secondData.Count) return false;

        // Stored JSON object keys are ordinal and independent of insertion order.
        if (firstData.Count == 0)
        {
            return true;
        }

        if (secondData is Dictionary<string, string> secondDict
            && (ReferenceEquals(secondDict.Comparer, StringComparer.Ordinal)
                || ReferenceEquals(secondDict.Comparer, EqualityComparer<string>.Default)))
        {
            foreach (KeyValuePair<string, string> entry in firstData)
            {
                if (!secondDict.TryGetValue(entry.Key, out string? otherValue)
                    || !string.Equals(entry.Value, otherValue, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        Dictionary<string, string> secondOrdinal = new(secondData.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> other in secondData)
        {
            if (!secondOrdinal.TryAdd(other.Key, other.Value))
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, string> entry in firstData)
        {
            if (!secondOrdinal.TryGetValue(entry.Key, out string? otherValue)
                || !string.Equals(entry.Value, otherValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
