// <copyright file="ConversationQueryHandlerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Verifies tenant-safe conversation query handling.
/// </summary>
public sealed class ConversationQueryHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly BusinessReference Business = new("crm", "case-123");
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Tenant denial returns the same hidden shape as a missing record and never reads projection storage.
    /// </summary>
    [Fact]
    public async Task DetailDeniedTenantShouldNotReadProjection()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001",
            ConversationTenantAccessDenialReason.MissingMember));
        FakeProjectionReadStore store = new();
        ConversationQueryHandler handler = new(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(0);
        access.Calls.ShouldBe(1);
    }

    /// <summary>
    /// Projection poison data is denied instead of trusting tenant ids returned by storage.
    /// </summary>
    [Fact]
    public async Task DetailShouldRejectProjectionTenantMismatch()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModels(OtherTenant, Conversation),
        };
        ConversationQueryHandler handler = new(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// List authorization occurs before any filter evaluation or projection read.
    /// </summary>
    [Fact]
    public async Task ListDeniedTenantShouldNotReadOrFilterProjection()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001",
            ConversationTenantAccessDenialReason.TenantDisabled));
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
            ],
        };
        ConversationQueryHandler handler = new(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(BusinessReference: Business)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Conversations.ShouldBeEmpty();
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// List filters are exact, tenant-scoped, and do not trust mixed-tenant projection rows.
    /// </summary>
    [Fact]
    public async Task ListShouldApplyTenantScopeBeforeFiltersAndPagination()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(OtherTenant, new ConversationId("conversation-poison"), Business, Project, Folder, Participant),
                Summary(Tenant, Conversation, Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conversation-folder-miss"), Business, Project, new FolderId("folder-other"), Participant),
            ],
        };
        ConversationQueryHandler handler = new(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(
                    Business,
                    Project,
                    Folder,
                    "Open",
                    ParticipantPartyId: Participant),
                new ConversationPageRequest(10)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Conversations.Count.ShouldBe(1);
        result.Conversations[0].ConversationId.ShouldBe(Conversation);
        result.Page.ReturnedCount.ShouldBe(1);
    }

    /// <summary>
    /// Malformed cursors fail closed before authorization-sensitive reads.
    /// </summary>
    [Fact]
    public async Task MalformedCursorShouldFailClosedWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new();
        ConversationQueryHandler handler = new(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, "not-a-valid-cursor")),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Conversations.ShouldBeEmpty();
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Caller-mismatched cursors fail closed after authorization and do not widen reads.
    /// </summary>
    [Fact]
    public async Task CallerMismatchedCursorShouldNotFallBackToFirstPage()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryHandler handler = new(access, store);
        string cursor = ConversationQueryCursor.EncodeForTests(
            Tenant,
            "different-caller",
            ConversationListFilterV1.Empty,
            offset: 1,
            issuedAt: Now);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, cursor)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Conversations.ShouldBeEmpty();
        store.ListReads.ShouldBe(0);
    }

    private static FakeTenantAccessService AllowedAccess()
        => new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001"));

    private static ConversationProjectedReadModels ProjectedModels(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            Detail(tenantId, conversationId));

    private static ConversationSummaryProjectionV1 Summary(
        TenantId tenantId,
        ConversationId conversationId,
        BusinessReference? business,
        ProjectId? project,
        FolderId? folder,
        PartyId participant)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness(),
            "Open",
            "Case 123",
            business,
            project,
            folder,
            [participant],
            MessageCount: 1,
            FileReferenceCount: 0);

    private static ConversationDetailProjectionV1 Detail(TenantId tenantId, ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness(),
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello from the adopter.", Now)],
            []);

    private static ProjectionFreshnessV1 Freshness()
        => new(
            SchemaVersion.Current,
            "pos:0000000001",
            1,
            Now,
            Now.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public int Calls { get; private set; }

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
        {
            Calls++;
            return ValueTask.FromResult(decision);
        }
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public ConversationProjectedReadModels? Models { get; set; }

        public IReadOnlyList<ConversationSummaryProjectionV1> Summaries { get; set; } = [];

        public int DetailReads { get; private set; }

        public int ListReads { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            DetailReads++;
            return ValueTask.FromResult(Models);
        }

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            ListReads++;
            return ValueTask.FromResult(Summaries);
        }
    }
}
