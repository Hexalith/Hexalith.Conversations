// <copyright file="DerivedKeyLifecycleTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
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
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Story 12.3 (<c>AC-12.3-03</c>) integration lifecycle, migration, concurrency, replay, rebuild, and rollback tests.
/// </summary>
public sealed class DerivedKeyLifecycleTests
{
    private const string AppId = "conversations-12-3-lifecycle";
    private const string ServiceVersion = "v1";
    private const string ProjectionType = "conversation-read-model";
    private const string Caller = "caller-lifecycle-12-3";

    private static readonly TenantId TenantA = new("tenant-lifecycle-a");
    private static readonly TenantId TenantB = new("tenant-lifecycle-b");
    private static readonly ConversationId SharedConversation = new("conv-shared-001");
    private static readonly PartyId Actor = new("party-lifecycle-actor");
    private static readonly DateTimeOffset OccurredAt = new(2026, 11, 27, 11, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static IConfiguration ComponentConfiguration { get; } = new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            [ConversationProjectionStoreOptions.ConfigurationKey] = "conversations-12-3-lifecycle-state",
        }).Build();

    private static string StateStoreName => ConversationProjectionStoreOptions.FromConfiguration(ComponentConfiguration).StateStoreName;

    /// <summary>
    /// Tenants with colliding base64url-adjacent names and identical conversation identifiers remain strictly isolated,
    /// and cursors never cross tenant boundaries (<c>TENANT_DISCLOSURE</c>).
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task TenantCollisionsShouldNeverDiscloseAcrossTenants()
    {
        EmitCandidateBinding();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore store = new();
        await using WebApplication app = ComposeHost(store, new DerivedKeyLifecycleFixedTimeProvider(OccurredAt.AddSeconds(5)), TenantA, TenantB);
        using IServiceScope scope = app.Services.CreateScope();

        await InitializeTenantAsync(scope.ServiceProvider, TenantA, cancellationToken);
        await InitializeTenantAsync(scope.ServiceProvider, TenantB, cancellationToken);

        for (int index = 0; index < 4; index++)
        {
            ConversationId convId = index == 0 ? SharedConversation : new ConversationId($"conv-iso-{index:00}");
            await DispatchCreatedAsync(scope.ServiceProvider, TenantA, convId, $"dispatch-ta-{index}", $"Secret Tenant A {index}", 1, OccurredAt.AddSeconds(index), cancellationToken);
            await DispatchCreatedAsync(scope.ServiceProvider, TenantB, convId, $"dispatch-tb-{index}", $"Secret Tenant B {index}", 1, OccurredAt.AddSeconds(index), cancellationToken);
        }

        ConversationQueryHandler handler = scope.ServiceProvider.GetRequiredService<ConversationQueryHandler>();

        ConversationDetailResult detailA = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-a", SharedConversation),
            cancellationToken);
        ConversationDetailResult detailB = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantB, Caller, "corr-b", SharedConversation),
            cancellationToken);

        detailA.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        detailB.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        detailA.Details!.TenantId.ShouldBe(TenantA);
        detailA.Details.Label.ShouldBe("Secret Tenant A 0");
        detailB.Details!.TenantId.ShouldBe(TenantB);
        detailB.Details.Label.ShouldBe("Secret Tenant B 0");

        ConversationListResult pageA1 = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-la1", Page: new ConversationPageRequest(2)),
            cancellationToken);
        pageA1.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        pageA1.Conversations.ShouldAllBe(c => c.TenantId == TenantA && c.Label!.StartsWith("Secret Tenant A", StringComparison.Ordinal));
        pageA1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();

        // Presenting Tenant A's cursor to Tenant B must fail closed with zero disclosure.
        ConversationListResult crossTenantReplay = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantB, Caller, "corr-cross", Page: new ConversationPageRequest(2, pageA1.Page.ContinuationCursor)),
            cancellationToken);
        crossTenantReplay.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden, "TENANT_DISCLOSURE: cross-tenant cursor accepted");
        crossTenantReplay.Conversations.ShouldBeEmpty();
    }

    /// <summary>
    /// Retries, delayed out-of-order deliveries, and equal-position conflicting dispatches converge deterministically
    /// with stable tokens on retries and cursor invalidation on real mutations.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RetriesOutOfOrderAndEqualPositionConflictsShouldConvergeDeterministically()
    {
        EmitCandidateBinding();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore store = new();
        await using WebApplication app = ComposeHost(store, new DerivedKeyLifecycleFixedTimeProvider(OccurredAt.AddSeconds(10)), TenantA);
        using IServiceScope scope = app.Services.CreateScope();
        await InitializeTenantAsync(scope.ServiceProvider, TenantA, cancellationToken);

        ConversationId conv1 = new("conv-order-01");
        ConversationId conv2 = new("conv-order-02");
        ConversationId conv3 = new("conv-order-03");

        await DispatchCreatedAsync(scope.ServiceProvider, TenantA, conv1, "dispatch-c1-p1", "Conv 1", 1, OccurredAt, cancellationToken);
        await DispatchCreatedAsync(scope.ServiceProvider, TenantA, conv2, "dispatch-c2-p1", "Conv 2", 1, OccurredAt.AddSeconds(1), cancellationToken);
        await DispatchCreatedAsync(scope.ServiceProvider, TenantA, conv3, "dispatch-c3-p1", "Conv 3", 1, OccurredAt.AddSeconds(2), cancellationToken);

        ConversationQueryHandler handler = scope.ServiceProvider.GetRequiredService<ConversationQueryHandler>();
        ConversationListResult page1 = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-p1", Page: new ConversationPageRequest(2)),
            cancellationToken);
        page1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();

        // Exact retry must be idempotent and keep the cursor valid.
        await DispatchCreatedAsync(scope.ServiceProvider, TenantA, conv1, "dispatch-c1-p1", "Conv 1", 1, OccurredAt, cancellationToken);
        ConversationListResult page2AfterRetry = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-p2", Page: new ConversationPageRequest(2, page1.Page.ContinuationCursor)),
            cancellationToken);
        page2AfterRetry.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        page2AfterRetry.Conversations.Count.ShouldBe(1);

        // Out-of-order: deliver position 2, then stale position 1 -> position 2 wins, cursor invalidated.
        await DispatchUpdatedAsync(scope.ServiceProvider, TenantA, conv1, "dispatch-c1-p2", "Conv 1 v2", 2, OccurredAt.AddSeconds(3), cancellationToken);
        await DispatchCreatedAsync(scope.ServiceProvider, TenantA, conv1, "dispatch-c1-p1-late", "Conv 1 stale", 1, OccurredAt, cancellationToken);

        ConversationDetailResult openAfterOutOfOrder = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-ooo", conv1),
            cancellationToken);
        openAfterOutOfOrder.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        openAfterOutOfOrder.Details!.Label.ShouldBe("Conv 1 v2");
        openAfterOutOfOrder.Details.Freshness.LastAppliedEventPosition.ShouldBe(2);

        ConversationListResult staleCursorResult = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-stale", Page: new ConversationPageRequest(2, page1.Page.ContinuationCursor)),
            cancellationToken);
        staleCursorResult.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden, "mutation must invalidate prior generation cursor");
        staleCursorResult.Conversations.ShouldBeEmpty();

        // Equal-position conflict: two stores receiving dispatches at same position in opposite orders converge to the same winner.
        ConversationProjectedReadModels winnerAB = await RunEqualPositionConflictAsync("dispatch-eq-a", "dispatch-eq-b", cancellationToken);
        ConversationProjectedReadModels winnerBA = await RunEqualPositionConflictAsync("dispatch-eq-b", "dispatch-eq-a", cancellationToken);
        winnerAB.DispatchId.ShouldBe(winnerBA.DispatchId, "REPLAY_NONDETERMINISTIC: equal-position tie-breaker must be order-independent");
        winnerAB.Summary.Label.ShouldBe(winnerBA.Summary.Label);
    }

    /// <summary>
    /// Legacy v1 indexes, v2 indexes without persisted <c>GenerationToken</c>, and v2 indexes with persisted
    /// <c>GenerationToken</c> produce identical generation tokens and upgrade safely (<c>DERIVED_KEY_TRANSITION_UNSAFE</c>).
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LegacyV1AndV2WithoutPersistedTokenShouldMatchPersistedGenerationToken()
    {
        EmitCandidateBinding();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore store = new();
        await using WebApplication app = ComposeHost(store, new DerivedKeyLifecycleFixedTimeProvider(OccurredAt.AddSeconds(10)), TenantA);
        using IServiceScope scope = app.Services.CreateScope();
        await InitializeTenantAsync(scope.ServiceProvider, TenantA, cancellationToken);

        for (int index = 0; index < 3; index++)
        {
            await DispatchCreatedAsync(
                scope.ServiceProvider,
                TenantA,
                new ConversationId($"conv-mig-{index:00}"),
                $"dispatch-mig-{index:00}",
                $"Migration {index}",
                1,
                OccurredAt.AddSeconds(index),
                cancellationToken);
        }

        ConversationQueryHandler handler = scope.ServiceProvider.GetRequiredService<ConversationQueryHandler>();
        ConversationListResult candidatePage1 = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-v2", Page: new ConversationPageRequest(2)),
            cancellationToken);
        candidatePage1.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        candidatePage1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();
        string cursorFromCandidate = candidatePage1.Page.ContinuationCursor!;

        // Pin pre-deployment compatibility: compute the exact pre-Story-12.3 digest
        // (SHA256(JsonSerializer.SerializeToUtf8Bytes(summaries.OrderBy(ConversationId)))) and verify the
        // candidate accepts an unexpired pre-deployment cursor when protection keys and index are unchanged.
        ConversationProjectionIndexReadModel persistedIndex = store.Snapshot<ConversationProjectionIndexReadModel>(
            StateStoreName,
            TenantIndexKey(TenantA))!;
        string previousDigest = Convert.ToHexStringLower(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(
                persistedIndex.Summaries.OrderBy(static s => s.ConversationId.Value, StringComparer.Ordinal))));
        IQueryCursorCodec cursorCodec = scope.ServiceProvider.GetRequiredService<IQueryCursorCodec>();
        string preDeploymentCursor = cursorCodec.Encode(
            ConversationListCursor.QueryType,
            ConversationListCursor.BuildScope(TenantA, Caller, new ConversationListFilterV1()),
            ConversationListCursor.EncodePosition(2, OccurredAt.AddSeconds(9), previousDigest));

        ConversationListResult page2WithPreDeploymentCursor = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-pre-deploy-cursor", Page: new ConversationPageRequest(2, preDeploymentCursor)),
            cancellationToken);
        page2WithPreDeploymentCursor.FreshnessState.ShouldBe(
            ProjectionTrustState.Current,
            "DERIVED_KEY_TRANSITION_UNSAFE: candidate must accept unexpired pre-deployment cursor for unchanged index");
        page2WithPreDeploymentCursor.Conversations.Count.ShouldBe(1);

        // Verify v2 index without GenerationToken matches the candidate cursor.
        JsonObject persistedJson = store.Snapshot<JsonObject>(StateStoreName, TenantIndexKey(TenantA))!;
        _ = persistedJson.Remove("generationToken");
        store.SeedRaw(StateStoreName, TenantIndexKey(TenantA), persistedJson);

        ConversationListResult page2WithoutPersistedToken = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-v2-fallback", Page: new ConversationPageRequest(2, cursorFromCandidate)),
            cancellationToken);
        page2WithoutPersistedToken.FreshnessState.ShouldBe(ProjectionTrustState.Current, "DERIVED_KEY_TRANSITION_UNSAFE: fallback token must match candidate token");
        page2WithoutPersistedToken.Conversations.Count.ShouldBe(1);

        // Seed v1 record (no SchemaVersion/Lifecycle/Watermarks/GenerationToken): reads as Rebuilding, upgrades safely on write.
        JsonObject v1Json = persistedJson.DeepClone().AsObject();
        _ = v1Json.Remove("schemaVersion");
        _ = v1Json.Remove("lifecycle");
        _ = v1Json.Remove("watermarks");
        store.SeedRaw(StateStoreName, TenantIndexKey(TenantA), v1Json);
        ConversationListResult legacyV1Read = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-v1", Page: new ConversationPageRequest(2, preDeploymentCursor)),
            cancellationToken);
        legacyV1Read.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        legacyV1Read.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Rebuilding);
        legacyV1Read.Conversations.Count.ShouldBe(1);

        // Next live write upgrades v1 -> v2 Rebuilding with seeded watermarks.
        await DispatchCreatedAsync(
            scope.ServiceProvider,
            TenantA,
            new ConversationId("conv-mig-03"),
            "dispatch-mig-03",
            "Migration 3",
            1,
            OccurredAt.AddSeconds(4),
            cancellationToken);
        ConversationProjectionIndexReadModel upgraded = store.Snapshot<ConversationProjectionIndexReadModel>(
            StateStoreName,
            TenantIndexKey(TenantA))!;
        upgraded.Summaries.Count.ShouldBe(4);
        ConversationListResult afterUpgrade = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-after-upgrade"),
            cancellationToken);
        afterUpgrade.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        afterUpgrade.Conversations.Count.ShouldBe(4);
    }

    /// <summary>
    /// Concurrent projection writes under one tenant converge without lost updates.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ConcurrentWritesShouldConvergeWithoutLostUpdates()
    {
        EmitCandidateBinding();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore store = new();
        await using WebApplication app = ComposeHost(store, new DerivedKeyLifecycleFixedTimeProvider(OccurredAt.AddSeconds(10)), TenantA);
        using IServiceScope initScope = app.Services.CreateScope();
        await InitializeTenantAsync(initScope.ServiceProvider, TenantA, cancellationToken);

        Task[] writes = Enumerable.Range(0, 8).Select(async index =>
        {
            using IServiceScope workerScope = app.Services.CreateScope();
            await DispatchCreatedAsync(
                workerScope.ServiceProvider,
                TenantA,
                new ConversationId($"conv-concurrent-{index:00}"),
                $"dispatch-concurrent-{index:00}",
                $"Concurrent {index}",
                1,
                OccurredAt.AddSeconds(index),
                cancellationToken);
        }).ToArray();

        await Task.WhenAll(writes);

        using IServiceScope verifyScope = app.Services.CreateScope();
        ConversationQueryHandler handler = verifyScope.ServiceProvider.GetRequiredService<ConversationQueryHandler>();
        ConversationListResult list = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-concurrent"),
            cancellationToken);
        list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        list.Conversations.Count.ShouldBe(8);
    }

    /// <summary>
    /// Deletion, interrupted rebuild recovery, deterministic multi-clock replay, and rollback rehearsal
    /// (<c>redeploy-then-rebuild</c>) converge cleanly (<c>REPLAY_NONDETERMINISTIC</c>).
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task DeletionInterruptedRebuildReplayAndRollbackRehearsalShouldConvergeDeterministically()
    {
        EmitCandidateBinding();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore storeA = new();
        DerivedKeyLifecycleFixedTimeProvider clockA = new(OccurredAt.AddSeconds(10));
        EphemeralDataProtectionProvider sharedProtection = new();
        await using WebApplication appA = ComposeHost(storeA, clockA, sharedProtection, TenantA);
        using IServiceScope scopeA = appA.Services.CreateScope();
        await InitializeTenantAsync(scopeA.ServiceProvider, TenantA, cancellationToken);

        ConversationId siblingConversation = new("conv-rb-sibling-002");
        await DispatchCreatedAsync(scopeA.ServiceProvider, TenantA, SharedConversation, "dispatch-rb-01", "Rollback Target", 1, OccurredAt, cancellationToken);
        await DispatchCreatedAsync(scopeA.ServiceProvider, TenantA, siblingConversation, "dispatch-rb-02", "Rollback Sibling", 1, OccurredAt.AddSeconds(1), cancellationToken);
        ConversationQueryHandler handlerA = scopeA.ServiceProvider.GetRequiredService<ConversationQueryHandler>();
        ConversationDetailResult beforeDelete = await handlerA.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-before", SharedConversation),
            cancellationToken);
        beforeDelete.FreshnessState.ShouldBe(ProjectionTrustState.Current);

        ConversationListResult candidatePage1BeforeRollback = await handlerA.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-cand-p1", Page: new ConversationPageRequest(1)),
            cancellationToken);
        candidatePage1BeforeRollback.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        candidatePage1BeforeRollback.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();

        // Operator delete of all derived keys for both conversations.
        await EraseAllDerivedKeysAsync(storeA, TenantA, SharedConversation, "dispatch-rb-01", cancellationToken);
        await EraseAllDerivedKeysAsync(storeA, TenantA, siblingConversation, "dispatch-rb-02", cancellationToken);
        ConversationListResult deletedList = await handlerA.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-del"),
            cancellationToken);
        deletedList.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);

        // Interrupted rebuild (fault injected during batch execution): must fail closed without reporting completion
        // and both List and Open must withhold the incomplete generation before the fault clears.
        bool faultedOnce = false;
        storeA.BatchFaultHook = (_, _, _) =>
        {
            if (!faultedOnce)
            {
                faultedOnce = true;
                throw new InvalidOperationException("Simulated mid-rebuild crash.");
            }

            return Task.CompletedTask;
        };

        ProjectionDispatchRequest rebuildRequest = CreateRequest(TenantA, SharedConversation, "rebuild-rb-01", "Rollback Target", 1, OccurredAt);
        ProjectionDispatchResponse interrupted = await DomainProjectionDispatcher.RebuildAsync(
            scopeA.ServiceProvider,
            rebuildRequest,
            new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = AppId, ServiceVersion = ServiceVersion },
            cancellationToken);
        interrupted.Outcomes.ShouldHaveSingleItem().Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        faultedOnce.ShouldBeTrue();

        // Verify read outcomes DURING interrupted rebuild BEFORE clearing the fault.
        ConversationListResult listDuringInterruptedRebuild = await handlerA.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-interrupted-list"),
            cancellationToken);
        listDuringInterruptedRebuild.FreshnessState.ShouldNotBe(ProjectionTrustState.Current);
        listDuringInterruptedRebuild.Conversations.ShouldNotContain(c => c.ConversationId == SharedConversation);

        ConversationDetailResult openDuringInterruptedRebuild = await handlerA.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-interrupted-open", SharedConversation),
            cancellationToken);
        openDuringInterruptedRebuild.FreshnessState.ShouldNotBe(ProjectionTrustState.Current);
        openDuringInterruptedRebuild.Details.ShouldBeNull();

        // Retry rebuild after fault clears -> converges cleanly.
        storeA.BatchFaultHook = null;
        ProjectionDispatchResponse rebuiltA = await DomainProjectionDispatcher.RebuildAsync(
            scopeA.ServiceProvider,
            rebuildRequest,
            new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = AppId, ServiceVersion = ServiceVersion },
            cancellationToken);
        rebuiltA.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);

        // Also verify interrupted rebuild on a POPULATED tenant index: leave siblingConversation readable while
        // injecting a mid-batch fault when rebuilding a new conversation, confirming BOTH List and Open withhold
        // the incomplete generation BEFORE clearing the fault.
        InMemoryReadModelStore populatedStore = new();
        await using WebApplication populatedApp = ComposeHost(populatedStore, clockA, sharedProtection, TenantA);
        using IServiceScope populatedScope = populatedApp.Services.CreateScope();
        await InitializeTenantAsync(populatedScope.ServiceProvider, TenantA, cancellationToken);
        await DispatchCreatedAsync(populatedScope.ServiceProvider, TenantA, siblingConversation, "dispatch-pop-sib", "Healthy Sibling", 1, OccurredAt, cancellationToken);

        ConversationId interruptedPopId = new("conv-rb-interrupted-pop");
        bool popFaultedOnce = false;
        populatedStore.BatchFaultHook = (_, _, _) =>
        {
            if (!popFaultedOnce)
            {
                popFaultedOnce = true;
                throw new InvalidOperationException("Simulated mid-rebuild crash on populated index.");
            }

            return Task.CompletedTask;
        };
        ProjectionDispatchRequest popRebuildReq = CreateRequest(TenantA, interruptedPopId, "rebuild-pop-01", "Interrupted Target", 1, OccurredAt.AddSeconds(2));
        ProjectionDispatchResponse popInterrupted = await DomainProjectionDispatcher.RebuildAsync(
            populatedScope.ServiceProvider,
            popRebuildReq,
            new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = AppId, ServiceVersion = ServiceVersion },
            cancellationToken);
        popInterrupted.Outcomes.ShouldHaveSingleItem().Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        popFaultedOnce.ShouldBeTrue();

        ConversationQueryHandler populatedHandler = populatedScope.ServiceProvider.GetRequiredService<ConversationQueryHandler>();
        ConversationListResult popListDuringFault = await populatedHandler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-pop-int-list"),
            cancellationToken);
        popListDuringFault.Conversations.ShouldNotContain(c => c.ConversationId == interruptedPopId);
        ConversationDetailResult popOpenDuringFault = await populatedHandler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-pop-int-open", interruptedPopId),
            cancellationToken);
        popOpenDuringFault.FreshnessState.ShouldNotBe(ProjectionTrustState.Current);
        popOpenDuringFault.Details.ShouldBeNull();

        populatedStore.BatchFaultHook = null;
        ProjectionDispatchResponse popRebuilt = await DomainProjectionDispatcher.RebuildAsync(
            populatedScope.ServiceProvider,
            popRebuildReq,
            new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = AppId, ServiceVersion = ServiceVersion },
            cancellationToken);
        popRebuilt.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ConversationDetailResult popOpenAfterRetry = await populatedHandler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-pop-rec-open", interruptedPopId),
            cancellationToken);
        popOpenAfterRetry.FreshnessState.ShouldBe(ProjectionTrustState.Current);

        // Real prior-binary rollback rehearsal (redeploy pre-Story-12.3 binary from commit b7c12907, then rebuild
        // from EventStore under a different wall clock without reverse-writing derived state).
        using DerivedKeyPriorBinaryRollbackHarness priorHarness = DerivedKeyPriorBinaryRollbackHarness.LoadPriorBinary();

        // First prove the prior binary reads unchanged candidate-written state and accepts the candidate's
        // continuation cursor (cross-binary deployment/rollback cursor continuity).
        InMemoryReadModelStore crossBinaryStore = new();
        await using WebApplication candidateCrossApp = ComposeHost(crossBinaryStore, clockA, sharedProtection, TenantA);
        using IServiceScope candidateCrossScope = candidateCrossApp.Services.CreateScope();
        await InitializeTenantAsync(candidateCrossScope.ServiceProvider, TenantA, cancellationToken);
        await DispatchCreatedAsync(candidateCrossScope.ServiceProvider, TenantA, SharedConversation, "dispatch-rb-01", "Rollback Target", 1, OccurredAt, cancellationToken);
        await DispatchCreatedAsync(candidateCrossScope.ServiceProvider, TenantA, siblingConversation, "dispatch-rb-02", "Rollback Sibling", 1, OccurredAt.AddSeconds(1), cancellationToken);
        ConversationQueryHandler candidateCrossHandler = candidateCrossScope.ServiceProvider.GetRequiredService<ConversationQueryHandler>();
        ConversationListResult candidateCrossP1 = await candidateCrossHandler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-xb-cand-p1", Page: new ConversationPageRequest(1)),
            cancellationToken);
        candidateCrossP1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();

        await using WebApplication priorCrossApp = priorHarness.ComposePriorHost(
            crossBinaryStore,
            ComponentConfiguration,
            StateStoreName,
            clockA,
            sharedProtection,
            TenantA);
        using IServiceScope priorCrossScope = priorCrossApp.Services.CreateScope();
        ConversationListResult priorCrossP2 = await priorHarness.ListAsync(
            priorCrossScope.ServiceProvider,
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-xb-prior-p2", Page: new ConversationPageRequest(1, candidateCrossP1.Page.ContinuationCursor)),
            cancellationToken);
        priorCrossP2.FreshnessState.ShouldBe(
            ProjectionTrustState.Current,
            "DERIVED_KEY_TRANSITION_UNSAFE: prior binary must accept candidate cursor on unchanged state");
        priorCrossP2.Conversations.Count.ShouldBe(1);

        ConversationListResult priorIssuedP1 = await priorHarness.ListAsync(
            priorCrossScope.ServiceProvider,
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-xb-prior-p1", Page: new ConversationPageRequest(1)),
            cancellationToken);
        priorIssuedP1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();
        ConversationListResult candidateAcceptsPriorP2 = await candidateCrossHandler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-xb-cand-p2", Page: new ConversationPageRequest(1, priorIssuedP1.Page.ContinuationCursor)),
            cancellationToken);
        candidateAcceptsPriorP2.FreshnessState.ShouldBe(
            ProjectionTrustState.Current,
            "DERIVED_KEY_TRANSITION_UNSAFE: candidate binary must accept prior binary cursor on unchanged state");
        candidateAcceptsPriorP2.Conversations.Count.ShouldBe(1);

        // Now execute full redeploy-then-rebuild on the prior binary: delete derived keys, redeploy prior binary
        // on a different wall clock, rebuild from EventStore, and inspect persisted keys + List/Open outcomes.
        await EraseAllDerivedKeysAsync(crossBinaryStore, TenantA, SharedConversation, "dispatch-rb-01", cancellationToken);
        await EraseAllDerivedKeysAsync(crossBinaryStore, TenantA, siblingConversation, "dispatch-rb-02", cancellationToken);

        DerivedKeyLifecycleFixedTimeProvider clockB = new(OccurredAt.AddSeconds(45));
        await using WebApplication priorRollbackApp = priorHarness.ComposePriorHost(
            crossBinaryStore,
            ComponentConfiguration,
            StateStoreName,
            clockB,
            sharedProtection,
            TenantA);
        using IServiceScope priorRollbackScope = priorRollbackApp.Services.CreateScope();
        ProjectionDispatchResponse rebuiltByPrior = await DomainProjectionDispatcher.RebuildAsync(
            priorRollbackScope.ServiceProvider,
            rebuildRequest,
            new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = AppId, ServiceVersion = ServiceVersion },
            cancellationToken);
        rebuiltByPrior.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);

        // Verify persisted derived-key state written by the prior binary matches ADR 0005 keys and structure.
        JsonObject? priorPersistedConversation = crossBinaryStore.Snapshot<JsonObject>(StateStoreName, ConversationKey(TenantA, SharedConversation));
        JsonObject? priorPersistedIndex = crossBinaryStore.Snapshot<JsonObject>(StateStoreName, TenantIndexKey(TenantA));
        JsonObject? priorPersistedLedger = crossBinaryStore.Snapshot<JsonObject>(StateStoreName, DispatchLedgerKey("rebuild-rb-01"));
        JsonObject? priorPersistedProof = crossBinaryStore.Snapshot<JsonObject>(StateStoreName, ResolutionKey("projection:conversations-resolution-proof", TenantA, SharedConversation));
        priorPersistedConversation.ShouldNotBeNull();
        priorPersistedIndex.ShouldNotBeNull();
        priorPersistedLedger.ShouldNotBeNull();
        priorPersistedProof.ShouldNotBeNull();

        ConversationDetailResult afterA = await handlerA.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-after-a", SharedConversation),
            cancellationToken);
        ConversationDetailResult afterPriorOpen = await priorHarness.GetAsync(
            priorRollbackScope.ServiceProvider,
            new GetConversationQuery(SchemaVersion.Current, TenantA, Caller, "corr-after-prior-open", SharedConversation),
            cancellationToken);
        ConversationListResult afterPriorList = await priorHarness.ListAsync(
            priorRollbackScope.ServiceProvider,
            new ListConversationsQuery(SchemaVersion.Current, TenantA, Caller, "corr-after-prior-list"),
            cancellationToken);

        afterA.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        afterPriorOpen.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        afterPriorList.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        afterPriorList.Conversations.ShouldContain(c => c.ConversationId == SharedConversation);
        CanonicalizeWithoutObservationTimes(afterA.Details!).ShouldBe(
            CanonicalizeWithoutObservationTimes(beforeDelete.Details!),
            "REPLAY_NONDETERMINISTIC: rebuild must reproduce pre-deletion detail");
        CanonicalizeWithoutObservationTimes(afterPriorOpen.Details!).ShouldBe(
            CanonicalizeWithoutObservationTimes(afterA.Details!),
            "REPLAY_NONDETERMINISTIC: prior-binary rollback rebuild under a different clock must match candidate rebuild");
    }

    private static async Task<ConversationProjectedReadModels> RunEqualPositionConflictAsync(
        string firstDispatch,
        string secondDispatch,
        CancellationToken cancellationToken)
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store);
        ConversationProjectionMaterializer materializer = new();

        ConversationProjectedReadModels first = MaterializeSingle(materializer, TenantA, SharedConversation, firstDispatch, $"Label {firstDispatch}", 1);
        ConversationProjectedReadModels second = MaterializeSingle(materializer, TenantA, SharedConversation, secondDispatch, $"Label {secondDispatch}", 1);
        await writer.PersistAsync(first, cancellationToken);
        await writer.PersistAsync(second, cancellationToken);

        return store.Snapshot<ConversationProjectedReadModels>(
            "statestore",
            ConversationKey(TenantA, SharedConversation))!;
    }

    private static ConversationProjectedReadModels MaterializeSingle(
        ConversationProjectionMaterializer materializer,
        TenantId tenant,
        ConversationId conversationId,
        string dispatchId,
        string label,
        long sequence)
    {
        ConversationCreated created = new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                $"event-{dispatchId}",
                ConversationEventType.ConversationCreated,
                tenant,
                conversationId,
                $"corr-{dispatchId}",
                OccurredAt,
                Actor,
                $"caus-{dispatchId}"),
            Label: label);
        ConversationProjectedReadModels projected = materializer.Project(
            tenant,
            conversationId,
            [new ConversationProjectionEventRecord(sequence, created)],
            OccurredAt.AddSeconds(1),
            TimeSpan.FromMinutes(5));
        return projected with { DispatchId = dispatchId };
    }

    private static WebApplication ComposeHost(
        InMemoryReadModelStore store,
        TimeProvider timeProvider,
        params TenantId[] allowedTenants)
        => ComposeHost(store, timeProvider, dataProtectionProvider: null, allowedTenants);

    private static WebApplication ComposeHost(
        InMemoryReadModelStore store,
        TimeProvider timeProvider,
        IDataProtectionProvider? dataProtectionProvider,
        params TenantId[] allowedTenants)
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
        builder.Services.AddSingleton(timeProvider);
        builder.Services.AddSingleton<IConversationTenantAccessService>(new DerivedKeyLifecycleTenantAccess(allowedTenants));
        builder.Services.AddConversationTenantAccess();
        builder.Services.AddConversationQueries(builder.Configuration);
        if (dataProtectionProvider is not null)
        {
            builder.Services.AddSingleton(dataProtectionProvider);
        }

        return builder.Build();
    }

    private static async Task InitializeTenantAsync(IServiceProvider services, TenantId tenant, CancellationToken cancellationToken)
    {
        TenantCreated created = new(tenant.Value, $"Tenant {tenant.Value}", null, OccurredAt);
        EventStoreDomainEventEnvelope envelope = new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            tenant.Value,
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
                .ProcessAsync(envelope, cancellationToken))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
    }

    private static async Task DispatchCreatedAsync(
        IServiceProvider services,
        TenantId tenant,
        ConversationId conversationId,
        string dispatchId,
        string label,
        long sequence,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        ProjectionDispatchRequest request = CreateRequest(tenant, conversationId, dispatchId, label, sequence, occurredAt);
        DomainProjectionCatalogRegistry catalog = services.GetRequiredService<DomainProjectionCatalogRegistry>();
        catalog.Register(
            request.CatalogFingerprint,
            [new ProjectionDispatchRoute(ConversationProjectionHandler.ConversationDomain, ProjectionType)]);
        ProjectionDispatchResponse response = await DomainProjectionDispatcher.DispatchAsync(
            services,
            request,
            new ProjectionDispatchOptions(),
            catalog,
            cancellationToken);
        response.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
    }

    private static async Task DispatchUpdatedAsync(
        IServiceProvider services,
        TenantId tenant,
        ConversationId conversationId,
        string dispatchId,
        string updatedLabel,
        long sequence,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        ProjectionDispatchRoute[] routes = [new(ConversationProjectionHandler.ConversationDomain, ProjectionType)];
        string fingerprint = ProjectionRouteCatalogFingerprint.Compute(AppId, ServiceVersion, routes);
        ConversationCreated created = new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                $"evt-create-{conversationId.Value}",
                ConversationEventType.ConversationCreated,
                tenant,
                conversationId,
                $"corr-create-{conversationId.Value}",
                OccurredAt,
                Actor,
                $"caus-create-{conversationId.Value}"),
            Label: "Initial");
        ConversationMetadataUpdated updated = new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                $"evt-upd-{dispatchId}",
                ConversationEventType.ConversationMetadataUpdated,
                tenant,
                conversationId,
                $"corr-upd-{dispatchId}",
                occurredAt,
                Actor,
                $"caus-upd-{dispatchId}"),
            Label: updatedLabel);
        ProjectionRequest projectionRequest = new(
            tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            conversationId.Value,
            [
                new ProjectionEventDto(nameof(ConversationCreated), JsonSerializer.SerializeToUtf8Bytes(created, JsonOptions), "json", 1, OccurredAt, $"corr-create-{conversationId.Value}"),
                new ProjectionEventDto(nameof(ConversationMetadataUpdated), JsonSerializer.SerializeToUtf8Bytes(updated, JsonOptions), "json", sequence, occurredAt, $"corr-upd-{dispatchId}"),
            ]);
        ProjectionDispatchRequest request = new(projectionRequest, [ProjectionType], dispatchId, fingerprint);
        DomainProjectionCatalogRegistry catalog = services.GetRequiredService<DomainProjectionCatalogRegistry>();
        catalog.Register(fingerprint, routes);
        ProjectionDispatchResponse response = await DomainProjectionDispatcher.DispatchAsync(
            services,
            request,
            new ProjectionDispatchOptions(),
            catalog,
            cancellationToken);
        response.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
    }

    private static ProjectionDispatchRequest CreateRequest(
        TenantId tenant,
        ConversationId conversationId,
        string dispatchId,
        string label,
        long sequence,
        DateTimeOffset occurredAt)
    {
        ProjectionDispatchRoute[] routes = [new(ConversationProjectionHandler.ConversationDomain, ProjectionType)];
        string fingerprint = ProjectionRouteCatalogFingerprint.Compute(AppId, ServiceVersion, routes);
        ConversationCreated created = new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                $"evt-{conversationId.Value}-{sequence}",
                ConversationEventType.ConversationCreated,
                tenant,
                conversationId,
                $"corr-{dispatchId}",
                occurredAt,
                Actor,
                $"caus-{dispatchId}"),
            Label: label);
        ProjectionRequest request = new(
            tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            conversationId.Value,
            [new ProjectionEventDto(
                nameof(ConversationCreated),
                JsonSerializer.SerializeToUtf8Bytes(created, JsonOptions),
                "json",
                sequence,
                occurredAt,
                $"corr-{dispatchId}")]);
        return new ProjectionDispatchRequest(request, [ProjectionType], dispatchId, fingerprint);
    }

    private static async Task EraseAllDerivedKeysAsync(
        InMemoryReadModelStore store,
        TenantId tenant,
        ConversationId conversationId,
        string dispatchId,
        CancellationToken cancellationToken)
    {
        string[] keys =
        [
            ConversationKey(tenant, conversationId),
            TenantIndexKey(tenant),
            DispatchLedgerKey(dispatchId),
            ResolutionKey("projection:conversations-resolution", tenant, conversationId),
            ResolutionKey("projection:conversations-resolution-selection", tenant, conversationId),
            ResolutionKey("projection:conversations-resolution-proof", tenant, conversationId),
            ResolutionKey("projection:conversations-resolution-fence", tenant, conversationId),
        ];
        foreach (string key in keys)
        {
            (bool present, string etag) = await store.TryReadEtagAsync(StateStoreName, key, cancellationToken);
            if (present)
            {
                _ = await store.TryEraseAsync(StateStoreName, key, etag, cancellationToken);
            }
        }
    }

    private static string CanonicalizeWithoutObservationTimes<T>(T value)
    {
        JsonNode node = JsonSerializer.SerializeToNode(value, JsonOptions)!;
        StripObservationTimes(node);
        return node.ToJsonString();
    }

    private static void StripObservationTimes(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            _ = obj.Remove("projectionGeneratedAt");
            _ = obj.Remove("lagDuration");
            foreach (KeyValuePair<string, JsonNode?> child in obj.ToList())
            {
                if (child.Value is not null)
                {
                    StripObservationTimes(child.Value);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? item in array)
            {
                if (item is not null)
                {
                    StripObservationTimes(item);
                }
            }
        }
    }

    private static string ConversationKey(TenantId tenant, ConversationId conversationId)
        => $"projection:conversations:{EncodeSegment(tenant.Value)}:{EncodeSegment(conversationId.Value)}";

    private static string TenantIndexKey(TenantId tenant)
        => $"projection:conversations-index:{EncodeSegment(tenant.Value)}";

    private static string DispatchLedgerKey(string dispatchId)
        => $"projection:conversations-dispatch:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(dispatchId)))}";

    private static string ResolutionKey(string prefix, TenantId tenant, ConversationId conversationId)
        => $"{prefix}:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tenant.Value)))}:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(conversationId.Value)))}";

    private static string EncodeSegment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static void EmitCandidateBinding()
    {
        Assembly serverAssembly = typeof(ConversationProjectionReadStore).Assembly;
        Assembly testAssembly = typeof(DerivedKeyLifecycleTests).Assembly;
        string info = serverAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        int plus = info.LastIndexOf('+');
        string commit = plus >= 0 && plus + 1 < info.Length ? info[(plus + 1)..] : "unknown";
        string serverSha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(serverAssembly.Location)));
        string testSha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(testAssembly.Location)));
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"CANDIDATE_BINDING|commit={commit}|serverSha256={serverSha}|testAssemblySha256={testSha}");
    }
}
