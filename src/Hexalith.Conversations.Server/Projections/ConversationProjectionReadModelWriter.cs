// <copyright file="ConversationProjectionReadModelWriter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Persists materialized conversation read models through the shared EventStore
/// <see cref="ReadModelWritePolicy"/> (optimistic-concurrency, reload-and-merge) over
/// <see cref="IReadModelStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the persistence <b>substrate</b> for FR-5. Writes never call <see cref="IReadModelStore.SaveAsync"/>
/// directly and never hand-roll a read-modify-write loop — all concurrency is delegated to the SDK policy:
/// the per-conversation summary/detail pair is written through an idempotent full-replace transform, and the
/// per-tenant summary index is maintained through an idempotent merge (dedup by conversation identity, newest
/// generation wins). Re-applying the same materialization yields the same persisted value (NFR5).
/// </para>
/// <para>
/// The production named projection handler publishes a pending dispatch reference through this writer before
/// the detail key changes, then persists the detail and completed index generation. This ordering lets readers
/// detect every partial generation and fail closed until the dispatch ledger is completed.
/// </para>
/// </remarks>
public sealed class ConversationProjectionReadModelWriter
{
    private readonly IReadModelStore _store;
    private readonly ILogger<ConversationProjectionReadModelWriter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionReadModelWriter"/> class.
    /// </summary>
    /// <param name="store">The shared read-model store.</param>
    /// <param name="logger">An optional logger for conflict/exhaustion diagnostics.</param>
    public ConversationProjectionReadModelWriter(
        IReadModelStore store,
        ILogger<ConversationProjectionReadModelWriter>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? NullLogger<ConversationProjectionReadModelWriter>.Instance;
    }

