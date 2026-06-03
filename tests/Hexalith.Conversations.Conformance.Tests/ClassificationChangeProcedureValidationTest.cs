// <copyright file="ClassificationChangeProcedureValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.5 (AC1–AC5) — validates the COMMITTED classification dispute-resolution + reclassification
/// escape-hatch procedure on disk: <c>docs/release-evidence/classification-change-procedure-v1.json</c> (+ <c>.md</c>),
/// the procedure that governs how <c>consume-promote-keep-inventory-v1.json</c>'s append-only <c>changeLog</c> is
/// amended under FR-2.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors <see cref="ConsumePromoteKeepInventoryValidationTest"/>: repo-root discovery → re-read the committed JSON →
/// assert STRUCTURAL / governance invariants, never re-deriving the inventory's hand-curated per-area LOC. It reads a
/// STATIC, never-regenerated artifact, so it is not exposed to the pre-existing snapshot-regeneration test-isolation
/// race noted in the Story 1.5 Dev Notes.
/// </para>
/// <para>
/// The teeth (AC2/AC5e): each <c>reclassification</c> worked example is folded over a detached COPY of the accepted
/// inventory areas, the full FR-2 invariant is re-checked on the post-fold set, and the example's stated
/// <c>expectedPlumbingAfter</c> is compared against the recomputed Σ(Consume+Promote). A real computation on real
/// data — which is why every worked example must target a REAL <c>areaId</c>.
/// </para>
/// <para>
/// Content-safety is scoped exactly as the inventory validator (AC5g): the artifact legitimately NAMES SDK / Commons
/// capabilities (EventStore, TypeMapper, NameTypeMapper) — that is its purpose. Only payload secrets, drive paths, and
/// provider IDs are forbidden.
/// </para>
/// </remarks>
public sealed class ClassificationChangeProcedureValidationTest
{
    private const string ProcedureFileName = "classification-change-procedure-v1.json";
    private const string ProcedureMarkdownFileName = "classification-change-procedure-v1.md";
    private const string InventoryFileName = "consume-promote-keep-inventory-v1.json";

    private static readonly string[] ValidClassifications = ["Consume", "Promote", "Keep"];

    // Scoped content-safety: NO capability-name ban (the artifact exists to record them). Only secrets / host drive
    // paths / provider IDs must never appear. Identical set to the inventory validator (AC5g).
    private static readonly string[] ForbiddenFragments =
    [
        "C:\\",
        "D:\\",
        "BEGIN RSA PRIVATE KEY",
        "BEGIN PRIVATE KEY",
        "password=",
        "secret=",
    ];

    // -------------------------------------------------------------------------------------------------------------
    // (a) Artifacts committed + accepted + governance fields set.
    // -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void CommittedProcedureShouldBeAcceptedAndDeclareGovernance()
    {
        using JsonDocument doc = LoadProcedureJson();
        JsonElement root = doc.RootElement;

        root.GetProperty("artifact").GetString().ShouldBe("classification-change-procedure");
        root.GetProperty("version").GetInt32().ShouldBe(1);
        root.GetProperty("status").GetString().ShouldBe("accepted");
        root.GetProperty("acceptedDate").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("governsArtifact").GetString().ShouldBe(InventoryFileName, "The procedure must declare the inventory it governs (AC4 discoverability).");
        root.GetProperty("governingFr").GetString().ShouldBe("FR-2", "The procedure governs FR-2 (AC4).");
    }

    [Fact]
    public void BothProcedureArtifactsAndTheGovernedInventoryShouldBeCommitted()
    {
        string dir = ReleaseEvidenceDirectory();

        File.Exists(Path.Combine(dir, ProcedureFileName)).ShouldBeTrue("The machine-readable procedure .json must be committed (AC5).");
        string mdPath = Path.Combine(dir, ProcedureMarkdownFileName);
        File.Exists(mdPath).ShouldBeTrue("The human-readable procedure .md must be committed alongside the .json (AC5).");
        File.ReadAllText(mdPath).Trim().ShouldNotBeNullOrWhiteSpace("The human-readable procedure .md must not be empty.");

        File.Exists(Path.Combine(dir, InventoryFileName)).ShouldBeTrue("The governed inventory .json must exist (the procedure is meaningless without it).");
    }

