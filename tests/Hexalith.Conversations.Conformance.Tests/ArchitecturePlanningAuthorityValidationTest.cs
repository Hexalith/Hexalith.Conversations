// <copyright file="ArchitecturePlanningAuthorityValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 6.1 — enforces the rebaselined architecture and append-only Epic 6 planning authority.
/// </summary>
/// <remarks>
/// Assertions here are deliberately semantic rather than token-presence. A planning gate that passes on
/// the presence of a noun can be satisfied by prose that inverts the rule it is supposed to protect, so
/// polarity-bearing clauses are asserted verbatim, their negations are asserted absent, and prohibitions
/// are scanned across the whole document rather than inside a single extracted block.
/// </remarks>
public sealed class ArchitecturePlanningAuthorityValidationTest
{
    private const string ArchitectureVersion = "conversations-architecture-2026-08-01-v8";
    private const string BaselineRevision = "f31aa5ada2e37e1ec5f3e4b8e907525b37da863f";

    /// <summary>
    /// The active authority. Amendments are append-only, so the v2 overlay and v3-v7 amendment blocks
    /// coexist in the epic plan. Asserting each block against its
    /// own declared version is what stops a later amendment from being written as a silent rewrite of an
    /// earlier one - which is exactly what happened to v4 on 2026-07-29 and is why v6 republishes rather
    /// than edits.
    /// </summary>
    private const string OverlayVersion = "epic-6-authority-2026-08-01-v8";
    private const string V7OverlayVersion = "epic-6-authority-2026-08-01-v7";
    private const string V7ArchitectureVersion = "conversations-architecture-2026-08-01-v7";
    private const string V6OverlayVersion = "epic-6-authority-2026-07-31-v6";
    private const string V6ArchitectureVersion = "conversations-architecture-2026-07-31-v6";
    private const string V5OverlayVersion = "epic-6-authority-2026-07-28-v5";
    private const string V5ArchitectureVersion = "conversations-architecture-2026-07-28-v5";
    private const string V4OverlayVersion = "epic-6-authority-2026-07-28-v4";
    private const string PreviousOverlayVersion = "epic-6-authority-2026-07-27-v3";
    private const string BaseOverlayVersion = "epic-6-authority-2026-07-15-v2";
    private const string V4ArchitectureVersion = "conversations-architecture-2026-07-28-v4";
    private const string PreviousArchitectureVersion = "conversations-architecture-2026-07-27-v3";
    private const string ModuleTestAppHost = "Hexalith.Conversations.AppHost";
    private const int HistoricalEpicPrefixLength = 55536;
    private const string HistoricalEpicPrefixSha256 = "bd437b802513591c4af299ff0997bb694ced40304e1a178c3d53e95f88f0e8a8";
    private const int HistoricalV2OverlayLength = 14843;
    private const string HistoricalV2OverlaySha256 = "8825a7a2fe21c9d9ae99b3193911bc9ca0186275528235a4f222781d0d463baa";

    private const string ArchitecturePath = "_bmad-output/planning-artifacts/architecture.md";
    private const string EpicsPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md";
    private const string ContextPath = "_bmad-output/implementation-artifacts/epic-6-context.md";
    private const string PrdPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md";
    private const string AddendumPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md";

    /// <summary>
    /// The initiative authority the plan derives from. The tests hard-code denominators such as "20
    /// initiative FRs", so the sources those numbers come from must be pinned too; otherwise the plan can
    /// keep asserting a stale count while the PRD moves underneath it.
    /// </summary>
    private const string PrdSha256 = "884981cefea501e5d6636b8f797581487a5d83cc65f8f4ef53879f3484a140f8";

    private const string AddendumSha256 = "5a0caab66c9eb4b0469d79e77a6c265dd24136e46cc980eeaf58a79cee53e96b";

    /// <summary>Reusable runtime projects the domain module must never own in target state.</summary>
    private static readonly string[] ProhibitedModuleOwnedRuntimeProjects =
    [
        "Hexalith.Conversations.Aspire",
        "Hexalith.Conversations.ServiceDefaults",
        "Hexalith.Conversations.Hosting",
        "Hexalith.Conversations.Host",
        "Hexalith.Conversations.Bootstrap",
        "Hexalith.Conversations.Composition",
    ];

    /// <summary>
    /// Markers that exempt an entire section. Deliberately limited to explicit history: a broader list
    /// would exempt live sections whose titles merely mention migration, including the target-ownership
    /// section, which is exactly where a target-state ownership claim must be caught.
    /// </summary>
    private static readonly string[] SectionSupersessionMarkers =
    [
        "superseded",
        "historical",
    ];

    /// <summary>
    /// Markers that qualify a single line as describing drift or a prohibition rather than target state.
    /// </summary>
    private static readonly string[] LineSupersessionMarkers =
    [
        "superseded",
        "historical",
        "pre-story-6.2",
        "pre-6.2",
        "migration input",
        "not target architecture",
        "never module-owned",
        "own no",
        "removes them",
        "prohibited",
        "consumed, never module-owned",
    ];

    /// <summary>
    /// Terms that make an AppHost reference explicitly test-only or non-production.
    /// Every entry must carry non-shipping semantics on its own. Generic words that
    /// merely co-occur with a test AppHost today ("end-to-end", "local user",
    /// "module-scoped", "focused tests", "ownership and treatment") were removed on
    /// 2026-07-27 (code review pass 2): they let a line such as "Hexalith.Conversations.AppHost
    /// owns the production end-to-end deployment topology" satisfy a guard whose whole
    /// job is to reject exactly that claim. This is the "5% P95" is-a-substring-of
    /// "45% P95" failure Story 6.1's review pass 2 established as a standing lesson.
    /// </summary>
    private static readonly string[] TestAppHostBoundaryMarkers =
    [
        "test apphost",
        "test-boundary",
        "test-only",
        "test harness",
        "test infrastructure",
        "user-test harness",
        "non-packable",
        "non-publishable",
        "non-shipping",
        "not a production",
        "never shipped",
        "not shipped",

        // Line-wrap accommodation, not a semantic marker: the v3 amendment paragraph
        // wraps so that the only physical line naming the AppHost is "...supersedes v2
        // only for the ownership and treatment of `Hexalith.Conversations.AppHost`.",
        // while "non-packable, non-publishable" lands on the following line. Scoped to
        // the exact wrapped phrase so it cannot qualify an arbitrary sentence, and
        // backstopped by ProductionOwnershipAssertions below.
        "only for the ownership and treatment of",
    ];

    /// <summary>
    /// Affirmative production/deployment ownership claims. A line asserting any of these
    /// can never be a qualified test-AppHost line, no matter which boundary marker it also
    /// contains — this is what stops a marker from being smuggled into a sentence that says
    /// the opposite of what the guard is checking for. Phrased to affirm ownership, so the
    /// amendment's own "It is not a production or deployment composition root." is unaffected.
    /// </summary>
    private static readonly string[] ProductionOwnershipAssertions =
    [
        "owns the production",
        "owns production",
        "owns the deployment",
        "owns deployment",
        "production deployment",
        "production topology",
        "deployment topology",
        "production composition root",
        "production runtime",
    ];

    private static readonly string[] ExpectedHistoricalStories =
    [
        "1.1", "1.2", "1.3", "1.4", "1.5",
        "2.1", "2.2", "2.3", "2.4", "2.5", "2.6", "2.7",
        "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7",
        "4.1", "4.2",
        "5.1", "5.2", "5.3",
    ];

    /// <summary>
    /// Dispositions that carry the ownership correction. Row count and non-emptiness cannot detect a
    /// reversed disposition, so the load-bearing rows pin required content.
    /// </summary>
    private static readonly (string Story, string RequiredFinding, string RequiredDisposition)[] LoadBearingDispositions =
    [
        ("2.1", "local host framing", "6.2"),
        ("3.4", "ServiceDefaults", "6.2"),
        ("3.5", "AppHost", "6.2"),
        ("4.1", "prohibited project ownership", "6.5"),
        ("5.3", "SM-C2", "6.6"),
    ];

    /// <summary>Placeholder cell values that must not satisfy a "populated cell" assertion.</summary>
    private static readonly string[] PlaceholderCellValues = ["-", "--", "n/a", "na", "tbd", "todo", "?", "…"];

    [Fact]
    public void ArchitectureFrontmatterShouldBindCanonicalCurrentAuthority()
    {
        string frontmatter = ExtractFrontmatter(ReadRepositoryFile(ArchitecturePath));

        AssertYamlScalar(frontmatter, "status", "authority-correction-only-not-ready");
        AssertYamlScalar(frontmatter, "rebaselinedAt", "2026-08-01");
        AssertYamlScalar(frontmatter, "authorityVersion", ArchitectureVersion);
        AssertYamlScalar(frontmatter, "baselineRevision", BaselineRevision);

        frontmatter.ShouldContain(PrdPath);
        frontmatter.ShouldContain(AddendumPath);
        frontmatter.ShouldContain("sprint-change-proposal-2026-07-15.md");
        frontmatter.ShouldContain("sprint-change-proposal-2026-07-15-submodule-promotion-completion-gate.md");
        frontmatter.ShouldContain("sprint-change-proposal-2026-07-27.md");
        frontmatter.ShouldContain("sprint-change-proposal-2026-07-28.md");
        frontmatter.ShouldContain("sprint-change-proposal-2026-08-01.md");
        frontmatter.ShouldContain("sprint-change-proposal-2026-08-01-implementation-readiness-authority-correction.md");
        frontmatter.ShouldContain("epic-6-current-execution-view-v1.md");

        // Provenance must be complete: an entire binding architecture section derives from the projection
        // read-store proposal and ADR 0003, so omitting them from correctionAuthority understates authority.
        frontmatter.ShouldContain("sprint-change-proposal-2026-07-15-projection-read-store-population.md");
        frontmatter.ShouldContain("0003-projection-read-store-population-proof.md");

        // A completed-workflow claim must not coexist unqualified with a corrective-only status.
        frontmatter.ShouldNotContain("\nstatus: 'complete'");
        Regex.IsMatch(frontmatter, @"^lastStep:", RegexOptions.Multiline)
            .ShouldBeFalse("Historical workflow completion metadata must be labeled historical, not presented as current.");
        Regex.IsMatch(frontmatter, @"^completedAt:", RegexOptions.Multiline)
            .ShouldBeFalse("Historical workflow completion metadata must be labeled historical, not presented as current.");
    }

    [Fact]
    public void InitiativeAuthoritySourcesShouldRemainPinned()
    {
        ComputeSha256(File.ReadAllBytes(RepositoryPath(PrdPath)))
            .ShouldBe(PrdSha256, "The initiative PRD is the source of the asserted denominators and must be pinned.");
        ComputeSha256(File.ReadAllBytes(RepositoryPath(AddendumPath)))
            .ShouldBe(AddendumSha256, "The initiative addendum is initiative authority and must be pinned.");
    }

