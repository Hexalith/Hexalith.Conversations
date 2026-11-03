// <copyright file="ConversationTenantIndexLifecycleTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Story 16.2 / AD-7: version 2 of the tenant index is the sole lifecycle/watermark record, only the writer authors
/// its transitions, only <c>TenantCreated</c> creates <c>Ready</c>, and reads map every state without writing.
/// </summary>
public sealed class ConversationTenantIndexLifecycleTest
{
    private static readonly TenantId Tenant = new("tenant-lifecycle");
    private static readonly ConversationId ConversationA = new("conversation-a");
    private static readonly ConversationId ConversationB = new("conversation-b");
    private static readonly PartyId Actor = new("party-lifecycle");
    private static readonly DateTimeOffset Started = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly string StoreName = ConversationProjectionReadModelKeys.StateStoreName;
    private static readonly string IndexKey = ConversationProjectionReadModelKeys.TenantIndexKey(Tenant);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary><c>TenantCreated</c> with no index creates a version 2 <c>Ready(empty)</c> index.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task TenantCreatedWithoutAnIndexShouldInitializeVersionTwoReadyEmpty()
    {
        InMemoryReadModelStore store = new();

        bool created = await new ConversationProjectionReadModelWriter(store)
            .InitializeTenantIndexAsync(Tenant, TestContext.Current.CancellationToken);

        created.ShouldBeTrue();
        ConversationProjectionIndexReadModel index = Index(store).ShouldNotBeNull();
        index.SchemaVersion.ShouldBe(2);
        index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        index.Watermarks.ShouldNotBeNull().ShouldBeEmpty();
        index.Summaries.ShouldBeEmpty();
        index.Dispatches.ShouldBeEmpty();
        (await new ConversationProjectionReadStore(store).ListAsync(Tenant, TestContext.Current.CancellationToken))
            .ReadState.ShouldBe(ConversationProjectionIndexReadState.Ready);
    }

    /// <summary>An existing index, whatever its version or lifecycle, is left unchanged by initialization.</summary>
    /// <param name="existing">The existing index shape.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("v1")]
    [InlineData("rebuilding")]
    [InlineData("ready")]
    public async Task TenantCreatedShouldLeaveAnExistingIndexUnchanged(string existing)
    {
        InMemoryReadModelStore store = new();
        ConversationProjectedReadModels models = Models(ConversationA, 1, "dispatch-a");
        store.SeedRaw(StoreName, IndexKey, existing switch
        {
            "v1" => new ConversationProjectionIndexReadModel { Summaries = [models.Summary], Dispatches = Dispatch(models) },
            "rebuilding" => V2(ConversationProjectionIndexLifecycle.Rebuilding, [models.Summary], Dispatch(models), Watermark(ConversationA, 1)),
            _ => V2(ConversationProjectionIndexLifecycle.Ready, [models.Summary], Dispatch(models), Watermark(ConversationA, 1)),
        });
        ReadModelEntry<ConversationProjectionIndexReadModel> before = await store.GetAsync<ConversationProjectionIndexReadModel>(
            StoreName, IndexKey, TestContext.Current.CancellationToken);

        bool created = await new ConversationProjectionReadModelWriter(store)
            .InitializeTenantIndexAsync(Tenant, TestContext.Current.CancellationToken);

        created.ShouldBeFalse();
        ReadModelEntry<ConversationProjectionIndexReadModel> after = await store.GetAsync<ConversationProjectionIndexReadModel>(
            StoreName, IndexKey, TestContext.Current.CancellationToken);
        after.ETag.ShouldBe(before.ETag);
    }

