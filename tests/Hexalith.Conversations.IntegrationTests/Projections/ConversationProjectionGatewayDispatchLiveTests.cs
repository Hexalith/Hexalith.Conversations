// <copyright file="ConversationProjectionGatewayDispatchLiveTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.Server.Projections;
using Hexalith.EventStore.Testing.Builders;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Proves that an accepted production append reaches the Conversations named asynchronous projection handler
/// through the EventStore gateway and lands in the configured DAPR integration state store, with the production
/// query surface reading that state back.
/// </summary>
/// <remarks>
/// This is the ADR 0003 Verification 1-2 lane. It differs from
/// <see cref="ConversationProjectionReadStorePopulationLiveTests"/> in the two ways that matter: delivery is
/// driven by <see cref="IProjectionUpdateOrchestrator"/> (which reaches the gateway's named-projection
/// coordinator and the domain-service dispatcher over DAPR service invocation) rather than by calling the
/// dispatcher in-process, and the configured <c>IReadModelStore</c> is the DAPR-backed adapter rather than an
/// in-memory fake.
/// </remarks>
[Collection(ConversationGatewayLiveCollection.Name)]
public sealed class ConversationProjectionGatewayDispatchLiveTests(ConversationGatewayLiveFixture fixture)
{
    private static readonly JsonSerializerOptions CommandPayloadOptions = new();

    [Fact]
    public async Task GatewayDeliveryShouldPopulateConfiguredStateStoreAndProductionQueries()
    {
        fixture.RequireAvailable();

        string tenantId = "tenant-gateway-001";
        string conversationId = $"conversation-gateway-{Guid.NewGuid():N}";
        AggregateIdentity identity = new(tenantId, ConversationProjectionHandler.ConversationDomain, conversationId);

        // The immediate-delivery contract is what this lane proves. A non-zero refresh interval would make
        // UpdateProjectionAsync register polling work and return without dispatching, so the assertion below
        // would pass against an empty store for the wrong reason.
        fixture.Services.GetRequiredService<IOptions<ProjectionOptions>>()
            .Value.GetRefreshIntervalMs(identity.Domain)
            .ShouldBe(0, "the gateway lane must exercise immediate delivery, not the poller registration path");

        // The configured read-model store must be the DAPR integration adapter. Without this the whole lane
        // could pass against an in-memory fake and prove nothing ADR 0003 asks for.
        IReadModelStore store = fixture.Services.GetRequiredService<IReadModelStore>();
        _ = store.ShouldBeOfType<DaprReadModelStore>();

        // The gateway learned the route from the domain service's own operational-index metadata, so AC4's
        // canonical named route is advertised by the product rather than asserted by this fixture.
        fixture.DiscoveredNamedProjectionTypes.ShouldContain(
            ConversationAsyncProjectionHandler.ConversationReadModelProjectionType);

        EventEnvelope persisted = await AppendConversationAsync(identity, "Gateway production-path proof");

        await fixture.Services
            .GetRequiredService<IProjectionUpdateOrchestrator>()
            .UpdateProjectionAsync(identity, TestContext.Current.CancellationToken);

        ReadModelEntry<ConversationProjectedReadModels> detail = await store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationGatewayLiveFixture.StateStoreName,
                ConversationKey(tenantId, conversationId),
                TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionIndexReadModel> index = await store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationGatewayLiveFixture.StateStoreName,
                TenantIndexKey(tenantId),
                TestContext.Current.CancellationToken);

        detail.Value.ShouldNotBeNull("the gateway delivery must persist the per-conversation read model");
        index.Value.ShouldNotBeNull("the gateway delivery must persist the tenant index");
        detail.Value!.Detail.Freshness.LastAppliedEventPosition.ShouldBe(persisted.SequenceNumber);
        index.Value!.Summaries
            .ShouldHaveSingleItem()
            .ConversationId.Value.ShouldBe(conversationId);

        // Both keys must land on one generation. The query surface exposes cross-key disagreement as
        // Rebuilding rather than repairing it on read, so asserting the generation here is what distinguishes
        // "the gateway delivered" from "the gateway delivered something the query cannot trust".
        detail.Value.Summary.Freshness.ShouldBe(detail.Value.Detail.Freshness);
        index.Value.Summaries[0].Freshness.ShouldBe(detail.Value.Detail.Freshness);
        detail.Value.Detail.Freshness.FreshnessState.ShouldBe(
            ProjectionTrustState.Current,
            $"persisted event type name was '{persisted.EventTypeName}' and the reason code was "
            + $"'{detail.Value.Detail.Freshness.ReasonCode}'");

        (ConversationDetailResult detailResult, ConversationListResult listResult) =
            await QueryAsync(tenantId, conversationId);

        detailResult.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        listResult.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        detailResult.Details.ShouldNotBeNull();
        detailResult.Details!.Freshness.LastAppliedEventPosition.ShouldBe(persisted.SequenceNumber);
        listResult.Conversations.ShouldHaveSingleItem().ConversationId.Value.ShouldBe(conversationId);

