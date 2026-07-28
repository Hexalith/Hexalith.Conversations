// <copyright file="ConversationAsyncProjectionHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
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
    private static readonly JsonSerializerOptions FingerprintJsonOptions = new(JsonSerializerDefaults.Web);
    private const int DispatchLedgerRetryCount = 5;

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
            if (!candidate.Detail.Freshness.AllowsTrustBearingDecision()
                || !candidate.Summary.Freshness.AllowsTrustBearingDecision())
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
            if (ledger.Status == ConversationProjectionDispatchStatus.Completed)
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
            if (!models.Detail.Freshness.AllowsTrustBearingDecision()
                || !models.Summary.Freshness.AllowsTrustBearingDecision())
            {
                return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
            }

            await _writer.MarkPendingAsync(models, cancellationToken).ConfigureAwait(false);
            await _writer.PersistAsync(models, cancellationToken).ConfigureAwait(false);
            await CompleteDispatchAsync(ledger, cancellationToken).ConfigureAwait(false);
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

        if (!models.Detail.Freshness.AllowsTrustBearingDecision()
            || !models.Summary.Freshness.AllowsTrustBearingDecision())
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

        Dictionary<string, ConversationSummaryProjectionV1> retained = (persistedIndex.Value?.Summaries ?? [])
            .Where(summary => summary.TenantId == models.Summary.TenantId)
            .Where(summary => summary.ConversationId != models.Summary.ConversationId)
            .GroupBy(summary => summary.ConversationId.Value, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(summary => summary.Freshness.LastAppliedEventPosition)
                .ThenByDescending(summary => summary.Freshness.ProjectionGeneratedAt)
                .First())
            .Where(summary => persistedIndex.Value!.Dispatches.TryGetValue(
                    summary.ConversationId.Value,
                    out ConversationProjectionDispatchReference? reference)
                && reference.LastAppliedEventPosition == summary.Freshness.LastAppliedEventPosition
                && !string.IsNullOrWhiteSpace(reference.DispatchId))
            .ToDictionary(summary => summary.ConversationId.Value, StringComparer.Ordinal);
        Dictionary<string, ConversationProjectionDispatchReference> dispatches = retained.Keys
            .ToDictionary(
                static conversationId => conversationId,
                conversationId => persistedIndex.Value!.Dispatches[conversationId],
                StringComparer.Ordinal);
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
        }

        throw new InvalidOperationException("The projection dispatch completion concurrency budget was exhausted.");
    }

    private static string ComputeRequestFingerprint(ProjectionRequest request)
        => Convert.ToHexStringLower(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(request, FingerprintJsonOptions)));

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