    // -------------------------------------------------------------------------------------------------------------
    // (b) changeLogEntrySchema defines required fields for BOTH entry types.
    // -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ChangeLogEntrySchemaShouldDefineBothEntryTypes()
    {
        using JsonDocument doc = LoadProcedureJson();
        JsonElement schema = doc.RootElement.GetProperty("changeLogEntrySchema");

        string[] challengeRequired = RequiredFields(schema, "challenge");
        foreach (string field in new[] { "entryId", "type", "areaId", "date", "raisedBy", "rationale", "resolution", "resolutionRationale" })
        {
            challengeRequired.ShouldContain(field, $"challenge schema must require '{field}' (AC1).");
        }

        string[] resolutionEnum = schema.GetProperty("challenge").GetProperty("resolutionEnum").EnumerateArray().Select(e => e.GetString()!).ToArray();
        resolutionEnum.ShouldBe(["upheld", "reclassified"], ignoreOrder: true, "challenge resolution enum must be {upheld, reclassified} (AC1).");

        string[] reclassRequired = RequiredFields(schema, "reclassification");
        foreach (string field in new[] { "entryId", "type", "areaId", "date", "reclassifiedBy", "from", "to", "rationale" })
        {
            reclassRequired.ShouldContain(field, $"reclassification schema must require '{field}' (AC2).");
        }

        string[] classificationEnum = schema.GetProperty("reclassification").GetProperty("classificationEnum").EnumerateArray().Select(e => e.GetString()!).ToArray();
        classificationEnum.ShouldBe(ValidClassifications, ignoreOrder: true, "reclassification classification enum must be {Consume, Promote, Keep} (AC2).");
    }

    // -------------------------------------------------------------------------------------------------------------
    // (c) Every worked example conforms to its type's schema; at least one upheld challenge + one reclassification.
    // -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void WorkedExamplesShouldIncludeAnUpheldChallengeAndAReclassification()
    {
        JsonElement[] examples = LoadWorkedExamples();
        examples.ShouldNotBeEmpty("At least one challenge and one reclassification worked example are required (AC5).");

        examples.Any(e => e.GetProperty("type").GetString() == "challenge" && e.GetProperty("resolution").GetString() == "upheld")
            .ShouldBeTrue("At least one UPHELD challenge worked example is required (AC1/AC5).");
        examples.Any(e => e.GetProperty("type").GetString() == "reclassification")
            .ShouldBeTrue("At least one reclassification worked example is required (AC2/AC5).");
    }

    [Fact]
    public void EveryWorkedExampleShouldConformToItsTypeSchema()
    {
        using JsonDocument doc = LoadProcedureJson();
        JsonElement schema = doc.RootElement.GetProperty("changeLogEntrySchema");

        foreach (JsonElement example in LoadWorkedExamples())
        {
            example.GetProperty("example").GetBoolean().ShouldBeTrue("Every worked example must be flagged example:true (illustrative, not applied).");
            string entryId = example.GetProperty("entryId").GetString()!;
            string type = example.GetProperty("type").GetString()!;

            AssertEntryConformsToSchema(example, schema, $"worked example '{entryId}'");
        }
    }

