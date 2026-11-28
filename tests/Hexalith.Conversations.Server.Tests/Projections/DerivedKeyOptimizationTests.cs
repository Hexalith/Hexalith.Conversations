// <copyright file="DerivedKeyOptimizationTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Story 12.3 (<c>AC-12.3-01</c>, <c>AC-12.3-02</c>) unit correctness, bounded-read, and scale-profiling tests.
/// </summary>
public sealed class DerivedKeyOptimizationTests
{
    private const string Caller = "caller-12-3";
    private static readonly TenantId Tenant = new("tenant-12-3");
    private static readonly TenantId OtherTenant = new("tenant-other-12-3");
    private static readonly PartyId Actor = new("party-actor-12-3");
    private static readonly PartyId Participant = new("party-member-12-3");
    private static readonly BusinessReference Business = new("crm", "case-12-3");
    private static readonly ProjectId Project = new("project-12-3");
    private static readonly FolderId Folder = new("folder-12-3");
    private static readonly DateTimeOffset BaseTimestamp = new(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// <c>AC-12.3-01</c>: List (page 1 &amp; 2) and Open across 100, 1,000, and 10,000 rows use only
    /// approved ADR 0005 bounded reads with zero unexpected fan-out and zero store writes.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ListAndOpenShouldUseApprovedBoundedReads()
    {
        EmitCandidateBinding();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ITestOutputHelper? output = TestContext.Current.TestOutputHelper;

        foreach (int rowCount in new[] { 100, 1_000, 10_000 })
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, rowCount);
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListFilterV1 filter = new(BusinessReference: Business);

            // List Page 1: 1 tenant-index GET + 2 bounded page bulk reads (25 details + 25 ledgers).
            spy.Reset();
            ConversationListResult page1 = await handler.ListAsync(
                new ListConversationsQuery(SchemaVersion.Current, Tenant, Caller, "corr-p1", filter, new ConversationPageRequest(25)),
                cancellationToken);
            page1.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            page1.Conversations.Count.ShouldBe(25);
            page1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED: list page 1 must not write");
            spy.GetCalls.ShouldBe(1, $"DERIVED_KEY_FANOUT_UNBOUNDED: expected 1 index GET for {rowCount} rows");
            spy.SingleReadKeys.ShouldBe([ConversationProjectionReadModelKeys.TenantIndexKey(Tenant)]);
            spy.BulkGetCalls.ShouldBe(2, $"DERIVED_KEY_FANOUT_UNBOUNDED: expected 2 page bulk reads for {rowCount} rows");
            spy.BulkReadBatches[0].Count.ShouldBe(25);
            spy.BulkReadBatches[1].Count.ShouldBe(25);

            // List Page 2: 1 tenant-index GET + 2 bounded page bulk reads (25 details + 25 ledgers).
            spy.Reset();
            ConversationListResult page2 = await handler.ListAsync(
                new ListConversationsQuery(SchemaVersion.Current, Tenant, Caller, "corr-p2", filter, new ConversationPageRequest(25, page1.Page.ContinuationCursor)),
                cancellationToken);
            page2.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            page2.Conversations.Count.ShouldBe(25);
            page2.Conversations.Select(static c => c.ConversationId).Intersect(page1.Conversations.Select(static c => c.ConversationId)).ShouldBeEmpty();
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED: list page 2 must not write");
            spy.GetCalls.ShouldBe(1, $"DERIVED_KEY_FANOUT_UNBOUNDED: expected 1 index GET on page 2 for {rowCount} rows");
            spy.SingleReadKeys.ShouldBe([ConversationProjectionReadModelKeys.TenantIndexKey(Tenant)]);
            spy.BulkGetCalls.ShouldBe(2, $"DERIVED_KEY_FANOUT_UNBOUNDED: expected 2 page bulk reads on page 2 for {rowCount} rows");
            spy.BulkReadBatches[0].Count.ShouldBe(25);
            spy.BulkReadBatches[1].Count.ShouldBe(25);

            // Open: exactly 3 single-key reads (1 detail + 1 targeted tenant-index + 1 dispatch ledger), 0 bulk reads, 0 writes.
            ConversationId targetId = new("conversation-00000");
            spy.Reset();
            ConversationDetailResult opened = await handler.GetAsync(
                new GetConversationQuery(SchemaVersion.Current, Tenant, Caller, "corr-open", targetId),
                cancellationToken);
            opened.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            opened.Details.ShouldNotBeNull();
            opened.Details!.ConversationId.ShouldBe(targetId);
            opened.Details.Messages.Count.ShouldBe(10);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED: open must not write");
            spy.BulkGetCalls.ShouldBe(0, $"DERIVED_KEY_FANOUT_UNBOUNDED: open must not issue bulk reads for {rowCount} rows");
            spy.GetCalls.ShouldBe(3, $"DERIVED_KEY_FANOUT_UNBOUNDED: expected 3 GETs for open at {rowCount} rows");
            spy.SingleReadKeys.ShouldBe(
            [
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, targetId),
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-00000"),
            ]);

            if (rowCount == 10_000)
            {
                void OpenForAllocationMeasurement()
                {
                    ConversationDetailResult result = handler.GetAsync(OpenQuery(targetId), cancellationToken)
                        .AsTask().GetAwaiter().GetResult();
                    if (result.FreshnessState != ProjectionTrustState.Current)
                    {
                        throw new InvalidOperationException("Allocation profiling requires a successful Open.");
                    }

                    GC.KeepAlive(result);
                }

                OpenForAllocationMeasurement();
                (_, long targetedBytes) = MeasureSync(OpenForAllocationMeasurement, iterations: 3);
                spy.MaterializeFullIndexForTargetedRead = true;
                OpenForAllocationMeasurement();
                (_, long fullIndexBytes) = MeasureSync(OpenForAllocationMeasurement, iterations: 3);
                spy.MaterializeFullIndexForTargetedRead = false;
                targetedBytes.ShouldBeLessThan(fullIndexBytes * 9 / 10,
                    "DERIVED_KEY_FANOUT_UNBOUNDED: warmed Open must avoid full index materialization allocations");
            }

