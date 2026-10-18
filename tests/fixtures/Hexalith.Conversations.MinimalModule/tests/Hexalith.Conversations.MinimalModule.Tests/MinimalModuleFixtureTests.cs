using System.Text.Json;
using Hexalith.Conversations.MinimalModule.Contracts;
using Hexalith.Conversations.MinimalModule.Server;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.MinimalModule.Tests;

public sealed class MinimalModuleFixtureTests
{
    private const string Tenant = "tenant-a";
    private const string Id = "conversation-1";

    private static WebApplication BuildHost()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.AddMinimalConversationModule();
        WebApplication app = builder.Build();
        _ = app.UseEventStoreDomainService();
        return app;
    }

    private static CommandEnvelope Command(CreateConversation command)
        => new(
            MessageId: Guid.NewGuid().ToString(),
            TenantId: Tenant,
            Domain: CreateConversation.Domain,
            AggregateId: command.AggregateId,
            CommandType: CreateConversation.CommandType,
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "corr-1",
            CausationId: null,
            UserId: "user-1",
            Extensions: null);

    private static RouteEndpoint[] Endpoints(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints).OfType<RouteEndpoint>().ToArray();

    [Fact]
    public void Registration_ExposesTheModuleHandlersThroughThePlatformHost()
    {
        using WebApplication app = BuildHost();

        app.Services.GetServices<IDomainProjectionHandler>().ShouldContain(static handler => handler is ConversationProjection);
        app.Services.GetServices<IDomainQueryHandler>().ShouldContain(static handler => handler is ConversationTitleQuery);
        app.Services.GetService<EventStoreDomainEventProcessor>().ShouldNotBeNull();
    }

    [Fact]
    public async Task CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay()
    {
        IAsyncDomainProcessor aggregate = new ConversationAggregate();

        DomainResult first = await aggregate.ProcessAsync(Command(new CreateConversation(Id, "Hello")), currentState: null, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        ConversationCreated created = first.Events.ShouldHaveSingleItem().ShouldBeOfType<ConversationCreated>();
        created.ShouldBe(new ConversationCreated(Id, "Hello"));

        ConversationState state = new();
        state.Apply(created);
        DomainResult second = await aggregate.ProcessAsync(Command(new CreateConversation(Id, "Hello")), state, TestContext.Current.CancellationToken);

        second.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void Projection_IsDispatchedByThePlatformDispatcher()
    {
        using WebApplication app = BuildHost();
        ProjectionEventDto evt = new(
            nameof(ConversationCreated),
            JsonSerializer.SerializeToUtf8Bytes(new ConversationCreated(Id, "Hello")),
            "json",
            1,
            DateTimeOffset.UnixEpoch,
            "corr-1");

        ProjectionResponse? response = DomainProjectionDispatcher.Project(
            app.Services,
            new ProjectionRequest(Tenant, ConversationModule.Domain, Id, [evt]),
            TestContext.Current.CancellationToken);

        response.ShouldNotBeNull();
        response.State.GetProperty("title").GetString().ShouldBe("Hello");
    }

    [Fact]
    public async Task Query_IsDispatchedByThePlatformDispatcher()
    {
        using WebApplication app = BuildHost();
        QueryEnvelope query = new(Tenant, ConversationModule.Domain, Id, ConversationModule.TitleQueryType, [], "corr-1", "user-1");

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(app.Services, query, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.GetPayload().GetProperty("AggregateId").GetString().ShouldBe(Id);
    }

    [Fact]
    public void SubscriptionAndPublication_AreServedByPlatformOwnedEndpointsOnly()
    {
        using WebApplication app = BuildHost();
        RouteEndpoint[] endpoints = Endpoints(app);
        string[] routes = [.. endpoints.Select(static endpoint => EventStoreDomainServiceRoutes.Normalize(endpoint.RoutePattern.RawText))];

        routes.ShouldContain("/dapr/subscribe");
        routes.ShouldContain("/conversation/events");
        endpoints
            .Where(static endpoint => EventStoreDomainServiceEndpointInventory.IsSidecarOriginated(
                endpoint, EventStoreDomainServiceRoutes.Normalize(endpoint.RoutePattern.RawText)))
            .Select(static endpoint => EventStoreDomainServiceRoutes.Normalize(endpoint.RoutePattern.RawText))
            .ShouldBe(["/conversation/events", "/dapr/subscribe"], ignoreOrder: true);
        EventStoreDomainServiceEndpointInventory.Validate(endpoints, fallbackPolicy: null)
            .ShouldNotContain(static violation => violation.Contains("dapr/subscribe", StringComparison.Ordinal));
        typeof(ConversationModule).Assembly.GetTypes()
            .Where(static type => typeof(Microsoft.AspNetCore.Routing.EndpointDataSource).IsAssignableFrom(type))
            .ShouldBeEmpty("the module authors no endpoint of its own");
    }

    [Fact]
    public async Task PublishedEvent_IsConsumedThroughThePlatformProcessor()
    {
        using WebApplication app = BuildHost();
        EventStoreDomainEventProcessor processor = app.Services.GetRequiredService<EventStoreDomainEventProcessor>();
        EventStoreDomainEventEnvelope envelope = new(
            MessageId: Guid.NewGuid().ToString("D"),
            AggregateId: Id,
            TenantId: Tenant,
            EventTypeName: typeof(ConversationCreated).FullName!,
            SequenceNumber: 1,
            Timestamp: DateTimeOffset.UnixEpoch,
            CorrelationId: "correlation-1",
            SerializationFormat: "json",
            Payload: JsonSerializer.SerializeToUtf8Bytes(new ConversationCreated(Id, "Hello")))
        {
            Domain = ConversationModule.Domain,
            AggregateType = nameof(ConversationAggregate),
            PayloadVersion = 1,
        };

        EventStoreDomainEventProcessingResult result = await processor.ProcessAsync(envelope, TestContext.Current.CancellationToken);

        result.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        app.Services.GetRequiredService<ConversationObservations>().Observed.ShouldContain(Id);
    }
}
