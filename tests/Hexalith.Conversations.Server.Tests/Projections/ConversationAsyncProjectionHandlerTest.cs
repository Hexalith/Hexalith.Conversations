// <copyright file="ConversationAsyncProjectionHandlerTest.cs" company="ITANEO">
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

/// <summary>Exercises durable named dispatch and full-replay planning at the Conversations boundary.</summary>
public sealed class ConversationAsyncProjectionHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Started = new(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AcceptedAppendShouldCompleteOnlyAfterBothExactKeysAreDurable()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-001",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldNotBeNull();
        ConversationProjectionIndexReadModel? index = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));
        index.ShouldNotBeNull();
        index!.Summaries.ShouldHaveSingleItem().ConversationId.ShouldBe(Conversation);
        index.Dispatches[Conversation.Value].DispatchId.ShouldBe("dispatch-001");
        store.Snapshot<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-001"))!
            .Status.ShouldBe(ConversationProjectionDispatchStatus.Completed);
    }

    [Fact]
    public async Task StableDuplicateShouldConvergeWithoutDuplicatingTheTenantIndex()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest request = Request(Tenant, Conversation);

        DomainProjectionHandlerResult first = await handler.ProjectAsync(request, "dispatch-duplicate", TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectedReadModels> detailBefore = await store.GetAsync<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionIndexReadModel> indexBefore = await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionDispatchLedger> ledgerBefore = await store.GetAsync<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-duplicate"),
            TestContext.Current.CancellationToken);

        DomainProjectionHandlerResult duplicate = await handler.ProjectAsync(request, "dispatch-duplicate", TestContext.Current.CancellationToken);

        first.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        duplicate.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Count.ShouldBe(3);
        (await store.GetAsync<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            TestContext.Current.CancellationToken)).ETag.ShouldBe(detailBefore.ETag);
        (await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken)).ETag.ShouldBe(indexBefore.ETag);
        (await store.GetAsync<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-duplicate"),
            TestContext.Current.CancellationToken)).ETag.ShouldBe(ledgerBefore.ETag);
        store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!
            .Summaries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SecondWriteFailureShouldBeRetryableAndRetryShouldConverge()
    {
        InMemoryReadModelStore inner = new();
        FailCompletedIndexSaveOnceReadModelStore store = new(inner);
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest request = Request(Tenant, Conversation);

        DomainProjectionHandlerResult partial = await handler.ProjectAsync(request, "dispatch-partial", TestContext.Current.CancellationToken);

        partial.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        partial.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.PartialRetry);
        inner.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldNotBeNull();
        ConversationProjectionIndexReadModel? pendingIndex = inner.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));
        pendingIndex.ShouldNotBeNull();
        pendingIndex!.Summaries.ShouldBeEmpty();
        pendingIndex.Dispatches[Conversation.Value].DispatchId.ShouldBe("dispatch-partial");
        inner.Snapshot<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-partial"))!
            .Status.ShouldBe(ConversationProjectionDispatchStatus.Pending);
        ConversationProjectionReadStore readStore = new(inner);
        _ = await Should.ThrowAsync<ConversationProjectionConsistencyException>(
            async () => await readStore.ReadAsync(Tenant, Conversation, TestContext.Current.CancellationToken));
        _ = await Should.ThrowAsync<ConversationProjectionConsistencyException>(
            async () => await readStore.ListAsync(Tenant, TestContext.Current.CancellationToken));

        DomainProjectionHandlerResult retried = await handler.ProjectAsync(request, "dispatch-partial", TestContext.Current.CancellationToken);

        retried.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        inner.Count.ShouldBe(3);
        inner.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!
            .Summaries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UnavailableStoreShouldReturnIndeterminateWithoutRawStorageDetail()
    {
        ConversationAsyncProjectionHandler handler = Handler(new UnavailableReadModelStore());

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-unavailable",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Indeterminate);
        result.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.HandlerFailure);
        result.ReasonCode!.ShouldNotContain("backend");
    }

    [Fact]
    public async Task CrossTenantEventShouldFailWithoutWritingEitherTenantScope()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        TenantId foreignTenant = new("tenant-foreign");

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation, Created(foreignTenant, Conversation)),
            "dispatch-foreign",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        result.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.HandlerFailure);
        store.Count.ShouldBe(0);
    }

    [Fact]
    public async Task FullReplayPlanShouldContainExactGenerationAndLedgerKeysWithoutMutatingLiveState()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);

        DomainProjectionRebuildPlan plan = await handler.PrepareRebuildAsync(
            Request(Tenant, Conversation),
            "rebuild-001",
            TestContext.Current.CancellationToken);

        plan.StoreName.ShouldBe(ConversationProjectionReadModelKeys.StateStoreName);
        plan.Operations.Select(operation => operation.Key).ShouldBe(
        [
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            ConversationProjectionReadModelKeys.DispatchLedgerKey("rebuild-001"),
        ]);
        plan.Operations.ShouldAllBe(operation => operation.Kind == ReadModelBatchOperationKind.Write);
        store.Count.ShouldBe(0, "rebuild preparation must remain side-effect free until coordinated promotion");
    }

    [Fact]
    public async Task NonCurrentMaterializationShouldFailWithoutPublishingDispatchState()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest gap = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [EventDto(Created(Tenant, Conversation), sequence: 2)]);

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            gap,
            "dispatch-gap",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        store.Count.ShouldBe(0);
    }

    [Fact]
    public async Task EmptyRebuildHistoryShouldBeRejectedAsTerminalInput()
    {
        ConversationAsyncProjectionHandler handler = Handler(new InMemoryReadModelStore());
        ProjectionRequest empty = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            []);

        DomainProjectionRebuildRejectedException exception = await Should.ThrowAsync<DomainProjectionRebuildRejectedException>(
            () => handler.PrepareRebuildAsync(empty, "rebuild-empty", TestContext.Current.CancellationToken));

        exception.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.HandlerFailure);
    }

    [Fact]
    public async Task PopulatedRebuildPlanShouldRetainValidSiblingAndUsePersistedEtags()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        await handler.ProjectAsync(
            Request(Tenant, ConversationB()),
            "dispatch-sibling",
            TestContext.Current.CancellationToken);
        await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-existing",
            TestContext.Current.CancellationToken);

        ReadModelEntry<ConversationProjectionIndexReadModel> validIndex = await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken);
        TenantId foreignTenant = new("tenant-foreign");
        ConversationId foreignConversation = new("conversation-foreign");
        ConversationProjectedReadModels foreign = new ConversationProjectionMaterializer().Project(
            foreignTenant,
            foreignConversation,
            [new ConversationProjectionEventRecord(1, Created(foreignTenant, foreignConversation))],
            Started.AddSeconds(2),
            TimeSpan.FromMinutes(5)) with
        {
            DispatchId = "dispatch-foreign-index",
        };
        Dictionary<string, ConversationProjectionDispatchReference> corruptedDispatches = new(
            validIndex.Value!.Dispatches,
            StringComparer.Ordinal)
        {
            [foreignConversation.Value] = new("dispatch-foreign-index", 1),
        };
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Summaries = [.. validIndex.Value.Summaries, validIndex.Value.Summaries[0], foreign.Summary],
                Dispatches = corruptedDispatches,
            });

        ReadModelEntry<ConversationProjectedReadModels> detailBefore = await store.GetAsync<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionIndexReadModel> indexBefore = await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken);

        DomainProjectionRebuildPlan plan = await handler.PrepareRebuildAsync(
            Request(Tenant, Conversation),
            "rebuild-populated",
            TestContext.Current.CancellationToken);

        ReadModelBatchOperation detailOperation = plan.Operations[0];
        ReadModelBatchOperation indexOperation = plan.Operations[1];
        detailOperation.Concurrency.ExpectedETag.ShouldBe(detailBefore.ETag);
        indexOperation.Concurrency.ExpectedETag.ShouldBe(indexBefore.ETag);
        plan.Operations[2].Concurrency.ShouldBe(ReadModelBatchConcurrency.CreateOnly);

        ConversationProjectionIndexReadModel rebuiltIndex = JsonSerializer.Deserialize<ConversationProjectionIndexReadModel>(
            indexOperation.CanonicalValue.Span,
            JsonOptions)!;
        rebuiltIndex.Summaries.Select(summary => summary.ConversationId.Value).ShouldBe(
            [Conversation.Value, ConversationB().Value],
            ignoreOrder: true);
        rebuiltIndex.Dispatches.Keys.ShouldBe(
            [Conversation.Value, ConversationB().Value],
            ignoreOrder: true);
    }

    private static ConversationAsyncProjectionHandler Handler(IReadModelStore store)
        => new(
            new ConversationProjectionMaterializer(),
            new ConversationProjectionReadModelWriter(store),
            store,
            new FixedTimeProvider(Started.AddSeconds(2)));

    private static ProjectionRequest Request(
        TenantId tenantId,
        ConversationId conversationId,
        ConversationCreated? created = null)
    {
        ConversationCreated @event = created ?? Created(tenantId, conversationId);
        return new ProjectionRequest(
            tenantId.Value,
            ConversationProjectionHandler.ConversationDomain,
            conversationId.Value,
            [EventDto(@event, sequence: 1)]);
    }

    private static ProjectionEventDto EventDto(ConversationCreated @event, long sequence)
        => new(
            nameof(ConversationCreated),
            JsonSerializer.SerializeToUtf8Bytes(@event, JsonOptions),
            "json",
            sequence,
            Started,
            "correlation-001");

    private static ConversationId ConversationB() => new("conversation-002");

    private static ConversationCreated Created(TenantId tenantId, ConversationId conversationId)
        => new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-created-001",
                ConversationEventType.ConversationCreated,
                tenantId,
                conversationId,
                "correlation-001",
                Started,
                Actor,
                "causation-001"),
            Label: "Production path conversation");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FailCompletedIndexSaveOnceReadModelStore(InMemoryReadModelStore inner) : IReadModelStore
    {
        private int _failed;

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.GetAsync<TValue>(storeName, key, cancellationToken);

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.SaveAsync(storeName, key, value, cancellationToken);

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            if (value is ConversationProjectionIndexReadModel { Summaries.Count: > 0 }
                && Interlocked.Exchange(ref _failed, 1) == 0)
            {
                throw new InvalidOperationException("injected completed-index failure");
            }

            return inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
        }
    }

    private sealed class UnavailableReadModelStore : IReadModelStore
    {
        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
            => throw new IOException("backend storage detail");

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
            => throw new IOException("backend storage detail");

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
            => throw new IOException("backend storage detail");
    }
}
