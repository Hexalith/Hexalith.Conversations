// <copyright file="DurableTenantAccessHarness.cs" company="ITANEO">
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
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Client.Projections;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// One shared read-model state store and authoritative Tenants stream behind any number of enrolled
/// Conversations hosts (replicas or restarted processes).
/// </summary>
internal sealed class DurableTenantAccessHarness
{
    private readonly List<EventStoreDomainEventEnvelope> _history = [];

    /// <summary>Gets the shared persisted state.</summary>
    public InMemoryReadModelStore Store { get; } = new();

    /// <summary>Gets the shared authoritative tenant stream.</summary>
    public FakeEventStoreGatewayClient Gateway { get; } = new();

    /// <summary>Gets the enrolled host configuration.</summary>
    public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tenants:PubSubName"] = "enrolled-tenant-events",
            ["Tenants:TopicName"] = "enrolled-tenant-topic",
            ["Dapr:DeclarativeSubscriptions"] = "true",
            [ConversationProjectionStoreOptions.ConfigurationKey] = "conversations-tenant-access-state",
            [ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey] = "https://gateway.test/",
            ["EventStore:Authentication:Authority"] = "https://authority.test/",
            ["EventStore:Authentication:TokenEndpoint"] = "https://authority.test/connect/token",
            ["EventStore:Authentication:ClientId"] = "tenant-access-worker",
            ["EventStore:Authentication:ClientSecret"] = "tenant-access-worker-secret",
        })
        .Build();

    /// <summary>Gets the enrolled read-model state-store component name, read from the enrolled configuration.</summary>
    public static string StateStoreName => ConversationProjectionStoreOptions.FromConfiguration(Configuration).StateStoreName;

    /// <summary>Creates a tenant event delivery envelope with the producer's raw-envelope shape.</summary>
    /// <typeparam name="TEvent">The tenant event type.</typeparam>
    /// <param name="event">The tenant event.</param>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="sequence">The tenant stream sequence.</param>
    /// <returns>The envelope.</returns>
    public static EventStoreDomainEventEnvelope Envelope<TEvent>(TEvent @event, string tenantId, long sequence)
        where TEvent : IEventPayload
        => new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            tenantId,
            "system",
            @event.GetType().FullName!,
            sequence,
            new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero).AddMinutes(sequence),
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "json",
            JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType()))
        {
            Domain = "tenants",
            UserId = "tenants-producer",
        };

    /// <summary>Creates an enrolled Conversations host over the shared state.</summary>
    /// <param name="withHistory">Whether the host can read authoritative Tenants history.</param>
    /// <param name="store">An optional store overriding the shared one.</param>
    /// <returns>The host services.</returns>
    public ServiceProvider CreateHost(bool withHistory = true, IReadModelStore? store = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(store ?? Store);

        // A host-supplied gateway client is kept by the enrolled registration; an unreachable one models a host
        // whose history reads fail, without any network call.
        services.AddSingleton<IEventStoreGatewayClient>(withHistory ? Gateway : new FakeEventStoreGatewayClient().ConfigureStreamReadUnavailable("platform:tenants"));

        services.AddConversationTenantAccess(Configuration);
        return services.BuildServiceProvider();
    }

    /// <summary>Persists an event in authoritative history and returns its delivery envelope.</summary>
    /// <typeparam name="TEvent">The tenant event type.</typeparam>
    /// <param name="event">The tenant event.</param>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="sequence">The tenant stream sequence.</param>
    /// <returns>The envelope.</returns>
    public EventStoreDomainEventEnvelope Persist<TEvent>(TEvent @event, string tenantId, long sequence)
        where TEvent : IEventPayload
    {
        EventStoreDomainEventEnvelope envelope = Envelope(@event, tenantId, sequence);
        _history.Add(envelope);
        return envelope;
    }

    /// <summary>Arranges one bounded authoritative history response on the shared SDK fake.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="fromSequence">The exclusive lower bound expected from the production reader.</param>
    /// <param name="toSequence">The inclusive recovery or duplicate-verification bound.</param>
    public void ConfigureHistory(string tenantId, long fromSequence, long toSequence)
    {
        EventStoreDomainEventEnvelope[] stream = [.. _history.Where(envelope => envelope.AggregateId == tenantId).OrderBy(envelope => envelope.SequenceNumber)];
        StreamReadEvent[] events = [.. stream.Where(envelope => envelope.SequenceNumber > fromSequence && envelope.SequenceNumber <= toSequence)
            .Select(envelope => new StreamReadEvent(envelope.SequenceNumber, envelope.EventTypeName, envelope.Payload,
                envelope.SerializationFormat, 1, envelope.MessageId, envelope.CorrelationId, envelope.CausationId, envelope.Timestamp, envelope.UserId))];
        Gateway.ConfigureStreamReadSuccess(new StreamReadPage("system", "tenants", tenantId, events,
            new StreamReadMetadata(fromSequence, toSequence, events.LastOrDefault()?.SequenceNumber,
                stream.LastOrDefault()?.SequenceNumber ?? 0, events.Length, false, null)));
    }

    /// <summary>Reads the persisted record of a tenant directly from the state store.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <returns>The persisted record, when any.</returns>
    public CheckpointedProjectionRecord<TenantLocalState>? Record(string tenantId)
        => Store.Snapshot<CheckpointedProjectionRecord<TenantLocalState>>(StateStoreName, DurableTenantProjectionStore.KeyFor(tenantId));

    /// <summary>Delivers an envelope through the production consumer processor of a host.</summary>
    /// <param name="host">The host.</param>
    /// <param name="envelope">The envelope.</param>
    /// <returns>The processing result.</returns>
    public static Task<EventStoreDomainEventProcessingResult> DeliverAsync(ServiceProvider host, EventStoreDomainEventEnvelope envelope)
        => host.GetRequiredService<EventStoreDomainEventProcessor>().ProcessAsync(envelope, TestContext.Current.CancellationToken);

    /// <summary>Resolves the production access service of a host.</summary>
    /// <param name="host">The host.</param>
    /// <returns>The access service.</returns>
    public static IConversationTenantAccessService Access(ServiceProvider host)
        => host.GetRequiredService<IConversationTenantAccessService>();
}
