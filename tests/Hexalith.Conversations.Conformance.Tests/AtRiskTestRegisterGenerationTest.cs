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
        };

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
}
