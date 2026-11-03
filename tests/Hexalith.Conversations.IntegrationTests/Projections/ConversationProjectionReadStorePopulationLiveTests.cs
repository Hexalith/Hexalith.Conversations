// <copyright file="ConversationProjectionReadStorePopulationLiveTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Commons.UniqueIds;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
    private static string StateStoreName => ConversationProjectionStoreOptions.FromConfiguration(ComponentConfiguration).StateStoreName;

    private static IConfiguration ComponentConfiguration { get; } = new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            [ConversationProjectionStoreOptions.ConfigurationKey] = "conversations-population-test-state",
        }).Build();
    private const string ProjectionType = "conversation-read-model";

    private static readonly TenantId Tenant = new("tenant-live-001");
    private static readonly ConversationId Conversation = new("conversation-live-001");
    private static readonly PartyId Actor = new("party-live-actor");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // One authoritative history: every dispatch and the full replay below project the same persisted event, so
    // deterministic replay (AD-8) must reproduce its event-derived timestamps exactly.
    private static readonly DateTimeOffset OccurredAt = DateTimeOffset.UtcNow;

    [Fact]
    public async Task ProductionDispatchShouldPopulateExactKeysAndCurrentDetailListQueries()
    {
        InMemoryReadModelStore store = new();
        await using WebApplication app = ComposeProductionBoundary(store);
        using IServiceScope scope = app.Services.CreateScope();

        // Story 16.2 / AD-7: TenantCreated initializes the tenant's index Ready(empty); the first conversation of an
        // initialized tenant then lists as Current.
        await InitializeTenantAsync(scope.ServiceProvider);
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
        await InitializeTenantAsync(scope.ServiceProvider);
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

        // Erase EVERY derived key family, including the dispatch ledger. Leaving the ledger behind would make
        // this a partial deletion and would never exercise the surviving-ledger path (guarded by
        // CompletedLedgerWithoutDurableKeysShouldRePersistInsteadOfReportingAFalseCompletion in Server.Tests).
        await EraseAsync(store, ConversationKey());
        await EraseAsync(store, TenantIndexKey());
        await EraseAsync(store, DispatchLedgerKey("dispatch-live-before-delete"));

        (ConversationDetailResult deletedDetail, ConversationListResult deletedList) = await QueryAsync(scope.ServiceProvider);

        // The detail read cannot serve a generation that is gone.
        deletedDetail.FreshnessState.ShouldNotBe(ProjectionTrustState.Current);
        deletedDetail.Details.ShouldBeNull();

        // Story 16.2 / AD-7: the erased index was the tenant's only lifecycle record. An authorized tenant without
        // it is Unavailable, never an empty Current page (the pass-10 D3 limitation is closed: TenantCreated now
        // writes the record at first use, so a missing record means destroyed state, not an empty tenant).
        deletedList.Conversations.ShouldBeEmpty();
        deletedList.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        deletedList.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Unavailable);

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
        afterDetail.Details.ShouldNotBeNull();
        afterList.Conversations.ShouldHaveSingleItem();

        // Rebuilding one conversation recreates the index but cannot prove tenant-wide coverage, so the restored
        // index is Rebuilding (only TenantCreated creates Ready; tenant-wide rebuild adoption is a follow-up).
        afterList.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        afterList.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Rebuilding);

        // AC6 demands an EQUIVALENT per-conversation record, not merely a matching identity and position:
        // the whole rebuilt detail record and the whole rebuilt index row are compared field-for-field, so a
        // replay that drops a label, participant, or message can never pass as convergence.
        CanonicalizeWithoutCaptureTimes(afterDetail.Details).ShouldBe(
            CanonicalizeWithoutCaptureTimes(beforeDetail.Details),
            "the rebuilt detail record must be equivalent to the pre-deletion record");
        CanonicalizeWithoutCaptureTimes(afterList.Conversations[0]).ShouldBe(
            CanonicalizeWithoutCaptureTimes(beforeList.Conversations[0]),
            "the rebuilt tenant index row must be equivalent to the pre-deletion row");
    }

    private static string CanonicalizeWithoutCaptureTimes<T>(T value)
    {
        JsonNode node = JsonSerializer.SerializeToNode(value, JsonOptions)
            ?? throw new InvalidOperationException("The projection record serialized to null.");
        RemoveCaptureTimeFields(node);
        return node.ToJsonString();
    }

    private static void RemoveCaptureTimeFields(JsonNode node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                // Only genuine observation metadata is removed: it is legitimately fresh on every projection run.
                // Story 16.2 / AD-8 closed the pass-10 timestamp-reproduction gap: lastAppliedEventTimestamp is the
                // replay anchor and every occurredAt is event-derived, so both are now compared exactly and a
                // dropped, added, reordered or re-dated item fails the equivalence.
                foreach (string property in new[]
                         {
                             "projectionGeneratedAt",
                             "lagDuration",
                         })
                {
                    _ = jsonObject.Remove(property);
                }

                foreach (KeyValuePair<string, JsonNode?> child in jsonObject.ToList())
                {
                    if (child.Value is not null)
                    {
                        RemoveCaptureTimeFields(child.Value);
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (JsonNode? item in jsonArray)
                {
                    if (item is not null)
                    {
                        RemoveCaptureTimeFields(item);
                    }
                }

                break;
            default:
                break;
        }
    }

    private static WebApplication ComposeProductionBoundary(InMemoryReadModelStore store)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(ComponentConfiguration);
        builder.Configuration["EventStore:DataProtection:StateStoreName"] = StateStoreName;
        builder.Configuration["EventStore:DataProtection:PersistToStateStore"] = "false";
        builder.Configuration["Hexalith:Conversations:Queries:Cursor:MaxOffset"] = "100000";
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddSingleton<IReadModelStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStagingStore>(store);
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowTenantAccessService());

        // The tenant-access registration contributes the TenantCreated index initializer; the explicit allow-all
        // access service above is kept because the registration only adds it when absent.
        builder.Services.AddConversationTenantAccess();
        builder.Services.AddConversationQueries(builder.Configuration);
        return builder.Build();
    }

    /// <summary>Delivers the tenant's <c>TenantCreated</c> through the production consumer processor.</summary>
    /// <param name="services">The scoped services.</param>
    /// <returns>A task.</returns>
    private static async Task InitializeTenantAsync(IServiceProvider services)
    {
        TenantCreated created = new(Tenant.Value, "Live population tenant", null, OccurredAt);
        EventStoreDomainEventEnvelope envelope = new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            Tenant.Value,
            "system",
            typeof(TenantCreated).FullName!,
            1,
            OccurredAt,
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "json",
            JsonSerializer.SerializeToUtf8Bytes(created))
        {
            Domain = "tenants",
            UserId = "tenants-producer",
        };

        (await services.GetRequiredService<EventStoreDomainEventProcessor>()
                .ProcessAsync(envelope, TestContext.Current.CancellationToken))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
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
        DateTimeOffset occurredAt = OccurredAt;
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
        => $"projection:conversations:{EncodeKeySegment(Tenant.Value)}:{EncodeKeySegment(Conversation.Value)}";

    private static string TenantIndexKey()
        => $"projection:conversations-index:{EncodeKeySegment(Tenant.Value)}";

    private static string EncodeKeySegment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>
    /// Builds the third derived key family this route writes, derived the same way production does so the test
    /// cannot drift from the real key shape.
    /// </summary>
    /// <param name="dispatchId">The stable dispatch identity.</param>
    /// <returns>The dispatch-ledger state-store key.</returns>
    private static string DispatchLedgerKey(string dispatchId)
        => $"projection:conversations-dispatch:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(dispatchId)))}";

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
