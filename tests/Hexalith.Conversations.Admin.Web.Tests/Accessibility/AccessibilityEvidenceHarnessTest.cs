// <copyright file="AccessibilityEvidenceHarnessTest.cs" company="ITANEO">
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

namespace Hexalith.Conversations.Admin.Web.Tests.Accessibility;

/// <summary>
/// Generates machine-readable accessibility-tree, heading-outline, landmark, keyboard
/// focus-order, and accessible-name safety evidence for Story 3.8B against the rendered
/// Admin Web surface driven through a real headless browser.
/// </summary>
[Collection(RenderedWorkspaceCollection.Name)]
public sealed class AccessibilityEvidenceHarnessTest(
    AdminWebHostFixture host,
    PlaywrightFixture playwright)
{
    // Captures the document reading order a screen reader follows: the document title, the
    // trust-order section heading, the four trust panels, the command gate, then the timeline.
    private static readonly string[] ExpectedHeadingOrder =
    [
        "workspace-title",
        "trust-order-heading",
        "tenant-scope-heading",
        "record-identity-heading",
        "trust-posture-heading",
        "evidence-completeness-heading",
        "command-eligibility-heading",
        "timeline-heading",
    ];

    [Fact]
    public async Task AccessibilityEvidenceHarnessShouldProveSafeAccessibleWorkspace()
    {
        string repoRoot = RepositoryPaths.FindRoot();
        string evidenceRoot = Path.Combine(
            repoRoot,
            "_bmad-output",
            "implementation-artifacts",
            "evidence",
            "3-8b-accessibility-tree-keyboard-screen-reader-safety");
        Directory.CreateDirectory(evidenceRoot);

        // Refresh artifacts so a failed or partial run can never leave a stale green result.
        DeleteStaleEvidence(evidenceRoot);

        BuyerAcceptanceInvestigationWorkspaceCatalog catalog = new();
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();

        AccessibilityScenario[] scenarios = BuildScenarios(catalog);
        List<AccessibilityEvidenceRecord> records = [];
        List<AriaSnapshotRecord> snapshots = [];
        List<FocusOrderRecord> focusTraces = [];
        int totalCommandButtons = 0;
        int forcedColorsRows = 0;

        bool ContainsSentinel(string haystack)
            => seed.PoisonSentinelValues.Any(s => haystack.Contains(s, StringComparison.OrdinalIgnoreCase));

        try
        {
            foreach (AccessibilityScenario scenario in scenarios)
            {
                await using IBrowserContext context = await playwright.Browser.NewContextAsync(
                    CreateContextOptions(scenario));
                IPage page = await context.NewPageAsync();

                string url = $"{host.BaseAddress}/investigations?fixture={Uri.EscapeDataString(scenario.FixtureId)}";
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await page.Locator("[data-testid='workspace-root']").WaitForAsync();
                await page.WaitForFunctionAsync(
                    "() => { const r = document.querySelector('[data-testid=\"workspace-root\"]'); return r !== null && r.getAttribute('data-current-viewport') !== 'unknown'; }");

                // --- Accessibility tree (what assistive technology consumes) ---
                string ariaSnapshot = await page.Locator("body").AriaSnapshotAsync();

                // --- Heading outline in document order ---
                string[] headingOutline = await page.EvaluateAsync<string[]>(
                    "() => Array.from(document.querySelectorAll('h1,h2,h3,h4,h5,h6')).map(h => h.tagName.toLowerCase() + '|' + (h.id || '') + '|' + ((h.innerText || h.textContent) || '').trim())");
                int h1Count = await page.EvaluateAsync<int>("() => document.querySelectorAll('h1').length");

                // --- Landmarks via accessible roles ---
                int mainCount = await page.GetByRole(AriaRole.Main).CountAsync();
                int bannerCount = await page.GetByRole(AriaRole.Banner).CountAsync();
                int searchCount = await page.GetByRole(AriaRole.Search).CountAsync();
                bool skipLinkPresent = await page.Locator("[data-testid='skip-to-record']").CountAsync() > 0;

                // --- Accessible-name / description surface (resolved) ---
                string accessibleTextSurface = await CollectAccessibleTextAsync(page);

                // --- Trust-order reading order (mirrors the 3.8A trust ordering contract) ---
                string[] trustOrder = await page.EvaluateAsync<string[]>(
                    "() => Array.from(document.querySelectorAll('[data-trust-rank]')).map(e => e.getAttribute('data-testid'))");
                bool trustBeforeTimeline = await page.EvaluateAsync<bool>(
                    "() => { const ids = ['tenant-scope','record-identity','trust-posture','evidence-completeness','command-eligibility']; const timelineEl = document.querySelector('[data-testid=\"timeline\"]'); if (!timelineEl) { return false; } const timelineTop = timelineEl.getBoundingClientRect().top; return ids.every(id => { const el = document.querySelector('[data-testid=\"' + id + '\"]'); return el !== null && el.getBoundingClientRect().top <= timelineTop; }); }");

                // --- Blocked-command reasons resolvable through the accessible description ---
                int commandButtonCount = await page.Locator("[data-testid='command-action']").CountAsync();
                bool blockedReasonExposed = await page.EvaluateAsync<bool>(
                    "() => { const buttons = Array.from(document.querySelectorAll('[data-testid=\"command-action\"]')); if (buttons.length === 0) { return true; } return buttons.every(b => { const id = b.getAttribute('aria-describedby'); if (!id) { return false; } const desc = document.getElementById(id); return desc !== null && ((desc.innerText || desc.textContent) || '').trim().length > 0; }); }");

                // --- Keyboard focus-order trace ---
                List<string> focusTrace = await CaptureFocusOrderAsync(page);

                // --- Visual-constraint signals ---
                bool forcedColorsActive = await page.EvaluateAsync<bool>(
                    "() => window.matchMedia('(forced-colors: active)').matches");
                bool reducedMotionActive = await page.EvaluateAsync<bool>(
                    "() => window.matchMedia('(prefers-reduced-motion: reduce)').matches");

                // Compute every flag from observed state, then record BEFORE asserting so a
                // failing row is persisted as a real failure rather than dropped.
                bool singleDocumentHeading = h1Count == 1;
                bool headingOrderValid = IsHeadingOrderValid(headingOutline);
                bool landmarksPresent = mainCount == 1 && bannerCount >= 1 && searchCount >= 1 && skipLinkPresent;
                bool trustOrderPreserved = IsTrustOrderPreserved(trustOrder) && trustBeforeTimeline;
                bool accessibleNameScanPassed =
                    !ContainsSentinel(ariaSnapshot)
                    && !ContainsSentinel(accessibleTextSurface)
                    && !ContainsSentinel(string.Join("\n", headingOutline));
                bool skipLinkFocusedFirst = focusTrace.Count > 0 && focusTrace[0].Contains("skip-to-record", StringComparison.Ordinal);
                bool focusTraceContentSafe = !ContainsSentinel(string.Join("\n", focusTrace));
                bool forcedColorsHonored = !scenario.ForcedColors || (forcedColorsActive && reducedMotionActive);

                totalCommandButtons += commandButtonCount;
                if (scenario.ForcedColors)
                {
                    forcedColorsRows++;
                }

                records.Add(new AccessibilityEvidenceRecord(
                    scenario.FixtureId,
                    scenario.Mode,
                    SingleDocumentHeading: singleDocumentHeading,
                    HeadingOrderValid: headingOrderValid,
                    LandmarksPresent: landmarksPresent,
                    TrustOrderPreserved: trustOrderPreserved,
                    AccessibleNameScanPassed: accessibleNameScanPassed,
                    SkipLinkFocusedFirst: skipLinkFocusedFirst,
                    FocusTraceContentSafe: focusTraceContentSafe,
                    BlockedReasonExposed: blockedReasonExposed,
                    CommandButtonCount: commandButtonCount,
                    ForcedColorsHonored: forcedColorsHonored,
                    HeadingOutline: headingOutline));
                snapshots.Add(new AriaSnapshotRecord(scenario.FixtureId, scenario.Mode, ariaSnapshot));
                focusTraces.Add(new FocusOrderRecord(scenario.FixtureId, scenario.Mode, focusTrace));

                string where = $"{scenario.FixtureId}/{scenario.Mode}";
                singleDocumentHeading.ShouldBeTrue($"Expected exactly one <h1> for {where}.");
                headingOrderValid.ShouldBeTrue($"Heading outline did not follow trust order for {where}.");
                landmarksPresent.ShouldBeTrue($"Banner/search/main landmarks or skip link missing for {where}.");
                trustOrderPreserved.ShouldBeTrue($"Trust order not preserved for {where}.");
                accessibleNameScanPassed.ShouldBeTrue($"A poison sentinel leaked into an accessible name/description for {where}.");
                skipLinkFocusedFirst.ShouldBeTrue($"The skip link must be the first focusable control for {where}.");
                focusTraceContentSafe.ShouldBeTrue($"Keyboard focus trace exposed a poison sentinel for {where}.");
                blockedReasonExposed.ShouldBeTrue($"A command button lacked an accessible blocked-reason description for {where}.");
                forcedColorsHonored.ShouldBeTrue($"Forced-colors / reduced-motion context was not honored for {where}.");
            }

            // Suite-level guard against vacuity: prove command buttons and a forced-colors row
            // were actually exercised, so the per-row checks above had something to assert on.
            totalCommandButtons.ShouldBeGreaterThan(0, "No command buttons were rendered across the matrix.");
            forcedColorsRows.ShouldBeGreaterThan(0, "No forced-colors / reduced-motion row was exercised.");
        }
        finally
        {
            WriteEvidence(evidenceRoot, records, snapshots, focusTraces);
        }
    }

    private static AccessibilityScenario[] BuildScenarios(BuyerAcceptanceInvestigationWorkspaceCatalog catalog)
    {
        List<AccessibilityScenario> scenarios =
        [
            .. catalog.List().Select(fixture => new AccessibilityScenario(fixture.FixtureId, "default", 1280, 800)),
        ];

        // Forced-colors + reduced-motion coverage on the dedicated fixture.
        scenarios.Add(new AccessibilityScenario(
            "HighContrast_ReducedMotion_BrowserZoom",
            "forced-colors-reduced-motion",
            1280,
            800,
            ForcedColors: true));

        // 200% browser-zoom equivalents (halved CSS viewport forces a real reflow) prove that
        // labels, trust order, and blocked-action reasons survive magnification.
        scenarios.Add(new AccessibilityScenario("TenantA_Admin_FullTrust", "zoom-200", 640, 400));
        scenarios.Add(new AccessibilityScenario("TenantA_MobileTriage_ReadOnly", "zoom-200", 640, 400));

        return [.. scenarios];
    }

    private static BrowserNewContextOptions CreateContextOptions(AccessibilityScenario scenario)
    {
        BrowserNewContextOptions options = new()
        {
            ViewportSize = new ViewportSize
            {
                Width = scenario.Width,
                Height = scenario.Height,
            },
        };

        if (scenario.ForcedColors)
        {
            options.ForcedColors = ForcedColors.Active;
            options.ReducedMotion = ReducedMotion.Reduce;
        }

        return options;
    }

    private static async Task<string> CollectAccessibleTextAsync(IPage page)
        => await page.EvaluateAsync<string>(
            """
            () => {
                const parts = [];
                document.querySelectorAll('[aria-label]').forEach(e => parts.push(e.getAttribute('aria-label')));
                document.querySelectorAll('[title]').forEach(e => parts.push(e.getAttribute('title')));
                document.querySelectorAll('[aria-describedby],[aria-labelledby]').forEach(e => {
                    ['aria-describedby','aria-labelledby'].forEach(attr => {
                        const v = e.getAttribute(attr);
                        if (v) { v.split(/\s+/).forEach(id => { const t = document.getElementById(id); if (t) { parts.push((t.innerText || t.textContent) || ''); } }); }
                    });
                });
                document.querySelectorAll('h1,h2,h3,h4,h5,h6').forEach(h => parts.push((h.innerText || h.textContent) || ''));
                document.querySelectorAll('[aria-live],[role="status"],[role="alert"]').forEach(e => parts.push((e.innerText || e.textContent) || ''));
                return parts.join('\n');
            }
            """);

    private static async Task<List<string>> CaptureFocusOrderAsync(IPage page)
    {
        const int maxTabStops = 12;
        List<string> trace = [];

        // Reset focus to the document start so the first Tab lands on the skip link.
        await page.EvaluateAsync("() => { if (document.activeElement) { document.activeElement.blur(); } window.focus(); }");

        for (int i = 0; i < maxTabStops; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            string descriptor = await page.EvaluateAsync<string>(
                "() => { const e = document.activeElement; if (!e || e === document.body) { return 'body'; } return [e.tagName.toLowerCase(), e.getAttribute('data-testid') || '', e.getAttribute('role') || '', ((e.innerText || e.textContent) || '').trim().slice(0, 60)].join('|'); }");

            if (descriptor == "body" && trace.Count > 0)
            {
                break;
            }

            trace.Add(descriptor);
        }

        return trace;
    }

    private static bool IsHeadingOrderValid(string[] headingOutline)
    {
        // Exactly one h1 and the expected trust-order ids appear in ascending document order.
        if (headingOutline.Count(entry => entry.StartsWith("h1|", StringComparison.Ordinal)) != 1)
        {
            return false;
        }

        string[] ids =
        [
            .. headingOutline
                .Select(entry => entry.Split('|', 3))
                .Where(parts => parts.Length >= 2 && parts[1].Length > 0)
                .Select(parts => parts[1]),
        ];

        int lastIndex = -1;
        foreach (string expected in ExpectedHeadingOrder)
        {
            int position = Array.IndexOf(ids, expected);
            if (position <= lastIndex)
            {
                return false;
            }

            lastIndex = position;
        }

        return true;
    }

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

    private static void DeleteStaleEvidence(string evidenceRoot)
    {
        foreach (string name in new[]
                 {
                     "accessibility-matrix.json",
                     "aria-snapshots.json",
                     "accessible-name-scan.json",
                     "focus-order-trace.json",
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
        IReadOnlyList<AccessibilityEvidenceRecord> records,
        IReadOnlyList<AriaSnapshotRecord> snapshots,
        IReadOnlyList<FocusOrderRecord> focusTraces)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        File.WriteAllText(
            Path.Combine(evidenceRoot, "accessibility-matrix.json"),
            JsonSerializer.Serialize(records, options));
        File.WriteAllText(
            Path.Combine(evidenceRoot, "aria-snapshots.json"),
            JsonSerializer.Serialize(snapshots, options));
        File.WriteAllText(
            Path.Combine(evidenceRoot, "focus-order-trace.json"),
            JsonSerializer.Serialize(focusTraces, options));

        AccessibleNameScanRecord[] nameScan =
        [
            .. records.Select(record => new AccessibleNameScanRecord(
                record.FixtureId,
                record.Mode,
                record.AccessibleNameScanPassed,
                record.FocusTraceContentSafe)),
        ];
        File.WriteAllText(
            Path.Combine(evidenceRoot, "accessible-name-scan.json"),
            JsonSerializer.Serialize(nameScan, options));

        string[] fixtureIds =
        [
            .. records.Select(record => record.FixtureId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
        ];
        File.WriteAllText(
            Path.Combine(evidenceRoot, "fixture-matrix.json"),
            JsonSerializer.Serialize(fixtureIds, options));

        string Status(bool passed) => passed ? "passed" : "FAILED";
        bool singleHeading = records.All(record => record.SingleDocumentHeading);
        bool headingOrder = records.All(record => record.HeadingOrderValid);
        bool landmarks = records.All(record => record.LandmarksPresent);
        bool trustOrder = records.All(record => record.TrustOrderPreserved);
        bool nameScanPassed = records.All(record => record.AccessibleNameScanPassed);
        bool focusSafe = records.All(record => record.SkipLinkFocusedFirst && record.FocusTraceContentSafe);
        bool blockedReason = records.All(record => record.BlockedReasonExposed);
        bool forcedColors = records.Where(record => record.Mode == "forced-colors-reduced-motion").All(record => record.ForcedColorsHonored);

        File.WriteAllText(
            Path.Combine(evidenceRoot, "evidence-summary.md"),
            $"""
            # Story 3.8B Accessibility Evidence

            Generated by `Hexalith.Conversations.Admin.Web.Tests` (Accessibility lane). All flags
            below are derived from the per-row measurements in `accessibility-matrix.json`, driven
            through a real headless Chromium against the rendered Admin Web host — not hard-coded
            and not DTO-only.

            - Fixtures: {fixtureIds.Length}
            - Scenario rows: {records.Count}
            - Single document heading (one h1): {Status(singleHeading)}
            - Heading outline follows trust order: {Status(headingOrder)}
            - Banner / search / main landmarks + skip link: {Status(landmarks)}
            - Trust order precedes timeline reliance: {Status(trustOrder)}
            - Accessible-name / description sentinel scan: {Status(nameScanPassed)}
            - Keyboard focus order (skip link first, content-safe): {Status(focusSafe)}
            - Blocked-command reasons exposed to assistive technology: {Status(blockedReason)}
            - High contrast + reduced motion honored: {Status(forcedColors)}

            ## Evidence files

            - `accessibility-matrix.json` — per-row accessibility flags and heading outline.
            - `aria-snapshots.json` — captured accessibility tree (the source a screen reader
              renders) for every fixture and mode.
            - `focus-order-trace.json` — keyboard Tab traversal trace per row.
            - `accessible-name-scan.json` — accessible name/description forbidden-sentinel scan.
            - `fixture-matrix.json` — distinct fixtures exercised.
            - `manual-keyboard-screen-reader-notes.md` — keyboard-only walkthrough and
              accessibility-tree (screen-reader) reading transcript, plus the human Narrator/NVDA
              audible confirmation checklist.

            This evidence is scoped to Story 3.8B accessibility (accessibility tree, keyboard,
            and screen-reader safety). Story 3.8A responsive layout / mobile safe-triage and
            Story 3.8C rendered leakage / clipboard / browser-title / telemetry-disclosure
            closure remain owned by those stories.
            """);
    }

    private sealed record AccessibilityScenario(
        string FixtureId,
        string Mode,
        int Width,
        int Height,
        bool ForcedColors = false);

    private sealed record AccessibilityEvidenceRecord(
        string FixtureId,
        string Mode,
        bool SingleDocumentHeading,
        bool HeadingOrderValid,
        bool LandmarksPresent,
        bool TrustOrderPreserved,
        bool AccessibleNameScanPassed,
        bool SkipLinkFocusedFirst,
        bool FocusTraceContentSafe,
        bool BlockedReasonExposed,
        int CommandButtonCount,
        bool ForcedColorsHonored,
        IReadOnlyList<string> HeadingOutline);

    private sealed record AriaSnapshotRecord(string FixtureId, string Mode, string AriaSnapshot);

    private sealed record FocusOrderRecord(string FixtureId, string Mode, IReadOnlyList<string> Trace);

    private sealed record AccessibleNameScanRecord(
        string FixtureId,
        string Mode,
        bool AccessibleNameScanPassed,
        bool FocusTraceContentSafe);
}
