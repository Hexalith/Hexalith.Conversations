// <copyright file="ConversationsDomainServiceHostCompositionTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations;
using Hexalith.Conversations.Server;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults;

using Dapr.Client;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

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
        builder.Services.AddConversationTenantAccess();
        builder.Services.AddConversationQueries(builder.Configuration);

        WebApplication app = builder.Build();

        // Exactly what Program.cs performs. The DAPR pub/sub pipeline is no longer listed here because it is
        // no longer the module's to perform: AddConversationTenantAccess registers a consumer, so
        // UseEventStoreDomainService now wires CloudEvents unwrapping, the subscription route, and
        // /dapr/subscribe itself (pass-10 decision D1). The route assertions below still hold, which is what
        // proves the promotion actually carries the behaviour rather than dropping it.
        app.UseEventStoreDomainService();
        return app;
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("The repository root could not be resolved.");
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
    /// The consumed Tenants subscription surface is mapped. Without <c>/tenants/events</c> the registered
    /// tenants consumer is unreachable, and without <c>/dapr/subscribe</c> the sidecar never learns the topic
    /// exists — either way the tenant access projection stays empty and every authorized read fails closed
    /// forever. Neither route was asserted anywhere before pass 10.
    /// </summary>
    /// <param name="route">The subscription route that must resolve.</param>
    [Theory]
    [InlineData("/tenants/events")]
    [InlineData("/dapr/subscribe")]
    public async Task TenantSubscriptionEndpointShouldResolve(string route)
    {
        await using WebApplication app = ComposeHost();

        MappedRoutes(app).ShouldContain(route);
    }

    /// <summary>
    /// AC3 ownership guard. DAPR event-subscription plumbing is generic host capability that belongs on the
    /// public platform surface, never in a domain module. Pass-10 decision D1 promoted it into
    /// <c>UseEventStoreDomainService()</c>; this asserts the module does not re-acquire it. The behaviour
    /// itself is proved by <c>TenantSubscriptionEndpointShouldResolve</c>, which is where a regression in the
    /// promoted capability would surface — this test guards the ownership boundary, not the wiring.
    /// </summary>
    [Fact]
    public void ProductionHostShouldNotRetypeGenericSubscriptionPlumbing()
    {
        string programPath = Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.Conversations.Server",
            "Program.cs");
        File.Exists(programPath).ShouldBeTrue(programPath);
        string program = File.ReadAllText(programPath);

        foreach (string generic in new[]
                 {
                     "app.UseCloudEvents();",
                     "app.MapEventStoreDomainEvents();",
                     "app.MapSubscribeHandler();",
                 })
        {
            program.ShouldNotContain(
                generic,
                Case.Sensitive,
                $"'{generic}' is generic subscription plumbing owned by UseEventStoreDomainService(); a domain "
                + "module re-typing it is the AC3 violation pass 10 promoted away.");
        }
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

    /// <summary>
    /// The ServiceDefaults status-code mapping remains serving for Healthy/Degraded and unavailable for Unhealthy.
    /// </summary>
    [Fact]
    public void ServiceDefaultsHealthStatusCodesShouldPreserveCurrentMapping()
    {
        IDictionary<HealthStatus, int> statusCodes =
            Hexalith.Commons.ServiceDefaults.HexalithServiceDefaults.CreateHealthStatusCodes();

        statusCodes[HealthStatus.Healthy].ShouldBe(StatusCodes.Status200OK);
        statusCodes[HealthStatus.Degraded].ShouldBe(StatusCodes.Status200OK);
        statusCodes[HealthStatus.Unhealthy].ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Development health responses stay detailed JSON for /health and /ready.
    /// </summary>
    [Fact]
    public async Task ServiceDefaultsDevelopmentHealthWriterShouldProduceDetailedJson()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        HealthReport report = new(
            new Dictionary<string, HealthReportEntry>
            {
                ["self"] = new(HealthStatus.Healthy, "ok", TimeSpan.FromMilliseconds(1), exception: null, data: new Dictionary<string, object>()),
            },
            TimeSpan.FromMilliseconds(1));

        await Extensions.WriteHealthCheckJsonResponse(context, report);

        context.Response.ContentType.ShouldBe("application/json; charset=utf-8");
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        string json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        json.ShouldContain("\"status\"");
        json.ShouldContain("\"results\"");
        json.ShouldContain("\"self\"");
    }

    /// <summary>
    /// Health probes are excluded from ASP.NET Core tracing while application traffic remains traced.
    /// </summary>
    [Theory]
    [InlineData("/health", false)]
    [InlineData("/alive", false)]
    [InlineData("/ready", false)]
    [InlineData("/process", true)]
    public void ServiceDefaultsShouldExcludeHealthProbesFromTracing(string path, bool expected)
    {
        DefaultHttpContext context = new();
        context.Request.Path = path;

        Extensions.ShouldTraceHttpRequest(context).ShouldBe(expected);
    }

    /// <summary>
    /// The current EventStore runtime ServiceDefaults path still registers the expected side effects once.
    /// </summary>
    [Fact]
    public void AddEventStoreDomainServiceShouldRegisterServiceDefaultsSideEffects()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        HealthCheckServiceOptions healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        healthOptions.Registrations.Count(static registration => registration.Name == "self").ShouldBe(1);

        string descriptorText = string.Join(Environment.NewLine, builder.Services.Select(static descriptor => descriptor.ToString()));
        descriptorText.ShouldContain("ServiceDiscovery");
        descriptorText.ShouldContain("Resilience");
        descriptorText.ShouldContain("OpenTelemetry");
    }

    /// <summary>
    /// The canonical platform host owns the single generic Dapr client registration required by read models.
    /// </summary>
    [Fact]
    public void AddEventStoreDomainServiceShouldOwnDaprClientRegistration()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);

        builder.Services.Count(static descriptor => descriptor.ServiceType == typeof(DaprClient)).ShouldBe(1);
    }

    /// <summary>
    /// The canonical platform host also owns the Data Protection provider the platform-owned query cursor codec
    /// depends on, so a domain module never has to patch that generic gap inside its own composition root.
    /// </summary>
    /// <remarks>
    /// Asserted against the resolved provider rather than the descriptor list: Data Protection registers through
    /// a builder whose descriptor shape is an implementation detail, and what this story cares about is that a
    /// module host which registers nothing itself can still resolve the dependency.
    /// </remarks>
    [Fact]
    public void AddEventStoreDomainServiceShouldOwnDataProtectionRegistration()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        provider.GetService<IDataProtectionProvider>().ShouldNotBeNull(
            "AddEventStoreDomainService must supply the Data Protection provider the query cursor codec needs; "
            + "Conversations must not register it, because a generic platform gap is fixed in the owning public surface.");
    }

    /// <summary>
    /// Conversations' own query registration must not re-register the generic provider it consumes.
    /// </summary>
    [Fact]
    public void ConversationQueryRegistrationShouldNotOwnDataProtection()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Services.AddConversationTenantAccess();
        builder.Services.AddConversationQueries(builder.Configuration);

        builder.Services
            .Any(static descriptor => descriptor.ServiceType == typeof(IDataProtectionProvider))
            .ShouldBeFalse(
                "the Conversations module must consume the platform's Data Protection provider, never introduce one.");
    }
}
