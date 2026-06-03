// <copyright file="AtRiskTestRegisterGenerationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.3 (AC5) — generates and self-validates the committed at-risk test register at
/// <c>docs/release-evidence/at-risk-test-register-v1.json</c>.
/// </summary>
/// <remarks>
/// The register is the central Story 1.3 deliverable and the SEED of the FR-20 removed-test justification
/// ledger that Story 5.2 reconciles. It maps every internal-coupled (at-risk) test — and, where a file is
/// split per-assertion, each at-risk assertion group — to a classification in
/// {re-express, never delete | re-express | plumbing-only-retire | coupled-by-design-retarget-in-owning-story},
/// with the coupling, rationale, the owning Epic 2/3 story (for every retire/retarget entry), and the
/// re-expression artifact (for every re-express entry). It also records the re-expressions this story added
/// to the oracle and folds in the two Story 1.2 carry-forward gaps.
///
/// Mirrors <see cref="PublicContractShapeSnapshotGenerationTest"/> and <see cref="ReleaseConformanceArtifactGenerationTest"/>:
/// repo-root discovery, deterministic indented-JSON write into <c>docs/release-evidence/</c>, then re-read +
/// re-validate + content-safety scan in the same pass, so the committed file always round-trips and never
/// leaks substrate mechanics.
/// </remarks>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class AtRiskTestRegisterGenerationTest
{
    private const string ReExpressNeverDelete = "re-express, never delete";
    private const string ReExpress = "re-express";
    private const string PlumbingOnlyRetire = "plumbing-only-retire";
    private const string CoupledRetarget = "coupled-by-design-retarget-in-owning-story";

    private static readonly JsonSerializerOptions RegisterOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public void RegisterShouldEnumerateEveryAtRiskTestDeterministically()
    {
        AtRiskTestRegisterV1 first = BuildRegister();
        AtRiskTestRegisterV1 second = BuildRegister();

        first.Tests.ShouldNotBeEmpty();
        first.ReExpressionsAddedByThisStory.ShouldNotBeEmpty();
        first.CarryForwardsFromStory12.ShouldNotBeEmpty();
        JsonSerializer.Serialize(first, RegisterOptions)
            .ShouldBe(JsonSerializer.Serialize(second, RegisterOptions));
    }

    [Fact]
    public void EveryRetireOrRetargetEntryShouldNameItsOwningStory()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        foreach (AtRiskTestEntry entry in register.Tests)
        {
            if (entry.Classification is PlumbingOnlyRetire or CoupledRetarget)
            {
                entry.OwningStory.ShouldNotBeNullOrWhiteSpace(
                    $"At-risk entry '{entry.File}' is '{entry.Classification}' and must name an owning Epic 2/3 story.");
            }

            if (entry.Classification is ReExpress or ReExpressNeverDelete)
            {
                entry.ReExpressedAs.ShouldNotBeNullOrWhiteSpace(
                    $"At-risk entry '{entry.File}' is '{entry.Classification}' and must name its re-expression artifact.");
            }

            entry.BaselineGreen.ShouldBeTrue($"At-risk entry '{entry.File}' must be green on the baseline commit.");
        }
    }

    [Fact]
    public void EveryReExpressionShouldBeGreenAndAnchored()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        foreach (ReExpressionEntry entry in register.ReExpressionsAddedByThisStory)
        {
            entry.BaselineGreen.ShouldBeTrue();
            entry.File.ShouldNotBeNullOrWhiteSpace();
            entry.PublicSurface.ShouldNotBeNullOrWhiteSpace();
            if (entry.Classification == CoupledRetarget)
            {
                entry.OwningStory.ShouldNotBeNullOrWhiteSpace();
            }
        }

        foreach (CarryForwardEntry carryForward in register.CarryForwardsFromStory12)
        {
            carryForward.Disposition.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void EveryStory21StructuralDispositionShouldBeAnchoredAndGreen()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        register.Story21StructuralDispositions.ShouldNotBeEmpty();
        foreach (Story21StructuralDisposition disposition in register.Story21StructuralDispositions)
        {
            disposition.Subject.ShouldNotBeNullOrWhiteSpace();
            disposition.Change.ShouldNotBeNullOrWhiteSpace();
            disposition.Ac.ShouldNotBeNullOrWhiteSpace();
            disposition.OwningStory.ShouldNotBeNullOrWhiteSpace();
            disposition.GreenAfterChange.ShouldBeTrue(
                $"Story 2.1 structural disposition '{disposition.Subject}' must be green after the recorded change.");
        }
    }

    [Fact]
    public void EveryStory22StructuralDispositionShouldBeAnchoredAndGreen()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        register.Story22StructuralDispositions.ShouldNotBeEmpty();
        foreach (Story22StructuralDisposition disposition in register.Story22StructuralDispositions)
        {
            disposition.Subject.ShouldNotBeNullOrWhiteSpace();
            disposition.Change.ShouldNotBeNullOrWhiteSpace();
            disposition.Ac.ShouldNotBeNullOrWhiteSpace();
            disposition.OwningStory.ShouldNotBeNullOrWhiteSpace();
            disposition.GreenAfterChange.ShouldBeTrue(
                $"Story 2.2 structural disposition '{disposition.Subject}' must be green after the recorded change.");
        }
    }

    [Fact]
    public void EveryStory23StructuralDispositionShouldBeAnchoredAndGreen()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        register.Story23StructuralDispositions.ShouldNotBeEmpty();
        foreach (Story23StructuralDisposition disposition in register.Story23StructuralDispositions)
        {
            disposition.Subject.ShouldNotBeNullOrWhiteSpace();
            disposition.Change.ShouldNotBeNullOrWhiteSpace();
            disposition.Ac.ShouldNotBeNullOrWhiteSpace();
            disposition.OwningStory.ShouldNotBeNullOrWhiteSpace();
            disposition.GreenAfterChange.ShouldBeTrue(
                $"Story 2.3 structural disposition '{disposition.Subject}' must be green after the recorded change.");
        }
    }

    [Fact]
    public void EveryStory24StructuralDispositionShouldBeAnchoredAndGreen()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        register.Story24StructuralDispositions.ShouldNotBeEmpty();
        foreach (Story24StructuralDisposition disposition in register.Story24StructuralDispositions)
        {
            disposition.Subject.ShouldNotBeNullOrWhiteSpace();
            disposition.Change.ShouldNotBeNullOrWhiteSpace();
            disposition.Ac.ShouldNotBeNullOrWhiteSpace();
            disposition.OwningStory.ShouldNotBeNullOrWhiteSpace();
            disposition.GreenAfterChange.ShouldBeTrue(
                $"Story 2.4 structural disposition '{disposition.Subject}' must be green after the recorded change.");
        }
    }

    [Fact]
    public void EveryStory25StructuralDispositionShouldBeAnchoredAndGreen()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        register.Story25StructuralDispositions.ShouldNotBeEmpty();
        foreach (Story25StructuralDisposition disposition in register.Story25StructuralDispositions)
        {
            disposition.Subject.ShouldNotBeNullOrWhiteSpace();
            disposition.Change.ShouldNotBeNullOrWhiteSpace();
            disposition.Ac.ShouldNotBeNullOrWhiteSpace();
            disposition.OwningStory.ShouldNotBeNullOrWhiteSpace();
            disposition.GreenAfterChange.ShouldBeTrue(
                $"Story 2.5 structural disposition '{disposition.Subject}' must be green after the recorded change.");
        }
    }

    [Fact]
    public void EveryStory26StructuralDispositionShouldBeAnchoredAndGreen()
    {
        AtRiskTestRegisterV1 register = BuildRegister();

        register.Story26StructuralDispositions.ShouldNotBeEmpty();
        foreach (Story26StructuralDisposition disposition in register.Story26StructuralDispositions)
        {
            disposition.Subject.ShouldNotBeNullOrWhiteSpace();
            disposition.Change.ShouldNotBeNullOrWhiteSpace();
            disposition.Ac.ShouldNotBeNullOrWhiteSpace();
            disposition.OwningStory.ShouldNotBeNullOrWhiteSpace();
            disposition.GreenAfterChange.ShouldBeTrue(
                $"Story 2.6 structural disposition '{disposition.Subject}' must be green after the recorded change.");
        }
    }

    [Fact]
    public void RegisterShouldBeContentSafe()
    {
        // The emitted artifact must contain ONLY public Conversations concepts and test/behavior/type names —
        // never substrate mechanics or host paths. Mirrors the existing release-evidence content-safety scans.
        string[] forbidden =
        [
            "EventStore",
            "snapshot",
            "SignalR",
            "dispatcher",
            "repository",
            "provider-session",
            "poison",
            "C:\\",
            "D:\\",
        ];

        string json = JsonSerializer.Serialize(BuildRegister(), RegisterOptions);

        foreach (string fragment in forbidden)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"At-risk register must not contain forbidden fragment '{fragment}'.");
        }
    }

    [Fact]
    public void GenerateAndSaveAtRiskTestRegisterFile()
    {
        AtRiskTestRegisterV1 register = BuildRegister();
        string json = JsonSerializer.Serialize(register, RegisterOptions);

        string root = FindRepositoryRoot();
        string dir = Path.Combine(root, "docs", "release-evidence");
        string path = Path.Combine(dir, "at-risk-test-register-v1.json");

        Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);

        string readBack = File.ReadAllText(path);
        AtRiskTestRegisterV1? parsed = JsonSerializer.Deserialize<AtRiskTestRegisterV1>(readBack, RegisterOptions);
        parsed.ShouldNotBeNull();
        parsed!.Tests.Count.ShouldBe(register.Tests.Count);
        parsed.BaselineCommit.ShouldBe(register.BaselineCommit);

        // Determinism guard: re-serializing the round-tripped artifact reproduces the committed bytes exactly.
        JsonSerializer.Serialize(parsed, RegisterOptions).ShouldBe(json);
    }

    private static AtRiskTestRegisterV1 BuildRegister()
        => new()
        {
            ArtifactKind = "at-risk-test-register",
            Version = "v1",
            BaselineCommit = "a68c6e3",
            GeneratedBy = $"{nameof(AtRiskTestRegisterGenerationTest)}.{nameof(GenerateAndSaveAtRiskTestRegisterFile)}",
            Description =
                "Story 1.3 at-risk test register. Maps every internal-coupled test (and each at-risk assertion "
                + "group, where a file is split per-assertion) that the Boilerplate Reduction refactor would "
                + "otherwise break for the wrong reason, to a classification with its coupling, rationale, owning "
                + "Epic 2/3 story (for every retire/retarget entry), and re-expression artifact (for every "
                + "re-express entry). This register is the SEED of the FR-20 removed-test justification ledger that "
                + "Story 5.2 reconciles, so no later plumbing-only deletion is unaccounted for, and the input to "
                + "Story 5.1's structural-survivability confirmation. No test is deleted in Story 1.3: re-expressions "
                + "are added to the oracle and the original at-risk tests are kept until their owning story retires "
                + "or retargets them with the code. Regenerate with: dotnet test "
                + "tests/Hexalith.Conversations.Conformance.Tests --filter "
                + "\"FullyQualifiedName~AtRiskTestRegisterGenerationTest\".",
            ClassificationLegend = new ClassificationLegend(
                ReExpressNeverDelete: "Behavior safety net (e.g. the governance audit-pairing test). Re-expressed "
                    + "against the public surface and kept forever; its deletion would silently drop a conformance behavior.",
                ReExpress: "Behavior assertion whose coupling is incidental; re-expressed against the public surface "
                    + "here so the underlying plumbing can move freely in its owning story.",
                PlumbingOnlyRetire: "Assertion with no externally-observable release-gate behavior; retired WITH its "
                    + "code in the named owning Epic 2/3 story. Recorded here so Story 5.2 reconciles every later test-count reduction.",
                CoupledRetarget: "Test that must keep touching live/internal types to do its job; the owning story "
                    + "updates the type reference rather than dropping the test."),
            Tests = BuildTestEntries(),
            ReExpressionsAddedByThisStory = BuildReExpressions(),
            CarryForwardsFromStory12 = BuildCarryForwards(),
            ProjectReferenceDisposition = new ProjectReferenceDisposition(
                Reference: "tests/Hexalith.Conversations.Conformance.Tests -> src/Hexalith.Conversations.Server",
                Classification: CoupledRetarget,
                Rationale: "The public-surface conformance suites and the live characterization tests transitively "
                    + "depend on the Server plumbing assembly. Removing the reference now would break the still-coupled "
                    + "telemetry/status suites, the Story 1.2 live characterization tests, and the Story 1.3 read-surface "
                    + "and idempotency re-expressions. Removal is the owning stories' job, not this story's.",
                TargetEndState: "Public-surface suites no longer transitively depend on the Server plumbing assembly.",
                PathToGetThere: "Retarget the coupled telemetry/status suites in their owning stories (3.3 / 3.2) and "
                    + "the projection/idempotency couplings (2.5 / 2.2); the last owning story removes the reference (or "
                    + "extract a Server-coupled fixtures sub-project). Tracked here so Story 5.1's oracle is structurally survivable.",
                RemovedInThisStory: false,
                OwningStory: "Story 3.3 (last of 2.2 / 2.5 / 3.2 / 3.3 to clear)"),
            Story21StructuralDispositions = BuildStory21StructuralDispositions(),
            Story22StructuralDispositions = BuildStory22StructuralDispositions(),
            Story23StructuralDispositions = BuildStory23StructuralDispositions(),
            Story24StructuralDispositions = BuildStory24StructuralDispositions(),
            Story25StructuralDispositions = BuildStory25StructuralDispositions(),
            Story26StructuralDispositions = BuildStory26StructuralDispositions(),
        };

    private static IReadOnlyList<Story26StructuralDisposition> BuildStory26StructuralDispositions() =>
    [
        new(
            Subject: "FR-8 / Story 2.6 disposition: the inventory Consume target for 'generic-serialization-converters' "
                + "- 'Commons TypeMapper + generic value/identifier JSON converters + source-gen JSON-context base' - "
                + "re-scoped (the generic-converter / JSON-context-base build + the NameTypeMapper publicize are deferred "
                + "to FR-14 / Story 3.6)",
            Change: "Verified at the recorded Commons gitlink 30620b9 that the named shared target does NOT exist as "
                + "consumable surface: Commons exposes only the public static TypeMapper.GetMap<TMappable>() (plus "
                + "GetObject / GetType / GetMappableTypes), all constrained where TMappable : IMappableType, and an "
                + "internal NameTypeMapper<TMappable>. Commons ships NO generic value/identifier JSON converter and NO "
                + "source-generated JSON-serialization-context base (a search for a generic JSON converter type across the "
                + "Commons sources is empty), and Hexalith.PolymorphicSerializations is neither in Central Package "
                + "Management nor referenced under src/. So nothing in the Epic-2 (Consume-only, no-Commons-edit) surface "
                + "exists to remove-and-replace the 215-LOC generic converters with. Building that shared converter / "
                + "context-base capability and publicizing NameTypeMapper is a Promote = FR-14 = Story 3.6, opened as an "
                + "explicit Epic-3 dependency, not attempted here. The five converter files stay in place, "
                + "behavior-identical.",
            Ac: "AC-1 / AC-5",
            Rationale: "Recorded per the Story 1.5 escape hatch. Because this RE-SCOPES an accepted Consume area (the "
                + "consume is deferred to FR-14/3.6, not realized here), it differs from the Story 2.4 / 2.5 no-relabel "
                + "corrections and DOES take an append-only inventory changeLog entry "
                + "(CL-generic-serialization-converters-challenge-1) per classification-change-procedure-v1, mirroring "
                + "CL-shared-host-api-challenge-1: the area stays literally labeled Consume (deletion is deferred, not "
                + "reclassified) so it is a challenge/upheld note with no from/to, and its frozen approxLoc (215) is not "
                + "mutated. The deferral is recorded non-silently (FR-2) so Story 3.6 inherits a clear dependency.",
            OwningStory: "Story 2.6 (FR-8, adopt shared serialization helpers for generic converters)",
            GreenAfterChange: true),
        new(
            Subject: "Within-area reclassification of 'generic-serialization-converters': the prefixed-identifier "
                + "converters encode a genuine domain rule (Keep-aligned); only the two value-base skeletons are ruleless "
                + "machinery",
            Change: "Classified each of the five files. (a) ConversationStringValueJsonConverter<T> and "
                + "ConversationIntValueJsonConverter<T> are genuinely ruleless machinery - a token-type guard plus "
                + "Create / GetValue delegation, no domain rule - the eventual FR-14/3.6 deletion target. (b) "
                + "PrefixedIdentifierJsonConverter<T> plus the seven concrete identifier converters "
                + "(conv: / tenant: / party: / project: / folder: / file: / message:) encode a genuine domain rule - the "
                + "URN-style prefix prevents silent cross-type substitution between identifier families on the wire - so "
                + "they are Keep-aligned, not generic-replaceable. (c) SchemaVersionJsonConverter's only validation "
                + "(value >= 1) lives in the SchemaVersion value type, not the converter. Because no shared replacement "
                + "exists yet, all five files remain in place behavior-unchanged pending FR-14/3.6.",
            Ac: "AC-3",
            Rationale: "Strict reading of FR-8 ('generic value converters with no domain rules are replaced; only "
                + "converters encoding genuine domain rules remain'): the prefix invariant is a real correctness / "
                + "security rule, so those converters belong with Keep, not the generic-replaceable machinery. No file is "
                + "deleted now (no shared replacement exists). Recorded append-only; no area's frozen approxLoc (215 / 432) "
                + "is mutated.",
            OwningStory: "Story 2.6 (FR-8, adopt shared serialization helpers for generic converters)",
            GreenAfterChange: true),
        new(
            Subject: "AC-2 TypeMapper.GetMap() consume evaluation (negative finding) and the generic-converter "
                + "wire-shape oracle confirmed pinned",
            Change: "The only public Commons serialization helper, TypeMapper.GetMap<TMappable>(), is constrained "
                + "where TMappable : IMappableType and instantiates each mapped type via a public parameterless "
                + "constructor. The one hand-rolled type-name-to-Type map in the module "
                + "(ConversationProjectionHandler.BuildPublicEventTypeMap(), 13 public events keyed by type.Name) cannot "
                + "adopt it without making those public event records implement IMappableType (plus a parameterless "
                + "constructor) - a public-contract reshape that would break the empty public-contract-shape diff and is "
                + "exactly the polymorphic-registration concern FR-14/3.6 owns. No Conversations contract implements "
                + "IMappableType at baseline. Conclusion: no clean Epic-2 consume of TypeMapper.GetMap() exists; the map "
                + "is left as-is (no src/ change) and the negative finding recorded. The ContractSerializationTest "
                + "exact-wire-shape oracle (the tenant: / conv: / party: / project: / folder: / file: / message: prefixes, "
                + "schemaVersion:1, and the string / int encodings) is confirmed green and un-weakened - it is the "
                + "byte-exact characterization the future FR-14/3.6 replacement must preserve.",
            Ac: "AC-2 / AC-4 / AC-5",
            Rationale: "Do NOT reshape public contracts to manufacture a consume (NFR8; the empty-diff gate). Behavior "
                + "preserved: no source change, the wire-shape oracle stays intact as the FR-14/3.6 characterization "
                + "target. No test is retired by this story; the new ledger validation fact "
                + "(EveryStory26StructuralDispositionShouldBeAnchoredAndGreen) keeps the conformance count monotonic "
                + "vs the Story 2.5 close of 355.",
            OwningStory: "Story 2.6 (FR-8, adopt shared serialization helpers for generic converters)",
            GreenAfterChange: true),
    ];

    private static IReadOnlyList<Story25StructuralDisposition> BuildStory25StructuralDispositions() =>
    [
        new(
            Subject: "FR-6 / Story 2.5 disposition correction: the inventory 'Promote' label for "
                + "projection-orchestration -> 'Consume'",
            Change: "The inventory labeled the projection-orchestration area Promote (FR-6) on the assumption that "
                + "the full-replay projection seam had to be promoted into the platform. Verified against the "
                + "platform working tree at the recorded gitlink: the seam (the projection-handler interface, the "
                + "matching request router, the convention discovery/registration, and the shared /project endpoint) "
                + "ALREADY ships in the platform - the reference counter sample and the platform widget test fixture "
                + "both implement it. Nothing is promoted. The local orchestration is consumed-and-deleted: a new "
                + "ConversationProjectionHandler implements the platform projection-handler interface and is "
                + "auto-discovered by the Story 2.1 Server-assembly convention scan with NO host route/discovery edit, "
                + "so the generic replay/routing/discovery orchestration and the /project endpoint are owned by the "
                + "platform, not the module. Mirrors the Story 2.4 label correction.",
            Ac: "AC-1 / AC-5",
            Rationale: "Consume (FR-6): the platform owns the generic replay/routing/discovery and the /project "
                + "endpoint, so the module keeps only its conversation-specific logic - strengthening the "
                + "technical-module boundary (NFR8). Recorded per the Story 1.5 escape hatch. No inventory area is "
                + "relabeled: the area paths are unchanged and its frozen approxLoc (1,800 / 1,175) is not mutated, so "
                + "per the Story 2.3 / 2.4 no-relabel reasoning no inventory changeLog entry is required (the outcome "
                + "is identical under either label; only the seam-pre-exists fact moved). The projected read model "
                + "rides the existing public Contracts shapes, so the public contract-shape baseline diff stays empty.",
            OwningStory: "Story 2.5 (FR-6, consume the platform projection-handler seam, keep the conversation logic)",
            GreenAfterChange: true),
        new(
            Subject: "Conversation-specific materialization logic (ConversationProjectionMaterializer: the idempotent "
                + "per-event accumulator, the freshness formula, search-trust / trust-posture, default command "
                + "eligibility, and evidence construction)",
            Change: "KEEP, behavior-unchanged (AC-2). The new ConversationProjectionHandler decodes the request's "
                + "events into the public conversation event vocabulary (reusing the existing attribute-based "
                + "serialization, no new converter) and DELEGATES the replay loop, field selection, freshness formula, "
                + "and evidence construction to ConversationProjectionMaterializer.Project(...), which stays the single "
                + "shared materialization entry point invoked by BOTH the handler and ConversationProjectionRebuildVerifier "
                + "- no second hand-rolled replay loop is introduced. The conversation-specific replay loop is preserved "
                + "logic the handler calls, exactly as AC-1 frames it; only the generic replay / routing / discovery "
                + "orchestration moved to the platform. The Contracts/Projections field-selection / freshness DTOs are "
                + "not reshaped (Keep, in the 196-type baseline), so the public contract-shape diff is empty.",
            Ac: "AC-1 / AC-2 / AC-4",
            Rationale: "The materialization logic is conversation-specific domain surface the platform seam does not "
                + "provide, so it is Keep, not consumed. Open-question resolution recorded: retaining the in-module "
                + "Project(...) entry point is the AC-1-blessed structure (the requirement is that the GENERIC "
                + "orchestration is the platform's, not that the materialization method vanish). Verified green with no "
                + "behavior change to the kept logic.",
            OwningStory: "Story 2.5 (FR-6, projection seam)",
            GreenAfterChange: true),
        new(
            Subject: "ConversationProjectionMaterializerTest plumbing-only-retire @ 2.5 assertions and "
                + "LiveProjectionFreshnessOracleCharacterizationTest coupled-by-design-retarget @ 2.5",
            Change: "RETAINED, not retired or retargeted. The Story 1.3 register marked these for retirement / retarget "
                + "on the premise that FR-6 would remove the in-module replay / routing orchestration (so the raw-output "
                + "assertions would lose their reachable subject). Under the verified disposition correction the "
                + "in-module materialization entry point is PRESERVED (it is the shared kept-logic seam the handler and "
                + "the rebuild verifier both call), so the AC-4 precondition - 'when the local orchestration frame is "
                + "removed' - did not occur. The plumbing-only ConversationProjectionMaterializerTest assertions "
                + "therefore remain reachable behavior tests of the kept logic and are kept, not dropped (honoring "
                + "'never silently drop a behavior assertion'); LiveProjectionFreshnessOracleCharacterizationTest still "
                + "binds the live materializer it characterizes, so it needs no structural retarget and its assertion "
                + "strength is unchanged. Net test count holds or grows: a new seam-level behavior test "
                + "(ConversationProjectionHandlerTest) and a discovery fact assert the projected field / freshness / "
                + "evidence values and a degraded reason code THROUGH the platform projection-handler seam, plus this "
                + "ledger fact. Nothing is retired under this story, so no count reduction is left for Story 5.2 to "
                + "reconcile here.",
            Ac: "AC-3 / AC-4 / AC-5",
            Rationale: "Behavior preservation (NFR1): retiring assertions that still cover reachable kept-logic "
                + "behavior, with no equivalent replacement, would drop coverage - which AC-4 and the standing gate "
                + "forbid. The register's plumbing-only / retarget rows stand for a future story that actually relocates "
                + "or deletes the in-module materialization method, if ever; recorded append-only so no later count "
                + "change is unaccounted for.",
            OwningStory: "Story 2.5 (FR-6, projection seam)",
            GreenAfterChange: true),
    ];

    private static IReadOnlyList<Story24StructuralDisposition> BuildStory24StructuralDispositions() =>
    [
        new(
            Subject: "FR-5 / Story 2.4 disposition correction: the epics' 'remove-and-replace' label -> 'greenfield-adopt'",
            Change: "The epics labeled Story 2.4 remove-and-replace on the first-pass assumption that the module held "
                + "hand-written Dapr state-store calls and merge-on-write loops to delete. Verified against the working "
                + "tree: there was ZERO bespoke Dapr / state-store / ETag / merge-loop / optimistic-concurrency code under "
                + "src for read-model persistence to remove. The work is purely additive adoption — register the platform "
                + "persisted read-model store (the shared IReadModelStore, backed by DaprReadModelStore), implement the "
                + "production IConversationProjectionReadStore over it, and add a thin write path through the platform "
                + "ReadModelWritePolicy (optimistic-concurrency, reload-and-merge) — and it closes the production "
                + "IConversationProjectionReadStore binding deferred from Story 2.3 (five query/governance services "
                + "required it; only in-memory test fakes satisfied it before, so the host could not resolve the query "
                + "graph).",
            Ac: "AC-5",
            Rationale: "Consume (FR-5): the platform owns persisted read-model integrity and optimistic concurrency, so "
                + "no local state store or read-modify-write loop is hand-rolled — strengthening the technical-module "
                + "boundary (NFR8). Behavior-preserving: the fail-closed read shapes the suite pins still hold through the "
                + "real store-backed read path. The disposition correction is recorded per the Story 1.5 escape hatch; no "
                + "inventory area is relabeled (there is no FR-5 area, and the projection-orchestration area stays a Story "
                + "2.5 concern), so no inventory changeLog entry is required (mirrors Story 2.3's no-glob-empties reasoning). "
                + "The persisted value rides the existing public Contracts shapes, so the public contract-shape baseline "
                + "diff stays empty.",
            OwningStory: "Story 2.4 (FR-5, persist read models via the shared store + write policy)",
            GreenAfterChange: true),
        new(
            Subject: "Server-boundary assembly-metadata guard (ServerBoundaryTest) — the Dapr.Client absence clause",
            Change: "Re-expressed, not weakened. The host now registers the platform persisted read-model store, which "
                + "the platform documents as requiring a registered DaprClient, so the host calls AddDaprClient() (from "
                + "Dapr.AspNetCore). That extension's signature names a Dapr.Client type, which unavoidably introduces a "
                + "Dapr.Client ASSEMBLY-METADATA reference, so the assembly-level absence assertion is replaced by a "
                + "presence assertion (a silent removal of the read-model-store registration now turns the fact red). The "
                + "architecturally-meaningful invariant — no DIRECT Dapr.Client package/project reference in the Server "
                + "csproj — is preserved and still asserted at the csproj level by the companion guard "
                + "(ServerProjectFileShouldDeclareDomainServiceHostAndNoForbiddenRuntimeReferences). All other "
                + "forbidden-runtime clauses (the gateway, server-side Tenants, Parties, the UI shell) are unchanged.",
            Ac: "AC-1 / AC-5",
            Rationale: "Premise change driven by FR-5: registering the platform read-model store legitimately requires a "
                + "DaprClient — the canonical domain-service host pattern. Recorded append-only per agreements A2/A3 rather "
                + "than silently editing the assertion. Assertion strength is preserved or increased (assembly-level "
                + "absence -> required presence; the direct-package-dependency guard stays fully intact). Mirrors the Story "
                + "2.1 re-expression of this same guard.",
            OwningStory: "Story 2.4 (FR-5, persist read models via the shared store + write policy)",
            GreenAfterChange: true),
        new(
            Subject: "Conversation read-model field-selection / freshness Contracts DTOs and the projection-read "
                + "fail-closed boundary",
            Change: "KEEP, unchanged (AC-4). The persisted value rides the existing public ConversationSummaryProjectionV1 "
                + "/ ConversationDetailProjectionV1 / ProjectionFreshnessV1 shapes with no reshape, so the public "
                + "contract-shape baseline diff stays empty. The new persistence/concurrency tests are additive (round-trip, "
                + "no-lost-update, retry-exhaustion, idempotency) and the fail-closed read assertions (hidden / unavailable "
                + "/ rebuilding / identity-mismatch) are re-expressed through the production read store over the shared "
                + "IReadModelStore without weakening; the in-memory fake-backed read tests stay green unchanged.",
            Ac: "AC-4 / AC-5",
            Rationale: "Domain surface the platform store does not provide, so it is Keep, not consumed; persistence rides "
                + "the existing shapes. Recorded append-only to clarify Story 2.4 scope without rewriting accepted rows. "
                + "Verified green with no source edits to the Contracts projection DTOs or the read-service fail-closed "
                + "branches.",
            OwningStory: "Story 2.4 (FR-5, persist read models via the shared store + write policy)",
            GreenAfterChange: true),
    ];

    private static IReadOnlyList<Story23StructuralDisposition> BuildStory23StructuralDispositions() =>
    [
        new(
            Subject: "Hand-rolled HMAC-SHA256 list continuation cursor codec (ConversationQueryCursor) and the "
                + "crypto half of ConversationQueryCursorOptions (SigningKey / KeyId)",
            Change: "Removed and replaced by the platform protected-cursor codec (IQueryCursorCodec, ASP.NET Core "
                + "Data Protection backed) plus the QueryCursorScope binding builder. Tenant / caller / filter "
                + "fingerprint / sort version bind into the scope; offset / issued-at / projection-generation token ride "
                + "in the protected position; the MaxAge / MaxOffset / clock-skew guards stay as domain checks re-applied "
                + "after decode (the platform codec has no wall-clock lifetime and no offset ceiling). No HMACSHA256 / "
                + "FixedTimeEquals cursor-signing code remains under the Server query boundary. The codec is registered "
                + "once via the platform helper under the stable purpose 'Hexalith.Conversations.QueryCursor.v1'.",
            Ac: "AC-1 / AC-2 / AC-5",
            Rationale: "Consume (FR-4): the platform owns cursor integrity, so the local HMAC codec is removed, "
                + "strengthening the technical-module boundary. Behavior-preserving by re-expression: every fail-closed "
                + "rejection the suite pins still fails closed. The public contract-shape baseline is unaffected — the "
                + "codec lived in the Server assembly and the continuation cursor is an opaque string on the public "
                + "ConversationPageMetadata / ConversationListResult Contracts surface.",
            OwningStory: "Story 2.3 (FR-4, consume the platform query-handler seam and cursor codec)",
            GreenAfterChange: true),
        new(
            Subject: "HMAC-specific cursor fail-closed tests (Tampered / CursorSignedWithDifferentKey / Expired / "
                + "FutureDated / GenerationMismatched / TenantMismatched / CallerMismatched / Malformed / ExcessiveOffset) "
                + "and the ForgeCursorWithOffset helper",
            Change: "Re-expressed against the platform cursor codec, not deleted. Each test still asserts the same safe "
                + "Hidden / Forbidden shape; the integrity cases (tampered, different purpose/key, malformed) still assert "
                + "zero projection rows read, and the scope-binding cases (tenant, caller) now ALSO assert zero reads "
                + "(caught at the scope boundary before any read — assertion strength increased, not reduced). The retired "
                + "ForgeCursorWithOffset (which hand-built an HMAC payload) is rebuilt to forge via the codec position. A "
                + "net-new cursor round-trip test (IssuedContinuationCursorShouldRoundTripToNextPage) and the query-seam "
                + "reach tests (ConversationDomainQueryDispatchTest, ExplicitAssemblyScanShouldDiscoverConversationDomainQueryHandlers) "
                + "were added, so the standing conformance count holds or grows.",
            Ac: "AC-2 / AC-5",
            Rationale: "Re-expressed, never weakened (agreements A2/A3). Assertion strength is preserved or increased "
                + "versus the Story 1.1 baseline; no fail-closed behavior is dropped. Append-only record so no later "
                + "count change is unaccounted for.",
            OwningStory: "Story 2.3 (FR-4, consume the platform query-handler seam and cursor codec)",
            GreenAfterChange: true),
        new(
            Subject: "Conversation-specific query logic: filter dimensions, worst-case freshness aggregation, read-time "
                + "hydration (citations / audit records / privileged-justification review), temporal reconstruction "
                + "(ConversationTemporalReconstructionService and the temporal permalink / anchor contracts), and the "
                + "Contracts/Queries DTOs",
            Change: "KEEP, unchanged (AC-4). The story exposes this logic through the platform query-handler seam "
                + "(IDomainQueryHandler) as a thin adapter (ConversationDomainQueryHandlerBase plus the list and detail "
                + "handlers) that deserializes the envelope, delegates to the existing ConversationQueryHandler, and "
                + "serializes the result — it reimplements none of the filter / freshness / hydration / temporal logic. No "
                + "source edits to ConversationTemporalReconstructionService, the temporal permalink contracts, or the "
                + "Contracts/Queries DTOs.",
            Ac: "AC-3 / AC-4",
            Rationale: "Domain surface the platform seam does not provide, so it is Keep, not consumed. The temporal "
                + "permalink / anchor path is an unsigned domain contract on a separate path, left untouched, so temporal "
                + "cursors and permalinks re-resolve to the same position. Verified green and unchanged (the temporal "
                + "reconstruction and Contracts/Queries tests stay green with no source edits).",
            OwningStory: "Story 2.3 (FR-4, consume the platform query-handler seam and cursor codec)",
            GreenAfterChange: true),
    ];

    private static IReadOnlyList<Story22StructuralDisposition> BuildStory22StructuralDispositions() =>
    [
        new(
            Subject: "Redundant command-status idempotency-bridge shim (Server-side, public static) + its unit test "
                + "(tests/Hexalith.Conversations.Server.Tests/.../...IdempotencyBridgeTest.cs)",
            Change: "Deleted (shim + its test + the now-empty Server and Server.Tests directories holding only them). "
                + "The shim's sole method interpreted a substrate command-status record into a Conversations idempotency "
                + "decision and, for every possible input, returned the same retryable-uncertainty decision. It had ZERO "
                + "production references (consumed only by its own unit test). The SDK base class the aggregate already "
                + "extends owns command status and the command lifecycle, so the shim bridged nothing. Deleting it is "
                + "behavior-preserving by construction (dead code). The net-new base-class reflection-dispatch / replay "
                + "teeth test (ConversationAggregateBaseClassDispatchTest) offsets the removed unit test, so the standing "
                + "conformance suite count stays monotonic.",
            Ac: "AC-2 / AC-5",
            Rationale: "Redundant: the base class / SDK already owns command status and the shim was dead with zero "
                + "production references. Recorded append-only per agreements A2/A3 — a removed test carries a ledger "
                + "justification. Classified redundant, not weakened: no release-gate behavior is lost because the path "
                + "was never live. Traceable to the seeded 'Story 2.2 (FR-7)' register rows above. The public "
                + "contract-shape baseline is unaffected (the shim lived in the Server assembly, outside the Contracts "
                + "public-contract-shape surface).",
            OwningStory: "Story 2.2 (FR-7, shared aggregate base-class dispatch / idempotency-bridge shims)",
            GreenAfterChange: true),
        new(
            Subject: "Genuine Conversations idempotency contract + domain replay-verification "
                + "(IdempotentConversationCommandExecutor, Idempotency/*, ConversationReplayVerifier)",
            Change: "KEEP, unchanged (AC-3). The seeded FR-7 'idempotency-bridge shims' wording refers to the dead "
                + "command-status shim removed above, NOT these. The base class supplies command DISPATCH and whole-stream "
                + "replay; it does not supply Conversations' explicit idempotency reserve/replay/conflict lifecycle nor the "
                + "content-safe per-event replay-verification (tenant/conversation scope, position-gap/reorder detection, "
                + "schema-version checks, duplicate-identity rules). ConversationReplayVerifier's inner per-event apply switch "
                + "also stays: the SDK exposes no public per-event apply seam usable mid-verification (a reasonable Epic 3 "
                + "promote-later candidate, logged via classification-change-procedure-v1 if pursued).",
            Ac: "AC-3",
            Rationale: "These encode domain logic the base class does not provide, so they are Keep, not redundant shims. "
                + "Recorded append-only to clarify the actual Story 2.2 scope without rewriting the seeded rows. Verified "
                + "green and unchanged by this story (no source edits to Idempotency/*, the executor, or the verifier).",
            OwningStory: "Story 2.2 (FR-7, shared aggregate base-class dispatch / idempotency-bridge shims)",
            GreenAfterChange: true),
    ];

    private static IReadOnlyList<Story21StructuralDisposition> BuildStory21StructuralDispositions() =>
    [
        new(
            Subject: "tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs",
            Change: "Premise re-expressed (not weakened). The scaffold-era guard asserted the Server referenced NO "
                + "domain-service host SDK. Story 2.1 fills the unbuilt host slot, so the guard now REQUIRES the shared "
                + "domain-service host SDK project reference (a positive assertion, so a silent removal of the host is "
                + "caught) while still forbidding the genuinely out-of-bounds dependencies: the gateway, server-side "
                + "Tenants, Parties, the UI shell, and a direct Dapr.Client reference (DAPR arrives transitively via "
                + "Dapr.AspNetCore). Assertion strength increased, not reduced.",
            Ac: "AC-4 / AC-7",
            Rationale: "The story changes exactly the premise this guard encoded (Server is no longer a non-host "
                + "scaffold). Recorded append-only per agreement A2 rather than silently editing the assertions.",
            OwningStory: "Story 2.1 (FR-3, shared two-line domain-service host)",
            GreenAfterChange: true),
        new(
            Subject: "tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs"
                + " [ProjectReferencesShouldFollowScaffoldBoundaryDirection]",
            Change: "Expected Server reference set updated to include the shared domain-service host SDK project "
                + "reference. The same scaffold-boundary premise as ServerBoundaryTest: the Server is now the host, so "
                + "the host SDK reference is expected, not forbidden. Direction guard otherwise unchanged (still pins "
                + "every other Conversations project's reference set).",
            Ac: "AC-4 / AC-7",
            Rationale: "Updated, not silently broken — adding the host reference would otherwise fail this exact-set "
                + "guard. Recorded append-only per agreement A2.",
            OwningStory: "Story 2.1 (FR-3, shared two-line domain-service host)",
            GreenAfterChange: true),
        new(
            Subject: "Residual internal governance audit gate (fail-closed-on-sink-failure)",
            Change: "Surfaced into the conformance oracle (not retired). New oracle test "
                + "GovernanceAuditSinkFailClosedConformanceTest drives the public governed command-handler surface with a "
                + "throwing audit sink and asserts a fail-closed rejection (audit_unavailable) with no mutation event; a "
                + "contrast fact asserts a healthy sink emits the mutation. The gate itself stays internal (exposing it "
                + "would change the public contract shape, which the standing gate forbids).",
            Ac: "AC-5 / AC-7",
            Rationale: "Surface chosen over retire because the behavior is live and used by the governed handlers, so the "
                + "shared host does not make it redundant. Fault-injection verified: bypassing the gate's catch turns the "
                + "throwing-sink fact red, so green is real evidence (Epic 1 L1/A1). Resolves the Story 1.2 carry-forward "
                + "'internal-governance-audit-gate' and Epic 1 retro action T3.",
            OwningStory: "Story 2.1 (FR-3, shared two-line domain-service host)",
            GreenAfterChange: true),
    ];

    private static IReadOnlyList<AtRiskTestEntry> BuildTestEntries() =>
    [
        new(
            File: "tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs",
            Coupling: "Reflection over ConversationAggregate internals plus Server.CommandHandlers / Server.Governance / "
                + "Server.Projections / Server.Queries / Server.Api concrete handler and service types.",
            Classification: ReExpressNeverDelete,
            Rationale: "Release-gate behavior safety net: every implemented governance mutation pairs its state-change "
                + "event with audit evidence; non-governance commands carry no audit dependency. Its survival through the "
                + "refactor is the point. The audit-pairing behavior is re-expressed against the public command/state/event/"
                + "DomainResult surface and relocated into the oracle; the original is kept in place (no deletion in this story).",
            OwningStory: null,
            ReExpressedAs: "tests/Hexalith.Conversations.Conformance.Tests/GovernanceAuditPairingSafetyNetConformanceTest.cs",
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs"
                + " [assertion group: ReadOnlyWorkspaceBoundariesShouldNotReferenceMutationExecutionTypes, "
                + "PrivilegedJustificationShouldBeImplementedAsPreconditionAuditBoundary, and the handler-type / "
                + "handler-constructor structural fragments of ImplementedGovernanceMutationPathsShouldRemainExplicit and "
                + "NonGovernanceConversationActivityShouldRemainOutsideAuditDegradationHandling]",
            Coupling: "Structural reflection over Server.Api / Server.Queries / Server.Projections / Server.Governance / "
                + "Server.CommandHandlers concrete types, IConversationGovernanceAuditService, ConversationGovernanceAuditGate, "
                + "IdempotentConversationCommandExecutor, and ConversationPrivilegedOperationalJustificationService.",
            Classification: PlumbingOnlyRetire,
            Rationale: "Asserts code-structure constraints (read-only boundaries do not depend on mutation/audit types; "
                + "privileged-justification service wiring), not externally-observable release-gate behavior, so it cannot be "
                + "re-expressed through the public surface. Kept in place now; retired when the Server handlers/services relocate.",
            OwningStory: "Story 2.1 (FR-3, shared host handler/service re-registration)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs"
                + " [behavior assertions: redaction non-leakage on replay; freshness downgrade surfacing "
                + "(stale/rebuilding/gap/mixed-tenant/unavailable); governance evidence anchoring; field selection]",
            Coupling: "Directly instantiates ConversationProjectionMaterializer and asserts on the Server-internal "
                + "ConversationProjectedReadModels output.",
            Classification: ReExpress,
            Rationale: "These behaviors are observable through the public projection-read surface "
                + "(ConversationProjectionReadService returning Contracts.Projections DTOs), so they are re-expressed there "
                + "and stay pinned after the materializer orchestration moves. Cross-references the Story 1.2 "
                + "LiveProjectionFreshnessOracleCharacterizationTest, which pins the materializer-level branches; the "
                + "re-expression covers the read-service path that test does not.",
            OwningStory: "Story 2.5 (FR-6, SDK projection seam - promote orchestration, keep logic)",
            ReExpressedAs: "tests/Hexalith.Conversations.Conformance.Tests/ConversationProjectionReadSurfaceConformanceTest.cs"
                + " (+ tests/Hexalith.Conversations.Conformance.Tests/LiveProjectionFreshnessOracleCharacterizationTest.cs)",
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs"
                + " [plumbing-only assertions: command-eligibility internal posture (AssertCommands); unknown-event-type "
                + "downgrade; contradictory-metadata downgrade; gap / out-of-order internal reason codes; mixed-tenant and "
                + "malformed-content rejection internals]",
            Coupling: "Asserts raw ConversationProjectedReadModels orchestration mechanics with no externally-observable "
                + "public-read equivalent.",
            Classification: PlumbingOnlyRetire,
            Rationale: "The materializer's generic replay/dispatch orchestration is promoted to the SDK; only orchestration-"
                + "mechanics assertions are retired (the field-selection/freshness logic with a public-read equivalent is "
                + "re-expressed in the entry above). Kept in place now; retired with the code in the owning story.",
            OwningStory: "Story 2.5 (FR-6, SDK projection seam)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Server.Tests/Idempotency/IdempotentConversationCommandExecutorTest.cs"
                + " [behavior assertions: completed-duplicate replay-without-mutation; conflicting-key rejection before "
                + "mutation; pending-key retryable uncertainty; duplicate-rejection reason-code preservation; replay-payload "
                + "secret exclusion]",
            Coupling: "Directly instantiates IdempotentConversationCommandExecutor over InMemoryConversationIdempotencyStore.",
            Classification: ReExpress,
            Rationale: "Observable idempotency behavior is re-expressed against the public DomainResult + "
                + "ConversationIdempotencyReplayResult outcome surface and the ConversationRejectedDomainEvent envelope. "
                + "Cross-references the Story 1.2 LiveIdempotencyOracleCharacterizationTest (duplicate-replay case); the "
                + "re-expression covers the conflict, pending, reason-preservation, and payload-secret-exclusion cases it omits.",
            OwningStory: "Story 2.2 (FR-7, shared aggregate base-class dispatch / idempotency-bridge shims)",
            ReExpressedAs: "tests/Hexalith.Conversations.Conformance.Tests/LiveIdempotencyConflictOracleCharacterizationTest.cs"
                + " (+ tests/Hexalith.Conversations.Conformance.Tests/LiveIdempotencyOracleCharacterizationTest.cs)",
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Server.Tests/Idempotency/IdempotentConversationCommandExecutorTest.cs"
                + " [plumbing-only assertions: reservation invalidation lifecycle on retryable/throwing outcomes; injected "
                + "clock completion-timestamp wiring; raw store record status; audit-handle canonical length-prefixed "
                + "encoding internals]",
            Coupling: "Asserts internal store record state and executor reserve/complete mechanics with no externally-"
                + "observable release-gate behavior.",
            Classification: PlumbingOnlyRetire,
            Rationale: "The executor is a consume-aggregate-base item; its reserve/complete/invalidate mechanics are the "
                + "bridge shims FR-7 removes. Internal-store-state assertions retire with that code; the observable "
                + "idempotency outcome behavior is re-expressed in the entry above. Kept in place now.",
            OwningStory: "Story 2.2 (FR-7, shared aggregate base-class dispatch / idempotency-bridge shims)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuiteTest.cs"
                + " (+ engine TelemetryCardinalityConformanceSuite.cs)",
            Coupling: "using Hexalith.Conversations.Server.Diagnostics (ConversationConformanceTelemetry, "
                + "ConversationProjectionTelemetry, freshness/lag classes) and Hexalith.Conversations.Server.TenantAccess.",
            Classification: CoupledRetarget,
            Rationale: "Asserts operational-telemetry cardinality behavior that genuinely needs the diagnostics types; the "
                + "realistic disposition is to keep the suite and have the owning story update it to the promoted type, not "
                + "a full public-surface re-expression. Story 1.1 hand-off.",
            OwningStory: "Story 3.3 (FR-15, telemetry/diagnostics promotion)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuiteTest.cs"
                + " (+ engine TelemetryRedactionConformanceSuite.cs)",
            Coupling: "using Hexalith.Conversations.Server.Diagnostics (and Server.TenantAccess in the engine).",
            Classification: CoupledRetarget,
            Rationale: "Asserts operational telemetry-redaction behavior that needs the diagnostics types; retargeted to the "
                + "promoted type in its owning story. Story 1.1 hand-off.",
            OwningStory: "Story 3.3 (FR-15, telemetry/diagnostics promotion)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/ConformanceStatusConformanceSuite.cs"
                + " + tests/Hexalith.Conversations.Conformance.Tests/ConformanceStatusConformanceFixtures.cs",
            Coupling: "Engine and fixtures use Hexalith.Conversations.Server.Diagnostics "
                + "(ConversationConformanceStatusClassifier, ConversationConformanceStatusClass). The "
                + "ConformanceStatusConformanceSuiteTest class itself is clean; the coupling is in the engine/fixtures.",
            Classification: CoupledRetarget,
            Rationale: "Verified Story 1.1 discrepancy: the status suite asserts diagnostics-classifier behavior through "
                + "its engine. Retargeted to the promoted classifier type in its owning story.",
            OwningStory: "Story 3.3 (FR-15, telemetry/diagnostics promotion)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/TelemetryDisclosureConformanceFixtures.cs",
            Coupling: "using Hexalith.Conversations.Server.Diagnostics and Hexalith.Conversations.Server.TenantAccess.",
            Classification: CoupledRetarget,
            Rationale: "Shared telemetry fixture binding the diagnostics types (retarget @ 3.3) and tenant-access types "
                + "(retarget @ 3.2). Stays; owning stories update the type references.",
            OwningStory: "Story 3.3 (FR-15) for diagnostics; Story 3.2 (FR-11) for the tenant-access fragment",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs",
            Coupling: "using Hexalith.Conversations.Server.TenantAccess (ConversationTenantAccessGuard, "
                + "ConversationTenantAccessService, decision/requirement/denial types).",
            Classification: CoupledRetarget,
            Rationale: "Story 1.2 live characterization test that deliberately drives the live tenant guard across all "
                + "fail-closed trigger states to catch fail-open mutations. Never deleted; retargeted when the tenant-access "
                + "type relocates.",
            OwningStory: "Story 3.2 (FR-11, tenant-access promotion)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/LiveProjectionFreshnessOracleCharacterizationTest.cs",
            Coupling: "using Hexalith.Conversations.Server.Projections (ConversationProjectionMaterializer) and "
                + "Hexalith.Conversations.Server.Diagnostics (ConversationProjectionFreshnessClassifier).",
            Classification: CoupledRetarget,
            Rationale: "Story 1.2 live characterization test that drives the live materializer/classifier to catch fail-open "
                + "degraded-as-fresh and redaction-leak mutations. Never deleted; retargeted when the projection seam moves.",
            OwningStory: "Story 2.5 (FR-6, SDK projection seam)",
            ReExpressedAs: null,
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/LiveIdempotencyOracleCharacterizationTest.cs",
            Coupling: "using Hexalith.Conversations.Server.CommandHandlers (IdempotentConversationCommandExecutor, "
                + "InMemoryConversationIdempotencyStore) and the Hexalith.Conversations.Idempotency core namespace.",
            Classification: CoupledRetarget,
            Rationale: "Story 1.2 live characterization test that drives the live executor to catch a flipped dedup branch. "
                + "Never deleted; retargeted when the executor seam moves. The Idempotency core namespace itself may shift "
                + "under FR-7 - a residual coupling tracked by the same owning story.",
            OwningStory: "Story 2.2 (FR-7, shared aggregate base-class dispatch / idempotency-bridge shims)",
            ReExpressedAs: null,
            BaselineGreen: true),
    ];

    private static IReadOnlyList<ReExpressionEntry> BuildReExpressions() =>
    [
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/GovernanceAuditPairingSafetyNetConformanceTest.cs",
            PublicSurface: "ConversationAggregate.Handle(command, state) -> DomainResult.Events, asserting governance "
                + "mutation events carry GovernanceAuditEvidenceReference and that missing/mismatched evidence fails closed; "
                + "non-governance commands emit events with no audit-evidence dependency. Core types only (no Server).",
            Classification: ReExpressNeverDelete,
            OwningStory: null,
            CoversCasesNotIn: "Fully decoupled survivable net; supersedes the reflection inventory of the original Server.Tests "
                + "safety net for the behavior-bearing invariant.",
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/ConversationProjectionReadSurfaceConformanceTest.cs",
            PublicSurface: "ConversationProjectionReadService.ReadDetailAsync(...) -> ConversationProjectionReadResult and "
                + "Contracts.Projections DTOs: redaction non-leakage through the public read DTO, governance evidence "
                + "anchoring, and fail-closed gating for every degraded materializer state.",
            Classification: CoupledRetarget,
            OwningStory: "Story 2.5 (FR-6, SDK projection seam)",
            CoversCasesNotIn: "LiveProjectionFreshnessOracleCharacterizationTest (which asserts at the raw materializer level) "
                + "- this asserts the adopter-facing read-service path.",
            BaselineGreen: true),
        new(
            File: "tests/Hexalith.Conversations.Conformance.Tests/LiveIdempotencyConflictOracleCharacterizationTest.cs",
            PublicSurface: "IdempotentConversationCommandExecutor.ExecuteAsync(...) observed through DomainResult, "
                + "ConversationIdempotencyReplayResult.Outcome public fields, and ConversationRejectedDomainEvent: conflict "
                + "rejection, pending retryable uncertainty, rejection-reason preservation, and replay-payload secret exclusion.",
            Classification: CoupledRetarget,
            OwningStory: "Story 2.2 (FR-7, shared aggregate base-class dispatch / idempotency-bridge shims)",
            CoversCasesNotIn: "LiveIdempotencyOracleCharacterizationTest (duplicate-replay-without-mutation) - this covers the "
                + "conflict, pending, reason-preservation, and payload-secret-exclusion cases it omits.",
            BaselineGreen: true),
    ];

    private static IReadOnlyList<CarryForwardEntry> BuildCarryForwards() =>
    [
        new(
            Id: "internal-governance-audit-gate",
            Description: "Story 1.2 carry-forward: the live fail-closed-on-sink-failure governance audit gate is internal "
                + "(visible only to Server.Tests, unreachable from the oracle).",
            Disposition: "CLOSED by Story 2.1 (AC-5 / Epic 1 retro action T3). Surfaced, not retired: the "
                + "fail-closed-on-sink-failure behavior is live and used by the governed command handlers, so the shared "
                + "host does not make it redundant. The gate stays internal (making it public would change the public "
                + "contract shape, which the standing conformance gate forbids); its behavior is now observable in the "
                + "oracle through the public governed command-handler surface by "
                + "GovernanceAuditSinkFailClosedConformanceTest, where a throwing audit sink yields a fail-closed "
                + "rejection (audit_unavailable) with no mutation event, and a contrast fact proves a healthy sink emits "
                + "the mutation. Fault-injection verified: bypassing the gate's catch turns the throwing-sink fact red.",
            OwningStory: "Story 2.1 (FR-3, shared host handler/service re-registration)"),
        new(
            Id: "test-parallelism-race",
            Description: "Story 1.2 carry-forward: an observed-once (not reproduced) race under test parallelism between the "
                + "public contract-shape baseline generator (which writes a committed evidence file) and the release baseline "
                + "validation test (which reads it).",
            Disposition: "CLOSED by Story 2.1 (Epic 1 retro action T1). It recurred while running the standing gate (Epic 2 "
                + "raised run frequency, as the retro predicted), so it was fixed test-only: every reader and writer of a "
                + "committed release-evidence artifact now shares the non-parallel ReleaseEvidenceArtifactCollection, so a "
                + "generation test never interleaves with a validation test of the same file. No assertion strength changed; "
                + "the rest of the suite stays parallel. Verified green across repeated full-suite runs.",
            OwningStory: "Story 2.1 (FR-3, shared two-line domain-service host)"),
    ];

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

    /// <summary>Root of the at-risk test register artifact.</summary>
    internal sealed record AtRiskTestRegisterV1
    {
        public required string ArtifactKind { get; init; }

        public required string Version { get; init; }

        public required string BaselineCommit { get; init; }

        public required string GeneratedBy { get; init; }

        public required string Description { get; init; }

        public required ClassificationLegend ClassificationLegend { get; init; }

        public required IReadOnlyList<AtRiskTestEntry> Tests { get; init; }

        public required IReadOnlyList<ReExpressionEntry> ReExpressionsAddedByThisStory { get; init; }

        public required IReadOnlyList<CarryForwardEntry> CarryForwardsFromStory12 { get; init; }

        public required ProjectReferenceDisposition ProjectReferenceDisposition { get; init; }

        public required IReadOnlyList<Story21StructuralDisposition> Story21StructuralDispositions { get; init; }

        public required IReadOnlyList<Story22StructuralDisposition> Story22StructuralDispositions { get; init; }

        public required IReadOnlyList<Story23StructuralDisposition> Story23StructuralDispositions { get; init; }

        public required IReadOnlyList<Story24StructuralDisposition> Story24StructuralDispositions { get; init; }

        public required IReadOnlyList<Story25StructuralDisposition> Story25StructuralDispositions { get; init; }

        public required IReadOnlyList<Story26StructuralDisposition> Story26StructuralDispositions { get; init; }
    }

    internal sealed record ClassificationLegend(
        string ReExpressNeverDelete,
        string ReExpress,
        string PlumbingOnlyRetire,
        string CoupledRetarget);

    internal sealed record AtRiskTestEntry(
        string File,
        string Coupling,
        string Classification,
        string Rationale,
        string? OwningStory,
        string? ReExpressedAs,
        bool BaselineGreen);

    internal sealed record ReExpressionEntry(
        string File,
        string PublicSurface,
        string Classification,
        string? OwningStory,
        string CoversCasesNotIn,
        bool BaselineGreen);

    internal sealed record CarryForwardEntry(
        string Id,
        string Description,
        string Disposition,
        string? OwningStory);

    internal sealed record ProjectReferenceDisposition(
        string Reference,
        string Classification,
        string Rationale,
        string TargetEndState,
        string PathToGetThere,
        bool RemovedInThisStory,
        string OwningStory);

    /// <summary>
    /// Append-only record of a test/guard whose premise an owning story deliberately changed (per team
    /// agreement A2: no test is silently weakened — every modification is recorded and traceable).
    /// </summary>
    internal sealed record Story21StructuralDisposition(
        string Subject,
        string Change,
        string Ac,
        string Rationale,
        string OwningStory,
        bool GreenAfterChange);

    /// <summary>
    /// Append-only record of a Story 2.2 (FR-7) structural disposition: a shim removed as redundant, or a
    /// genuine domain subsystem confirmed Keep, recorded so no later test-count reduction is unaccounted for
    /// (agreements A2/A3; Story 5.2 reconciliation).
    /// </summary>
    internal sealed record Story22StructuralDisposition(
        string Subject,
        string Change,
        string Ac,
        string Rationale,
        string OwningStory,
        bool GreenAfterChange);

    /// <summary>
    /// Append-only record of a Story 2.3 (FR-4) structural disposition: the hand-rolled cursor codec removed and
    /// replaced by the platform codec, the HMAC-specific cursor tests re-expressed (not deleted), and the
    /// conversation-specific query/filter/freshness/hydration/temporal surface confirmed Keep — recorded so no
    /// later test-count reduction is unaccounted for (agreements A2/A3; Story 5.2 reconciliation).
    /// </summary>
    internal sealed record Story23StructuralDisposition(
        string Subject,
        string Change,
        string Ac,
        string Rationale,
        string OwningStory,
        bool GreenAfterChange);

    /// <summary>
    /// Append-only record of a Story 2.4 (FR-5) structural disposition: the "remove-and-replace" -> "greenfield-adopt"
    /// correction (no bespoke state-store/merge code existed; the work adds the shared persisted read-model store +
    /// write policy and closes the deferred-from-2.3 read-store binding), the re-expressed Server-boundary
    /// Dapr.Client clause, and the projection Contracts DTOs confirmed Keep — recorded so no later test-count
    /// change is unaccounted for (agreements A2/A3; Story 5.2 reconciliation).
    /// </summary>
    internal sealed record Story24StructuralDisposition(
        string Subject,
        string Change,
        string Ac,
        string Rationale,
        string OwningStory,
        bool GreenAfterChange);

    /// <summary>
    /// Append-only record of a Story 2.5 (FR-6) structural disposition: the "Promote" -> "Consume" correction (the
    /// full-replay projection seam pre-exists in the platform, so the local orchestration is consumed-and-deleted,
    /// not promoted), the conversation-specific materialization logic confirmed Keep and delegated to behind the
    /// platform seam, and the projection materializer / live-freshness tests retained (the AC-4 retire/retarget
    /// precondition did not occur because the in-module materialization entry point is preserved) — recorded so no
    /// later test-count change is unaccounted for (agreements A2/A3; Story 5.2 reconciliation).
    /// </summary>
    internal sealed record Story25StructuralDisposition(
        string Subject,
        string Change,
        string Ac,
        string Rationale,
        string OwningStory,
        bool GreenAfterChange);

    /// <summary>
    /// Append-only record of a Story 2.6 (FR-8) structural disposition: the verified-gap finding that FR-8's named
    /// shared target (Commons generic value/identifier converters + a source-gen JSON-context base) does not exist in
    /// the Epic-2 consumable surface, so the build + the <c>NameTypeMapper</c> publicize are re-scoped to FR-14 /
    /// Story 3.6 (an inventory <c>changeLog</c> entry accompanies this re-scope); the within-area reclassification of
    /// the prefixed-identifier converters as a genuine domain rule (Keep-aligned) versus the two ruleless value-base
    /// skeletons; and the AC-2 negative finding (the only public helper needs a public-contract reshape) with the
    /// generic-converter wire-shape oracle confirmed pinned — recorded so no later test-count change is unaccounted for
    /// (agreements A2/A3; Story 5.2 reconciliation).
    /// </summary>
    internal sealed record Story26StructuralDisposition(
        string Subject,
        string Change,
        string Ac,
        string Rationale,
        string OwningStory,
        bool GreenAfterChange);
}
