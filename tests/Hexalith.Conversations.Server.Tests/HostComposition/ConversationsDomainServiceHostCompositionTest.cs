// <copyright file="ConversationsDomainServiceHostCompositionTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations;
using Hexalith.Conversations.Server;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.HostComposition;

/// <summary>
/// Story 2.1 (AC-2) — proves the shared two-line EventStore domain-service host composes and that every
/// canonical domain endpoint resolves through the SDK route table, using the exact wiring
/// <c>Program.cs</c> performs (the explicit-assemblies overload over the Conversations domain and Server
/// boundary assemblies).
/// </summary>
/// <remarks>
/// "Resolve" here means the routes are mapped and the host composes/boots without throwing — not that every
/// endpoint dispatches live (no <c>IDomainQueryHandler</c>/<c>IDomainProjectionHandler</c> implementations
/// exist yet; those are adopted in Stories 2.3 / 2.5). Endpoint execution requires a live DAPR sidecar /
/// EventStore gateway and is an integration concern, so this asserts route presence and composition only.
/// The new package <c>Microsoft.AspNetCore.Mvc.Testing</c> is intentionally not introduced (CPM / no new
/// package versions); the SDK's minimal-host composition is driven directly instead.
/// </remarks>
public sealed class ConversationsDomainServiceHostCompositionTest
{
    private static WebApplication ComposeHost()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // The exact wiring Program.cs performs: explicit-assemblies overload over the domain assembly
        // (ConversationAggregate) plus the Server boundary assembly (forward-compatible handler discovery).
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);

        WebApplication app = builder.Build();
        app.UseEventStoreDomainService();
        return app;
    }

    private static IReadOnlyCollection<string> MappedRoutes(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => "/" + endpoint.RoutePattern.RawText!.TrimStart('/'))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// The host composes without throwing using the canonical two-line SDK idiom.
    /// </summary>
    [Fact]
    public async Task HostShouldComposeWithoutThrowing()
    {
        await using WebApplication app = ComposeHost();

        app.ShouldNotBeNull();
        MappedRoutes(app).ShouldNotBeEmpty();
    }

    /// <summary>
    /// Every canonical EventStore domain-service endpoint is mapped via the SDK route table.
    /// </summary>
    [Theory]
    [InlineData("/")]
    [InlineData("/process")]
    [InlineData("/replay-state")]
    [InlineData("/query")]
    [InlineData("/project")]
    [InlineData("/admin/operational-index-metadata")]
    public async Task CanonicalDomainEndpointShouldResolve(string route)
    {
        await using WebApplication app = ComposeHost();

        MappedRoutes(app).ShouldContain(route);
    }

    /// <summary>
    /// The ServiceDefaults health endpoints are mapped alongside the canonical domain routes.
    /// </summary>
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/ready")]
    public async Task ServiceDefaultsHealthEndpointShouldResolve(string route)
    {
        await using WebApplication app = ComposeHost();

        MappedRoutes(app).ShouldContain(route);
    }
}
