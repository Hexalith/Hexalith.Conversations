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
    /// A persisted summary/detail pair reads back identically, and the list boundary validates it with one
    /// tenant-index read plus bounded platform bulk reads (no per-conversation remote fan-out, NFR2).
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
        await SeedCompletedLedgerAsync(inner, models);

        ConversationProjectedReadModels? readBack = await readStore.ReadAsync(Tenant, ConversationA, TestContext.Current.CancellationToken);
        readBack.ShouldNotBeNull();
        readBack!.Summary.ConversationId.ShouldBe(ConversationA);
        readBack.Summary.Label.ShouldBe(models.Summary.Label);
        readBack.Detail.ConversationId.ShouldBe(ConversationA);
        readBack.Detail.Label.ShouldBe(models.Detail.Label);

        store.ResetCounters();
        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(Tenant, TestContext.Current.CancellationToken);

        snapshot.Summaries.Select(summary => summary.ConversationId.Value).ShouldBe([ConversationA.Value]);
        snapshot.HasIncompleteDispatch.ShouldBeFalse();
        store.GetCalls.ShouldBe(1, "ListAsync must issue only the tenant-index scalar read.");
        store.BulkGetCalls.ShouldBe(0, "ListAsync must not read any detail or ledger key (NFR2, no per-conversation fan-out).");

        store.ResetCounters();
        IReadOnlySet<string> inconsistent = await readStore.ValidatePageAsync(
            Tenant,
            snapshot,
            snapshot.Summaries,
            TestContext.Current.CancellationToken);

        inconsistent.ShouldBeEmpty();
        store.GetCalls.ShouldBe(0, "page verification must not re-read the tenant index it was given.");
        store.BulkGetCalls.ShouldBe(2, "page verification bulk-reads detail generations and completion ledgers.");
    }

    /// <summary>
    /// A list larger than one platform page is validated in bounded chunks for both details and ledgers.
    /// </summary>
    [Fact]
    public async Task ListValidationShouldUseBoundedBulkPages()
    {
        const int ConversationCount = 205;
        InMemoryReadModelStore inner = new();
        CountingReadModelStore store = new(inner);
        ConversationProjectionReadStore readStore = new(store);
        var summaries = new List<ConversationSummaryProjectionV1>(ConversationCount);
        var dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(
            ConversationCount,
            StringComparer.Ordinal);

        for (int index = 0; index < ConversationCount; index++)
        {
            ConversationProjectedReadModels models = Models(new ConversationId($"conversation-{index:D3}"), position: 1);
            summaries.Add(models.Summary);
            dispatches[models.Summary.ConversationId.Value] = new(
                models.DispatchId,
                models.Summary.Freshness.LastAppliedEventPosition);
            inner.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, models.Summary.ConversationId),
                models);
            inner.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey(models.DispatchId),
                CompletedLedger(models));
        }

        inner.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Summaries = summaries,
                Dispatches = dispatches,
            });

        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(
            Tenant,
            TestContext.Current.CancellationToken);

        // The whole point of the split: listing a 205-conversation tenant costs exactly one read, whatever the
        // tenant's size. Verification cost is charged to the page, not to the tenant.
        snapshot.Summaries.Count.ShouldBe(ConversationCount);
        store.GetCalls.ShouldBe(1);
        store.BulkGetCalls.ShouldBe(0, "listing must never fan out over the tenant (NFR2, no N+1).");

        store.ResetCounters();
        IReadOnlyList<ConversationSummaryProjectionV1> page = [.. snapshot.Summaries.Take(25)];
        IReadOnlySet<string> pageInconsistent = await readStore.ValidatePageAsync(
            Tenant,
            snapshot,
            page,
            TestContext.Current.CancellationToken);

        pageInconsistent.ShouldBeEmpty();
        store.BulkGetCalls.ShouldBe(2, "a 25-row page costs one detail page and one ledger page.");
        store.MaximumBulkKeyCount.ShouldBe(
            25,
            "verification must request only the page's keys, never the tenant's.");

        // Chunking still bounds a caller that deliberately verifies everything.
        store.ResetCounters();
        IReadOnlySet<string> allInconsistent = await readStore.ValidatePageAsync(
            Tenant,
            snapshot,
            snapshot.Summaries,
            TestContext.Current.CancellationToken);

        allInconsistent.ShouldBeEmpty();
        store.BulkGetCalls.ShouldBe(6, "205 details and 205 ledgers require three bounded pages each.");
        store.MaximumBulkKeyCount.ShouldBe(100);
        store.ObservedParallelism.ShouldBe([8]);
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
            async () => await writer.PersistAsync(models, TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
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
    /// Equal-position deliveries converge on one deterministic dispatch winner regardless of arrival order,
    /// so the separately persisted detail and tenant-index keys cannot settle on different identities.
    /// </summary>
    [Fact]
    public async Task EqualPositionDispatchesShouldChooseTheSameWinnerInEitherOrder()
    {
        ConversationProjectedReadModels lower = Models(
            ConversationA,
            position: 1,
            dispatchId: "dispatch-a",
            label: "lower tie-break identity");
        ConversationProjectedReadModels higher = Models(
            ConversationA,
            position: 1,
            dispatchId: "dispatch-z",
            label: "higher tie-break identity");

        foreach (ConversationProjectedReadModels[] order in new[]
        {
            new[] { lower, higher },
            new[] { higher, lower },
        })
        {
            InMemoryReadModelStore store = new();
            ConversationProjectionReadModelWriter writer = new(store);
            foreach (ConversationProjectedReadModels generation in order)
            {
                await writer.PersistAsync(generation, TestContext.Current.CancellationToken);
            }

            ConversationProjectedReadModels detail = store.Snapshot<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, ConversationA))!;
            ConversationProjectionIndexReadModel index = store.Snapshot<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;

            detail.DispatchId.ShouldBe("dispatch-z");
            detail.Summary.Label.ShouldBe("higher tie-break identity");
            index.Dispatches[ConversationA.Value].DispatchId.ShouldBe(detail.DispatchId);
            index.Summaries.ShouldHaveSingleItem().Label.ShouldBe(detail.Summary.Label);
        }
    }

    /// <summary>Opaque public identifiers remain legal and cannot collide when composed into state keys.</summary>
    [Fact]
    public void ProjectionKeysShouldEncodeOpaqueIdentifierSegments()
    {
        string first = ConversationProjectionReadModelKeys.ConversationKey(
            new TenantId("tenant:a"),
            new ConversationId("b"));
        string second = ConversationProjectionReadModelKeys.ConversationKey(
            new TenantId("tenant"),
            new ConversationId("a:b"));

        first.ShouldNotBe(second);
        ConversationProjectionReadModelKeys.TenantIndexKey(new TenantId("tenant:a"))
            .ShouldNotBe(ConversationProjectionReadModelKeys.TenantIndexKey(new TenantId("tenant")));
    }

    /// <summary>
    /// AC-4 (fail-soft empty): a tenant with nothing persisted lists an empty set from the single index read,
    /// never null and never a per-conversation fan-out.
    /// </summary>
    [Fact]
    public async Task ListAsyncShouldReadAnAbsentIndexAsAnEmptyTenant()
    {
        InMemoryReadModelStore inner = new();
        CountingReadModelStore store = new(inner);
        ConversationProjectionReadStore readStore = new(store);

        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(
            Tenant,
            TestContext.Current.CancellationToken);

        // A tenant that has never held a conversation has no index key. Treating that as a cross-key
        // inconsistency would leave every new tenant permanently Rebuilding with an empty page.
        snapshot.Summaries.ShouldBeEmpty();
        snapshot.Dispatches.ShouldBeEmpty();
        snapshot.HasIncompleteDispatch.ShouldBeFalse();
        store.GetCalls.ShouldBe(1, "an empty tenant costs exactly the tenant-index read.");
        store.BulkGetCalls.ShouldBe(0);
    }

    /// <summary>
    /// An index naming the same conversation twice is structural corruption of the index and still fails closed
    /// for the whole tenant: no page taken from it can be trusted.
    /// </summary>
    [Fact]
    public async Task ListAsyncShouldRejectAnIndexNamingOneConversationTwice()
    {
        InMemoryReadModelStore inner = new();
        CountingReadModelStore store = new(inner);
        ConversationProjectionReadStore readStore = new(store);
        ConversationProjectedReadModels models = Models(ConversationA, position: 1);
        inner.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Summaries = [models.Summary, models.Summary],
                Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
                {
                    [ConversationA.Value] = new(models.DispatchId, 1),
                },
            });

        _ = await Should.ThrowAsync<ConversationProjectionConsistencyException>(
            async () => await readStore.ListAsync(Tenant, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// A dispatch reference the summaries do not reflect marks the snapshot incomplete without making any
    /// individual conversation unreadable: the tenant may be holding a conversation no page can show yet.
    /// </summary>
    [Fact]
    public async Task ListAsyncShouldFlagAPendingDispatchWithoutFailingTheTenant()
    {
        InMemoryReadModelStore inner = new();
        CountingReadModelStore store = new(inner);
        ConversationProjectionReadStore readStore = new(store);
        ConversationProjectedReadModels persisted = Models(ConversationA, position: 1);
        inner.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Summaries = [persisted.Summary],
                Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
                {
                    [ConversationA.Value] = new(persisted.DispatchId, 1),

                    // ConversationB is mid-dispatch: marked pending, no summary written yet.
                    [ConversationB.Value] = new("dispatch-in-flight", 1),
                },
            });

        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(
            Tenant,
            TestContext.Current.CancellationToken);

        snapshot.HasIncompleteDispatch.ShouldBeTrue();
        snapshot.Summaries.Select(summary => summary.ConversationId.Value).ShouldBe([ConversationA.Value]);
    }

    /// <summary>
    /// Matching detail and index payloads are not sufficient while their dispatch ledger is pending; the real
    /// bulk validator must withhold the row until completion becomes durable.
    /// </summary>
    [Fact]
    public async Task ValidatePageShouldRejectAnOtherwiseMatchingPendingLedger()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectedReadModels models = Models(ConversationA, position: 1);
        await new ConversationProjectionReadModelWriter(store)
            .PersistAsync(models, TestContext.Current.CancellationToken);
        await store.SaveAsync(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey(models.DispatchId),
            CompletedLedger(models) with { Status = ConversationProjectionDispatchStatus.Pending },
            TestContext.Current.CancellationToken);
        ConversationProjectionReadStore readStore = new(store);
        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(
            Tenant,
            TestContext.Current.CancellationToken);

        IReadOnlySet<string> inconsistent = await readStore.ValidatePageAsync(
            Tenant,
            snapshot,
            snapshot.Summaries,
            TestContext.Current.CancellationToken);

        inconsistent.ShouldBe([ConversationA.Value]);
    }

    /// <summary>
    /// A completed ledger is retained only for the supported redelivery window. Once it expires, matching
    /// durable detail/index generations remain readable; this bounds ledger growth without expiring projections.
    /// </summary>
    [Fact]
    public async Task MatchingProjectionShouldRemainReadableAfterDispatchLedgerExpires()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectedReadModels models = Models(ConversationA, position: 1);
        await new ConversationProjectionReadModelWriter(store)
            .PersistAsync(models, TestContext.Current.CancellationToken);
        ConversationProjectionReadStore readStore = new(store);

        ConversationProjectedReadModels? read = await readStore.ReadAsync(
            Tenant,
            ConversationA,
            TestContext.Current.CancellationToken);
        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(
            Tenant,
            TestContext.Current.CancellationToken);
        IReadOnlySet<string> inconsistent = await readStore.ValidatePageAsync(
            Tenant,
            snapshot,
            snapshot.Summaries,
            TestContext.Current.CancellationToken);

        read.ShouldNotBeNull();
        read!.DispatchId.ShouldBe(models.DispatchId);
        inconsistent.ShouldBeEmpty();
    }

    private static ConversationProjectedReadModels Models(
        ConversationId conversationId,
        long position,
        string? dispatchId = null,
        string? label = null)
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
                    Label: label ?? $"Case {conversationId.Value}")),
            ],
            Now.AddSeconds(1),
            TimeSpan.FromMinutes(5)) with
        {
            DispatchId = dispatchId ?? $"dispatch-{conversationId.Value}-{position}",
        };

    private static Task SeedCompletedLedgerAsync(
        InMemoryReadModelStore store,
        ConversationProjectedReadModels models)
        => store.SaveAsync(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey(models.DispatchId),
            CompletedLedger(models),
            TestContext.Current.CancellationToken);

    private static ConversationProjectionDispatchLedger CompletedLedger(ConversationProjectedReadModels models)
        => new(
            models.DispatchId,
            $"fingerprint-{models.DispatchId}",
            models.Summary.TenantId,
            models.Summary.ConversationId,
            models.Summary.Freshness.ProjectionGeneratedAt,
            ConversationProjectionDispatchStatus.Completed);

    /// <summary>
    /// A thin <see cref="IReadModelStore"/> decorator that counts underlying reads so a test can prove the list
    /// boundary issues exactly one store read.
    /// </summary>
    private sealed class CountingReadModelStore(InMemoryReadModelStore inner) : IReadModelStore, IReadModelBulkStore
    {
        public int GetCalls { get; private set; }

        public int BulkGetCalls { get; private set; }

        public int MaximumBulkKeyCount { get; private set; }

        public IReadOnlyList<int> ObservedParallelism => [.. _observedParallelism.Order()];

        private readonly HashSet<int> _observedParallelism = [];

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

        public Task<IReadOnlyList<ReadModelBulkEntry<TValue>>> GetManyAsync<TValue>(
            string storeName,
            IReadOnlyList<string> keys,
            int parallelism,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            BulkGetCalls++;
            MaximumBulkKeyCount = Math.Max(MaximumBulkKeyCount, keys.Count);
            _observedParallelism.Add(parallelism);
            return inner.GetManyAsync<TValue>(storeName, keys, parallelism, cancellationToken);
        }

        public void SeedRaw<TValue>(string storeName, string key, TValue value)
            where TValue : class
            => inner.SeedRaw(storeName, key, value);

        public void ResetCounters()
        {
            GetCalls = 0;
            BulkGetCalls = 0;
            MaximumBulkKeyCount = 0;
            _observedParallelism.Clear();
        }
    }
}
