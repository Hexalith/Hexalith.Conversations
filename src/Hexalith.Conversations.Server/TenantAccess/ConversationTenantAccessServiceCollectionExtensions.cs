// <copyright file="ConversationTenantAccessServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.TenantAccess;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Registers Conversations tenant access services at the server/application boundary.
/// </summary>
public static class ConversationTenantAccessServiceCollectionExtensions
{
    /// <summary>
    /// The configuration key of the EventStore gateway origin the enrolled host reads authoritative Tenants history from.
    /// </summary>
    public const string GatewayBaseAddressKey = "Hexalith:Conversations:TenantAccess:GatewayBaseAddress";

    /// <summary>
    /// Adds the local Tenants projection client and Conversations tenant access boundary.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationTenantAccess(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddTenantAccess<
            IConversationTenantProjectionSignal,
            DefaultConversationTenantProjectionSignal,
            IConversationTenantAccessService,
            ConversationTenantAccessService>(static services =>
            {
                services.AddConversationTenantIndexInitializer(ConversationProjectionStoreOptions.Legacy);
                services.AddHexalithTenants();
            });
    }

    /// <summary>
    /// Registers the Tenants SDK consumer with required enrolled delivery inputs, the durable tenant projection over
    /// the enrolled read-model state store, and the EventStore gateway client its history reads use.
    /// </summary>
    /// <remarks>
    /// The gateway client is registered once on the worker authority transport (<c>EventStore:Authentication</c>);
    /// the opt-in deletion runtime reuses that registration when it targets the same origin. A gateway client the host
    /// registered itself is kept. Startup refuses when the gateway origin or the worker authority is missing.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The enrolled host configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationTenantAccess(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        string? pubSub = configuration["Tenants:PubSubName"];
        string? topic = configuration["Tenants:TopicName"];
        if (string.IsNullOrWhiteSpace(pubSub) || string.IsNullOrWhiteSpace(topic))
        {
            throw new InvalidOperationException("Enrollment requires 'Tenants:PubSubName' and 'Tenants:TopicName'.");
        }

        if (!bool.TryParse(configuration["Dapr:DeclarativeSubscriptions"], out bool declarative) || !declarative)
        {
            throw new InvalidOperationException("Enrollment requires 'Dapr:DeclarativeSubscriptions=true'.");
        }

        // The enrolled host persists the Tenants projection in its existing read-model state store, so every
        // replica and every restart reads the same checkpointed state, health, and checkpoint.
        string readModelStateStore = ConversationProjectionStoreOptions.FromConfiguration(configuration).StateStoreName;

        // Older-delivery verification and gap recovery read authoritative Tenants history through the EventStore
        // gateway on the worker authority transport; without it a gapped tenant could never recover, so refuse.
        if (!ConversationEventStoreGatewayRegistration.TryParse(configuration[GatewayBaseAddressKey], out Uri gateway))
        {
            throw new InvalidOperationException(
                $"Enrollment requires '{GatewayBaseAddressKey}' (an HTTPS, or loopback HTTP, gateway origin).");
        }

        IReadOnlyList<string> authority = ConversationEventStoreGatewayRegistration.AuthorityFailures(configuration);
        if (authority.Count > 0)
        {
            throw new InvalidOperationException("Enrollment requires the gateway worker authority: " + string.Join(", ", authority) + ".");
        }

        ConversationEventStoreGatewayRegistration.Add(services, gateway, configuration, keepHostClient: true);
        return services.AddTenantAccess<
            IConversationTenantProjectionSignal,
            DefaultConversationTenantProjectionSignal,
            IConversationTenantAccessService,
            ConversationTenantAccessService>(collection =>
            {
                // Before every Tenants handler: the processor dispatches TenantCreated sequentially in registration
                // order, so an in-order TenantCreated makes the index Ready before the tenant becomes healthy. Out of
                // order, Tenants gap recovery replays TenantCreated from history without this handler; the index then
                // stays missing until TenantCreated is delivered here (see ConversationTenantIndexInitializer).
                collection.AddConversationTenantIndexInitializer(new ConversationProjectionStoreOptions(readModelStateStore));
                collection.AddHexalithTenantsDurableProjection(readModelStateStore);
                collection.AddHexalithTenants(options =>
                {
                    options.PubSubName = pubSub;
                    options.TopicName = topic;
                });
            });
    }

    /// <summary>
    /// Registers the <c>TenantCreated</c> handler that initializes the tenant's Conversations index (AD-7).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="fallbackStoreOptions">The component binding used when the host registered no index writer.</param>
    /// <remarks>
    /// The handler asks the host's <see cref="ConversationProjectionReadModelWriter"/>, the only author of index
    /// transitions; a composition without the query boundary gets one over the same read-model store binding.
    /// </remarks>
    private static void AddConversationTenantIndexInitializer(
        this IServiceCollection services,
        ConversationProjectionStoreOptions fallbackStoreOptions)
    {
        services.TryAddScoped(provider => new ConversationTenantIndexInitializer(
            provider.GetService<ConversationProjectionReadModelWriter>()
                ?? new ConversationProjectionReadModelWriter(
                    provider.GetService<ConversationProjectionStoreOptions>() ?? fallbackStoreOptions,
                    provider.GetRequiredService<IReadModelStore>(),
                    provider.GetService<ILogger<ConversationProjectionReadModelWriter>>())));
        services.AddEventStoreDomainEventHandler<TenantCreated, ConversationTenantIndexInitializer>();
    }
}
