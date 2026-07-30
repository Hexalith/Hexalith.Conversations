// <copyright file="ConsumePromoteKeepInventoryValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.4 (AC5) — validates the COMMITTED Consume/Promote/Keep inventory on disk:
/// <c>docs/release-evidence/consume-promote-keep-inventory-v1.json</c> (the artifact FR-2 governs, Story 1.5
/// amends, and Story 5.3 reads for the SM-1 plumbing-LOC baseline).
/// </summary>
/// <remarks>
/// <para>
/// Mirrors <see cref="ReleaseBaselineValidationTest"/> and <see cref="AtRiskTestRegisterGenerationTest"/>:
/// repo-root discovery → re-read the committed JSON → assert the STRUCTURAL invariants. It deliberately asserts
/// the invariants (every area exactly once; single classification; no unclassified / dual-classified; per-area
/// LOC sum reconciles to the recorded <c>sourceTotalLoc</c>; <c>plumbingBaselineLoc</c> = Σ(Consume) + Σ(Promote)),
/// NOT the exact hand-curated per-area LOC values, which are human-accepted estimates per AC5.
/// </para>
/// <para>
/// Content-safety here is scoped (AC5): this internal planning artifact legitimately NAMES technical-module SDK /
/// Commons capabilities (EventStore, TenantAccessProjectionHandler, …) — that is its purpose (AC2). The
/// forbidden-substrate-term scan the public-contract / oracle artifacts apply is NOT applied; only payload
/// secrets, drive paths, and provider IDs are forbidden.
/// </para>
/// </remarks>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class ConsumePromoteKeepInventoryValidationTest
{
    private const string InventoryFileName = "consume-promote-keep-inventory-v1.json";
    private const string Story62DispositionFileName = "consume-promote-keep-story-6-2-disposition-v1.json";

    private static readonly string[] ValidClassifications = ["Consume", "Promote", "Keep"];

    // Scoped content-safety: NO capability-name ban here (the artifact exists to record them). Only secrets / host
    // drive paths / provider IDs must never appear.
    private static readonly string[] ForbiddenFragments =
    [
        "C:\\",
        "D:\\",
        "BEGIN RSA PRIVATE KEY",
        "BEGIN PRIVATE KEY",
        "password=",
        "secret=",
    ];

    [Fact]
    public void CommittedInventoryShouldBeAcceptedAndDeclareFr2Governance()
    {
        using JsonDocument doc = LoadCommittedJson();
        JsonElement root = doc.RootElement;

        root.GetProperty("artifact").GetString().ShouldBe("consume-promote-keep-inventory");
        root.GetProperty("version").GetInt32().ShouldBe(1);
        root.GetProperty("status").GetString().ShouldBe("accepted");
        root.GetProperty("acceptedDate").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("baselineCommit").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("fr2Governed").GetBoolean().ShouldBeTrue("The inventory must declare itself the artifact FR-2 governs.");
        root.GetProperty("sm1BaselineFor").GetString().ShouldBe("Story 5.3");

        // The append-only change log must exist (Story 1.5 reclassifications append here; never rewrite a row).
        root.GetProperty("changeLog").ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public void EveryAreaShouldAppearExactlyOnceWithASingleValidClassification()
    {
        JsonElement[] areas = LoadAreas();
        areas.ShouldNotBeEmpty();

        string[] ids = areas.Select(a => a.GetProperty("areaId").GetString()!).ToArray();
        ids.ShouldAllBe(id => !string.IsNullOrWhiteSpace(id));
        ids.Length.ShouldBe(ids.Distinct().Count(), "Every area must appear exactly once (no duplicate areaId).");

        foreach (JsonElement area in areas)
        {
            string id = area.GetProperty("areaId").GetString()!;
            string classification = area.GetProperty("classification").GetString()!;
            ValidClassifications.ShouldContain(classification, $"Area '{id}' has an invalid/dual classification '{classification}'.");
            area.GetProperty("paths").EnumerateArray().Any().ShouldBeTrue($"Area '{id}' must record at least one source path.");
            area.GetProperty("approxLoc").GetInt32().ShouldBeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void EveryConsumeOrPromoteEntryShouldNameItsCapabilityFrAndOwningStory()
    {
        foreach (JsonElement area in LoadAreas())
        {
            string id = area.GetProperty("areaId").GetString()!;
            string classification = area.GetProperty("classification").GetString()!;

            if (classification is "Consume" or "Promote")
            {
                area.GetProperty("targetCapability").GetString()
                    .ShouldNotBeNullOrWhiteSpace($"{classification} area '{id}' must name its target capability (AC2).");
                area.GetProperty("fr").GetString()
                    .ShouldNotBeNullOrWhiteSpace($"{classification} area '{id}' must cross-reference its governing FR (AC2).");
                area.GetProperty("owningStory").GetString()
                    .ShouldNotBeNullOrWhiteSpace($"{classification} area '{id}' must cross-reference its owning Epic 2/3 story (AC2).");

                string capabilityStatus = area.GetProperty("capabilityStatus").GetString()!;
                string expected = classification == "Consume" ? "existing" : "to-be-promoted";
                capabilityStatus.ShouldBe(expected, $"{classification} area '{id}' capabilityStatus should be '{expected}'.");
            }
            else
            {
                area.GetProperty("keepRationale").GetString()
                    .ShouldNotBeNullOrWhiteSpace($"Keep area '{id}' must record a one-line domain-logic rationale (AC2).");
            }
        }
    }

    [Fact]
    public void PromoteLaterCandidatesShouldBeKeptNowNotPromoted()
    {
        foreach (JsonElement area in LoadAreas())
        {
            if (area.TryGetProperty("promoteLaterCandidate", out JsonElement flag) && flag.GetBoolean())
            {
                string id = area.GetProperty("areaId").GetString()!;
                area.GetProperty("classification").GetString()
                    .ShouldBe("Keep", $"promoteLaterCandidate area '{id}' must be Keep-now (OQ-3), never a Promote row in this pilot.");
                area.GetProperty("oq3Note").GetString()
                    .ShouldNotBeNullOrWhiteSpace($"promoteLaterCandidate area '{id}' must carry the OQ-3 boundary note.");
            }
        }
    }

    [Fact]
    public void PerAreaLocShouldReconcileToTheRecordedSourceTotal()
    {
        using JsonDocument doc = LoadCommittedJson();
        JsonElement root = doc.RootElement;

        int sourceTotal = root.GetProperty("sourceTotalLoc").GetInt32();
        JsonElement[] areas = root.GetProperty("areas").EnumerateArray().ToArray();

        int sumOfArea = areas.Sum(a => a.GetProperty("approxLoc").GetInt32());
        sumOfArea.ShouldBe(sourceTotal, "Per-area LOC must reconcile to the recorded sourceTotalLoc (no unattributed remainder, no double-count).");

        JsonElement reconciliation = root.GetProperty("reconciliation");
        reconciliation.GetProperty("sumOfAreaLoc").GetInt32().ShouldBe(sourceTotal);
        reconciliation.GetProperty("sourceTotalLoc").GetInt32().ShouldBe(sourceTotal);
        reconciliation.GetProperty("unattributedRemainder").GetInt32().ShouldBe(0);
    }

    [Fact]
    public void PlumbingBaselineShouldEqualConsumePlusPromoteLoc()
    {
        using JsonDocument doc = LoadCommittedJson();
        JsonElement root = doc.RootElement;

        JsonElement[] areas = root.GetProperty("areas").EnumerateArray().ToArray();
        int consume = areas.Where(a => a.GetProperty("classification").GetString() == "Consume").Sum(a => a.GetProperty("approxLoc").GetInt32());
        int promote = areas.Where(a => a.GetProperty("classification").GetString() == "Promote").Sum(a => a.GetProperty("approxLoc").GetInt32());

        int recorded = root.GetProperty("plumbingBaselineLoc").GetInt32();
        recorded.ShouldBe(consume + promote, "plumbingBaselineLoc must equal Σ(Consume) + Σ(Promote) (Keep excluded).");

        // The derivation block must show the same split it claims (AC3 reproducibility).
        JsonElement derivation = root.GetProperty("plumbingDerivation");
        derivation.GetProperty("consumeSubtotal").GetInt32().ShouldBe(consume);
        derivation.GetProperty("promoteSubtotal").GetInt32().ShouldBe(promote);
        derivation.GetProperty("plumbingBaselineLoc").GetInt32().ShouldBe(consume + promote);
    }

    [Fact]
    public void CommittedInventoryShouldPassScopedContentSafetyScan()
    {
        // Whole-file scan: the artifact may NAME SDK/Commons capabilities (AC2), but must not embed secrets,
        // drive paths, or provider IDs. Mirrors the sibling release-evidence content-safety scans, scoped per AC5.
        string raw = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), InventoryFileName));

        foreach (string fragment in ForbiddenFragments)
        {
            raw.ShouldNotContain(fragment, Case.Insensitive, $"Inventory must not contain forbidden fragment '{fragment}'.");
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // QA gap-coverage facts (qa-generate-e2e-tests). The seven facts above already cover the headline invariants; the
    // following close AC gaps the original validator left unasserted. All remain structural / internal-consistency
    // checks — they assert relationships the artifact claims about ITSELF, never the hand-curated per-area LOC values.
    // ---------------------------------------------------------------------------------------------------------------

    [Fact]
    public void BothJsonAndMarkdownSiblingArtifactsShouldBeCommitted()
    {
        // AC5: the artifact ships as a machine-readable .json AND a human-readable .md, alongside the sibling
        // release-evidence pairs. The original validator only read the .json — assert the .md is present too.
        string dir = ReleaseEvidenceDirectory();
        string mdPath = Path.Combine(dir, "consume-promote-keep-inventory-v1.md");

        File.Exists(Path.Combine(dir, InventoryFileName)).ShouldBeTrue("The machine-readable inventory .json must be committed (AC5).");
        File.Exists(mdPath).ShouldBeTrue("The human-readable inventory .md must be committed alongside the .json (AC5).");
        File.ReadAllText(mdPath).Trim().ShouldNotBeNullOrWhiteSpace("The human-readable inventory .md must not be empty.");
    }

    [Fact]
    public void Story62DispositionShouldPreserveSignedV1AndReproduceTheConsumedPath()
    {
        JsonElement[] entries = LoadStory62DispositionEntries();

        entries.Length.ShouldBe(1);
        JsonElement entry = entries.Single();
        entry.GetProperty("entryId").GetString().ShouldBe("CL-service-defaults-greenfield-consumption-1");
        entry.GetProperty("areaId").GetString().ShouldBe("service-defaults-greenfield");
        entry.GetProperty("resolution").GetString().ShouldBe("upheld");
        EntryReferencesPath(entry, "src/Hexalith.Conversations.ServiceDefaults/").ShouldBeTrue();
    }

    [Fact]
    public void NoSourceFileShouldBeDoubleCountedAcrossAreas()
    {
        // AC1: "no source double-counted". A path-STRING comparison is insufficient — a 'Foo/**' glob in one area can
        // re-include a file that another area carved out as an explicit file (e.g. a split row's duplicate-fake file),
        // and the two path strings differ so a string check passes while the FILE is counted twice. Resolve every
        // declared path spec to its actual .cs files and assert each file is attributed to exactly one area.
        string repoRoot = FindRepositoryRoot();
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement area in LoadAreas())
        {
            string id = area.GetProperty("areaId").GetString()!;
            string classification = area.GetProperty("classification").GetString()!;
            foreach (JsonElement pathElement in area.GetProperty("paths").EnumerateArray())
            {
                string spec = pathElement.GetString()!;
                string[] files = ResolveCsFiles(repoRoot, spec).ToArray();
                if (files.Length == 0)
                {
                    // A Consume/Promote glob legitimately empties once its owning story consumes the code — that is the
                    // Boilerplate Reduction refactor's GOAL (measured as a reduction by SM-1), not a stale/mistyped path.
                    // Tolerate the empty resolution ONLY when an append-only disposition entry accounts for THIS SPECIFIC
                    // consumed glob. Signed v1 remains byte-identical; later entries live in a base-hash-bound supplement
                    // whose deletion claim is re-derived from git. Scope the tolerance to the exact spec — not merely to
                    // the area — so a stale/mistyped SIBLING glob in an area with another entry is still caught. Keep the
                    // hard failure for an unaccounted empty and for Keep areas
                    // (vanishing Keep code is a regression, not a consumption). There is nothing to attribute when zero
                    // files resolve, so the double-count check is skipped for this spec.
                    bool consumptionLogged = classification is "Consume" or "Promote"
                        && ChangeLogAccountsForConsumedSpec(id, spec);
                    consumptionLogged.ShouldBeTrue(
                        $"Area '{id}' path '{spec}' resolves to no .cs file. If this glob was consumed by its owning story, "
                        + "record it (referencing this path) in the signed inventory changeLog or a mechanically bound "
                        + "versioned disposition supplement; otherwise it is a "
                        + "stale or mistyped path.");
                    continue;
                }

                foreach (string file in files)
                {
                    owners.ContainsKey(file).ShouldBeFalse(
                        $"Source file '{Path.GetRelativePath(repoRoot, file)}' is double-counted: attributed to both "
                        + $"'{owners.GetValueOrDefault(file)}' and '{id}' (AC1: no source double-counted).");
                    owners[file] = id;
                }
            }
        }
    }

    [Fact]
    public void ReconciliationPerClassificationSubtotalsShouldBeConsistent()
    {
        // AC1: the reconciliation block records consume/promote/keep subtotals. Assert each equals the actual
        // per-classification sum and that the three together reconcile to sourceTotalLoc.
        using JsonDocument doc = LoadCommittedJson();
        JsonElement root = doc.RootElement;
        JsonElement[] areas = root.GetProperty("areas").EnumerateArray().ToArray();

        int SumOf(string classification) => areas
            .Where(a => a.GetProperty("classification").GetString() == classification)
            .Sum(a => a.GetProperty("approxLoc").GetInt32());

        int consume = SumOf("Consume");
        int promote = SumOf("Promote");
        int keep = SumOf("Keep");

        JsonElement reconciliation = root.GetProperty("reconciliation");
        reconciliation.GetProperty("consumeSubtotal").GetInt32().ShouldBe(consume, "reconciliation.consumeSubtotal must equal Σ(Consume).");
        reconciliation.GetProperty("promoteSubtotal").GetInt32().ShouldBe(promote, "reconciliation.promoteSubtotal must equal Σ(Promote).");
        reconciliation.GetProperty("keepSubtotal").GetInt32().ShouldBe(keep, "reconciliation.keepSubtotal must equal Σ(Keep).");

        (consume + promote + keep).ShouldBe(root.GetProperty("sourceTotalLoc").GetInt32(),
            "Consume + Promote + Keep subtotals must reconcile to sourceTotalLoc.");
    }

    [Fact]
    public void RecordedPlumbingPercentageShouldMatchTheComputedRatio()
    {
        // AC3 sanity: the recorded plumbingBaselinePctOfSource must be the honest ratio of the recorded
        // plumbingBaselineLoc to sourceTotalLoc (the "~50% vs measured" sanity the addendum correction hinges on).
        using JsonDocument doc = LoadCommittedJson();
        JsonElement root = doc.RootElement;

        double plumbing = root.GetProperty("plumbingBaselineLoc").GetInt32();
        double source = root.GetProperty("sourceTotalLoc").GetInt32();
        double recordedPct = root.GetProperty("plumbingBaselinePctOfSource").GetDouble();

        double computedPct = 100.0 * plumbing / source;
        recordedPct.ShouldBe(computedPct, 0.05, "plumbingBaselinePctOfSource must equal 100 * plumbingBaselineLoc / sourceTotalLoc.");
    }

    [Fact]
    public void PlumbingDerivationRowsShouldEnumerateExactlyTheConsumeAndPromoteAreas()
    {
        // AC3 reproducibility: the derivation must not just total correctly — its consumeRows / promoteRows must
        // enumerate EXACTLY the Consume / Promote areas (same ids, same per-row LOC), so the baseline is auditable.
        using JsonDocument doc = LoadCommittedJson();
        JsonElement root = doc.RootElement;
        JsonElement[] areas = root.GetProperty("areas").EnumerateArray().ToArray();
        JsonElement derivation = root.GetProperty("plumbingDerivation");

        AssertDerivationRowsMatch(derivation.GetProperty("consumeRows"), areas, "Consume");
        AssertDerivationRowsMatch(derivation.GetProperty("promoteRows"), areas, "Promote");
    }

    [Fact]
    public void AddendumFirstPassShouldBeExplicitlyConfirmedOrCorrected()
    {
        // AC3: the addendum's first-pass ~18,000 (~50%) figure must be EXPLICITLY confirmed or corrected; a
        // correction must carry a one-line "why" so SM-1's denominator stays honest and reproducible.
        using JsonDocument doc = LoadCommittedJson();
        JsonElement addendum = doc.RootElement.GetProperty("plumbingDerivation").GetProperty("addendumFirstPass");

        string verdict = addendum.GetProperty("verdict").GetString()!;
        new[] { "confirmed", "corrected" }.ShouldContain(verdict, "addendumFirstPass.verdict must be 'confirmed' or 'corrected' (AC3).");

        if (verdict == "corrected")
        {
            addendum.GetProperty("why").GetString()
                .ShouldNotBeNullOrWhiteSpace("A corrected addendum first-pass must record a one-line 'why' (AC3 honesty-of-measurement).");
        }
    }

    [Fact]
    public void InventoryShouldRecordVersioningConventionAndLeaveOpenQuestionsOpen()
    {
        // AC4 + Dev-notes guardrail: the artifact records its own immutability/versioning convention, and does NOT
        // pre-decide OQ-1 (landing zone) / OQ-2 (SM-1 target) / OQ-3 (promote-later boundary).
        using JsonDocument doc = LoadCommittedJson();
        JsonElement root = doc.RootElement;

        root.GetProperty("versioningConvention").GetString()
            .ShouldNotBeNullOrWhiteSpace("The inventory must record its own versioning convention (AC4).");

        string openQuestions = string.Join(
            " ",
            root.GetProperty("openQuestionsNotResolvedHere").EnumerateArray().Select(e => e.GetString()));

        foreach (string oq in new[] { "OQ-1", "OQ-2", "OQ-3" })
        {
            openQuestions.ShouldContain(oq, Case.Sensitive, $"{oq} must be recorded as not-resolved-here (Dev-notes guardrail: do not pre-decide open questions).");
        }
    }

    [Fact]
    public void ConsumePromoteCrossReferencesShouldUseWellFormedFrAndStoryIdentifiers()
    {
        // AC2: every Consume/Promote row cross-references a governing FR and an owning Epic 2/3 story. Beyond
        // non-empty (already asserted), the identifiers must be well-formed (FR-<n> and <epic>.<story>).
        foreach (JsonElement area in LoadAreas())
        {
            string classification = area.GetProperty("classification").GetString()!;
            if (classification is not ("Consume" or "Promote"))
            {
                continue;
            }

            string id = area.GetProperty("areaId").GetString()!;
            string fr = area.GetProperty("fr").GetString()!;
            string owningStory = area.GetProperty("owningStory").GetString()!;

            Regex.IsMatch(fr, "^FR-[0-9]+$").ShouldBeTrue($"{classification} area '{id}' FR '{fr}' must be a well-formed FR identifier (AC2).");
            Regex.IsMatch(owningStory, "^[0-9]+\\.[0-9]+$").ShouldBeTrue($"{classification} area '{id}' owningStory '{owningStory}' must be a well-formed Epic.Story identifier (AC2).");
        }
    }

    private static void AssertDerivationRowsMatch(JsonElement derivationRows, JsonElement[] areas, string classification)
    {
        var actual = areas
            .Where(a => a.GetProperty("classification").GetString() == classification)
            .ToDictionary(a => a.GetProperty("areaId").GetString()!, a => a.GetProperty("approxLoc").GetInt32());

        var derived = derivationRows.EnumerateArray()
            .ToDictionary(r => r.GetProperty("areaId").GetString()!, r => r.GetProperty("approxLoc").GetInt32());

        string[] derivedIds = derived.Keys.OrderBy(k => k).ToArray();
        string[] actualIds = actual.Keys.OrderBy(k => k).ToArray();
        derivedIds.SequenceEqual(actualIds).ShouldBeTrue(
            $"plumbingDerivation must list exactly the {classification} areas (AC3 reproducibility). "
            + $"Derived=[{string.Join(",", derivedIds)}] Actual=[{string.Join(",", actualIds)}]");

        foreach ((string id, int loc) in derived)
        {
            loc.ShouldBe(actual[id], $"Derivation row '{id}' LOC must match the {classification} area's approxLoc.");
        }
    }

    /// <summary>
    /// Resolves an inventory path spec to the actual .cs files it covers, honouring the same glob conventions the
    /// inventory uses: a <c>/**</c> suffix matches every .cs under the directory recursively, a <c>/*.cs</c> suffix
    /// matches direct-child .cs files only, and any other value is an explicit file. obj/bin outputs are excluded to
    /// match the recorded counting method.
    /// </summary>
    private static IEnumerable<string> ResolveCsFiles(string repoRoot, string spec)
    {
        static string Normalize(string p) => p.Replace('/', Path.DirectorySeparatorChar);

        IEnumerable<string> matches;
        if (spec.EndsWith("/**", StringComparison.Ordinal))
        {
            string dir = Path.Combine(repoRoot, Normalize(spec[..^3]));
            matches = Directory.Exists(dir)
                ? Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                : [];
        }
        else if (spec.EndsWith("/*.cs", StringComparison.Ordinal))
        {
            string dir = Path.Combine(repoRoot, Normalize(spec[..^5]));
            matches = Directory.Exists(dir)
                ? Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
                : [];
        }
        else
        {
            string file = Path.Combine(repoRoot, Normalize(spec));
            matches = File.Exists(file) ? [file] : [];
        }

        return matches
            .Select(Path.GetFullPath)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static JsonElement[] LoadAreas()
    {
        // Clone each element so it survives the JsonDocument being disposed (detached copies).
        using JsonDocument doc = LoadCommittedJson();
        return doc.RootElement.GetProperty("areas").EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    // True when the signed inventory changeLog or its mechanically bound Story 6.2 supplement carries an entry that
    // targets this area AND references the specific now-empty path spec. Matching on the spec's literal prefix keeps
    // the tolerance scoped to the exact consumed glob, so an unrelated stale sibling glob still fails.
    private static bool ChangeLogAccountsForConsumedSpec(string areaId, string spec)
    {
        string specPrefix = SpecLiteralPrefix(spec);
        using JsonDocument doc = LoadCommittedJson();
        IEnumerable<JsonElement> entries = doc.RootElement.GetProperty("changeLog")
            .EnumerateArray()
            .Select(entry => entry.Clone())
            .Concat(LoadStory62DispositionEntries());
        return entries
            .Where(e => e.TryGetProperty("areaId", out JsonElement a) && a.GetString() == areaId)
            .Any(e => EntryReferencesPath(e, specPrefix));
    }

    private static JsonElement[] LoadStory62DispositionEntries()
    {
        string repositoryRoot = FindRepositoryRoot();
        string artifactPath = Path.Combine(ReleaseEvidenceDirectory(), Story62DispositionFileName);
        File.Exists(artifactPath).ShouldBeTrue($"Expected Story 6.2 disposition file at '{artifactPath}'.");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        JsonElement root = document.RootElement;
        root.GetProperty("artifactVersion").GetString().ShouldBe("consume-promote-keep-story-6-2-disposition-v1");
        root.GetProperty("story").GetString().ShouldBe("6.2");

        JsonElement baseInventory = root.GetProperty("baseInventory");
        baseInventory.GetProperty("path").GetString().ShouldBe($"docs/release-evidence/{InventoryFileName}");
        string inventoryPath = Path.Combine(repositoryRoot, baseInventory.GetProperty("path").GetString()!);
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inventoryPath))).ToLowerInvariant()
            .ShouldBe(baseInventory.GetProperty("sha256").GetString());

        JsonElement verification = root.GetProperty("sourceVerification");
        string baseline = verification.GetProperty("baseline").GetString()!;
        string candidate = verification.GetProperty("candidate").GetString()!;
        verification.GetProperty("result").GetString().ShouldBe("verified");
        string[] recordedDeletedPaths =
        [
            .. verification.GetProperty("deletedPaths")
                .EnumerateArray()
                .Select(path => path.GetString()!)
                .Order(StringComparer.Ordinal),
        ];
        string[] actualDeletedPaths = RunGit(
                repositoryRoot,
                "diff",
                "--diff-filter=D",
                "--name-only",
                baseline,
                candidate,
                "--",
                "src/Hexalith.Conversations.ServiceDefaults")
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Order(StringComparer.Ordinal)
            .ToArray();
        actualDeletedPaths.ShouldBe(recordedDeletedPaths);

        return root.GetProperty("changeLog").EnumerateArray().Select(entry => entry.Clone()).ToArray();
    }

    // The fixed (pre-wildcard) portion of a path spec — e.g. "src/Foo/Bar/**" → "src/Foo/Bar/". Used to confirm a
    // changeLog entry names the consumed glob without depending on the exact wildcard text.
    private static string SpecLiteralPrefix(string spec)
    {
        int wildcard = spec.IndexOfAny(['*', '?']);
        return wildcard < 0 ? spec : spec[..wildcard];
    }

    // True when any text field of the changeLog entry mentions the consumed spec's literal prefix.
    private static bool EntryReferencesPath(JsonElement entry, string specPrefix)
    {
        if (specPrefix.Length == 0)
        {
            return false;
        }

        foreach (JsonProperty property in entry.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String
                && property.Value.GetString()!.Contains(specPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static JsonDocument LoadCommittedJson()
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), InventoryFileName);
        File.Exists(path).ShouldBeTrue($"Expected committed inventory file at '{path}'.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string RunGit(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)!;
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(milliseconds: 120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} did not complete within 120 seconds.");
        }

        string output = outputTask.GetAwaiter().GetResult();
        string error = errorTask.GetAwaiter().GetResult();
        process.ExitCode.ShouldBe(0, $"git {string.Join(' ', arguments)} failed: {error}");
        return output;
    }

    private static string ReleaseEvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "docs", "release-evidence");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