    [Fact]
    public void ArchitectureShouldStateScopeAndPreservationDenominators()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string section = ExtractSection(architecture, "### Scope And Preservation Denominators", "### Target Ownership And Current Migration Input");
        string flat = NormalizeWhitespace(section);

        // AC1's subject is this document. Without these assertions the denominators can be rewritten here
        // while the overlay and context still read correctly.
        flat.ShouldContain("20 initiative requirements");
        flat.ShouldContain("104 `Feature-FR`s");
        flat.ShouldContain("77 `Feature-NFR`s");
        flat.ShouldContain("52 UX decisions");
        flat.ShouldContain("13,289 LOC");
        flat.ShouldContain("**FR-16 alone is deferred and non-activated**");
        flat.ShouldContain("not renegotiable");

        flat.ShouldNotContain("baseline is renegotiable");
        flat.ShouldNotContain("may be revised at any time");
    }

    [Fact]
    public void ArchitectureRegistersExactInitiativeLandingZonesAndDeferredFrSixteen()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string section = ExtractSection(architecture, "### Initiative Landing-Zone Register", "### Open-Question Disposition Register");
        string[] rows = MarkdownDataRows(section, "FR-");

        rows.Length.ShouldBe(7);
        rows.Select(GetFirstTableCell).ShouldBe(["FR-10", "FR-11", "FR-12", "FR-13", "FR-14", "FR-15", "FR-16"]);
        rows.ShouldAllBe(row => TableCells(row).Length == 4);
        AssertPopulatedCells(rows, skip: 1);

        // Assert the intended cell, not the whole row: an owner name sitting in the responsibility cell
        // must not satisfy an owner assertion.
        AssertCell(rows, "FR-10", 2, "Hexalith.EventStore.ServiceDefaults");
        AssertCell(rows, "FR-10", 2, "UseEventStoreDomainService");
        AssertCell(rows, "FR-11", 2, "Hexalith.Commons.TenantAccess");
        AssertCell(rows, "FR-12", 2, "Hexalith.Commons.Http");
        AssertCell(rows, "FR-13", 2, "Hexalith.EventStore.Aspire");
        AssertCell(rows, "FR-14", 2, "Hexalith.Commons.Serialization");
        AssertCell(rows, "FR-15", 2, "Hexalith.Commons.Diagnostics");
        AssertCell(rows, "FR-16", 1, "deferred-non-activated");

        // OQ-1 forbids a Conversations facade, so no register row may name the module as a landing zone.
        foreach (string row in rows)
        {
            AssertOwnerNamesNoModuleSurface(TableCells(row)[2], GetFirstTableCell(row), "Architecture register");
        }
    }

    [Fact]
    public void ArchitectureShouldResolveEveryOpenQuestionExactlyOnce()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string section = ExtractSection(architecture, "### Open-Question Disposition Register", "### SM-C2 Versioned Hot-Path Inventory And Gate");
        string[] rows = MarkdownDataRows(section, "OQ-");

        rows.Length.ShouldBe(5);
        rows.Select(GetFirstTableCell).ShouldBe(["OQ-1", "OQ-2", "OQ-3", "OQ-4", "OQ-5"]);
        rows.ShouldAllBe(row => TableCells(row).Length == 4);
        AssertPopulatedCells(rows, skip: 1);
        rows.ShouldAllBe(row => Regex.IsMatch(TableCells(row)[1], @"^resolved-\d{4}-\d{2}-\d{2}$"));

        // Bind each decision to its required content, in the decision cell.
        AssertCell(rows, "OQ-1", 2, "no new shared module or Conversations facade is authorized");
        AssertCell(rows, "OQ-2", 2, ">=40%");
        AssertCell(rows, "OQ-3", 2, "stay domain-owned");
        AssertCell(rows, "OQ-4", 2, "FR-16 is deferred");

        // "5% P95 regression" is a substring of "45% P95 regression"; anchor the number so the only
        // numeric guard on the performance gate cannot be inflated silently.
        string oqFive = TableCells(rows.Single(row => GetFirstTableCell(row) == "OQ-5"))[2];
        Regex.IsMatch(oqFive, @"no more than 5% P95 regression", RegexOptions.CultureInvariant)
            .ShouldBeTrue("OQ-5 must state the 5% P95 bound exactly.");
        Regex.IsMatch(oqFive, @"\b(?!5%)\d+% P95", RegexOptions.CultureInvariant)
            .ShouldBeFalse($"OQ-5 must not state any P95 bound other than 5%: '{oqFive}'.");

        // The document claims exactly one authoritative row per OQ. Enforce that across the whole file,
        // not only inside the extracted register.
        foreach (string id in new[] { "OQ-1", "OQ-2", "OQ-3", "OQ-4", "OQ-5" })
        {
            MarkdownDataRows(architecture, id)
                .Count(row => GetFirstTableCell(row) == id)
                .ShouldBe(1, $"There must be exactly one authoritative table row for {id} in the whole document.");
        }
    }

    [Fact]
    public void SmCTwoShouldFreezeNonemptyInventoryAndOneToOneComparablePostResults()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string section = ExtractSection(architecture, "### SM-C2 Versioned Hot-Path Inventory And Gate", "### Still-Binding Domain And Runtime Decisions");
        string flat = NormalizeWhitespace(section);
        string[] rows = MarkdownDataRows(section, "HP-");

        flat.ShouldContain("sm-c2-hot-path-inventory-v1");
        rows.Select(GetFirstTableCell).ShouldBe(["HP-CREATE", "HP-APPEND", "HP-LIST", "HP-OPEN"]);
        rows.Length.ShouldBeGreaterThan(0);
        rows.ShouldAllBe(row => TableCells(row).Length == 5);

        // Every cell, not only the post-disposition cell: an inventory row with no classification,
        // operation, or envelope evidence would otherwise satisfy the only gate backing AC2.
        AssertPopulatedCells(rows, skip: 1);

        // Polarity-bearing clauses. The anti-cherry-pick property is the point of the freeze, so assert
        // the prohibitions verbatim and assert their inversions are absent.
        flat.ShouldContain("frozen by Story 6.1 before baseline capture");
        flat.ShouldContain("one baseline result for every row");
        flat.ShouldContain("exactly one disposition and result for every baseline row");
        flat.ShouldContain("rows cannot be selected after measurement");
        flat.ShouldContain("cannot be substituted for warm rows");
        flat.ShouldContain("post P95 <= 1.05 x baseline P95");
        flat.ShouldContain("blocks completion");

        flat.ShouldNotContain("may be revised at any time");
        flat.ShouldNotContain("may be selected after measurement");
        flat.ShouldNotContain("may be substituted for warm rows");
        flat.ShouldNotContain("is acceptable");

        foreach (string semantic in new[] { "workload and data", "concurrency", "environment and runtime", "benchmark tool/version", "warm/cold classification", "repetition policy", "raw-result processing", "measured commit" })
        {
            flat.ShouldContain(semantic);
        }
    }

    [Fact]
    public void TargetTreeAndReadinessShouldBeCorrectiveAndPlatformOwned()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string targetTreeSection = ExtractSection(architecture, "### Corrected Target Directory Structure", "### Historical May 14 Directory Structure (Superseded)");
        string targetTree = ExtractFirstFencedBlock(targetTreeSection);
        string readiness = ExtractSection(architecture, "### Corrective Readiness", "## Project Context Analysis");
        string flatReadiness = NormalizeWhitespace(readiness);

        targetTree.ShouldContain("Hexalith.Conversations.Contracts/");
        targetTree.ShouldContain("Hexalith.Conversations.Server/");
        targetTree.ShouldContain("Hexalith.Conversations.AppHost/");
        targetTree.ShouldContain("Hexalith.Conversations.AppHost.Tests/");
        targetTree.ShouldContain("non-packable/non-publishable module user-test harness");

        foreach (string prohibited in ProhibitedModuleOwnedRuntimeProjects)
        {
            targetTree.ShouldNotContain(prohibited, Case.Insensitive);
        }

        // The target-tree section must not hide a second fenced tree that reintroduces ownership.
        CountOccurrences(targetTreeSection, "```").ShouldBe(2, "The corrected target tree section must contain exactly one fenced block.");

        string flatArchitecture = NormalizeWhitespace(architecture);
        flatArchitecture.ShouldContain("`IsPackable=false`, `IsPublishable=false`");
        flatArchitecture.ShouldContain("remains pre-Story-6.2 drift and is removed");
        flatArchitecture.ShouldContain("is not a production/deployment composition root");
        flatArchitecture.ShouldContain("Platform deployment owns production topology and composition");
        flatReadiness.ShouldContain("AUTHORITY CORRECTION ONLY — NOT READY");
        flatReadiness.ShouldContain("completed spine is `6.1 -> 6.7 -> 6.2`");
        flatReadiness.ShouldContain("Every remaining story is held");
        flatReadiness.ShouldContain("regardless of its file-lifecycle status");
        flatReadiness.ShouldContain("independent implementation-readiness assessment returns `READY`");

        // The v4 amendment must state the derivation rule where architecture is read, not only in the epic
        // plan. Without this, architecture could keep describing hand-authored records as acceptable.
        string finalRecord = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(architecture, "### 2026-07-28 Mechanical Final-Record Amendment"));
        finalRecord.ShouldContain($"Architecture version `{V4ArchitectureVersion}` supersedes v3");
        finalRecord.ShouldContain("derived outputs of a generator");
        finalRecord.ShouldContain("Derivation sources are exactly four");
        finalRecord.ShouldContain("goes red rather than stale");
        finalRecord.ShouldNotContain("may be authored as prose");

        // The v5 amendment must state the tiering rule and both prohibitions where architecture is read.
        // Asserting the prohibitions verbatim is the point: an amendment that merely names two tiers can be
        // satisfied by publicising a Server type or softening an assertion, which are the two ways this
        // decision degrades while still appearing closed.
        string oracleTiering = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(architecture, "### 2026-07-28 Conformance Oracle Tiering Amendment"));
        oracleTiering.ShouldContain($"Architecture version `{V5ArchitectureVersion}` supersedes v4");
        oracleTiering.ShouldContain("two declared tiers");
        oracleTiering.ShouldContain("references no non-packable module assembly");
        oracleTiering.ShouldContain("asserted from the resolved compile surface");
        oracleTiering.ShouldContain("is **prohibited**");
        oracleTiering.ShouldContain("is a **conformance failure**");
        oracleTiering.ShouldContain("declared and correct, not a defect scheduled for removal");
        oracleTiering.ShouldContain("frozen FR-20 denominator is unchanged");

        // The v6 amendment must state the amended performance rule where architecture is read, and must
        // carry the non-relaxation of the correctness requirement with it. Architecture describing the old
        // unconditional +-5% rule while the epic plan describes the amended one is the drift this asserts
        // against.
        string smC2Threshold = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(architecture, "### 2026-07-31 SM-C2 Threshold And Record-Contract Amendment"));
        smC2Threshold.ShouldContain($"Architecture version `{V6ArchitectureVersion}` supersedes v5");
        smC2Threshold.ShouldContain("approved-cost ceiling");
        smC2Threshold.ShouldContain("a further regression still goes red");
        smC2Threshold.ShouldContain("authorizes no repair-on-read");
        smC2Threshold.ShouldContain("source-tree dirt blocked outside record outputs");

        string projectionProofLifecycle = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(architecture, "### 2026-08-01 Projection-Proof Evidence-Lifecycle Amendment"));
        projectionProofLifecycle.ShouldContain($"Architecture version `{V7ArchitectureVersion}` supersedes v6");
        projectionProofLifecycle.ShouldContain("immutable point-in-time evidence");
        projectionProofLifecycle.ShouldContain("resolves root-owned blobs from that candidate");
        projectionProofLifecycle.ShouldContain("does not substitute the current `HEAD`");
        projectionProofLifecycle.ShouldContain("exactly one approved current head");
        projectionProofLifecycle.ShouldContain("PROJECTION_PROOF_SUPERSESSION_REQUIRED");
        projectionProofLifecycle.ShouldContain("Story 6.2 remains `done`");
        projectionProofLifecycle.ShouldContain("Successor artifacts and validator changes remain Story 6.12 implementation work");

        string authorityCorrection = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(architecture, "### 2026-08-01 Implementation Readiness Authority Correction"));
        authorityCorrection.ShouldContain($"Architecture version `{ArchitectureVersion}` supersedes v7");
        authorityCorrection.ShouldContain("complete effective Story 6.1-6.12 execution contract");
        authorityCorrection.ShouldContain("AUTHORITY CORRECTION ONLY — NOT READY");
        authorityCorrection.ShouldContain("HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN");
        authorityCorrection.ShouldContain("v6 ceiling/disclosure disposition is historical completion context only");
        authorityCorrection.ShouldContain("assessor is never instructed or modified to return `READY`");
        authorityCorrection.ShouldContain("does not implement those story deliverables");

        // Polarity guards: the amendment must not be rewritten into permission for the two failure modes.
        oracleTiering.ShouldNotContain("may be widened");
        oracleTiering.ShouldNotContain("may be weakened");
        oracleTiering.ShouldNotContain("the reference must be removed");
    }

    [Fact]
    public void CanonicalDomainHostPairShouldBeTaughtInCurrentAuthority()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);

        // Scope the positive guidance to the current rebaseline: superseded starter text must not be able
        // to satisfy a current-authority requirement.
        string rebaseline = ExtractSection(architecture, "## 2026-07-15 Authority Rebaseline", "## Project Context Analysis");
        string flat = NormalizeWhitespace(rebaseline);

        flat.ShouldContain("builder.AddEventStoreDomainService");
        flat.ShouldContain("app.UseEventStoreDomainService");
        flat.ShouldContain("must never teach direct `MapEventStoreDomainService()` use");

        // The document must not simultaneously prohibit and demonstrate the lower-level mapper.
        Regex.Matches(architecture, @"MapEventStoreDomainService", RegexOptions.CultureInvariant)
            .Count
            .ShouldBe(1, "The lower-level mapper may appear only in its prohibition sentence.");
    }

    [Fact]
    public void NoTargetStateOwnershipOrUnqualifiedReadinessShouldSurviveAnywhereInTheDocument()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string[] lines = architecture.Replace("\r\n", "\n").Split('\n');
        string currentHeading = string.Empty;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                currentHeading = line;
            }

            bool inSupersededSection = IsSupersededSectionHeading(currentHeading);
            bool lineIsQualified = IsQualifiedLine(line);

            if (line.Contains(ModuleTestAppHost, StringComparison.OrdinalIgnoreCase))
            {
                (inSupersededSection || IsQualifiedTestAppHostLine(line)).ShouldBeTrue(
                    $"'{ModuleTestAppHost}' at line {index + 1} is not explicitly constrained to a non-shipping test boundary: '{line.Trim()}'.");
            }

            foreach (string prohibited in ProhibitedModuleOwnedRuntimeProjects)
            {
                if (line.Contains(prohibited, StringComparison.OrdinalIgnoreCase))
                {
                    (inSupersededSection || lineIsQualified).ShouldBeTrue(
                        $"'{prohibited}' at line {index + 1} presents module-owned hosting outside a superseded or migration-input span: '{line.Trim()}'.");
                }
            }

            if (line.Contains("READY FOR IMPLEMENTATION", StringComparison.Ordinal)
                && !line.Contains("READY FOR CORRECTIVE IMPLEMENTATION ONLY", StringComparison.Ordinal))
            {
                (inSupersededSection || lineIsQualified).ShouldBeTrue(
                    $"An unqualified implementation-readiness verdict at line {index + 1} contradicts corrective-only authority: '{line.Trim()}'.");
            }
        }
    }

    [Fact]
    public void StillBindingReplayProjectionParticipantIdempotencyAndLegalRulesShouldRemain()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);

        // Bound the extraction to the section's own content. Using a distant heading as the end marker lets
        // any later-inserted section satisfy these assertions on overlapping topics.
        string section = ExtractSectionUntilNextHeadingOfSameLevel(architecture, "### Still-Binding Domain And Runtime Decisions");
        string flat = NormalizeWhitespace(section);

        flat.ShouldContain("Mixed-version streams");
        flat.ShouldContain("readers/upcasters");
        flat.ShouldContain("EventStore history has precedence");
        flat.ShouldContain("quarantined");
        flat.ShouldContain("rebuild starts from EventStore");
        flat.ShouldContain("Parties validation fails closed");
        flat.ShouldContain("policy-defined non-personal hydration placeholder");
        flat.ShouldContain("same key with a different payload");
        flat.ShouldContain("unknown client/provider outcome");
        flat.ShouldContain("legal-policy mechanisms");

        flat.ShouldNotContain("no longer fails closed");
        flat.ShouldNotContain("may be skipped");
    }

    [Fact]
    public void EpicPlanShouldPreserveHistoricalPrefixAndContainExactDispositionRows()
    {
        byte[] epicBytes = File.ReadAllBytes(RepositoryPath(EpicsPath));
        epicBytes.Length.ShouldBeGreaterThan(HistoricalEpicPrefixLength + HistoricalV2OverlayLength);
        ComputeSha256(epicBytes.AsSpan(0, HistoricalEpicPrefixLength)).ShouldBe(HistoricalEpicPrefixSha256);
        ComputeSha256(epicBytes.AsSpan(HistoricalEpicPrefixLength, HistoricalV2OverlayLength))
            .ShouldBe(HistoricalV2OverlaySha256, "The approved v2 overlay must remain byte-identical while v3 is appended.");

        // The frozen boundary must be anchored to the baseline commit, not only to this document's own
        // self-declared constants; otherwise a wrong freeze stays self-consistently green forever.
        if (TryReadGitBlobBytes(BaselineRevision, EpicsPath, out byte[] baselineBytes))
        {
            baselineBytes.Length.ShouldBe(HistoricalEpicPrefixLength, "The frozen prefix length must equal the epic plan at the work baseline.");
            ComputeSha256(baselineBytes).ShouldBe(HistoricalEpicPrefixSha256, "The frozen prefix must equal the epic plan at the work baseline.");
        }

        // Append-only is a byte property: the historical prefix contains multi-byte characters, so a
        // character index into the decoded document cannot be compared against the frozen prefix length.
        string appended = Encoding.UTF8.GetString(epicBytes.AsSpan(HistoricalEpicPrefixLength));
        string baseOverlayEnd = $"<!-- EPIC-6-AUTHORITY-OVERLAY:END version={BaseOverlayVersion} -->";
        string amendmentBegin = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN version={PreviousOverlayVersion} supersedes={BaseOverlayVersion} -->";
        string amendmentEnd = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:END version={PreviousOverlayVersion} -->";
        string v4AmendmentBegin = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:BEGIN version={V4OverlayVersion} supersedes={PreviousOverlayVersion} -->";
        string v4AmendmentEnd = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:END version={V4OverlayVersion} -->";
        string v5AmendmentBegin = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:BEGIN version={V5OverlayVersion} supersedes={V4OverlayVersion} -->";
        string v5AmendmentEnd = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:END version={V5OverlayVersion} -->";
        string v6AmendmentBegin = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:BEGIN version={V6OverlayVersion} supersedes={V5OverlayVersion} -->";
        string v6AmendmentEnd = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:END version={V6OverlayVersion} -->";
        string activeAmendmentBegin = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:BEGIN version={V7OverlayVersion} supersedes={V6OverlayVersion} -->";
        string activeAmendmentEnd = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:END version={V7OverlayVersion} -->";
        string v8AmendmentBegin = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN version={OverlayVersion} supersedes={V7OverlayVersion} -->";
        string v8AmendmentEnd = $"<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:END version={OverlayVersion} -->";
        appended.ShouldStartWith(
            $"\n<!-- EPIC-6-AUTHORITY-OVERLAY:BEGIN version={BaseOverlayVersion} prefix-bytes={HistoricalEpicPrefixLength} prefix-sha256={HistoricalEpicPrefixSha256}",
            Case.Sensitive,
            "The Epic 6 overlay must start immediately after the frozen historical prefix and declare the boundary it appends to.");
        int baseOverlayEndIndex = appended.IndexOf(baseOverlayEnd, StringComparison.Ordinal);
        baseOverlayEndIndex.ShouldBeGreaterThan(0, "The v2 authority must remain present before its v3 amendment.");
        string amendment = appended[(baseOverlayEndIndex + baseOverlayEnd.Length)..].TrimStart('\r', '\n');
        amendment.ShouldStartWith(amendmentBegin, Case.Sensitive);

        // Each amendment appends after the previous one closes. Asserting the chain rather than only the
        // final marker is what prevents a new amendment from being spliced inside, or in place of, an
        // earlier block that is supposed to be immutable.
        int amendmentEndIndex = amendment.IndexOf(amendmentEnd, StringComparison.Ordinal);
        amendmentEndIndex.ShouldBeGreaterThan(0, "The v3 amendment must remain present and closed before the v4 amendment.");
        string v4Amendment = amendment[(amendmentEndIndex + amendmentEnd.Length)..].TrimStart('\r', '\n');
        v4Amendment.ShouldStartWith(v4AmendmentBegin, Case.Sensitive, "The v4 amendment must be appended immediately after the v3 amendment closes.");

        int v4AmendmentEndIndex = v4Amendment.IndexOf(v4AmendmentEnd, StringComparison.Ordinal);
        v4AmendmentEndIndex.ShouldBeGreaterThan(0, "The v4 amendment must remain present and closed before the v5 amendment.");
        string v4Block = v4Amendment[..(v4AmendmentEndIndex + v4AmendmentEnd.Length)];
        string v5Amendment = v4Amendment[(v4AmendmentEndIndex + v4AmendmentEnd.Length)..].TrimStart('\r', '\n');
        v5Amendment.ShouldStartWith(v5AmendmentBegin, Case.Sensitive, "The v5 amendment must be appended immediately after the v4 amendment closes.");

        int v5AmendmentEndIndex = v5Amendment.IndexOf(v5AmendmentEnd, StringComparison.Ordinal);
        v5AmendmentEndIndex.ShouldBeGreaterThan(0, "The v5 amendment must remain present and closed before the v6 amendment.");
        string v5Block = v5Amendment[..(v5AmendmentEndIndex + v5AmendmentEnd.Length)];
        string v6Amendment = v5Amendment[(v5AmendmentEndIndex + v5AmendmentEnd.Length)..].TrimStart('\r', '\n');
        v6Amendment.ShouldStartWith(v6AmendmentBegin, Case.Sensitive, "The v6 amendment must be appended immediately after the v5 amendment closes.");

        int v6AmendmentEndIndex = v6Amendment.IndexOf(v6AmendmentEnd, StringComparison.Ordinal);
        v6AmendmentEndIndex.ShouldBeGreaterThan(0, "The v6 amendment must remain present and closed before the v7 amendment.");
        string v6Block = v6Amendment[..(v6AmendmentEndIndex + v6AmendmentEnd.Length)];
        string activeAmendment = v6Amendment[(v6AmendmentEndIndex + v6AmendmentEnd.Length)..].TrimStart('\r', '\n');
        activeAmendment.ShouldStartWith(activeAmendmentBegin, Case.Sensitive, "The v7 amendment must be appended immediately after the v6 amendment closes.");
        int activeAmendmentEndIndex = activeAmendment.IndexOf(activeAmendmentEnd, StringComparison.Ordinal);
        activeAmendmentEndIndex.ShouldBeGreaterThan(0, "The v7 amendment must remain present and closed before the v8 amendment.");
        string activeBlock = activeAmendment[..(activeAmendmentEndIndex + activeAmendmentEnd.Length)];
        string v8Amendment = activeAmendment[(activeAmendmentEndIndex + activeAmendmentEnd.Length)..].TrimStart('\r', '\n');
        v8Amendment.ShouldStartWith(v8AmendmentBegin, Case.Sensitive, "The v8 amendment must be appended immediately after the v7 amendment closes.");
        int v8AmendmentEndIndex = v8Amendment.IndexOf(v8AmendmentEnd, StringComparison.Ordinal);
        v8AmendmentEndIndex.ShouldBeGreaterThan(0, "The immutable v8 amendment must remain present and closed before successor authority.");
        string v8Block = v8Amendment[..(v8AmendmentEndIndex + v8AmendmentEnd.Length)];
        v8Block.TrimEnd().ShouldEndWith(v8AmendmentEnd);

        string epics = Encoding.UTF8.GetString(epicBytes);
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY:BEGIN").ShouldBe(1, "Exactly one append-only authority overlay may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY:END").ShouldBe(1, "Exactly one append-only authority overlay may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN").ShouldBe(1, "Exactly one v3 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:END").ShouldBe(1, "Exactly one v3 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:BEGIN").ShouldBe(1, "Exactly one v4 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:END").ShouldBe(1, "Exactly one v4 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:BEGIN").ShouldBe(1, "Exactly one v5 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:END").ShouldBe(1, "Exactly one v5 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:BEGIN").ShouldBe(1, "Exactly one v6 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:END").ShouldBe(1, "Exactly one v6 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:BEGIN").ShouldBe(1, "Exactly one v7 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:END").ShouldBe(1, "Exactly one v7 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN").ShouldBe(1, "Exactly one v8 authority amendment may exist.");
        CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:END").ShouldBe(1, "Exactly one v8 authority amendment may exist.");

        AssertSingleOccurrence(epics, "### Exact Historical Story Dispositions");
        string dispositionSection = ExtractSection(epics, "### Exact Historical Story Dispositions", "### Corrective Initiative-FR Coverage");
        string[] rows = MarkdownDataRows(dispositionSection, string.Empty);

        rows.Length.ShouldBe(24);
        rows.Select(GetFirstTableCell).ShouldBe(ExpectedHistoricalStories);
        rows.ShouldAllBe(row => TableCells(row).Length == 3);
        AssertPopulatedCells(rows, skip: 1);

        // Row keys and non-emptiness cannot detect a reversed disposition, so pin the rows that carry the
        // ownership correction and forbid a retain-as-target reading.
        foreach ((string story, string requiredFinding, string requiredDisposition) in LoadBearingDispositions)
        {
            string[] cells = TableCells(rows.Single(row => GetFirstTableCell(row) == story));
            cells[1].ShouldContain(requiredFinding, Case.Insensitive, $"Disposition row {story} must retain its recorded finding.");
            cells[2].ShouldContain(requiredDisposition, Case.Insensitive, $"Disposition row {story} must retain its corrective landing.");
            cells[2].ShouldNotContain("Retain the Conversations-owned", Case.Insensitive, $"Disposition row {story} must not reinstate module-owned hosting.");
            cells[2].ShouldNotContain("as target architecture", Case.Insensitive, $"Disposition row {story} must not reinstate module-owned hosting.");
        }

        string dispositionAmendment = ExtractSection(amendment, "### Superseding Story Dispositions", "### Story 6.2 Corrected Acceptance");
        string[] amendmentRows = MarkdownDataRows(dispositionAmendment, "6.");
        amendmentRows.Select(GetFirstTableCell).ShouldBe(["6.1", "6.2", "6.3", "6.4", "6.5", "6.6", "6.7"]);
        AssertCell(amendmentRows, "6.2", 1, "Retain and constrain the existing AppHost as test-only");
        AssertCell(amendmentRows, "6.2", 1, "Do not select or modify FrontComposer.AppHost or EventStore.AppHost");
        AssertCell(amendmentRows, "6.5", 1, "non-shipping module test AppHost");
        AssertCell(amendmentRows, "6.7", 1, "No change");

        // The v4 amendment adds a story, so it must carry its own complete disposition table. Extracting it
        // from the v4 block rather than from the document keeps the v3 table's row set immutable.
        string v4Dispositions = ExtractSection(v4Block, "### Superseding Story Dispositions", "### Binding Dependency Order");
        string[] v4Rows = MarkdownDataRows(v4Dispositions, "6.");
        v4Rows.Select(GetFirstTableCell).ShouldBe(["6.1", "6.2", "6.3", "6.4", "6.5", "6.6", "6.7", "6.8"]);
        v4Rows.ShouldAllBe(row => TableCells(row).Length == 2);
        AssertPopulatedCells(v4Rows, skip: 1);
        AssertCell(v4Rows, "6.2", 1, "completes under the pre-6.8 process");
        AssertCell(v4Rows, "6.6", 1, "consumes generated records");
        AssertCell(v4Rows, "6.7", 1, "No change");

        // The added story must be defined where the dev agent reads it, and its record-integrity rules must
        // survive as text rather than as a row that merely names it.
        AssertSingleOccurrence(v4Block, "### Story 6.8:");
        string storySection = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(v4Block, "### Story 6.8:"));
        storySection.ShouldContain("derived from the four sources");
        storySection.ShouldContain("blocks as stale");
        storySection.ShouldContain("inside a root-declared submodule blocks");
        storySection.ShouldContain("red rather than stale");
        storySection.ShouldContain("cannot report a pass having derived nothing");
        storySection.ShouldNotContain("counts may be carried forward");

        // The v5 amendment adds Story 6.9 and amends two earlier stories, so it carries its own complete
        // disposition table extending the row set to 6.9. It is extracted from the v5 block by name, not
        // from "the last amendment": binding v5's assertions to whichever amendment happens to be newest is
        // how a later amendment silently inherits, or drops, an earlier one's obligations.
        string activeDispositions = ExtractSection(v5Block, "### Superseding Story Dispositions", "### Story 6.3 Amended Acceptance");
        string[] activeRows = MarkdownDataRows(activeDispositions, "6.");
        activeRows.Select(GetFirstTableCell).ShouldBe(["6.1", "6.2", "6.3", "6.4", "6.5", "6.6", "6.7", "6.8", "6.9"]);
        activeRows.ShouldAllBe(row => TableCells(row).Length == 2);
        AssertPopulatedCells(activeRows, skip: 1);
        AssertCell(activeRows, "6.3", 1, "oracle tiering");
        AssertCell(activeRows, "6.6", 1, "consumes the tiering decision");
        AssertCell(activeRows, "6.9", 1, "New corrective story");

        // Story 6.9's two prohibitions must survive as text in the block the dev agent reads. A row that
        // merely names the story cannot stop the decision from being "closed" by widening the public
        // contract or weakening an assertion, which are the only two ways it silently fails.
        AssertSingleOccurrence(v5Block, "### Story 6.9:");
        string tieringStory = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(v5Block, "### Story 6.9:"));
        tieringStory.ShouldContain("Widening the public contract is not an available resolution");
        tieringStory.ShouldContain("assertion strength preserved");
        tieringStory.ShouldContain("resolved compile surface");
        tieringStory.ShouldContain("monotonic");
        tieringStory.ShouldContain("derived from a machine-readable result artifact, never transcribed");
        tieringStory.ShouldContain("Frozen denominator membership is unchanged");
        tieringStory.ShouldContain("The v1 artifacts are not edited");

        // Polarity guards for the two degradation paths, plus the alternate outcome that keeps the decision
        // honest: removing the reference is a permitted result, not a forbidden one.
        tieringStory.ShouldNotContain("may be weakened");
        tieringStory.ShouldNotContain("may widen the public contract");
        tieringStory.ShouldContain("valid and successful result");

        // The v6 amendment carries its own narrow disposition table naming only the stories it touches, so
        // an amendment that quietly re-dispositioned an untouched story would have to say so.
        string v6Dispositions = ExtractSection(v6Block, "### Story Dispositions Amended By This Overlay", "### Binding Dependency Order");
        string[] v6Rows = MarkdownDataRows(v6Dispositions, "6.");
        v6Rows.Select(GetFirstTableCell).ShouldBe(["6.2", "6.6", "6.8", "6.11"]);
        v6Rows.ShouldAllBe(row => TableCells(row).Length == 2);
        AssertPopulatedCells(v6Rows, skip: 1);
        AssertCell(v6Rows, "6.2", 1, "AC1's pass rule is amended");
        AssertCell(v6Rows, "6.11", 1, "New.");

        string v7Dispositions = ExtractSection(activeBlock, "### Story Dispositions Amended By This Overlay", "### Binding Dependency Order");
        string[] v7Rows = MarkdownDataRows(v7Dispositions, "6.");
        v7Rows.Select(GetFirstTableCell).ShouldBe(["6.2", "6.3", "6.6", "6.12"]);
        v7Rows.ShouldAllBe(row => TableCells(row).Length == 2);
        AssertPopulatedCells(v7Rows, skip: 1);
        AssertCell(v7Rows, "6.2", 1, "No status, acceptance, record, or evidence change");
        AssertCell(v7Rows, "6.12", 1, "New.");

        AssertSingleOccurrence(activeBlock, "### Story 6.12:");
        string proofLifecycleStory = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(activeBlock, "### Story 6.12:"));
        proofLifecycleStory.ShouldContain("Story 6.2 remains `done`");
        proofLifecycleStory.ShouldContain("reads root-owned blobs from umbrella candidate");
        proofLifecycleStory.ShouldContain("does not prohibit later unrelated root gitlink");
        proofLifecycleStory.ShouldContain("ADR 0004");
        proofLifecycleStory.ShouldContain("projection-read-store-population-proof-v3");
        proofLifecycleStory.ShouldContain("PROJECTION_PROOF_SUPERSESSION_REQUIRED");
        proofLifecycleStory.ShouldContain("does not modify production source");
        proofLifecycleStory.ShouldContain("does not weaken or delete a projection assertion");

        string v8Dispositions = ExtractSection(v8Amendment, "### Current Story Dispositions", "### Topological Dependency Plan");
        string[] v8Rows = MarkdownDataRows(v8Dispositions, "6.");
        v8Rows.Select(GetFirstTableCell).ShouldBe(Enumerable.Range(1, 12).Select(story => $"6.{story}").ToArray());
        v8Rows.ShouldAllBe(row => TableCells(row).Length == 3);
        AssertPopulatedCells(v8Rows, skip: 1);
        AssertCell(v8Rows, "6.1", 1, "done");
        AssertCell(v8Rows, "6.2", 1, "done");
        AssertCell(v8Rows, "6.7", 1, "done");
        AssertCell(v8Rows, "6.12", 1, "ready-for-dev");

        foreach (int story in Enumerable.Range(1, 12))
        {
            AssertSingleOccurrence(v8Amendment, $"### Story 6.{story}:");
        }
    }

    [Fact]
    public void EpicOverlayShouldPreserveFullDenominatorAndCorrectiveFrCoverage()
    {
        string epics = ReadRepositoryFile(EpicsPath);
        string overlay = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY:BEGIN", "EPIC-6-AUTHORITY-OVERLAY:END");
        string requirementSection = ExtractSection(overlay, "### Requirement Authority And Denominators", "### Exact Historical Story Dispositions");
        string flat = NormalizeWhitespace(requirementSection);

        // Require the enumeration itself to be complete and canonically spelled, not merely a set union of
        // any FR numbers mentioned anywhere in the section.
        string enumeration = ExtractBetween(requirementSection, "The initiative surface is exactly:", "\n\n");
        foreach (int number in Enumerable.Range(1, 20))
        {
            enumeration.ShouldContain($"FR-{number}", Case.Sensitive, $"The initiative surface enumeration must name FR-{number}.");
        }

        int[] initiativeFrs = Regex.Matches(enumeration, @"\bFR-(\d{1,2})\b", RegexOptions.CultureInvariant)
            .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(number => number)
            .ToArray();
        initiativeFrs.ShouldBe(Enumerable.Range(1, 20).ToArray());

        flat.ShouldContain("FR-16 is the only initiative non-activation");
        flat.ShouldContain("all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria");
        flat.ShouldContain("named owner approval, recorded rationale, and compatibility evidence");
        flat.ShouldContain("13,289-LOC SM-1 baseline");
        flat.ShouldContain("cannot shrink");

        AssertSingleOccurrence(epics, "### Corrective Initiative-FR Coverage");
        string coverage = ExtractSection(overlay, "### Corrective Initiative-FR Coverage", "## Epic 6:");
        string[] coverageRows = MarkdownDataRows(coverage, "FR-");

        coverageRows.Length.ShouldBeGreaterThan(0);
        coverageRows.ShouldAllBe(row => TableCells(row).Length == 2);
        AssertPopulatedCells(coverageRows, skip: 1);

        // Every initiative requirement must have a landing, not just the seven corrective ones.
        int[] coveredFrs = coverageRows
            .SelectMany(row => Regex.Matches(GetFirstTableCell(row), @"\bFR-(\d{1,2})\b", RegexOptions.CultureInvariant))
            .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        foreach (int number in Enumerable.Range(1, 20))
        {
            bool covered = coveredFrs.Contains(number)
                || coverageRows.Any(row => IsCoveredByRange(GetFirstTableCell(row), number));
            covered.ShouldBeTrue($"The corrective coverage table must assign a landing for FR-{number}.");
        }

        foreach (string required in new[] { "FR-3", "FR-10", "FR-13", "FR-17", "FR-18", "FR-19", "FR-20" })
        {
            coverageRows.Any(row => GetFirstTableCell(row).Contains(required, StringComparison.Ordinal))
                .ShouldBeTrue($"The corrective coverage table must carry an explicit row for {required}.");
        }
    }

    [Fact]
    public void EpicOverlayShouldBindTheDependencyOrderItAuthorizes()
    {
        string epics = ReadRepositoryFile(EpicsPath);
        string previousOverlay = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY:BEGIN", "EPIC-6-AUTHORITY-OVERLAY:END");
        string amendment = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN", "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:END");
        string v4Amendment = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:BEGIN", "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:END");
        string v5Amendment = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:BEGIN", "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V5:END");
        string v6Amendment = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:BEGIN", "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V6:END");
        string activeAmendment = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:BEGIN", "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V7:END");
        string v8Amendment = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN", "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:END");

        // The overlay is the artifact a story's dev agent reads. Asserting the order only in architecture
        // lets the epic plan authorize the sequence AC4 forbids.
        AssertSingleOccurrence(previousOverlay, "### Binding Dependency Order");
        AssertSingleOccurrence(amendment, "### Binding Dependency Order");
        AssertSingleOccurrence(v4Amendment, "### Binding Dependency Order");
        AssertSingleOccurrence(v5Amendment, "### Binding Dependency Order");
        AssertSingleOccurrence(v6Amendment, "### Binding Dependency Order");
        AssertSingleOccurrence(activeAmendment, "### Binding Dependency Order");
        AssertSingleOccurrence(v8Amendment, "### Topological Dependency Plan");
        string previousOrder = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(previousOverlay, "### Binding Dependency Order"));
        string order = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(amendment, "### Binding Dependency Order"));
        string v4Order = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(v4Amendment, "### Binding Dependency Order"));
        string v5Order = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(v5Amendment, "### Binding Dependency Order"));
        string v6Order = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(v6Amendment, "### Binding Dependency Order"));
        string activeOrder = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(activeAmendment, "### Binding Dependency Order"));
        string v8Order = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(v8Amendment, "### Topological Dependency Plan"));

        previousOrder.ShouldContain("6.1 -> 6.7 -> 6.2");
        previousOrder.ShouldContain("Story 6.7 and the frozen SM-C2 benchmark both precede Story 6.2 completion");
        previousOrder.ShouldContain("Story 6.2 precedes Story 6.5");
        previousOrder.ShouldContain("Story 6.6 remains last");
        order.ShouldContain("6.1 authority correction -> 6.7 -> 6.2 -> 6.5 -> 6.6");
        order.ShouldContain("SM-C2 baseline remains a pre-change gate for 6.2");

        order.ShouldNotContain("6.1 -> 6.2 -> 6.7");
        order.ShouldNotContain("6.2 may complete before Story 6.7");
        order.ShouldNotContain("Story 6.5 may precede Story 6.2");

        // The v4 order must place 6.8 after 6.2 and keep every constraint the v3 order carried. Asserting
        // it inside the v4 block is what makes the superseding order binding rather than merely present.
        v4Order.ShouldContain("6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6");
        v4Order.ShouldContain("SM-C2 baseline remains a pre-change gate for 6.2");
        v4Order.ShouldContain("Story 6.6 remains last");
        v4Order.ShouldContain("mechanically generated final record");

        v4Order.ShouldNotContain("6.1 -> 6.2 -> 6.7");
        v4Order.ShouldNotContain("6.2 -> 6.5 -> 6.8");
        v4Order.ShouldNotContain("Story 6.8 is optional");

        // The v5 order preserves the v4 spine and adds 6.9 as a parallel constraint rather than a new link
        // in it. Asserting the spine verbatim inside the v5 block is what stops a later amendment from
        // quietly re-sequencing 6.7, 6.2, or 6.8 while appearing only to add a story.
        v6Order.ShouldContain("6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6");
        v6Order.ShouldContain("6.9 -> 6.3");
        v6Order.ShouldContain("6.9 -> 6.6");
        v6Order.ShouldContain("Story 6.6 remains last");

        v6Order.ShouldNotContain("6.1 -> 6.2 -> 6.7");
        v6Order.ShouldNotContain("Story 6.9 is optional");
        v6Order.ShouldNotContain("6.9 may complete after 6.6");

        activeOrder.ShouldContain("6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6");
        activeOrder.ShouldContain("6.8 -> 6.12 -> 6.3 completion");
        activeOrder.ShouldContain("6.12 -> 6.6");
        activeOrder.ShouldContain("Story 6.6 remains last");
        activeOrder.ShouldNotContain("Story 6.12 is optional");
        activeOrder.ShouldNotContain("6.12 may complete after 6.6");

        v8Order.ShouldContain("6.1 -> 6.7");
        v8Order.ShouldContain("6.7 -> 6.2");
        v8Order.ShouldContain("6.8 -> 6.10");
        v8Order.ShouldContain("6.9 -> 6.10");
        v8Order.ShouldContain("6.8 -> 6.12");
        v8Order.ShouldContain("6.10 -> completion of 6.3");
        v8Order.ShouldContain("6.12 -> completion of 6.3");
        v8Order.ShouldContain("6.2 -> 6.11");
        v8Order.ShouldContain("6.11 -> 6.6");
        v8Order.ShouldContain("6.12 -> 6.6");
        v8Order.ShouldContain("The graph is acyclic");
        v8Order.ShouldContain("mutually independent");

        // The overlay must record its own amendment history so a post-freeze change cannot be invisible.
        string previousOverlayFlat = NormalizeWhitespace(previousOverlay);
        string amendmentFlat = NormalizeWhitespace(amendment);
        string v4AmendmentFlat = NormalizeWhitespace(v4Amendment);
        string v5AmendmentFlat = NormalizeWhitespace(v5Amendment);
        string v6AmendmentFlat = NormalizeWhitespace(v6Amendment);
        string activeAmendmentFlat = NormalizeWhitespace(activeAmendment);
        string v8AmendmentFlat = NormalizeWhitespace(v8Amendment);
        previousOverlayFlat.ShouldContain($"**Overlay version:** `{BaseOverlayVersion}`");
        previousOverlayFlat.ShouldContain("Overlay amendment log");
        amendmentFlat.ShouldContain($"**Overlay version:** `{PreviousOverlayVersion}`");
        amendmentFlat.ShouldContain($"**Architecture authority:** `{PreviousArchitectureVersion}`");
        amendmentFlat.ShouldContain("non-packable, non-publishable composition harness");
        amendmentFlat.ShouldContain("not a production or deployment composition root");
        v4AmendmentFlat.ShouldContain($"**Overlay version:** `{V4OverlayVersion}`");
        v4AmendmentFlat.ShouldContain($"**Architecture authority:** `{V4ArchitectureVersion}`");
        v4AmendmentFlat.ShouldContain($"**Supersedes:** `{PreviousOverlayVersion}`");
        v5AmendmentFlat.ShouldContain($"**Overlay version:** `{V5OverlayVersion}`");
        v5AmendmentFlat.ShouldContain($"**Architecture authority:** `{V5ArchitectureVersion}`");
        v5AmendmentFlat.ShouldContain($"**Supersedes:** `{V4OverlayVersion}`");
        v6AmendmentFlat.ShouldContain($"**Overlay version:** `{V6OverlayVersion}`");
        v6AmendmentFlat.ShouldContain($"**Architecture authority:** `{V6ArchitectureVersion}`");
        v6AmendmentFlat.ShouldContain($"**Supersedes:** `{V5OverlayVersion}`");
        activeAmendmentFlat.ShouldContain($"**Overlay version:** `{V7OverlayVersion}`");
        activeAmendmentFlat.ShouldContain($"**Architecture authority:** `{V7ArchitectureVersion}`");
        activeAmendmentFlat.ShouldContain($"**Supersedes:** `{V6OverlayVersion}`");
        v8AmendmentFlat.ShouldContain($"**Overlay version:** `{OverlayVersion}`");
        v8AmendmentFlat.ShouldContain($"**Architecture authority:** `{ArchitectureVersion}`");
        v8AmendmentFlat.ShouldContain($"**Supersedes:** `{V7OverlayVersion}`");

        // The v4 amendment supersedes ownership language nowhere. If it ever starts doing so, the test
        // AppHost decision would be silently reopened by a record-keeping amendment.
        v4AmendmentFlat.ShouldNotContain("production composition root");
        v4AmendmentFlat.ShouldNotContain("reusable runtime capability is module-owned");

        // The v5 amendment is equally narrow: it tiers the conformance oracle and must not reopen hosting
        // ownership or the frozen preservation denominator under cover of a test-structure change.
        v5AmendmentFlat.ShouldNotContain("production composition root");
        v5AmendmentFlat.ShouldNotContain("reusable runtime capability is module-owned");
        v5AmendmentFlat.ShouldContain("frozen FR-20 denominator is unchanged");

        // The v6 amendment relaxes a performance threshold, which is the amendment most able to do damage by
        // implication. It must not reopen hosting ownership, and it must not weaken the projection
        // correctness requirement whose cost is the entire reason the threshold moved: an amendment that
        // bought speed by allowing repair-on-read would satisfy the gate and defeat AC6.
        v6AmendmentFlat.ShouldNotContain("production composition root");
        v6AmendmentFlat.ShouldNotContain("reusable runtime capability is module-owned");
        v6AmendmentFlat.ShouldContain("does **not** relax AC6");
        v6AmendmentFlat.ShouldContain("does not authorize repairing cross-key inconsistency on read");
        v6AmendmentFlat.ShouldContain("approved-cost ceiling");
        v6AmendmentFlat.ShouldContain("may not be cited as evidence of no regression");
        v6AmendmentFlat.ShouldContain("frozen FR-20 denominator is unchanged");

        activeAmendmentFlat.ShouldNotContain("production composition root");
        activeAmendmentFlat.ShouldNotContain("reusable runtime capability is module-owned");
        activeAmendmentFlat.ShouldContain("immutable point-in-time evidence");
        activeAmendmentFlat.ShouldContain("never substitutes current `HEAD`");
        activeAmendmentFlat.ShouldContain("Exactly one approved successor-chain head");
        activeAmendmentFlat.ShouldContain("PROJECTION_PROOF_SUPERSESSION_REQUIRED");
        activeAmendmentFlat.ShouldContain("No completed Story 6.2 record or v2 evidence byte is rewritten");
        activeAmendmentFlat.ShouldContain("frozen FR-20 denominator is unchanged");

        v8AmendmentFlat.ShouldContain("AUTHORITY CORRECTION ONLY — NOT READY");
        v8AmendmentFlat.ShouldContain("post P95 <= 1.05 x baseline P95");
        v8AmendmentFlat.ShouldContain("not a current Story 6.6 pass option");
        v8AmendmentFlat.ShouldContain("assessor is not instructed or modified to return a particular verdict");
        v8AmendmentFlat.ShouldContain("does not implement Stories 6.3-6.6 or 6.8-6.12");

        // The log continuation exists because the v2 table it points at sits inside the immutable byte
        // range. Without this, v6 could silently stop recording amendments the way v3, v4, and v5 did.
        activeAmendmentFlat.ShouldContain("Overlay Amendment Log");
        foreach (string recorded in new[] { PreviousOverlayVersion, V4OverlayVersion, V5OverlayVersion, V6OverlayVersion, V7OverlayVersion })
        {
            activeAmendmentFlat.ShouldContain(
                $"`{recorded}`",
                Case.Sensitive,
                "every amendment must record itself in the continuation log");
        }

        foreach (string recorded in new[] { PreviousOverlayVersion, V4OverlayVersion, V5OverlayVersion, V6OverlayVersion, V7OverlayVersion, OverlayVersion })
        {
            v8AmendmentFlat.ShouldContain($"`{recorded}`", Case.Sensitive, "the comprehensive v8 continuation must retain the complete amendment chain");
        }
    }

    [Fact]
    public void EpicOverlayAndGeneratedContextShouldBeVersionAndStoryEquivalent()
    {
        string epics = ReadRepositoryFile(EpicsPath);
        TryReadGitBlobBytes("HEAD", ContextPath, out byte[] contextBytes)
            .ShouldBeTrue("Historical v8 validation requires the committed Epic 6 context, independent of unrelated working-tree edits.");
        string context = Encoding.UTF8.GetString(contextBytes);
        string contextFrontmatter = ExtractFrontmatter(context);

        epics.ShouldContain($"version={OverlayVersion}");
        AssertYamlScalar(contextFrontmatter, "overlay_version", OverlayVersion);
        AssertYamlScalar(contextFrontmatter, "architecture_version", ArchitectureVersion);
        context.ShouldContain("# Epic 6 Context:");

        for (int story = 1; story <= 12; story++)
        {
            epics.ShouldContain($"### Story 6.{story}:");
            context.ShouldContain($"### 6.{story} ");
        }

        string flatContext = NormalizeWhitespace(context);

        foreach (string semantic in new[]
        {
            "FR-16 is the only non-activation",
            "13,289 LOC",
            "104 Feature-FRs",
            "77 Feature-NFRs",
            "52 UX decisions",
            "post P95 <= 1.05 x baseline P95",
            "6.1 -> 6.7 -> 6.2",
            "Never initialize, update, or traverse nested submodules",
            "6.6 is last",
            "6.8 + 6.10 precede completion of 6.5",
            "6.8 follows 6.2 and precedes completion of 6.3, 6.4, 6.5, and 6.6",
            "6.8 precedes 6.12",
            "6.9 + 6.10 + 6.12 precede completion of 6.3",
            "PROJECTION_PROOF_SUPERSESSION_REQUIRED",
            "Completed projection proof is validated at its declared candidate and dependency identities",
            "exactly one approved predecessor-linked chain head",
            "AUTHORITY CORRECTION ONLY — NOT READY",
            "No remaining Epic 6 implementation may start or resume",
            "Story 6.10 is independent of 6.12",
            "mandatory before 6.6",
            "publish its complete actual result unchanged",

            // The invariant, not just the story title: a context that names Story 6.8 while leaving the
            // record rules out gives the dev agent a story with no binding rule to implement.
            "Counts, file paths, submodule state, and root gitlink state in a completion record are derived outputs",
            "may not restate its numbers",
            "second hand-maintained file list is a conformance failure",
            "red rather than stale",
        })
        {
            flatContext.ShouldContain(semantic);
        }

        // Correspondence must be semantic, not just matching version strings. The derived context is the
        // file a dev agent loads, so its own landing-zone table must agree with the register it derives
        // from, and it must carry the obligations the overlay added.
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string register = ExtractSection(architecture, "### Initiative Landing-Zone Register", "### Open-Question Disposition Register");
        string[] registerRows = MarkdownDataRows(register, "FR-");
        string[] contextRows = MarkdownDataRows(context, "FR-");

        foreach (string registerRow in registerRows)
        {
            string requirement = GetFirstTableCell(registerRow);
            string[] matching = contextRows.Where(row => GetFirstTableCell(row) == requirement).ToArray();
            matching.Length.ShouldBe(1, $"The derived context must carry exactly one landing-zone row for {requirement}.");

            string contextOwner = TableCells(matching[0])[1];
            AssertOwnerNamesNoModuleSurface(contextOwner, requirement, "Derived context");

            foreach (string owner in RegisterOwnerModules(TableCells(registerRow)[2]))
            {
                contextOwner.ShouldContain(ShortModuleName(owner), Case.Insensitive, $"Derived context row {requirement} must agree with the architecture register owner '{owner}'.");
            }
        }

        // Obligations the v2 overlay added must be carried forward through v3, or the dev agent reading the context
        // never sees them.
        foreach (string obligation in new[]
        {
            "ADR 0003",
            "ADR 0004",
            "IAsyncDomainProjectionHandler",
            "projection-read-store-population-proof-v2",
            "projection-read-store-population-proof-v3",
        })
        {
            flatContext.ShouldContain(obligation, Case.Sensitive, $"The derived context must carry the overlay obligation '{obligation}'.");
        }

        flatContext.ShouldContain("non-packable, non-publishable module-scoped AppHost");
        flatContext.ShouldContain("not a production or deployment composition root");
        flatContext.ShouldContain("Platform deployment owns production topology and composition");
        flatContext.ShouldContain("platform libraries own reusable runtime capability");

        flatContext.ShouldNotContain("activates all preserved feature scope");
    }

    [Fact]
    public void PromotionCompletionInvariantShouldBeScopedToDeclaredRootGitlinks()
    {
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string section = NormalizeWhitespace(ExtractSectionUntilNextHeadingOfSameLevel(architecture, "### Promotion Completion Invariant"));

        section.ShouldContain("exact root `references/...` paths");
        section.ShouldContain("clean including untracked files");
        section.ShouldContain("availability policy");
        section.ShouldContain("mode-`160000` gitlink");
        section.ShouldContain("root `.gitmodules`");
        section.ShouldContain("never initializes or traverses nested submodules");
        section.ShouldContain("unrelated state as warnings");
        section.ShouldContain("gitlinks changed since the work baseline");
    }

    [Fact]
    public void NamedPlatformLandingZonesShouldExposeSignatureCompatiblePublicApis()
    {
        // Drive verification from the register itself. A hardcoded expectation list cannot detect a
        // register row repointed at an API that does not exist.
        string architecture = ReadRepositoryFile(ArchitecturePath);
        string register = ExtractSection(architecture, "### Initiative Landing-Zone Register", "### Open-Question Disposition Register");
        string[] rows = MarkdownDataRows(register, "FR-");

        List<string> declaredApis = [];

        foreach (string row in rows)
        {
            if (GetFirstTableCell(row) == "FR-16")
            {
                continue;
            }

            declaredApis.AddRange(Regex.Matches(TableCells(row)[2], @"`(?<identifier>[A-Z][A-Za-z0-9]*(?:<[^`>]*>)?)`", RegexOptions.CultureInvariant)
                .Select(match => match.Groups["identifier"].Value));
        }

        declaredApis = [.. declaredApis.Distinct(StringComparer.Ordinal)];
        declaredApis.Count.ShouldBeGreaterThanOrEqualTo(6, "The register must name the public platform APIs it depends on.");

        foreach (string api in declaredApis)
        {
            AssertDeclaredApiExists(api);
        }

        // Every API the register names must be covered by a known expectation, so a new register entry
        // cannot silently escape verification.
        foreach (string api in declaredApis)
        {
            KnownPlatformApis.ShouldContainKey(StripGenerics(api), $"Register names '{api}' but no verification expectation exists for it.");
        }
    }

    /// <summary>
    /// Expected public surface for every API the register may name. Values are the declaring file, the
    /// return type, whether the member is static, and required parameter fragments.
    /// </summary>
    private static readonly Dictionary<string, (string RelativePath, string ReturnType, bool IsStatic, string[] Parameters)> KnownPlatformApis = new(StringComparer.Ordinal)
    {
        ["AddEventStoreDomainService"] = ("references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs", "WebApplicationBuilder", true, ["this WebApplicationBuilder builder"]),
        ["UseEventStoreDomainService"] = ("references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs", "WebApplication", true, ["this WebApplication app"]),
        ["AddEventStoreDomainTelemetry"] = ("references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainTelemetryExtensions.cs", "WebApplicationBuilder", true, ["this WebApplicationBuilder builder", "string domain"]),
        ["MapDefaultEndpoints"] = ("references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs", "WebApplication", true, ["this WebApplication app"]),
        ["AddServiceDefaults"] = ("references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs", "TBuilder", true, ["this TBuilder builder"]),
        ["AddHexalithEventStore"] = ("references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs", "HexalithEventStoreResources", true, ["this IDistributedApplicationBuilder builder"]),
        ["AddEventStoreDomainModule"] = ("references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs", "IResourceBuilder<ProjectResource>", true, ["HexalithEventStoreResources"]),
        ["AddTypedHttpClient"] = ("references/Hexalith.Commons/src/libraries/Hexalith.Commons.Http/HttpClientRegistration.cs", "IHttpClientBuilder", true, ["this IServiceCollection services"]),
        ["AddTenantAccess"] = ("references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/TenantAccessRegistration.cs", "IServiceCollection", true, ["this IServiceCollection services"]),
        ["Create"] = ("references/Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/PolymorphicTypeRegistry.cs", "PolymorphicTypeRegistry", true, ["IEnumerable<PolymorphicTypeRegistration> registrations"]),
        ["CreateWeb"] = ("references/Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/JsonSerializationOptions.cs", "JsonSerializerOptions", true, []),
    };

    [Fact]
    public void NamedPlatformTypesShouldBePublic()
    {
        AssertPublicType("references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/TenantAccessProjectionHandler.cs", "class", "TenantAccessProjectionHandler");
        AssertPublicType("references/Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryMeter.cs", "class", "BoundedTelemetryMeter");
        AssertPublicMethod(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryMeter.cs",
            "BoundedTelemetryCounter",
            "CreateCounter",
            isStatic: false,
            "BoundedTelemetryCounterDefinition definition");
    }

    private static void AssertDeclaredApiExists(string api)
    {
        string name = StripGenerics(api);

        if (!KnownPlatformApis.TryGetValue(name, out (string RelativePath, string ReturnType, bool IsStatic, string[] Parameters) expectation))
        {
            return;
        }

        AssertPublicMethod(expectation.RelativePath, expectation.ReturnType, name, expectation.IsStatic, expectation.Parameters);

        // If the register declares generic arity, the declaration must actually provide it.
        int declaredArity = GenericArity(api);

        if (declaredArity > 0)
        {
            string source = ReadPlatformEvidence(expectation.RelativePath);
            Regex.IsMatch(source, $@"\b{Regex.Escape(name)}\s*<(?<parameters>[^>]+)>\s*\(", RegexOptions.CultureInvariant)
                .ShouldBeTrue($"The register declares {api} with generic parameters, but no generic declaration was found.");

            Match match = Regex.Match(source, $@"\b{Regex.Escape(name)}\s*<(?<parameters>[^>]+)>\s*\(", RegexOptions.CultureInvariant);
            match.Groups["parameters"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Length
                .ShouldBe(declaredArity, $"The register declares {api} with {declaredArity} generic parameters.");
        }
    }

    private static void AssertPublicMethod(string relativePath, string returnType, string methodName, bool isStatic, params string[] parameterFragments)
    {
        // Strip comments first: a signature quoted in a doc sample or commented out is not a consumer-callable API.
        string source = StripCommentsAndDisabledRegions(ReadPlatformEvidence(relativePath));
        string staticToken = isStatic ? @"static\s+" : string.Empty;
        string pattern = $@"\bpublic\s+{staticToken}(?:async\s+)?{Regex.Escape(returnType)}\s+{Regex.Escape(methodName)}(?:<[^>]+>)?\s*\((?<parameters>[\s\S]*?)\)\s*(?:where\b|\{{|=>)";
        MatchCollection matches = Regex.Matches(source, pattern, RegexOptions.CultureInvariant);
        matches.Count.ShouldBeGreaterThan(0, $"Expected public {(isStatic ? "static " : string.Empty)}{returnType} {methodName}(...) in {relativePath}.");

        // A public member of a non-public type is not consumer-callable.
        AssertDeclaringTypeIsPublic(source, relativePath, methodName);

        if (parameterFragments.Length == 0)
        {
            matches.Any(match => match.Groups["parameters"].Value.Trim().Length == 0)
                .ShouldBeTrue($"No public zero-parameter signature for {methodName} was found in {relativePath}.");
            return;
        }

        matches.Any(match => parameterFragments.All(fragment => match.Groups["parameters"].Value.Contains(fragment, StringComparison.Ordinal)))
            .ShouldBeTrue($"No public signature for {methodName} in {relativePath} contained required parameters: {string.Join(", ", parameterFragments)}.");
    }

    private static void AssertDeclaringTypeIsPublic(string source, string relativePath, string memberName)
    {
        MatchCollection typeDeclarations = Regex.Matches(
            source,
            @"^\s*(?<modifiers>(?:public|internal|private|protected|static|sealed|partial|abstract|file|\s)*)\b(?:class|record|struct|interface)\s+(?<name>\w+)",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        typeDeclarations.Count.ShouldBeGreaterThan(0, $"Expected a type declaration in {relativePath}.");
        typeDeclarations.Any(match => match.Groups["modifiers"].Value.Contains("public", StringComparison.Ordinal))
            .ShouldBeTrue($"'{memberName}' in {relativePath} must be declared inside a public type to be consumer-callable.");
    }

    private static void AssertPublicType(string relativePath, string typeKind, string typeName)
    {
        string source = StripCommentsAndDisabledRegions(ReadPlatformEvidence(relativePath));

        // Allow any modifier order and spacing (public sealed partial class, public sealed record, ...).
        Regex.IsMatch(source, $@"\bpublic\s+(?:\w+\s+)*{Regex.Escape(typeKind)}\s+{Regex.Escape(typeName)}(?:<|\b)", RegexOptions.CultureInvariant)
            .ShouldBeTrue($"Expected public {typeKind} {typeName} in {relativePath}.");
    }

    /// <summary>
    /// Reads submodule public-surface evidence only from the commit the umbrella actually records.
    /// Current checkout bytes are not admissible substitutes for unavailable historical evidence.
    /// </summary>
    private static string ReadPlatformEvidence(string relativePath)
    {
        string submodule = SubmoduleRootOf(relativePath);

        if (submodule.Length > 0)
        {
            TryReadRecordedGitlink(submodule, out string gitlink).ShouldBeTrue(
                $"Platform evidence '{relativePath}' is unavailable because HEAD does not record a mode-160000 gitlink for '{submodule}'.");
            TryReadSubmoduleBlob(submodule, gitlink, relativePath[(submodule.Length + 1)..], out string recorded).ShouldBeTrue(
                $"Platform evidence '{relativePath}' is unavailable at recorded gitlink {gitlink}; current checkout bytes are not historical evidence.");
            return recorded;
        }

        string fullPath = RepositoryPath(relativePath);
        File.Exists(fullPath).ShouldBeTrue(
            $"Platform evidence '{relativePath}' is unavailable. Initialize root submodules, or record the surface at its gitlink; a missing checkout must not read as a missing platform API.");
        return File.ReadAllText(fullPath);
    }

    private static string SubmoduleRootOf(string relativePath)
    {
        if (!relativePath.StartsWith("references/", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string[] segments = relativePath.Split('/');
        return segments.Length < 2 ? string.Empty : $"{segments[0]}/{segments[1]}";
    }

    private static bool TryReadRecordedGitlink(string submodulePath, out string commit)
    {
        commit = string.Empty;

        if (!TryRunGit(out string output, "ls-tree", "HEAD", submodulePath))
        {
            return false;
        }

        Match match = Regex.Match(output, @"^160000 commit (?<sha>[0-9a-f]{40})", RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return false;
        }

        commit = match.Groups["sha"].Value;
        return true;
    }

    private static bool TryReadSubmoduleBlob(string submodulePath, string commit, string pathInSubmodule, out string content)
    {
        content = string.Empty;
        string workingDirectory = RepositoryPath(submodulePath);

        if (!Directory.Exists(workingDirectory))
        {
            return false;
        }

        if (!TryRunGitIn(workingDirectory, out string output, "cat-file", "blob", $"{commit}:{pathInSubmodule}"))
        {
            return false;
        }

        content = output;
        return true;
    }

    private static IEnumerable<string> RegisterOwnerModules(string ownerCell)
        => Regex.Matches(ownerCell, @"`(?<module>Hexalith\.(?:EventStore|Commons)\.\w+)`", RegexOptions.CultureInvariant)
            .Select(match => match.Groups["module"].Value)
            .Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Returns the last two segments of a module name, e.g. "Commons.Http". Matching on the final segment
    /// alone would let a module-owned facade such as "Hexalith.Conversations.Http" satisfy a
    /// "Hexalith.Commons.Http" owner requirement.
    /// </summary>
    private static string ShortModuleName(string module)
    {
        string[] segments = module.Split('.');
        return segments.Length < 2 ? module : $"{segments[^2]}.{segments[^1]}";
    }

    /// <summary>
    /// OQ-1 authorizes no Conversations facade as a landing zone, so no owner cell in the register or the
    /// derived context may name a module-owned type at all.
    /// </summary>
    private static void AssertOwnerNamesNoModuleSurface(string ownerCell, string requirement, string artifact)
    {
        Match match = Regex.Match(ownerCell, @"Hexalith\.Conversations\.\w+", RegexOptions.CultureInvariant);
        match.Success.ShouldBeFalse(
            $"{artifact} row {requirement} names module-owned '{match.Value}' as a landing zone; OQ-1 authorizes no Conversations facade.");

        foreach (string prohibited in ProhibitedModuleOwnedRuntimeProjects)
        {
            ownerCell.ShouldNotContain(prohibited, Case.Insensitive, $"{artifact} row {requirement} must not name a module-owned runtime project as a landing zone.");
        }
    }

    private static bool IsCoveredByRange(string cell, int number)
    {
        Match match = Regex.Match(cell, @"FR-(?<from>\d{1,2})\s*(?:through|-|–|to)\s*FR-(?<to>\d{1,2})", RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return false;
        }

        int from = int.Parse(match.Groups["from"].Value, System.Globalization.CultureInfo.InvariantCulture);
        int to = int.Parse(match.Groups["to"].Value, System.Globalization.CultureInfo.InvariantCulture);
        return number >= from && number <= to;
    }

    private static string StripGenerics(string identifier)
    {
        int index = identifier.IndexOf('<', StringComparison.Ordinal);
        return index < 0 ? identifier : identifier[..index];
    }

    private static int GenericArity(string identifier)
    {
        int start = identifier.IndexOf('<', StringComparison.Ordinal);

        if (start < 0 || !identifier.EndsWith('>'))
        {
            return 0;
        }

        return identifier[(start + 1)..^1]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static string StripCommentsAndDisabledRegions(string source)
    {
        string withoutBlockComments = Regex.Replace(source, @"/\*[\s\S]*?\*/", string.Empty, RegexOptions.CultureInvariant);
        string withoutLineComments = Regex.Replace(withoutBlockComments, @"^[ \t]*///?.*$", string.Empty, RegexOptions.Multiline | RegexOptions.CultureInvariant);
        string withoutTrailingComments = Regex.Replace(withoutLineComments, @"//.*$", string.Empty, RegexOptions.Multiline | RegexOptions.CultureInvariant);
        return Regex.Replace(withoutTrailingComments, @"^\s*#if\s+false[\s\S]*?^\s*#endif", string.Empty, RegexOptions.Multiline | RegexOptions.CultureInvariant);
    }

    private static bool IsSupersededSectionHeading(string heading)
        => SectionSupersessionMarkers.Any(marker => heading.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static bool IsQualifiedLine(string line)
        => LineSupersessionMarkers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static bool IsQualifiedTestAppHostLine(string line)
        => TestAppHostBoundaryMarkers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase))
        && !ProductionOwnershipAssertions.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static void AssertYamlScalar(string frontmatter, string key, string expected)
    {
        Match match = Regex.Match(frontmatter, $@"^{Regex.Escape(key)}:\s*'?(?<value>[^'\r\n]+?)'?\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        match.Success.ShouldBeTrue($"Frontmatter must declare '{key}'.");
        match.Groups["value"].Value.ShouldBe(expected, $"Frontmatter '{key}' must be '{expected}'.");
    }

    private static void AssertCell(string[] rows, string key, int cellIndex, string expected)
    {
        string[] cells = TableCells(rows.Single(row => GetFirstTableCell(row) == key));
        cells.Length.ShouldBeGreaterThan(cellIndex);
        cells[cellIndex].ShouldContain(expected, Case.Sensitive, $"Row '{key}' cell {cellIndex} must contain '{expected}'.");
    }

    private static void AssertPopulatedCells(string[] rows, int skip)
    {
        foreach (string row in rows)
        {
            foreach (string cell in TableCells(row).Skip(skip))
            {
                string trimmed = cell.Trim().Trim('​', '﻿');
                trimmed.ShouldNotBeNullOrWhiteSpace($"Row '{GetFirstTableCell(row)}' has an empty cell.");
                PlaceholderCellValues.ShouldNotContain(
                    trimmed.ToLowerInvariant(),
                    $"Row '{GetFirstTableCell(row)}' uses placeholder cell content '{trimmed}'.");
            }
        }
    }

    private static void AssertSingleOccurrence(string content, string marker)
        => CountOccurrences(content, marker).ShouldBe(1, $"'{marker}' must occur exactly once; a duplicate hides contradictory rows.");

    /// <summary>Collapses whitespace runs so a reflowed document does not break multi-word assertions.</summary>
    private static string NormalizeWhitespace(string content)
        => Regex.Replace(content.Replace("\r\n", "\n"), @"[ \t\n]+", " ").Trim();

    private static string ExtractFrontmatter(string content)
    {
        string normalized = content.Replace("\r\n", "\n");
        normalized.ShouldStartWith("---\n");
        int end = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(4, "Frontmatter must be terminated.");
        return normalized[4..end];
    }

    private static string ExtractSection(string content, string startHeading, string nextHeading)
        => ExtractBetween(content, startHeading, nextHeading);

    /// <summary>
    /// Bounds a section at the next heading of the same or higher level, so a later-inserted sibling
    /// section cannot be absorbed into the extracted region and satisfy its assertions.
    /// </summary>
    private static string ExtractSectionUntilNextHeadingOfSameLevel(string content, string startHeading)
    {
        string normalized = content.Replace("\r\n", "\n");
        int start = normalized.IndexOf(startHeading, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"Missing start heading '{startHeading}'.");

        int level = startHeading.TakeWhile(character => character == '#').Count();
        level.ShouldBeGreaterThan(0, "The start marker must be a Markdown heading.");

        int searchFrom = start + startHeading.Length;

        while (true)
        {
            int next = normalized.IndexOf("\n#", searchFrom, StringComparison.Ordinal);

            if (next < 0)
            {
                return normalized[start..];
            }

            int hashes = normalized.Skip(next + 1).TakeWhile(character => character == '#').Count();

            if (hashes <= level)
            {
                return normalized[start..next];
            }

            searchFrom = next + 1;
        }
    }

    private static string ExtractBetween(string content, string startMarker, string endMarker)
    {
        int start = content.IndexOf(startMarker, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"Missing start marker '{startMarker}'.");
        int end = content.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start, $"Missing end marker '{endMarker}' after '{startMarker}'.");
        return content[start..end];
    }

    private static string ExtractFirstFencedBlock(string content)
    {
        string normalized = content.Replace("\r\n", "\n");
        int start = normalized.IndexOf("```text\n", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        start += "```text\n".Length;
        int end = normalized.IndexOf("\n```", start, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start);
        return normalized[start..end];
    }

    /// <summary>
    /// Returns Markdown table data rows, skipping header and alignment-separator rows and ignoring rows
    /// inside fenced code blocks.
    /// </summary>
    private static string[] MarkdownDataRows(string section, string firstCellPrefix)
    {
        List<string> rows = [];
        bool insideFence = false;

        foreach (string rawLine in section.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                insideFence = !insideFence;
                continue;
            }

            if (insideFence || !line.StartsWith("|", StringComparison.Ordinal))
            {
                continue;
            }

            string first = GetFirstTableCell(line);

            if (string.Equals(first, "Story", StringComparison.Ordinal)
                || string.Equals(first, "Requirement", StringComparison.Ordinal)
                || string.Equals(first, "Initiative requirement", StringComparison.Ordinal)
                || string.Equals(first, "ID", StringComparison.Ordinal)
                || string.Equals(first, "FR", StringComparison.Ordinal)
                || string.Equals(first, "Overlay version", StringComparison.Ordinal)
                || string.Equals(first, "Hot-path ID", StringComparison.Ordinal))
            {
                continue;
            }

            // Alignment separators may be written as ---, :---, ---:, or :---:.
            if (Regex.IsMatch(first, @"^:?-{3,}:?$"))
            {
                continue;
            }

            if (!first.StartsWith(firstCellPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            rows.Add(line);
        }

        return [.. rows];
    }

    private static int CountOccurrences(string content, string value)
    {
        int count = 0;
        int index = content.IndexOf(value, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = content.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string GetFirstTableCell(string row)
        => TableCells(row)[0];

    /// <summary>Splits a Markdown table row, honouring escaped pipes and pipes inside inline code.</summary>
    private static string[] TableCells(string row)
    {
        string trimmed = row.Trim();
        trimmed = trimmed.StartsWith('|') ? trimmed[1..] : trimmed;
        trimmed = trimmed.EndsWith('|') && !trimmed.EndsWith("\\|", StringComparison.Ordinal) ? trimmed[..^1] : trimmed;

        List<string> cells = [];
        StringBuilder current = new();
        bool insideCode = false;

        for (int index = 0; index < trimmed.Length; index++)
        {
            char character = trimmed[index];

            if (character == '\\' && index + 1 < trimmed.Length && trimmed[index + 1] == '|')
            {
                current.Append('|');
                index++;
                continue;
            }

            if (character == '`')
            {
                insideCode = !insideCode;
                current.Append(character);
                continue;
            }

            if (character == '|' && !insideCode)
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        cells.Add(current.ToString().Trim());
        return [.. cells];
    }

    private static string ComputeSha256(ReadOnlySpan<byte> bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string ReadRepositoryFile(string relativePath)
        => File.ReadAllText(RepositoryPath(relativePath));

    private static string RepositoryPath(string relativePath)
        => Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static bool TryReadGitBlobBytes(string revision, string repositoryRelativePath, out byte[] content)
    {
        content = [];

        if (!TryStartGit(FindRepositoryRoot(), out Process? process, "cat-file", "blob", $"{revision}:{repositoryRelativePath}"))
        {
            return false;
        }

        using Process started = process;
        using MemoryStream buffer = new();
        Task<string> errorTask = started.StandardError.ReadToEndAsync();
        started.StandardOutput.BaseStream.CopyTo(buffer);

        if (!started.WaitForExit(GitTimeoutMilliseconds))
        {
            return false;
        }

        errorTask.Wait(GitTimeoutMilliseconds);

        if (started.ExitCode != 0)
        {
            return false;
        }

        content = buffer.ToArray();
        return true;
    }

    private static bool TryRunGit(out string output, params string[] arguments)
        => TryRunGitIn(FindRepositoryRoot(), out output, arguments);

    private static bool TryRunGitIn(string workingDirectory, out string output, params string[] arguments)
    {
        output = string.Empty;

        if (!TryStartGit(workingDirectory, out Process? process, arguments))
        {
            return false;
        }

        using Process started = process;
        Task<string> outputTask = started.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = started.StandardError.ReadToEndAsync();

        if (!started.WaitForExit(GitTimeoutMilliseconds))
        {
            return false;
        }

        output = outputTask.Wait(GitTimeoutMilliseconds) ? outputTask.Result : string.Empty;
        errorTask.Wait(GitTimeoutMilliseconds);
        return started.ExitCode == 0;
    }

    private const int GitTimeoutMilliseconds = 60_000;

    private static bool TryStartGit(string workingDirectory, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Process? process, params string[] arguments)
    {
        process = null;
        ProcessStartInfo startInfo = new()
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("core.quotepath=false");

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            process = Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }

        return process is not null;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
