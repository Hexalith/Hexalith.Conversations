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

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Owns the production named Conversations read-model projection and its full-replay rebuild plan.
/// </summary>
/// <remarks>
/// Immediate dispatch first records a pending stable dispatch identity, then publishes that reference to the
/// tenant index before mutating the detail generation. The ledger's generation timestamp is reused on retry,
/// and an already-completed dispatch is a no-op. Coordinated rebuild uses the platform batch plan so the detail,
/// tenant index, and completed dispatch ledger are promoted as one visible generation.
/// </remarks>
public sealed class ConversationAsyncProjectionHandler : IAsyncDomainProjectionRebuildHandler
{
    /// <summary>The named production projection route.</summary>
    public const string ConversationReadModelProjectionType = "conversation-read-model";

    private static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromMinutes(5);
    private static readonly byte[] FingerprintSeparator = [0x1F];
    private const int DispatchLedgerRetryCount = 5;
    private const int DispatchLedgerBackoffMilliseconds = 5;

    private readonly ConversationProjectionMaterializer _materializer;
    private readonly IReadModelStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationProjectionReadModelWriter _writer;

    /// <summary>Initializes the named projection handler.</summary>
    public ConversationAsyncProjectionHandler(
        ConversationProjectionMaterializer materializer,
        ConversationProjectionReadModelWriter writer,
        IReadModelStore store,
        TimeProvider? timeProvider = null)
    {
        _materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public string Domain => ConversationProjectionHandler.ConversationDomain;

    /// <inheritdoc/>
    public string ProjectionType => ConversationReadModelProjectionType;

    /// <inheritdoc/>
    public DomainProjectionRebuildSemantics RebuildSemantics => DomainProjectionRebuildSemantics.FullReplay;

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
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        IReadOnlyList<ConversationProjectionEventRecord> events;
        try
        {
            events = ConversationProjectionEventDecoder.Decode(request.Events);
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        string requestFingerprint = ComputeRequestFingerprint(request);
        DateTimeOffset candidateGeneratedAt = _timeProvider.GetUtcNow();
        try
        {
            ConversationProjectedReadModels candidate = Materialize(request, events, candidateGeneratedAt);
            if (!AllowsPersistence(candidate.Detail.Freshness)
                || !AllowsPersistence(candidate.Summary.Freshness))
            {
                return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        try
        {
            ConversationProjectionDispatchLedger ledger = await AcquireDispatchAsync(
                request,
                dispatchId,
                requestFingerprint,
                candidateGeneratedAt,
                cancellationToken).ConfigureAwait(false);
            // A completed ledger is only a completion if the generation it describes is still readable. The
            // ledger lives under a third key family, so derived-state deletion or a store rollback can leave it
            // behind after both read-model keys are gone. Reporting Completed from the ledger alone would then
            // claim a durable generation no reader can observe; re-persisting instead converges idempotently.
            if (ledger.Status == ConversationProjectionDispatchStatus.Completed
                && await GenerationIsDurableAsync(request, dispatchId, cancellationToken).ConfigureAwait(false))
            {
                return DomainProjectionHandlerResult.Completed();
            }

            ConversationProjectedReadModels models = Materialize(
                request,
                events,
                ledger.ProjectionGeneratedAt) with
            {
                DispatchId = dispatchId,
            };
            if (!AllowsPersistence(models.Detail.Freshness)
                || !AllowsPersistence(models.Summary.Freshness))
            {
                return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
            }

            await _writer.MarkPendingAsync(models, cancellationToken).ConfigureAwait(false);
            await _writer.PersistAsync(models, cancellationToken).ConfigureAwait(false);

            // Both keys are durable at this point. Completing the ledger under an already-cancelled token would
            // leave a correct generation that every reader refuses, so completion is not cancellable.
            await CompleteDispatchAsync(ledger, CancellationToken.None).ConfigureAwait(false);
            return DomainProjectionHandlerResult.Completed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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

        ConversationProjectedReadModels models;
        try
        {
            models = Materialize(
                request,
                ConversationProjectionEventDecoder.Decode(request.Events),
                _timeProvider.GetUtcNow()) with
            {
                DispatchId = operationId,
            };
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            throw new DomainProjectionRebuildRejectedException(ProjectionDispatchReasonCodes.HandlerFailure);
        }

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
        ReadModelEntry<ConversationProjectionDispatchLedger> persistedLedger = await _store
            .GetAsync<ConversationProjectionDispatchLedger>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey(operationId),
                cancellationToken)
            .ConfigureAwait(false);

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
        foreach ((string conversationId, ConversationProjectionDispatchReference reference)
            in persistedIndex.Value?.Dispatches ?? new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal))
        {
            if (retained.ContainsKey(conversationId) && !string.IsNullOrWhiteSpace(reference.DispatchId))
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
        ConversationProjectionDispatchLedger completedLedger = new(
            operationId,
            ComputeRequestFingerprint(request),
            models.Summary.TenantId,
            models.Summary.ConversationId,
            models.Summary.Freshness.ProjectionGeneratedAt,
            ConversationProjectionDispatchStatus.Completed);

        return new DomainProjectionRebuildPlan(
            ConversationProjectionReadModelKeys.StateStoreName,
            [
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.ConversationKey(models.Summary.TenantId, models.Summary.ConversationId),
                    models,
                    WriteConcurrency(persistedDetail)),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.TenantIndexKey(models.Summary.TenantId),
                    index,
                    WriteConcurrency(persistedIndex)),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.DispatchLedgerKey(operationId),
                    completedLedger,
                    WriteConcurrency(persistedLedger)),
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
            if (await _store.TrySaveAsync(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ledgerKey,
                    pending,
                    string.Empty,
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
            if (current.Status == ConversationProjectionDispatchStatus.Completed)
            {
                return;
            }

            ConversationProjectionDispatchLedger completed = current with
            {
                Status = ConversationProjectionDispatchStatus.Completed,
            };
            if (await _store.TrySaveAsync(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ledgerKey,
                    completed,
                    entry.ETag ?? string.Empty,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                return;
            }

            await BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The projection dispatch completion concurrency budget was exhausted.");
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
    /// Correlation identifiers, message identifiers, user identifiers, delivery timestamps and backfilled
    /// global positions legitimately differ between two deliveries of the same dispatch. Hashing the whole
    /// serialized request would treat those differences as identity reuse and fail the dispatch terminally, so
    /// the fingerprint covers only the route, the tenant, the aggregate, and each event's sequence, type and
    /// payload — the values that must not change under one dispatch identity.
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
        ProjectionRequest request,
        string dispatchId,
        CancellationToken cancellationToken)
    {
        TenantId tenantId = new(request.TenantId);
        ConversationId conversationId = new(request.AggregateId);
        ReadModelEntry<ConversationProjectedReadModels> detail = await _store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(tenantId, conversationId),
                cancellationToken)
            .ConfigureAwait(false);
        if (detail.Value is null
            || !string.Equals(detail.Value.DispatchId, dispatchId, StringComparison.Ordinal))
        {
            return false;
        }

        ReadModelEntry<ConversationProjectionIndexReadModel> index = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);
        return index.Value is not null
            && index.Value.Dispatches.TryGetValue(conversationId.Value, out ConversationProjectionDispatchReference? reference)
            && string.Equals(reference.DispatchId, dispatchId, StringComparison.Ordinal)
            && index.Value.Summaries.Any(summary => summary.ConversationId == conversationId);
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