    /// <summary>A live delivery to an absent index produces version 2 <c>Rebuilding</c>, never <c>Ready</c>.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveDeliveryToAnAbsentIndexShouldProduceRebuilding()
    {
        InMemoryReadModelStore store = new();

        await new ConversationProjectionReadModelWriter(store)
            .PersistAsync(Models(ConversationA, 2, "dispatch-a"), TestContext.Current.CancellationToken);

        ConversationProjectionIndexReadModel index = Index(store).ShouldNotBeNull();
        index.SchemaVersion.ShouldBe(2);
        index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Rebuilding);
        index.Watermarks.ShouldNotBeNull()[ConversationA.Value].ShouldBe(2);
    }

    /// <summary>A live delivery to a version 1 index produces <c>Rebuilding</c> with watermarks seeded from its entries.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveDeliveryToAVersionOneIndexShouldProduceRebuildingWithSeededWatermarks()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectedReadModels legacy = Models(ConversationA, 3, "dispatch-a");
        store.SeedRaw(StoreName, IndexKey, new ConversationProjectionIndexReadModel
        {
            Summaries = [legacy.Summary],
            Dispatches = Dispatch(legacy),
        });

        await new ConversationProjectionReadModelWriter(store)
            .PersistAsync(Models(ConversationB, 1, "dispatch-b"), TestContext.Current.CancellationToken);

        ConversationProjectionIndexReadModel index = Index(store).ShouldNotBeNull();
        index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Rebuilding);
        index.Watermarks.ShouldNotBeNull().ShouldBe(new Dictionary<string, long>(StringComparer.Ordinal)
        {
            [ConversationA.Value] = 3,
            [ConversationB.Value] = 1,
        });
        index.Summaries.Select(summary => summary.ConversationId).ShouldBe([ConversationA, ConversationB]);
    }

    /// <summary>
    /// Live deliveries keep a <c>Ready</c> index ready, advance watermarks only with a stored completed summary,
    /// and never lower one; a pending marker never advances a watermark.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ReadyIndexShouldStayReadyWithNonRegressingWatermarks()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store);
        await writer.InitializeTenantIndexAsync(Tenant, TestContext.Current.CancellationToken);

        await writer.MarkPendingAsync(Models(ConversationA, 1, "dispatch-a1"), TestContext.Current.CancellationToken);
        Index(store)!.Watermarks.ShouldNotBeNull().ContainsKey(ConversationA.Value).ShouldBeFalse("a pending marker publishes no summary");

        await writer.PersistAsync(Models(ConversationA, 1, "dispatch-a1"), TestContext.Current.CancellationToken);
        await writer.PersistAsync(Models(ConversationA, 3, "dispatch-a3"), TestContext.Current.CancellationToken);
        await writer.PersistAsync(Models(ConversationA, 2, "dispatch-a2"), TestContext.Current.CancellationToken);

        ConversationProjectionIndexReadModel index = Index(store).ShouldNotBeNull();
        index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        index.Watermarks.ShouldNotBeNull()[ConversationA.Value].ShouldBe(3);
        index.Summaries.ShouldHaveSingleItem().Freshness.LastAppliedEventPosition.ShouldBe(3);
        (await new ConversationProjectionReadStore(store).ListAsync(Tenant, TestContext.Current.CancellationToken))
            .ReadState.ShouldBe(ConversationProjectionIndexReadState.Ready);
    }

    /// <summary>
    /// The rebuild plan's replacement index keeps the lifecycle and never lowers a watermark; it never creates
    /// <c>Ready</c> from an absent index.
    /// </summary>
    [Fact]
    public void RebuildIndexShouldKeepTheLifecycleAndNeverLowerAWatermark()
    {
        ConversationProjectedReadModels newer = Models(ConversationA, 5, "dispatch-a5");
        ConversationProjectedReadModels older = Models(ConversationA, 3, "rebuild-a3");
        ConversationProjectionIndexReadModel ready = V2(
            ConversationProjectionIndexLifecycle.Ready, [newer.Summary], Dispatch(newer), Watermark(ConversationA, 5));

        ConversationProjectionIndexReadModel replaced = ConversationProjectionReadModelWriter.PlanRebuildIndex(
            ready, [older.Summary], Dispatch(older));
        ConversationProjectionIndexReadModel fromAbsent = ConversationProjectionReadModelWriter.PlanRebuildIndex(
            null, [older.Summary], Dispatch(older));

        replaced.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        replaced.Watermarks.ShouldNotBeNull()[ConversationA.Value].ShouldBe(5);
        ConversationProjectionReadStore.ClassifyLifecycle(replaced).ShouldBe(ConversationProjectionIndexReadState.GapDetected);
        fromAbsent.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Rebuilding);
        fromAbsent.Watermarks.ShouldNotBeNull()[ConversationA.Value].ShouldBe(3);
    }

    /// <summary>
    /// A full-replay rebuild of a conversation of an initialized tenant keeps the index <c>Ready</c> and its watermark,
    /// through the production handler's plan rather than the writer helper alone.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task HandlerRebuildPlanShouldKeepAnInitializedIndexReady()
    {
        InMemoryReadModelStore store = await InitializedTenantWithOneConversationAsync();

        DomainProjectionRebuildPlan plan = await Handler(store).PrepareRebuildAsync(
            Request(ConversationA), "rebuild-a", TestContext.Current.CancellationToken);

        ConversationProjectionIndexReadModel index = JsonSerializer.Deserialize<ConversationProjectionIndexReadModel>(
            plan.Operations.Single(operation => operation.Key == IndexKey).CanonicalValue.Span,
            JsonOptions)!;
        index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        index.Watermarks.ShouldNotBeNull()[ConversationA.Value].ShouldBe(1);
        ConversationProjectionReadStore.ClassifyLifecycle(index).ShouldBe(ConversationProjectionIndexReadState.Ready);
    }

    /// <summary>
    /// A terminal reconcile that removes another conversation's pending marker keeps an initialized index <c>Ready</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task TerminalReconcileShouldKeepAnInitializedIndexReady()
    {
        InMemoryReadModelStore store = await InitializedTenantWithOneConversationAsync();
        ConversationProjectionReadModelWriter writer = new(store);
        await writer.MarkPendingAsync(Models(ConversationB, 1, "dispatch-b"), TestContext.Current.CancellationToken);

        bool reconciled = await writer.TryReconcileTerminalDispatchAsync(
            Tenant, ConversationB, "dispatch-b", TestContext.Current.CancellationToken);

        reconciled.ShouldBeTrue();
        ConversationProjectionIndexReadModel index = Index(store).ShouldNotBeNull();
        index.Dispatches.ContainsKey(ConversationB.Value).ShouldBeFalse();
        index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        ConversationProjectionReadStore.ClassifyLifecycle(index).ShouldBe(ConversationProjectionIndexReadState.Ready);
    }

    /// <summary>
    /// The writer refuses an unknown schema version without touching the index, and never infers a lifecycle a
    /// contradictory version 2 record lacks: readers still report it as contradictory after a live delivery.
    /// </summary>
    /// <param name="shape">The persisted index shape.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("unknown-schema-version")]
    [InlineData("v2-without-lifecycle")]
    [InlineData("v2-unknown-lifecycle")]
    public async Task WriterShouldNotRepairOrOverwriteAnUnsupportedIndex(string shape)
    {
        InMemoryReadModelStore store = Seed(shape);
        ReadModelEntry<ConversationProjectionIndexReadModel> before = await store.GetAsync<ConversationProjectionIndexReadModel>(
            StoreName, IndexKey, TestContext.Current.CancellationToken);
        ConversationProjectionReadModelWriter writer = new(store);

        if (shape == "unknown-schema-version")
        {
            _ = await Should.ThrowAsync<InvalidOperationException>(() => writer.PersistAsync(
                Models(ConversationB, 1, "dispatch-b"), TestContext.Current.CancellationToken));
            ReadModelEntry<ConversationProjectionIndexReadModel> after = await store.GetAsync<ConversationProjectionIndexReadModel>(
                StoreName, IndexKey, TestContext.Current.CancellationToken);
            after.ETag.ShouldBe(before.ETag);
            return;
        }

        await writer.PersistAsync(Models(ConversationB, 1, "dispatch-b"), TestContext.Current.CancellationToken);

        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await new ConversationProjectionReadStore(store).ListAsync(Tenant, TestContext.Current.CancellationToken));
    }

    /// <summary>Every persisted index shape maps to exactly one read state, without writing.</summary>
    /// <param name="shape">The persisted index shape.</param>
    /// <param name="expected">The expected read state.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("ready-empty", "Ready")]
    [InlineData("ready-covered", "Ready")]
    [InlineData("v1", "Rebuilding")]
    [InlineData("v1-explicit", "Rebuilding")]
    [InlineData("rebuilding", "Rebuilding")]
    [InlineData("rebuilding-with-gap", "Rebuilding")]
    [InlineData("summary-below-watermark", "GapDetected")]
    [InlineData("watermark-without-summary", "GapDetected")]
    [InlineData("summary-without-watermark", "GapDetected")]
    [InlineData("missing", "Missing")]
    public async Task ReadStoreShouldMapEachLifecycleShape(string shape, string expected)
    {
        InMemoryReadModelStore store = Seed(shape);
        int before = store.Count;

        ConversationProjectionIndexSnapshot snapshot = await new ConversationProjectionReadStore(store)
            .ListAsync(Tenant, TestContext.Current.CancellationToken);

        snapshot.ReadState.ShouldBe(Enum.Parse<ConversationProjectionIndexReadState>(expected));
        store.Count.ShouldBe(before, "a read never writes or infers a lifecycle");
    }

    /// <summary>Duplicated entries and contradictory lifecycle records fail closed as contradictory metadata.</summary>
    /// <param name="shape">The persisted index shape.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("duplicate-entries")]
    [InlineData("v2-without-lifecycle")]
    [InlineData("v2-unknown-lifecycle")]
    [InlineData("v2-without-watermarks")]
    [InlineData("unknown-schema-version")]
    [InlineData("v1-with-lifecycle")]
    [InlineData("summary-above-watermark")]
    [InlineData("non-positive-watermark")]
    [InlineData("blank-watermark-key")]
    public async Task ReadStoreShouldRejectContradictoryLifecycleRecords(string shape)
    {
        InMemoryReadModelStore store = Seed(shape);

        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await new ConversationProjectionReadStore(store).ListAsync(Tenant, TestContext.Current.CancellationToken));
    }

    private static InMemoryReadModelStore Seed(string shape)
    {
        InMemoryReadModelStore store = new();
        ConversationProjectedReadModels a = Models(ConversationA, 2, "dispatch-a");
        ConversationProjectedReadModels b = Models(ConversationB, 1, "dispatch-b");
        ConversationProjectionIndexReadModel? index = shape switch
        {
            "ready-empty" => ConversationProjectionReadModelWriter.CreateInitializedIndex(),
            "ready-covered" => V2(ConversationProjectionIndexLifecycle.Ready, [a.Summary], Dispatch(a), Watermark(ConversationA, 2)),
            "v1" => new ConversationProjectionIndexReadModel { Summaries = [a.Summary], Dispatches = Dispatch(a) },
            "v1-explicit" => new ConversationProjectionIndexReadModel { SchemaVersion = 1, Summaries = [a.Summary], Dispatches = Dispatch(a) },
            "rebuilding" => V2(ConversationProjectionIndexLifecycle.Rebuilding, [a.Summary], Dispatch(a), Watermark(ConversationA, 2)),
            "rebuilding-with-gap" => V2(ConversationProjectionIndexLifecycle.Rebuilding, [a.Summary], Dispatch(a), Watermark(ConversationA, 4)),
            "summary-below-watermark" => V2(ConversationProjectionIndexLifecycle.Ready, [a.Summary], Dispatch(a), Watermark(ConversationA, 4)),
            "watermark-without-summary" => V2(
                ConversationProjectionIndexLifecycle.Ready,
                [a.Summary],
                Dispatch(a),
                new Dictionary<string, long>(StringComparer.Ordinal) { [ConversationA.Value] = 2, [ConversationB.Value] = 1 }),
            "summary-without-watermark" => V2(
                ConversationProjectionIndexLifecycle.Ready,
                [a.Summary, b.Summary],
                new Dictionary<string, ConversationProjectionDispatchReference>(Dispatch(a), StringComparer.Ordinal) { [ConversationB.Value] = new("dispatch-b", 1) },
                Watermark(ConversationA, 2)),
            "missing" => null,
            "duplicate-entries" => V2(ConversationProjectionIndexLifecycle.Ready, [a.Summary, a.Summary], Dispatch(a), Watermark(ConversationA, 2)),
            "v2-without-lifecycle" => new ConversationProjectionIndexReadModel
            {
                SchemaVersion = 2,
                Watermarks = Watermark(ConversationA, 2),
                Summaries = [a.Summary],
                Dispatches = Dispatch(a),
            },
            "v2-unknown-lifecycle" => V2((ConversationProjectionIndexLifecycle)9, [a.Summary], Dispatch(a), Watermark(ConversationA, 2)),
            "v2-without-watermarks" => new ConversationProjectionIndexReadModel
            {
                SchemaVersion = 2,
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Summaries = [a.Summary],
                Dispatches = Dispatch(a),
            },
            "unknown-schema-version" => new ConversationProjectionIndexReadModel
            {
                SchemaVersion = 3,
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Watermarks = Watermark(ConversationA, 2),
                Summaries = [a.Summary],
                Dispatches = Dispatch(a),
            },
            "v1-with-lifecycle" => new ConversationProjectionIndexReadModel
            {
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Summaries = [a.Summary],
                Dispatches = Dispatch(a),
            },
            "summary-above-watermark" => V2(ConversationProjectionIndexLifecycle.Ready, [a.Summary], Dispatch(a), Watermark(ConversationA, 1)),
            "non-positive-watermark" => V2(
                ConversationProjectionIndexLifecycle.Ready,
                [a.Summary],
                Dispatch(a),
                new Dictionary<string, long>(StringComparer.Ordinal) { [ConversationA.Value] = 2, [ConversationB.Value] = 0 }),
            "blank-watermark-key" => V2(
                ConversationProjectionIndexLifecycle.Ready,
                [a.Summary],
                Dispatch(a),
                new Dictionary<string, long>(StringComparer.Ordinal) { [ConversationA.Value] = 2, [" "] = 1 }),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        if (index is not null)
        {
            store.SeedRaw(StoreName, IndexKey, index);
        }

        return store;
    }

    private static async Task<InMemoryReadModelStore> InitializedTenantWithOneConversationAsync()
    {
        InMemoryReadModelStore store = new();
        (await new ConversationProjectionReadModelWriter(store).InitializeTenantIndexAsync(Tenant, TestContext.Current.CancellationToken))
            .ShouldBeTrue();
        (await Handler(store).ProjectAsync(Request(ConversationA), "dispatch-a", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        Index(store).ShouldNotBeNull().Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        return store;
    }

    private static ConversationAsyncProjectionHandler Handler(InMemoryReadModelStore store)
        => new(
            new ConversationProjectionMaterializer(),
            new ConversationProjectionReadModelWriter(store),
            store,
            new FixedTimeProvider(Started.AddMinutes(1)));

    private static ProjectionRequest Request(ConversationId conversationId)
        => new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            conversationId.Value,
            [new ProjectionEventDto(
                nameof(ConversationCreated),
                JsonSerializer.SerializeToUtf8Bytes(Created(conversationId), JsonOptions),
                "json",
                1,
                Started,
                "correlation-lifecycle",
                MessageId: $"message-created-{conversationId.Value}")]);

    private static ConversationCreated Created(ConversationId conversationId)
        => new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                $"event-created-{conversationId.Value}",
                ConversationEventType.ConversationCreated,
                Tenant,
                conversationId,
                "correlation-lifecycle",
                Started,
                Actor),
            Label: "Lifecycle conversation");

    private static ConversationProjectionIndexReadModel? Index(InMemoryReadModelStore store)
        => store.Snapshot<ConversationProjectionIndexReadModel>(StoreName, IndexKey);

    private static ConversationProjectionIndexReadModel V2(
        ConversationProjectionIndexLifecycle lifecycle,
        IReadOnlyList<ConversationSummaryProjectionV1> summaries,
        IReadOnlyDictionary<string, ConversationProjectionDispatchReference> dispatches,
        IReadOnlyDictionary<string, long> watermarks)
        => new()
        {
            SchemaVersion = 2,
            Lifecycle = lifecycle,
            Watermarks = watermarks,
            Summaries = summaries,
            Dispatches = dispatches,
        };

    private static Dictionary<string, long> Watermark(ConversationId conversationId, long position)
        => new(StringComparer.Ordinal) { [conversationId.Value] = position };

    private static Dictionary<string, ConversationProjectionDispatchReference> Dispatch(ConversationProjectedReadModels models)
        => new(StringComparer.Ordinal)
        {
            [models.Summary.ConversationId.Value] = new(models.DispatchId, models.Summary.Freshness.LastAppliedEventPosition),
        };

    private static ConversationProjectedReadModels Models(ConversationId conversationId, long position, string dispatchId)
    {
        List<ConversationProjectionEventRecord> events = [new(1, Created(conversationId))];
        for (long sequence = 2; sequence <= position; sequence++)
        {
            events.Add(new(sequence, new ConversationProjectionPositionOnlyEvent($"message-{conversationId.Value}-{sequence}", Started.AddSeconds(sequence))));
        }

        return new ConversationProjectionMaterializer().Project(
            Tenant,
            conversationId,
            events,
            Started.AddMinutes(1),
            TimeSpan.FromMinutes(5)) with
        {
            DispatchId = dispatchId,
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
