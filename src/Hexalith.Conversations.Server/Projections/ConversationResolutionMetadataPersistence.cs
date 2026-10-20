using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Maintains lasting, content-free generation proof through the platform write policy.</summary>
internal sealed class ConversationResolutionMetadataPersistence(IReadModelStore store)
{
    /// <summary>Resumes SDK-owned interrupted batches before accepting another projection write.</summary>
    internal async Task RecoverWritesAsync(TenantId tenantId, ConversationId conversationId, CancellationToken cancellationToken)
    {
        if (store is not IReadModelPendingBatchRecoveryStore recovery)
        {
            return;
        }

        // A committed batch compacts its first operation before its remaining participants and final
        // recovery anchor. Probe every own key; a plain fence alone does not prove recovery is complete.
        string[] participants =
        [
            ConversationProjectionReadModelKeys.ResolutionFenceKey(tenantId, conversationId),
            ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenantId, conversationId),
        ];
        foreach (string key in participants)
        {
            ReadModelPendingBatchRecoveryResult result = await recovery.RecoverPendingAsync(new(
                ConversationProjectionReadModelKeys.StateStoreName, ConversationProjectionReadModelKeys.ResolutionStorageToken(tenantId.Value),
                ConversationProjectionHandler.ConversationDomain, ConversationProjectionReadModelKeys.ResolutionStorageToken(conversationId.Value), "resolution-metadata", "pending-discovery"),
                key, cancellationToken).ConfigureAwait(false);
            if (!result.IsResolved)
            {
                throw new InvalidOperationException("An interrupted generation batch remains unavailable for recovery.");
            }
        }
    }

    /// <summary>Publishes pending proof before the legacy index can move.</summary>
    internal async Task MarkPendingAsync(ConversationResolutionMetadataReadModel model, CancellationToken cancellationToken)
    {
        ConversationResolutionGeneration incoming = ConversationResolutionGeneration.From(model, true);
        await ClaimAsync(ConversationProjectionReadModelKeys.ResolutionProofKey(model.TenantId, model.ConversationId), incoming, forceWrite: true, cancellationToken)
            .ConfigureAwait(false);
        await ClaimAsync(ConversationProjectionReadModelKeys.ResolutionFenceKey(model.TenantId, model.ConversationId), incoming, forceWrite: false, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ClaimAsync(string key, ConversationResolutionGeneration incoming, bool forceWrite, CancellationToken cancellationToken)
    {
        var observed = await store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName, key, cancellationToken).ConfigureAwait(false);
        // Repairing a completed same-generation dispatch must first reopen the outer pending proof.
        // Private unchanged claims can retain their ETag until a metadata batch needs to advance them.
        if (!forceWrite && observed.Value == MergePending(observed.Value, incoming)) return;
        _ = await ReadModelWritePolicy.UpdateAsync<ConversationResolutionMetadataProof>(store,
            ConversationProjectionReadModelKeys.StateStoreName, key,
            existing => MergePending(existing, incoming), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Coordinates only bounded metadata and its content-free fence in the SDK retained batch.</summary>
    internal async Task WriteMetadataAsync(ConversationResolutionMetadataReadModel model, CancellationToken cancellationToken)
    {
        if (store is not IReadModelBatchStore batches)
        {
            throw new InvalidOperationException("Resolution metadata requires coordinated generation writes.");
        }

        string proofKey = ConversationProjectionReadModelKeys.ResolutionFenceKey(model.TenantId, model.ConversationId);
        var proof = await store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName, proofKey, cancellationToken).ConfigureAwait(false);
        if (proof.Value?.Current is not { } current)
        {
            throw new InvalidOperationException("The pending generation proof disappeared.");
        }

        if (current.CompareTo(ConversationResolutionGeneration.From(model, true)) > 0)
        {
            return;
        }

        if (!current.Matches(model))
        {
            throw new InvalidOperationException("The generation claim was displaced.");
        }

        ConversationResolutionMetadataProof pending = proof.Value with { Current = current with { IsPending = true } };
        List<ReadModelBatchOperation> operations =
        [
            ReadModelBatchOperation.Write(proofKey, pending, Concurrency(proof)),
        ];
        // Metadata participates only in this content-free batch. Even a query racing the outer gate
        // can retrieve no detail/index values through the SDK's retained recovery marker.
        string metadataKey = ConversationProjectionReadModelKeys.ResolutionMetadataKey(model.TenantId, model.ConversationId);
        var metadata = await store.GetAsync<ConversationResolutionMetadataReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName, metadataKey, cancellationToken).ConfigureAwait(false);
        if (metadata.Value is not null && ConversationResolutionGeneration.From(metadata.Value, false)
            .CompareTo(ConversationResolutionGeneration.From(model, false)) > 0)
        {
            throw new InvalidOperationException("Metadata advanced beyond its claimed generation.");
        }

        operations.Add(ReadModelBatchOperation.Write(metadataKey, model, Concurrency(metadata)));

        // The entire immutable manifest defines the retry identity, including its expected ETags. An
        // optimistic conflict gets a new manifest; interrupted identical work resumes the same SDK batch.
        string batchId = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            operations.Select(operation => new { operation.Key, operation.Concurrency,
                Value = Convert.ToBase64String(operation.CanonicalValue.Span) }))));
        ReadModelBatchResult result = await batches.ExecuteAsync(new(new(
            ConversationProjectionReadModelKeys.StateStoreName, ConversationProjectionReadModelKeys.ResolutionStorageToken(model.TenantId.Value), ConversationProjectionHandler.ConversationDomain,
            ConversationProjectionReadModelKeys.ResolutionStorageToken(model.ConversationId.Value), "resolution-metadata", batchId), operations)
            { RetainManifestForRecovery = true }, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return;
        }

        // Concurrency and crash recovery are platform-owned. The existing projection dispatcher retries
        // this delivery; a new snapshot defines a new immutable batch after an optimistic conflict.
        throw new InvalidOperationException("The coordinated metadata generation remains incomplete.");
    }

    private static ReadModelBatchConcurrency Concurrency<T>(ReadModelEntry<T> entry) where T : class
        => string.IsNullOrEmpty(entry.ETag) ? ReadModelBatchConcurrency.CreateOnly : ReadModelBatchConcurrency.Match(entry.ETag);

    /// <summary>Completes only the proof whose exact components were verified by the production handler.</summary>
    internal async Task CompleteAsync(ConversationResolutionMetadataReadModel model, CancellationToken cancellationToken)
    {
        _ = await ReadModelWritePolicy.UpdateAsync<ConversationResolutionMetadataProof>(store,
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionFenceKey(model.TenantId, model.ConversationId),
            existing => existing is not null && existing.Barrier is null && existing.Current is { } generation && generation.Matches(model)
                ? existing with { Current = generation with { IsPending = false } }
                : throw new InvalidOperationException("The metadata generation advanced before completion."),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        _ = await ReadModelWritePolicy.UpdateAsync<ConversationResolutionMetadataProof>(store,
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionProofKey(model.TenantId, model.ConversationId),
            existing => existing is not null && existing.Barrier is null && existing.Current is { } generation && generation.Matches(model)
                ? existing with { Current = generation with { IsPending = false } }
                : throw new InvalidOperationException("The read proof advanced before completion."),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Recovers the original source generation after the bounded ledger expires.</summary>
    internal async Task<DateTimeOffset?> RecoverGeneratedAtAsync(
        TenantId tenantId, ConversationId conversationId, string dispatchId, string fingerprint,
        CancellationToken cancellationToken)
    {
        var entry = await store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionProofKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);
        var fence = await store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionFenceKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);
        ConversationResolutionGeneration?[] candidates =
        [entry.Value?.Current, entry.Value?.PreviousCompleted, fence.Value?.Current, fence.Value?.PreviousCompleted];
        ConversationResolutionGeneration[] found = [.. candidates.OfType<ConversationResolutionGeneration>()
            .Where(generation => generation.DispatchId == dispatchId)];
        if (found.Length == 0)
        {
            return null;
        }

        ConversationResolutionGeneration original = found[0];
        if (found.Any(generation => generation.TenantId != tenantId || generation.ConversationId != conversationId
            || generation.RequestFingerprint != fingerprint || generation.Freshness != original.Freshness
            || generation.SubsetDigest != original.SubsetDigest))
        {
            throw new ArgumentException("The lasting dispatch identity was reused for different input.");
        }

        return original.Freshness.ProjectionGeneratedAt;
    }

    /// <summary>Retains unavailable proof while an unfenced rich writer may still resume.</summary>
    internal async Task<bool> TryRestoreTerminalAsync(
        TenantId tenantId, ConversationId conversationId, string dispatchId, CancellationToken cancellationToken)
    {
        await RecoverWritesAsync(tenantId, conversationId, cancellationToken).ConfigureAwait(false);
        var proof = await store.GetAsync<ConversationResolutionMetadataProof>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionProofKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);
        // Legacy index compensation does not stop a paused CAS detail writer. Keep the outer proof
        // unavailable until a supported retry/rebuild verifies the winning components and completes it.
        return proof.Value?.Barrier is null && (proof.Value?.Current is not { IsPending: true } pending
            || pending.DispatchId != dispatchId);
    }

    /// <summary>Invalidates before an accepted rejected input or a direct legacy writer can advance.</summary>
    internal async Task InvalidateAsync(TenantId tenant, ConversationId conversation, long position,
        string dispatchId, CancellationToken cancellationToken, bool requiresNewerGeneration = false)
    {
        if (position < 1) return;
        _ = await ReadModelWritePolicy.UpdateAsync<ConversationResolutionMetadataProof>(store,
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ResolutionProofKey(tenant, conversation),
            existing =>
            {
                if (existing?.Current is { } current && ConversationProjectionReadModelWriter.CompareGeneration(
                    position, dispatchId, current.Freshness.LastAppliedEventPosition, current.DispatchId) < 0)
                    return existing;
                if (existing?.Barrier is { } barrier && ConversationProjectionReadModelWriter.CompareGeneration(
                    position, dispatchId, barrier.Position, barrier.DispatchId) < 0)
                    return existing;
                return new(existing?.Current, existing?.PreviousCompleted, Barrier: new(position, dispatchId,
                    requiresNewerGeneration || existing?.Barrier is { RequiresNewerGeneration: true } oldBarrier
                        && oldBarrier.Position == position && oldBarrier.DispatchId == dispatchId));
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Preserves the deterministic winner and the last completed compensation target.</summary>
    internal static ConversationResolutionMetadataProof MergePending(
        ConversationResolutionMetadataProof? existing, ConversationResolutionGeneration incoming)
    {
        if (existing?.Barrier is { } barrier && ConversationProjectionReadModelWriter.CompareGeneration(
            incoming.Freshness.LastAppliedEventPosition, incoming.DispatchId, barrier.Position, barrier.DispatchId)
                < (barrier.RequiresNewerGeneration ? 1 : 0))
            return existing;
        if (existing?.Current is null)
        {
            return new(incoming);
        }

        int order = incoming.CompareTo(existing.Current);
        if (order < 0)
        {
            return existing;
        }

        if (order == 0)
        {
            if (incoming with { IsPending = existing.Current.IsPending } != existing.Current)
            {
                throw new ArgumentException("A stable generation changed identity.");
            }

            return existing with
            {
                Current = incoming,
                PreviousCompleted = existing.Current.IsPending ? existing.PreviousCompleted : existing.Current,
                Barrier = null,
            };
        }

        return new(incoming, existing.Current.IsPending ? existing.PreviousCompleted : existing.Current);
    }
}