        fixture.RecordBoundaryAssertion();
    }

    [Fact]
    public async Task DuplicateGatewayDeliveryShouldConvergeWithoutChangingPersistedState()
    {
        fixture.RequireAvailable();

        string tenantId = "tenant-gateway-002";
        string conversationId = $"conversation-gateway-{Guid.NewGuid():N}";
        AggregateIdentity identity = new(tenantId, ConversationProjectionHandler.ConversationDomain, conversationId);
        IReadModelStore store = fixture.Services.GetRequiredService<IReadModelStore>();
        IProjectionUpdateOrchestrator orchestrator = fixture.Services.GetRequiredService<IProjectionUpdateOrchestrator>();

        _ = await AppendConversationAsync(identity, "Gateway duplicate-delivery proof");
        await orchestrator.UpdateProjectionAsync(identity, TestContext.Current.CancellationToken);

        ReadModelEntry<ConversationProjectedReadModels> first = await store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationGatewayLiveFixture.StateStoreName,
                ConversationKey(tenantId, conversationId),
                TestContext.Current.CancellationToken);
        first.Value.ShouldNotBeNull();

        await orchestrator.UpdateProjectionAsync(identity, TestContext.Current.CancellationToken);

        ReadModelEntry<ConversationProjectedReadModels> second = await store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationGatewayLiveFixture.StateStoreName,
                ConversationKey(tenantId, conversationId),
                TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionIndexReadModel> index = await store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationGatewayLiveFixture.StateStoreName,
                TenantIndexKey(tenantId),
                TestContext.Current.CancellationToken);

        second.Value.ShouldNotBeNull();
        second.Value!.Detail.Freshness.LastAppliedEventPosition
            .ShouldBe(first.Value!.Detail.Freshness.LastAppliedEventPosition);
        index.Value.ShouldNotBeNull();
        index.Value!.Summaries.Count.ShouldBe(1, "a duplicate delivery must not duplicate the tenant index row");

        (ConversationDetailResult detailResult, ConversationListResult listResult) =
            await QueryAsync(tenantId, conversationId);
        detailResult.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        listResult.Conversations.ShouldHaveSingleItem();

        fixture.RecordBoundaryAssertion();
    }

    private static string ConversationKey(string tenantId, string conversationId)
        => $"projection:conversations:{EncodeKeySegment(tenantId)}:{EncodeKeySegment(conversationId)}";

    private static string TenantIndexKey(string tenantId)
        => $"projection:conversations-index:{EncodeKeySegment(tenantId)}";

    private static string EncodeKeySegment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private async Task<EventEnvelope> AppendConversationAsync(AggregateIdentity identity, string label)
    {
        CreateConversation command = new(
            new CreateConversationCommand(
                new ConversationCommandMetadata(
                    SchemaVersion.Current,
                    new TenantId(identity.TenantId),
                    new PartyId("party-gateway-actor"),
                    $"correlation-{identity.AggregateId}",
                    $"causation-{identity.AggregateId}",
                    $"idempotency-{identity.AggregateId}"),
                new BusinessReference("crm", $"case-{identity.AggregateId}"),
                new ProjectId("project-gateway-001"),
                new FolderId("folder-gateway-001"),
                label),
            new ConversationId(identity.AggregateId),
            // Freshness is computed against the applied event's timestamp, so a fixed literal would age past
            // the stale threshold and classify a correctly delivered projection as Stale.
            DateTimeOffset.UtcNow,
            $"event-{identity.AggregateId}-created");

        CommandEnvelope envelope = new CommandEnvelopeBuilder()
            .WithTenantId(identity.TenantId)
            .WithDomain(identity.Domain)
            .WithAggregateId(identity.AggregateId)
            .WithCommandType(nameof(CreateConversation))
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(command, CommandPayloadOptions))
            .WithCorrelationId($"correlation-{identity.AggregateId}")
            .WithUserId("party-gateway-actor")
            .Build();

        IAggregateActor aggregate = fixture.CreateAggregateActor(
            identity.TenantId,
            identity.Domain,
            identity.AggregateId);
        CommandProcessingResult result = await aggregate.ProcessCommandAsync(envelope);
        result.Accepted.ShouldBeTrue(result.ErrorMessage);

        return (await aggregate.GetEventsAsync(0)).ShouldHaveSingleItem();
    }

    private async Task<(ConversationDetailResult Detail, ConversationListResult List)> QueryAsync(
        string tenantId,
        string conversationId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        ConversationQueryHandler handler = scope.ServiceProvider.GetRequiredService<ConversationQueryHandler>();
        TenantId tenant = new(tenantId);
        ConversationDetailResult detail = await handler.GetAsync(
            new GetConversationQuery(
                SchemaVersion.Current,
                tenant,
                "caller-gateway-001",
                $"query-detail-{conversationId}",
                new ConversationId(conversationId)),
            TestContext.Current.CancellationToken);
        ConversationListResult list = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, "caller-gateway-001", $"query-list-{conversationId}"),
            TestContext.Current.CancellationToken);
        return (detail, list);
    }
}

/// <summary>
/// Serializes every live gateway test onto one sidecar so parallel classes cannot fight over the DAPR runtime.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConversationGatewayLiveCollection : ICollectionFixture<ConversationGatewayLiveFixture>
{
    /// <summary>The collection name shared by the live gateway tests.</summary>
    public const string Name = "ConversationGatewayLive";
}
