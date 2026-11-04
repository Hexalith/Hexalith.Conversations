// <copyright file="ConversationTenantIndexInitializerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.UniqueIds;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Story 16.2 / AD-7: <c>TenantCreated</c> initializes the tenant's Conversations index as <c>Ready(empty)</c> before
/// any Tenants handler runs, so a tenant becomes healthy only after its index is ready.
/// </summary>
public sealed class ConversationTenantIndexInitializerTest
{
    private const string Caller = "user-initialized";
    private static readonly DateTimeOffset At = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The initializer is registered before every Tenants <c>TenantCreated</c> handler.</summary>
    /// <param name="enrolled">Whether the enrolled (configured) registration is used.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InitializerShouldBeRegisteredBeforeTheTenantsHandlers(bool enrolled)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IReadModelStore>(new InMemoryReadModelStore());
        if (enrolled)
        {
            services.AddConversationTenantAccess(DurableTenantAccessHarness.Configuration);
        }
        else
        {
            services.AddConversationTenantAccess();
        }

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IEventStoreDomainEventHandler<TenantCreated>[] handlers =
            [.. scope.ServiceProvider.GetServices<IEventStoreDomainEventHandler<TenantCreated>>()];

        handlers.Length.ShouldBeGreaterThanOrEqualTo(2);
        handlers[0].ShouldBeOfType<ConversationTenantIndexInitializer>();
        handlers.Skip(1).ShouldContain(handler => handler is TenantProjectionEventHandler);
        handlers.Count(handler => handler is ConversationTenantIndexInitializer).ShouldBe(1);
    }

    /// <summary>
    /// Delivering <c>TenantCreated</c> creates the version 2 <c>Ready(empty)</c> index. A same-message redelivery is a
    /// processor duplicate; a redelivery under a fresh message identity reaches the initializer, whose create-only
    /// write leaves the existing index unchanged.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task TenantCreatedShouldInitializeTheIndexAndRedeliveryShouldLeaveItUnchanged()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider host = harness.CreateHost();
        string tenant = "tenant-initialized";
        var created = harness.Persist(new TenantCreated(tenant, "Initialized", null, At), tenant, 1);

        (await DurableTenantAccessHarness.DeliverAsync(host, created)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        ConversationProjectionIndexReadModel index = Index(harness, tenant).ShouldNotBeNull();
        ReadModelEntry<ConversationProjectionIndexReadModel> first = await harness.Store.GetAsync<ConversationProjectionIndexReadModel>(
            DurableTenantAccessHarness.StateStoreName, IndexKey(tenant), TestContext.Current.CancellationToken);
        (await DurableTenantAccessHarness.DeliverAsync(host, created)).ShouldBe(EventStoreDomainEventProcessingResult.Duplicate);
        (await DurableTenantAccessHarness.DeliverAsync(host, created with { MessageId = UniqueIdHelper.GenerateSortableUniqueStringId() }))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        ReadModelEntry<ConversationProjectionIndexReadModel> second = await harness.Store.GetAsync<ConversationProjectionIndexReadModel>(
            DurableTenantAccessHarness.StateStoreName, IndexKey(tenant), TestContext.Current.CancellationToken);

        index.SchemaVersion.ShouldBe(2);
        index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        index.Summaries.ShouldBeEmpty();
        second.ETag.ShouldBe(first.ETag);
    }

    /// <summary>A delivery from another domain, or naming another tenant than its stream, writes no index.</summary>
    /// <param name="defect">The delivery defect.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("other-domain")]
    [InlineData("mismatched-tenant")]
    public async Task ForeignTenantCreatedDeliveryShouldWriteNoIndex(string defect)
    {
        InMemoryReadModelStore store = new();
        ConversationTenantIndexInitializer initializer = new(new ConversationProjectionReadModelWriter(store));
        EventStoreDomainEventContext context = new("system", "tenant-foreign", "message-1", 1, At, "correlation-1")
        {
            Domain = defect == "other-domain" ? "global-administrators" : "tenants",
        };

        await initializer.HandleAsync(
            new TenantCreated(defect == "mismatched-tenant" ? "tenant-other" : "tenant-foreign", "Foreign", null, At),
            context,
            TestContext.Current.CancellationToken);

        store.Count.ShouldBe(0);
    }

    /// <summary>
    /// A retryable index failure throws before the Tenants handler runs, so the tenant never becomes healthy without a
    /// ready index; the redelivery initializes the index and then the tenant.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task IndexFailureShouldKeepTheTenantUnhealthyUntilTheRedeliveryInitializesIt()
    {
        DurableTenantAccessHarness harness = new();
        string tenant = "tenant-index-outage";
        FailingIndexStore failing = new(harness.Store, IndexKey(tenant));
        using ServiceProvider host = harness.CreateHost(store: failing);
        var created = harness.Persist(new TenantCreated(tenant, "Outage", null, At), tenant, 1);
        var member = harness.Persist(new UserAddedToTenant(tenant, Caller, TenantRole.TenantReader), tenant, 2);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => DurableTenantAccessHarness.DeliverAsync(host, created));

        harness.Record(tenant).ShouldBeNull("the Tenants handler never ran, so the tenant cannot authorize");
        Index(harness, tenant).ShouldBeNull();

        failing.Fail = false;
        (await DurableTenantAccessHarness.DeliverAsync(host, created)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantAccessHarness.DeliverAsync(host, member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        Index(harness, tenant).ShouldNotBeNull().Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Ready);
        (await DurableTenantAccessHarness.Access(host).CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            new TenantId(tenant),
            Caller,
            commandTenantId: new TenantId(tenant),
            cancellationToken: TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();
    }

    private static string IndexKey(string tenant) => ConversationProjectionReadModelKeys.TenantIndexKey(new TenantId(tenant));

    private static ConversationProjectionIndexReadModel? Index(DurableTenantAccessHarness harness, string tenant)
        => harness.Store.Snapshot<ConversationProjectionIndexReadModel>(DurableTenantAccessHarness.StateStoreName, IndexKey(tenant));

    /// <summary>Fails every write of one key, standing in for a retryable state-store outage.</summary>
    private sealed class FailingIndexStore(InMemoryReadModelStore inner, string failingKey) : IReadModelStore, IReadModelExpiringStore
    {
        public bool Fail { get; set; } = true;

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.GetAsync<TValue>(storeName, key, cancellationToken);

        public Task<ReadModelEntry<TValue>> GetCompactedAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.GetCompactedAsync<TValue>(storeName, key, cancellationToken);

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
            => Fail && key == failingKey
                ? Task.FromException(new InvalidOperationException("Synthetic index outage."))
                : inner.SaveAsync(storeName, key, value, cancellationToken);

        public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
            where TValue : class
            => Fail && key == failingKey
                ? Task.FromException<bool>(new InvalidOperationException("Synthetic index outage."))
                : inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);

        public Task<bool> TrySaveWithTimeToLiveAsync<TValue>(
            string storeName, string key, TValue value, string etag, TimeSpan timeToLive, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
    }
}
