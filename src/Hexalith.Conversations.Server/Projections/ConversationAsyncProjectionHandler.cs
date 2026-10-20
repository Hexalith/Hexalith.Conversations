// <copyright file="ConversationAsyncProjectionHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Owns the production named Conversations read-model projection and its full-replay rebuild plan.
/// </summary>
/// <remarks>
/// Immediate dispatch marks the content-free read proof pending before legacy monotonic CAS component writes.
/// Only bounded metadata and a private content-free fence use retained SDK batches, so even SDK marker reads
/// cannot retrieve transcript data through this query. Completion requires
/// durable legacy components, isolated metadata, both proofs and the bounded dispatch ledger. Rebuild promotes
/// the same generation atomically through the existing default SDK protocol.
/// </remarks>
public sealed class ConversationAsyncProjectionHandler :
    IAsyncDomainProjectionRebuildHandler,
    IAsyncDomainProjectionReconciliationHandler
{
    /// <summary>The named production projection route.</summary>
    public const string ConversationReadModelProjectionType = "conversation-read-model";

    private static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromMinutes(5);
    private static readonly byte[] FingerprintSeparator = [0x1F];
    private const int BulkReadChunkSize = 100;
    private const int BulkReadParallelism = 8;
    private const int DispatchLedgerRetryCount = 5;
    private const int DispatchLedgerBackoffMilliseconds = 5;

    private readonly ConversationProjectionMaterializer _materializer;
    private readonly TimeSpan _completionTimeout;
    private readonly IReadModelExpiringStore? _expiringStore;
    private readonly TimeSpan _ledgerTimeToLive;
    private readonly IReadModelStore _store;
    private readonly ConversationResolutionMetadataPersistence _resolution;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationProjectionReadModelWriter _writer;

    /// <summary>Initializes the named projection handler.</summary>
    public ConversationAsyncProjectionHandler(
        ConversationProjectionMaterializer materializer,
        ConversationProjectionReadModelWriter writer,
        IReadModelStore store,
        TimeProvider? timeProvider = null,
        IReadModelExpiringStore? expiringStore = null,
        IOptions<ProjectionDispatchOptions>? dispatchOptions = null)
    {
        _materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _resolution = new(_store);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _expiringStore = expiringStore ?? store as IReadModelExpiringStore;
        ProjectionDispatchOptions options = dispatchOptions?.Value ?? new ProjectionDispatchOptions();
        options.Validate();
        _completionTimeout = options.RetryLeaseDuration;
        _ledgerTimeToLive = options.RedeliveryWindow;
    }

    /// <inheritdoc/>
    public string Domain => ConversationProjectionHandler.ConversationDomain;

    /// <inheritdoc/>
    public string ProjectionType => ConversationReadModelProjectionType;

    /// <inheritdoc/>
    public DomainProjectionRebuildSemantics RebuildSemantics => DomainProjectionRebuildSemantics.FullReplay;

    /// <inheritdoc/>
    public async Task<DomainProjectionHandlerResult> ReconcileAsync(
        ProjectionRequest request,
        string dispatchId,
        CancellationToken cancellationToken)
    {
        DomainProjectionHandlerResult retried = await ProjectAsync(request, dispatchId, cancellationToken)
            .ConfigureAwait(false);
        if (retried.Status is ProjectionDispatchStatus.Completed or ProjectionDispatchStatus.AlreadyCompleted)
        {
            return retried;
        }

        bool reconciled = await _writer.TryReconcileTerminalDispatchAsync(
                new TenantId(request.TenantId),
                new ConversationId(request.AggregateId),
                dispatchId,
                cancellationToken)
            .ConfigureAwait(false);
        reconciled = reconciled && await _resolution.TryRestoreTerminalAsync(
            new TenantId(request.TenantId), new ConversationId(request.AggregateId), dispatchId, cancellationToken)
            .ConfigureAwait(false);
        return reconciled
            ? DomainProjectionHandlerResult.Completed()
            : DomainProjectionHandlerResult.Retryable(ProjectionDispatchReasonCodes.PartialRetry);
    }

    /// <inheritdoc/>
    public async Task<DomainProjectionHandlerResult> ProjectAsync(
        ProjectionRequest request,
        string dispatchId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchId);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Domain, Domain, StringComparison.Ordinal))
        {
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.UnsupportedRoute);
        }

        // Both entry points reject an empty slice the same way: PrepareRebuildAsync throws a typed rejection,
        // so immediate dispatch must not quietly rely on the freshness gate to notice.
        if (request.Events is not { Length: > 0 })
        {
            return await RejectObservedInputAsync(request, dispatchId, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<ConversationProjectionEventRecord> events;
        try
        {
            events = ConversationProjectionEventDecoder.Decode(request.Events);
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            return await RejectObservedInputAsync(request, dispatchId, cancellationToken).ConfigureAwait(false);
        }

        if (events.All(static record => record.Event is ConversationProjectionPositionOnlyEvent))
        {
            return DomainProjectionHandlerResult.Completed();
        }

        string requestFingerprint = ComputeRequestFingerprint(request);
        DateTimeOffset candidateGeneratedAt = _timeProvider.GetUtcNow();
        try
        {
            ConversationProjectedReadModels candidate = Materialize(request, events, candidateGeneratedAt);
            if (!AllowsPersistence(candidate.Detail.Freshness)
                || !AllowsPersistence(candidate.Summary.Freshness))
            {
                return await RejectObservedInputAsync(request, dispatchId, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            return await RejectObservedInputAsync(request, dispatchId, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await _resolution.RecoverWritesAsync(new TenantId(request.TenantId),
                new ConversationId(request.AggregateId), cancellationToken).ConfigureAwait(false);
            candidateGeneratedAt = await _resolution.RecoverGeneratedAtAsync(
                new TenantId(request.TenantId), new ConversationId(request.AggregateId), dispatchId,
                requestFingerprint, cancellationToken).ConfigureAwait(false) ?? candidateGeneratedAt;
            ConversationProjectionDispatchLedger ledger = await AcquireDispatchAsync(
                request,
                dispatchId,
                requestFingerprint,
                candidateGeneratedAt,
                cancellationToken).ConfigureAwait(false);
            ConversationProjectedReadModels models = Materialize(
                request,
                events,
                ledger.ProjectionGeneratedAt) with
            {
                DispatchId = dispatchId,
            };
            // A completed ledger is only a completion if the generation it describes is still readable. The
            // ledger lives under a third key family, so derived-state deletion or a store rollback can leave it
            // behind after both read-model keys are gone. Reporting Completed from the ledger alone would then
            // claim a durable generation no reader can observe; re-persisting instead converges idempotently.
            if (ledger.Status == ConversationProjectionDispatchStatus.Completed
                && await GenerationIsDurableAsync(models, ledger, cancellationToken, requireCompletedProof: true).ConfigureAwait(false))
            {
                return DomainProjectionHandlerResult.Completed();
            }

            if (!AllowsPersistence(models.Detail.Freshness)
                || !AllowsPersistence(models.Summary.Freshness))
            {
                return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
            }

            ConversationResolutionMetadataReadModel metadata = ConversationResolutionMetadataReadModel.From(models, requestFingerprint);
            await _resolution.MarkPendingAsync(metadata, cancellationToken).ConfigureAwait(false);
            await _writer.MarkGenerationPendingAsync(models, cancellationToken).ConfigureAwait(false);
            await _writer.PersistGenerationAsync(models, cancellationToken).ConfigureAwait(false);

            // Both keys are durable at this point. Completion is independent of caller cancellation so a late
            // disconnect cannot strand a correct generation, but it is still bounded by the delivery lease.
            using CancellationTokenSource completionTimeout =
                new(_completionTimeout, _timeProvider);
            await _resolution.WriteMetadataAsync(metadata, completionTimeout.Token).ConfigureAwait(false);
            if (!await CompleteResolutionGenerationAsync(models, ledger, completionTimeout.Token).ConfigureAwait(false))
            {
                return DomainProjectionHandlerResult.Retryable(ProjectionDispatchReasonCodes.PartialRetry);
            }

            await CompleteDispatchAsync(ledger, completionTimeout.Token).ConfigureAwait(false);
            return DomainProjectionHandlerResult.Completed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return DomainProjectionHandlerResult.Retryable(ProjectionDispatchReasonCodes.PartialRetry);
        }
        catch (ArgumentException)
        {
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
        }
        catch (InvalidOperationException)
        {
            return DomainProjectionHandlerResult.Retryable(ProjectionDispatchReasonCodes.PartialRetry);
        }
        catch (Exception)
        {
            return DomainProjectionHandlerResult.Indeterminate(ProjectionDispatchReasonCodes.HandlerFailure);
        }
    }

    private async Task<DomainProjectionHandlerResult> RejectObservedInputAsync(
        ProjectionRequest request, string dispatchId, CancellationToken cancellationToken)
    {
        long observed = request.Events?.Where(static item => item is not null).Select(static item => item.SequenceNumber)
            .DefaultIfEmpty(0).Max() ?? 0;
        try
        {
            await _resolution.InvalidateAsync(new TenantId(request.TenantId), new ConversationId(request.AggregateId),
                observed, dispatchId, cancellationToken).ConfigureAwait(false);
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return DomainProjectionHandlerResult.Retryable(ProjectionDispatchReasonCodes.PartialRetry); }
    }

    /// <inheritdoc/>
    public async Task<DomainProjectionRebuildPlan> PrepareRebuildAsync(
        ProjectionRequest request,
        string operationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(request.Domain, Domain, StringComparison.Ordinal))
        {
            throw new ArgumentException("The projection route is not supported.", nameof(request));
        }

        if (request.Events is not { Length: > 0 })
        {
            throw new DomainProjectionRebuildRejectedException(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        IReadOnlyList<ConversationProjectionEventRecord> events;
        try
        {
            events = ConversationProjectionEventDecoder.Decode(request.Events);
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            throw new DomainProjectionRebuildRejectedException(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        string requestFingerprint = ComputeRequestFingerprint(request);
        ReadModelEntry<ConversationProjectionDispatchLedger> persistedLedger = await _store
            .GetAsync<ConversationProjectionDispatchLedger>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey(operationId),
                cancellationToken)
            .ConfigureAwait(false);
        ConversationProjectionDispatchLedger? existingLedger = persistedLedger.Value;
        if (existingLedger is not null)
        {
            try
            {
                _ = ValidateDispatchIdentity(existingLedger, request, operationId, requestFingerprint);
            }
            catch (ArgumentException)
            {
                throw new DomainProjectionRebuildRejectedException(ProjectionDispatchReasonCodes.DeliveryIdentityConflict);
            }
        }

        ConversationProjectedReadModels models = Materialize(
            request,
            events,
            existingLedger?.ProjectionGeneratedAt
                ?? await _resolution.RecoverGeneratedAtAsync(new TenantId(request.TenantId),
                    new ConversationId(request.AggregateId), operationId, requestFingerprint, cancellationToken).ConfigureAwait(false)
                ?? _timeProvider.GetUtcNow()) with
        {
            DispatchId = operationId,
        };

        if (!AllowsPersistence(models.Detail.Freshness)
            || !AllowsPersistence(models.Summary.Freshness))
        {
            throw new DomainProjectionRebuildRejectedException(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        ReadModelEntry<ConversationProjectedReadModels> persistedDetail = await _store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(models.Summary.TenantId, models.Summary.ConversationId),
                cancellationToken)
            .ConfigureAwait(false);
        ReadModelEntry<ConversationProjectionIndexReadModel> persistedIndex = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(models.Summary.TenantId),
                cancellationToken)
            .ConfigureAwait(false);
        // Current SDK rebuild coexistence with the legacy IDomainProjectionHandler uses a default marker:
        // no retained rich manifest or owner-selection manifest is attached. Keep that enrollment unchanged;
        // the read proof may participate in this content-free atomic visibility protocol only.
        ConversationResolutionMetadataReadModel metadata = ConversationResolutionMetadataReadModel.From(models, requestFingerprint);
        ReadModelEntry<ConversationResolutionMetadataReadModel> persistedMetadata = await _store
            .GetAsync<ConversationResolutionMetadataReadModel>(ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ResolutionMetadataKey(metadata.TenantId, metadata.ConversationId), cancellationToken)
            .ConfigureAwait(false);
        ReadModelEntry<ConversationResolutionMetadataProof> persistedProof = await _store
            .GetAsync<ConversationResolutionMetadataProof>(ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ResolutionProofKey(metadata.TenantId, metadata.ConversationId), cancellationToken)
            .ConfigureAwait(false);
        var persistedFence = await _store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionFenceKey(metadata.TenantId, metadata.ConversationId), cancellationToken).ConfigureAwait(false);
        ConversationResolutionGeneration generation = ConversationResolutionGeneration.From(metadata, false);
        if ((persistedProof.Value?.Barrier is { } barrier && ConversationProjectionReadModelWriter.CompareGeneration(
                generation.Freshness.LastAppliedEventPosition, generation.DispatchId, barrier.Position, barrier.DispatchId)
                    < (barrier.RequiresNewerGeneration ? 1 : 0))
            || (persistedProof.Value?.Current is { } currentProof && currentProof.CompareTo(generation) > 0)
            || (persistedFence.Value?.Current is { } currentFence && currentFence.CompareTo(generation) > 0)
            || (persistedMetadata.Value is not null && ConversationResolutionGeneration.From(persistedMetadata.Value, false).CompareTo(generation) > 0)
            || (persistedDetail.Value is not null
                && ConversationProjectionReadModelWriter.CompareGeneration(
                    persistedDetail.Value.Summary.Freshness.LastAppliedEventPosition, persistedDetail.Value.DispatchId,
                    models.Summary.Freshness.LastAppliedEventPosition, models.DispatchId) > 0))
        {
            throw new DomainProjectionRebuildRejectedException(ProjectionDispatchReasonCodes.PartialRetry);
        }

        ConversationResolutionMetadataProof rebuiltProof = ConversationResolutionMetadataPersistence
            .MergePending(persistedProof.Value, generation) with { Current = generation };
        // Rebuilding one conversation replaces the whole index value, so every sibling must survive it intact.
        // Foreign-tenant rows and duplicate entries are sanitized because they are corruption; a sibling whose
        // dispatch reference does not currently match its summary is mid-flight, not corrupt, and dropping it
        // would delete a live conversation from every page. Per-row verification withholds such a row at read
        // time without this plan having to destroy it.
        Dictionary<string, ConversationSummaryProjectionV1> retained = (persistedIndex.Value?.Summaries ?? [])
            .Where(summary => summary.TenantId == models.Summary.TenantId)
            .Where(summary => summary.ConversationId != models.Summary.ConversationId)
            .GroupBy(summary => summary.ConversationId.Value, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(summary => summary.Freshness.LastAppliedEventPosition)
                .ThenByDescending(summary => summary.Freshness.ProjectionGeneratedAt)
                .First())
            .ToDictionary(summary => summary.ConversationId.Value, StringComparer.Ordinal);
        Dictionary<string, ConversationProjectionDispatchReference> dispatches = new(StringComparer.Ordinal);
        Dictionary<string, ConversationProjectionDispatchReference> unverifiedDispatches = new(StringComparer.Ordinal);
        foreach ((string conversationId, ConversationProjectionDispatchReference reference)
            in persistedIndex.Value?.Dispatches ?? new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal))
        {
            if (string.Equals(conversationId, models.Summary.ConversationId.Value, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(conversationId)
                || string.IsNullOrWhiteSpace(reference.DispatchId))
            {
                continue;
            }

            if (retained.ContainsKey(conversationId))
            {
                dispatches[conversationId] = reference;
                continue;
            }

            unverifiedDispatches[conversationId] = reference;
        }

        IReadOnlyDictionary<string, ConversationProjectionDispatchLedger?> siblingLedgers =
            await BulkReadSiblingLedgersAsync(unverifiedDispatches.Values, cancellationToken).ConfigureAwait(false);
        foreach ((string conversationId, ConversationProjectionDispatchReference reference) in unverifiedDispatches)
        {
            // A reference without a summary is either an accepted sibling mid-dispatch or corrupt debris.
            // Prove its tenant/conversation binding from its ledger before retaining it in the replacement
            // index; dropping a valid pending reference makes that accepted conversation invisible forever.
            ConversationProjectionDispatchLedger? siblingLedger = siblingLedgers[
                ConversationProjectionReadModelKeys.DispatchLedgerKey(reference.DispatchId)];
            if (siblingLedger is not null
                && string.Equals(siblingLedger.DispatchId, reference.DispatchId, StringComparison.Ordinal)
                && siblingLedger.TenantId == models.Summary.TenantId
                && string.Equals(siblingLedger.ConversationId.Value, conversationId, StringComparison.Ordinal))
            {
                dispatches[conversationId] = reference;
            }
        }

        retained[models.Summary.ConversationId.Value] = models.Summary;
        dispatches[models.Summary.ConversationId.Value] = new(
            operationId,
            models.Summary.Freshness.LastAppliedEventPosition);
        ConversationProjectionIndexReadModel index = new()
        {
            Summaries = [.. retained.Values.OrderBy(summary => summary.ConversationId.Value, StringComparer.Ordinal)],
            Dispatches = dispatches,
        };
        ConversationProjectionDispatchLedger completedLedger = existingLedger is null
            ? new(
                operationId,
                requestFingerprint,
                models.Summary.TenantId,
                models.Summary.ConversationId,
                models.Summary.Freshness.ProjectionGeneratedAt,
                ConversationProjectionDispatchStatus.Completed)
            : existingLedger with { Status = ConversationProjectionDispatchStatus.Completed };

        return new DomainProjectionRebuildPlan(
            ConversationProjectionReadModelKeys.StateStoreName,
            [
                // Install the outer gate first. The native SDK exposes no ETag for an envelope, so the
                // narrow reader withholds from the first install until this generation is compacted.
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.ResolutionProofKey(metadata.TenantId, metadata.ConversationId),
                    rebuiltProof, WriteConcurrency(persistedProof)),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.ResolutionMetadataKey(metadata.TenantId, metadata.ConversationId),
                    metadata, WriteConcurrency(persistedMetadata)),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.ConversationKey(models.Summary.TenantId, models.Summary.ConversationId),
                    models, WriteConcurrency(persistedDetail)),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.TenantIndexKey(models.Summary.TenantId),
                    index, WriteConcurrency(persistedIndex)),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.DispatchLedgerKey(operationId),
                    completedLedger, WriteConcurrency(persistedLedger), _ledgerTimeToLive),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.ResolutionFenceKey(metadata.TenantId, metadata.ConversationId),
                    rebuiltProof, WriteConcurrency(persistedFence)),
            ]);
    }

    private ConversationProjectedReadModels Materialize(
        ProjectionRequest request,
        IReadOnlyList<ConversationProjectionEventRecord> events,
        DateTimeOffset projectionGeneratedAt)
        => _materializer.Project(
            new TenantId(request.TenantId),
            new ConversationId(request.AggregateId),
            events,
            projectionGeneratedAt,
            DefaultStaleAfter,
            isRebuilding: false,
            metadataWriteFailed: false);

    private async Task<ConversationProjectionDispatchLedger> AcquireDispatchAsync(
        ProjectionRequest request,
        string dispatchId,
        string requestFingerprint,
        DateTimeOffset projectionGeneratedAt,
        CancellationToken cancellationToken)
    {
        string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey(dispatchId);
        for (int attempt = 0; attempt < DispatchLedgerRetryCount; attempt++)
        {
            ReadModelEntry<ConversationProjectionDispatchLedger> entry = await _store
                .GetAsync<ConversationProjectionDispatchLedger>(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ledgerKey,
                    cancellationToken)
                .ConfigureAwait(false);
            if (entry.Value is not null)
            {
                return ValidateDispatchIdentity(entry.Value, request, dispatchId, requestFingerprint);
            }

            ConversationProjectionDispatchLedger pending = new(
                dispatchId,
                requestFingerprint,
                new TenantId(request.TenantId),
                new ConversationId(request.AggregateId),
                projectionGeneratedAt,
                ConversationProjectionDispatchStatus.Pending);
            IReadModelExpiringStore expiringStore = _expiringStore
                ?? throw new InvalidOperationException("The read-model store does not support expiring dispatch ledgers.");
            if (await expiringStore.TrySaveWithTimeToLiveAsync(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ledgerKey,
                    pending,
                    string.Empty,
                    _ledgerTimeToLive,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                return pending;
            }

            // Yield between attempts. A tight read/compare-and-set loop burns the whole budget in microseconds
            // under contention, turning a condition that would resolve on its own into a retry storm.
            await BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The projection dispatch ledger concurrency budget was exhausted.");
    }

    private async Task CompleteDispatchAsync(
        ConversationProjectionDispatchLedger ledger,
        CancellationToken cancellationToken)
    {
        string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey(ledger.DispatchId);
        for (int attempt = 0; attempt < DispatchLedgerRetryCount; attempt++)
        {
            ReadModelEntry<ConversationProjectionDispatchLedger> entry = await _store
                .GetAsync<ConversationProjectionDispatchLedger>(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ledgerKey,
                    cancellationToken)
                .ConfigureAwait(false);
            ConversationProjectionDispatchLedger current = entry.Value
                ?? throw new InvalidOperationException("The projection dispatch ledger disappeared before completion.");
            ValidateCompletionIdentity(ledger, current);
            if (current.Status == ConversationProjectionDispatchStatus.Completed)
            {
                return;
            }

            ConversationProjectionDispatchLedger completed = current with
            {
                Status = ConversationProjectionDispatchStatus.Completed,
            };
            IReadModelExpiringStore expiringStore = _expiringStore
                ?? throw new InvalidOperationException("The read-model store does not support expiring dispatch ledgers.");
            if (await expiringStore.TrySaveWithTimeToLiveAsync(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ledgerKey,
                    completed,
                    entry.ETag ?? string.Empty,
                    _ledgerTimeToLive,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                return;
            }

            await BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The projection dispatch completion concurrency budget was exhausted.");
    }

    private async Task<IReadOnlyDictionary<string, ConversationProjectionDispatchLedger?>> BulkReadSiblingLedgersAsync(
        IEnumerable<ConversationProjectionDispatchReference> references,
        CancellationToken cancellationToken)
    {
        string[] keys = [.. references
            .Select(static reference => ConversationProjectionReadModelKeys.DispatchLedgerKey(reference.DispatchId))
            .Distinct(StringComparer.Ordinal)];
        if (keys.Length == 0)
        {
            return new Dictionary<string, ConversationProjectionDispatchLedger?>(StringComparer.Ordinal);
        }

        if (_store is not IReadModelBulkStore bulkStore)
        {
            throw new InvalidOperationException("The configured read-model store does not support bounded bulk reads.");
        }

        var values = new Dictionary<string, ConversationProjectionDispatchLedger?>(keys.Length, StringComparer.Ordinal);
        foreach (string[] chunk in keys.Chunk(BulkReadChunkSize))
        {
            HashSet<string> requested = new(chunk, StringComparer.Ordinal);
            IReadOnlyList<ReadModelBulkEntry<ConversationProjectionDispatchLedger>> entries = await bulkStore
                .GetManyAsync<ConversationProjectionDispatchLedger>(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    chunk,
                    BulkReadParallelism,
                    cancellationToken)
                .ConfigureAwait(false);
            if (entries.Count != chunk.Length
                || entries.Select(static item => item.Key).Distinct(StringComparer.Ordinal).Count() != entries.Count)
            {
                throw new InvalidOperationException("The sibling dispatch-ledger bulk read returned an invalid result set.");
            }

            foreach (ReadModelBulkEntry<ConversationProjectionDispatchLedger> entry in entries)
            {
                if (!requested.Contains(entry.Key))
                {
                    throw new InvalidOperationException("The sibling dispatch-ledger bulk read returned an unrequested key.");
                }

                values[entry.Key] = entry.Value;
            }
        }

        if (values.Count != keys.Length)
        {
            throw new InvalidOperationException("The sibling dispatch-ledger bulk read omitted a requested key.");
        }

        return values;
    }

    private static void ValidateCompletionIdentity(
        ConversationProjectionDispatchLedger expected,
        ConversationProjectionDispatchLedger current)
    {
        if (!Enum.IsDefined(current.Status)
            || current.Status is not (ConversationProjectionDispatchStatus.Pending
                or ConversationProjectionDispatchStatus.Completed)
            || !string.Equals(current.DispatchId, expected.DispatchId, StringComparison.Ordinal)
            || !string.Equals(current.RequestFingerprint, expected.RequestFingerprint, StringComparison.Ordinal)
            || current.TenantId != expected.TenantId
            || current.ConversationId != expected.ConversationId
            || current.ProjectionGeneratedAt.UtcTicks != expected.ProjectionGeneratedAt.UtcTicks)
        {
            throw new ArgumentException("The projection dispatch ledger changed identity before completion.", nameof(current));
        }
    }

    private static Task BackoffAsync(int attempt, CancellationToken cancellationToken)
        => Task.Delay(
            TimeSpan.FromMilliseconds(DispatchLedgerBackoffMilliseconds * (attempt + 1)),
            cancellationToken);

    /// <summary>
    /// Binds a stable dispatch identity to <b>what</b> is being projected, not to how one delivery happened to
    /// be shaped.
    /// </summary>
    /// <remarks>
    /// Correlation identifiers, message identifiers, user identifiers and backfilled global positions
    /// legitimately differ between two deliveries of the same dispatch. Hashing the whole serialized request
    /// would treat those differences as identity reuse and fail the dispatch terminally. The fingerprint
    /// therefore covers the route, tenant, aggregate, and each event's sequence, type and payload, plus the
    /// event timestamp only for a position-only event whose timestamp changes materialized freshness.
    /// <para>
    /// That extra segment is safe under at-least-once delivery because the hashed value is the event's
    /// persisted stream timestamp, not a per-delivery stamp: it is carried from the stored envelope
    /// (<c>ProjectionEventWireBuilder</c> populates <c>ProjectionEventDto.Timestamp</c> from
    /// <c>envelope.Timestamp</c>), so a redelivery of the same event reproduces the same value and converges.
    /// A per-delivery timestamp must never enter this fingerprint.
    /// </para>
    /// </remarks>
    private static string ComputeRequestFingerprint(ProjectionRequest request)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendFingerprintSegment(hash, request.Domain);
        AppendFingerprintSegment(hash, request.TenantId);
        AppendFingerprintSegment(hash, request.AggregateId);
        foreach (ProjectionEventDto evt in request.Events ?? [])
        {
            AppendFingerprintSegment(hash, evt.SequenceNumber.ToString(CultureInfo.InvariantCulture));
            AppendFingerprintSegment(hash, evt.EventTypeName);
            AppendFingerprintSegment(hash, evt.SerializationFormat);
            if (ConversationProjectionEventDecoder.IsPositionOnlyEventType(evt.EventTypeName))
            {
                AppendFingerprintSegment(hash, evt.Timestamp.UtcTicks.ToString(CultureInfo.InvariantCulture));
            }

            hash.AppendData(evt.Payload ?? []);
            hash.AppendData(FingerprintSeparator);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void AppendFingerprintSegment(IncrementalHash hash, string? value)
    {
        hash.AppendData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        hash.AppendData(FingerprintSeparator);
    }

    /// <summary>
    /// Gets a value indicating whether a materialization may be persisted under this freshness.
    /// </summary>
    /// <remarks>
    /// Staleness is a read-time trust signal describing how far behind a projection is; it is not a reason to
    /// refuse to write. Gating persistence on <c>!IsStale</c> would make replay or rebuild of any conversation
    /// whose newest event is older than <see cref="DefaultStaleAfter"/> permanently impossible — which is
    /// exactly what full replay exists to do — and would make any projection outage longer than that threshold
    /// unrecoverable. Gaps, out-of-order delivery, unsupported versions, contradictory metadata and
    /// never-created aggregates still block, because those describe a materialization that is wrong rather
    /// than merely late.
    /// </remarks>
    private static bool AllowsPersistence(ProjectionFreshnessV1 freshness)
        => (freshness.FreshnessState == ProjectionTrustState.Current
                || freshness.FreshnessState == ProjectionTrustState.Stale)
            && (freshness.ReasonCode == ProjectionFreshnessReasonCode.Current
                || freshness.ReasonCode == ProjectionFreshnessReasonCode.StaleThresholdExceeded);

    private async Task<bool> GenerationIsDurableAsync(
        ConversationProjectedReadModels expected,
        ConversationProjectionDispatchLedger ledger,
        CancellationToken cancellationToken,
        bool requireCompletedProof = false)
    {
        TenantId tenantId = expected.Summary.TenantId;
        ConversationId conversationId = expected.Summary.ConversationId;
        ConversationResolutionMetadataReadModel metadata = ConversationResolutionMetadataReadModel.From(expected, ledger.RequestFingerprint);
        var proof = await _store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionProofKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);
        var fence = await _store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionFenceKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);
        var storedMetadata = await _store.GetAsync<ConversationResolutionMetadataReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(proof.ETag) || string.IsNullOrEmpty(fence.ETag) || string.IsNullOrEmpty(storedMetadata.ETag)
            || proof.Value?.Barrier is not null || fence.Value?.Barrier is not null
            || proof.Value?.Current is not { } generation || storedMetadata.Value is null
            || fence.Value?.Current is not { } fencedGeneration
            || !fencedGeneration.Matches(metadata)
            || (requireCompletedProof && (generation.IsPending || fencedGeneration.IsPending))
            || !generation.Matches(metadata) || !generation.Matches(storedMetadata.Value))
        {
            return false;
        }

        ReadModelEntry<ConversationProjectedReadModels> detail = await _store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(tenantId, conversationId),
                cancellationToken)
            .ConfigureAwait(false);
        if (detail.Value is null || string.IsNullOrEmpty(detail.ETag)
            || !string.Equals(detail.Value.DispatchId, ledger.DispatchId, StringComparison.Ordinal)
            || detail.Value.Summary.TenantId != tenantId
            || detail.Value.Detail.TenantId != tenantId
            || detail.Value.Summary.ConversationId != conversationId
            || detail.Value.Detail.ConversationId != conversationId
            || detail.Value.Summary.Freshness != detail.Value.Detail.Freshness
            || detail.Value.Summary.Freshness.ProjectionGeneratedAt.UtcTicks != ledger.ProjectionGeneratedAt.UtcTicks
            || !JsonElement.DeepEquals(
                JsonSerializer.SerializeToElement(detail.Value.Summary),
                JsonSerializer.SerializeToElement(expected.Summary))
            || !JsonElement.DeepEquals(
                JsonSerializer.SerializeToElement(detail.Value.Detail),
                JsonSerializer.SerializeToElement(expected.Detail)))
        {
            return false;
        }

        ReadModelEntry<ConversationProjectionIndexReadModel> index = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);
        if (requireCompletedProof)
        {
            var actualLedger = await _store.GetAsync<ConversationProjectionDispatchLedger>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey(ledger.DispatchId), cancellationToken).ConfigureAwait(false);
            if (actualLedger.Value is not null && (string.IsNullOrEmpty(actualLedger.ETag)
                || actualLedger.Value.Status != ConversationProjectionDispatchStatus.Completed
                || actualLedger.Value.DispatchId != ledger.DispatchId || actualLedger.Value.RequestFingerprint != ledger.RequestFingerprint
                || actualLedger.Value.TenantId != tenantId || actualLedger.Value.ConversationId != conversationId
                || actualLedger.Value.ProjectionGeneratedAt != generation.Freshness.ProjectionGeneratedAt))
            {
                return false;
            }
        }

        var confirmedProof = await _store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionProofKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);
        return confirmedProof.Value == proof.Value && confirmedProof.ETag == proof.ETag
            && !string.IsNullOrEmpty(index.ETag) && index.Value is not null
            && index.Value.Dispatches.TryGetValue(conversationId.Value, out ConversationProjectionDispatchReference? reference)
            && !reference.IsPending
            && string.Equals(reference.DispatchId, ledger.DispatchId, StringComparison.Ordinal)
            && reference.LastAppliedEventPosition == detail.Value.Summary.Freshness.LastAppliedEventPosition
            && index.Value.Summaries.Any(summary =>
                summary.ConversationId == conversationId
                && JsonElement.DeepEquals(
                    JsonSerializer.SerializeToElement(summary),
                    JsonSerializer.SerializeToElement(detail.Value.Summary)));
    }

    private async Task<bool> CompleteResolutionGenerationAsync(
        ConversationProjectedReadModels expected, ConversationProjectionDispatchLedger ledger, CancellationToken cancellationToken)
    {
        ConversationResolutionMetadataReadModel metadata = ConversationResolutionMetadataReadModel.From(expected, ledger.RequestFingerprint);
        if (await GenerationIsDurableAsync(expected, ledger, cancellationToken).ConfigureAwait(false))
        {
            await _resolution.CompleteAsync(metadata, cancellationToken).ConfigureAwait(false);
            return true;
        }

        // An old delivery may finish behind a newer completed winner, but cannot complete a partial winner.
        var proof = await _store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionProofKey(metadata.TenantId, metadata.ConversationId), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(proof.ETag) || proof.Value?.Barrier is not null || proof.Value?.Current is not { IsPending: false } winner
            || winner.CompareTo(ConversationResolutionGeneration.From(metadata, false)) <= 0)
        {
            return false;
        }

        var detail = await _store.GetAsync<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(metadata.TenantId, metadata.ConversationId), cancellationToken).ConfigureAwait(false);
        if (detail.Value is null)
        {
            return false;
        }

        return await GenerationIsDurableAsync(detail.Value,
            new(winner.DispatchId, winner.RequestFingerprint, winner.TenantId, winner.ConversationId,
                winner.Freshness.ProjectionGeneratedAt, ConversationProjectionDispatchStatus.Completed),
            cancellationToken, requireCompletedProof: true).ConfigureAwait(false);
    }

    private static ConversationProjectionDispatchLedger ValidateDispatchIdentity(
        ConversationProjectionDispatchLedger ledger,
        ProjectionRequest request,
        string dispatchId,
        string requestFingerprint)
    {
        if (!string.Equals(ledger.DispatchId, dispatchId, StringComparison.Ordinal)
            || !string.Equals(ledger.RequestFingerprint, requestFingerprint, StringComparison.Ordinal)
            || ledger.TenantId.Value != request.TenantId
            || ledger.ConversationId.Value != request.AggregateId)
        {
            throw new ArgumentException("The stable projection dispatch identity was reused for different input.", nameof(dispatchId));
        }

        return ledger;
    }

    private static ReadModelBatchConcurrency WriteConcurrency<TValue>(ReadModelEntry<TValue> entry)
        where TValue : class
        => string.IsNullOrEmpty(entry.ETag)
            ? ReadModelBatchConcurrency.CreateOnly
            : ReadModelBatchConcurrency.Match(entry.ETag);
}
