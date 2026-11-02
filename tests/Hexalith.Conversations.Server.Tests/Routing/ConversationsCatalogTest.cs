using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Routing;
using Hexalith.Conversations.EventStoreExtensions;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Server.Tests.Routing;

/// <summary>Verifies the internal catalog against actual owner receivers and serialized semantics.</summary>
public sealed class ConversationsCatalogTest
{
    /// <summary>Verifies complete compiled schemas use actual wrappers and only internal workload operations.</summary>
    [Fact]
    public void CompleteCompiledSchemasUseActualWrappersAndOnlyInternalWorkloadOperations()
    {
        ConversationsCatalogAdapters.Create().Count.ShouldBe(13);
        var owners = new[] { typeof(ConversationsCatalogDeclarations).Assembly };
        var routes = CompiledRouteCatalogDeclarations.Read(owners);
        var operations = CompiledRouteCatalogDeclarations.ReadOperations(owners);
        routes.Count.ShouldBe(22);
        routes.Count(route => route.Kind == "command").ShouldBe(13);
        routes.Count(route => route.Kind == "query").ShouldBe(7);
        routes.Count(route => route.Kind == "projection").ShouldBe(2);
        routes.All(route => route.CredentialKind == "workload" && route.PlatformAdmission is null).ShouldBeTrue();
        routes.Where(route => route.Kind == "command").All(route => route.Domain == "conversation"
            && route.Operation == EventStoreOperationIdentifiers.DomainServiceProcess).ShouldBeTrue();
        operations.All(operation => operation.Ingress == "internal"
            && operation.Digest == RouteCatalogCodec.ComputeOperationContractDigest(operation)).ShouldBeTrue();
        foreach (var route in routes.Where(route => route.Execution!.HandlerType.StartsWith("Hexalith.Conversations.Commands.", StringComparison.Ordinal)))
        {
            var properties = operations.Single(operation => operation.RouteId == route.RouteId).RequestSchema.GetProperty("properties");
            properties.TryGetProperty("publicCommand", out _).ShouldBeTrue();
            properties.TryGetProperty("eventId", out _).ShouldBeTrue();
        }
        operations.Single(operation => operation.RouteId == "conversation-create-conversation").RequestSchema
            .GetProperty("properties").TryGetProperty("createdAt", out _).ShouldBeTrue();
        routes.Where(route => route.Kind == "query" && route.Execution!.RequiresDelegatedContext).Count().ShouldBe(2);
        routes.Where(route => route.Kind == "query" && route.Domain == "conversations").Count().ShouldBe(4);
        routes.Where(route => route.Kind == "query" && route.Domain == "conversation").Count().ShouldBe(3);
        var listProperties = operations.Single(operation => operation.MessageType == "conversation-list").RequestSchema.GetProperty("properties");
        listProperties.EnumerateObject().Select(property => property.Name).Order().ShouldBe(["filter", "page"]);
        operations.Single(operation => operation.MessageType == "conversation-detail").RequestSchema.GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).ShouldBe(["conversationId"]);
        foreach (var route in routes.Where(route => route.Kind == "query" && route.Execution!.RequiresDelegatedContext))
        {
            var schema = operations.Single(operation => operation.RouteId == route.RouteId).RequestSchema;
            schema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
            if (schema.TryGetProperty("properties", out var properties)) properties.EnumerateObject().ShouldBeEmpty();
        }
        typeof(ConversationsCatalogDeclarations).Assembly.GetReferencedAssemblies().Any(reference => reference.Name == "Hexalith.Conversations").ShouldBeFalse();
    }

    /// <summary>Verifies missing or foreign selected owner refuses schema export.</summary>
    [Fact]
    public void MissingOrForeignSelectedOwnerRefusesSchemaExport()
    {
        Should.Throw<ArgumentNullException>(() => ConversationsCatalogDeclarations.CreateOperations(null!));
        Should.Throw<InvalidOperationException>(() => ConversationsCatalogDeclarations.CreateOperations(typeof(string).Assembly));
        var missing = new AssemblyLoadContext("conversations-catalog-missing-owner", isCollectible: true);
        Assembly isolatedContracts = missing.LoadFromAssemblyPath(typeof(ConversationsCatalogDeclarations).Assembly.Location);
        var source = isolatedContracts.GetType(typeof(ConversationsCatalogDeclarations).FullName!, throwOnError: true)!;
        Should.Throw<TargetInvocationException>(() => source.GetProperty("Operations")!.GetValue(null))
            .InnerException.ShouldBeOfType<InvalidOperationException>();
        var foreign = new AssemblyLoadContext("conversations-catalog-foreign-owner", isCollectible: true);
        Assembly foreignDomain = foreign.LoadFromAssemblyPath(typeof(ConversationAggregate).Assembly.Location);
        Should.Throw<InvalidOperationException>(() => ConversationsCatalogDeclarations.CreateOperations(foreignDomain));
        missing.Unload();
        foreign.Unload();
    }

    /// <summary>Verifies actual sdk discovery and finite receivers match every owner declaration and refuse drift.</summary>
    [Fact]
    public async Task ActualSdkDiscoveryAndFiniteReceiversMatchEveryOwnerDeclarationAndRefuseDrift()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(typeof(ConversationAggregate).Assembly, typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddConversationTenantAccess();
        builder.Services.AddConversationQueries(_ => { });
        builder.Services.Configure<DomainProjectionIdentityOptions>(options => { options.AppId = "conversation"; options.ServiceVersion = "v1"; });
        builder.Services.AddSingleton(new LegacyProjectionCatalogRegistration(
            ConversationsCatalogDeclarations.Projections.Where(route => route.ProjectionType == "conversation").ToArray()));
        await using var app = builder.Build();
        app.MapEventStoreDomainService();
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var routes = ConversationsCatalogDeclarations.Routes;
        services.GetRequiredKeyedService<IDomainProcessor>("conversation").ShouldBeOfType<ConversationAggregate>();
        var payloads = typeof(ConversationAggregate).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "Handle").Select(method => method.GetParameters()[0].ParameterType).ToArray();
        payloads.Select(type => type.FullName).Order().ShouldBe(routes.Where(route => route.Kind == "command").Select(route => route.Execution!.HandlerType).Order());
        services.GetServices<IDomainQueryHandler>().Select(handler => (handler.Domain, handler.QueryType, handler.GetType().FullName!)).Order()
            .ShouldBe(routes.Where(route => route.Kind == "query").Select(route => (route.Domain, route.MessageType!, route.Execution!.HandlerType)).Order());
        services.GetServices<IAsyncDomainProjectionHandler>().Select(handler => (handler.Domain, handler.ProjectionType)).ShouldBe([("conversation", "conversation-read-model")]);
        Endpoint[] endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        var targets = DevelopmentDomainRouteCatalog.Targets(services, endpoints, routes);
        targets.Count.ShouldBe(22);
        foreach (var target in targets)
        {
            target.AppId.ShouldBe("conversation");
            target.Method.ShouldBe(ConversationsCatalogDeclarations.ReceiverMethods[target.RouteId]);
        }
        Should.Throw<RouteCatalogUnavailableException>(() => DevelopmentDomainRouteCatalog.Targets(services, endpoints, routes.Skip(1).ToArray()));
        Should.Throw<RouteCatalogUnavailableException>(() => DevelopmentDomainRouteCatalog.Targets(services,
            endpoints.OfType<RouteEndpoint>().Where(endpoint => endpoint.RoutePattern.RawText != "/project").ToArray(), routes));
        Should.Throw<RouteCatalogUnavailableException>(() => DevelopmentDomainRouteCatalog.Targets(services, endpoints,
            routes.Select(route => route.ProjectionType == "conversation-read-model" ? route with { Execution = route.Execution! with { HandlerType = "foreign" } } : route).ToArray()));
    }
}
