// <copyright file="Story16_3EvidenceTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.IntegrationTests.Projections;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.Server.Projections;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Builders;
using Hexalith.EventStore.Testing.Integration;
using Hexalith.Tenants.Client.Projections;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>Native production-boundary proof of durable terminal recovery and its safety controls.</summary>
[Collection(Story16_3LiveCollection.Name)]
/// <param name="fixture">The isolated authenticated production topology.</param>
public sealed class Story16_3EvidenceTests(Story16_3LiveFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Tenant = "tenant-gateway-001";
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private IProjectionDeliveryRetryScheduler Scheduler => fixture.Gateway.Services.GetRequiredService<IProjectionDeliveryRetryScheduler>();

    /// <summary>Resource health cannot substitute for endpoint readiness, and only selected bindings collide.</summary>
    [Fact]
    public async Task Scenario01()
    {
        var missingDapr = Should.Throw<FileNotFoundException>(() => DaprDiagnostics.RequireDaprExecutable(
            Path.Combine(Path.GetTempPath(), "missing-daprd-" + Guid.NewGuid().ToString("N"))));
        missingDapr.Message.ShouldContain(DaprDiagnostics.DaprExecutableNotFound);
        using var client = new HttpClient { BaseAddress = new(fixture.Gateway.ApplicationHttpEndpoint) };
        using (var alive = await client.GetAsync("/alive", Token)) alive.StatusCode.ShouldBe(HttpStatusCode.OK);
        var readiness = await Should.ThrowAsync<TimeoutException>(() => DaprDiagnostics.WaitForAppHostEndpointAsync(
            client, "/missing-endpoint?tenantId=private", "conversations.http", TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(10), Token));
        readiness.Message.ShouldContain(DaprDiagnostics.AppHostEndpointNotReady);
        readiness.Message.ShouldContain("conversations.http");
        readiness.Message.ShouldNotContain("private");
        using var occupied = new TcpListener(IPAddress.Loopback, 0);
        occupied.Start();
        var binding = (IPEndPoint)occupied.LocalEndpoint;
        var collision = Should.Throw<InvalidOperationException>(() => DaprDiagnostics.RequireAvailableDaprControlPlaneBindings(
            new Dictionary<string, IPEndPoint> { ["placement.grpc"] = binding }, Token));
        collision.Message.ShouldContain(DaprDiagnostics.DaprControlPlanePortCollision);
        collision.Message.ShouldContain($"port={binding.Port}");
        occupied.Stop();
        DaprDiagnostics.RequireAvailableDaprControlPlaneBindings(new Dictionary<string, IPEndPoint> { ["placement.grpc"] = binding }, Token);
        using var request = fixture.Gateway.CreateAppChannelRequest("/admin/operational-index-metadata", HttpMethod.Post);
        request.Content = JsonContent.Create(new { Domains = new[] { "conversation" } });
        using var denied = await client.SendAsync(request, Token);
        denied.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        fixture.Complete(nameof(Scenario01));
    }

    /// <summary>A controlled first failure leaves an observable pending marker and terminal scheduler row.</summary>
    [Fact]
    public async Task Scenario02()
    {
        AggregateIdentity identity = await CreateFailedAsync();
        ProjectionDeliveryRetryWorkItem work = await ReadTerminalAsync(identity);
        work.PendingRoutes.ShouldBeEmpty();
        work.TerminalRoutes.ShouldBe(new[] { ConversationAsyncProjectionHandler.ConversationReadModelProjectionType });
        (await Scheduler.GetDueAsync(DateTimeOffset.UtcNow.AddDays(1), 100, Token)).ShouldNotContain(item => item.WorkId == work.WorkId);
        var index = await fixture.Store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationGatewayLiveFixture.StateStoreName, Story16_3LiveFixture.IndexKey(Tenant), Token);
        index.Value.ShouldNotBeNull();
        index.Value!.Dispatches[identity.AggregateId].IsPending.ShouldBeTrue();
        var missingDetail = await fixture.Store.GetAsync<ConversationProjectedReadModels>(
            ConversationGatewayLiveFixture.StateStoreName, Story16_3LiveFixture.DetailKey(Tenant, identity.AggregateId), Token);
        missingDetail.Value.ShouldBeNull();
        fixture.FailureStore.Failures.ShouldBeGreaterThan(0);
        fixture.RefusedAuthorityRequests.ShouldBeGreaterThan(0, "the real SDK history worker must fail closed without an authority grant");
        await RecoverAsync(identity, work);
        fixture.Complete(nameof(Scenario02));
    }

    /// <summary>The production coordinator sends the reconcile route and clears exactly the original durable work.</summary>
    [Fact]
    public async Task Scenario03()
    {
        AggregateIdentity identity = await CreateFailedAsync();
        ProjectionDeliveryRetryWorkItem work = await ReadTerminalAsync(identity);
        ProjectionDispatchRequest original = fixture.Wire.Last(item => item.Path == "/project/v2"
            && item.Request.DispatchId == work.DispatchId).Request;
        original.Request.NativeSourceProof.ShouldNotBeNull("the original actor dispatch must carry its native source hint");
        int before = fixture.Wire.Count;
        await RecoverAsync(identity, work);
        Story16_3WireObservation reconcile = fixture.Wire.Skip(before).Last(observation => observation.Path == "/project/v2/reconcile"
            && observation.Request.DispatchId == work.DispatchId && observation.Status == 200
            && observation.ChannelAuthenticated && observation.WorkloadAuthenticated);
        JsonSerializer.Serialize(reconcile.Request.Request.NativeSourceProof, Json).ShouldBe(
            JsonSerializer.Serialize(original.Request.NativeSourceProof, Json),
            "coordinator redispatch must retain the original actor native hint, not mint or drop one");
        (await QueryDetailAsync(identity)).FreshnessState.ShouldBe(ProjectionTrustState.Current);
        await TriggerAsync(identity);
        (await Scheduler.ReadAsync(work.WorkId, Token)).ShouldBeNull();
        (await QueryDetailAsync(identity)).Details.ShouldNotBeNull();
        fixture.Complete(nameof(Scenario03));
    }

    /// <summary>Route drift and absent workload authority fail closed without erasing terminal evidence.</summary>
    [Fact]
    public async Task Scenario04()
    {
        AggregateIdentity identity = await CreateFailedAsync();
        ProjectionDeliveryRetryWorkItem work = await ReadTerminalAsync(identity);
        ProjectionDispatchRequest original = fixture.Wire.Last(item => item.Request.DispatchId == work.DispatchId).Request;
        using (var denied = await fixture.SendAsync("/project/v2/reconcile", EventStoreWorkloadOperations.DomainServiceProject, original, Token, false))
            denied.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using (var drift = await fixture.SendAsync("/project/v2/reconcile", EventStoreWorkloadOperations.DomainServiceProject,
            original with { CatalogFingerprint = new string('0', 64) }, Token))
        {
            drift.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable,
                "the committed-generation receiver guard must refuse drift before projection dispatch");
        }
        using (var removed = await fixture.SendAsync("/project/v2/removed", EventStoreWorkloadOperations.DomainServiceProject, original, Token))
            removed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
                "an unmapped method must retain the host's fail-closed fallback authorization");
        (await ReadTerminalAsync(identity)).DispatchId.ShouldBe(work.DispatchId);
        await RecoverAsync(identity, work);
        var outsider = await QueryDetailAsync(identity, "nonmember");
        outsider.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        fixture.Complete(nameof(Scenario04));
    }

    /// <summary>Duplicate old recovery traffic cannot replace a later production append's persisted projection.</summary>
    [Fact]
    public async Task Scenario05()
    {
        AggregateIdentity identity = await CreateFailedAsync();
        ProjectionDeliveryRetryWorkItem work = await ReadTerminalAsync(identity);
        ProjectionDispatchRequest original = fixture.Wire.Last(item => item.Request.DispatchId == work.DispatchId).Request;
        await RecoverAsync(identity, work);
        var update = new ReassignConversationProject(new ReassignConversationProjectCommand(Metadata(identity, "progress"),
            new(identity.AggregateId), new(ConversationProjectAssignmentOperation.Assign, new("project-newer"))),
            DateTimeOffset.UtcNow, "event-newer-" + identity.AggregateId);
        await AppendAsync(identity, update);
        await TriggerAsync(identity);
        var newer = await PersistedAsync(identity);
        newer.Detail.Freshness.LastAppliedEventPosition.ShouldBe(2);
        using (var duplicate = await fixture.SendAsync("/project/v2/reconcile", EventStoreWorkloadOperations.DomainServiceProject, original, Token))
            duplicate.StatusCode.ShouldBe(HttpStatusCode.OK);
        // Assert before the next trigger: redelivering head 2 would otherwise repair, and mask, a duplicate overwrite.
        JsonSerializer.Serialize(await PersistedAsync(identity), Json).ShouldBe(JsonSerializer.Serialize(newer, Json),
            "the duplicate original reconcile must not replace the newer persisted generation");
        (await QueryDetailAsync(identity)).Details!.ProjectId.ShouldBe(new ProjectId("project-newer"));
        await TriggerAsync(identity);
        JsonSerializer.Serialize(await PersistedAsync(identity), Json).ShouldBe(JsonSerializer.Serialize(newer, Json));
        (await QueryDetailAsync(identity)).Details!.ProjectId.ShouldBe(new ProjectId("project-newer"));
        (await Scheduler.ReadAsync(work.WorkId, Token)).ShouldBeNull();
        fixture.Complete(nameof(Scenario05));
    }

    private async Task<AggregateIdentity> CreateFailedAsync()
    {
        // A private Redis instance makes the fixed command-authority tenant disposable for this collection.
        await EnsureTenantAsync();
        var identity = new AggregateIdentity(Tenant, ConversationProjectionHandler.ConversationDomain, "story163-" + Guid.NewGuid().ToString("N"));
        var command = new CreateConversation(new CreateConversationCommand(Metadata(identity, "create"),
            new("crm", "case-" + identity.AggregateId), new("project-initial"), new("folder-fixture"), "Terminal recovery"),
            new(identity.AggregateId), DateTimeOffset.UtcNow, "event-created-" + identity.AggregateId);
        await AppendAsync(identity, command);
        fixture.FailureStore.Block(Story16_3LiveFixture.DetailKey(Tenant, identity.AggregateId));
        int failuresBefore = fixture.FailureStore.Failures;
        await TriggerAsync(identity);
        fixture.FailureStore.Failures.ShouldBeGreaterThan(failuresBefore,
            "this terminal work must come from the selected persistence injection, not unavailable history or authority; " + fixture.Stages);
        return identity;
    }

    private async Task EnsureTenantAsync()
    {
        var record = await fixture.Store.GetAsync<CheckpointedProjectionRecord<TenantLocalState>>(ConversationGatewayLiveFixture.StateStoreName, DurableTenantProjectionStore.KeyFor(Tenant), Token);
        if (record.Value is null) await fixture.AdmitTenantAsync(Tenant, Token);
        record = await fixture.Store.GetAsync<CheckpointedProjectionRecord<TenantLocalState>>(ConversationGatewayLiveFixture.StateStoreName,
            DurableTenantProjectionStore.KeyFor(Tenant), Token);
        record.Value.ShouldNotBeNull();
        record.Value!.IsHealthy.ShouldBeTrue();
        record.Value.LastContiguousSequence.ShouldBe(2);
        record.Value.State.ShouldNotBeNull();
        var list = await QueryListAsync();
        list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
    }

    private static ConversationCommandMetadata Metadata(AggregateIdentity identity, string operation) => new(
        SchemaVersion.Current, new(Tenant), new(Story16_3LiveFixture.Caller), "correlation-" + identity.AggregateId,
        "causation-" + operation + identity.AggregateId, "idempotency-" + operation + identity.AggregateId);

    private async Task AppendAsync<T>(AggregateIdentity identity, T command)
    {
        var envelope = new CommandEnvelopeBuilder().WithTenantId(identity.TenantId).WithDomain(identity.Domain)
            .WithAggregateId(identity.AggregateId).WithCommandType(typeof(T).Name).WithPayload(JsonSerializer.SerializeToUtf8Bytes(command))
            .WithCorrelationId("correlation-" + identity.AggregateId).WithUserId(Story16_3LiveFixture.Caller).Build();
        IAggregateActor actor = fixture.Gateway.CreateAggregateActor(identity.TenantId, identity.Domain, identity.AggregateId);
        CommandProcessingResult result = await actor.ProcessCommandAsync(envelope);
        result.Accepted.ShouldBeTrue(result.ErrorMessage);
        (await actor.GetEventsAsync(0)).ShouldNotBeEmpty();
    }

    private Task TriggerAsync(AggregateIdentity identity) => fixture.Gateway.Services.GetRequiredService<IProjectionUpdateOrchestrator>()
        .UpdateProjectionAsync(identity, Token);

    private async Task<ProjectionDeliveryRetryWorkItem> ReadTerminalAsync(AggregateIdentity identity)
    {
        string id = ProjectionDeliveryRetryWorkItem.CreateWorkId(identity.TenantId, identity.Domain, identity.AggregateId, 1);
        var work = await Scheduler.ReadAsync(id, Token);
        work.ShouldNotBeNull("durable terminal work must exist before recovery");
        work!.TerminalRoutes.ShouldNotBeEmpty(JsonSerializer.Serialize(work, Json));
        return work;
    }

    private async Task RecoverAsync(AggregateIdentity identity, ProjectionDeliveryRetryWorkItem work)
    {
        fixture.FailureStore.Release();
        await TriggerAsync(identity);
        (await Scheduler.ReadAsync(work.WorkId, Token)).ShouldBeNull("the production coordinator must durably clear the original item; " + fixture.Stages);
        (await PersistedAsync(identity)).Detail.Freshness.LastAppliedEventPosition.ShouldBeGreaterThan(0);
        var index = await fixture.Store.GetAsync<ConversationProjectionIndexReadModel>(ConversationGatewayLiveFixture.StateStoreName, Story16_3LiveFixture.IndexKey(Tenant), Token);
        index.Value!.Dispatches[identity.AggregateId].IsPending.ShouldBeFalse();
    }

    private async Task<ConversationProjectedReadModels> PersistedAsync(AggregateIdentity identity)
    {
        var result = await fixture.Store.GetAsync<ConversationProjectedReadModels>(ConversationGatewayLiveFixture.StateStoreName,
            Story16_3LiveFixture.DetailKey(Tenant, identity.AggregateId), Token);
        result.Value.ShouldNotBeNull();
        return result.Value!;
    }

    private async Task<ConversationDetailResult> QueryDetailAsync(AggregateIdentity identity, string caller = Story16_3LiveFixture.Caller)
        => await QueryAsync<ConversationDetailResult>(new(Tenant, "conversations", identity.AggregateId,
            "conversation-detail", [], "query-" + identity.AggregateId, caller));

    private Task<ConversationListResult> QueryListAsync() => QueryAsync<ConversationListResult>(new(Tenant, "conversations", "tenant-list",
        "conversation-list", [], "query-list", Story16_3LiveFixture.Caller));

    private async Task<T> QueryAsync<T>(QueryEnvelope query)
    {
        using var response = await fixture.SendAsync("/query", EventStoreWorkloadOperations.DomainServiceQuery, query, Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Token));
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(Json, Token);
        result.ShouldNotBeNull();
        result!.Success.ShouldBeTrue(result.ErrorMessage);
        return JsonSerializer.Deserialize<T>(result.PayloadBytes!, Json)!;
    }
}
