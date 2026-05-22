// <copyright file="ConversationQueryHandlerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.Options;

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
        ConversationQueryHandler handler = CreateHandler(access, store);

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
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// Nonexistent projection returns the same hidden shape as an unauthorized caller.
    /// </summary>
    [Fact]
    public async Task DetailNonexistentConversationShouldReturnHiddenSameAsUnauthorized()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new() { Models = null };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Forbidden);
        result.Details.ShouldBeNull();
        result.SafeNextAction.ShouldBe("The requested conversation is not available.");
    }

    /// <summary>
    /// Authorized detail reads hydrate stable references after projection data is accepted.
    /// </summary>
    [Fact]
    public async Task DetailShouldHydrateAfterAuthorizedProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new() { Models = ProjectedModels(Tenant, Conversation) };
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [Actor] = new ReferenceHydrationResult<PartyId>(Actor, ReferenceHydrationStatus.Current, "Actor", "actor-token", "Available"),
                [Participant] = new ReferenceHydrationResult<PartyId>(Participant, ReferenceHydrationStatus.Current, "Participant", "participant-token", "Available"),
            },
            ProjectResults =
            {
                [Project] = new ReferenceHydrationResult<ProjectId>(Project, ReferenceHydrationStatus.Unavailable),
            },
            FolderResults =
            {
                [Folder] = new ReferenceHydrationResult<FolderId>(Folder, ReferenceHydrationStatus.Redacted),
            },
        };
        ConversationQueryHandler handler = CreateHandler(access, store, hydration: new ConversationReadHydrationService(directory));

        ConversationDetailResult result = await handler.GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        result.Details.ShouldNotBeNull();
        result.Details.PartyHydration.Count.ShouldBe(2);
        result.Details.PartyHydration.Single(x => x.PartyId == Participant).SafeLabel.ShouldBe("Participant");
        result.Details.ProjectHydration.ShouldNotBeNull();
        result.Details.ProjectHydration.HydrationState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Details.FolderHydration.ShouldNotBeNull();
        result.Details.FolderHydration.HydrationState.ShouldBe(ProjectionTrustState.Redacted);
        directory.PartyBatchCalls.ShouldBe(1);
        directory.LastContext.ShouldNotBeNull();
        directory.LastContext.TenantId.ShouldBe(Tenant);
        directory.LastContext.CallerPrincipalId.ShouldBe("caller-001");
        directory.LastContext.CorrelationId.ShouldBe("correlation-001");

        static GetConversationQuery GetQuery()
            => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation);
    }

    /// <summary>
    /// The audit-record handler entry point uses the governed read boundary and returns citeable evidence.
    /// </summary>
    [Fact]
    public async Task AuditRecordShouldReadThroughGovernedQueryEntryPoint()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Models = ProjectedModelsWithAuditRecord(Tenant, Conversation),
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationAuditRecordResult result = await handler.GetAuditRecordAsync(
            new GetConversationAuditRecordQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "audit-evidence-001",
                AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.ActionClass.ShouldBe(AuditRecordActionClassification.Allowed);
        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Details.ShouldNotBeNull();
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
        result.Details.ActorPartyId.ShouldBe(Actor);
        result.Details.PolicyTreatment.ExportEligible.ShouldBeFalse();
        access.Calls.ShouldBe(1);
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// The privileged-action review entry point delegates to the governed review boundary.
    /// </summary>
    [Fact]
    public async Task PrivilegedJustificationShouldReadThroughGovernedQueryEntryPoint()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "caller-001"));
        FakePrivilegedReviewSource source = new()
        {
            Details = PrivilegedDetails(),
        };
        ConversationPrivilegedJustificationReviewService reviewService = new(access, source);
        ConversationQueryHandler handler = CreateHandler(
            access,
            new FakeProjectionReadStore(),
            privilegedReview: reviewService);

        PrivilegedOperationalJustificationResult result = await handler.GetPrivilegedOperationalJustificationAsync(
            new GetPrivilegedOperationalJustificationQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                "audit-evidence-privileged-001"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Current);
        result.Details.ShouldNotBeNull();
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-privileged-001");
        result.Details.OperationClass.ShouldBe(PrivilegedOperationalActionClass.Read);
        access.Calls.ShouldBe(1);
        source.Reads.ShouldBe(1);
    }

    /// <summary>
    /// Unauthorized, nonexistent, cross-tenant, and missing-projection details all return the same external shape.
    /// </summary>
    [Fact]
    public async Task DetailDenialPathsShouldShareSameShape()
    {
        ConversationDetailResult unauthorized = await CreateHandler(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Denied(
                ConversationTenantAccessRequirement.Read,
                Tenant,
                "caller-001",
                ConversationTenantAccessDenialReason.MissingMember)),
            new FakeProjectionReadStore())
            .GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        ConversationDetailResult nonexistent = await CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = null })
            .GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        ConversationDetailResult crossTenant = await CreateHandler(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = ProjectedModels(OtherTenant, Conversation) })
            .GetAsync(GetQuery(), TestContext.Current.CancellationToken);

        unauthorized.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        nonexistent.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        crossTenant.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);

        unauthorized.ReasonCode.ShouldBe(nonexistent.ReasonCode);
        unauthorized.ReasonCode.ShouldBe(crossTenant.ReasonCode);
        unauthorized.SafeNextAction.ShouldBe(nonexistent.SafeNextAction);
        unauthorized.SafeNextAction.ShouldBe(crossTenant.SafeNextAction);
        unauthorized.Details.ShouldBeNull();
        nonexistent.Details.ShouldBeNull();
        crossTenant.Details.ShouldBeNull();

        static GetConversationQuery GetQuery()
            => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation);
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
        ConversationQueryHandler handler = CreateHandler(access, store);

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
        ConversationQueryHandler handler = CreateHandler(access, store);

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
    /// Mixed-generation rows from the projection store surface as Rebuilding instead of leaking inconsistent rows.
    /// </summary>
    [Fact]
    public async Task ListShouldRejectMixedGenerationCandidates()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-a"), Business, Project, Folder, Participant, cursor: "pos:1"),
                Summary(Tenant, new ConversationId("conv-b"), Business, Project, Folder, Participant, cursor: "pos:2"),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MixedGeneration);
        result.Conversations.ShouldBeEmpty();
    }

    /// <summary>
    /// A page combining Current and Stale rows reports the worst-case freshness, not the first row.
    /// </summary>
    [Fact]
    public async Task ListFreshnessShouldAggregateWorstCaseAcrossPage()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-current"), Business, Project, Folder, Participant),
                Summary(
                    Tenant,
                    new ConversationId("conv-stale"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Conversations.Count.ShouldBe(2);
    }

    /// <summary>
    /// A non-current accessible match beyond the returned page still downgrades list freshness.
    /// </summary>
    [Fact]
    public async Task ListFreshnessShouldAggregateWorstCaseAcrossAllAccessibleMatchesBeforePaging()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-current"), Business, Project, Folder, Participant),
                Summary(
                    Tenant,
                    new ConversationId("conv-stale"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            ],
        };
        ConversationQueryHandler handler = CreateHandler(access, store);

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(1)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Conversations.Count.ShouldBe(1);
        result.Page.ContinuationCursor.ShouldNotBeNull();
    }

    /// <summary>
    /// A non-current accessible match beyond the continuation lookahead still downgrades list freshness.
    /// </summary>
    [Fact]
    public async Task ListFreshnessShouldIncludeAccessibleMatchesBeyondLookahead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-current-1"), Business, Project, Folder, Participant, lastAppliedAt: Now.AddMinutes(3)),
                Summary(Tenant, new ConversationId("conv-current-2"), Business, Project, Folder, Participant, lastAppliedAt: Now.AddMinutes(2)),
                Summary(
                    Tenant,
                    new ConversationId("conv-stale"),
                    Business,
                    Project,
                    Folder,
                    Participant,
                    lastAppliedAt: Now.AddMinutes(1),
                    freshnessState: ProjectionTrustState.Stale,
                    reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            ],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(1)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Conversations.Single().ConversationId.Value.ShouldBe("conv-current-1");
        result.Page.ContinuationCursor.ShouldNotBeNull();
    }

    /// <summary>
    /// Each list filter dimension narrows the result to only its matching row, in isolation.
    /// </summary>
    [Theory]
    [InlineData("business")]
    [InlineData("project")]
    [InlineData("folder")]
    [InlineData("lifecycle")]
    [InlineData("participant")]
    [InlineData("redaction")]
    [InlineData("freshness")]
    [InlineData("audit")]
    [InlineData("verification")]
    public async Task ListShouldFilterByEachDimensionExactly(string dimension)
    {
        FakeTenantAccessService access = AllowedAccess();
        BusinessReference otherBusiness = new("crm", "case-999");
        ProjectId otherProject = new("project-999");
        FolderId otherFolder = new("folder-999");
        PartyId otherParticipant = new("party-other");
        ConversationSearchTrustPreviewV1 matchingTrust = TrustPreview(
            ProjectionTrustState.Redacted,
            ProjectionTrustState.Stale,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified);
        ConversationSearchTrustPreviewV1 nonMatchingTrust = TrustPreview(
            ProjectionTrustState.Current,
            ProjectionTrustState.Current,
            ConversationAuditReadinessState.Incomplete,
            ConversationVerificationState.Unverified);

        (IReadOnlyList<ConversationSummaryProjectionV1> rows, ConversationListFilterV1 filter) = dimension switch
        {
            "business" => (
                (IReadOnlyList<ConversationSummaryProjectionV1>)
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), otherBusiness, Project, Folder, Participant),
                ],
                new ConversationListFilterV1(BusinessReference: Business)),
            "project" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, otherProject, Folder, Participant),
                ],
                new ConversationListFilterV1(ProjectId: Project)),
            "folder" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, otherFolder, Participant),
                ],
                new ConversationListFilterV1(FolderId: Folder)),
            "lifecycle" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, lifecycle: "Closed"),
                ],
                new ConversationListFilterV1(LifecycleState: "Open")),
            "participant" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, otherParticipant),
                ],
                new ConversationListFilterV1(ParticipantPartyId: Participant)),
            "redaction" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant, trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(RedactionState: ProjectionTrustState.Redacted)),
            "freshness" => (
                [
                    Summary(
                        Tenant,
                        new ConversationId("match"),
                        Business,
                        Project,
                        Folder,
                        Participant,
                        freshnessState: ProjectionTrustState.Stale,
                        reason: ProjectionFreshnessReasonCode.StaleThresholdExceeded,
                        trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(FreshnessState: ProjectionTrustState.Stale)),
            "audit" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant, trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(AuditReadiness: ConversationAuditReadinessState.Ready)),
            "verification" => (
                [
                    Summary(Tenant, new ConversationId("match"), Business, Project, Folder, Participant, trustPreview: matchingTrust),
                    Summary(Tenant, new ConversationId("miss"), Business, Project, Folder, Participant, trustPreview: nonMatchingTrust),
                ],
                new ConversationListFilterV1(VerificationState: ConversationVerificationState.Verified)),
            _ => throw new ArgumentOutOfRangeException(nameof(dimension)),
        };

        FakeProjectionReadStore store = new() { Summaries = rows };
        ConversationQueryHandler handler = CreateHandler(access, store);
        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", filter),
            TestContext.Current.CancellationToken);

        result.Conversations.Count.ShouldBe(1);
        result.Conversations[0].ConversationId.Value.ShouldBe("match");
        result.Conversations[0].SearchTrustPreview.MatchSource.ShouldNotBe(ConversationSearchMatchSource.Unknown);
    }

    /// <summary>
    /// ProjectedAt range and RecentActivityAfter filter out rows outside the window.
    /// </summary>
    [Fact]
    public async Task ListShouldFilterByProjectedAtRangeAndRecentActivity()
    {
        FakeTenantAccessService access = AllowedAccess();
        DateTimeOffset early = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset middle = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset late = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-early"), Business, Project, Folder, Participant, lastAppliedAt: early),
                Summary(Tenant, new ConversationId("conv-middle"), Business, Project, Folder, Participant, lastAppliedAt: middle),
                Summary(Tenant, new ConversationId("conv-late"), Business, Project, Folder, Participant, lastAppliedAt: late),
            ],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(
                    ProjectedAtFrom: middle.AddDays(-1),
                    ProjectedAtTo: late.AddDays(-1),
                    RecentActivityAfter: early)),
            TestContext.Current.CancellationToken);

        result.Conversations.Count.ShouldBe(1);
        result.Conversations[0].ConversationId.Value.ShouldBe("conv-middle");
    }

    /// <summary>
    /// Pagination boundary: the page returns at most PageSize rows even when more accessible rows exist.
    /// </summary>
    [Fact]
    public async Task ListPaginationShouldNotLeakBeyondPageSize()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-a"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-b"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conv-c"), Business, Project, Folder, Participant),
            ],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(2)),
            TestContext.Current.CancellationToken);

        result.Conversations.Count.ShouldBe(2);
        result.Page.ReturnedCount.ShouldBe(2);
        result.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// List hydration runs only after stable ordering and paging select the visible page.
    /// </summary>
    [Fact]
    public async Task ListHydrationShouldOnlyUseReturnedPageReferences()
    {
        FakeTenantAccessService access = AllowedAccess();
        PartyId first = new("party-first");
        PartyId second = new("party-second");
        PartyId third = new("party-third");
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conv-first"), Business, Project, Folder, first, lastAppliedAt: Now.AddMinutes(3)),
                Summary(Tenant, new ConversationId("conv-second"), Business, Project, Folder, second, lastAppliedAt: Now.AddMinutes(2)),
                Summary(Tenant, new ConversationId("conv-third"), Business, Project, Folder, third, lastAppliedAt: Now.AddMinutes(1)),
            ],
        };
        FakeReferenceHydrationDirectory directory = new()
        {
            PartyResults =
            {
                [first] = new ReferenceHydrationResult<PartyId>(first, ReferenceHydrationStatus.Current, "Z label", "first-token", "Available"),
                [second] = new ReferenceHydrationResult<PartyId>(second, ReferenceHydrationStatus.Current, "A label", "second-token", "Available"),
                [third] = new ReferenceHydrationResult<PartyId>(third, ReferenceHydrationStatus.Current, "Hidden page label", "third-token", "Available"),
            },
        };
        ConversationQueryHandler handler = CreateHandler(access, store, hydration: new ConversationReadHydrationService(directory));

        ConversationListResult result = await handler.ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(2)),
            TestContext.Current.CancellationToken);

        result.Conversations.Select(summary => summary.ConversationId.Value).ShouldBe(["conv-first", "conv-second"]);
        directory.LastPartyIds.ShouldBe([first, second], ignoreOrder: true);
        directory.LastPartyIds.ShouldNotContain(third);
        result.Conversations[0].PartyHydration.Single().SafeLabel.ShouldBe("Z label");
        result.Conversations[1].PartyHydration.Single().SafeLabel.ShouldBe("A label");
        result.Conversations[0].SearchTrustPreview.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Current);
    }

    /// <summary>
    /// Empty list results use safe copy and expose no facet, autocomplete, or recent-search metadata.
    /// </summary>
    [Fact]
    public async Task NoAccessibleMatchesShouldUseSafeEmptyShape()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, new BusinessReference("crm", "case-999"), Project, Folder, Participant)],
        };

        ConversationListResult result = await CreateHandler(access, store).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                new ConversationListFilterV1(BusinessReference: Business)),
            TestContext.Current.CancellationToken);

        result.Conversations.ShouldBeEmpty();
        result.Page.ReturnedCount.ShouldBe(0);
        result.SafeNextAction.ShouldBe("No accessible matches.");

        string json = JsonSerializer.Serialize(result);
        json.ShouldNotContain("facet", Case.Insensitive);
        json.ShouldNotContain("autocomplete", Case.Insensitive);
        json.ShouldNotContain("recentSearch", Case.Insensitive);
        json.ShouldNotContain("total", Case.Insensitive);
    }

    /// <summary>
    /// Malformed cursors fail closed before authorization-sensitive reads.
    /// </summary>
    [Fact]
    public async Task MalformedCursorShouldFailClosedWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new();
        ConversationQueryHandler handler = CreateHandler(access, store);

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
        ConversationQueryCursor cursorService = CreateCursor();
        ConversationQueryHandler handler = CreateHandler(access, store, cursor: cursorService);
        string cursor = cursorService.EncodeForTests(
            Tenant,
            "different-caller",
            ConversationListFilterV1.Empty,
            offset: 1,
            projectionGenerationToken: "pos:1:1",
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
    }

    /// <summary>
    /// Tampered cursor signatures fail closed; the verifier never reads projection storage.
    /// </summary>
    [Fact]
    public async Task TamperedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryCursor cursorService = CreateCursor();
        string original = cursorService.EncodeForTests(
            Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        // Flip a byte of the base64 payload to break the HMAC.
        byte[] bytes = Convert.FromBase64String(original);
        bytes[^1] ^= 0xFF;
        string tampered = Convert.ToBase64String(bytes);

        ConversationListResult result = await CreateHandler(access, store, cursor: cursorService).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, tampered)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Cursors signed under a different deployment key are rejected.
    /// </summary>
    [Fact]
    public async Task CursorSignedWithDifferentKeyShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryCursor otherKeyCursor = CreateCursor(seed: 99, keyId: "other-key");
        string foreign = otherKeyCursor.EncodeForTests(
            Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: CreateCursor()).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, foreign)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        store.ListReads.ShouldBe(0);
    }

    /// <summary>
    /// Cursors older than the configured MaxAge fail closed.
    /// </summary>
    [Fact]
    public async Task ExpiredCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryCursor cursorService = CreateCursor();
        FakeTimeProvider time = new(Now.AddHours(2));
        string aged = cursorService.EncodeForTests(
            Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: cursorService, time: time).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, aged)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
    }

    /// <summary>
    /// Future-dated cursors (clock skew or forged) fail closed via the age lower bound.
    /// </summary>
    [Fact]
    public async Task FutureDatedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryCursor cursorService = CreateCursor();
        string futureCursor = cursorService.EncodeForTests(
            Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now.AddHours(1));

        ConversationListResult result = await CreateHandler(access, store, cursor: cursorService, time: new FakeTimeProvider(Now)).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, futureCursor)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
    }

    /// <summary>
    /// Cursors issued against a different projection generation token fail closed.
    /// </summary>
    [Fact]
    public async Task GenerationMismatchedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryCursor cursorService = CreateCursor();
        string staleGen = cursorService.EncodeForTests(
            Tenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:OLD:0", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: cursorService).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, staleGen)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
    }

    /// <summary>
    /// Cursors issued for a different tenant fail closed.
    /// </summary>
    [Fact]
    public async Task TenantMismatchedCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryCursor cursorService = CreateCursor();
        string foreign = cursorService.EncodeForTests(
            OtherTenant, "caller-001", ConversationListFilterV1.Empty, 0, "pos:1:1", Now);

        ConversationListResult result = await CreateHandler(access, store, cursor: cursorService).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, foreign)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
    }

    /// <summary>
    /// Cursors with offsets above the configured MaxOffset fail closed.
    /// </summary>
    [Fact]
    public async Task ExcessiveOffsetCursorShouldFailClosed()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, Conversation, Business, Project, Folder, Participant)],
        };
        ConversationQueryCursorOptions options = OptionsFor(maxOffset: 10);
        ConversationQueryCursor cursorService = new(Options.Create(options));
        // Encode a cursor at the boundary and then manually craft a payload with offset > MaxOffset.
        // Easiest path: bypass Encode and hand-craft. We re-use cursorService with offset 5 which is fine.
        string oversize = ForgeCursorWithOffset(cursorService, options, offset: 999_999);

        ConversationListResult result = await CreateHandler(access, store, cursor: cursorService).ListAsync(
            new ListConversationsQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Page: new ConversationPageRequest(10, oversize)),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
    }

    /// <summary>
    /// Public query contracts do not expose any field that would let a caller supply a provider session id.
    /// </summary>
    [Fact]
    public void GetAndListContractsShouldNotExposeProviderSessionField()
    {
        Type detailQuery = typeof(GetConversationQuery);
        Type listQuery = typeof(ListConversationsQuery);
        Type filter = typeof(ConversationListFilterV1);

        foreach (Type type in new[] { detailQuery, listQuery, filter })
        {
            foreach (System.Reflection.PropertyInfo property in type.GetProperties())
            {
                property.Name.ShouldNotContain("Session", Case.Insensitive);
                property.Name.ShouldNotContain("Provider", Case.Insensitive);
            }
        }
    }

    private static FakeTenantAccessService AllowedAccess()
        => new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001"));

    private static ConversationQueryHandler CreateHandler(
        FakeTenantAccessService access,
        FakeProjectionReadStore store,
        ConversationQueryCursor? cursor = null,
        TimeProvider? time = null,
        ConversationReadHydrationService? hydration = null,
        ConversationPrivilegedJustificationReviewService? privilegedReview = null)
    {
        ConversationQueryCursor cursorInstance = cursor ?? CreateCursor();
        ConversationProjectionReadService readService = new(access, store);
        return new ConversationQueryHandler(
            access,
            store,
            readService,
            cursorInstance,
            time ?? new FakeTimeProvider(Now),
            hydration,
            privilegedJustificationReviewService: privilegedReview);
    }

    private static ConversationQueryCursor CreateCursor(int seed = 42, string keyId = "test-key-1")
        => new(Options.Create(OptionsFor(seed, keyId)));

    private static ConversationQueryCursorOptions OptionsFor(int seed = 42, string keyId = "test-key-1", int maxOffset = 100_000)
    {
        byte[] key = new byte[32];
        Random rng = new(seed);
        rng.NextBytes(key);
        return new ConversationQueryCursorOptions
        {
            SigningKey = key,
            KeyId = keyId,
            MaxOffset = maxOffset,
        };
    }

    private static string ForgeCursorWithOffset(
        ConversationQueryCursor cursorService,
        ConversationQueryCursorOptions options,
        int offset)
    {
        ConversationQueryCursor.CursorPayload payload = new(
            1,
            options.KeyId,
            Tenant.Value,
            "caller-001",
            ConversationQueryCursor.Fingerprint(ConversationListFilterV1.Empty),
            ConversationQueryCursor.SortVersion,
            "pos:1:1",
            offset,
            Now.UtcDateTime);
        string payloadJson = JsonSerializer.Serialize(payload);
        using HMACSHA256 hmac = new(options.SigningKey);
        string signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson)));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payloadJson}.{signature}"));
    }

    private static ConversationProjectedReadModels ProjectedModels(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            Detail(tenantId, conversationId));

    private static ConversationProjectedReadModels ProjectedModelsWithAuditRecord(TenantId tenantId, ConversationId conversationId)
        => new(
            Summary(tenantId, conversationId, Business, Project, Folder, Participant),
            DetailWithAuditRecord(tenantId, conversationId));

    private static ConversationSummaryProjectionV1 Summary(
        TenantId tenantId,
        ConversationId conversationId,
        BusinessReference? business,
        ProjectId? project,
        FolderId? folder,
        PartyId participant,
        string lifecycle = "Open",
        string cursor = "pos:0000000001",
        DateTimeOffset? lastAppliedAt = null,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reason = null,
        ConversationSearchTrustPreviewV1? trustPreview = null)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness(cursor, lastAppliedAt, freshnessState, reason),
            lifecycle,
            "Case 123",
            business,
            project,
            folder,
            [participant],
            MessageCount: 1,
            FileReferenceCount: 0,
            SearchTrustPreview: trustPreview);

    private static ConversationSearchTrustPreviewV1 TrustPreview(
        ProjectionTrustState redactionState,
        ProjectionTrustState freshnessState,
        ConversationAuditReadinessState auditReadiness,
        ConversationVerificationState verificationState)
        => new(
            freshnessState,
            freshnessState == ProjectionTrustState.Current
                ? ProjectionFreshnessReasonCode.Current
                : ProjectionFreshnessReasonCode.StaleThresholdExceeded,
            redactionState,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            auditReadiness,
            verificationState,
            ConversationSearchMatchSource.TenantScope,
            "Visible through authorized tenant scope.");

    private static ConversationDetailProjectionV1 Detail(TenantId tenantId, ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness("pos:0000000001"),
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello from the adopter.", Now)],
            []);

    private static ConversationDetailProjectionV1 DetailWithAuditRecord(TenantId tenantId, ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            Freshness("pos:0000000001"),
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), Actor, "Hello from the adopter.", Now)],
            [],
            ActiveRetentionPolicy: new ConversationRetentionPolicyProjectionV1(
                "retention-policy-standard",
                "customer-request",
                Actor,
                Now,
                new GovernanceAuditEvidenceReference(
                    new AuditEvidenceHandle("audit-evidence-001"),
                    "retention-policy-standard",
                    Now)));

    private static PrivilegedOperationalJustificationDetailsV1 PrivilegedDetails()
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "customer-request",
            Now,
            GovernanceOutcome.Succeeded,
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-privileged-001"),
                "privileged-review-policy",
                Now),
            ProjectionTrustState.Current,
            Freshness("pos:0000000001"),
            "Use the returned audit handle as governed evidence.",
            "correlation-001");

    private static ProjectionFreshnessV1 Freshness(
        string cursor = "pos:0000000001",
        DateTimeOffset? lastAppliedAt = null,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reason = null)
    {
        ProjectionTrustState state = freshnessState ?? ProjectionTrustState.Current;
        bool isStale = state == ProjectionTrustState.Stale;
        return new(
            SchemaVersion.Current,
            cursor,
            1,
            lastAppliedAt ?? Now,
            (lastAppliedAt ?? Now).AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: isStale,
            state,
            reason ?? ProjectionFreshnessReasonCode.Current);
    }

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

    private sealed class FakePrivilegedReviewSource : IPrivilegedOperationalJustificationReviewSource
    {
        public PrivilegedOperationalJustificationDetailsV1? Details { get; set; }

        public int Reads { get; private set; }

        public ValueTask<PrivilegedOperationalJustificationDetailsV1?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            AuditEvidenceHandle auditEvidenceHandle,
            CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(Details);
        }
    }

    private sealed class FakeReferenceHydrationDirectory : IConversationReferenceHydrationDirectory
    {
        public Dictionary<PartyId, ReferenceHydrationResult<PartyId>> PartyResults { get; } = [];

        public Dictionary<ProjectId, ReferenceHydrationResult<ProjectId>> ProjectResults { get; } = [];

        public Dictionary<FolderId, ReferenceHydrationResult<FolderId>> FolderResults { get; } = [];

        public Dictionary<FileId, ReferenceHydrationResult<FileId>> FileResults { get; } = [];

        public int PartyBatchCalls { get; private set; }

        public ConversationHydrationContext? LastContext { get; private set; }

        public IReadOnlyList<PartyId> LastPartyIds { get; private set; } = [];

        public ValueTask<IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>> HydratePartiesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<PartyId> partyIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PartyBatchCalls++;
            LastContext = context;
            LastPartyIds = partyIds.ToList();
            return ValueTask.FromResult((IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>)PartyResults);
        }

        public ValueTask<IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>> HydrateProjectsAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<ProjectId> projectIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult((IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>)ProjectResults);
        }

        public ValueTask<IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>> HydrateFoldersAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FolderId> folderIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult((IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>)FolderResults);
        }

        public ValueTask<IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>> HydrateFilesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FileId> fileIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult((IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>)FileResults);
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
