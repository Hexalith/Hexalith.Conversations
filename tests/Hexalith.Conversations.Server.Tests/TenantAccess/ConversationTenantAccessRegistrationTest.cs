// <copyright file="ConversationTenantAccessRegistrationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Verifies Tenants integration registration remains server-boundary scoped.
/// </summary>
public sealed class ConversationTenantAccessRegistrationTest
{
    /// <summary>
    /// The registration adds Tenants projection services, the Conversations projection signal, and the access boundary.
    /// </summary>
    [Fact]
    public void AddConversationTenantAccessShouldRegisterTenantsProjectionAndAccessService()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddConversationTenantAccess();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConversationTenantAccessService>()
            .ShouldBeOfType<ConversationTenantAccessService>();
        provider.GetRequiredService<ITenantProjectionStore>().ShouldNotBeNull();
        provider.GetRequiredService<TenantProjectionEventHandler>().ShouldNotBeNull();

        // F12: the projection signal is registered explicitly so freshness/poisoning checks
        // remain a first-class dependency, not a silent cast against the store.
        provider.GetRequiredService<IConversationTenantProjectionSignal>()
            .ShouldBeOfType<DefaultConversationTenantProjectionSignal>();
    }

    /// <summary>
    /// Calling the shared registration twice keeps the Conversations facade registrations singular.
    /// </summary>
    [Fact]
    public void AddConversationTenantAccessShouldKeepFacadeRegistrationsSingularWhenCalledTwice()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddConversationTenantAccess();
        services.AddConversationTenantAccess();

        services.Count(static descriptor => descriptor.ServiceType == typeof(IConversationTenantAccessService))
            .ShouldBe(1);
        services.Count(static descriptor => descriptor.ServiceType == typeof(IConversationTenantProjectionSignal))
            .ShouldBe(1);
    }

    /// <summary>Configured Tenants registration consumes injected delivery names and its SDK processor updates isolated state.</summary>
    /// <returns>A task for the consumer-state assertions.</returns>
    [Fact]
    public async Task EnrolledTenantConsumerUsesConfiguredDeliveryAndUpdatesItsProjection()
    {
        ServiceCollection services = new();
        services.AddLogging();
        InMemoryReadModelStore readModels = new();
        services.AddSingleton<IReadModelStore>(readModels);
        services.AddConversationTenantAccess(EnrolledConfiguration());
        using ServiceProvider provider = services.BuildServiceProvider();
        EventStoreDomainEventsOptions options = provider.GetRequiredService<IOptions<EventStoreDomainEventsOptions>>().Value;
        options.PubSubName.ShouldBe("enrolled-tenant-events");
        options.TopicName.ShouldBe("enrolled-tenant-topic");
        options.SubscriptionRoute.ShouldBe("/tenants/events");
        TenantCreated created = new("tenant-configured", "Configured tenant", null, DateTimeOffset.UtcNow);
        // The producer's ordinary raw-envelope contract uses a sortable message ULID and system/tenants scope.
        EventStoreDomainEventEnvelope envelope = new(UniqueIdHelper.GenerateSortableUniqueStringId(), created.TenantId, "system", typeof(TenantCreated).FullName!,
            1, created.CreatedAt, UniqueIdHelper.GenerateSortableUniqueStringId(), "json", JsonSerializer.SerializeToUtf8Bytes(created, created.GetType()))
        {
            Domain = "tenants",
            UserId = "enrollment-consumer",
        };
        EventIdentityValidator.ValidateSubscription(envelope.TenantId, envelope.Domain, envelope.AggregateId, envelope.AggregateType,
            envelope.EventTypeName, envelope.MessageId, envelope.CorrelationId, envelope.CausationId, envelope.SequenceNumber);
        (await provider.GetRequiredService<EventStoreDomainEventProcessor>().ProcessAsync(envelope, TestContext.Current.CancellationToken))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await provider.GetRequiredService<ITenantProjectionStore>().GetAsync(created.TenantId, TestContext.Current.CancellationToken))!
            .Name.ShouldBe(created.Name);

        // The enrolled projection is the durable record in the enrolled read-model state store.
        provider.GetRequiredService<ITenantProjectionStore>().ShouldBeOfType<DurableTenantProjectionStore>();
        CheckpointedProjectionRecord<TenantLocalState> record = readModels
            .Snapshot<CheckpointedProjectionRecord<TenantLocalState>>(
                ConversationProjectionStoreOptions.FromConfiguration(EnrolledConfiguration()).StateStoreName,
                DurableTenantProjectionStore.KeyFor(created.TenantId))
            .ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        record.LastContiguousSequence.ShouldBe(1);
        record.State.ShouldNotBeNull().Name.ShouldBe(created.Name);

        // The enrolled history reader is the SDK gateway client on the worker authority transport.
        services.Count(static descriptor => descriptor.ServiceType == typeof(IEventStoreGatewayClient)).ShouldBe(1);
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEventStoreGatewayClient>().ShouldBeOfType<EventStoreGatewayClient>();
    }

    /// <summary>
    /// Missing enrolled pub/sub, topic, declarative, read-model store, gateway origin, or gateway worker authority
    /// configuration refuses before delivery registration.
    /// </summary>
    /// <param name="key">The missing input.</param>
    /// <param name="value">The absent or invalid value.</param>
    [Theory]
    [InlineData("Tenants:PubSubName", null)]
    [InlineData("Tenants:PubSubName", " ")]
    [InlineData("Tenants:TopicName", null)]
    [InlineData("Tenants:TopicName", " ")]
    [InlineData("Dapr:DeclarativeSubscriptions", null)]
    [InlineData("Dapr:DeclarativeSubscriptions", " ")]
    [InlineData("Dapr:DeclarativeSubscriptions", "false")]
    [InlineData("EventStore:Projections:ReadModelStateStoreName", null)]
    [InlineData("EventStore:Projections:ReadModelStateStoreName", " ")]
    [InlineData(ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey, null)]
    [InlineData(ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey, "http://gateway.example/")]
    [InlineData(ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey, "https://gateway.test/api/")]
    [InlineData("EventStore:Authentication:Authority", null)]
    [InlineData("EventStore:Authentication:TokenEndpoint", null)]
    [InlineData("EventStore:Authentication:ClientId", null)]
    [InlineData("EventStore:Authentication:ClientSecret", null)]
    [InlineData("EventStore:Authentication:GrantType", "password")]
    public void MissingEnrolledDeliveryInputsFailBeforeConsumerRegistration(string key, string? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ServiceCollection services = new();
        string message = Should.Throw<InvalidOperationException>(() => services.AddConversationTenantAccess(EnrolledConfiguration(key, value)))
            .Message;
        message.ShouldContain(key);
        message.ShouldNotContain("enrolled-worker-secret");
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(EventStoreDomainEventProcessor));
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(IEventStoreGatewayClient));
    }

    /// <summary>An HTTPS or loopback HTTP gateway origin is accepted and registers the SDK history client.</summary>
    /// <param name="origin">The gateway origin.</param>
    [Theory]
    [InlineData("https://gateway.test/")]
    [InlineData("http://127.0.0.1:5000/")]
    [InlineData("http://localhost:5000")]
    public void LoopbackHttpAndHttpsGatewayOriginsAreAccepted(string origin)
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddConversationTenantAccess(EnrolledConfiguration(ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey, origin));

        services.Single(static descriptor => descriptor.ServiceType == typeof(ConversationEventStoreGatewayRegistration))
            .ImplementationInstance.ShouldBeOfType<ConversationEventStoreGatewayRegistration>().BaseAddress.ShouldBe(new Uri(origin));
        services.Count(static descriptor => descriptor.ServiceType == typeof(IEventStoreGatewayClient)).ShouldBe(1);
    }

    /// <summary>A host-supplied client is kept and still recorded, so a later composition never adds a second one.</summary>
    [Fact]
    public void HostSuppliedGatewayClientIsKeptAndRecorded()
    {
        ServiceCollection services = new();
        services.AddLogging();
        IEventStoreGatewayClient hostClient = new TenantStreamHistoryGateway();
        services.AddSingleton(hostClient);

        services.AddConversationTenantAccess(EnrolledConfiguration());
        ConversationEventStoreGatewayRegistration.Add(services, new Uri("https://gateway.test/"), EnrolledConfiguration(), keepHostClient: false);

        services.Count(static descriptor => descriptor.ServiceType == typeof(ConversationEventStoreGatewayRegistration)).ShouldBe(1);
        services.Single(static descriptor => descriptor.ServiceType == typeof(IEventStoreGatewayClient)).ImplementationInstance.ShouldBeSameAs(hostClient);
    }

    /// <summary>A keyed gateway client never stands in for the unkeyed history reader.</summary>
    [Fact]
    public void KeyedGatewayClientDoesNotReplaceTheHistoryReader()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<IEventStoreGatewayClient>("other-purpose", new TenantStreamHistoryGateway());

        services.AddConversationTenantAccess(EnrolledConfiguration());

        services.Count(static descriptor => descriptor.ServiceType == typeof(IEventStoreGatewayClient) && !descriptor.IsKeyedService).ShouldBe(1);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEventStoreGatewayClient>().ShouldBeOfType<EventStoreGatewayClient>();
    }

    private static IConfiguration EnrolledConfiguration(string? key = null, string? value = null)
    {
        Dictionary<string, string?> values = new()
        {
            ["Tenants:PubSubName"] = "enrolled-tenant-events",
            ["Tenants:TopicName"] = "enrolled-tenant-topic",
            ["Dapr:DeclarativeSubscriptions"] = "true",
            ["EventStore:Projections:ReadModelStateStoreName"] = "enrolled-read-model-state",
            [ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey] = "https://gateway.test/",
            ["EventStore:Authentication:Authority"] = "https://authority.test/",
            ["EventStore:Authentication:TokenEndpoint"] = "https://authority.test/connect/token",
            ["EventStore:Authentication:ClientId"] = "enrolled-worker",
            ["EventStore:Authentication:ClientSecret"] = "enrolled-worker-secret",
        };
        if (key is not null) values[key] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    /// <summary>
    /// F20: omitting <see cref="ConversationTenantAccessServiceCollectionExtensions.AddConversationTenantAccess"/>
    /// must leave the access service unresolvable so protected routes cannot fall back to a permissive default.
    /// </summary>
    [Fact]
    public void OmittingRegistrationShouldLeaveAccessServiceUnresolvable()
    {
        ServiceCollection services = new();
        services.AddLogging();

        // Intentionally NOT calling AddConversationTenantAccess.
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IConversationTenantAccessService>().ShouldBeNull();
        provider.GetService<IConversationTenantProjectionSignal>().ShouldBeNull();

        // Requesting the access service via GetRequiredService must throw, never fall back.
        Should.Throw<InvalidOperationException>(() =>
            provider.GetRequiredService<IConversationTenantAccessService>());
    }
}
