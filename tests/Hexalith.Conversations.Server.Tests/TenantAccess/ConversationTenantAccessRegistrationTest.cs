// <copyright file="ConversationTenantAccessRegistrationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;

using Microsoft.Extensions.DependencyInjection;

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
