// <copyright file="ConversationPrivilegedJustificationReviewServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Verifies privileged operational justification review behavior.
/// </summary>
public sealed class ConversationPrivilegedJustificationReviewServiceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AuthorizedReviewerShouldInspectCoherentPrivilegedActionRecord()
    {
        ConversationPrivilegedJustificationReviewService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Governance,
                Tenant,
                "caller-001")),
            new FakeReviewSource { Details = Details(ProjectionTrustState.Current) });

        PrivilegedOperationalJustificationResult result = await service.GetAsync(
            Query("audit-evidence-privileged-001"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Current);
        result.Details.ShouldNotBeNull();
        result.Details.ActorPartyId.ShouldBe(Actor);
        result.Details.TenantId.ShouldBe(Tenant);
        result.Details.OperationClass.ShouldBe(PrivilegedOperationalActionClass.Read);
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-privileged-001");
    }

    [Fact]
    public async Task UnauthorizedReviewerShouldNotResolvePrivilegedEvidence()
    {
        FakeReviewSource source = new() { Details = Details(ProjectionTrustState.Current) };
        ConversationPrivilegedJustificationReviewService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Denied(
                ConversationTenantAccessRequirement.Governance,
                Tenant,
                "caller-001",
                ConversationTenantAccessDenialReason.MissingMember)),
            source);

        PrivilegedOperationalJustificationResult result = await service.GetAsync(
            Query("storage://raw-audit-location"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Details.ShouldBeNull();
        source.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task MalformedHandleShouldHideAfterAuthorizationWithoutReadingSource()
    {
        FakeReviewSource source = new() { Details = Details(ProjectionTrustState.Current) };
        ConversationPrivilegedJustificationReviewService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Governance,
                Tenant,
                "caller-001")),
            source);

        PrivilegedOperationalJustificationResult result = await service.GetAsync(
            Query("bad handle"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Forbidden);
        source.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task UnavailableReviewSourceShouldReturnExplicitUnavailableState()
    {
        ConversationPrivilegedJustificationReviewService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Governance,
                Tenant,
                "caller-001")),
            new FakeReviewSource { ThrowOnRead = true });

        PrivilegedOperationalJustificationResult result = await service.GetAsync(
            Query("audit-evidence-privileged-001"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Details.ShouldBeNull();
    }

    [Fact]
    public async Task RedactedReviewFieldsShouldRemainExplicitRatherThanNullHidden()
    {
        ConversationPrivilegedJustificationReviewService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Governance,
                Tenant,
                "caller-001")),
            new FakeReviewSource { Details = Details(ProjectionTrustState.Redacted) });

        PrivilegedOperationalJustificationResult result = await service.GetAsync(
            Query("audit-evidence-privileged-001"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Redacted);
        result.Details.ShouldNotBeNull();
        result.Details.VisibilityState.ShouldBe(ProjectionTrustState.Redacted);
        result.Details.SafeNextAction.ShouldContain("withheld", Case.Insensitive);
    }

    [Fact]
    public async Task StaleReviewEvidenceShouldFailClosedWithoutReturningDetails()
    {
        ConversationPrivilegedJustificationReviewService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Governance,
                Tenant,
                "caller-001")),
            new FakeReviewSource
            {
                Details = Details(
                    ProjectionTrustState.Current,
                    ProjectionTrustState.Stale,
                    ProjectionFreshnessReasonCode.StaleThresholdExceeded),
            });

        PrivilegedOperationalJustificationResult result = await service.GetAsync(
            Query("audit-evidence-privileged-001"),
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Stale);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Details.ShouldBeNull();
    }

    private static GetPrivilegedOperationalJustificationQuery Query(string handle)
        => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation, handle);

    private static PrivilegedOperationalJustificationDetailsV1 Details(
        ProjectionTrustState visibility,
        ProjectionTrustState freshnessState = null!,
        ProjectionFreshnessReasonCode freshnessReason = null!)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            visibility == ProjectionTrustState.Redacted ? "withheld" : "customer-request",
            Now,
            GovernanceOutcome.Succeeded,
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-privileged-001"),
                "privileged-review-policy",
                Now),
            visibility,
            new ProjectionFreshnessV1(
                SchemaVersion.Current,
                "pos:0000000001",
                1,
                Now,
                Now.AddSeconds(1),
                TimeSpan.FromSeconds(1),
                freshnessState == ProjectionTrustState.Stale,
                freshnessState ?? ProjectionTrustState.Current,
                freshnessReason ?? ProjectionFreshnessReasonCode.Current),
            visibility == ProjectionTrustState.Redacted
                ? "Privileged justification rationale is withheld by policy."
                : "Use the returned audit handle as governed evidence.",
            "correlation-001");

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
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
            => ValueTask.FromResult(decision with { Requirement = requirement });
    }

    private sealed class FakeReviewSource : IPrivilegedOperationalJustificationReviewSource
    {
        public PrivilegedOperationalJustificationDetailsV1? Details { get; set; }

        public bool ThrowOnRead { get; set; }

        public int Reads { get; private set; }

        public ValueTask<PrivilegedOperationalJustificationDetailsV1?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            AuditEvidenceHandle auditEvidenceHandle,
            CancellationToken cancellationToken = default)
        {
            Reads++;
            if (ThrowOnRead)
            {
                throw new IOException("review source unavailable");
            }

            return ValueTask.FromResult(Details);
        }
    }
}