    // -------------------------------------------------------------------------------------------------------------
    // (d) Every worked-example / changeLog entry targets a REAL areaId in the accepted inventory.
    // -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void EveryEntryShouldTargetARealInventoryAreaId()
    {
        HashSet<string> realAreaIds = InventoryAreaIds();

        foreach (JsonElement example in LoadWorkedExamples())
        {
            string areaId = example.GetProperty("areaId").GetString()!;
            realAreaIds.ShouldContain(areaId, $"Worked example '{example.GetProperty("entryId").GetString()}' targets areaId '{areaId}' which does not exist in the accepted inventory (no dangling reference, AC5d).");
        }

        foreach (JsonElement entry in LoadInventoryChangeLog())
        {
            string areaId = entry.GetProperty("areaId").GetString()!;
            realAreaIds.ShouldContain(areaId, $"Inventory changeLog entry targets areaId '{areaId}' which does not exist in the inventory (AC5d).");
        }
    }

    // -------------------------------------------------------------------------------------------------------------
    // (e) TEETH — fold each reclassification example over a copy of the accepted areas, re-check FR-2, recompute plumbing.
    // -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FoldingEachReclassificationExampleShouldPreserveFr2InvariantAndRecomputePlumbing()
    {
        // Accepted baseline, read fresh from the (byte-immutable) inventory.
        JsonElement[] acceptedAreas = LoadInventoryAreas();
        var acceptedClassification = acceptedAreas.ToDictionary(
            a => a.GetProperty("areaId").GetString()!,
            a => a.GetProperty("classification").GetString()!);
        var approxLoc = acceptedAreas.ToDictionary(
            a => a.GetProperty("areaId").GetString()!,
            a => a.GetProperty("approxLoc").GetInt32());
        int sourceTotal = acceptedAreas.Sum(a => a.GetProperty("approxLoc").GetInt32());

        int reclassExamples = 0;
        foreach (JsonElement example in LoadWorkedExamples())
        {
            if (example.GetProperty("type").GetString() != "reclassification")
            {
                continue;
            }

            reclassExamples++;
            string entryId = example.GetProperty("entryId").GetString()!;
            string areaId = example.GetProperty("areaId").GetString()!;
            string from = example.GetProperty("from").GetString()!;
            string to = example.GetProperty("to").GetString()!;

            // The reclassification is applied OFF the accepted baseline: 'from' must equal the accepted call.
            acceptedClassification[areaId].ShouldBe(from, $"{entryId}: 'from' must equal the area's accepted classification (a reclassification is applied off the Story 1.4 baseline).");

            // Fold over a COPY of the accepted classification map.
            var folded = new Dictionary<string, string>(acceptedClassification, StringComparer.Ordinal)
            {
                [areaId] = to,
            };

            // Re-check the FULL FR-2 invariant on the post-fold set.
            folded.Count.ShouldBe(acceptedClassification.Count, $"{entryId}: a relabel must not add or drop an area (FR-2: every area exactly once).");
            foreach ((string id, string classification) in folded)
            {
                ValidClassifications.ShouldContain(classification, $"{entryId}: post-fold area '{id}' carries an invalid/dual classification '{classification}'.");
            }

            // LOC reconciliation is invariant under a relabel (no re-measurement).
            folded.Keys.Sum(id => approxLoc[id]).ShouldBe(sourceTotal, $"{entryId}: per-area LOC must still reconcile to sourceTotalLoc after the fold (relabel never re-measures).");

            // Recompute plumbing = Σ(Consume) + Σ(Promote) over the post-fold set.
            int recomputed = folded.Where(kv => kv.Value is "Consume" or "Promote").Sum(kv => approxLoc[kv.Key]);
            example.TryGetProperty("expectedPlumbingAfter", out JsonElement expected).ShouldBeTrue($"{entryId}: a reclassification worked example must record expectedPlumbingAfter (the teeth value).");
            expected.GetInt32().ShouldBe(recomputed, $"{entryId}: expectedPlumbingAfter must equal the recomputed Σ(Consume+Promote) over the post-fold set (AC2/AC5e).");

            // If the example flips a promoteLaterCandidate to Promote, its rationale must record the OQ-3 boundary crossing.
            JsonElement acceptedArea = acceptedAreas.First(a => a.GetProperty("areaId").GetString() == areaId);
            if (to == "Promote" && acceptedArea.TryGetProperty("promoteLaterCandidate", out JsonElement flag) && flag.GetBoolean())
            {
                example.GetProperty("rationale").GetString()!.ShouldContain("OQ-3", Case.Sensitive, $"{entryId}: flipping a promoteLaterCandidate to Promote must record that OQ-3's boundary was crossed deliberately (AC3).");
            }
        }

        reclassExamples.ShouldBeGreaterThan(0, "At least one reclassification worked example must exercise the fold-and-recheck teeth (AC5e).");
    }

