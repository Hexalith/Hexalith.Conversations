// <copyright file="ConversationTenantAccessServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Tenants.Client.Registration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.AddHexalithTenants();

        // Register a Conversations-owned signal that delegates to a signal-capable
        // ITenantProjectionStore when one is present, and otherwise emits a one-shot
        // warning so the absence of freshness/poisoning detection is visible to operators
        // (F12: optional-interface fail-open is now an explicit, logged dependency).
        services.TryAddScoped<IConversationTenantProjectionSignal, DefaultConversationTenantProjectionSignal>();
        services.TryAddScoped<IConversationTenantAccessService, ConversationTenantAccessService>();
        return services;
    }
}
