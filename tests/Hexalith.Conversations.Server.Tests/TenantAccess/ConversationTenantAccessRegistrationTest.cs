// <copyright file="ConversationTenantAccessRegistrationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Subscription;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Verifies Tenants integration registration remains server-boundary scoped.
/// </summary>
public sealed class ConversationTenantAccessRegistrationTest
{
    /// <summary>
    /// The registration adds Tenants projection services and the Conversations tenant access boundary.
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
        provider.GetRequiredService<TenantEventProcessor>().ShouldNotBeNull();
    }
}