    /// <summary>
    /// Persists a materialized summary/detail pair and merges its summary into the tenant index, retrying
    /// under optimistic concurrency on an ETag conflict.
    /// </summary>
    /// <param name="models">The materialized summary/detail pair to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the optimistic-concurrency retry budget is exhausted.</exception>
    public async Task PersistAsync(ConversationProjectedReadModels models, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentException.ThrowIfNullOrWhiteSpace(models.DispatchId);

        ConversationSummaryProjectionV1 summary = models.Summary;
        string conversationKey = ConversationProjectionReadModelKeys.ConversationKey(summary.TenantId, summary.ConversationId);

        // Newest generation wins, matching the tenant index's merge rule. Both keys must agree on which
        // generation is authoritative: an unconditional overwrite here would let a retried older dispatch
        // clobber a newer detail while the index rejected its summary, splitting the two keys permanently.
        // Equal-position deliveries use the dispatch identity as a deterministic tie-breaker. Both the detail
        // and index independently choose the same winner even when their two writes interleave across replicas.
        // Re-applying the same materialization remains a no-op (NFR5).
        _ = await ReadModelWritePolicy
            .UpdateAsync<ConversationProjectedReadModels>(
                _store,
                ConversationProjectionReadModelKeys.StateStoreName,
                conversationKey,
                existing => existing is not null
                    && CompareGeneration(
                        summary.Freshness.LastAppliedEventPosition,
                        models.DispatchId,
                        existing.Summary.Freshness.LastAppliedEventPosition,
                        existing.DispatchId) < 0
                        ? existing
                        : models,
                new ReadModelWriteContext(
                    ConversationProjectionReadModelKeys.ConversationKeyCategory,
                    nameof(ConversationProjectedReadModels)),
                _logger,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // Tenant index: merge this conversation's summary into the per-tenant index. On an ETag conflict the
        // policy reloads the competing index and re-merges, so concurrent writers do not lose each other's
        // entries (no lost update). The merge is idempotent and returns a new instance.
        ConversationProjectionIndexReadModel incoming = new()
        {
            Summaries = [summary],
            Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
            {
                [summary.ConversationId.Value] = new(
                    models.DispatchId,
                    summary.Freshness.LastAppliedEventPosition,
                    IsPending: false),
            },
        };
        _ = await ReadModelWritePolicy
            .MergeAsync(
                _store,
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(summary.TenantId),
                incoming,
                static () => new ConversationProjectionIndexReadModel(),
                MergeIndex,
                new ReadModelWriteContext(
                    ConversationProjectionReadModelKeys.TenantIndexKeyCategory,
                    nameof(ConversationProjectionIndexReadModel)),
                _logger,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes the pending dispatch reference before either generation key is mutated, making every later
    /// partial write observable to fail-closed readers.
    /// </summary>
    /// <param name="models">The materialized generation about to be persisted.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the index-marker update.</returns>
    public async Task MarkPendingAsync(
        ConversationProjectedReadModels models,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentException.ThrowIfNullOrWhiteSpace(models.DispatchId);

        ConversationSummaryProjectionV1 summary = models.Summary;
        ConversationProjectionIndexReadModel incoming = new()
        {
            Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
            {
                [summary.ConversationId.Value] = new(
                    models.DispatchId,
                    summary.Freshness.LastAppliedEventPosition,
                    IsPending: true),
            },
        };
        _ = await ReadModelWritePolicy
            .MergeAsync(
                _store,
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(summary.TenantId),
                incoming,
                static () => new ConversationProjectionIndexReadModel(),
                MergeIndex,
                new ReadModelWriteContext(
                    ConversationProjectionReadModelKeys.TenantIndexKeyCategory,
                    nameof(ConversationProjectionIndexReadModel)),
                _logger,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Idempotently merges an incoming index into the persisted one: upsert by conversation identity (newest
    /// generation wins), returning a new instance so the persisted argument is never mutated.
    /// </summary>
    /// <param name="persisted">The currently persisted index.</param>
    /// <param name="incoming">The incoming index carrying the conversation(s) to upsert.</param>
    /// <returns>A new merged index.</returns>
    internal static ConversationProjectionIndexReadModel MergeIndex(
        ConversationProjectionIndexReadModel persisted,
        ConversationProjectionIndexReadModel incoming)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        ArgumentNullException.ThrowIfNull(incoming);

        Dictionary<string, ConversationSummaryProjectionV1> byConversation = new(StringComparer.Ordinal);
        foreach (ConversationSummaryProjectionV1 summary in persisted.Summaries)
        {
            byConversation[summary.ConversationId.Value] = summary;
        }

        Dictionary<string, ConversationProjectionDispatchReference> dispatches = new(
            persisted.Dispatches,
            StringComparer.Ordinal);
        foreach ((string conversationId, ConversationProjectionDispatchReference reference) in incoming.Dispatches)
        {
            if (!dispatches.TryGetValue(conversationId, out ConversationProjectionDispatchReference? existing)
                || CompareGeneration(
                    reference.LastAppliedEventPosition,
                    reference.DispatchId,
                    existing.LastAppliedEventPosition,
                    existing.DispatchId) >= 0)
            {
                dispatches[conversationId] = PrepareWinningReference(reference, existing);
            }
        }

        foreach (ConversationSummaryProjectionV1 summary in incoming.Summaries)
        {
            string conversationId = summary.ConversationId.Value;
            if (incoming.Dispatches.TryGetValue(conversationId, out ConversationProjectionDispatchReference? incomingReference))
            {
                if (incomingReference.LastAppliedEventPosition != summary.Freshness.LastAppliedEventPosition)
                {
                    throw new ArgumentException("An index summary and dispatch reference must describe the same position.", nameof(incoming));
                }

                // Only the summary belonging to the winning reference may move. Merging the two fields
                // independently lets equal-position A/B dispatches leave a permanently split index.
                if (dispatches.TryGetValue(conversationId, out ConversationProjectionDispatchReference? winner)
                    && SameGeneration(winner, incomingReference))
                {
                    byConversation[conversationId] = summary;
                }

                continue;
            }

            // Compatibility for callers that merge a summary-only index: positions still advance monotonically.
            if (!byConversation.TryGetValue(conversationId, out ConversationSummaryProjectionV1? existingSummary)
                || summary.Freshness.LastAppliedEventPosition >= existingSummary.Freshness.LastAppliedEventPosition)
            {
                byConversation[conversationId] = summary;
            }
        }

        return new ConversationProjectionIndexReadModel
        {
            Summaries = [.. byConversation.Values.OrderBy(summary => summary.ConversationId.Value, StringComparer.Ordinal)],
            Dispatches = dispatches,
        };
    }

    /// <summary>
    /// Removes one terminal pending marker only when it has not advanced the durable conversation generation.
    /// </summary>
    /// <param name="tenantId">The authoritative tenant identity.</param>
    /// <param name="conversationId">The authoritative conversation identity.</param>
    /// <param name="dispatchId">The terminal stable dispatch identity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> when the marker is absent, superseded, or safely compensated.</returns>
    public async Task<bool> TryReconcileTerminalDispatchAsync(
        TenantId tenantId,
        ConversationId conversationId,
        string dispatchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchId);

        ReadModelEntry<ConversationProjectedReadModels> detail = await _store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(tenantId, conversationId),
                cancellationToken)
            .ConfigureAwait(false);
        ReadModelEntry<ConversationProjectionIndexReadModel> current = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);
        if (current.Value is null
            || !current.Value.Dispatches.TryGetValue(conversationId.Value, out ConversationProjectionDispatchReference? marker)
            || !string.Equals(marker.DispatchId, dispatchId, StringComparison.Ordinal)
            || !marker.IsPending)
        {
            return true;
        }

        ConversationSummaryProjectionV1? indexedSummary = current.Value.Summaries
            .SingleOrDefault(summary => summary.ConversationId == conversationId);
        if (string.Equals(detail.Value?.DispatchId, dispatchId, StringComparison.Ordinal)
            || (indexedSummary?.Freshness.LastAppliedEventPosition == marker.LastAppliedEventPosition
                && marker.PreviousLastAppliedEventPosition != marker.LastAppliedEventPosition))
        {
            return false;
        }

        ConversationProjectionIndexReadModel reconciled = await ReadModelWritePolicy
            .UpdateAsync<ConversationProjectionIndexReadModel>(
                _store,
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                existing => ReconcilePendingReference(existing, conversationId.Value, dispatchId),
                new ReadModelWriteContext(
                    ConversationProjectionReadModelKeys.TenantIndexKeyCategory,
                    nameof(ConversationProjectionIndexReadModel)),
                _logger,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return !reconciled.Dispatches.TryGetValue(conversationId.Value, out ConversationProjectionDispatchReference? remaining)
            || !string.Equals(remaining.DispatchId, dispatchId, StringComparison.Ordinal)
            || !remaining.IsPending;
    }

    private static ConversationProjectionIndexReadModel ReconcilePendingReference(
        ConversationProjectionIndexReadModel? existing,
        string conversationId,
        string dispatchId)
    {
        ConversationProjectionIndexReadModel current = existing ?? new ConversationProjectionIndexReadModel();
        if (!current.Dispatches.TryGetValue(conversationId, out ConversationProjectionDispatchReference? marker)
            || !string.Equals(marker.DispatchId, dispatchId, StringComparison.Ordinal)
            || !marker.IsPending)
        {
            return current;
        }

        var dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(current.Dispatches, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(marker.PreviousDispatchId)
            && marker.PreviousLastAppliedEventPosition is > 0)
        {
            dispatches[conversationId] = new ConversationProjectionDispatchReference(
                marker.PreviousDispatchId,
                marker.PreviousLastAppliedEventPosition.Value);
        }
        else
        {
            _ = dispatches.Remove(conversationId);
        }

        return new ConversationProjectionIndexReadModel
        {
            Summaries = current.Summaries,
            Dispatches = dispatches,
        };
    }

    private static ConversationProjectionDispatchReference PrepareWinningReference(
        ConversationProjectionDispatchReference incoming,
        ConversationProjectionDispatchReference? existing)
    {
        if (!incoming.IsPending)
        {
            return incoming with
            {
                PreviousDispatchId = null,
                PreviousLastAppliedEventPosition = null,
            };
        }

        if (existing is null)
        {
            return incoming;
        }

        if (SameGeneration(existing, incoming))
        {
            if (!existing.IsPending)
            {
                return existing;
            }

            return incoming with
            {
                PreviousDispatchId = existing.PreviousDispatchId,
                PreviousLastAppliedEventPosition = existing.PreviousLastAppliedEventPosition,
            };
        }

        return incoming with
        {
            PreviousDispatchId = existing.IsPending ? existing.PreviousDispatchId : existing.DispatchId,
            PreviousLastAppliedEventPosition = existing.IsPending
                ? existing.PreviousLastAppliedEventPosition
                : existing.LastAppliedEventPosition,
        };
    }

    private static int CompareGeneration(
        long incomingPosition,
        string incomingDispatchId,
        long existingPosition,
        string existingDispatchId)
    {
        int positionComparison = incomingPosition.CompareTo(existingPosition);
        return positionComparison != 0
            ? positionComparison
            : StringComparer.Ordinal.Compare(incomingDispatchId, existingDispatchId);
    }

    private static bool SameGeneration(
        ConversationProjectionDispatchReference first,
        ConversationProjectionDispatchReference second)
        => first.LastAppliedEventPosition == second.LastAppliedEventPosition
            && string.Equals(first.DispatchId, second.DispatchId, StringComparison.Ordinal);
}
