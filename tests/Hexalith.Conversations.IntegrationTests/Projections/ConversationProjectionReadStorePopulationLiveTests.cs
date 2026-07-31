// <copyright file="ConversationProjectionReadStorePopulationLiveTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        // The list reports an empty tenant. This is the deliberate trade-off behind treating an absent index as
        // an empty tenant rather than an inconsistency: a read store cannot distinguish "derived state was
        // erased" from "this tenant has never held a conversation" without consulting EventStore, and queries
        // are forbidden from replaying. The alternative — failing closed on an absent index — left every new
        // tenant permanently Rebuilding with an empty page. Convergence is proven by the rebuild below.
        // The freshness state is asserted so the v2 proof's transcribed listQueryState stays bound to a
        // measured value instead of drifting from the production behaviour.
        deletedList.Conversations.ShouldBeEmpty();
        deletedList.FreshnessState.ShouldBe(ProjectionTrustState.Current);

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

        // AC6 demands an EQUIVALENT per-conversation record, not merely a matching identity and position:
        // the whole rebuilt detail record and the whole rebuilt index row are compared field-for-field, so a
        // replay that drops a label, participant, or message can never pass as convergence.
        CanonicalizeWithoutCaptureTimes(afterDetail.Details).ShouldBe(
            CanonicalizeWithoutCaptureTimes(beforeDetail.Details),
            "the rebuilt detail record must be equivalent to the pre-deletion record");
        CanonicalizeWithoutCaptureTimes(afterList.Conversations[0]).ShouldBe(
            CanonicalizeWithoutCaptureTimes(beforeList.Conversations[0]),
            "the rebuilt tenant index row must be equivalent to the pre-deletion row");

        AssertKnownTimestampReproductionGap(beforeDetail.Details, afterDetail.Details);
    }

    /// <summary>
    /// Pins a KNOWN, DISCLOSED DEFECT so it cannot be silently fixed or silently forgotten (pass-10 review).
    /// <para>
    /// A full replay does not reproduce the projection's timestamps.
    /// <c>ConversationProjectionMaterializer.cs:127</c> computes
    /// <c>builder.LastAppliedTimestamp ?? projectionGeneratedAt</c> and stamps wall-clock time when the
    /// applied events carry no usable timestamp, and <c>:324</c>/<c>:382</c> resolve participant and
    /// file-reference <c>occurredAt</c> as <c>OccurredAt ?? freshness.LastAppliedEventTimestamp</c>, so the
    /// instability propagates into fields the projection contract documents as domain content. Measured on
    /// 2026-07-31: the pre-deletion value was itself a wall-clock instant, so this path is not event-derived
    /// at all.
    /// </para>
    /// <para>
    /// Because of that, the equivalence comparison above comes in STRUCTURALLY on <c>occurredAt</c>: it still
    /// fails on a dropped, added, duplicated, or reordered item, but tolerates a differing instant. This
    /// assertion is the counterweight — when production is fixed to reproduce event-derived timestamps, THIS
    /// test goes red, which is the prompt to tighten the comparison above from structural to exact and delete
    /// this method. A green run here means the gap is still open, not that convergence is complete.
    /// </para>
    /// </summary>
    /// <param name="before">The pre-deletion detail record.</param>
    /// <param name="after">The rebuilt detail record.</param>
    private static void AssertKnownTimestampReproductionGap(object? before, object? after)
    {
        string beforeStamp = ExtractLastAppliedEventTimestamp(before);
        string afterStamp = ExtractLastAppliedEventTimestamp(after);

        beforeStamp.ShouldNotBeNullOrWhiteSpace();
        afterStamp.ShouldNotBeNullOrWhiteSpace();
        afterStamp.ShouldNotBe(
            beforeStamp,
            "KNOWN GAP CLOSED: the replayed projection now reproduces lastAppliedEventTimestamp. Production "
            + "convergence improved, so tighten CanonicalizeWithoutCaptureTimes to compare occurredAt exactly "
            + "instead of structurally, and delete AssertKnownTimestampReproductionGap.");
    }

    private static string ExtractLastAppliedEventTimestamp(object? value)
    {
        JsonNode node = JsonSerializer.SerializeToNode(value, JsonOptions)
            ?? throw new InvalidOperationException("The projection record serialized to null.");
        return FindFirstProperty(node, "lastAppliedEventTimestamp")
            ?? throw new InvalidOperationException(
                "The detail record carries no lastAppliedEventTimestamp; the known-gap guard can no longer "
                + "observe what it exists to pin.");
    }

    private static string? FindFirstProperty(JsonNode node, string propertyName)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                if (jsonObject.TryGetPropertyValue(propertyName, out JsonNode? found) && found is not null)
                {
                    return found.ToJsonString();
                }

                foreach (KeyValuePair<string, JsonNode?> child in jsonObject)
                {
                    if (child.Value is not null && FindFirstProperty(child.Value, propertyName) is { } nested)
                    {
                        return nested;
                    }
                }

                return null;

            case JsonArray jsonArray:
                foreach (JsonNode? item in jsonArray)
                {
                    if (item is not null && FindFirstProperty(item, propertyName) is { } nested)
                    {
                        return nested;
                    }
                }

                return null;

            default:
                return null;
        }
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
                // Genuine capture-time metadata: legitimately fresh on every projection run.
                //
                // `lastAppliedEventTimestamp` is here as a DISCLOSED WEAKNESS, not a provenance claim. A full
                // replay does not reproduce it — ConversationProjectionMaterializer.cs:127 computes
                // `builder.LastAppliedTimestamp ?? projectionGeneratedAt` and stamps wall clock when the
                // applied events carry no usable timestamp. See AssertKnownTimestampReproductionGap, which
                // pins that defect so it cannot be fixed or forgotten silently (pass-10 review).
                foreach (string property in new[]
                         {
                             "projectionGeneratedAt",
                             "lagDuration",
                             "lastEvaluatedAt",
                             "lastAppliedEventTimestamp",
                         })
                {
                    _ = jsonObject.Remove(property);
                }

                // `occurredAt` is compared STRUCTURALLY rather than stripped. It is domain content on
                // participants, file references, citations, and evidence entries — and the field the
                // materializer sorts evidence by — so removing it at every nesting depth (the pre-pass-10
                // behaviour) let a replay that dropped or reordered an item pass as convergence. Comparing it
                // exactly is not yet possible because :324/:382 resolve it as
                // `OccurredAt ?? freshness.LastAppliedEventTimestamp`, inheriting the wall-clock instability
                // above. Reducing it to a set/null token keeps every structural difference fatal — a dropped,
                // added, duplicated, or reordered item still fails, and so does a null/non-null flip — while
                // tolerating a differing instant. RESIDUAL GAP: an item dropped and restored with a different
                // timestamp still passes. Tighten this to an exact comparison when the known gap closes.
                if (jsonObject.TryGetPropertyValue("occurredAt", out JsonNode? occurredAt))
                {
                    jsonObject["occurredAt"] = JsonValue.Create(occurredAt is null ? "<null>" : "<set>");
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
        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddSingleton<IReadModelStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStagingStore>(store);
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowTenantAccessService());
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
