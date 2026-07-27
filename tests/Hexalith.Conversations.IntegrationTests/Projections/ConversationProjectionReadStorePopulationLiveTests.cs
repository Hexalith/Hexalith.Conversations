// <copyright file="ConversationProjectionReadStorePopulationLiveTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Proves the production named dispatcher populates the configured state store consumed by real detail/list
/// query services, and that full replay restores deleted derived state.
/// </summary>
public sealed class ConversationProjectionReadStorePopulationLiveTests
{
    private const string AppId = "conversations-live-proof";
    private const string ServiceVersion = "v1";
    private const string StateStoreName = "statestore";
    private const string ProjectionType = "conversation-read-model";

    private static readonly TenantId Tenant = new("tenant-live-001");
    private static readonly ConversationId Conversation = new("conversation-live-001");
    private static readonly PartyId Actor = new("party-live-actor");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ProductionDispatchShouldPopulateExactKeysAndCurrentDetailListQueries()
    {
        InMemoryReadModelStore store = new();
        await using WebApplication app = ComposeProductionBoundary(store);
        using IServiceScope scope = app.Services.CreateScope();
        ProjectionDispatchRequest request = DispatchRequest("dispatch-live-001");
        DomainProjectionCatalogRegistry catalog = RegisterCatalog(scope.ServiceProvider, request.CatalogFingerprint);

        ProjectionDispatchResponse accepted = await DomainProjectionDispatcher.DispatchAsync(
            scope.ServiceProvider,
            request,
            new ProjectionDispatchOptions(),
            catalog,
            TestContext.Current.CancellationToken);
        ProjectionDispatchResponse duplicate = await DomainProjectionDispatcher.DispatchAsync(
            scope.ServiceProvider,
            request,
            new ProjectionDispatchOptions(),
            catalog,
            TestContext.Current.CancellationToken);

        accepted.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
        duplicate.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);

        ConversationProjectedReadModels? detailState = store.Snapshot<ConversationProjectedReadModels>(
            StateStoreName,
            ConversationKey());
        ConversationProjectionIndexReadModel? indexState = store.Snapshot<ConversationProjectionIndexReadModel>(
            StateStoreName,
            TenantIndexKey());
        detailState.ShouldNotBeNull();
        indexState.ShouldNotBeNull();
        indexState!.Summaries.ShouldHaveSingleItem().ConversationId.ShouldBe(Conversation);

