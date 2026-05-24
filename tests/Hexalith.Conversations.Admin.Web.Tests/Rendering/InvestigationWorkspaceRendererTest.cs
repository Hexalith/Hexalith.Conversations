// <copyright file="InvestigationWorkspaceRendererTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Admin.Web.Rendering;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Admin.Web.Tests.Rendering;

/// <summary>
/// Verifies the rendered workspace consumes permission-safe fixture data.
/// </summary>
public sealed class InvestigationWorkspaceRendererTest
{
    [Fact]
    public void CatalogShouldExposeRequiredResponsiveFixtures()
    {
        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();

        catalog.List().Select(fixture => fixture.FixtureId).ShouldBe(
            [
                "TenantA_Admin_FullTrust",
                "TenantA_Reviewer_RedactedParticipants",
                "TenantA_MobileTriage_ReadOnly",
                "TenantB_NoAccess_CrossTenantPoison",
                "MixedTimeline_PartialLoad_RedactedEvents",
                "VirtualizedTimeline_RestrictedRowsAdjacentToVisibleRows",
                "UnauthorizedExisting_IndistinguishableFromMissing",
                "PermissionDowngrade_WhileDrawerOpen",
                "MissingCitation_IncompleteEvidence",
                "UnresolvedParticipant_DegradedHydration",
                "TenantA_Stale_RebuildingProjection",
                "HighContrast_ReducedMotion_BrowserZoom",
            ],
            ignoreOrder: false);
    }

    [Fact]
    public void RendererShouldNotRenderCrossTenantPoisonSentinels()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();
        InvestigationWorkspaceRenderer renderer = new();

        string html = renderer.Render(catalog.Get("TenantB_NoAccess_CrossTenantPoison"));

        foreach (string sentinel in seed.PoisonSentinelValues)
        {
            html.ShouldNotContain(sentinel, Case.Insensitive);
        }

        html.ShouldContain("data-testid=\"tenant-scope\"");
        html.ShouldContain("data-testid=\"record-identity\"");
        html.ShouldContain("data-testid=\"trust-posture\"");
        html.ShouldContain("data-testid=\"evidence-completeness\"");
        html.ShouldContain("data-testid=\"command-eligibility\"");
        html.ShouldContain("data-testid=\"timeline\"");
    }

    [Fact]
    public void RendererEmitsSuppliedContentVerbatim_SoSentinelScanCanFail()
    {
        // Negative control: the renderer faithfully emits whatever the view model carries.
        // This proves the poison-sentinel scan is capable of failing, so the clean scan of
        // the cross-tenant fixture is a real result of the tenant guard, not a vacuous pass.
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        string sentinel = seed.PoisonSentinelValues[0];
        InvestigationWorkspaceViewModel leaky = new(
            "LeakyControl",
            "leaky",
            $"Tenant scope: {sentinel}",
            "Record identity",
            "Trust posture",
            "Evidence completeness",
            "Command eligibility",
            "tenant.leaky-control",
            MobileReadOnlyTriage: true,
            SafeFixtureTags: [],
            Summary: null,
            Detail: null,
            EvidenceEntries: [],
            CommandEligibility: []);

        string html = new InvestigationWorkspaceRenderer().Render(leaky);

        html.ShouldContain(sentinel);
    }

    [Fact]
    public void CrossTenantFixtureIsBuiltFromPoisonProjectionYetHidesEverySentinel()
    {
        // The cross-tenant fixture is constructed by feeding the actual poison projection
        // (tenant 'poison-tenant') through the catalog's fail-closed tenant boundary. Because
        // the record is outside the authorized scope it maps to an indistinguishable hidden
        // read, so no sentinel survives into any rendered surface.
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();

        InvestigationWorkspaceViewModel workspace = catalog.Get("TenantB_NoAccess_CrossTenantPoison");
        workspace.IsHiddenRead.ShouldBeTrue();

        string html = new InvestigationWorkspaceRenderer().Render(workspace);
        foreach (string sentinel in seed.PoisonSentinelValues)
        {
            html.ShouldNotContain(sentinel, Case.Insensitive);
        }
    }

    [Fact]
    public void RendererShouldDisableGovernanceChangingActionsForMobileTriage()
    {
        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();
        InvestigationWorkspaceRenderer renderer = new();

        string html = renderer.Render(catalog.Get("TenantA_MobileTriage_ReadOnly"));

        html.ShouldContain("data-mobile-triage=\"read-only\"");
        html.ShouldContain("data-action-classification=\"governance-changing\"");
        html.ShouldContain("disabled aria-disabled=\"true\"");
    }
}