            // Record scale profile (list page 1, list page 2, open, write) at 100 / 1,000 / 10,000 rows.
            ProfileScaleRow(output, backing, handler, filter, rowCount, targetId, cancellationToken);
        }

        AssertListAllocations(cancellationToken);
    }

    private static void AssertListAllocations(CancellationToken cancellationToken)
    {
        InMemoryReadModelStore backing = new();
        SeedReadyTenant(backing, rowCount: 100, richRowCount: 25, richMessageCount: 100);
        DerivedKeyReadStoreSpy spy = new(backing);
        ConversationQueryHandler handler = CreateHandler(spy);
        ListConversationsQuery query = ListQuery(new ConversationListFilterV1(BusinessReference: Business));

        void ListForAllocationMeasurement()
        {
            ConversationListResult result = handler.ListAsync(query, cancellationToken).AsTask().GetAwaiter().GetResult();
            if (result.FreshnessState != ProjectionTrustState.Current || result.Conversations.Count != 25)
            {
                throw new InvalidOperationException("Allocation profiling requires a complete List page.");
            }

            GC.KeepAlive(result);
        }

        ListForAllocationMeasurement();
        (_, long pageValidationBytes) = MeasureSync(ListForAllocationMeasurement, iterations: 3);
        spy.MaterializeFullDetailForPageRead = true;
        ListForAllocationMeasurement();
        (_, long fullDetailBytes) = MeasureSync(ListForAllocationMeasurement, iterations: 3);
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"List allocation validation: {pageValidationBytes} B optimized, {fullDetailBytes} B with full detail");
        (fullDetailBytes - pageValidationBytes).ShouldBeGreaterThan(4_096,
            "DERIVED_KEY_FANOUT_UNBOUNDED: warmed List must avoid full detail materialization allocations");
        spy.GetCalls.ShouldBe(8);
        spy.BulkGetCalls.ShouldBe(16);
        spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
    }

    /// <summary>
    /// <c>AC-12.3-02</c>: Every invalid derived-state fixture (missing, duplicate, stale, advanced, malformed,
    /// misfiled, pending, inconsistent) fails closed and performs zero durable repairs.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task InvalidDerivedStateShouldFailClosedWithoutRepair()
    {
        EmitCandidateBinding();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ConversationId convA = new("conversation-00000");
        ConversationId convB = new("conversation-00001");
        ConversationListFilterV1 filter = new();

        // 1. Missing tenant index -> List Unavailable, Open Forbidden, zero writes.
        {
            InMemoryReadModelStore backing = new();
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);
            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);

            list.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: missing index");
            list.Conversations.ShouldBeEmpty();
            open.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: missing open");
            open.Details.ShouldBeNull();
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 2. Duplicate conversation entry in tenant index -> List Unavailable/MetadataContradictory, Open Rebuilding, zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 5);
            ConversationProjectionIndexReadModel index = backing.Snapshot<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
            ConversationProjectionIndexReadModel duplicated = new()
            {
                SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Watermarks = index.Watermarks,
                Summaries = [.. index.Summaries, index.Summaries[0]],
                Dispatches = index.Dispatches,
            };
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                duplicated);
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);
            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);

            list.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: duplicate list");
            list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataContradictory);
            list.Conversations.ShouldBeEmpty();
            open.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: duplicate open");
            open.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MixedGeneration);
            open.Details.ShouldBeNull();
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 3. Stale detail (index ahead of detail) -> Open Rebuilding, List withholds stale row & suppresses continuation, zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 30);
            ConversationProjectedReadModels stale = BuildModels(Tenant, convA, position: 1, dispatchId: "dispatch-stale-0");
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA),
                stale);
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);
            ConversationListResult list = await handler.ListAsync(ListQuery(filter, pageSize: 25), cancellationToken);

            open.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: stale open");
            open.Details.ShouldBeNull();
            list.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: stale list");
            list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MixedGeneration);
            list.Conversations.ShouldNotContain(c => c.ConversationId == convA);
            list.Page.ContinuationCursor.ShouldBeNull("withheld row must suppress continuation cursor");
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 4. Advanced summary ahead of watermark -> List Unavailable/MetadataContradictory, zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 5);
            ConversationProjectionIndexReadModel index = backing.Snapshot<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
            ConversationSummaryProjectionV1 advanced = BuildModels(Tenant, convA, position: 5, dispatchId: "dispatch-00000").Summary;
            ConversationProjectionIndexReadModel corrupted = new()
            {
                SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Watermarks = index.Watermarks, // still 1
                Summaries = [advanced, .. index.Summaries.Skip(1)],
                Dispatches = index.Dispatches,
            };
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                corrupted);
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            list.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: advanced summary");
            list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataContradictory);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 5. Malformed index (unknown schema version / malformed lifecycle) -> List Unavailable/MetadataContradictory, zero writes.
        {
            InMemoryReadModelStore backing = new();
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                new { SchemaVersion = 99, Lifecycle = 2, Watermarks = new Dictionary<string, long>() });
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            list.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: malformed schema");
            list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataContradictory);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 6. Malformed JSON in detail key -> Open Unavailable, List Unavailable (deserialization fails closed), zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 5);
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA),
                new { NotASummary = true, Detail = "corrupt" });
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);
            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            open.FreshnessState.ShouldBeOneOf(ProjectionTrustState.Unavailable, ProjectionTrustState.Rebuilding);
            open.Details.ShouldBeNull();
            list.FreshnessState.ShouldBeOneOf(ProjectionTrustState.Unavailable, ProjectionTrustState.Rebuilding);
            list.Conversations.ShouldNotContain(c => c.ConversationId == convA);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 7. Misfiled cross-tenant record under Tenant's conversation key -> Open Forbidden (PoisonEvent), List withholds, zero disclosure, zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 5);
            ConversationProjectedReadModels misfiled = BuildModels(OtherTenant, convA, position: 1, dispatchId: "dispatch-00000");
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA),
                misfiled);
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);
            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            open.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: misfiled open");
            open.Details.ShouldBeNull();
            list.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: misfiled list");
            list.Conversations.ShouldNotContain(c => c.ConversationId == convA);
            list.Conversations.ShouldAllBe(c => c.TenantId == Tenant);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 8. Pending dispatch marker -> Open Rebuilding, List withholds pending row while keeping healthy sibling readable, zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 5);
            ConversationProjectionIndexReadModel index = backing.Snapshot<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
            var dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(index.Dispatches, StringComparer.Ordinal)
            {
                [convA.Value] = new("dispatch-00000", 1, IsPending: true),
            };
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                new ConversationProjectionIndexReadModel
                {
                    SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                    Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                    Watermarks = index.Watermarks,
                    Summaries = index.Summaries,
                    Dispatches = dispatches,
                });
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationDetailResult openPending = await handler.GetAsync(OpenQuery(convA), cancellationToken);
            ConversationDetailResult openHealthy = await handler.GetAsync(OpenQuery(convB), cancellationToken);
            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            openPending.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: pending open");
            openPending.Details.ShouldBeNull();
            openHealthy.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            list.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
            list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MixedGeneration);
            list.Conversations.ShouldNotContain(c => c.ConversationId == convA);
            list.Conversations.ShouldContain(c => c.ConversationId == convB);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 9. Inconsistent dispatch ledger (wrong tenant / wrong status / wrong timestamp) -> Open Rebuilding, List withholds, zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 5);
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-00000"),
                new ConversationProjectionDispatchLedger(
                    "dispatch-00000",
                    "fingerprint-00000",
                    OtherTenant,
                    convA,
                    BaseTimestamp.AddSeconds(1),
                    ConversationProjectionDispatchStatus.Completed));
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);
            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            open.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: ledger tenant mismatch");
            open.Details.ShouldBeNull();
            list.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
            list.Conversations.ShouldNotContain(c => c.ConversationId == convA);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 10. Inconsistent provider bulk response -> List Unavailable, zero writes.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 5);
            DerivedKeyReadStoreSpy spy = new(backing) { CorruptBulkResponse = static _ => true };
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            list.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: corrupt bulk");
            list.Conversations.ShouldBeEmpty();
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 11. Version 1 index carrying GenerationToken contradicts itself -> List Unavailable/MetadataContradictory.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 2);
            ConversationProjectionIndexReadModel seeded = backing.Snapshot<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                new ConversationProjectionIndexReadModel
                {
                    SchemaVersion = 1,
                    Summaries = seeded.Summaries,
                    Dispatches = seeded.Dispatches,
                    GenerationToken = new string('a', 64),
                });
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            list.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: v1 with GenerationToken");
            list.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataContradictory);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 12. Whitespace or malformed persisted GenerationToken falls back safely instead of throwing on cursor encode.
        foreach (string malformedToken in new[] { "   ", "not-hex", new string('A', 64), new string('g', 64) })
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 30);
            ConversationProjectionIndexReadModel seeded = backing.Snapshot<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                new ConversationProjectionIndexReadModel
                {
                    SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                    Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                    Watermarks = seeded.Watermarks,
                    Summaries = seeded.Summaries,
                    Dispatches = seeded.Dispatches,
                    GenerationToken = malformedToken,
                });
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult page1 = await handler.ListAsync(ListQuery(filter, pageSize: 25), cancellationToken);
            page1.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            page1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();
            ConversationListResult page2 = await handler.ListAsync(ListQuery(filter, pageSize: 25, cursor: page1.Page.ContinuationCursor), cancellationToken);
            page2.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            page2.Conversations.Count.ShouldBe(5);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 13. Stale valid-shaped persisted GenerationToken after summaries and matching detail records change/reorder -> old cursor MUST fail closed (Forbidden).
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 30);
            ConversationProjectionIndexReadModel initialIndex = backing.Snapshot<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
            string originalToken = ConversationIndexGenerationToken.Compute(initialIndex.Summaries);
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                new ConversationProjectionIndexReadModel
                {
                    SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                    Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                    Watermarks = initialIndex.Watermarks,
                    Summaries = initialIndex.Summaries,
                    Dispatches = initialIndex.Dispatches,
                    GenerationToken = originalToken,
                });
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationListResult page1 = await handler.ListAsync(ListQuery(filter, pageSize: 25), cancellationToken);
            page1.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            string oldCursor = page1.Page.ContinuationCursor!;
            oldCursor.ShouldNotBeNullOrWhiteSpace();

            // Mutate conversation-00029 (moving its timestamp so list order changes) AND update its detail/ledger,
            // while leaving the old valid-shaped 64-hex GenerationToken on the tenant index.
            ConversationId movedId = new("conversation-00029");
            ConversationProjectedReadModels updatedModels = BuildModels(
                Tenant,
                movedId,
                position: 2,
                dispatchId: "dispatch-moved-00029",
                timestampOverride: BaseTimestamp.AddMinutes(5));
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, movedId),
                updatedModels);
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-moved-00029"),
                new ConversationProjectionDispatchLedger(
                    "dispatch-moved-00029",
                    "fingerprint-moved-00029",
                    Tenant,
                    movedId,
                    updatedModels.Summary.Freshness.ProjectionGeneratedAt,
                    ConversationProjectionDispatchStatus.Completed));
            List<ConversationSummaryProjectionV1> mutatedSummaries =
            [
                .. initialIndex.Summaries.Where(s => s.ConversationId != movedId),
                updatedModels.Summary,
            ];
            Dictionary<string, long> mutatedWatermarks = new(initialIndex.Watermarks!, StringComparer.Ordinal)
            {
                [movedId.Value] = 2,
            };
            Dictionary<string, ConversationProjectionDispatchReference> mutatedDispatches = new(initialIndex.Dispatches, StringComparer.Ordinal)
            {
                [movedId.Value] = new("dispatch-moved-00029", 2, IsPending: false),
            };
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                new ConversationProjectionIndexReadModel
                {
                    SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                    Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                    Watermarks = mutatedWatermarks,
                    Summaries = mutatedSummaries,
                    Dispatches = mutatedDispatches,
                    GenerationToken = originalToken, // stale valid-shaped 64-hex digest
                });

            spy.Reset();
            ConversationListResult stalePage2 = await handler.ListAsync(ListQuery(filter, pageSize: 25, cursor: oldCursor), cancellationToken);
            stalePage2.FreshnessState.ShouldBe(
                ProjectionTrustState.Forbidden,
                "DERIVED_STATE_FAIL_CLOSED_VIOLATION: stale valid-shaped GenerationToken must not validate cursor against changed rows");
            stalePage2.Conversations.ShouldBeEmpty();
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 14. Syntactically valid malformed detail/wrapper records (missing/invalid lifecycleState, missing
        // schemaVersion, null message element, empty/NBSP attribute key, type-corrupted wrapper metadata) must fail closed on BOTH Open and List.
        foreach (string mutation in new[] { "missing-lifecycle", "invalid-lifecycle", "missing-schema", "null-message", "message-without-required-fields", "empty-attr-key", "nbsp-attr-key", "bad-source-head-seq", "bad-replay-anchor", "bad-native-source-proof" })
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 3);
            string detailKey = ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA);
            JsonObject stored = backing.Snapshot<JsonObject>(ConversationProjectionReadModelKeys.StateStoreName, detailKey)!;
            JsonObject detailObj = stored["detail"]!.AsObject();
            switch (mutation)
            {
                case "missing-lifecycle":
                    _ = detailObj.Remove("lifecycleState");
                    break;
                case "invalid-lifecycle":
                    detailObj["lifecycleState"] = "Deleted";
                    break;
                case "missing-schema":
                    _ = detailObj.Remove("schemaVersion");
                    break;
                case "null-message":
                    detailObj["messages"] = new JsonArray((JsonNode?)null);
                    break;
                case "message-without-required-fields":
                    detailObj["messages"] = new JsonArray(new JsonObject());
                    break;
                case "empty-attr-key":
                    detailObj["attributes"] = new JsonObject { ["  "] = "bad" };
                    break;
                case "nbsp-attr-key":
                    detailObj["attributes"] = new JsonObject { ["\u00A0"] = "bad" };
                    break;
                case "bad-source-head-seq":
                    stored["sourceHeadSequence"] = "not-a-number";
                    break;
                case "bad-replay-anchor":
                    stored["replayAnchorAt"] = "not-a-timestamp";
                    break;
                case "bad-native-source-proof":
                    stored["nativeSourceProof"] = new JsonArray();
                    break;
            }

            backing.SeedRaw(ConversationProjectionReadModelKeys.StateStoreName, detailKey, stored);
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);
            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            open.FreshnessState.ShouldNotBe(ProjectionTrustState.Current, $"DERIVED_STATE_FAIL_CLOSED_VIOLATION: open accepted malformed detail ({mutation})");
            open.Details.ShouldBeNull();
            list.FreshnessState.ShouldNotBe(ProjectionTrustState.Current, $"DERIVED_STATE_FAIL_CLOSED_VIOLATION: list certified malformed detail ({mutation})");
            list.Conversations.ShouldNotContain(c => c.ConversationId == convA);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 15. Malformed recognized tenant-index fields, including sibling summaries, fail closed on Open.
        foreach (string indexMutation in new[] { "bad-schema", "bad-watermarks-type", "bad-watermark-value", "bad-lifecycle", "bad-sibling-dispatch-type", "bad-sibling-dispatch-position", "sibling-summary-missing-id", "sibling-summary-missing-freshness", "sibling-summary-invalid-id", "sibling-summary-negative-message-count", "null-dispatches-absent-open" })
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 3);
            string indexKey = ConversationProjectionReadModelKeys.TenantIndexKey(Tenant);
            JsonObject indexObj = backing.Snapshot<JsonObject>(ConversationProjectionReadModelKeys.StateStoreName, indexKey)!;
            ConversationId queryTarget = convA;
            switch (indexMutation)
            {
                case "bad-schema":
                    indexObj["schemaVersion"] = "two";
                    break;
                case "bad-watermarks-type":
                    indexObj["watermarks"] = "corrupt";
                    break;
                case "bad-watermark-value":
                    indexObj["watermarks"]!.AsObject()[convB.Value] = "not-a-number";
                    break;
                case "bad-lifecycle":
                    indexObj["lifecycle"] = "not-an-enum";
                    break;
                case "bad-sibling-dispatch-type":
                    indexObj["dispatches"]!.AsObject()[convB.Value] = "not-an-object";
                    break;
                case "bad-sibling-dispatch-position":
                    indexObj["dispatches"]!.AsObject()[convB.Value]!.AsObject()["lastAppliedEventPosition"] = "bad";
                    break;
                case "sibling-summary-missing-id":
                    indexObj["summaries"]!.AsArray().Add(new JsonObject());
                    break;
                case "sibling-summary-missing-freshness":
                    _ = indexObj["summaries"]!.AsArray()[1]!.AsObject().Remove("freshness");
                    break;
                case "sibling-summary-invalid-id":
                    indexObj["summaries"]!.AsArray()[1]!.AsObject()["conversationId"] = "conv: ";
                    break;
                case "sibling-summary-negative-message-count":
                    indexObj["summaries"]!.AsArray()[1]!.AsObject()["messageCount"] = -1;
                    break;
                case "null-dispatches-absent-open":
                    indexObj["dispatches"] = null;
                    queryTarget = new ConversationId("conversation-absent");
                    break;
            }

            backing.SeedRaw(ConversationProjectionReadModelKeys.StateStoreName, indexKey, indexObj);
            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);

            ConversationDetailResult open = await handler.GetAsync(OpenQuery(queryTarget), cancellationToken);
            open.FreshnessState.ShouldBeOneOf(ProjectionTrustState.Unavailable, ProjectionTrustState.Rebuilding);
            open.Details.ShouldBeNull();
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");
        }

        // 16. Legacy Web JSON numeric strings and the final value of a repeated lifecycle remain readable.
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 3);
            string indexKey = ConversationProjectionReadModelKeys.TenantIndexKey(Tenant);
            JsonObject index = backing.Snapshot<JsonObject>(ConversationProjectionReadModelKeys.StateStoreName, indexKey)!;
            index["schemaVersion"] = "2";
            index["watermarks"]!.AsObject()[convB.Value] = "1";
            JsonObject siblingDispatch = index["dispatches"]!.AsObject()[convB.Value]!.AsObject();
            siblingDispatch["lastAppliedEventPosition"] = "1";
            siblingDispatch["previousLastAppliedEventPosition"] = "1";
            index["summaries"]!.AsArray()[1]!.AsObject()["messageCount"] = "1";
            backing.SeedRaw(ConversationProjectionReadModelKeys.StateStoreName, indexKey, index);

            string detailKey = ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA);
            string detailJson = backing.Snapshot<JsonObject>(ConversationProjectionReadModelKeys.StateStoreName, detailKey)!.ToJsonString();
            using JsonDocument repeatedLifecycle = JsonDocument.Parse(detailJson.Replace(
                "\"detail\":{",
                "\"detail\":{\"lifecycleState\":\"Deleted\",",
                StringComparison.Ordinal));
            backing.SeedRaw(ConversationProjectionReadModelKeys.StateStoreName, detailKey, repeatedLifecycle);

            DerivedKeyReadStoreSpy spy = new(backing);
            ConversationQueryHandler handler = CreateHandler(spy);
            ConversationDetailResult open = await handler.GetAsync(OpenQuery(convA), cancellationToken);
            ConversationListResult list = await handler.ListAsync(ListQuery(filter), cancellationToken);

            open.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            open.Details!.LifecycleState.ShouldBe("Open");
            list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            list.Conversations.Select(summary => summary.ConversationId).ShouldContain(convA);
            spy.Writes.ShouldBeEmpty("READ_SIDE_REPAIR_ATTEMPTED");

            string scalarDetail = repeatedLifecycle.RootElement.GetProperty("detail").GetRawText();
            string malformedLabel = string.Concat("{\"label\":\"\\uD800\",", scalarDetail.AsSpan(1));
            JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
            _ = Should.Throw<Exception>(() => JsonSerializer.Deserialize<ConversationDetailProjectionV1>(malformedLabel, options));
            _ = Should.Throw<Exception>(
                () => JsonSerializer.Deserialize<ConversationDetailValidationHeader>(malformedLabel, options),
                "DERIVED_STATE_FAIL_CLOSED_VIOLATION: list accepted an undecodable label");
            AssertIndexAndDetailSerializerBinding(backing, convA);
        }
    }

    private static void AssertIndexAndDetailSerializerBinding(InMemoryReadModelStore backing, ConversationId targetId)
    {
        string storeName = ConversationProjectionReadModelKeys.StateStoreName;
        string indexKey = ConversationProjectionReadModelKeys.TenantIndexKey(Tenant);
        ConversationProjectionIndexReadModel index = backing.Snapshot<ConversationProjectionIndexReadModel>(storeName, indexKey)!;
        ConversationProjectedReadModels models = backing.Snapshot<ConversationProjectedReadModels>(
            storeName, ConversationProjectionReadModelKeys.ConversationKey(Tenant, targetId))!;
        ConversationDetailProjectionV1 detail = models.Detail;
        JsonSerializerOptions web = new(JsonSerializerDefaults.Web);
        string indexJson = JsonSerializer.Serialize(index, web);
        ConversationProjectionIndexLookupTarget? previous = ConversationProjectionIndexLookupScope.Begin(targetId.Value);
        try
        {
            string repeated = string.Concat("{\"summaries\":[null],\"dispatches\":null,", indexJson.AsSpan(1));
            ConversationProjectionIndexLookupResult lookup = JsonSerializer
                .Deserialize<ConversationProjectionIndexLookupResult>(repeated, web)!;
            lookup.MatchedSummary!.ConversationId.ShouldBe(targetId);
            lookup.DispatchReference.ShouldNotBeNull();
            string invalidEarlierSummary = string.Concat("{\"summaries\":[{}],", indexJson.AsSpan(1));
            _ = Should.Throw<JsonException>(
                () => JsonSerializer.Deserialize<ConversationProjectionIndexLookupResult>(invalidEarlierSummary, web));

            JsonSerializerOptions alternate = new(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = false,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            };
            JsonObject alternateIndex = JsonSerializer.SerializeToNode(index, alternate)!.AsObject();
            JsonObject alternateDetail = JsonSerializer.SerializeToNode(detail, alternate)!.AsObject();
            alternateIndex.Deserialize<ConversationProjectionIndexLookupResult>(alternate)!.MatchedSummary!.ConversationId.ShouldBe(targetId);
            alternateDetail.Deserialize<ConversationDetailValidationHeader>(alternate)!.ConversationId.ShouldBe(targetId);

            alternateIndex["unmapped"] = true;
            alternateDetail["unmapped"] = true;
            _ = Should.Throw<JsonException>(() => alternateIndex.Deserialize<ConversationProjectionIndexLookupResult>(alternate));
            _ = Should.Throw<JsonException>(() => alternateDetail.Deserialize<ConversationDetailValidationHeader>(alternate));

            JsonSerializerOptions customId = new(JsonSerializerDefaults.Web);
            customId.Converters.Add(new DerivedKeyConversationIdConverter());
            JsonSerializer.Deserialize<ConversationProjectionIndexLookupResult>(JsonSerializer.Serialize(index, customId), customId)!
                .MatchedSummary!.ConversationId.ShouldBe(targetId);
            AssertPageRoundTrip(models, customId);

            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(static info =>
            {
                if (info.Type == typeof(ConversationProjectedReadModels))
                {
                    info.Properties.Single(property => property.Name == "detail").Name = "projectionDetail";
                }
            });
            AssertPageRoundTrip(models, new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = resolver });

            JsonSerializerOptions preserve = new(JsonSerializerDefaults.Web) { ReferenceHandler = ReferenceHandler.Preserve };
            JsonObject preserveIndex = new()
            {
                ["summaries"] = new JsonObject
                {
                    ["$id"] = "summaries",
                    ["$values"] = new JsonArray(JsonSerializer.SerializeToNode(models.Summary, web)),
                },
                ["dispatches"] = JsonSerializer.SerializeToNode(index.Dispatches, web),
            };
            ConversationProjectionIndexReadModel expectedIndex = preserveIndex.Deserialize<ConversationProjectionIndexReadModel>(preserve)!;
            ConversationProjectionIndexLookupResult preservedLookup = preserveIndex.Deserialize<ConversationProjectionIndexLookupResult>(preserve)!;
            preservedLookup.MatchedSummary!.ConversationId.ShouldBe(expectedIndex.Summaries.ShouldHaveSingleItem().ConversationId);
            preservedLookup.MatchCount.ShouldBe(1);
        }
        finally
        {
            ConversationProjectionIndexLookupScope.End(previous);
        }
    }

    private static void AssertPageRoundTrip(ConversationProjectedReadModels models, JsonSerializerOptions options)
        => AssertPageValidation(JsonSerializer.Serialize(models, options), options);

    private static void AssertPageValidation(string json, JsonSerializerOptions options)
    {
        ConversationProjectedReadModels expected = JsonSerializer.Deserialize<ConversationProjectedReadModels>(json, options)!;
        ConversationPageValidationRecord page = JsonSerializer.Deserialize<ConversationPageValidationRecord>(json, options)!;
        page.Summary.ConversationId.ShouldBe(expected.Summary.ConversationId);
        page.Detail.ConversationId.ShouldBe(expected.Detail.ConversationId);
        page.Detail.Freshness.ShouldBe(expected.Detail.Freshness);
        page.DispatchId.ShouldBe(expected.DispatchId);
    }

    /// <summary>
    /// Persisted <c>GenerationToken</c> is bypassed when a cross-tenant poison summary row is present in <c>Summaries</c>,
    /// so the cursor token reflects only the tenant-scoped rows.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task PersistedGenerationTokenShouldBeBypassedWhenCrossTenantPoisonSummaryIsPresent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore backing = new();
        SeedReadyTenant(backing, 3);
        ConversationProjectionIndexReadModel clean = backing.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;

        ConversationId poisonId = new("conversation-poison");
        ConversationSummaryProjectionV1 poisonSummary = BuildModels(OtherTenant, poisonId, 1, "dispatch-poison").Summary;
        List<ConversationSummaryProjectionV1> poisonedSummaries = [.. clean.Summaries, poisonSummary];
        Dictionary<string, long> watermarks = new(clean.Watermarks!, StringComparer.Ordinal) { [poisonId.Value] = 1 };
        Dictionary<string, ConversationProjectionDispatchReference> dispatches = new(clean.Dispatches, StringComparer.Ordinal)
        {
            [poisonId.Value] = new("dispatch-poison", 1, IsPending: false),
        };
        string tokenIncludingPoison = ConversationIndexGenerationToken.Compute(poisonedSummaries);
        string tokenCleanOnly = ConversationIndexGenerationToken.Compute(clean.Summaries);
        tokenIncludingPoison.ShouldNotBe(tokenCleanOnly);

        backing.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Watermarks = watermarks,
                Summaries = poisonedSummaries,
                Dispatches = dispatches,
                GenerationToken = tokenIncludingPoison,
            });

        ConversationQueryHandler handler = CreateHandler(backing);
        ConversationListResult page1 = await handler.ListAsync(ListQuery(new(), pageSize: 2), cancellationToken);
        page1.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();

        // Replace with clean tenant-only index carrying clean token: cursor minted above must match clean-only token.
        backing.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Watermarks = clean.Watermarks,
                Summaries = clean.Summaries,
                Dispatches = clean.Dispatches,
                GenerationToken = tokenCleanOnly,
            });

        ConversationListResult page2 = await handler.ListAsync(ListQuery(new(), pageSize: 2, cursor: page1.Page.ContinuationCursor), cancellationToken);
        page2.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        page2.Conversations.Count.ShouldBe(1);
    }

    /// <summary>
    /// <see cref="ConversationIndexGenerationToken.Compute"/> is order-independent across unsorted summaries and
    /// <see cref="ProviderCorrelationMetadata.ExtensionData"/>, sensitive to mutations, and covers every public
    /// property on <see cref="ConversationSummaryProjectionV1"/>.
    /// </summary>
    [Fact]
    public void GenerationTokenShouldHashAllSummaryPropertiesCanonically()
    {
        // Reflection guard: any newly added property on ConversationSummaryProjectionV1 must remain covered.
        string[] expectedProperties =
        [
            "BusinessReference",
            "ConversationId",
            "FileReferenceCount",
            "FolderId",
            "Freshness",
            "Label",
            "LifecycleState",
            "MessageCount",
            "ParticipantPartyIds",
            "ProjectId",
            "ProviderCorrelation",
            "SchemaVersion",
            "SearchTrustPreview",
            "TenantId",
        ];
        string[] actualProperties =
        [
            .. typeof(ConversationSummaryProjectionV1)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(static p => p.Name)
                .OrderBy(static n => n, StringComparer.Ordinal),
        ];
        actualProperties.ShouldBe(expectedProperties);

        ConversationSummaryProjectionV1 s1 = BuildModels(Tenant, new ConversationId("conv-a"), 1, "d-a").Summary with
        {
            ProviderCorrelation = new ProviderCorrelationMetadata(
                "openai",
                "assistant",
                SchemaVersion.Current,
                "sess-1",
                "resp-1",
                new Dictionary<string, string>(StringComparer.Ordinal) { ["a"] = "1", ["b"] = "2" }),
        };
        ConversationSummaryProjectionV1 s1MutatedExt = s1 with
        {
            ProviderCorrelation = new ProviderCorrelationMetadata(
                "openai",
                "assistant",
                SchemaVersion.Current,
                "sess-1",
                "resp-1",
                new Dictionary<string, string>(StringComparer.Ordinal) { ["a"] = "1", ["b"] = "changed" }),
        };
        ConversationSummaryProjectionV1 s2 = BuildModels(Tenant, new ConversationId("conv-b"), 1, "d-b").Summary;

        string orderedToken = ConversationIndexGenerationToken.Compute([s1, s2]);
        string unsortedToken = ConversationIndexGenerationToken.Compute([s2, s1]);
        string mutatedToken = ConversationIndexGenerationToken.Compute([s1MutatedExt, s2]);

        // Pin byte-for-byte digest compatibility with pre-Story-12.3 JsonSerializer.SerializeToUtf8Bytes implementation.
        string expectedPreviousDigest = Convert.ToHexStringLower(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(
                new[] { s2, s1 }.OrderBy(static s => s.ConversationId.Value, StringComparer.Ordinal))));

        ConversationIndexGenerationToken.IsValidPersistedToken(orderedToken).ShouldBeTrue();
        orderedToken.ShouldBe(expectedPreviousDigest, "DERIVED_KEY_TRANSITION_UNSAFE: streaming token must match pre-deployment digest");
        unsortedToken.ShouldBe(orderedToken);
        mutatedToken.ShouldNotBe(orderedToken);
    }

    /// <summary>
    /// <see cref="ConversationProjectionReadStore.ValidatePageAsync"/> observes page details BEFORE reading dispatch
    /// ledgers so a detail restored during redelivery cannot combine with an earlier expired-ledger read to falsely
    /// report <c>Current</c>, and a failed detail bulk read never launches a sibling ledger read.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ValidatePageShouldObserveLedgerAfterDetailReadAndNotLeakReadsOnFailure()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ConversationId convA = new("conversation-00000");

        // Case A: Detail restoration race — detail is missing and ledger expired; while detail bulk read is gated,
        // redelivery creates Pending ledger (step 1) and writes restored detail (step 5) before completion (step 10).
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 2);
            string detailKey = ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA);
            string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-00000");
            ConversationProjectedReadModels originalDetail = backing.Snapshot<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                detailKey)!;

            // Remove detail and expire ledger.
            (bool dp, string dEtag) = await backing.TryReadEtagAsync(ConversationProjectionReadModelKeys.StateStoreName, detailKey, cancellationToken);
            dp.ShouldBeTrue();
            (await backing.TryEraseAsync(ConversationProjectionReadModelKeys.StateStoreName, detailKey, dEtag, cancellationToken)).ShouldBeTrue();
            (bool lp, string lEtag) = await backing.TryReadEtagAsync(ConversationProjectionReadModelKeys.StateStoreName, ledgerKey, cancellationToken);
            lp.ShouldBeTrue();
            (await backing.TryEraseAsync(ConversationProjectionReadModelKeys.StateStoreName, ledgerKey, lEtag, cancellationToken)).ShouldBeTrue();

            TaskCompletionSource detailEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource releaseDetail = new(TaskCreationOptions.RunContinuationsAsynchronously);
            DerivedKeyGatedBulkStore gatedStore = new(backing)
            {
                OnDetailBulkReadAsync = async ct =>
                {
                    detailEntered.SetResult();
                    await releaseDetail.Task.WaitAsync(ct).ConfigureAwait(false);
                },
            };

            ConversationQueryHandler handler = CreateHandler(gatedStore);
            Task<ConversationListResult> listTask = handler.ListAsync(ListQuery(new()), cancellationToken).AsTask();

            await detailEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            gatedStore.DetailBulkCalls.ShouldBe(1);
            gatedStore.LedgerBulkCalls.ShouldBe(0, "ledger bulk read must not start before detail bulk read completes");

            // Redelivery step 1 (Pending ledger) + step 5 (restored detail), before step 10 (Completed ledger).
            backing.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ledgerKey,
                new ConversationProjectionDispatchLedger(
                    "dispatch-00000",
                    "fingerprint-00000",
                    Tenant,
                    convA,
                    originalDetail.Summary.Freshness.ProjectionGeneratedAt,
                    ConversationProjectionDispatchStatus.Pending));
            backing.SeedRaw(ConversationProjectionReadModelKeys.StateStoreName, detailKey, originalDetail);

            releaseDetail.SetResult();
            ConversationListResult result = await listTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            gatedStore.LedgerBulkCalls.ShouldBe(1);
            result.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding, "DERIVED_STATE_FAIL_CLOSED_VIOLATION: in-flight restoration must not certify Current");
            result.Conversations.ShouldNotContain(c => c.ConversationId == convA);
        }

        // Case B: Detail bulk read fails -> ledger bulk read is never started (zero leaked sibling reads).
        {
            InMemoryReadModelStore backing = new();
            SeedReadyTenant(backing, 2);
            DerivedKeyGatedBulkStore gatedStore = new(backing)
            {
                OnDetailBulkReadAsync = static _ => Task.FromException(new InvalidOperationException("Simulated detail bulk read outage.")),
            };
            ConversationProjectionReadStore readStore = new(gatedStore);
            ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(Tenant, cancellationToken);
            _ = await Should.ThrowAsync<InvalidOperationException>(
                () => readStore.ValidatePageAsync(Tenant, snapshot, snapshot.Summaries, cancellationToken).AsTask());
            gatedStore.DetailBulkCalls.ShouldBe(1);
            gatedStore.LedgerBulkCalls.ShouldBe(0);
        }
    }

    /// <summary>
    /// Provider correlation extension comparison preserves ordinal equality in linear time across populated bags
    /// and non-ordinal dictionary implementations.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ProviderCorrelationExtensionComparisonShouldBeLinearAndOrdinal()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore backing = new();
        SeedReadyTenant(backing, 2);
        ConversationId convA = new("conversation-00000");

        Dictionary<string, string> bagForward = new(StringComparer.Ordinal);
        Dictionary<string, string> bagReverse = new(StringComparer.Ordinal);
        for (int i = 0; i < 150; i++)
        {
            bagForward[$"ext-key-{i:000}"] = $"val-{i:000}";
        }

        for (int i = 149; i >= 0; i--)
        {
            bagReverse[$"ext-key-{i:000}"] = $"val-{i:000}";
        }

        ConversationProjectedReadModels original = backing.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA))!;
        ConversationSummaryProjectionV1 withForward = original.Summary with
        {
            ProviderCorrelation = new ProviderCorrelationMetadata("openai", "assistant", SchemaVersion.Current, "s-1", "r-1", bagForward),
        };
        ConversationSummaryProjectionV1 withReverse = original.Summary with
        {
            ProviderCorrelation = new ProviderCorrelationMetadata("openai", "assistant", SchemaVersion.Current, "s-1", "r-1", bagReverse),
        };
        ConversationProjectedReadModels updatedDetail = new(withForward, original.Detail with { ProviderCorrelation = withForward.ProviderCorrelation })
        {
            DispatchId = original.DispatchId,
        };
        backing.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA),
            updatedDetail);
        ConversationProjectionIndexReadModel idx = backing.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
        backing.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                SchemaVersion = idx.SchemaVersion,
                Lifecycle = idx.Lifecycle,
                Watermarks = idx.Watermarks,
                Summaries = [withReverse, .. idx.Summaries.Skip(1)],
                Dispatches = idx.Dispatches,
            });

        ConversationProjectionReadStore readStore = new(backing);
        ConversationProjectedReadModels? opened = await readStore.ReadAsync(Tenant, convA, cancellationToken);
        opened.ShouldNotBeNull();

        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(Tenant, cancellationToken);
        IReadOnlySet<string> inconsistent = await readStore.ValidatePageAsync(Tenant, snapshot, snapshot.Summaries, cancellationToken);
        inconsistent.ShouldBeEmpty();

        // Case-different key must fail ordinal equality (even when compared against a case-insensitive dictionary).
        Dictionary<string, string> caseDifferent = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> kv in bagForward)
        {
            caseDifferent[kv.Key.ToUpperInvariant()] = kv.Value;
        }

        ConversationSummaryProjectionV1 withCaseDifferent = original.Summary with
        {
            ProviderCorrelation = new ProviderCorrelationMetadata("openai", "assistant", SchemaVersion.Current, "s-1", "r-1", caseDifferent),
        };
        IReadOnlySet<string> caseMismatch = await readStore.ValidatePageAsync(Tenant, snapshot, [withCaseDifferent], cancellationToken);
        caseMismatch.ShouldContain(convA.Value);
    }

    /// <summary>
    /// <see cref="ConversationProjectionReadStore.ValidatePageAsync"/> withholds a row when <c>Summary</c>,
    /// <c>DispatchId</c>, and ledger all match while only <c>Detail</c> header fields disagree.
    /// </summary>
    /// <param name="mismatchKind">Which isolated detail-header field is mutated.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("detail-freshness")]
    [InlineData("detail-tenant")]
    [InlineData("detail-conversation")]
    public async Task ValidatePageShouldWithholdWhenOnlyDetailHeaderDisagrees(string mismatchKind)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore store = new();
        SeedReadyTenant(store, 2);
        ConversationId convA = new("conversation-00000");
        ConversationProjectedReadModels original = store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA))!;

        ConversationDetailProjectionV1 d = original.Detail;
        ProjectionFreshnessV1 f = d.Freshness;
        ConversationDetailProjectionV1 mutatedDetail = mismatchKind switch
        {
            "detail-freshness" => new ConversationDetailProjectionV1(
                d.SchemaVersion,
                d.TenantId,
                d.ConversationId,
                new ProjectionFreshnessV1(
                    f.ProjectionContractSchemaVersion,
                    f.ProjectionCursor,
                    f.LastAppliedEventPosition,
                    f.LastAppliedEventTimestamp,
                    f.ProjectionGeneratedAt.AddSeconds(5),
                    f.LagDuration,
                    f.IsStale,
                    f.FreshnessState,
                    f.ReasonCode),
                d.LifecycleState,
                d.Label,
                d.BusinessReference,
                d.ProjectId,
                d.FolderId,
                d.ProviderCorrelation,
                d.Participants,
                d.Messages,
                d.FileReferences,
                d.Attributes,
                d.ActiveRetentionPolicy,
                d.SensitivityMarks,
                d.Redactions,
                d.TrustPosture,
                d.EvidenceEntries),
            "detail-tenant" => new ConversationDetailProjectionV1(
                d.SchemaVersion,
                OtherTenant,
                d.ConversationId,
                d.Freshness,
                d.LifecycleState,
                d.Label,
                d.BusinessReference,
                d.ProjectId,
                d.FolderId,
                d.ProviderCorrelation,
                d.Participants,
                d.Messages,
                d.FileReferences,
                d.Attributes,
                d.ActiveRetentionPolicy,
                d.SensitivityMarks,
                d.Redactions,
                d.TrustPosture,
                d.EvidenceEntries),
            "detail-conversation" => new ConversationDetailProjectionV1(
                d.SchemaVersion,
                d.TenantId,
                new ConversationId("conversation-00001"),
                d.Freshness,
                d.LifecycleState,
                d.Label,
                d.BusinessReference,
                d.ProjectId,
                d.FolderId,
                d.ProviderCorrelation,
                d.Participants,
                d.Messages,
                d.FileReferences,
                d.Attributes,
                d.ActiveRetentionPolicy,
                d.SensitivityMarks,
                d.Redactions,
                d.TrustPosture,
                d.EvidenceEntries),
            _ => throw new ArgumentOutOfRangeException(nameof(mismatchKind)),
        };

        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, convA),
            new ConversationProjectedReadModels(original.Summary, mutatedDetail) { DispatchId = original.DispatchId });

        ConversationProjectionReadStore readStore = new(store);
        ConversationProjectionIndexSnapshot snapshot = await readStore.ListAsync(Tenant, cancellationToken);
        IReadOnlySet<string> inconsistent = await readStore.ValidatePageAsync(Tenant, snapshot, snapshot.Summaries, cancellationToken);

        inconsistent.ShouldContain(convA.Value);
        inconsistent.ShouldNotContain("conversation-00001");
    }

    /// <summary>
    /// Concurrent asynchronous <see cref="ConversationProjectionReadStore.ReadAsync"/> calls isolate
    /// <see cref="ConversationProjectionIndexLookupScope"/> across async continuations.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ConcurrentAsyncReadAsyncShouldIsolateLookupScope()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryReadModelStore backing = new();
        SeedReadyTenant(backing, 12);
        DerivedKeyReadStoreSpy spy = new(backing) { AsyncReadDelay = TimeSpan.FromMilliseconds(10) };
        ConversationProjectionReadStore readStore = new(spy);

        Task<(ConversationId Expected, ConversationProjectedReadModels? Actual)>[] tasks = Enumerable.Range(0, 12)
            .Select(async index =>
            {
                ConversationId id = new($"conversation-{index:00000}");
                ConversationProjectedReadModels? result = await readStore.ReadAsync(Tenant, id, cancellationToken);
                return (id, result);
            })
            .ToArray();

        (ConversationId Expected, ConversationProjectedReadModels? Actual)[] results = await Task.WhenAll(tasks);
        foreach ((ConversationId expected, ConversationProjectedReadModels? actual) in results)
        {
            actual.ShouldNotBeNull();
            actual!.Summary.ConversationId.ShouldBe(expected);
            actual.Detail.ConversationId.ShouldBe(expected);
        }
    }

    /// <summary>
    /// Direct <see cref="ConversationProjectionReadStore.ClassifyLifecycle"/> preserves per-summary gap detection
    /// on duplicate IDs, <see cref="ConversationProjectionIndexLookupConverter"/> enforces last-write-wins and rejects
    /// non-string IDs, <see cref="IncrementalHashBufferWriter"/> guards disposal/bounds, and pending markers preserve
    /// the persisted generation token.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task InternalGuardsShouldHandleDuplicatesMalformedLookupAndBufferDisposal()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ConversationId convA = new("conversation-00000");
        ConversationSummaryProjectionV1 pos1 = BuildModels(Tenant, convA, position: 1, dispatchId: "d1").Summary;
        ConversationSummaryProjectionV1 pos2 = BuildModels(Tenant, convA, position: 2, dispatchId: "d2").Summary;

        // Direct ClassifyLifecycle with duplicate summary where one entry lags watermark must report GapDetected.
        ConversationProjectionIndexReadModel dupGap = new()
        {
            SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
            Lifecycle = ConversationProjectionIndexLifecycle.Ready,
            Watermarks = new Dictionary<string, long>(StringComparer.Ordinal) { [convA.Value] = 2 },
            Summaries = [pos2, pos1],
        };
        ConversationProjectionReadStore.ClassifyLifecycle(dupGap).ShouldBe(ConversationProjectionIndexReadState.GapDetected);

        // Lookup converter: duplicate "summaries" uses last-write-wins, non-string conversationId throws JsonException.
        JsonSerializerOptions web = new(JsonSerializerDefaults.Web);
        string summaryJson = JsonSerializer.Serialize(pos1, web);
        string dupSummariesJson = $"{{\"summaries\":[{summaryJson}],\"summaries\":[{summaryJson}],\"dispatches\":{{\"{convA.Value}\":{{\"dispatchId\":\"d1\",\"lastAppliedEventPosition\":1}}}}}}";
        ConversationProjectionIndexLookupTarget? prev = ConversationProjectionIndexLookupScope.Begin(convA.Value);
        try
        {
            ConversationProjectionIndexLookupResult? parsed =
                JsonSerializer.Deserialize<ConversationProjectionIndexLookupResult>(dupSummariesJson, web);
            parsed.ShouldNotBeNull();
            parsed!.MatchCount.ShouldBe(1);
            parsed.MatchedSummary.ShouldNotBeNull();

            string nonStringIdJson = "{\"summaries\":[{\"conversationId\":12345}]}";
            _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ConversationProjectionIndexLookupResult>(nonStringIdJson, web));
        }
        finally
        {
            ConversationProjectionIndexLookupScope.End(prev);
        }

        // IncrementalHashBufferWriter guards negative sizeHint, overflow count, and disposed calls.
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        IncrementalHashBufferWriter writer = new(hash);
        _ = Should.Throw<ArgumentOutOfRangeException>(() => writer.GetSpan(-1));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => writer.GetMemory(-1));
        _ = Should.Throw<InvalidOperationException>(() => writer.Advance(int.MaxValue));
        writer.Dispose();
        _ = Should.Throw<ObjectDisposedException>(() => writer.GetSpan(1));
        _ = Should.Throw<ObjectDisposedException>(() => writer.GetMemory(1));
        _ = Should.Throw<ObjectDisposedException>(() => writer.Advance(0));

        // Compose omits persisted GenerationToken (zero write overhead) while preserving SchemaVersion=2, Lifecycle, and Watermarks.
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter readModelWriter = new(store);
        await readModelWriter.InitializeTenantIndexAsync(Tenant, cancellationToken);
        ConversationProjectedReadModels initial = BuildModels(Tenant, convA, 1, "dispatch-1");
        await readModelWriter.PersistGenerationAsync(initial, cancellationToken);
        ConversationProjectionIndexReadModel afterPersist = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
        afterPersist.SchemaVersion.ShouldBe(ConversationProjectionIndexReadModel.LifecycleSchemaVersion);
        afterPersist.GenerationToken.ShouldBeNull();

        await readModelWriter.MarkGenerationPendingAsync(BuildModels(Tenant, convA, 2, "dispatch-2"), cancellationToken);
        ConversationProjectionIndexReadModel afterPending = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
        afterPending.SchemaVersion.ShouldBe(ConversationProjectionIndexReadModel.LifecycleSchemaVersion);
        afterPending.GenerationToken.ShouldBeNull();
    }

    private static void ProfileScaleRow(
        ITestOutputHelper? output,
        InMemoryReadModelStore backing,
        ConversationQueryHandler handler,
        ConversationListFilterV1 filter,
        int rowCount,
        ConversationId targetId,
        CancellationToken cancellationToken)
    {
        if (output is null)
        {
            return;
        }

        int iterations = rowCount switch
        {
            <= 100 => 40,
            <= 1_000 => 10,
            _ => 3,
        };

        // Warmup
        ConversationListResult warmP1 = handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, Caller, "warm-p1", filter, new ConversationPageRequest(25)),
            cancellationToken).AsTask().GetAwaiter().GetResult();
        _ = handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, Caller, "warm-p2", filter, new ConversationPageRequest(25, warmP1.Page.ContinuationCursor)),
            cancellationToken).AsTask().GetAwaiter().GetResult();
        _ = handler.GetAsync(OpenQuery(targetId), cancellationToken).AsTask().GetAwaiter().GetResult();

        (double listP1Us, long listP1Alloc) = MeasureSync(() =>
        {
            ConversationListResult r = handler.ListAsync(
                new ListConversationsQuery(SchemaVersion.Current, Tenant, Caller, "prof-p1", filter, new ConversationPageRequest(25)),
                cancellationToken).AsTask().GetAwaiter().GetResult();
            GC.KeepAlive(r);
        }, iterations);

        string cursor = warmP1.Page.ContinuationCursor!;
        (double listP2Us, long listP2Alloc) = MeasureSync(() =>
        {
            ConversationListResult r = handler.ListAsync(
                new ListConversationsQuery(SchemaVersion.Current, Tenant, Caller, "prof-p2", filter, new ConversationPageRequest(25, cursor)),
                cancellationToken).AsTask().GetAwaiter().GetResult();
            GC.KeepAlive(r);
        }, iterations);

        (double openUs, long openAlloc) = MeasureSync(() =>
        {
            ConversationDetailResult r = handler.GetAsync(OpenQuery(targetId), cancellationToken).AsTask().GetAwaiter().GetResult();
            GC.KeepAlive(r);
        }, iterations);

        // Measure full two-phase write path (MarkGenerationPendingAsync + PersistGenerationAsync) on a fresh clone.
        ConversationProjectionIndexReadModel indexSnapshot = backing.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
        InMemoryReadModelStore writeStore = new();
        writeStore.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            indexSnapshot);
        DerivedKeyReadStoreSpy writeSpy = new(writeStore);
        ConversationProjectionReadModelWriter writer = new(writeSpy);
        ConversationProjectedReadModels writeSample = BuildModels(Tenant, new ConversationId($"conversation-{rowCount + 1:00000}"), position: 1, dispatchId: $"dispatch-w-{rowCount}");
        writer.MarkGenerationPendingAsync(writeSample, cancellationToken).GetAwaiter().GetResult();
        writer.PersistGenerationAsync(writeSample, cancellationToken).GetAwaiter().GetResult();

        int writeCounter = 2;
        int writeIterations = Math.Max(2, iterations / 2);
        writeSpy.Reset();
        (double writeUs, long writeAlloc) = MeasureSync(() =>
        {
            ConversationProjectedReadModels next = BuildModels(Tenant, targetId, position: writeCounter, dispatchId: $"dispatch-w-{writeCounter}");
            writeCounter++;
            writer.MarkGenerationPendingAsync(next, cancellationToken).GetAwaiter().GetResult();
            writer.PersistGenerationAsync(next, cancellationToken).GetAwaiter().GetResult();
        }, writeIterations);
        int writeReadsPerOp = writeSpy.GetCalls / writeIterations;
        int writeWritesPerOp = writeSpy.Writes.Count / writeIterations;

        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"SCALE_PROFILE|rows={rowCount}|listP1_us={listP1Us:F1}|listP1_alloc={listP1Alloc}|listP2_us={listP2Us:F1}|listP2_alloc={listP2Alloc}|open_us={openUs:F1}|open_alloc={openAlloc}|fullWrite_us={writeUs:F1}|fullWrite_alloc={writeAlloc}|fullWrite_reads={writeReadsPerOp}|fullWrite_writes={writeWritesPerOp}"));
    }

    private static (double MeanMicroseconds, long AllocBytesPerOp) MeasureSync(Action action, int iterations)
    {
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }

        double elapsedUs = Stopwatch.GetElapsedTime(start).TotalMicroseconds / iterations;
        long allocBytes = (GC.GetAllocatedBytesForCurrentThread() - allocBefore) / iterations;
        return (elapsedUs, allocBytes);
    }

    private static ListConversationsQuery ListQuery(ConversationListFilterV1 filter, int pageSize = 25, string? cursor = null)
        => new(SchemaVersion.Current, Tenant, Caller, "corr-list", filter, new ConversationPageRequest(pageSize, cursor));

    private static GetConversationQuery OpenQuery(ConversationId conversationId)
        => new(SchemaVersion.Current, Tenant, Caller, "corr-open", conversationId);

    private static ConversationQueryHandler CreateHandler(IReadModelStore store)
    {
        DerivedKeyAllowTenantAccessService access = new(Tenant);
        ConversationProjectionReadStore readStore = new(store);
        ConversationProjectionReadService readService = new(access, readStore);
        QueryCursorCodec cursorCodec = new(new EphemeralDataProtectionProvider(), "Hexalith.Conversations.Story12.3.Cursor.v1");
        return new ConversationQueryHandler(
            access,
            readStore,
            readService,
            cursorCodec,
            timeProvider: new DerivedKeyFixedTimeProvider(BaseTimestamp.AddMinutes(1)),
            hydrationService: new ConversationReadHydrationService());
    }

    private static void SeedReadyTenant(
        InMemoryReadModelStore store, int rowCount, int richRowCount = 1, int richMessageCount = 10)
    {
        List<ConversationSummaryProjectionV1> summaries = new(rowCount);
        Dictionary<string, ConversationProjectionDispatchReference> dispatches = new(rowCount, StringComparer.Ordinal);
        Dictionary<string, long> watermarks = new(rowCount, StringComparer.Ordinal);

        for (int index = 0; index < rowCount; index++)
        {
            ConversationId conversationId = new($"conversation-{index:00000}");
            string dispatchId = $"dispatch-{index:00000}";
            ConversationProjectedReadModels models = BuildModels(
                Tenant, conversationId, position: 1, dispatchId, rich: index < richRowCount, richMessageCount: richMessageCount);
            summaries.Add(models.Summary);
            dispatches[conversationId.Value] = new ConversationProjectionDispatchReference(dispatchId, 1, IsPending: false);
            watermarks[conversationId.Value] = 1;

            store.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, conversationId),
                models);
            store.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.DispatchLedgerKey(dispatchId),
                new ConversationProjectionDispatchLedger(
                    dispatchId,
                    $"fingerprint-{index:00000}",
                    Tenant,
                    conversationId,
                    models.Summary.Freshness.ProjectionGeneratedAt,
                    ConversationProjectionDispatchStatus.Completed));
        }

        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                SchemaVersion = ConversationProjectionIndexReadModel.LifecycleSchemaVersion,
                Lifecycle = ConversationProjectionIndexLifecycle.Ready,
                Watermarks = watermarks,
                Summaries = summaries,
                Dispatches = dispatches,
            });
    }

    private static ConversationProjectedReadModels BuildModels(
        TenantId tenantId,
        ConversationId conversationId,
        long position,
        string dispatchId,
        bool rich = false,
        DateTimeOffset? timestampOverride = null,
        int richMessageCount = 10)
    {
        DateTimeOffset eventTime = timestampOverride ?? BaseTimestamp;
        ProjectionFreshnessV1 freshness = new(
            SchemaVersion.Current,
            $"pos:{position:0000000000}",
            position,
            eventTime,
            eventTime.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

        ProviderCorrelationMetadata providerCorrelation = new(
            "openai",
            "assistant",
            SchemaVersion.Current,
            $"sess-{conversationId.Value}",
            $"resp-{dispatchId}",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["region"] = "eu-west",
                ["channel"] = "web",
                ["model"] = "gpt-4.1",
                ["trace"] = dispatchId,
            });

        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            freshness,
            "Open",
            $"Conversation {conversationId.Value}",
            Business,
            Project,
            Folder,
            [Actor, Participant],
            MessageCount: rich ? richMessageCount : 1,
            FileReferenceCount: rich ? 1 : 0,
            ProviderCorrelation: providerCorrelation);

        ConversationDetailProjectionV1 detail = rich
            ? BuildRichDetail(summary, richMessageCount)
            : new ConversationDetailProjectionV1(
                SchemaVersion.Current,
                tenantId,
                conversationId,
                freshness,
                "Open",
                summary.Label,
                Business,
                Project,
                Folder,
                ProviderCorrelation: providerCorrelation,
                Participants: [new ConversationParticipantProjectionV1(Actor, ParticipantType.Human, ParticipantRole.Member)],
                Messages: [new ConversationTimelineMessageProjectionV1(new MessageId($"msg-{conversationId.Value}"), Actor, "Visible message", eventTime)]);

        return new ConversationProjectedReadModels(summary, detail) { DispatchId = dispatchId };
    }

    private static void EmitCandidateBinding()
    {
        Assembly serverAssembly = typeof(ConversationProjectionReadStore).Assembly;
        Assembly testAssembly = typeof(DerivedKeyOptimizationTests).Assembly;
        string info = serverAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        int plus = info.LastIndexOf('+');
        string commit = plus >= 0 && plus + 1 < info.Length ? info[(plus + 1)..] : "unknown";
        string serverSha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(serverAssembly.Location)));
        string testSha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(testAssembly.Location)));
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"CANDIDATE_BINDING|commit={commit}|serverSha256={serverSha}|testAssemblySha256={testSha}");
    }

    private static ConversationDetailProjectionV1 BuildRichDetail(ConversationSummaryProjectionV1 summary, int messageCount)
    {
        ConversationTimelineMessageProjectionV1[] messages =
        [
            .. Enumerable.Range(0, messageCount).Select(index => new ConversationTimelineMessageProjectionV1(
                new MessageId($"msg-rich-{index:00}"),
                index % 2 == 0 ? Actor : Participant,
                index == messageCount - 1 ? "[redacted]" : $"Visible message {index:00}",
                BaseTimestamp.AddSeconds(index))),
        ];

        return new ConversationDetailProjectionV1(
            SchemaVersion.Current,
            summary.TenantId,
            summary.ConversationId,
            summary.Freshness,
            "Open",
            summary.Label,
            Business,
            Project,
            Folder,
            ProviderCorrelation: summary.ProviderCorrelation,
            Participants:
            [
                new ConversationParticipantProjectionV1(Actor, ParticipantType.Human, ParticipantRole.Facilitator),
                new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member),
            ],
            Messages: messages,
            FileReferences:
            [
                new ConversationFileReferenceProjectionV1(new FileId("file-12-3"), Folder, messages[0].MessageId, BaseTimestamp),
            ],
            TrustPosture: ConversationEvidenceTrustPostureV1.FromFreshness(
                SchemaVersion.Current,
                summary.TenantId,
                summary.ConversationId,
                summary.Freshness),
            EvidenceEntries:
            [
                new ConversationEvidenceEntryV1(
                    $"message:{messages[0].MessageId.Value}",
                    "Message",
                    Actor,
                    BaseTimestamp,
                    ProjectionTrustState.Current,
                    ConversationCitationAvailability.Available,
                    ConversationAuditReadinessState.Ready,
                    ProjectionTrustState.Current,
                    MessageId: messages[0].MessageId,
                    VisibleText: messages[0].Text),
            ]);
    }
}
