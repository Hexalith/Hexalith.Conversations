// <copyright file="Story16_1LiveFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Dapr.Client;

using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.Configuration;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// Two live Dapr sidecars of one Conversations application identity over one Redis read-model state store:
/// the deployed shape of two replicas that share the durable tenant projection.
/// </summary>
public sealed class Story16_1LiveFixture : IAsyncLifetime
{
    private const int RedisPort = 6379;
    private readonly string _componentsDirectory = Path.Combine(Path.GetTempPath(), $"dapr-story161-{Guid.NewGuid():N}");
    private readonly Lock _gate = new();
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="Story16_1LiveFixture"/> class.
    /// </summary>
    public Story16_1LiveFixture()
    {
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tenants:PubSubName"] = $"story161-tenant-events-{Guid.NewGuid():N}",
                ["Tenants:TopicName"] = "tenants.events",
                ["Dapr:DeclarativeSubscriptions"] = "true",
                [ConversationProjectionStoreOptions.ConfigurationKey] = $"story161-tenant-state-{Guid.NewGuid():N}",

                // Enrollment requires the gateway origin and worker authority; every replica here supplies its own
                // history client, which the enrolled registration keeps, so neither is ever contacted.
                [ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey] = "https://gateway.test/",
                ["EventStore:Authentication:Authority"] = "https://authority.test/",
                ["EventStore:Authentication:TokenEndpoint"] = "https://authority.test/connect/token",
                ["EventStore:Authentication:ClientId"] = "story161-worker",
                ["EventStore:Authentication:ClientSecret"] = $"story161-{Guid.NewGuid():N}",
            })
            .Build();
        ReplicaA = new Story16_1LiveSidecar(AppId, _componentsDirectory);
        ReplicaB = new Story16_1LiveSidecar(AppId, _componentsDirectory);
    }

    /// <summary>Gets the shared application identity of both replicas.</summary>
    public string AppId { get; } = $"conversations-story161-{Guid.NewGuid():N}";

    /// <summary>Gets the enrolled host configuration both replicas use.</summary>
    public IConfiguration Configuration { get; }

    /// <summary>Gets the enrolled read-model state-store component name.</summary>
    public string StateStoreName => ConversationProjectionStoreOptions.FromConfiguration(Configuration).StateStoreName;

    /// <summary>Gets the first replica's sidecar.</summary>
    internal Story16_1LiveSidecar ReplicaA { get; }

    /// <summary>Gets the second replica's sidecar.</summary>
    internal Story16_1LiveSidecar ReplicaB { get; }

    /// <summary>Gets the authoritative Tenants stream.</summary>
    internal Story16_1TenantHistory History { get; } = new();

    /// <summary>Records a state key the run created so disposal removes it.</summary>
    /// <param name="key">The state key.</param>
    internal void Track(string key)
    {
        lock (_gate)
        {
            _ = _keys.Add(key);
        }
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _ = Directory.CreateDirectory(_componentsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(_componentsDirectory, "tenant-projection-state.yaml"),
            $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: {StateStoreName}
            spec:
              type: state.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "localhost:{RedisPort}"
                - name: redisPassword
                  value: ""
            scopes:
              - {AppId}
            """).ConfigureAwait(false);
        await ReplicaA.StartAsync().ConfigureAwait(false);
        await ReplicaB.StartAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            string[] keys;
            lock (_gate)
            {
                keys = [.. _keys];
            }

            if (keys.Length > 0)
            {
                using DaprClient client = ReplicaB.CreateClient();
                foreach (string key in keys)
                {
                    await client.DeleteStateAsync(StateStoreName, key).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception) when (exception is Dapr.DaprException or InvalidOperationException or HttpRequestException)
        {
            // Best-effort cleanup of disposable keys under this run's unique application identity.
        }
        finally
        {
            try
            {
                await ReplicaA.DisposeAsync().ConfigureAwait(false);
                await ReplicaB.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                History.Dispose();
            }
            if (Directory.Exists(_componentsDirectory))
            {
                Directory.Delete(_componentsDirectory, recursive: true);
            }
        }
    }
}
