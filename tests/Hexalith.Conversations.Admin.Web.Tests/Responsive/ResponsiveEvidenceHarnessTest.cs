// <copyright file="ResponsiveEvidenceHarnessTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Conversations.Admin.Web.Rendering;
using Hexalith.Conversations.Admin.Web.Tests.Fixtures;
using Hexalith.Conversations.Admin.Web.Tests.Support;
using Hexalith.Conversations.Testing.Fixtures;

using Microsoft.Playwright;

namespace Hexalith.Conversations.Admin.Web.Tests.Responsive;

/// <summary>
/// Generates machine-readable responsive rendered-surface evidence for Story 3.8A.
/// </summary>
[Collection(RenderedWorkspaceCollection.Name)]
public sealed class ResponsiveEvidenceHarnessTest(
    AdminWebHostFixture host,
    PlaywrightFixture playwright)
{
    // The zoom rows model true 200% browser zoom by halving the effective CSS viewport, which
    // forces a real reflow (CSS `zoom` would leave window.innerWidth unchanged and prove nothing).
    // ExpectedTelemetryViewport therefore reflects the reflowed width: a 1280px desktop at 200%
    // zoom presents a 640px CSS viewport and reclassifies as tablet.
    private static readonly ViewportSpec[] Viewports =
    [
        new("mobile-360x780", 360, 780, "mobile"),
        new("mobile-390x844", 390, 844, "mobile"),
        new("tablet-768x1024", 768, 1024, "tablet"),
        new("desktop-1280x800", 1280, 800, "desktop"),
        new("wide-desktop-1440x1000", 1440, 1000, "wide-desktop"),
        new("mobile-390x844-zoom-200", 390, 844, "mobile", 200),
        new("desktop-1280x800-zoom-200", 1280, 800, "tablet", 200),
    ];

    [Fact]
    public async Task ResponsiveEvidenceHarnessShouldProveViewportAndFixtureMatrix()
    {
        string repoRoot = RepositoryPaths.FindRoot();
        string evidenceRoot = Path.Combine(
            repoRoot,
            "_bmad-output",
            "implementation-artifacts",
            "evidence",
            "3-8a-responsive-layout-mobile-safe-triage");
        string screenshotRoot = Path.Combine(evidenceRoot, "screenshots");
        Directory.CreateDirectory(screenshotRoot);

        // Refresh the machine-readable artifacts so a failed or partial run can never leave a
        // stale green result behind; the finally block below rewrites them from this run's data.
        DeleteStaleEvidence(evidenceRoot);

        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        List<ResponsiveEvidenceRecord> records = [];
        List<TelemetryLabelScanRecord> telemetryRecords = [];
        int totalGovernanceControls = 0;
        int mobileGovernanceControls = 0;

        bool ContainsSentinel(string haystack)
            => seed.PoisonSentinelValues.Any(s => haystack.Contains(s, StringComparison.OrdinalIgnoreCase));

        try
        {
            foreach (InvestigationWorkspaceFixtureSummary fixture in catalog.List())
            {
                foreach (ViewportSpec viewport in Viewports)
                {
                    int effectiveWidth = viewport.Width * 100 / viewport.ZoomPercent;
                    int effectiveHeight = viewport.Height * 100 / viewport.ZoomPercent;

                    await using IBrowserContext context = await playwright.Browser.NewContextAsync(
                        CreateContextOptions(fixture, effectiveWidth, effectiveHeight));
                    IPage page = await context.NewPageAsync();

                    string url = $"{host.BaseAddress}/investigations?fixture={Uri.EscapeDataString(fixture.FixtureId)}";
                    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                    await page.Locator("[data-testid='workspace-root']").WaitForAsync();

                    // The viewport classifier and telemetry labels are set by an inline script. Wait
                    // for it to resolve before reading attributes so a slow run cannot observe the
                    // server-rendered "unknown" placeholder and flake.
                    await page.WaitForFunctionAsync(
                        "() => { const r = document.querySelector('[data-testid=\"workspace-root\"]'); return r !== null && r.getAttribute('data-current-viewport') !== 'unknown'; }");

                    string title = await page.TitleAsync();
                    string bodyText = await page.Locator("body").InnerTextAsync();
                    string attributes = await CollectAttributesAsync(page, "*");
                    string duplicateAttributes = await CollectAttributesAsync(page, "[data-responsive-duplicate='true']");
                    IReadOnlyList<string> duplicateTexts = await page.Locator("[data-responsive-duplicate='true']").AllInnerTextsAsync();
                    string duplicateText = string.Join(Environment.NewLine, duplicateTexts);
                    string[] telemetryLabels = await page.EvaluateAsync<string[]>(
                        "() => Array.from(document.querySelectorAll('[data-telemetry-label]')).map(e => e.getAttribute('data-telemetry-label'))");
                    string telemetryJoined = string.Join(Environment.NewLine, telemetryLabels);
                    string currentViewport = await page.Locator("[data-testid='workspace-root']")
                        .GetAttributeAsync("data-current-viewport") ?? string.Empty;
                    string[] trustOrder = await page.EvaluateAsync<string[]>(
                        "() => Array.from(document.querySelectorAll('[data-trust-rank]')).map(e => e.getAttribute('data-testid'))");

                    // Returns false (rather than throwing) when a trust panel or the timeline is
                    // missing, so a structural regression surfaces as a clear assertion failure.
                    bool trustBeforeTimeline = await page.EvaluateAsync<bool>(
                        "() => { const ids = ['tenant-scope','record-identity','trust-posture','evidence-completeness','command-eligibility']; const timelineEl = document.querySelector('[data-testid=\"timeline\"]'); if (!timelineEl) { return false; } const timelineTop = timelineEl.getBoundingClientRect().top; return ids.every(id => { const el = document.querySelector('[data-testid=\"' + id + '\"]'); return el !== null && el.getBoundingClientRect().top <= timelineTop; }); }");
                    int governanceControlCount = await page.EvaluateAsync<int>(
                        "() => document.querySelectorAll('[data-action-classification=\"governance-changing\"]').length");
                    bool mobileReadOnly = await page.EvaluateAsync<bool>(
                        "() => Array.from(document.querySelectorAll('[data-action-classification=\"governance-changing\"]')).every(e => e.disabled || e.getAttribute('aria-disabled') === 'true')");

                    totalGovernanceControls += governanceControlCount;
                    if (viewport.ExpectedTelemetryViewport == "mobile")
                    {
                        mobileGovernanceControls += governanceControlCount;
                    }

                    // Compute every evidence flag from observed state, then record the row BEFORE
                    // asserting, so a failing row is persisted as a real failure rather than dropped.
                    bool viewportMatched = currentViewport == viewport.ExpectedTelemetryViewport;
                    bool trustOrderPreserved = IsTrustOrderPreserved(trustOrder) && trustBeforeTimeline;
                    bool responsiveDuplicateSafetyPassed = !ContainsSentinel(duplicateText) && !ContainsSentinel(duplicateAttributes);
                    bool poisonSentinelScanPassed =
                        !ContainsSentinel(title)
                        && !ContainsSentinel(bodyText)
                        && !ContainsSentinel(attributes)
                        && responsiveDuplicateSafetyPassed
                        && !ContainsSentinel(telemetryJoined);
                    bool telemetryViewportTagged =
                        telemetryLabels.Length > 0
                        && telemetryLabels.All(label =>
                            label is not null
                            && label.EndsWith("." + viewport.ExpectedTelemetryViewport, StringComparison.Ordinal));

                    string? screenshotPath = null;
                    if (ShouldCaptureScreenshot(fixture.FixtureId, viewport))
                    {
                        screenshotPath = Path.Combine(screenshotRoot, $"{fixture.FixtureId}-{viewport.Name}.png");
                        await page.ScreenshotAsync(new PageScreenshotOptions
                        {
                            FullPage = true,
                            Path = screenshotPath,
                        });
                    }

                    records.Add(new ResponsiveEvidenceRecord(
                        fixture.FixtureId,
                        viewport.Name,
                        effectiveWidth,
                        effectiveHeight,
                        viewport.ExpectedTelemetryViewport,
                        viewport.ZoomPercent,
                        TrustOrderPreserved: trustOrderPreserved,
                        ResponsiveDuplicateSafetyPassed: responsiveDuplicateSafetyPassed,
                        MobileReadOnlyTriagePassed: mobileReadOnly,
                        GovernanceControlCount: governanceControlCount,
                        PoisonSentinelScanPassed: poisonSentinelScanPassed,
                        ScreenshotPath: screenshotPath is null ? null : Path.GetRelativePath(repoRoot, screenshotPath)));

                    telemetryRecords.Add(new TelemetryLabelScanRecord(
                        fixture.FixtureId,
                        viewport.Name,
                        viewport.ExpectedTelemetryViewport,
                        telemetryLabels.Order(StringComparer.Ordinal).ToArray(),
                        ContainsForbiddenSentinel: ContainsSentinel(telemetryJoined)));

                    viewportMatched.ShouldBeTrue(
                        $"Expected viewport '{viewport.ExpectedTelemetryViewport}' for {fixture.FixtureId}/{viewport.Name} but observed '{currentViewport}'.");
                    trustOrderPreserved.ShouldBeTrue(
                        $"Trust order not preserved for {fixture.FixtureId}/{viewport.Name}.");
                    poisonSentinelScanPassed.ShouldBeTrue(
                        $"Cross-tenant poison sentinel leaked into {fixture.FixtureId}/{viewport.Name}.");
                    telemetryViewportTagged.ShouldBeTrue(
                        $"Telemetry labels for {fixture.FixtureId}/{viewport.Name} must be non-empty and suffixed with '.{viewport.ExpectedTelemetryViewport}'.");
                    if (viewport.ExpectedTelemetryViewport == "mobile")
                    {
                        mobileReadOnly.ShouldBeTrue(
                            $"Governance-changing controls must be disabled on mobile for {fixture.FixtureId}/{viewport.Name}.");
                    }
                }
            }

            // Suite-level guard against vacuity: prove governance-changing controls were actually
            // rendered (and, on mobile, that the disabled-control assertion above had something to
            // assert on). Without this, an empty NodeList would make the per-row mobile check pass
            // trivially.
            totalGovernanceControls.ShouldBeGreaterThan(0, "No governance-changing controls were rendered across the matrix.");
            mobileGovernanceControls.ShouldBeGreaterThan(0, "No governance-changing controls were rendered on mobile viewports.");
        }
        finally
        {
            WriteEvidence(evidenceRoot, records, telemetryRecords);
        }
    }

    private static BrowserNewContextOptions CreateContextOptions(
        InvestigationWorkspaceFixtureSummary fixture,
        int width,
        int height)
    {
        BrowserNewContextOptions options = new()
        {
            ViewportSize = new ViewportSize
            {
                Width = width,
                Height = height,
            },
        };

        if (fixture.FixtureId == "HighContrast_ReducedMotion_BrowserZoom")
        {
            options.ForcedColors = ForcedColors.Active;
            options.ReducedMotion = ReducedMotion.Reduce;
        }

        return options;
    }

    private static async Task<string> CollectAttributesAsync(IPage page, string selector)
        => await page.EvaluateAsync<string>(
            @"selector => Array.from(document.querySelectorAll(selector))
                .flatMap(e => Array.from(e.attributes).map(a => a.name + '=' + a.value))
                .join('\n')",
            selector);

    private static bool IsTrustOrderPreserved(string[] trustOrder)
    {
        int scope = Array.IndexOf(trustOrder, "tenant-scope");
        int identity = Array.IndexOf(trustOrder, "record-identity");
        int trust = Array.IndexOf(trustOrder, "trust-posture");
        int completeness = Array.IndexOf(trustOrder, "evidence-completeness");
        int eligibility = Array.IndexOf(trustOrder, "command-eligibility");
        int timeline = Array.IndexOf(trustOrder, "timeline");

        return scope >= 0
            && identity > scope
            && trust > identity
            && completeness > trust
            && eligibility > completeness
            && timeline > eligibility;
    }

    private static bool ShouldCaptureScreenshot(string fixtureId, ViewportSpec viewport)
        => fixtureId is
            "TenantA_Admin_FullTrust" or
            "TenantB_NoAccess_CrossTenantPoison" or
            "HighContrast_ReducedMotion_BrowserZoom"
            || viewport.ZoomPercent == 200;

    private static void DeleteStaleEvidence(string evidenceRoot)
    {
        foreach (string name in new[]
                 {
                     "viewport-matrix.json",
                     "safe-telemetry-label-scan.json",
                     "fixture-matrix.json",
                     "evidence-summary.md",
                 })
        {
            string path = Path.Combine(evidenceRoot, name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void WriteEvidence(
        string evidenceRoot,
        IReadOnlyList<ResponsiveEvidenceRecord> records,
        IReadOnlyList<TelemetryLabelScanRecord> telemetryRecords)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        File.WriteAllText(
            Path.Combine(evidenceRoot, "viewport-matrix.json"),
            JsonSerializer.Serialize(records, options));
        File.WriteAllText(
            Path.Combine(evidenceRoot, "safe-telemetry-label-scan.json"),
            JsonSerializer.Serialize(telemetryRecords, options));

        string[] fixtureIds = records
            .Select(record => record.FixtureId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        File.WriteAllText(
            Path.Combine(evidenceRoot, "fixture-matrix.json"),
            JsonSerializer.Serialize(fixtureIds, options));

        IReadOnlyList<ResponsiveEvidenceRecord> mobileRows =
            [.. records.Where(record => record.TelemetryViewport == "mobile")];
        string Status(bool passed) => passed ? "passed" : "FAILED";
        bool trustOrder = records.All(record => record.TrustOrderPreserved);
        bool duplicateSafety = records.All(record => record.ResponsiveDuplicateSafetyPassed);
        bool mobileTriage = mobileRows.All(record => record.MobileReadOnlyTriagePassed)
            && mobileRows.Sum(record => record.GovernanceControlCount) > 0;
        bool poisonScan = records.All(record => record.PoisonSentinelScanPassed);
        bool telemetryScan = telemetryRecords.All(record => !record.ContainsForbiddenSentinel);

        File.WriteAllText(
            Path.Combine(evidenceRoot, "evidence-summary.md"),
            $"""
            # Story 3.8A Responsive Evidence

            Generated by `Hexalith.Conversations.Admin.Web.Tests`. All flags below are derived
            from the per-row measurements in `viewport-matrix.json`, not hard-coded.

            - Fixture count: {fixtureIds.Length}
            - Viewport rows: {records.Count}
            - Trust order: {Status(trustOrder)}
            - Responsive duplicate safety: {Status(duplicateSafety)}
            - Mobile safe triage: {Status(mobileTriage)}
            - Poison sentinel scan: {Status(poisonScan)}
            - Safe telemetry label scan: {Status(telemetryScan)}
            - Browser zoom equivalents: mobile and desktop 200 percent modelled as a halved CSS
              viewport so the layout actually reflows (a 1280px desktop reflows to the tablet layout).
            - High contrast / reduced motion: covered by `HighContrast_ReducedMotion_BrowserZoom`

            This evidence is scoped to Story 3.8A only. Accessibility-tree, screen-reader,
            clipboard, browser-title, tooltip, screenshot-disclosure, and full telemetry
            disclosure closure remain owned by Stories 3.8B and 3.8C.
            """);
    }

    private sealed record ViewportSpec(
        string Name,
        int Width,
        int Height,
        string ExpectedTelemetryViewport,
        int ZoomPercent = 100);

    private sealed record ResponsiveEvidenceRecord(
        string FixtureId,
        string Viewport,
        int Width,
        int Height,
        string TelemetryViewport,
        int ZoomPercent,
        bool TrustOrderPreserved,
        bool ResponsiveDuplicateSafetyPassed,
        bool MobileReadOnlyTriagePassed,
        int GovernanceControlCount,
        bool PoisonSentinelScanPassed,
        string? ScreenshotPath);

    private sealed record TelemetryLabelScanRecord(
        string FixtureId,
        string Viewport,
        string TelemetryViewport,
        IReadOnlyList<string> Labels,
        bool ContainsForbiddenSentinel);
}
