// <copyright file="ConversationProjectionReadModelPersistenceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Fakes;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Story 2.4 (AC-2 / AC-3) — proves conversation read models are persisted and updated through the shared
/// EventStore <see cref="ReadModelWritePolicy"/> (optimistic-concurrency, reload-and-merge) over the SDK
/// <see cref="IReadModelStore"/>, with no-lost-update concurrency behavior, fail-loud retry exhaustion, and an
/// idempotent write transform (NFR5). The store double is the canonical SDK <see cref="InMemoryReadModelStore"/>.
/// </summary>
public sealed class ConversationProjectionReadModelPersistenceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId ConversationA = new("conversation-a");
    private static readonly ConversationId ConversationB = new("conversation-b");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A persisted summary/detail pair reads back identically, and the list boundary returns it from a single
    /// tenant-scoped index read (no per-conversation fan-out, NFR2).
    /// </summary>
    [Fact]
    public async Task PersistedReadModelsRoundTripThroughStoreAndIndex()
    {
        InMemoryReadModelStore inner = new();
        CountingReadModelStore store = new(inner);
        ConversationProjectionReadModelWriter writer = new(store);
        ConversationProjectionReadStore readStore = new(store);

        ConversationProjectedReadModels models = Models(ConversationA, position: 1);
        await writer.PersistAsync(models, TestContext.Current.CancellationToken);

        ConversationProjectedReadModels? readBack = await readStore.ReadAsync(Tenant, ConversationA, TestContext.Current.CancellationToken);
        readBack.ShouldNotBeNull();
        readBack!.Summary.ConversationId.ShouldBe(ConversationA);
        readBack.Summary.Label.ShouldBe(models.Summary.Label);
        readBack.Detail.ConversationId.ShouldBe(ConversationA);
        readBack.Detail.Label.ShouldBe(models.Detail.Label);

        store.ResetCounters();
        IReadOnlyList<ConversationSummaryProjectionV1> listed = await readStore.ListAsync(Tenant, TestContext.Current.CancellationToken);

        listed.Select(summary => summary.ConversationId.Value).ShouldBe([ConversationA.Value]);
        store.GetCalls.ShouldBe(1, "ListAsync must perform a single tenant-index read, never a per-conversation fan-out (NFR2).");
    }

    /// <summary>
    /// AC-3: when a competing writer commits a different conversation into the tenant index between this
    /// writer's index read and its ETag-guarded save, the policy reloads the latest index and re-applies, so
    /// the final index reflects BOTH writers' effects (no lost update; not a blind overwrite).
    /// </summary>
    [Fact]
    public async Task ConcurrentIndexWriteIsReloadedAndReapplied()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store);

        ConversationSummaryProjectionV1 competingSummary = Models(ConversationA, position: 1).Summary;
        ConversationProjectedReadModels models = Models(ConversationB, position: 1);

        int trySaveCount = 0;
        store.ConcurrentWriteBeforeTrySave = () =>
        {
            trySaveCount++;

            // TrySave #1 is the per-conversation write (left to succeed). TrySave #2 is the tenant index's
            // first save attempt: a competing writer commits conversation A into the index right before the
            // ETag check, forcing exactly one conflict on the index merge path. The hook then clears itself so
            // the policy's reload-and-reapply attempt succeeds.
            if (trySaveCount == 2)
            {
                store.SeedRaw(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                    new ConversationProjectionIndexReadModel { Summaries = [competingSummary] });
            }
        };

        await writer.PersistAsync(models, TestContext.Current.CancellationToken);

        ConversationProjectionIndexReadModel? index = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));

        index.ShouldNotBeNull();
        index!.Summaries.Select(summary => summary.ConversationId.Value)
            .ShouldBe([ConversationA.Value, ConversationB.Value], ignoreOrder: true);
    }

    /// <summary>
    /// AC-3 (fail-loud): when every index save loses the ETag race, the policy surfaces an
    /// <see cref="InvalidOperationException"/> after its bounded retries rather than silently dropping the update.
    /// </summary>
    [Fact]
    public async Task ExhaustedIndexRetriesFailLoud()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store);

        ConversationSummaryProjectionV1 competingSummary = Models(ConversationA, position: 1).Summary;
        ConversationProjectedReadModels models = Models(ConversationB, position: 1);

        int trySaveCount = 0;
        store.ConcurrentWriteBeforeTrySave = () =>
        {
            trySaveCount++;

            // Leave the per-conversation write (TrySave #1) alone; force a fresh competing commit before every
            // index save (TrySave #2+), so every ETag check loses and the retry budget is exhausted.
            if (trySaveCount >= 2)
            {
                store.SeedRaw(
                    ConversationProjectionReadModelKeys.StateStoreName,
                    ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                    new ConversationProjectionIndexReadModel { Summaries = [competingSummary] });
            }
        };

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await writer.PersistAsync(models, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// NFR5: re-applying the same materialization leaves the persisted value unchanged and never duplicates the
    /// tenant-index entry (idempotent transform + idempotent merge dedup by conversation identity).
    /// </summary>
    [Fact]
    public async Task ReapplyingSameMaterializationIsIdempotent()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store);

        ConversationProjectedReadModels models = Models(ConversationA, position: 1);

        await writer.PersistAsync(models, TestContext.Current.CancellationToken);
        int keysAfterFirst = store.Count;
        await writer.PersistAsync(models, TestContext.Current.CancellationToken);

        store.Count.ShouldBe(keysAfterFirst, "re-applying the same materialization must not create new keys.");

        ConversationProjectionIndexReadModel? index = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));
        index.ShouldNotBeNull();
        index!.Summaries.Count.ShouldBe(1, "idempotent merge must not duplicate the index entry on re-apply.");
        index.Summaries[0].ConversationId.ShouldBe(ConversationA);
    }

    /// <summary>
    /// NFR5 (newest generation wins): re-persisting the same conversation at a higher applied event position
    /// supersedes the stale index entry in place — the index keeps a single entry for the conversation carrying
    /// the newer generation, never a duplicate and never the stale summary.
    /// </summary>
    [Fact]
    public async Task RepersistingAtHigherGenerationSupersedesIndexEntry()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store);

        await writer.PersistAsync(Models(ConversationA, position: 1), TestContext.Current.CancellationToken);
        await writer.PersistAsync(Models(ConversationA, position: 5), TestContext.Current.CancellationToken);

        ConversationProjectionIndexReadModel? index = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));

        index.ShouldNotBeNull();
        index!.Summaries.Count.ShouldBe(1, "a newer generation must replace, not duplicate, the index entry.");
        index.Summaries[0].ConversationId.ShouldBe(ConversationA);
        index.Summaries[0].Freshness.LastAppliedEventPosition.ShouldBe(
            5,
            "the newer generation (higher applied event position) must win in the index.");
    }

    /// <summary>
    /// NFR5 (stale-write guard): a late, out-of-order re-persist at a LOWER applied event position must not
    /// overwrite the newer summary already in the tenant index — the merge keeps the newest generation.
    /// </summary>
    [Fact]
    public async Task RepersistingAtLowerGenerationDoesNotOverwriteIndexEntry()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store);

        await writer.PersistAsync(Models(ConversationA, position: 5), TestContext.Current.CancellationToken);
        await writer.PersistAsync(Models(ConversationA, position: 1), TestContext.Current.CancellationToken);

        ConversationProjectionIndexReadModel? index = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));

        index.ShouldNotBeNull();
        index!.Summaries.Count.ShouldBe(1);
        index.Summaries[0].ConversationId.ShouldBe(ConversationA);
        index.Summaries[0].Freshness.LastAppliedEventPosition.ShouldBe(
            5,
            "an older (lower-position) re-apply must not regress the index to a stale generation.");
    }

    /// <summary>
    /// AC-4 (fail-soft empty): a tenant with nothing persisted lists an empty set from the single index read,
    /// never null and never a per-conversation fan-out.
    /// </summary>
    [Fact]
    public async Task ListAsyncReturnsEmptyWhenNoIndexExists()
    {
        InMemoryReadModelStore inner = new();
        CountingReadModelStore store = new(inner);
        ConversationProjectionReadStore readStore = new(store);

        IReadOnlyList<ConversationSummaryProjectionV1> listed = await readStore.ListAsync(Tenant, TestContext.Current.CancellationToken);

        listed.ShouldBeEmpty();
        store.GetCalls.ShouldBe(1, "an absent index must still resolve via a single tenant-index read (no fan-out).");
    }

    private static ConversationProjectedReadModels Models(ConversationId conversationId, long position)
        => new ConversationProjectionMaterializer().Project(
            Tenant,
            conversationId,
            [
                new ConversationProjectionEventRecord(position, new ConversationCreated(
                    new ConversationEventMetadata(
                        SchemaVersion.Current,
                        $"event-create-{conversationId.Value}",
                        ConversationEventType.ConversationCreated,
                        Tenant,
                        conversationId,
                        $"correlation-{conversationId.Value}",
                        Now,
                        Actor,
                        $"causation-{conversationId.Value}"),
                    Label: $"Case {conversationId.Value}")),
            ],
            Now.AddSeconds(1),
            TimeSpan.FromMinutes(5));

    /// <summary>
    /// A thin <see cref="IReadModelStore"/> decorator that counts underlying reads so a test can prove the list
    /// boundary issues exactly one store read.
    /// </summary>
    private sealed class CountingReadModelStore(InMemoryReadModelStore inner) : IReadModelStore
    {
        public int GetCalls { get; private set; }

        public int Count => inner.Count;

        public Action? ConcurrentWriteBeforeTrySave
        {
            get => inner.ConcurrentWriteBeforeTrySave;
            set => inner.ConcurrentWriteBeforeTrySave = value;
        }

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
        {
            GetCalls++;
            return inner.GetAsync<TValue>(storeName, key, cancellationToken);
        }

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.SaveAsync(storeName, key, value, cancellationToken);

        public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);

        public void SeedRaw<TValue>(string storeName, string key, TValue value)
            where TValue : class
            => inner.SeedRaw(storeName, key, value);

        public void ResetCounters() => GetCalls = 0;
    }
}
