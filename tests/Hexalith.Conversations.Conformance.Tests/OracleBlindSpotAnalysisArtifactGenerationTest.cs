// <copyright file="OracleBlindSpotAnalysisArtifactGenerationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.2 (AC1/AC4/AC5) — generates and validates the committed oracle blind-spot analysis evidence
/// <c>docs/release-evidence/oracle-blind-spot-analysis-v1.json</c>.
///
/// Mirrors the Story 1.1 evidence pattern (<see cref="ReleaseConformanceArtifactGenerationTest"/> /
/// <c>PublicContractShapeSnapshotGenerationTest</c>): repo-root discovery → deterministic indented-JSON write
/// into <c>docs/release-evidence/</c> → re-read + re-validate + content-safety scan. The artifact records, for
/// each of the five release-gate behaviors, the production path(s), the tests covering them, the measured gap,
/// the fault-injection experiment and its result, and the disposition.
/// </summary>
public sealed class OracleBlindSpotAnalysisArtifactGenerationTest
{
    private const string ArtifactFileName = "oracle-blind-spot-analysis-v1.json";
    private const string HeaderFileName = "oracle-blind-spot-analysis-v1.md";

    // Same content-safety vocabulary the existing release-evidence scans use (CoreFixtureContentSafetyTest).
    private static readonly string[] ForbiddenFragments =
    [
        "EventStore",
        "snapshot",
        "SignalR",
        "dispatcher",
        "repository",
        "provider-session",
        "provider payload",
        "raw exception",
        "C:\\",
        "D:\\",
    ];

    private static readonly JsonSerializerOptions WriteOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void BuiltArtifactShouldBeStructurallyComplete()
    {
        BlindSpotAnalysisV1 artifact = Build();

        artifact.ArtifactKind.ShouldBe("oracle-blind-spot-analysis");
        artifact.Version.ShouldBe("v1");
        artifact.BaselineCommit.Length.ShouldBe(40);
        artifact.BaselineCommit.ShouldAllBe(c => Uri.IsHexDigit(c) && !char.IsUpper(c));
        artifact.Branch.ShouldBe("main");

        // AC1: all five release-gate behaviors are recorded, each with a measured gap, a fault-injection
        // experiment with a result, and a disposition.
        // The recorded oracle size must be internally consistent: total = baseline + new tests. This guards
        // against the count drifting out of sync with reality (the run reports 294 = 260 + 34).
        artifact.OracleStatus.TotalAfterBackfill.ShouldBe(
            artifact.OracleStatus.BaselineBeforeBackfill + artifact.OracleStatus.BackfillTestCount);

        artifact.Behaviors.Count.ShouldBe(5);
        artifact.Behaviors.ShouldAllBe(b => b.ProductionPaths.Count > 0);
        artifact.Behaviors.ShouldAllBe(b => !string.IsNullOrWhiteSpace(b.MeasuredGap));
        artifact.Behaviors.ShouldAllBe(b => !string.IsNullOrWhiteSpace(b.FaultInjection.Result));
        artifact.Behaviors.ShouldAllBe(b => b.Disposition == "backfilled" || b.Disposition == "accepted-gap-with-rationale");

        // AC3: the tenant fail-closed behavior must record the demonstrated fail-open catch and that the
        // original (pre-backfill) oracle was blind to it.
        BehaviorFindingV1 tenant = artifact.Behaviors.Single(b => b.Id == 1);
        tenant.Disposition.ShouldBe("backfilled");
        tenant.FaultInjection.OriginalOracleResult.ShouldNotBeNull();
        tenant.FaultInjection.Result.ShouldContain("RED");
        tenant.BackfillTests.ShouldNotBeEmpty();

        // AC4: every accepted gap carries a rationale and a closing story.
        artifact.AcceptedGaps.ShouldAllBe(g => !string.IsNullOrWhiteSpace(g.Rationale) && !string.IsNullOrWhiteSpace(g.ClosingStory));
    }