        (ConversationDetailResult detail, ConversationListResult list) = await QueryAsync(scope.ServiceProvider);
        detail.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        detail.Details.ShouldNotBeNull();
        list.Conversations.ShouldHaveSingleItem().ConversationId.ShouldBe(Conversation);
        detail.Details!.Freshness.LastAppliedEventPosition.ShouldBe(1);
        list.Conversations[0].Freshness.LastAppliedEventPosition.ShouldBe(1);
        detailState!.Detail.Freshness.LastAppliedEventPosition.ShouldBe(1);
        indexState.Summaries[0].Freshness.LastAppliedEventPosition.ShouldBe(1);
    }

    [Fact]
    public async Task DerivedStateDeletionAndFullReplayShouldRestoreEquivalentKeysAndQueries()
    {
        InMemoryReadModelStore store = new();
        await using WebApplication app = ComposeProductionBoundary(store);
        using IServiceScope scope = app.Services.CreateScope();
        ProjectionDispatchRequest appendRequest = DispatchRequest("dispatch-live-before-delete");
        DomainProjectionCatalogRegistry catalog = RegisterCatalog(scope.ServiceProvider, appendRequest.CatalogFingerprint);

        ProjectionDispatchResponse accepted = await DomainProjectionDispatcher.DispatchAsync(
            scope.ServiceProvider,
            appendRequest,
            new ProjectionDispatchOptions(),
            catalog,
            TestContext.Current.CancellationToken);
        accepted.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (ConversationDetailResult beforeDetail, ConversationListResult beforeList) = await QueryAsync(scope.ServiceProvider);

        await EraseAsync(store, ConversationKey());
        await EraseAsync(store, TenantIndexKey());

        (ConversationDetailResult deletedDetail, ConversationListResult deletedList) = await QueryAsync(scope.ServiceProvider);
        deletedDetail.FreshnessState.ShouldNotBe(ProjectionTrustState.Current);
        deletedList.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        deletedList.Conversations.ShouldBeEmpty();

        ProjectionDispatchRequest rebuildRequest = DispatchRequest("rebuild-live-001");
        ProjectionDispatchResponse rebuilt = await DomainProjectionDispatcher.RebuildAsync(
            scope.ServiceProvider,
            rebuildRequest,
            new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = AppId, ServiceVersion = ServiceVersion },
            TestContext.Current.CancellationToken);

        rebuilt.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Snapshot<ConversationProjectedReadModels>(StateStoreName, ConversationKey()).ShouldNotBeNull();
        store.Snapshot<ConversationProjectionIndexReadModel>(StateStoreName, TenantIndexKey()).ShouldNotBeNull();

        (ConversationDetailResult afterDetail, ConversationListResult afterList) = await QueryAsync(scope.ServiceProvider);
        afterDetail.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        afterList.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        afterDetail.Details.ShouldNotBeNull();
        afterList.Conversations.ShouldHaveSingleItem();
        afterDetail.Details!.ConversationId.ShouldBe(beforeDetail.Details!.ConversationId);
        afterDetail.Details.Freshness.LastAppliedEventPosition.ShouldBe(
            beforeDetail.Details.Freshness.LastAppliedEventPosition);
        afterList.Conversations[0].ConversationId.ShouldBe(beforeList.Conversations[0].ConversationId);
        afterList.Conversations[0].Freshness.LastAppliedEventPosition.ShouldBe(
            beforeList.Conversations[0].Freshness.LastAppliedEventPosition);
    }

    private static WebApplication ComposeProductionBoundary(InMemoryReadModelStore store)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddSingleton<IReadModelStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStagingStore>(store);
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowTenantAccessService());
        builder.Services.AddDataProtection();
        builder.Services.AddConversationQueries(options => options.MaxOffset = 100_000);
        return builder.Build();
    }

    private static DomainProjectionCatalogRegistry RegisterCatalog(IServiceProvider services, string fingerprint)
    {
        DomainProjectionCatalogRegistry catalog = services.GetRequiredService<DomainProjectionCatalogRegistry>();
        catalog.Register(
            fingerprint,
            [new ProjectionDispatchRoute(ConversationProjectionHandler.ConversationDomain, ProjectionType)]);
        return catalog;
    }

    private static ProjectionDispatchRequest DispatchRequest(string dispatchId)
    {
        ProjectionDispatchRoute[] routes =
        [
            new(ConversationProjectionHandler.ConversationDomain, ProjectionType),
        ];
        string fingerprint = ProjectionRouteCatalogFingerprint.Compute(AppId, ServiceVersion, routes);
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        ConversationCreated created = new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-live-created-001",
                ConversationEventType.ConversationCreated,
                Tenant,
                Conversation,
                "correlation-live-001",
                occurredAt,
                Actor,
                "causation-live-001"),
            Label: "Live production-path proof");
        ProjectionRequest request = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [new ProjectionEventDto(
                nameof(ConversationCreated),
                JsonSerializer.SerializeToUtf8Bytes(created, JsonOptions),
                "json",
                1,
                occurredAt,
                "correlation-live-001")]);
        return new ProjectionDispatchRequest(request, [ProjectionType], dispatchId, fingerprint);
    }

    private static async Task<(ConversationDetailResult Detail, ConversationListResult List)> QueryAsync(IServiceProvider services)
    {
        ConversationQueryHandler handler = services.GetRequiredService<ConversationQueryHandler>();
        ConversationDetailResult detail = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-live-001", "query-detail-live", Conversation),
            TestContext.Current.CancellationToken);
        ConversationListResult list = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-live-001", "query-list-live"),
            TestContext.Current.CancellationToken);
        return (detail, list);
    }

    private static async Task EraseAsync(InMemoryReadModelStore store, string key)
    {
        (bool present, string etag) = await store.TryReadEtagAsync(
            StateStoreName,
            key,
            TestContext.Current.CancellationToken);
        present.ShouldBeTrue(key);
        (await store.TryEraseAsync(StateStoreName, key, etag, TestContext.Current.CancellationToken)).ShouldBeTrue(key);
    }

    private static string ConversationKey()
        => $"projection:conversations:{Tenant.Value}:{Conversation.Value}";

    private static string TenantIndexKey()
        => $"projection:conversations-index:{Tenant.Value}";

    private sealed class AllowTenantAccessService : IConversationTenantAccessService
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
            => ValueTask.FromResult(
                trustedTenantId == Tenant && routeTenantId == Tenant && !string.IsNullOrWhiteSpace(callerPrincipalId)
                    ? ConversationTenantAccessDecision.Allowed(requirement, Tenant, callerPrincipalId)
                    : ConversationTenantAccessDecision.Denied(
                        requirement,
                        trustedTenantId,
                        callerPrincipalId,
                        ConversationTenantAccessDenialReason.TenantMismatch));
    }
}
