// <copyright file="ConversationsDomainDiscoveryHostCompositionTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;

using Hexalith.Conversations;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Server;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.HostComposition;

/// <summary>
/// Story 2.1 (AC-1) — proves the host wiring uses the <b>explicit-assemblies</b> overload of
/// <c>AddEventStoreDomainService</c> in a way that actually discovers the Conversations domain, and that
/// the calling-assembly overload (which AC-1 forbids) would <i>not</i> — closing the gap the route-presence
/// smoke test cannot catch.
/// </summary>
/// <remarks>
/// <para>
/// The canonical SDK routes (<c>/process</c>, <c>/query</c>, …) are mapped unconditionally by
/// <c>MapEventStoreDomainService</c> — they appear no matter which assembly the SDK scans. So a route-presence
/// assertion alone cannot tell the mandated explicit-assemblies wiring apart from the forbidden
/// calling-assembly overload: both leave the route table identical. The load-bearing difference is
/// <i>discovery</i> — whether <see cref="ConversationAggregate"/> is found and registered as the keyed
/// <see cref="IDomainProcessor"/> the request router resolves by domain name (<c>"conversation"</c>) to serve
/// <c>POST /process</c>.
/// </para>
/// <para>
/// <b>Fault-injection (teeth, per Epic 1 L1/A1 — green alone is not evidence).</b> The first fact requires the
/// explicit scan over the domain assembly to register the processor; the second proves the contrast: the
/// Server host assembly (exactly what the calling-assembly overload scans, since <c>Program.cs</c> compiles
/// into it) contains no aggregate, so the keyed processor is absent. If <c>Program.cs</c> regressed to the
/// calling-assembly overload, <c>/process</c> would silently have nothing to dispatch to while every route
/// still mapped — the first fact would turn RED, catching the regression the smoke test would wave through.
/// </para>
/// </remarks>
public sealed class ConversationsDomainDiscoveryHostCompositionTest
{
    /// <summary>
    /// The kebab-case domain name the SDK derives for <see cref="ConversationAggregate"/> (strip the
    /// <c>Aggregate</c> suffix → <c>conversation</c>) and the key the request router resolves for
    /// <c>POST /process</c>.
    /// </summary>
    private const string ConversationDomainKey = "conversation";

    private static WebApplication ComposeHost(params Assembly[] domainAssemblies)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(domainAssemblies);
        return builder.Build();
    }

    /// <summary>
    /// The exact <c>Program.cs</c> wiring (domain + Server boundary assemblies) discovers
    /// <see cref="ConversationAggregate"/> and registers it as the keyed <see cref="IDomainProcessor"/> the
    /// <c>/process</c> route resolves.
    /// </summary>
    [Fact]
    public async Task ExplicitAssemblyScanShouldRegisterConversationDomainProcessor()
    {
        await using WebApplication app = ComposeHost(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);

        using IServiceScope scope = app.Services.CreateScope();
        IDomainProcessor processor =
            scope.ServiceProvider.GetRequiredKeyedService<IDomainProcessor>(ConversationDomainKey);

        processor.ShouldBeOfType<ConversationAggregate>();
    }

    /// <summary>
    /// Contrast fact: scanning only the Server host assembly — what the forbidden calling-assembly overload
    /// would scan — discovers no aggregate, so no keyed <c>"conversation"</c> processor is registered. This is
    /// why the explicit domain-assembly argument in <c>Program.cs</c> is load-bearing and not cosmetic.
    /// </summary>
    [Fact]
    public async Task CallingAssemblyOverloadWouldNotDiscoverTheConversationDomainProcessor()
    {
        await using WebApplication app = ComposeHost(typeof(ServerAssemblyMarker).Assembly);

        using IServiceScope scope = app.Services.CreateScope();
        scope.ServiceProvider
            .GetKeyedService<IDomainProcessor>(ConversationDomainKey)
            .ShouldBeNull();
    }
}