    // -------------------------------------------------------------------------------------------------------------
    // (f) The inventory's OWN changeLog is an array; every REAL entry (currently zero) conforms to the schema.
    // -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void InventoryChangeLogShouldBeAnArrayAndEveryRealEntryShouldConform()
    {
        using JsonDocument inventory = LoadInventoryJson();
        inventory.RootElement.GetProperty("changeLog").ValueKind.ShouldBe(JsonValueKind.Array, "The inventory's changeLog must be an array (the append point Story 1.4 provisioned).");

        using JsonDocument procedure = LoadProcedureJson();
        JsonElement schema = procedure.RootElement.GetProperty("changeLogEntrySchema");

        foreach (JsonElement entry in LoadInventoryChangeLog())
        {
            // Real entries (not example payloads) must conform to the same schema that guards future Epic 2/3 appends.
            AssertEntryConformsToSchema(entry, schema, $"inventory changeLog entry '{EntryLabel(entry)}'");
        }
    }

    // -------------------------------------------------------------------------------------------------------------
    // (g) Scoped content-safety.
    // -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void CommittedProcedureShouldPassScopedContentSafetyScan()
    {
        // Scan BOTH committed procedure artifacts (.json + .md): each is an equally-committed deliverable that could
        // in principle carry a forbidden fragment, so neither is exempt from the AC5g scope.
        foreach (string fileName in new[] { ProcedureFileName, ProcedureMarkdownFileName })
        {
            string raw = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), fileName));

            foreach (string fragment in ForbiddenFragments)
            {
                raw.ShouldNotContain(fragment, Case.Insensitive, $"Procedure artifact '{fileName}' must not contain forbidden fragment '{fragment}'.");
            }
        }
    }

    // =============================================================================================================
    // QA gap-coverage facts (qa-generate-e2e-tests). The nine facts above cover AC5(a)–(g) head-on; the following
    // close AC1–AC4 / AC5 claims the original validator left unasserted. All remain read-only structural / cross-
    // artifact consistency checks — they assert relationships the procedure claims about ITSELF and the inventory it
    // governs, never re-deriving the hand-curated per-area LOC.
    // =============================================================================================================

    // AC5 ("worked examples are illustrative only — NOT applied to the accepted inventory"): the strongest durable
    // machine check of "not applied" is that no worked-example entryId leaked into the inventory's REAL changeLog,
    // and the artifact declares the not-applied intent in its top-level note.
    [Fact]
    public void WorkedExampleEntriesShouldNotLeakIntoTheRealInventoryChangeLog()
    {
        HashSet<string> exampleIds = LoadWorkedExamples()
            .Select(e => e.GetProperty("entryId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (JsonElement entry in LoadInventoryChangeLog())
        {
            string entryId = EntryLabel(entry);
            exampleIds.ShouldNotContain(entryId, $"Worked example '{entryId}' was copied into the inventory's REAL changeLog — worked examples are illustrative ONLY and must never be applied (AC5).");
        }

        using JsonDocument doc = LoadProcedureJson();
        doc.RootElement.GetProperty("note").GetString()!
            .ShouldContain("not applied", Case.Insensitive, "The procedure must declare its worked examples illustrative / NOT applied to the accepted inventory (AC5).");
    }

    // AC2 / AC5e honesty-of-measurement: the procedure carries a frozen acceptedInventoryBaseline (the reference
    // values a recompute folds off). It must match the inventory it governs byte-for-byte in spirit, or the fold
    // teeth measure against a stale baseline and the no-silent-drift guarantee is hollow.
    [Fact]
    public void ProcedureRecordedBaselineShouldMatchTheGovernedInventory()
    {
        using JsonDocument procedure = LoadProcedureJson();
        using JsonDocument inventory = LoadInventoryJson();

        JsonElement baseline = procedure.RootElement.GetProperty("acceptedInventoryBaseline");
        JsonElement inv = inventory.RootElement;

        baseline.GetProperty("baselineCommit").GetString()
            .ShouldBe(inv.GetProperty("baselineCommit").GetString(), "The procedure's recorded baselineCommit must match the governed inventory's (no stale reference).");
        baseline.GetProperty("plumbingBaselineLoc").GetInt32()
            .ShouldBe(inv.GetProperty("plumbingBaselineLoc").GetInt32(), "The procedure's recorded plumbingBaselineLoc must match the governed inventory's (SM-1 denominator parity).");
        baseline.GetProperty("sourceTotalLoc").GetInt32()
            .ShouldBe(inv.GetProperty("sourceTotalLoc").GetInt32(), "The procedure's recorded sourceTotalLoc must match the governed inventory's (relabel never re-measures source).");
    }

    // AC2 ("reclassification only flips the classification label, never the area's approxLoc or paths"): a
    // reclassification entry is a fate correction, not a re-measurement. It must NOT carry an approxLoc/paths
    // override on the entry — LOC is fixed at baselineCommit and lives only on the inventory row.
    [Fact]
    public void ReclassificationEntriesShouldNotOverrideApproxLocOrPaths()
    {
        IEnumerable<JsonElement> reclassEntries = LoadWorkedExamples()
            .Concat(LoadInventoryChangeLog())
            .Where(e => e.GetProperty("type").GetString() == "reclassification");

        foreach (JsonElement entry in reclassEntries)
        {
            string entryId = EntryLabel(entry);
            entry.TryGetProperty("approxLoc", out _).ShouldBeFalse($"Reclassification entry '{entryId}' must NOT carry an approxLoc override — a relabel never re-measures LOC (AC2).");
            entry.TryGetProperty("paths", out _).ShouldBeFalse($"Reclassification entry '{entryId}' must NOT carry a paths override — a relabel never re-scopes the area (AC2).");
        }
    }

    // AC1 ("a reclassified challenge IS a reclassification and obeys the same logging rules"): every challenge whose
    // resolution is 'reclassified' must be accompanied by a 'reclassification' entry targeting the same areaId.
    // Vacuous in this pilot (the only challenge is upheld) — guards the cross-entry consistency future appends need.
    [Fact]
    public void EveryReclassifiedChallengeShouldHaveAMatchingReclassificationEntry()
    {
        JsonElement[] allEntries = LoadWorkedExamples().Concat(LoadInventoryChangeLog()).ToArray();

        var reclassAreaIds = allEntries
            .Where(e => e.GetProperty("type").GetString() == "reclassification")
            .Select(e => e.GetProperty("areaId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (JsonElement challenge in allEntries.Where(e => e.GetProperty("type").GetString() == "challenge"))
        {
            if (challenge.GetProperty("resolution").GetString() == "reclassified")
            {
                string areaId = challenge.GetProperty("areaId").GetString()!;
                reclassAreaIds.ShouldContain(areaId, $"Challenge '{EntryLabel(challenge)}' is resolved 'reclassified' but no matching reclassification entry targets areaId '{areaId}' (AC1: a reclassified challenge IS a reclassification).");
            }
        }
    }

    // AC2 no-silent-change, for REAL entries: every real reclassification entry must already be applied — the live
    // inventory area's classification equals the entry's 'to'. Vacuous now (changeLog is []); the rule bites the
    // moment Epic 2/3 appends a reclassification, catching an entry logged but never applied (or vice-versa).
    [Fact]
    public void EveryRealReclassificationEntryShouldBeAppliedToTheLiveClassification()
    {
        var liveClassification = LoadInventoryAreas().ToDictionary(
            a => a.GetProperty("areaId").GetString()!,
            a => a.GetProperty("classification").GetString()!,
            StringComparer.Ordinal);

        foreach (JsonElement entry in LoadInventoryChangeLog())
        {
            if (entry.GetProperty("type").GetString() != "reclassification")
            {
                continue;
            }

            string areaId = entry.GetProperty("areaId").GetString()!;
            string to = entry.GetProperty("to").GetString()!;
            liveClassification[areaId].ShouldBe(to, $"Real reclassification '{EntryLabel(entry)}' logs to='{to}' but the live inventory row carries '{liveClassification[areaId]}' — a logged reclassification must be applied (AC2 no-silent-change).");
        }
    }

    // AC4 discoverability ("a reader landing on either artifact finds the other"): the procedure→inventory link is
    // already asserted (governsArtifact). Assert the inventory→Story-1.5 back-reference too, so discoverability is
    // bidirectional without editing the byte-immutable inventory.
    [Fact]
    public void GovernedInventoryShouldBackReferenceStoryOneFiveForBidirectionalDiscoverability()
    {
        using JsonDocument inventory = LoadInventoryJson();
        inventory.RootElement.GetProperty("versioningConvention").GetString()!
            .ShouldContain("1.5", Case.Sensitive, "The inventory's versioningConvention must forward-reference Story 1.5 so the procedure is discoverable from the inventory (AC4 bidirectional discoverability).");
    }

    // AC4 ("copy-pasteable entry template"): the procedure ships a reusable template for an Epic 2/3 story to follow.
    // Assert it provides both entry-type shapes, each carrying every field its schema requires.
    [Fact]
    public void EntryTemplateShouldProvideCopyPasteableShapesForBothTypes()
    {
        using JsonDocument doc = LoadProcedureJson();
        JsonElement root = doc.RootElement;
        JsonElement template = root.GetProperty("entryTemplate");
        JsonElement schema = root.GetProperty("changeLogEntrySchema");

        foreach (string type in new[] { "challenge", "reclassification" })
        {
            template.TryGetProperty(type, out JsonElement typeTemplate).ShouldBeTrue($"entryTemplate must provide a copy-pasteable '{type}' shape (AC4).");
            foreach (string field in RequiredFields(schema, type))
            {
                typeTemplate.TryGetProperty(field, out _).ShouldBeTrue($"entryTemplate.{type} must include the required field '{field}' so a story can fill it in (AC4).");
            }
        }
    }

    // AC4 ("the procedure document spells out the 5 steps ... an implementing story can follow"): the .md must carry
    // the documented procedure, not merely be non-empty. Assert the load-bearing structural content is present.
    [Fact]
    public void ProcedureMarkdownShouldDocumentTheFiveStepProcedureAndCanonicalLog()
    {
        string md = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), ProcedureMarkdownFileName));

        foreach (string step in new[] { "Step 1", "Step 2", "Step 3", "Step 4", "Step 5" })
        {
            md.ShouldContain(step, Case.Insensitive, $"The procedure .md must document '{step}' of the 5-step procedure (AC4).");
        }

        md.ShouldContain("changeLog", Case.Insensitive, "The procedure .md must name the single canonical log (the inventory's changeLog) (AC4).");
        md.ShouldContain("append-only", Case.Insensitive, "The procedure .md must document the append-only / no-silent-edit rule (AC4).");
        md.ShouldContain("plumbingBaselineLoc", Case.Insensitive, "The procedure .md must document the recompute-plumbing-on-Keep<->plumbing-flip rule (AC4).");

        foreach (string type in new[] { "challenge", "reclassification" })
        {
            md.ShouldContain(type, Case.Insensitive, $"The procedure .md must document the '{type}' entry type (AC4 schema).");
        }
    }

    // -------------------------------------------------------------------------------------------------------------
    // Helpers (schema-conformance + load).
    // -------------------------------------------------------------------------------------------------------------

    private static void AssertEntryConformsToSchema(JsonElement entry, JsonElement schema, string label)
    {
        string type = entry.GetProperty("type").GetString()!;
        new[] { "challenge", "reclassification" }.ShouldContain(type, $"{label}: unknown entry type '{type}'.");

        foreach (string field in RequiredFields(schema, type))
        {
            entry.TryGetProperty(field, out JsonElement value).ShouldBeTrue($"{label}: missing required '{type}' field '{field}'.");
            (value.ValueKind != JsonValueKind.Null).ShouldBeTrue($"{label}: required field '{field}' must not be null.");
        }

        if (type == "challenge")
        {
            string resolution = entry.GetProperty("resolution").GetString()!;
            string[] resolutionEnum = schema.GetProperty("challenge").GetProperty("resolutionEnum").EnumerateArray().Select(e => e.GetString()!).ToArray();
            resolutionEnum.ShouldContain(resolution, $"{label}: resolution '{resolution}' must be one of {{{string.Join(", ", resolutionEnum)}}}.");
        }
        else
        {
            string from = entry.GetProperty("from").GetString()!;
            string to = entry.GetProperty("to").GetString()!;
            ValidClassifications.ShouldContain(from, $"{label}: 'from' classification '{from}' is invalid.");
            ValidClassifications.ShouldContain(to, $"{label}: 'to' classification '{to}' is invalid.");
            from.ShouldNotBe(to, $"{label}: a reclassification must change the label (from != to).");

            // Honour the pattern the schema itself declares (no schema/validator drift) — fall back to the canonical
            // <epic>.<story> shape if the schema omits it.
            string reclassifiedByPattern = schema.GetProperty("reclassification").TryGetProperty("reclassifiedByPattern", out JsonElement pattern)
                ? pattern.GetString()!
                : "^[0-9]+\\.[0-9]+$";
            Regex.IsMatch(entry.GetProperty("reclassifiedBy").GetString()!, reclassifiedByPattern)
                .ShouldBeTrue($"{label}: reclassifiedBy must match the schema's reclassifiedByPattern ('{reclassifiedByPattern}', a well-formed <epic>.<story> identifier).");
        }
    }

    private static string[] RequiredFields(JsonElement schema, string type)
        => schema.GetProperty(type).GetProperty("required").EnumerateArray().Select(e => e.GetString()!).ToArray();

    private static string EntryLabel(JsonElement entry)
        => entry.TryGetProperty("entryId", out JsonElement id) ? id.GetString() ?? "<no-id>" : "<no-id>";

    private static JsonElement[] LoadWorkedExamples()
    {
        using JsonDocument doc = LoadProcedureJson();
        return doc.RootElement.GetProperty("workedExamples").EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    private static JsonElement[] LoadInventoryAreas()
    {
        using JsonDocument doc = LoadInventoryJson();
        return doc.RootElement.GetProperty("areas").EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    private static JsonElement[] LoadInventoryChangeLog()
    {
        using JsonDocument doc = LoadInventoryJson();
        return doc.RootElement.GetProperty("changeLog").EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    private static HashSet<string> InventoryAreaIds()
        => LoadInventoryAreas().Select(a => a.GetProperty("areaId").GetString()!).ToHashSet(StringComparer.Ordinal);

    private static JsonDocument LoadProcedureJson() => LoadCommittedJson(ProcedureFileName);

    private static JsonDocument LoadInventoryJson() => LoadCommittedJson(InventoryFileName);

    private static JsonDocument LoadCommittedJson(string fileName)
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), fileName);
        File.Exists(path).ShouldBeTrue($"Expected committed file at '{path}'.");
        return JsonDocument.Parse(File.ReadAllText(path));
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
