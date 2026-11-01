// <copyright file="ConversationTenantAccessHostCompositionTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Tests.Agents;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Composes the host with the same registrations as <c>Program.cs</c> and proves the enrolled history reader is the
/// SDK gateway client on the worker authority transport, recovering an out-of-order gap behind a lost rejection.
/// </summary>
public sealed class ConversationTenantAccessHostCompositionTest
{
    private const string Caller = "user-composed";
    private const string GatewayStreamRead = "https://gateway.test/api/v1/streams/read";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset At = new(2026, 11, 1, 14, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The composed host resolves the SDK gateway client as its history reader, and a gap left by a rejection that
    /// never reached this host recovers through an authenticated gateway range read.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ProgramCompositionRecoversARejectionInducedGapThroughTheGateway()
    {
        DurableTenantAccessHarness harness = new();
        Dictionary<string, string?> settings = DeletionRuntimeTestSettings.Configuration(enabled: false);
        StubHttpHandler transport = new((request, body) => RespondAsync(harness.Gateway, request, body));
        await using WebApplication app = ComposeLikeProgram(settings, harness.Store, transport);
        string tenant = "tenant-composed";

        using (IServiceScope scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IEventStoreGatewayClient>().ShouldBeOfType<EventStoreGatewayClient>();
        }

        app.Services.GetRequiredService<ITenantProjectionStore>().ShouldBeOfType<DurableTenantProjectionStore>();
        EventStoreDomainEventProcessor processor = app.Services.GetRequiredService<EventStoreDomainEventProcessor>();
        EventStoreDomainEventEnvelope created = harness.Persist(new TenantCreated(tenant, "Composed", null, At), tenant, 1);
        EventStoreDomainEventEnvelope rejection = harness.Persist(new TenantAlreadyExistsRejection(tenant), tenant, 2);
        EventStoreDomainEventEnvelope member = harness.Persist(new UserAddedToTenant(tenant, Caller, TenantRole.TenantOwner), tenant, 3);

        (await processor.ProcessAsync(created, TestContext.Current.CancellationToken)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        // The rejection reached another consumer and was lost there: the member arrives here behind a gap.
        (await processor.ProcessAsync(member, TestContext.Current.CancellationToken)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        string storeName = ConversationProjectionStoreOptions.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()).StateStoreName;
        CheckpointedProjectionRecord<TenantLocalState> record = harness.Store
            .Snapshot<CheckpointedProjectionRecord<TenantLocalState>>(storeName, DurableTenantProjectionStore.KeyFor(tenant))
            .ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        record.LastContiguousSequence.ShouldBe(3);
        record.State.ShouldNotBeNull().Members[Caller].ShouldBe(TenantRole.TenantOwner);

        RecordedRequest read = transport.Requests.Where(static request => request.Uri.AbsoluteUri == GatewayStreamRead).ShouldHaveSingleItem();
        read.Method.ShouldBe(HttpMethod.Post);
        read.Authorization.ShouldBe("Bearer " + DeletionRuntimeTestSettings.WorkerToken);
        StreamReadRequest readRequest = JsonSerializer.Deserialize<StreamReadRequest>(read.Body, Web)!;
        readRequest.Tenant.ShouldBe("system");
        readRequest.Domain.ShouldBe("tenants");
        readRequest.AggregateId.ShouldBe(tenant);
        readRequest.FromSequence.ShouldBe(1);
        readRequest.ToSequence.ShouldBe(3);
        transport.Requests.ShouldContain(static request => request.Uri.AbsoluteUri == DeletionRuntimeTestSettings.TokenEndpoint);

        using (IServiceScope scope = app.Services.CreateScope())
        {
            ConversationTenantAccessDecision decision = await scope.ServiceProvider.GetRequiredService<IConversationTenantAccessService>()
                .CheckAccessAsync(
                    ConversationTenantAccessRequirement.Admin,
                    new TenantId(tenant),
                    Caller,
                    commandTenantId: new TenantId(tenant),
                    cancellationToken: TestContext.Current.CancellationToken);
            decision.IsAllowed.ShouldBeTrue();
            decision.ProjectionVersion.ShouldBe(3);
        }

        // The lost rejection's late redelivery is verified against history and never reapplied.
        (await processor.ProcessAsync(rejection, TestContext.Current.CancellationToken)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        harness.Store.Snapshot<CheckpointedProjectionRecord<TenantLocalState>>(storeName, DurableTenantProjectionStore.KeyFor(tenant))!
            .Version.ShouldBe(record.Version);
    }

    private static WebApplication ComposeLikeProgram(Dictionary<string, string?> settings, IReadModelStore store, HttpMessageHandler transport)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);

        // The test-owned state store replaces the Dapr adapter; everything else is the Program.cs sequence.
        builder.Services.AddSingleton(store);
        builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddConversationTenantAccess(builder.Configuration);
        builder.Services.AddConversationQueries(builder.Configuration);
        builder.Services.AddSingleton<IHttpMessageHandlerBuilderFilter>(new PrimaryHandlerOverride(transport));
        WebApplication app = builder.Build();
        app.UseEventStoreDomainService();
        return app;
    }

    private static async Task<HttpResponseMessage> RespondAsync(TenantStreamHistoryGateway history, HttpRequestMessage request, string body)
    {
        string uri = request.RequestUri!.AbsoluteUri;
        if (uri == DeletionRuntimeTestSettings.TokenEndpoint)
        {
            return Json(new { access_token = DeletionRuntimeTestSettings.WorkerToken, token_type = "Bearer", expires_in = 300 });
        }

        if (uri == GatewayStreamRead && request.Method == HttpMethod.Post)
        {
            StreamReadRequest read = JsonSerializer.Deserialize<StreamReadRequest>(body, Web)!;
            return Json(await history.ReadStreamAsync(read, CancellationToken.None));
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json(object value)
        => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value, Web), Encoding.UTF8, "application/json") };
}
