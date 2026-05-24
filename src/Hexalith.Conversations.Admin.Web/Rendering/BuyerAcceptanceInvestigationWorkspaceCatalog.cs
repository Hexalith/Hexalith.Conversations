// <copyright file="BuyerAcceptanceInvestigationWorkspaceCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Admin.Web.Rendering;

/// <summary>
/// Adapts existing buyer-acceptance fixtures into rendered responsive workspace fixtures.
/// </summary>
public sealed class BuyerAcceptanceInvestigationWorkspaceCatalog : IInvestigationWorkspaceCatalog
{
    public const string DefaultFixtureId = "TenantA_Admin_FullTrust";

    private readonly IReadOnlyDictionary<string, InvestigationWorkspaceViewModel> _fixtures;

    public BuyerAcceptanceInvestigationWorkspaceCatalog()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        Dictionary<string, InvestigationWorkspaceViewModel> fixtures = new(StringComparer.Ordinal)
        {
            [DefaultFixtureId] = FromProjection(
                DefaultFixtureId,
                "Tenant A administrator full trust",
                "tenant-a.admin.full-trust",
                "Tenant scope: Tenant A administrator",
                "Record identity: governed conversation full trust",
                "Trust posture: current trusted evidence",
                "Evidence completeness: complete citations and audit references",
                "Command eligibility: governance actions blocked from read surface",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.FullTrust)),
            ["TenantA_Reviewer_RedactedParticipants"] = FromProjection(
                "TenantA_Reviewer_RedactedParticipants",
                "Tenant A reviewer redacted participants",
                "tenant-a.reviewer.redacted-participants",
                "Tenant scope: Tenant A reviewer",
                "Record identity: governed redacted conversation",
                "Trust posture: redacted evidence available",
                "Evidence completeness: audit-backed redaction",
                "Command eligibility: read-only reviewer surface",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.Redacted)),
            ["TenantA_MobileTriage_ReadOnly"] = FromProjection(
                "TenantA_MobileTriage_ReadOnly",
                "Tenant A mobile triage read only",
                "tenant-a.mobile.read-only-triage",
                "Tenant scope: Tenant A mobile triage",
                "Record identity: governed conversation mobile triage",
                "Trust posture: current read-only triage",
                "Evidence completeness: current evidence with blocked governance actions",
                "Command eligibility: mobile read-only safe triage",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.BlockedCommand)),
            ["TenantB_NoAccess_CrossTenantPoison"] = FromUnauthorizedCrossTenant(
                "TenantB_NoAccess_CrossTenantPoison",
                "Tenant B no access cross-tenant attempt",
                "tenant-b.no-access.cross-tenant-attempt",
                "Tenant scope: no accessible tenant record",
                "Record identity: unavailable through current scope",
                "Trust posture: hidden by tenant boundary",
                "Evidence completeness: no accessible evidence",
                "Command eligibility: no governed action available",
                seed.AuthorizedTenantId,
                seed.PoisonProjection),
            ["MixedTimeline_PartialLoad_RedactedEvents"] = FromProjection(
                "MixedTimeline_PartialLoad_RedactedEvents",
                "Mixed timeline partial load redacted events",
                "tenant-a.timeline.partial-redacted",
                "Tenant scope: Tenant A reviewer",
                "Record identity: governed partial timeline",
                "Trust posture: redacted and partially loaded evidence",
                "Evidence completeness: incomplete timeline with redacted events",
                "Command eligibility: wait for current governed evidence",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.Redacted)),
            ["VirtualizedTimeline_RestrictedRowsAdjacentToVisibleRows"] = FromProjection(
                "VirtualizedTimeline_RestrictedRowsAdjacentToVisibleRows",
                "Virtualized timeline restricted adjacent rows",
                "tenant-a.timeline.virtualized-restricted",
                "Tenant scope: Tenant A reviewer",
                "Record identity: governed virtualized timeline",
                "Trust posture: visible rows are permission safe",
                "Evidence completeness: restricted adjacent rows withheld",
                "Command eligibility: read surface only",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.FullTrust)),
            ["UnauthorizedExisting_IndistinguishableFromMissing"] = Hidden(
                "UnauthorizedExisting_IndistinguishableFromMissing",
                "Unauthorized existing indistinguishable from missing",
                "tenant-a.unauthorized.safe-denial",
                "Tenant scope: unavailable through current authority",
                "Record identity: no accessible record",
                "Trust posture: hidden safe denial",
                "Evidence completeness: no accessible evidence",
                "Command eligibility: no governed action available"),
            ["PermissionDowngrade_WhileDrawerOpen"] = FromProjection(
                "PermissionDowngrade_WhileDrawerOpen",
                "Permission downgrade while drawer open",
                "tenant-a.permission-downgrade.drawer",
                "Tenant scope: Tenant A reviewer",
                "Record identity: governed downgraded conversation",
                "Trust posture: permission downgrade applied",
                "Evidence completeness: drawer uses authorized summary only",
                "Command eligibility: governance actions disabled",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.BlockedCommand)),
            ["MissingCitation_IncompleteEvidence"] = FromProjection(
                "MissingCitation_IncompleteEvidence",
                "Missing citation incomplete evidence",
                "tenant-a.missing-citation.incomplete",
                "Tenant scope: Tenant A reviewer",
                "Record identity: governed incomplete citation",
                "Trust posture: current evidence with incomplete citation",
                "Evidence completeness: citation unavailable",
                "Command eligibility: read-only remediation guidance",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.MissingCitation)),
            ["UnresolvedParticipant_DegradedHydration"] = FromProjection(
                "UnresolvedParticipant_DegradedHydration",
                "Unresolved participant degraded hydration",
                "tenant-a.unresolved-participant.degraded",
                "Tenant scope: Tenant A reviewer",
                "Record identity: governed unresolved participant",
                "Trust posture: degraded participant hydration",
                "Evidence completeness: evidence available with unresolved participant",
                "Command eligibility: wait for directory hydration",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.UnresolvedParticipant)),
            ["TenantA_Stale_RebuildingProjection"] = FromProjection(
                "TenantA_Stale_RebuildingProjection",
                "Tenant A stale rebuilding projection",
                "tenant-a.stale.rebuilding",
                "Tenant scope: Tenant A reviewer",
                "Record identity: governed stale conversation",
                "Trust posture: stale projection rebuilding",
                "Evidence completeness: stale evidence pending refresh",
                "Command eligibility: wait for current governed evidence",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.Stale)),
            ["HighContrast_ReducedMotion_BrowserZoom"] = FromProjection(
                "HighContrast_ReducedMotion_BrowserZoom",
                "High contrast reduced motion browser zoom",
                "tenant-a.high-contrast.reduced-motion.zoom",
                "Tenant scope: Tenant A operator",
                "Record identity: governed high contrast conversation",
                "Trust posture: current evidence under visual constraints",
                "Evidence completeness: complete evidence remains readable",
                "Command eligibility: safe blocked reasons remain visible",
                seed.AuthorizedProjections.Single(p => p.FixtureKind == BuyerAcceptanceDemoFixtureKind.FullTrust)),
        };

        _fixtures = fixtures;
    }

    public InvestigationWorkspaceViewModel Get(string? fixtureId)
    {
        string selected = string.IsNullOrWhiteSpace(fixtureId) ? DefaultFixtureId : fixtureId;
        return _fixtures.TryGetValue(selected, out InvestigationWorkspaceViewModel? workspace)
            ? workspace
            : _fixtures[DefaultFixtureId];
    }

    public IReadOnlyList<InvestigationWorkspaceFixtureSummary> List()
        => _fixtures
            .Values
            .Select(fixture => new InvestigationWorkspaceFixtureSummary(
                fixture.FixtureId,
                fixture.SafeLabel,
                fixture.SafeTelemetryLabel,
                fixture.MobileReadOnlyTriage))
            .ToArray();

    private static InvestigationWorkspaceViewModel FromProjection(
        string fixtureId,
        string safeLabel,
        string safeTelemetryLabel,
        string safeTenantScope,
        string safeRecordIdentity,
        string safeTrustPosture,
        string safeEvidenceCompleteness,
        string safeCommandEligibility,
        BuyerAcceptanceDemoProjectionPair pair)
    {
        ConversationEvidenceTrustPostureV1 posture = pair.Detail.TrustPosture;
        DateTimeOffset evaluatedAt = pair.Detail.Freshness.ProjectionGeneratedAt;

        // Every read surface must expose both a read-only action and a governance-changing
        // action so the mobile safe-triage assertion is non-vacuous: a fixture with no
        // governance control would pass `.every(disabled)` trivially. Missing classifications
        // are filled with safe metadata (the governance action is rendered disabled).
        List<ConversationCommandAvailabilityV1> commands = [.. posture.CommandEligibility];
        if (!commands.Any(command => command.ActionClassification == ConversationCommandAvailabilityV1.ReadOnlyActionClassification))
        {
            commands.Insert(0, SafeReadOnlyCommand(evaluatedAt));
        }

        if (!commands.Any(command => command.ActionClassification == ConversationCommandAvailabilityV1.GovernanceChangingActionClassification))
        {
            commands.Add(SafeBlockedGovernanceCommand(evaluatedAt));
        }

        return new InvestigationWorkspaceViewModel(
            fixtureId,
            safeLabel,
            safeTenantScope,
            safeRecordIdentity,
            safeTrustPosture,
            safeEvidenceCompleteness,
            safeCommandEligibility,
            safeTelemetryLabel,
            MobileReadOnlyTriage: true,
            SafeFixtureTags: ["responsive", "permission-safe", "synthetic"],
            pair.Summary,
            pair.Detail,
            pair.Detail.EvidenceEntries,
            commands);
    }

    private static InvestigationWorkspaceViewModel Hidden(
        string fixtureId,
        string safeLabel,
        string safeTelemetryLabel,
        string safeTenantScope,
        string safeRecordIdentity,
        string safeTrustPosture,
        string safeEvidenceCompleteness,
        string safeCommandEligibility)
        => new(
            fixtureId,
            safeLabel,
            safeTenantScope,
            safeRecordIdentity,
            safeTrustPosture,
            safeEvidenceCompleteness,
            safeCommandEligibility,
            safeTelemetryLabel,
            MobileReadOnlyTriage: true,
            SafeFixtureTags: ["responsive", "permission-safe", "safe-denial"],
            Summary: null,
            Detail: null,
            EvidenceEntries: [],
            CommandEligibility:
            [
                new ConversationCommandAvailabilityV1(
                    "read-governed-record",
                    ProjectionTrustState.Unavailable,
                    "conversations.read",
                    ProjectionTrustState.Unavailable,
                    "read",
                    ProjectionTrustState.Unavailable,
                    ConversationAuditReadinessState.Unknown,
                    "No governed record is available through this scope.",
                    new DateTimeOffset(2026, 5, 22, 9, 0, 0, TimeSpan.Zero),
                    ConversationCommandAvailabilityV1.ReadOnlyActionClassification),
            ]);

    /// <summary>
    /// Maps a candidate projection through a fail-closed tenant boundary. A record that is
    /// outside the operator's authorized tenant scope (the cross-tenant "poison" projection)
    /// is mapped to an indistinguishable hidden read, so none of its content — including the
    /// poison sentinel values — reaches the view model or any rendered surface. If this guard
    /// ever regressed to surface the cross-tenant projection, the rendered poison sentinels
    /// would appear and the Story 3.8A sentinel scans would fail.
    /// </summary>
    private static InvestigationWorkspaceViewModel FromUnauthorizedCrossTenant(
        string fixtureId,
        string safeLabel,
        string safeTelemetryLabel,
        string safeTenantScope,
        string safeRecordIdentity,
        string safeTrustPosture,
        string safeEvidenceCompleteness,
        string safeCommandEligibility,
        TenantId authorizedTenant,
        BuyerAcceptanceDemoProjectionPair candidate)
    {
        bool withinAuthorizedScope =
            candidate.Summary.TenantId == authorizedTenant
            && candidate.Detail.TenantId == authorizedTenant;

        return withinAuthorizedScope
            ? FromProjection(
                fixtureId,
                safeLabel,
                safeTelemetryLabel,
                safeTenantScope,
                safeRecordIdentity,
                safeTrustPosture,
                safeEvidenceCompleteness,
                safeCommandEligibility,
                candidate)
            : Hidden(
                fixtureId,
                safeLabel,
                safeTelemetryLabel,
                safeTenantScope,
                safeRecordIdentity,
                safeTrustPosture,
                safeEvidenceCompleteness,
                safeCommandEligibility);
    }

    private static ConversationCommandAvailabilityV1 SafeReadOnlyCommand(DateTimeOffset evaluatedAt)
        => new(
            "read-governed-record",
            ProjectionTrustState.Current,
            "conversations.read",
            ProjectionTrustState.Current,
            "read",
            ProjectionTrustState.Current,
            ConversationAuditReadinessState.Ready,
            "Read governed evidence.",
            evaluatedAt,
            ConversationCommandAvailabilityV1.ReadOnlyActionClassification);

    private static ConversationCommandAvailabilityV1 SafeBlockedGovernanceCommand(DateTimeOffset evaluatedAt)
        => new(
            "set-retention-policy",
            ProjectionTrustState.Unavailable,
            "conversations.governance",
            ProjectionTrustState.Current,
            "governance",
            ProjectionTrustState.Current,
            ConversationAuditReadinessState.Ready,
            "Action requires current evidence and audit readiness.",
            evaluatedAt,
            ConversationCommandAvailabilityV1.GovernanceChangingActionClassification);
}
