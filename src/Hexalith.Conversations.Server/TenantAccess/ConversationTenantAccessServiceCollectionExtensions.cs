// <copyright file="ConversationTenantAccessServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.TenantAccess;
using Hexalith.Tenants.Client.Registration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Registers Conversations tenant access services at the server/application boundary.
/// </summary>
public static class ConversationTenantAccessServiceCollectionExtensions
{
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
            ConversationTenantAccessService>(static services => services.AddHexalithTenants());
    }

    /// <summary>Registers the Tenants SDK consumer with required enrolled delivery inputs.</summary>
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

        return services.AddTenantAccess<
            IConversationTenantProjectionSignal,
            DefaultConversationTenantProjectionSignal,
            IConversationTenantAccessService,
            ConversationTenantAccessService>(collection => collection.AddHexalithTenants(options =>
            {
                options.PubSubName = pubSub;
                options.TopicName = topic;
            }));
    }

}
