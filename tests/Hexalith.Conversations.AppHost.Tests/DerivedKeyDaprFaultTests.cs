// <copyright file="DerivedKeyDaprFaultTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.EventStore.Client.Queries;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Conversations.AppHost.Tests;

/// <summary>
/// Story 12.3 (<c>AC-12.3-04</c>) real <c>daprd</c> + disposable Redis + <see cref="Hexalith.EventStore.Client.Projections.DaprReadModelStore"/>
/// correctness and fault-injection tests.
/// </summary>
/// <param name="fixture">The shared live Dapr+Redis fixture.</param>
public sealed class DerivedKeyDaprFaultTests(DerivedKeyDaprLiveFixture fixture) : IClassFixture<DerivedKeyDaprLiveFixture>
{
    private const string Caller = "caller-dapr-12-3";
    private static readonly PartyId Actor = new("party-dapr-actor");
    private static readonly BusinessReference Business = new("crm", "case-dapr-12-3");
    private static readonly DateTimeOffset BaseTimestamp = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// End-to-end bounded list (page 1 &amp; 2) and open through real <c>daprd</c> + Redis prove streaming
    /// <c>Utf8JsonReader</c> lookup, minimal detail-header bulk reads, and persisted <c>GenerationToken</c>
    /// cursor continuity + invalidation.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task BoundedReadsAndCursorContinuityShouldWorkOverRealDaprRedis()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TenantId tenant = new("tenant-dapr-ok-" + Guid.NewGuid().ToString("N")[..8]);
        DerivedKeyDaprFaultStore faultStore = new(fixture.Store);
        ConversationProjectionReadModelWriter writer = new(fixture.StoreOptions, faultStore, null);

        await InitializeTenantViaWriterAsync(faultStore, tenant, cancellationToken);
        for (int index = 0; index < 6; index++)
        {
            ConversationId convId = new($"conv-dapr-{index:00}");
            string dispatchId = $"dispatch-dapr-ok-{tenant.Value}-{index:00}";
            ConversationProjectedReadModels models = BuildModels(tenant, convId, 1, dispatchId, BaseTimestamp.AddSeconds(index));
            await PersistCompletedGenerationAsync(faultStore, writer, models, cancellationToken);
        }

        ConversationQueryHandler handler = CreateHandler(faultStore, tenant);
        faultStore.ResetWrites();