    [Fact]
    public void BuiltArtifactShouldBeDeterministic()
    {
        string first = JsonSerializer.Serialize(Build(), WriteOptions);
        string second = JsonSerializer.Serialize(Build(), WriteOptions);
        first.ShouldBe(second);
    }

    [Fact]
    public void ArtifactShouldPassContentSafetyScan()
    {
        string json = JsonSerializer.Serialize(Build(), WriteOptions);
        foreach (string fragment in ForbiddenFragments)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"Blind-spot artifact must not contain forbidden fragment '{fragment}'.");
        }
    }

    [Fact]
    public void GenerateAndSaveArtifactFile()
    {
        BlindSpotAnalysisV1 artifact = Build();
        string json = JsonSerializer.Serialize(artifact, WriteOptions);

        string dir = Path.Combine(FindRepositoryRoot(), "docs", "release-evidence");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, ArtifactFileName), json);

        // Re-read + re-validate: the committed bytes must round-trip and pass the same content-safety scan.
        string readBack = File.ReadAllText(Path.Combine(dir, ArtifactFileName));
        BlindSpotAnalysisV1? parsed = JsonSerializer.Deserialize<BlindSpotAnalysisV1>(readBack, WriteOptions);
        parsed.ShouldNotBeNull();
        parsed!.Behaviors.Count.ShouldBe(5);
        foreach (string fragment in ForbiddenFragments)
        {
            readBack.ShouldNotContain(fragment, Case.Insensitive);
        }
    }

    [Fact]
    public void CommittedHeaderShouldDescribeTheArtifact()
    {
        string path = Path.Combine(FindRepositoryRoot(), "docs", "release-evidence", HeaderFileName);
        File.Exists(path).ShouldBeTrue($"Expected committed header at '{path}'.");
        string md = File.ReadAllText(path);

        // AC5: the header states what it is, the commands used, the story, and the baseline commit.
        md.ShouldContain("Story 1.2");
        md.ShouldContain("--collect:\"XPlat Code Coverage\"");
        md.ShouldContain(Build().BaselineCommit);
    }

    private static BlindSpotAnalysisV1 Build() => new(
        ArtifactKind: "oracle-blind-spot-analysis",
        Version: "v1",
        Purpose: "Story 1.2 oracle-strengthening record for the Conversations Boilerplate Reduction initiative. "
            + "It measures where the Story 1.1 conformance oracle was blind on the five release-gate behaviors, "
            + "proves each gap with a targeted fault-injection experiment, and records the characterization tests "
            + "backfilled into the oracle to pin current observable behavior. The unifying finding: the 14 oracle "
            + "suites assert a synthetic scenario engine, while the live server decision code was exercised only by "
            + "the (non-oracle) server unit tests — so a fail-open mutation in live code rode green through the oracle.",
        BaselineCommit: "06641240a01e745b5db299da361f81dd6d505e6d",
        Branch: "main",
        WorkingTreeState: "src/ and tests/ clean at capture; all fault-injection edits were throwaway and reverted.",
        StoryReference: "Story 1.2 — Measure the oracle's blind spots and backfill characterization tests",
        RunDate: "2026-06-03",
        Toolchain: new ToolchainV1(
            DotnetSdk: "10.0.300",
            TargetFramework: "net10.0",
            TestStack: "xUnit v3, Shouldly",
            CoverageCollector: "coverlet.collector 8.0.1 (already in Directory.Packages.props; reused, no new tool added)",
            MutationApproach: "Targeted manual fault-injection (flip one safety-critical branch, run the oracle, revert). "
                + "Stryker.NET was intentionally not introduced into this repo."),
        Commands: new CommandsV1(
            ConformanceOracle: "dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj",
            Coverage: "dotnet test <project> --collect:\"XPlat Code Coverage\"",
            FaultInjection: "Flip one live deny/downgrade/dedup branch -> run the oracle -> observe the named backfill test go RED -> revert."),
        OracleStatus: new OracleStatusV1(
            BaselineBeforeBackfill: 260,
            BackfillTestCount: 34,
            TotalAfterBackfill: 294,
            Result: "green",
            Note: "294 = 260 Story 1.1 baseline tests + 34 new tests added by this story (29 live-decision-code "
                + "characterization test cases across 18 methods — including a 12-case fail-closed trigger-state theory — "
                + "plus 5 artifact generator/validator tests), all passing on main. No existing suite was weakened, "
                + "deleted, or had an assertion removed."),
        Behaviors:
        [
            new BehaviorFindingV1(
                Id: 1,
                Name: "Tenant fail-closed (NFR3)",
                Risk: "HIGH",
                ProductionPaths:
                [
                    "src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessService.cs",
                    "src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs",
                ],
                CoveringTestsBefore:
                [
                    "Oracle: TenantIsolationConformanceSuiteTest (asserts the synthetic scenario engine outcomes only)",
                    "Non-oracle: Server.Tests/TenantAccess/ConversationTenantAccessServiceTest + ConversationTenantAccessGuardTest (live code, but not part of the oracle)",
                ],
                MeasuredGap: "The oracle asserted the scenario-engine mirror; it never instantiated the live tenant access "
                    + "service or guard. A fail-open flip of the live cross-tenant / non-member deny was caught by nothing in the oracle.",
                FaultInjection: new FaultInjectionV1(
                    BranchFlipped: "ConversationTenantAccessService.DecideFromProjectionState: the non-member deny (MissingMember) returned Allowed.",
                    OriginalOracleResult: "GREEN — the original 260-test oracle stayed all-pass under the flip (blind spot confirmed).",
                    Result: "RED — LiveServiceShouldDenyCrossTenantMemberLeakage failed under the flip (fail-open now caught)."),
                BackfillTests:
                [
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState (unknown/disabled/stale/ambiguous/insufficient/unavailable + gap/rollback/unmapped-role/unmapped-status/malformed-projection/member-poisoned)",
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenTenantBindingIsMissing (missing)",
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenTenantIdIsMalformed",
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenCallerPrincipalIsMalformed",
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldDenyCrossTenantMemberLeakage",
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldDenyContradictoryTenantBindings",
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveGuardShouldNotRunProtectedOperationWhenLiveServiceDenies",
                    "LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldAllowAuthorizedOwner (positive control)",
                ],
                Disposition: "backfilled",
                AddressesAcceptanceCriteria: ["AC2", "AC3"]),
            new BehaviorFindingV1(
                Id: 2,
                Name: "Governance audit-pairing",
                Risk: "MEDIUM",
                ProductionPaths:
                [
                    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs (governed read-model pairing)",
                    "src/Hexalith.Conversations.Server/Governance/ (audit gate + verification; gate is internal)",
                ],
                CoveringTestsBefore:
                [
                    "Oracle: governance covered at outcome level across the adopter/buyer/release-scope suites",
                    "Non-oracle: Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest (reflection inventory; flagged to Story 1.3)",
                ],
                MeasuredGap: "The live audit gate that handlers route through is internal (visible only to the server unit-test "
                    + "assembly) and unreachable from the oracle. The reachable public surface is the materialized governed read "
                    + "model, which the oracle did not assert pairing on.",
                FaultInjection: new FaultInjectionV1(
                    BranchFlipped: "ConversationProjectionMaterializer.Apply(RetentionPolicySet): dropped the audited retention mutation from the read model.",
                    OriginalOracleResult: null,
                    Result: "RED — LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence failed under the flip (dropped pairing now caught)."),
                BackfillTests:
                [
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence (retention + sensitivity + redaction)",
                ],
                Disposition: "backfilled",
                AddressesAcceptanceCriteria: ["AC1", "AC2"]),
            new BehaviorFindingV1(
                Id: 3,
                Name: "Idempotency",
                Risk: "MEDIUM",
                ProductionPaths:
                [
                    "src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs",
                    "src/Hexalith.Conversations/Idempotency/ (decision, outcome, in-memory store)",
                ],
                CoveringTestsBefore:
                [
                    "Oracle: IdempotencyConformanceSuiteTest (synthetic scenario-engine outcomes only)",
                    "Non-oracle: Server.Tests/Idempotency/IdempotentConversationCommandExecutorTest (live executor, not part of the oracle)",
                ],
                MeasuredGap: "The oracle proved the synthetic dedup outcome but never ran the live executor; a broken dedup that "
                    + "re-executed a replay as new work was caught by nothing in the oracle.",
                FaultInjection: new FaultInjectionV1(
                    BranchFlipped: "IdempotentConversationCommandExecutor.ExecuteAsync: the Duplicate arm re-executed the mutation instead of replaying the stored outcome.",
                    OriginalOracleResult: null,
                    Result: "RED — LiveExecutorShouldReplayDuplicateWithoutReinvokingMutation failed under the flip (broken dedup now caught)."),
                BackfillTests:
                [
                    "LiveIdempotencyOracleCharacterizationTest.LiveExecutorShouldReplayDuplicateWithoutReinvokingMutation",
                    "LiveIdempotencyOracleCharacterizationTest.LiveExecutorShouldInvokeMutationOnceForFirstSubmission",
                ],
                Disposition: "backfilled",
                AddressesAcceptanceCriteria: ["AC1", "AC2"]),
            new BehaviorFindingV1(
                Id: 4,
                Name: "Redaction replay",
                Risk: "MEDIUM",
                ProductionPaths:
                [
                    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs (redaction suppression on replay)",
                    "src/Hexalith.Conversations/State/ (target-keyed redaction state)",
                ],
                CoveringTestsBefore:
                [
                    "Oracle: RedactionConformanceSuiteTest (synthetic scenario-engine outcomes only)",
                    "Non-oracle: Server.Tests/Projections/ConversationProjectionMaterializerTest (live replay suppression, not part of the oracle)",
                ],
                MeasuredGap: "The oracle did not exercise the live read-model rebuild; a regression that leaked redacted text when "
                    + "the message event replays after the redaction event was caught by nothing in the oracle.",
                FaultInjection: new FaultInjectionV1(
                    BranchFlipped: "ConversationProjectionMaterializer.Apply(MessageAppended): projected the original text instead of the redaction placeholder.",
                    OriginalOracleResult: null,
                    Result: "RED — LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction failed under the flip (replay leak now caught)."),
                BackfillTests:
                [
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction",
                ],
                Disposition: "backfilled",
                AddressesAcceptanceCriteria: ["AC1", "AC2"]),
            new BehaviorFindingV1(
                Id: 5,
                Name: "Projection freshness",
                Risk: "HIGH",
                ProductionPaths:
                [
                    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs (freshness downgrade)",
                    "src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClassifier.cs",
                ],
                CoveringTestsBefore:
                [
                    "Oracle: AdopterConformanceSuite.CheckProjectionFreshness (proves the STALE state only, via one synthetic fixture)",
                    "Non-oracle: Server.Tests/Projections + Diagnostics (rebuilding/unavailable/gap downgrades, not part of the oracle)",
                ],
                MeasuredGap: "The oracle proved only that a stale projection is degraded; the live materializer's rebuilding / "
                    + "unavailable / gap downgrades and the only-Current-is-trust-bearing rule were unpinned in the oracle.",
                FaultInjection: new FaultInjectionV1(
                    BranchFlipped: "ConversationProjectionMaterializer.CreateFreshness: suppressed the stale downgrade so an over-threshold projection stayed Current.",
                    OriginalOracleResult: null,
                    Result: "RED — LiveMaterializerShouldSurfaceStaleProjectionAsNonTrustBearing failed under the flip (degraded-as-fresh now caught)."),
                BackfillTests:
                [
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldReportCurrentProjectionAsTrustBearing",
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceStaleProjectionAsNonTrustBearing",
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceGapAsRebuildingNonTrustBearing",
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceActiveRebuildAsNonTrustBearing",
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceMetadataWriteFailureAsUnavailable",
                    "LiveProjectionFreshnessOracleCharacterizationTest.LiveFreshnessClassifierShouldNeverPromoteDegradedStatesToCurrent",
                ],
                Disposition: "backfilled",
                AddressesAcceptanceCriteria: ["AC1", "AC2"]),
        ],
        AcceptedGaps:
        [
            new AcceptedGapV1(
                Area: "Governance audit gate (internal fail-closed-on-sink-failure path)",
                Description: "The live audit gate that fails closed when the audit sink throws is internal and visible only to the "
                    + "server unit-test assembly, so the oracle cannot exercise it directly. The oracle backfill instead pins the public "
                    + "materialized pairing invariant; the internal gate stays covered by the server unit tests.",
                Rationale: "Making the internal gate oracle-reachable is an internal-coupling change, which is out of scope for this "
                    + "measurement/backfill story (do not decouple here).",
                ClosingStory: "Story 1.3 (oracle survivability / decoupling triage)"),
            new AcceptedGapV1(
                Area: "Line/branch coverage of every non-safety-critical projection branch",
                Description: "Coverage was used to locate the per-behavior safety-critical branch; exhaustive branch coverage of every "
                    + "projection helper was not pursued where the branch is not a release-gate fail-open risk.",
                Rationale: "This story pins the five release-gate behaviors against fail-open regressions, not 100% branch coverage; the "
                    + "non-gate branches are already exercised by the server unit tests.",
                ClosingStory: "Story 5.1 (full-suite run) keeps these exercised; no dedicated closing story required"),
        ],
        DetectedVariances:
        [
            new DetectedVarianceV1(
                Observation: "All five live decision paths were reachable from the oracle project only because it already references the "
                    + "server assembly; the backfilled tenant/idempotency tests touch server-internal construction. This is the same "
                    + "public-surface-vs-internal-coupling tension Story 1.1 flagged.",
                Handoff: "Story 1.3 owns the public-surface-only survivability triage."),
            new DetectedVarianceV1(
                Observation: "Under heavy parallelism the pre-existing generator test that rewrites a committed evidence file races the "
                    + "validation test that reads it; observed once as a transient content-safety failure, not reproducible across reruns, "
                    + "and the committed file stays byte-identical. Not caused by this story's backfills.",
                Handoff: "Recorded for Story 1.3; not fixed here (would modify an existing oracle test)."),
        ]);

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

    private sealed record BlindSpotAnalysisV1(
        string ArtifactKind,
        string Version,
        string Purpose,
        string BaselineCommit,
        string Branch,
        string WorkingTreeState,
        string StoryReference,
        string RunDate,
        ToolchainV1 Toolchain,
        CommandsV1 Commands,
        OracleStatusV1 OracleStatus,
        IReadOnlyList<BehaviorFindingV1> Behaviors,
        IReadOnlyList<AcceptedGapV1> AcceptedGaps,
        IReadOnlyList<DetectedVarianceV1> DetectedVariances);

    private sealed record ToolchainV1(
        string DotnetSdk,
        string TargetFramework,
        string TestStack,
        string CoverageCollector,
        string MutationApproach);

    private sealed record CommandsV1(string ConformanceOracle, string Coverage, string FaultInjection);

    private sealed record OracleStatusV1(
        int BaselineBeforeBackfill,
        int BackfillTestCount,
        int TotalAfterBackfill,
        string Result,
        string Note);

    private sealed record BehaviorFindingV1(
        int Id,
        string Name,
        string Risk,
        IReadOnlyList<string> ProductionPaths,
        IReadOnlyList<string> CoveringTestsBefore,
        string MeasuredGap,
        FaultInjectionV1 FaultInjection,
        IReadOnlyList<string> BackfillTests,
        string Disposition,
        IReadOnlyList<string> AddressesAcceptanceCriteria);

    private sealed record FaultInjectionV1(
        string BranchFlipped,
        string? OriginalOracleResult,
        string Result);

    private sealed record AcceptedGapV1(string Area, string Description, string Rationale, string ClosingStory);

    private sealed record DetectedVarianceV1(string Observation, string Handoff);
}
