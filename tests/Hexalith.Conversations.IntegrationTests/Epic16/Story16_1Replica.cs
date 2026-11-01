// <copyright file="Story16_1Replica.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Dapr.Client;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// One Conversations replica process: the production enrolled tenant-access registration over its own live
/// sidecar, with process-local marker and consumer state that a restart discards.
/// </summary>
internal sealed class Story16_1Replica : IDisposable
{
    private const int MaxDeliveries = 20;
    private readonly DaprClient _client;
    private readonly ServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="Story16_1Replica"/> class.
    /// </summary>
    /// <param name="fixture">The live fixture.</param>
    /// <param name="sidecar">The replica's sidecar.</param>
    /// <param name="withHistory">Whether the replica can read authoritative Tenants history.</param>
    /// <param name="rendezvous">An optional rendezvous holding first writes so two replicas race through Redis.</param>
    public Story16_1Replica(Story16_1LiveFixture fixture, Story16_1LiveSidecar sidecar, bool withHistory = true, Story16_1FirstWriteRendezvous? rendezvous = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(sidecar);
        _client = sidecar.CreateClient();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(_client);

        // The authoritative stream double; an unreachable one models a replica whose history reads fail.
        services.AddSingleton<IEventStoreGatewayClient>(withHistory ? fixture.History : new Story16_1TenantHistory { Unavailable = true });

        // The production enrolled registration: it binds the durable projection to the Dapr-backed SDK store.
        services.AddConversationTenantAccess(fixture.Configuration);
        if (rendezvous is not null)
        {
            services.Replace(ServiceDescriptor.Singleton<IReadModelStore>(provider =>
                new Story16_1FirstWriteRendezvousStore(provider.GetRequiredService<DaprReadModelStore>(), rendezvous)));
        }

        _services = services.BuildServiceProvider();
        StateStoreName = fixture.StateStoreName;
    }

    /// <summary>Gets the enrolled read-model state-store component name.</summary>
    public string StateStoreName { get; }

    /// <summary>Gets the replica services.</summary>
    public IServiceProvider Services => _services;

    /// <summary>Delivers once through the production consumer processor.</summary>
    /// <param name="envelope">The envelope.</param>
    /// <returns>The processing result.</returns>
    public Task<EventStoreDomainEventProcessingResult> DeliverOnceAsync(EventStoreDomainEventEnvelope envelope)
        => _services.GetRequiredService<EventStoreDomainEventProcessor>().ProcessAsync(envelope, TestContext.Current.CancellationToken);

    /// <summary>Delivers until acknowledged, redelivering retryable failures as Dapr would.</summary>
    /// <param name="envelope">The envelope.</param>
    /// <returns>The acknowledged processing result and the number of deliveries it took.</returns>
    public async Task<(EventStoreDomainEventProcessingResult Result, int Deliveries)> DeliverUntilAcknowledgedAsync(EventStoreDomainEventEnvelope envelope)
    {
        for (int delivery = 1; ; delivery++)
        {
            try
            {
                return (await DeliverOnceAsync(envelope).ConfigureAwait(false), delivery);
            }
            catch (CheckpointedProjectionUnavailableException) when (delivery < MaxDeliveries)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(25 * delivery), TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Evaluates tenant access through the production access service.</summary>
    /// <param name="requirement">The requirement.</param>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="caller">The caller principal.</param>
    /// <returns>The decision.</returns>
    public ValueTask<ConversationTenantAccessDecision> CheckAsync(ConversationTenantAccessRequirement requirement, string tenantId, string caller)
        => _services.GetRequiredService<IConversationTenantAccessService>().CheckAccessAsync(
            requirement,
            new TenantId(tenantId),
            caller,
            commandTenantId: new TenantId(tenantId),
            cancellationToken: TestContext.Current.CancellationToken);

    /// <summary>Reads the tenant snapshot through the replica's durable store.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <returns>The snapshot.</returns>
    public Task<TenantProjectionSnapshot?> SnapshotAsync(string tenantId)
        => _services.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(tenantId, TestContext.Current.CancellationToken);

    /// <summary>Reads the persisted record exactly as the Dapr state store holds it.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <returns>The persisted JSON document.</returns>
    public async Task<JsonElement> ReadPersistedAsync(string tenantId)
        => await _client
            .GetStateAsync<JsonElement>(StateStoreName, DurableTenantProjectionStore.KeyFor(tenantId), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Dispose()
    {
        _services.Dispose();
        _client.Dispose();
    }
}
