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
    }

    [Fact]
    public async Task StableDuplicateShouldConvergeWithoutDuplicatingTheTenantIndex()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest request = Request(Tenant, Conversation);

        DomainProjectionHandlerResult first = await handler.ProjectAsync(request, "dispatch-duplicate", TestContext.Current.CancellationToken);
        DomainProjectionHandlerResult duplicate = await handler.ProjectAsync(request, "dispatch-duplicate", TestContext.Current.CancellationToken);

        first.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        duplicate.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Count.ShouldBe(2);
        store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!
            .Summaries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SecondWriteFailureShouldBeRetryableAndRetryShouldConverge()
    {
        InMemoryReadModelStore inner = new();
        FailSecondSaveOnceReadModelStore store = new(inner);
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest request = Request(Tenant, Conversation);

        DomainProjectionHandlerResult partial = await handler.ProjectAsync(request, "dispatch-partial", TestContext.Current.CancellationToken);

        partial.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        partial.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.PartialRetry);
        inner.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldNotBeNull();
        inner.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant)).ShouldBeNull();

        DomainProjectionHandlerResult retried = await handler.ProjectAsync(request, "dispatch-partial", TestContext.Current.CancellationToken);

        retried.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        inner.Count.ShouldBe(2);
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
    public async Task FullReplayPlanShouldContainBothExactKeysWithoutMutatingLiveState()
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
        ]);
        plan.Operations.ShouldAllBe(operation => operation.Kind == ReadModelBatchOperationKind.Write);
        store.Count.ShouldBe(0, "rebuild preparation must remain side-effect free until coordinated promotion");
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
            [new ProjectionEventDto(
                nameof(ConversationCreated),
                JsonSerializer.SerializeToUtf8Bytes(@event, JsonOptions),
                "json",
                1,
                Started,
                "correlation-001")]);
    }

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

    private sealed class FailSecondSaveOnceReadModelStore(InMemoryReadModelStore inner) : IReadModelStore
    {
        private int _saveAttempts;

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
            if (Interlocked.Increment(ref _saveAttempts) == 2)
            {
                throw new InvalidOperationException("injected second-write failure");
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
