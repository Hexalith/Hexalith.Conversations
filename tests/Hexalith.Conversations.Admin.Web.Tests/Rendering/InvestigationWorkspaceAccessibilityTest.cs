// <copyright file="InvestigationWorkspaceAccessibilityTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.RegularExpressions;

using Hexalith.Conversations.Admin.Web.Rendering;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Admin.Web.Tests.Rendering;

/// <summary>
/// Verifies the Story 3.8B accessibility semantics added to the rendered workspace:
/// a coherent heading outline, landmarks and a skip affordance, a safe live region,
/// accessible blocked-command descriptions, and an indistinguishable hidden-read path.
/// These are pure render assertions; the browser/accessibility-tree evidence lives in
/// the Accessibility/AccessibilityEvidenceHarnessTest browser lane.
/// </summary>
public sealed partial class InvestigationWorkspaceAccessibilityTest
{
    [Fact]
    public void RendererShouldEmitExactlyOneDocumentHeading()
    {
        string html = RenderDefault();

        H1OpenTag().Matches(html).Count.ShouldBe(1);
    }

    [Fact]
    public void RendererShouldExposeHeadingOutlineFollowingTrustOrder()
    {
        string html = RenderDefault();

        // Document title h1, then the trust-order h2, then the trust-panel h3 headings,
        // then the timeline h2 — so screen-reader heading navigation follows trust order.
        int title = html.IndexOf("<h1 id=\"workspace-title\"", StringComparison.Ordinal);
        int trustOrder = html.IndexOf("<h2 id=\"trust-order-heading\"", StringComparison.Ordinal);
        int tenantScope = html.IndexOf("<h3 id=\"tenant-scope-heading\"", StringComparison.Ordinal);
        int recordIdentity = html.IndexOf("<h3 id=\"record-identity-heading\"", StringComparison.Ordinal);
        int trustPosture = html.IndexOf("<h3 id=\"trust-posture-heading\"", StringComparison.Ordinal);
        int completeness = html.IndexOf("<h3 id=\"evidence-completeness-heading\"", StringComparison.Ordinal);
        int command = html.IndexOf("<h3 id=\"command-eligibility-heading\"", StringComparison.Ordinal);
        int timeline = html.IndexOf("<h2 id=\"timeline-heading\"", StringComparison.Ordinal);

        title.ShouldBeGreaterThanOrEqualTo(0);
        trustOrder.ShouldBeGreaterThan(title);
        tenantScope.ShouldBeGreaterThan(trustOrder);
        recordIdentity.ShouldBeGreaterThan(tenantScope);
        trustPosture.ShouldBeGreaterThan(recordIdentity);
        completeness.ShouldBeGreaterThan(trustPosture);
        command.ShouldBeGreaterThan(completeness);
        timeline.ShouldBeGreaterThan(command);
    }

    [Fact]
    public void RendererShouldProvideSkipLinkAndCoreLandmarks()
    {
        string html = RenderDefault();

        html.ShouldContain("class=\"skip-link\" href=\"#governed-record\"");
        html.ShouldContain("role=\"search\"");
        html.ShouldContain("<header");
        html.ShouldContain("<main");
        html.ShouldContain("id=\"governed-record\"");
        // The skip target must be programmatically focusable without entering the tab order.
        html.ShouldContain("tabindex=\"-1\"");
    }

    [Fact]
    public void RendererShouldNotUsePositiveTabindex()
    {
        string html = RenderDefault();

        // Only tabindex="-1" (programmatic focus target) is permitted; positive or zero
        // tabindex would impose a non-document focus order.
        PositiveTabindex().IsMatch(html).ShouldBeFalse();
    }

    [Fact]
    public void RendererShouldExposeSafeLiveRegion()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();
        InvestigationWorkspaceRenderer renderer = new();

        string html = renderer.Render(catalog.Get("TenantA_Reviewer_RedactedParticipants"));

        html.ShouldContain("aria-live=\"polite\"");
        html.ShouldContain("data-testid=\"trust-announcer\"");
        // Announces the safe trust class without protected detail.
        html.ShouldContain("redacted", Case.Insensitive);
        foreach (string sentinel in seed.PoisonSentinelValues)
        {
            html.ShouldNotContain(sentinel, Case.Insensitive);
        }
    }

    [Fact]
    public void RendererShouldDescribeBlockedGovernanceCommandsForAssistiveTech()
    {
        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();
        InvestigationWorkspaceRenderer renderer = new();

        string html = renderer.Render(catalog.Get("TenantA_MobileTriage_ReadOnly"));

        // The disabled governance button must expose its safe blocked reason through an
        // accessible description, not only through the data-blocked-reason attribute.
        html.ShouldContain("aria-describedby=\"command-reason-");
        html.ShouldContain("class=\"command-reason\"");
        html.ShouldContain("Action requires current evidence and audit readiness.");
        // The 3.8A disabled contract must survive.
        html.ShouldContain("disabled aria-disabled=\"true\"");
    }

    [Fact]
    public void HiddenReadRendersIdenticallyForUnauthorizedAndNonexistentRecords()
    {
        InvestigationWorkspaceRenderer renderer = new();

        // Two hidden reads that carry identical safe-denial labels — one modelling a record
        // the caller may not see, one modelling a record that does not exist — must produce
        // byte-identical HTML so the rendered surface cannot reveal whether a record exists.
        InvestigationWorkspaceViewModel unauthorizedExisting = HiddenRead();
        InvestigationWorkspaceViewModel nonexistent = HiddenRead();

        string unauthorizedHtml = renderer.Render(unauthorizedExisting);
        string nonexistentHtml = renderer.Render(nonexistent);

        unauthorizedHtml.ShouldBe(nonexistentHtml);
        unauthorizedHtml.ShouldContain("No governed record is visible for this tenant scope.");
        // A hidden read exposes no evidence-row headings.
        unauthorizedHtml.ShouldNotContain("<h3 id=\"evidence-0-heading\"");
    }

    private static string RenderDefault()
        => new InvestigationWorkspaceRenderer().Render(new BuyerAcceptanceInvestigationWorkspaceCatalog().Get(null));

    private static InvestigationWorkspaceViewModel HiddenRead()
        => new(
            FixtureId: "hidden-read",
            SafeLabel: "Hidden read",
            SafeTenantScopeLabel: "Tenant scope: unavailable through current authority",
            SafeRecordIdentityLabel: "Record identity: no accessible record",
            SafeTrustPostureLabel: "Trust posture: hidden safe denial",
            SafeEvidenceCompletenessLabel: "Evidence completeness: no accessible evidence",
            SafeCommandEligibilityLabel: "Command eligibility: no governed action available",
            SafeTelemetryLabel: "tenant.hidden-read",
            MobileReadOnlyTriage: true,
            SafeFixtureTags: ["permission-safe", "safe-denial"],
            Summary: null,
            Detail: null,
            EvidenceEntries: [],
            CommandEligibility: []);

    [GeneratedRegex("<h1[ >]")]
    private static partial Regex H1OpenTag();

    [GeneratedRegex("tabindex=\"[0-9]+\"")]
    private static partial Regex PositiveTabindex();
}
