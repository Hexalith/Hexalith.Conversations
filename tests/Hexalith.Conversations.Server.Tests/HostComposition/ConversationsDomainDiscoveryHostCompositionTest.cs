// <copyright file="ConversationsDomainDiscoveryHostCompositionTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;

using Hexalith.Conversations;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Projections;
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

    /// <summary>
    /// Story 2.3 (AC-3) — the SDK assembly scan over the Server boundary assembly discovers and registers the
    /// conversation <see cref="IDomainQueryHandler"/> adapters (list + detail) the <c>/query</c> dispatch route
    /// resolves. The host's query-boundary wiring then makes them constructible. The read store is faked here
    /// (its production binding lands in Story 2.4); every other dependency comes from the real host wiring.
    /// </summary>
    [Fact]
    public async Task ExplicitAssemblyScanShouldDiscoverConversationDomainQueryHandlers()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowAllTenantAccessService());
        builder.Services.AddSingleton<IConversationProjectionReadStore>(new EmptyProjectionReadStore());
        builder.Services.AddDataProtection();
        builder.Services.AddConversationQueries(options => options.MaxOffset = 100_000);

        await using WebApplication app = builder.Build();
        using IServiceScope scope = app.Services.CreateScope();

        List<IDomainQueryHandler> handlers = scope.ServiceProvider.GetServices<IDomainQueryHandler>().ToList();

        handlers.ShouldContain(handler =>
            handler.Domain == ConversationDomainQueryHandlerBase.ConversationsDomain
            && handler.QueryType == ListConversationsDomainQueryHandler.ConversationListQueryType);
        handlers.ShouldContain(handler =>
            handler.Domain == ConversationDomainQueryHandlerBase.ConversationsDomain
            && handler.QueryType == GetConversationDomainQueryHandler.ConversationDetailQueryType);
    }

    /// <summary>
    /// Story 2.5 (AC-1) — the SDK assembly scan over the Server boundary assembly discovers and registers the
    /// conversation <see cref="IDomainProjectionHandler"/> the <c>/project</c> dispatch route resolves, with no
    /// <c>Program.cs</c> edit. Its <c>Domain</c> matches the aggregate domain key the projection actor routes on
    /// (singular), and its dependency (<see cref="ConversationProjectionMaterializer"/>) resolves from the real
    /// host query-boundary wiring — proving the production host can construct it.
    /// </summary>
    [Fact]
    public async Task ExplicitAssemblyScanShouldDiscoverConversationProjectionHandler()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);

        // Mirror the production host wiring (Program.cs): tenant access and the query boundary (which registers
        // the shared ConversationProjectionMaterializer the handler needs). The platform host owns DaprClient.
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowAllTenantAccessService());
        builder.Services.AddDataProtection();
        builder.Services.AddConversationQueries(options => options.MaxOffset = 100_000);

        await using WebApplication app = builder.Build();
        using IServiceScope scope = app.Services.CreateScope();

        IDomainProjectionHandler handler = scope.ServiceProvider.GetServices<IDomainProjectionHandler>().ShouldHaveSingleItem();

        handler.ShouldBeOfType<ConversationProjectionHandler>();
        handler.Domain.ShouldBe(ConversationDomainKey);
    }

    /// <summary>
    /// The same explicit scan discovers the rebuild-capable named production read-model handler.
    /// </summary>
    [Fact]
    public async Task ExplicitAssemblyScanShouldDiscoverNamedConversationReadModelHandler()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowAllTenantAccessService());
        builder.Services.AddDataProtection();
        builder.Services.AddConversationQueries(options => options.MaxOffset = 100_000);

        await using WebApplication app = builder.Build();
        using IServiceScope scope = app.Services.CreateScope();

        IAsyncDomainProjectionHandler handler = scope.ServiceProvider
            .GetServices<IAsyncDomainProjectionHandler>()
            .ShouldHaveSingleItem();

        handler.ShouldBeOfType<ConversationAsyncProjectionHandler>();
        handler.Domain.ShouldBe(ConversationDomainKey);
        handler.ProjectionType.ShouldBe(ConversationAsyncProjectionHandler.ConversationReadModelProjectionType);
        handler.ShouldBeAssignableTo<IAsyncDomainProjectionRebuildHandler>();
    }

    /// <summary>
    /// Story 2.4 (AC-1) — with the production read-model-store registrations the deferred-from-2.3 binding gap
    /// is closed: <see cref="IReadModelStore"/> resolves to the SDK <see cref="DaprReadModelStore"/>,
    /// <see cref="IConversationProjectionReadStore"/> resolves to the production
    /// <see cref="ConversationProjectionReadStore"/>, and the query/governance dependency graph that requires
    /// the read store builds with no missing-service throw (no test fake supplies the binding).
    /// </summary>
    [Fact]
    public async Task ProductionHostShouldResolveReadStoreBindingAndConsumerGraph()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);

        // Mirror the production host wiring (Program.cs): tenant access and the query boundary that registers
        // AddEventStoreReadModelStore + the production read-store binding. The platform host owns DaprClient.
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowAllTenantAccessService());
        builder.Services.AddDataProtection();
        builder.Services.AddConversationQueries(options => options.MaxOffset = 100_000);
        builder.Services.AddConversationGovernanceVerification();

        await using WebApplication app = builder.Build();
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        // The deferred-from-2.3 production binding facts.
        services.GetRequiredService<IReadModelStore>().ShouldBeOfType<DaprReadModelStore>();
        services.GetRequiredService<IConversationProjectionReadStore>().ShouldBeOfType<ConversationProjectionReadStore>();
        services.GetRequiredService<ConversationProjectionReadModelWriter>().ShouldNotBeNull();

        // The query/governance consumers of the read store now build from the real host.
        services.GetRequiredService<ConversationQueryHandler>().ShouldNotBeNull();
        services.GetRequiredService<ConversationProjectionReadService>().ShouldNotBeNull();
        services.GetRequiredService<ConversationAuditRecordAccessService>().ShouldNotBeNull();
        services.GetRequiredService<ConversationGovernanceVerificationService>().ShouldNotBeNull();
    }

    private sealed class AllowAllTenantAccessService : IConversationTenantAccessService
    {
        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationTenantAccessDecision.Allowed(
                requirement,
                trustedTenantId ?? new TenantId("tenant-001"),
                callerPrincipalId ?? "caller-001"));
    }

    private sealed class EmptyProjectionReadStore : IConversationProjectionReadStore
    {
        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<ConversationProjectedReadModels?>(null);

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult((IReadOnlyList<ConversationSummaryProjectionV1>)[]);
    }
}
