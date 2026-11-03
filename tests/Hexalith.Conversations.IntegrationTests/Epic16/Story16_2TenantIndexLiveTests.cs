// <copyright file="Story16_2TenantIndexLiveTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// Story 16.2 live evidence over a live Dapr sidecar and the shared Redis read-model state store: the tenant index
/// lifecycle written by <c>TenantCreated</c> drives truthful list reads (AD-7).
/// </summary>
/// <remarks>
/// Tenant events enter through the production consumer processor, the conversation through the production named
/// projection handler, and reads through the production query handler, all over the Dapr-backed SDK store.
/// </remarks>
[Collection(Story16_1LiveCollection.Name)]
public sealed class Story16_2TenantIndexLiveTests(Story16_1LiveFixture fixture)
{
    private const string Caller = "user-index-live";
    private static readonly DateTimeOffset At = DateTimeOffset.UtcNow;
    private static readonly Type Keys = typeof(ConversationProjectionReadModelWriter).Assembly
        .GetType("Hexalith.Conversations.Server.Projections.ConversationProjectionReadModelKeys", throwOnError: true)!;

    /// <summary><c>TenantCreated</c> initializes the index, so an authorized tenant without conversations lists Current, empty.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task TenantCreatedShouldListAnInitializedTenantAsCurrentEmpty()
    {
        string tenant = NewTenant();
        using LiveReplica replica = new(fixture);
        await replica.InitializeMemberTenantAsync(tenant);

        ConversationListResult list = await replica.ListAsync(tenant);

        list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Current);
        list.Conversations.ShouldBeEmpty();
        JsonElement index = await replica.ReadPersistedAsync(TenantIndexKey(tenant));
        index.GetProperty("schemaVersion").GetInt32().ShouldBe(2);
        index.GetProperty("lifecycle").GetInt32().ShouldBe((int)ConversationIndexLifecycleWire.Ready);
    }

    /// <summary>An erased index of an authorized tenant lists as Unavailable, never as a Current empty page.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ErasedIndexShouldListAsUnavailable()
    {
        string tenant = NewTenant();
        using LiveReplica replica = new(fixture);
        await replica.InitializeMemberTenantAsync(tenant);
        await replica.DeleteAsync(TenantIndexKey(tenant));

        ConversationListResult list = await replica.ListAsync(tenant);

        list.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Unavailable);
        list.Conversations.ShouldBeEmpty();
    }

    /// <summary>The first conversation of an initialized tenant lists as Current and keeps the index Ready.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task FirstConversationOfAnInitializedTenantShouldListAsCurrent()
    {
        string tenant = NewTenant();
        ConversationId conversation = new("conversation-index-live");

        // The dispatch ledger key does not include the tenant, so a fixed identity would collide with a leftover or
        // concurrent run's ledger and fail its identity check.
        string dispatchId = $"dispatch-index-live-{Guid.NewGuid():N}";
        using LiveReplica replica = new(fixture);
        await replica.InitializeMemberTenantAsync(tenant);
        Track(tenant, conversation, dispatchId);

        DomainProjectionHandlerResult projected = await replica.ProjectAsync(tenant, conversation, dispatchId);
        ConversationListResult list = await replica.ListAsync(tenant);

        projected.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Current);
        list.Conversations.ShouldHaveSingleItem().ConversationId.ShouldBe(conversation);
        JsonElement index = await replica.ReadPersistedAsync(TenantIndexKey(tenant));
        index.GetProperty("lifecycle").GetInt32().ShouldBe((int)ConversationIndexLifecycleWire.Ready);
        index.GetProperty("watermarks").GetProperty(conversation.Value).GetInt64().ShouldBe(1);
    }

    private static string Key(string method, params object[] arguments)
        => (string)Keys.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .Invoke(null, arguments)!;

    /// <summary>Derives a tenant's index key through the production key builder.</summary>
    /// <param name="tenant">The tenant identifier.</param>
    /// <returns>The tenant index state key.</returns>
    internal static string TenantIndexKey(string tenant) => Key("TenantIndexKey", new TenantId(tenant));

    private string NewTenant()
    {
        string tenant = $"tenant-index-live-{Guid.NewGuid():N}";
        fixture.Track(DurableTenantProjectionStore.KeyFor(tenant));
        fixture.Track(TenantIndexKey(tenant));
        return tenant;
    }

    private void Track(string tenant, ConversationId conversation, string dispatchId)
    {
        TenantId tenantId = new(tenant);
        fixture.Track(Key("ConversationKey", tenantId, conversation));
        fixture.Track(Key("DispatchLedgerKey", dispatchId));
        fixture.Track(Key("ResolutionMetadataKey", tenantId, conversation));
        fixture.Track(Key("ResolutionProofKey", tenantId, conversation));
        fixture.Track(Key("ResolutionFenceKey", tenantId, conversation));
        fixture.Track(Key("ResolutionSelectionKey", tenantId, conversation));
    }

    /// <summary>The persisted numeric lifecycle values of the version 2 index.</summary>
    private enum ConversationIndexLifecycleWire
    {
        Rebuilding = 1,
        Ready = 2,
    }

    /// <summary>One Conversations replica composed like the enrolled host over its own live sidecar.</summary>
    private sealed class LiveReplica : IDisposable
    {
        private readonly DaprClient _client;
        private readonly ServiceProvider _services;
        private readonly string _stateStoreName;
        private long _sequence;

        public LiveReplica(Story16_1LiveFixture fixture)
        {
            _client = fixture.ReplicaA.CreateClient();
            _stateStoreName = fixture.StateStoreName;
            IConfiguration configuration = new ConfigurationBuilder()
                .AddConfiguration(fixture.Configuration)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EventStore:DataProtection:StateStoreName"] = fixture.StateStoreName,
                    ["EventStore:DataProtection:PersistToStateStore"] = "false",
                })
                .Build();
            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton(_client);
            services.AddSingleton<IEventStoreGatewayClient>(fixture.History.Client);
            services.AddConversationTenantAccess(configuration);
            services.AddConversationQueries(configuration);
            _services = services.BuildServiceProvider();
            History = fixture.History;
        }

        private Story16_1TenantHistory History { get; }

        public async Task InitializeMemberTenantAsync(string tenant)
        {
            _sequence = 0;
            await DeliverAsync(new TenantCreated(tenant, "Index live", null, At), tenant);
            await DeliverAsync(new UserAddedToTenant(tenant, Caller, TenantRole.TenantContributor), tenant);
        }

        public async Task<ConversationListResult> ListAsync(string tenant)
        {
            using IServiceScope scope = _services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ConversationQueryHandler>().ListAsync(
                new ListConversationsQuery(SchemaVersion.Current, new TenantId(tenant), Caller, "correlation-index-live"),
                TestContext.Current.CancellationToken);
        }

        public Task<DomainProjectionHandlerResult> ProjectAsync(string tenant, ConversationId conversation, string dispatchId)
        {
            IReadModelStore store = _services.GetRequiredService<IReadModelStore>();
            ConversationAsyncProjectionHandler handler = new(
                _services.GetRequiredService<ConversationProjectionMaterializer>(),
                _services.GetRequiredService<ConversationProjectionReadModelWriter>(),
                store);
            ConversationCreated created = new(
                new ConversationEventMetadata(
                    SchemaVersion.Current,
                    $"event-created-{conversation.Value}",
                    ConversationEventType.ConversationCreated,
                    new TenantId(tenant),
                    conversation,
                    "correlation-index-live",
                    At,
                    new PartyId("party-index-live")),
                Label: "Index live conversation");
            ProjectionRequest request = new(
                tenant,
                ConversationProjectionHandler.ConversationDomain,
                conversation.Value,
                [new ProjectionEventDto(
                    nameof(ConversationCreated),
                    JsonSerializer.SerializeToUtf8Bytes(created, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    "json",
                    1,
                    At,
                    "correlation-index-live",
                    MessageId: $"message-{conversation.Value}")]);
            return handler.ProjectAsync(request, dispatchId, TestContext.Current.CancellationToken);
        }

        public async Task<JsonElement> ReadPersistedAsync(string key)
            => await _client.GetStateAsync<JsonElement>(_stateStoreName, key, cancellationToken: TestContext.Current.CancellationToken);

        public Task DeleteAsync(string key)
            => _client.DeleteStateAsync(_stateStoreName, key, cancellationToken: TestContext.Current.CancellationToken);

        public void Dispose()
        {
            _services.Dispose();
            _client.Dispose();
        }

        private async Task DeliverAsync<TEvent>(TEvent @event, string tenant)
            where TEvent : IEventPayload
        {
            EventStoreDomainEventEnvelope envelope = new(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                tenant,
                "system",
                @event.GetType().FullName!,
                ++_sequence,
                At.AddSeconds(_sequence),
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "json",
                JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType()))
            {
                Domain = "tenants",
                UserId = "tenants-producer",
            };
            History.Append(envelope);
            (await _services.GetRequiredService<EventStoreDomainEventProcessor>()
                    .ProcessAsync(envelope, TestContext.Current.CancellationToken))
                .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        }
    }
}