        ConversationListResult page1 = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-p1", Page: new ConversationPageRequest(3)),
            cancellationToken);
        page1.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        page1.Conversations.Count.ShouldBe(3);
        page1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();

        ConversationListResult page2 = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-p2", Page: new ConversationPageRequest(3, page1.Page.ContinuationCursor)),
            cancellationToken);
        page2.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        page2.Conversations.Count.ShouldBe(3);
        page2.Page.ContinuationCursor.ShouldBeNull();
        page2.Conversations.Select(static c => c.ConversationId).Intersect(page1.Conversations.Select(static c => c.ConversationId)).ShouldBeEmpty();

        ConversationDetailResult opened = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenant, Caller, "corr-open", new ConversationId("conv-dapr-00")),
            cancellationToken);
        opened.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        opened.Details.ShouldNotBeNull();
        opened.Details!.ConversationId.ShouldBe(new ConversationId("conv-dapr-00"));
        faultStore.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED: real Dapr reads must never write");

        // Mutating one conversation invalidates the prior page-1 cursor.
        ConversationProjectedReadModels updated = BuildModels(tenant, new ConversationId("conv-dapr-00"), 2, $"dispatch-dapr-upd-{tenant.Value}", BaseTimestamp.AddSeconds(20));
        await PersistCompletedGenerationAsync(faultStore, writer, updated, cancellationToken);

        ConversationListResult staleCursor = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-stale", Page: new ConversationPageRequest(3, page1.Page.ContinuationCursor)),
            cancellationToken);
        staleCursor.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        staleCursor.Conversations.ShouldBeEmpty();
    }

    /// <summary>
    /// Partial write failure (pending marker durable, detail write blocked) fails closed over real Dapr+Redis
    /// and converges deterministically upon retry.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task PartialWriteShouldFailClosedAndRetryShouldConvergeOverRealDaprRedis()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TenantId tenant = new("tenant-dapr-pw-" + Guid.NewGuid().ToString("N")[..8]);
        DerivedKeyDaprFaultStore faultStore = new(fixture.Store);
        ConversationProjectionReadModelWriter writer = new(fixture.StoreOptions, faultStore, null);
        await InitializeTenantViaWriterAsync(faultStore, tenant, cancellationToken);

        ConversationId healthyId = new("conv-healthy");
        ConversationId partialId = new("conv-partial");
        await PersistCompletedGenerationAsync(
            faultStore,
            writer,
            BuildModels(tenant, healthyId, 1, $"dispatch-healthy-{tenant.Value}", BaseTimestamp),
            cancellationToken);

        // Seed partial write: mark pending in tenant-index, then fail on conversation detail write.
        ConversationProjectedReadModels partialModels = BuildModels(tenant, partialId, 1, $"dispatch-partial-{tenant.Value}", BaseTimestamp.AddSeconds(1));
        await writer.MarkPendingAsync(partialModels, cancellationToken);
        faultStore.BlockedWriteKey = ConversationKey(tenant, partialId);
        _ = await Should.ThrowAsync<InvalidOperationException>(() => writer.PersistAsync(partialModels, cancellationToken));
        faultStore.BlockedWriteKey = null;

        ConversationQueryHandler handler = CreateHandler(faultStore, tenant);
        faultStore.ResetWrites();

        ConversationDetailResult openDuringPartial = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenant, Caller, "corr-pw-open-during", partialId),
            cancellationToken);
        openDuringPartial.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        openDuringPartial.Details.ShouldBeNull();

        ConversationListResult duringPartial = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-pw-list"),
            cancellationToken);
        duringPartial.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        duringPartial.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MixedGeneration);
        duringPartial.Conversations.ShouldContain(c => c.ConversationId == healthyId);
        duringPartial.Conversations.ShouldNotContain(c => c.ConversationId == partialId);
        faultStore.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");

        // Post-detail partial write failure: detail key lands in Redis, tenant-index merge is blocked.
        faultStore.BlockedWriteKey = TenantIndexKey(tenant);
        _ = await Should.ThrowAsync<InvalidOperationException>(() => writer.PersistAsync(partialModels, cancellationToken));
        faultStore.BlockedWriteKey = null;
        faultStore.ResetWrites();

        ConversationDetailResult openPostDetailPartial = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenant, Caller, "corr-pw-post-detail-open", partialId),
            cancellationToken);
        openPostDetailPartial.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        openPostDetailPartial.Details.ShouldBeNull();
        ConversationListResult listPostDetailPartial = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-pw-post-detail-list"),
            cancellationToken);
        listPostDetailPartial.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        listPostDetailPartial.Conversations.ShouldNotContain(c => c.ConversationId == partialId);
        faultStore.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");

        // Retry completes partial write and converges cleanly to Current.
        await PersistCompletedGenerationAsync(faultStore, writer, partialModels, cancellationToken);
        ConversationDetailResult openRecovered = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenant, Caller, "corr-pw-open", partialId),
            cancellationToken);
        ConversationListResult listRecovered = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-pw-rec"),
            cancellationToken);
        openRecovered.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        listRecovered.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        listRecovered.Conversations.Count.ShouldBe(2);
    }

    /// <summary>
    /// Artificial latency, mid-page bulk-read outages, and full store outages over real Dapr+Redis keep concurrent
    /// reads safe and fail closed to <c>Unavailable</c> with zero repairs.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LatencyAndOutageShouldFailClosedWithoutRepairOverRealDaprRedis()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TenantId tenant = new("tenant-dapr-lat-" + Guid.NewGuid().ToString("N")[..8]);
        DerivedKeyDaprFaultStore faultStore = new(fixture.Store);
        ConversationProjectionReadModelWriter writer = new(fixture.StoreOptions, faultStore, null);
        await InitializeTenantViaWriterAsync(faultStore, tenant, cancellationToken);

        ConversationId[] convIds = [.. Enumerable.Range(0, 6).Select(index => new ConversationId($"conv-lat-{index:00}"))];
        for (int index = 0; index < convIds.Length; index++)
        {
            await PersistCompletedGenerationAsync(
                faultStore,
                writer,
                BuildModels(tenant, convIds[index], 1, $"dispatch-lat-{tenant.Value}-{index:00}", BaseTimestamp.AddSeconds(index)),
                cancellationToken);
        }

        ConversationQueryHandler handler = CreateHandler(faultStore, tenant);

        // Latency injection: concurrent detail+ledger bulk reads and concurrent multi-conversation GetAsync succeed cleanly.
        faultStore.ReadLatency = TimeSpan.FromMilliseconds(20);
        faultStore.ResetWrites();
        ConversationListResult delayedList = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-lat"),
            cancellationToken);
        delayedList.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        delayedList.Conversations.Count.ShouldBe(convIds.Length);

        Task<(ConversationId Expected, ConversationDetailResult Result)>[] concurrentOpens = convIds
            .Select(async id =>
            {
                ConversationDetailResult res = await handler.GetAsync(
                    new GetConversationQuery(SchemaVersion.Current, tenant, Caller, $"corr-open-{id.Value}", id),
                    cancellationToken);
                return (id, res);
            })
            .ToArray();
        foreach ((ConversationId expected, ConversationDetailResult result) in await Task.WhenAll(concurrentOpens))
        {
            result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            result.Details.ShouldNotBeNull();
            result.Details!.ConversationId.ShouldBe(expected);
        }

        faultStore.Writes.ShouldBeEmpty();
        faultStore.ReadLatency = TimeSpan.Zero;

        // Mid-page bulk-read outage: index read succeeds, page bulk validation throws -> List fails closed to Unavailable.
        faultStore.SimulateBulkReadOutage = true;
        ConversationListResult midPageOutage = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-bulk-outage"),
            cancellationToken);
        midPageOutage.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        midPageOutage.Conversations.ShouldBeEmpty();
        faultStore.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        faultStore.SimulateBulkReadOutage = false;

        // Full outage injection: both List and Open fail closed to Unavailable with zero writes.
        faultStore.SimulateOutage = true;
        ConversationListResult outageList = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-outage-list"),
            cancellationToken);
        ConversationDetailResult outageOpen = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenant, Caller, "corr-outage-open", convIds[0]),
            cancellationToken);
        outageList.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        outageList.Conversations.ShouldBeEmpty();
        outageOpen.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        outageOpen.Details.ShouldBeNull();
        faultStore.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");

        // Recovery after outage clears.
        faultStore.SimulateOutage = false;
        ConversationDetailResult recoveredOpen = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenant, Caller, "corr-rec-open", convIds[0]),
            cancellationToken);
        recoveredOpen.FreshnessState.ShouldBe(ProjectionTrustState.Current);
    }

    /// <summary>
    /// Misfiled cross-tenant poison records and malformed JSON stored in Redis fail closed with zero disclosure
    /// and zero read-side repair.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task PoisonRecordsAndCrossTenantIsolationShouldNeverDiscloseOverRealDaprRedis()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TenantId tenantA = new("tenant-dapr-pa-" + Guid.NewGuid().ToString("N")[..8]);
        TenantId tenantB = new("tenant-dapr-pb-" + Guid.NewGuid().ToString("N")[..8]);
        DerivedKeyDaprFaultStore faultStore = new(fixture.Store);
        ConversationProjectionReadModelWriter writer = new(fixture.StoreOptions, faultStore, null);
        await InitializeTenantViaWriterAsync(faultStore, tenantA, cancellationToken);
        await InitializeTenantViaWriterAsync(faultStore, tenantB, cancellationToken);

        ConversationId convGood = new("conv-good");
        ConversationId convPoison = new("conv-poison");
        await PersistCompletedGenerationAsync(
            faultStore,
            writer,
            BuildModels(tenantA, convGood, 1, $"dispatch-good-{tenantA.Value}", BaseTimestamp),
            cancellationToken);
        ConversationProjectedReadModels validPoison = BuildModels(tenantA, convPoison, 1, $"dispatch-poison-{tenantA.Value}", BaseTimestamp.AddSeconds(1));
        await PersistCompletedGenerationAsync(faultStore, writer, validPoison, cancellationToken);

        // Overwrite convPoison's detail key in Redis directly with a Tenant B payload (misfiled cross-tenant poison).
        ConversationProjectedReadModels misfiled = BuildModels(tenantB, convPoison, 1, validPoison.DispatchId, BaseTimestamp.AddSeconds(1));
        (bool poisonPresent, string poisonEtag) = await faultStore.TryReadEtagAsync(
            DerivedKeyDaprLiveFixture.StateStoreName,
            ConversationKey(tenantA, convPoison),
            cancellationToken);
        poisonPresent.ShouldBeTrue();
        (await faultStore.TrySaveAsync(
            DerivedKeyDaprLiveFixture.StateStoreName,
            ConversationKey(tenantA, convPoison),
            misfiled,
            poisonEtag,
            cancellationToken)).ShouldBeTrue();

        ConversationQueryHandler handler = CreateHandler(faultStore, tenantA, tenantB);
        faultStore.ResetWrites();

        ConversationDetailResult poisonedOpen = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenantA, Caller, "corr-poison-open", convPoison),
            cancellationToken);
        ConversationListResult poisonedList = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenantA, Caller, "corr-poison-list"),
            cancellationToken);

        poisonedOpen.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        poisonedOpen.Details.ShouldBeNull();
        poisonedList.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        poisonedList.Conversations.ShouldNotContain(c => c.ConversationId == convPoison);
        poisonedList.Conversations.ShouldAllBe(c => c.TenantId == tenantA);
        faultStore.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");

        // Malformed Redis JSON payload under an authorized conversation key must fail closed without repairing.
        (bool corruptPresent, string corruptEtag) = await faultStore.TryReadEtagAsync(
            DerivedKeyDaprLiveFixture.StateStoreName,
            ConversationKey(tenantA, convPoison),
            cancellationToken);
        corruptPresent.ShouldBeTrue();
        (await faultStore.TrySaveAsync(
            DerivedKeyDaprLiveFixture.StateStoreName,
            ConversationKey(tenantA, convPoison),
            new { Summary = "not-an-object", Detail = 42 },
            corruptEtag,
            cancellationToken)).ShouldBeTrue();
        faultStore.ResetWrites();

        ConversationDetailResult malformedOpen = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, tenantA, Caller, "corr-malformed-open", convPoison),
            cancellationToken);
        ConversationListResult malformedList = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenantA, Caller, "corr-malformed-list"),
            cancellationToken);
        malformedOpen.FreshnessState.ShouldBeOneOf(ProjectionTrustState.Unavailable, ProjectionTrustState.Rebuilding);
        malformedOpen.Details.ShouldBeNull();
        malformedList.FreshnessState.ShouldBeOneOf(ProjectionTrustState.Unavailable, ProjectionTrustState.Rebuilding);
        malformedList.Conversations.ShouldNotContain(c => c.ConversationId == convPoison);
        faultStore.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
    }

    /// <summary>
    /// Concurrent ETag CAS writes against real Dapr+Redis converge cleanly with zero lost updates.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ConcurrentWritesShouldConvergeOverRealDaprRedis()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TenantId tenant = new("tenant-dapr-conc-" + Guid.NewGuid().ToString("N")[..8]);
        DerivedKeyDaprFaultStore faultStore = new(fixture.Store);
        ConversationProjectionReadModelWriter writer = new(fixture.StoreOptions, faultStore, null);
        await InitializeTenantViaWriterAsync(faultStore, tenant, cancellationToken);

        Task[] writers = Enumerable.Range(0, 6).Select(async index =>
        {
            ConversationId convId = new($"conv-conc-{index:00}");
            ConversationProjectedReadModels models = BuildModels(
                tenant,
                convId,
                1,
                $"dispatch-conc-{tenant.Value}-{index:00}",
                BaseTimestamp.AddSeconds(index));
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    await PersistCompletedGenerationAsync(faultStore, writer, models, cancellationToken);
                    return;
                }
                catch (InvalidOperationException) when (attempt < 10)
                {
                    await Task.Delay(15 * (attempt + 1), cancellationToken);
                }
            }
        }).ToArray();

        await Task.WhenAll(writers);

        ConversationQueryHandler handler = CreateHandler(faultStore, tenant);
        ConversationListResult list = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, tenant, Caller, "corr-conc-list"),
            cancellationToken);
        list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        list.Conversations.Count.ShouldBe(6);
    }

    private ConversationQueryHandler CreateHandler(DerivedKeyDaprFaultStore store, params TenantId[] tenants)
    {
        DerivedKeyDaprTenantAccess access = new(tenants);
        ConversationProjectionReadStore readStore = new(store, fixture.StoreOptions);
        ConversationProjectionReadService readService = new(access, readStore);
        QueryCursorCodec cursorCodec = new(new EphemeralDataProtectionProvider(), "Hexalith.Conversations.Story12.3.Dapr.Cursor.v1");
        return new ConversationQueryHandler(
            access,
            readStore,
            readService,
            cursorCodec,
            timeProvider: new DerivedKeyDaprFixedTimeProvider(BaseTimestamp.AddMinutes(1)),
            hydrationService: new ConversationReadHydrationService());
    }

    private async Task InitializeTenantViaWriterAsync(
        DerivedKeyDaprFaultStore store,
        TenantId tenant,
        CancellationToken cancellationToken)
    {
        await fixture.Client.SaveStateAsync(
            DerivedKeyDaprLiveFixture.StateStoreName,
            TenantIndexKey(tenant),
            new
            {
                SchemaVersion = 2,
                Lifecycle = 2,
                Watermarks = new Dictionary<string, long>(StringComparer.Ordinal),
                Summaries = Array.Empty<ConversationSummaryProjectionV1>(),
                Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal),
            },
            cancellationToken: cancellationToken);
        store.ResetWrites();
    }

    private async Task PersistCompletedGenerationAsync(
        DerivedKeyDaprFaultStore store,
        ConversationProjectionReadModelWriter writer,
        ConversationProjectedReadModels models,
        CancellationToken cancellationToken)
    {
        await writer.PersistAsync(models, cancellationToken);
        ConversationProjectionDispatchLedger ledger = new(
            models.DispatchId,
            $"fp-{models.DispatchId}",
            models.Summary.TenantId,
            models.Summary.ConversationId,
            models.Summary.Freshness.ProjectionGeneratedAt,
            ConversationProjectionDispatchStatus.Completed);
        await store.SaveAsync(
            fixture.StoreOptions.StateStoreName,
            DispatchLedgerKey(models.DispatchId),
            ledger,
            cancellationToken);
    }

    private static ConversationProjectedReadModels BuildModels(
        TenantId tenant,
        ConversationId conversationId,
        long position,
        string dispatchId,
        DateTimeOffset timestamp)
    {
        ProjectionFreshnessV1 freshness = new(
            SchemaVersion.Current,
            $"pos:{position:0000000000}",
            position,
            timestamp,
            timestamp.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);
        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            tenant,
            conversationId,
            freshness,
            "Open",
            $"Dapr Conv {conversationId.Value}",
            Business,
            ParticipantPartyIds: [Actor],
            MessageCount: 1);
        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            tenant,
            conversationId,
            freshness,
            "Open",
            summary.Label,
            Business,
            Participants: [new ConversationParticipantProjectionV1(Actor, ParticipantType.Human, ParticipantRole.Member)],
            Messages: [new ConversationTimelineMessageProjectionV1(new MessageId($"msg-{conversationId.Value}-{position}"), Actor, "Dapr visible message", timestamp)]);
        return new ConversationProjectedReadModels(summary, detail) { DispatchId = dispatchId };
    }

    private static string ConversationKey(TenantId tenant, ConversationId conversationId)
        => $"projection:conversations:{EncodeSegment(tenant.Value)}:{EncodeSegment(conversationId.Value)}";

    private static string TenantIndexKey(TenantId tenant)
        => $"projection:conversations-index:{EncodeSegment(tenant.Value)}";

    private static string DispatchLedgerKey(string dispatchId)
        => $"projection:conversations-dispatch:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(dispatchId)))}";

    private static string EncodeSegment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
