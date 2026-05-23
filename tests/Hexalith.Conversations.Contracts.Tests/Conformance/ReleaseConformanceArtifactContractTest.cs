// <copyright file="ReleaseConformanceArtifactContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies the release gate vocabulary, artifact record, <see cref="ReleaseConformanceArtifactV1.ValidateArtifact"/>,
/// <see cref="ReleaseConformanceArtifactV1.OverallStatus"/> computation, JSON shape, and the committed fixture file.
/// </summary>
public sealed class ReleaseConformanceArtifactContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset FixedTime = new(2026, 5, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReleaseGateStatusShouldCoverAllFourRequiredValues()
    {
        string[] expected = ["pass", "fail", "waived", "unknown-accepted"];
        ReleaseGateStatus.All.Select(s => s.Value).ShouldBe(expected);
    }

    [Fact]
    public void ReleaseGateStatusShouldRejectSynonyms()
    {
        foreach (string synonym in new[] { "ok", "pass-ish", "green", "red", "skip", "blocked", "ready" })
        {
            Should.Throw<ArgumentException>(() => ReleaseGateStatus.Parse(synonym));
        }
    }

    [Fact]
    public void ReleaseGateStatusShouldSerializeAsClosedVocabularyTokens()
    {
        string json = JsonSerializer.Serialize(ReleaseGateStatus.Pass, WebOptions);
        json.ShouldBe("\"pass\"");

        ReleaseGateStatus? parsed = JsonSerializer.Deserialize<ReleaseGateStatus>("\"pass\"", WebOptions);
        parsed.ShouldBe(ReleaseGateStatus.Pass);
    }

    [Fact]
    public void ReleaseGateStatusIsBlockingShouldOnlyBeTrueForFail()
    {
        ReleaseGateStatus.Fail.IsBlocking.ShouldBeTrue();
        ReleaseGateStatus.Pass.IsBlocking.ShouldBeFalse();
        ReleaseGateStatus.Waived.IsBlocking.ShouldBeFalse();
        ReleaseGateStatus.UnknownAccepted.IsBlocking.ShouldBeFalse();
    }

    [Fact]
    public void ReleaseGateIdShouldCoverAllSevenRequiredGates()
    {
        string[] expected =
        [
            "tenant-isolation",
            "audit-integrity",
            "redaction-non-leakage",
            "unsupported-schema-rejection",
            "projection-rebuild-determinism",
            "contract-compatibility",
            "provider-portability",
        ];
        ReleaseGateId.All.Select(g => g.Value).ShouldBe(expected);
    }

    [Fact]
    public void ReleaseGateIdShouldRejectUnknownGateIds()
    {
        foreach (string unknown in new[] { "tenant-leak", "unknown-gate", "pass", "fail", "ready" })
        {
            Should.Throw<ArgumentException>(() => ReleaseGateId.Parse(unknown));
        }
    }

    [Fact]
    public void ReleaseGateIdShouldSerializeAsClosedVocabularyTokens()
    {
        string json = JsonSerializer.Serialize(ReleaseGateId.TenantIsolation, WebOptions);
        json.ShouldBe("\"tenant-isolation\"");

        ReleaseGateId? parsed = JsonSerializer.Deserialize<ReleaseGateId>("\"tenant-isolation\"", WebOptions);
        parsed.ShouldBe(ReleaseGateId.TenantIsolation);
    }

    [Fact]
    public void ReleaseGateResultV1ShouldRejectNullGateId()
        => Should.Throw<ArgumentNullException>(
            () => new ReleaseGateResultV1(null!, ReleaseGateStatus.Pass, "Evidence.", "handle-001", FixedTime, "FR86"));

    [Fact]
    public void ReleaseGateResultV1ShouldRejectNullStatus()
        => Should.Throw<ArgumentNullException>(
            () => new ReleaseGateResultV1(ReleaseGateId.TenantIsolation, null!, "Evidence.", "handle-001", FixedTime, "FR86"));

    [Fact]
    public void ReleaseGateResultV1ShouldRejectEmptyEvidenceHandle()
        => Should.Throw<ArgumentException>(
            () => new ReleaseGateResultV1(ReleaseGateId.TenantIsolation, ReleaseGateStatus.Pass, "Evidence.", string.Empty, FixedTime, "FR86"));

    [Fact]
    public void ReleaseGateResultV1ShouldRejectEmptyRequirementId()
        => Should.Throw<ArgumentException>(
            () => new ReleaseGateResultV1(ReleaseGateId.TenantIsolation, ReleaseGateStatus.Pass, "Evidence.", "handle-001", FixedTime, string.Empty));

    [Fact]
    public void ReleaseGateResultV1ShouldRejectEmptyEvidenceSummary()
        => Should.Throw<ArgumentException>(
            () => new ReleaseGateResultV1(ReleaseGateId.TenantIsolation, ReleaseGateStatus.Pass, string.Empty, "handle-001", FixedTime, "FR86"));

    [Fact]
    public void ReleaseGateResultV1ShouldRejectNonUtcTimestamp()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => new ReleaseGateResultV1(
                ReleaseGateId.TenantIsolation,
                ReleaseGateStatus.Pass,
                "Evidence.",
                "handle-001",
                new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.FromHours(1)),
                "FR86"));

    [Fact]
    public void ReleaseConformanceArtifactV1ShouldRejectEmptyBuildHash()
        => Should.Throw<ArgumentException>(
            () => BuildArtifact(buildHash: string.Empty));

    [Fact]
    public void ReleaseConformanceArtifactV1ShouldRejectMissingSignerOrRunnerId()
        => Should.Throw<ArgumentException>(
            () => BuildArtifact(signerOrRunnerId: string.Empty));

    [Fact]
    public void ReleaseConformanceArtifactV1ShouldRejectNullSchemaVersion()
        => Should.Throw<ArgumentNullException>(
            () => new ReleaseConformanceArtifactV1(
                null!,
                "ci-build-hash",
                "test-runner",
                "test-env",
                "synthetic-dataset",
                "1032 tests",
                "release-manifest-stub",
                FixedTime,
                [SchemaVersion.Current],
                ["hexalith-conversations-contracts-1.0.0"],
                ["evidence-handle-001"],
                BuildAllGateResults(FixedTime)));

    [Fact]
    public void ReleaseConformanceArtifactV1ShouldRejectEmptyGateList()
        => Should.Throw<ArgumentException>(
            () => new ReleaseConformanceArtifactV1(
                SchemaVersion.Current,
                "ci-build-hash",
                "test-runner",
                "test-env",
                "synthetic-dataset",
                "1032 tests",
                "release-manifest-stub",
                FixedTime,
                [SchemaVersion.Current],
                ["hexalith-conversations-contracts-1.0.0"],
                ["evidence-handle-001"],
                []));

    [Fact]
    public void ReleaseConformanceArtifactV1ShouldRejectIncompleteGateList()
    {
        // Only one gate result - missing the other six
        Should.Throw<ArgumentException>(
            () => new ReleaseConformanceArtifactV1(
                SchemaVersion.Current,
                "ci-build-hash",
                "test-runner",
                "test-env",
                "synthetic-dataset",
                "1032 tests",
                "release-manifest-stub",
                FixedTime,
                [SchemaVersion.Current],
                ["hexalith-conversations-contracts-1.0.0"],
                ["evidence-handle-001"],
                [BuildGateResult(ReleaseGateId.TenantIsolation, ReleaseGateStatus.Pass, FixedTime)]));
    }

    [Fact]
    public void ValidateArtifactShouldReturnNoErrorsForValidArtifact()
    {
        ReleaseConformanceArtifactV1 valid = BuildArtifact();
        IReadOnlyList<string> errors = ReleaseConformanceArtifactV1.ValidateArtifact(valid);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateArtifactShouldReturnErrorWhenAllSevenGatesNotPresent()
    {
        // Build artifact missing provider-portability gate to trigger validation
        ReleaseGateResultV1[] incompleteGates = ReleaseGateId.All
            .Where(g => !g.Equals(ReleaseGateId.ProviderPortability))
            .Select((g, i) => BuildGateResult(g, ReleaseGateStatus.Pass, FixedTime, i))
            .ToArray();

        // Constructor will reject incomplete gate list
        Should.Throw<ArgumentException>(
            () => new ReleaseConformanceArtifactV1(
                SchemaVersion.Current,
                "ci-build-hash",
                "test-runner",
                "test-env",
                "synthetic-dataset",
                "1032 tests",
                "release-manifest-stub",
                FixedTime,
                [SchemaVersion.Current],
                ["hexalith-conversations-contracts-1.0.0"],
                ["evidence-handle-001"],
                incompleteGates));
    }

    [Fact]
    public void OverallStatusShouldBePassWhenAllGatesPass()
    {
        ReleaseConformanceArtifactV1 artifact = BuildArtifact(allStatus: ReleaseGateStatus.Pass);
        artifact.OverallStatus.ShouldBe(ReleaseGateStatus.Pass);
    }

    [Fact]
    public void OverallStatusShouldBeFailWhenAnyGateFails()
    {
        ReleaseGateResultV1[] gates = BuildAllGateResults(FixedTime, overrideFirst: ReleaseGateStatus.Fail);
        ReleaseConformanceArtifactV1 artifact = new(
            SchemaVersion.Current, "ci-build-hash", "test-runner", "test-env",
            "synthetic-dataset", "1032 tests", "release-manifest-stub", FixedTime,
            [SchemaVersion.Current], ["hexalith-conversations-contracts-1.0.0"],
            ["evidence-handle-001"], gates);

        artifact.OverallStatus.ShouldBe(ReleaseGateStatus.Fail);
    }

    [Fact]
    public void OverallStatusShouldBeWaivedWhenNoFailAndSomeWaived()
    {
        ReleaseGateResultV1[] gates = BuildAllGateResults(FixedTime, overrideFirst: ReleaseGateStatus.Waived);
        ReleaseConformanceArtifactV1 artifact = new(
            SchemaVersion.Current, "ci-build-hash", "test-runner", "test-env",
            "synthetic-dataset", "1032 tests", "release-manifest-stub", FixedTime,
            [SchemaVersion.Current], ["hexalith-conversations-contracts-1.0.0"],
            ["evidence-handle-001"], gates);

        artifact.OverallStatus.ShouldBe(ReleaseGateStatus.Waived);
    }

    [Fact]
    public void OverallStatusShouldBeUnknownAcceptedWhenMixedPassAndUnknownAccepted()
    {
        // Some pass, some unknown-accepted, no fail, no waived → unknown-accepted
        ReleaseGateResultV1[] gates = BuildAllGateResults(FixedTime, overrideFirst: ReleaseGateStatus.UnknownAccepted);
        ReleaseConformanceArtifactV1 artifact = new(
            SchemaVersion.Current, "ci-build-hash", "test-runner", "test-env",
            "synthetic-dataset", "1032 tests", "release-manifest-stub", FixedTime,
            [SchemaVersion.Current], ["hexalith-conversations-contracts-1.0.0"],
            ["evidence-handle-001"], gates);

        artifact.OverallStatus.ShouldBe(ReleaseGateStatus.UnknownAccepted);
    }

    [Fact]
    public void ArtifactShouldSerializeToStableCamelCaseWebJson()
    {
        ReleaseConformanceArtifactV1 artifact = BuildArtifact();

        string first = JsonSerializer.Serialize(artifact, WebOptions);
        string second = JsonSerializer.Serialize(BuildArtifact(), WebOptions);

        first.ShouldBe(second);
        first.ShouldContain("\"buildHash\":");
        first.ShouldContain("\"signerOrRunnerId\":");
        first.ShouldContain("\"gateResults\":");
        first.ShouldContain("\"overallStatus\":");
        first.ShouldNotContain("\"BuildHash\"", Case.Sensitive);
    }

    [Fact]
    public void ArtifactShouldRoundTripLosslessly()
    {
        ReleaseConformanceArtifactV1 artifact = BuildArtifact();

        string json = JsonSerializer.Serialize(artifact, WebOptions);
        ReleaseConformanceArtifactV1? parsed = JsonSerializer.Deserialize<ReleaseConformanceArtifactV1>(json, WebOptions);

        parsed.ShouldNotBeNull();
        parsed!.BuildHash.ShouldBe(artifact.BuildHash);
        parsed.SignerOrRunnerId.ShouldBe(artifact.SignerOrRunnerId);
        parsed.GateResults.Count.ShouldBe(artifact.GateResults.Count);
        parsed.OverallStatus.ShouldBe(artifact.OverallStatus);
    }

    [Fact]
    public void ArtifactShouldTolerateAdditiveJson()
    {
        ReleaseConformanceArtifactV1 artifact = BuildArtifact();

        string json = JsonSerializer.Serialize(artifact, WebOptions);
        JsonNode node = JsonNode.Parse(json)!;
        node["futureField"] = "ignored";

        ReleaseConformanceArtifactV1? parsed = JsonSerializer.Deserialize<ReleaseConformanceArtifactV1>(node.ToJsonString(), WebOptions);
        parsed.ShouldNotBeNull();
        parsed!.GateResults.Count.ShouldBe(ReleaseGateId.All.Count);
    }

    [Fact]
    public void FixtureFileShouldExistAndBeValid()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "release-evidence", "release-conformance-artifact-v1-fixture.json");

        File.Exists(path).ShouldBeTrue($"Expected fixture file to exist at '{path}'.");

        string json = File.ReadAllText(path);
        ReleaseConformanceArtifactV1? artifact = JsonSerializer.Deserialize<ReleaseConformanceArtifactV1>(json, WebOptions);

        artifact.ShouldNotBeNull();
        IReadOnlyList<string> errors = ReleaseConformanceArtifactV1.ValidateArtifact(artifact!);
        errors.ShouldBeEmpty($"Fixture validation errors: {string.Join(", ", errors)}");
    }

    [Fact]
    public void FixtureFileShouldContainAllSevenGateIds()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "release-evidence", "release-conformance-artifact-v1-fixture.json");

        File.Exists(path).ShouldBeTrue();
        string json = File.ReadAllText(path);

        ReleaseConformanceArtifactV1 artifact = JsonSerializer.Deserialize<ReleaseConformanceArtifactV1>(json, WebOptions)!;
        foreach (ReleaseGateId gate in ReleaseGateId.All)
        {
            artifact.GateResults.ShouldContain(
                r => r.GateId.Equals(gate),
                $"Fixture should contain gate result for '{gate.Value}'.");
        }
    }

    [Fact]
    public void FixtureFileShouldPassContentSafetyScan()
    {
        string[] forbidden =
        [
            "EventStore",
            "snapshot",
            "provider payload",
            "raw exception",
            "C:\\",
            "D:\\",
        ];

        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "release-evidence", "release-conformance-artifact-v1-fixture.json");

        File.Exists(path).ShouldBeTrue();
        string json = File.ReadAllText(path);

        foreach (string fragment in forbidden)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"Fixture file must not contain forbidden fragment '{fragment}'.");
        }
    }

    private static ReleaseConformanceArtifactV1 BuildArtifact(
        string buildHash = "ci-build-hash",
        string signerOrRunnerId = "test-runner",
        ReleaseGateStatus? allStatus = null)
    {
        IReadOnlyList<ReleaseGateResultV1> gates = allStatus is null
            ? BuildAllGateResults(FixedTime)
            : ReleaseGateId.All.Select((g, i) => BuildGateResult(g, allStatus, FixedTime, i)).ToArray();

        return new ReleaseConformanceArtifactV1(
            SchemaVersion.Current,
            buildHash,
            signerOrRunnerId,
            "test-env",
            "synthetic-dataset",
            "1032 tests",
            "release-manifest-stub",
            FixedTime,
            [SchemaVersion.Current],
            ["hexalith-conversations-contracts-1.0.0"],
            ["evidence-handle-001"],
            gates);
    }

    private static ReleaseGateResultV1[] BuildAllGateResults(
        DateTimeOffset evaluatedAt,
        ReleaseGateStatus? overrideFirst = null)
        => ReleaseGateId.All
            .Select((g, i) => BuildGateResult(
                g,
                overrideFirst is not null && i == 0 ? overrideFirst : ReleaseGateStatus.Pass,
                evaluatedAt,
                i))
            .ToArray();

    private static ReleaseGateResultV1 BuildGateResult(ReleaseGateId gateId, ReleaseGateStatus status, DateTimeOffset evaluatedAt, int index = 0)
        => new(gateId, status, "Gate result evidence verified.", $"gate-ref-{index:d2}", evaluatedAt, "FR86");

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
