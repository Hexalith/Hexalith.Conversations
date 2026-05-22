// <copyright file="ConversationAuditRecordAccessServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
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
/// Verifies governed audit-record read/export access boundaries.
/// </summary>
public sealed class ConversationAuditRecordAccessServiceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly MessageId Message = new("message-001");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AllowedReadShouldReturnCiteableAuditEvidence()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new() { Models = Models(Tenant, "retention-policy-standard") };
        ConversationAuditRecordAccessService service = new(access, store);

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.ActionClass.ShouldBe(AuditRecordActionClassification.Allowed);
        result.Details.ShouldNotBeNull();
        result.Details.TenantId.ShouldBe(Tenant);
        result.Details.ActorPartyId.ShouldBe(Actor);
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
        result.Details.PolicyTreatment.ExportEligible.ShouldBeFalse();
        store.DetailReads.ShouldBe(1);
    }

    [Fact]
    public async Task DeniedReadShouldNotParseHandleOrReadProjection()
    {
        FakeTenantAccessService access = DeniedAccess();
        FakeProjectionReadStore store = new() { Models = Models(Tenant, "retention-policy-standard") };
        ConversationAuditRecordAccessService service = new(access, store);

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("storage://raw-audit-location", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(0);
        access.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AllowedExportShouldReturnOnlySafeInMemoryResult()
    {
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = Models(Tenant, "retention-policy-standard") });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Exported),
            TestContext.Current.CancellationToken);

        result.ActionClass.ShouldBe(AuditRecordActionClassification.Exported);
        result.Details.ShouldNotBeNull();
        result.Details.PolicyTreatment.ExportEligible.ShouldBeTrue();
        result.SafeNextAction.ShouldContain("in-memory", Case.Insensitive);
        result.ToString().ShouldNotContain("storage", Case.Insensitive);
        result.ToString().ShouldNotContain("blob", Case.Insensitive);
    }

    [Fact]
    public async Task DeniedExportShouldShareHiddenShapeWithDeniedRead()
    {
        ConversationAuditRecordAccessService service = new(
            DeniedAccess(),
            new FakeProjectionReadStore { Models = Models(Tenant, "retention-policy-standard") });

        ConversationAuditRecordResult deniedRead = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);
        ConversationAuditRecordResult deniedExport = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Exported),
            TestContext.Current.CancellationToken);

        deniedExport.FreshnessState.ShouldBe(deniedRead.FreshnessState);
        deniedExport.ReasonCode.ShouldBe(deniedRead.ReasonCode);
        deniedExport.SafeNextAction.ShouldBe(deniedRead.SafeNextAction);
        deniedExport.Details.ShouldBeNull();
    }

    [Fact]
    public async Task PolicyBlockedExportShouldNotCreateUnmanagedExportSurface()
    {
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = Models(Tenant, "audit-policy-separate-log-required") });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Exported),
            TestContext.Current.CancellationToken);

        result.ActionClass.ShouldBe(AuditRecordActionClassification.PolicyBlocked);
        result.Details.ShouldBeNull();
        result.SafeNextAction.ShouldContain("blocked by policy", Case.Insensitive);
    }

    [Fact]
    public async Task ExpiredAuditRecordShouldRemainReviewableWithWithheldTreatment()
    {
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = Models(Tenant, "audit-policy-expired") });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.ActionClass.ShouldBe(AuditRecordActionClassification.Redacted);
        result.FreshnessState.ShouldBe(ProjectionTrustState.Redacted);
        result.Details.ShouldNotBeNull();
        result.Details.ActorPartyId.ShouldBe(Actor);
        result.Details.Timestamp.ShouldBe(Now);
        result.Details.PolicyTreatment.RetentionState.ShouldBe(ProjectionTrustState.Redacted);
        result.Details.PolicyTreatment.SafeNextAction.ShouldContain("withheld", Case.Insensitive);
    }

    [Theory]
    [InlineData("stale")]
    [InlineData("rebuilding")]
    public async Task StaleOrRebuildingProjectionShouldNotReturnAuthoritativeAuditDetails(string state)
    {
        ProjectionTrustState trustState = state == "stale" ? ProjectionTrustState.Stale : ProjectionTrustState.Rebuilding;
        ProjectionFreshnessReasonCode reason = state == "stale"
            ? ProjectionFreshnessReasonCode.StaleThresholdExceeded
            : ProjectionFreshnessReasonCode.Rebuilding;
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore
            {
                Models = Models(Tenant, "retention-policy-standard", trustState, reason),
            });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(trustState);
        result.ReasonCode.ShouldBe(reason);
        result.Details.ShouldBeNull();
    }

    [Fact]
    public async Task MalformedAuditHandleShouldHideWithoutProjectionReadAfterAuthorization()
    {
        FakeProjectionReadStore store = new() { Models = Models(Tenant, "retention-policy-standard") };
        ConversationAuditRecordAccessService service = new(AllowedAccess(), store);

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("bad handle", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task CrossTenantProjectionShouldHideProtectedRecordExistence()
    {
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = Models(OtherTenant, "retention-policy-standard") });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
    }

    [Fact]
    public async Task AuditSourceUnavailableShouldReturnContentSafeUnavailable()
    {
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore { ThrowOnRead = true });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Allowed),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Unavailable);
        result.Details.ShouldBeNull();
    }

    [Fact]
    public async Task RebuiltAuditViewShouldPreserveCiteableMetadataAndMessageRedaction()
    {
        ConversationProjectedReadModels original = Models(Tenant, "redaction-policy-standard", includeRedaction: true);
        ConversationProjectedReadModels rebuilt = Models(Tenant, "redaction-policy-standard", includeRedaction: true);
        ConversationAuditRecordResult first = await new ConversationAuditRecordAccessService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = original })
            .GetAsync(Query("audit-evidence-redaction-001", AuditRecordActionClassification.Allowed), TestContext.Current.CancellationToken);
        ConversationAuditRecordResult second = await new ConversationAuditRecordAccessService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = rebuilt })
            .GetAsync(Query("audit-evidence-redaction-001", AuditRecordActionClassification.Allowed), TestContext.Current.CancellationToken);

        first.Details.ShouldNotBeNull();
        second.Details.ShouldNotBeNull();
        first.Details.AuditEvidence.ShouldBe(second.Details.AuditEvidence);
        first.Details.GovernedTarget.ToTargetKey().ShouldBe(second.Details.GovernedTarget.ToTargetKey());
        original.Detail.Messages.Single().Text.ShouldBe("[redacted]");
        rebuilt.Detail.Messages.Single().Text.ShouldBe("[redacted]");
    }

    [Fact]
    public async Task AuditRecordGovernanceMutationAttemptShouldBePolicyBlocked()
    {
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = Models(Tenant, "retention-policy-standard") });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.SeparatelyLogged),
            TestContext.Current.CancellationToken);

        result.ActionClass.ShouldBe(AuditRecordActionClassification.PolicyBlocked);
        result.Details.ShouldBeNull();
    }

    [Theory]
    [InlineData("Denied")]
    [InlineData("Redacted")]
    public async Task OutcomeOnlyRequestedActionsShouldBePolicyBlocked(string requestedAction)
    {
        ConversationAuditRecordAccessService service = new(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = Models(Tenant, "retention-policy-standard") });

        ConversationAuditRecordResult result = await service.GetAsync(
            Query("audit-evidence-001", AuditRecordActionClassification.Parse(requestedAction)),
            TestContext.Current.CancellationToken);

        result.ActionClass.ShouldBe(AuditRecordActionClassification.PolicyBlocked);
        result.Details.ShouldBeNull();
    }

    private static GetConversationAuditRecordQuery Query(string handle, AuditRecordActionClassification action)
        => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation, handle, action);

    private static FakeTenantAccessService AllowedAccess()
        => new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001"));

    private static FakeTenantAccessService DeniedAccess()
        => new(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001",
            ConversationTenantAccessDenialReason.MissingMember));

    private static ConversationProjectedReadModels Models(
        TenantId tenantId,
        string policyReference,
        ProjectionTrustState? state = null,
        ProjectionFreshnessReasonCode? reason = null,
        bool includeRedaction = false)
    {
        ProjectionFreshnessV1 freshness = Freshness(state, reason);
        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            tenantId,
            Conversation,
            freshness,
            "Open",
            "Case 123",
            new BusinessReference("crm", "case-123"),
            Project,
            Folder,
            [Participant],
            MessageCount: 1,
            FileReferenceCount: 0);
        GovernanceAuditEvidenceReference retentionEvidence = new(
            new AuditEvidenceHandle("audit-evidence-001"),
            policyReference,
            Now);
        List<ConversationRedactionProjectionV1> redactions = includeRedaction
            ?
            [
                new ConversationRedactionProjectionV1(
                    new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
                    RedactionCategory.ContentSuppression,
                    policyReference,
                    "customer-request",
                    Actor,
                    Now.AddMinutes(1),
                    new GovernanceAuditEvidenceReference(
                        new AuditEvidenceHandle("audit-evidence-redaction-001"),
                        policyReference,
                        Now.AddMinutes(1)),
                    ProjectionTrustState.Redacted),
            ]
            : [];
        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            tenantId,
            Conversation,
            freshness,
            "Open",
            "Case 123",
            new BusinessReference("crm", "case-123"),
            Project,
            Folder,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(Message, Actor, includeRedaction ? "[redacted]" : "Hello", Now)],
            [],
            ActiveRetentionPolicy: new ConversationRetentionPolicyProjectionV1(
                policyReference,
                "customer-request",
                Actor,
                Now,
                retentionEvidence),
            Redactions: redactions);

        return new(summary, detail);
    }

    private static ProjectionFreshnessV1 Freshness(
        ProjectionTrustState? state = null,
        ProjectionFreshnessReasonCode? reason = null)
    {
        ProjectionTrustState trustState = state ?? ProjectionTrustState.Current;
        return new(
            SchemaVersion.Current,
            "pos:0000000001",
            1,
            Now,
            Now.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: trustState == ProjectionTrustState.Stale,
            trustState,
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

        public bool ThrowOnRead { get; set; }

        public int DetailReads { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            DetailReads++;
            if (ThrowOnRead)
            {
                throw new IOException("projection unavailable");
            }

            return ValueTask.FromResult(Models);
        }

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult((IReadOnlyList<ConversationSummaryProjectionV1>)[]);
    }
}
