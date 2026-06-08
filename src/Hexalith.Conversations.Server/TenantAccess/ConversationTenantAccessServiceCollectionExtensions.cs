// <copyright file="ConversationTenantAccessServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.TenantAccess;
using Hexalith.Tenants.Client.Registration;

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
}
