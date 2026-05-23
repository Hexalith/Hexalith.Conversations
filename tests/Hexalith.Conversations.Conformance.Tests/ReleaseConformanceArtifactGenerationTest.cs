// <copyright file="ReleaseConformanceArtifactGenerationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Testing.Fixtures;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Verifies that <see cref="ReleaseConformanceArtifactBuilder"/> produces a valid, deterministic,
/// content-safe <see cref="ReleaseConformanceArtifactV1"/> from the CORE fixture conformance run result,
/// and generates the committed fixture artifact file.
/// </summary>
public sealed class ReleaseConformanceArtifactGenerationTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset FixedTime = new(2026, 5, 23, 10, 0, 0, TimeSpan.Zero);

    private static ReleaseConformanceArtifactV1 BuildFromCoreFixture(DateTimeOffset? at = null)
    {
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();
        ConformanceRunResultV1 runResult = new AdopterConformanceSuite(fixture).Run();

        return new ReleaseConformanceArtifactBuilder(
            runResult,
            "ci-build-test-fixture",
            "test-runner",
            "test-env-local",
            "synthetic-dataset",
            "1032 tests",
            "release-manifest-stub",
            new FixedTimeProvider(at ?? FixedTime))
            .Build();
    }

    [Fact]
    public void BuilderShouldProduceValidArtifactFromCoreFixture()
    {
        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();

        IReadOnlyList<string> errors = ReleaseConformanceArtifactV1.ValidateArtifact(artifact);
        errors.ShouldBeEmpty($"Artifact validation errors: {string.Join(", ", errors)}");
    }

    [Fact]
    public void ArtifactShouldContainAllSevenRequiredGateEntries()
    {
        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();

        foreach (ReleaseGateId gate in ReleaseGateId.All)
        {
            artifact.GateResults.ShouldContain(
                r => r.GateId.Equals(gate),
                $"Artifact should contain gate result for '{gate.Value}'.");
        }
    }

    [Fact]
    public void OverallStatusShouldBeDeterministicAcrossRuns()
    {
        ReleaseConformanceArtifactV1 first = BuildFromCoreFixture();
        ReleaseConformanceArtifactV1 second = BuildFromCoreFixture();

        first.OverallStatus.ShouldBe(second.OverallStatus);
    }

    [Fact]
    public void AuditIntegrityGateShouldBePassWhenGovernancePreconditionIsReady()
    {
        // GovernancePrecondition check yields Ready outcome → audit-integrity gate should be pass
        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();

        ReleaseGateResultV1? auditGate = artifact.GateResults
            .SingleOrDefault(g => g.GateId.Equals(ReleaseGateId.AuditIntegrity));

        auditGate.ShouldNotBeNull();
        auditGate!.Status.ShouldBe(ReleaseGateStatus.Pass);
    }

    [Fact]
    public void TenantIsolationGateShouldBeUnknownAcceptedBecauseTenantBindingUsesUnknownOutcome()
    {
        // TenantBinding conformant check uses Unknown outcome (side-channel shape), not Ready → gate is unknown-accepted
        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();

        ReleaseGateResultV1? tenantGate = artifact.GateResults
            .SingleOrDefault(g => g.GateId.Equals(ReleaseGateId.TenantIsolation));

        tenantGate.ShouldNotBeNull();
        tenantGate!.Status.ShouldBe(ReleaseGateStatus.UnknownAccepted);
    }

    [Fact]
    public void ProviderPortabilityGateShouldAlwaysBeUnknownAcceptedWithNoAdopterMapping()
    {
        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();

        ReleaseGateResultV1? portabilityGate = artifact.GateResults
            .SingleOrDefault(g => g.GateId.Equals(ReleaseGateId.ProviderPortability));

        portabilityGate.ShouldNotBeNull();
        portabilityGate!.Status.ShouldBe(ReleaseGateStatus.UnknownAccepted);
    }

    [Fact]
    public void AllGateStatusesShouldBelongToTheClosedVocabulary()
    {
        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();

        artifact.GateResults.ShouldAllBe(r => ReleaseGateStatus.All.Contains(r.Status));
    }

    [Fact]
    public void ArtifactContentSafetyScanShouldPass()
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

        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();
        string json = JsonSerializer.Serialize(artifact, WebOptions);

        foreach (string fragment in forbidden)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"Artifact JSON must not contain forbidden fragment '{fragment}'.");
        }
    }

    [Fact]
    public void BuilderShouldRejectNullConformanceRunResult()
        => Should.Throw<ArgumentNullException>(
            () => new ReleaseConformanceArtifactBuilder(
                null!,
                "ci-build-test-fixture",
                "test-runner",
                "test-env-local",
                "synthetic-dataset",
                "1032 tests",
                "release-manifest-stub",
                new FixedTimeProvider(FixedTime)));

    [Fact]
    public void BuilderShouldRejectNullSignerOrRunnerId()
        => Should.Throw<ArgumentNullException>(
            () => new ReleaseConformanceArtifactBuilder(
                new AdopterConformanceSuite(ConversationConformanceCoreFixtures.Create()).Run(),
                "ci-build-test-fixture",
                null!,
                "test-env-local",
                "synthetic-dataset",
                "1032 tests",
                "release-manifest-stub",
                new FixedTimeProvider(FixedTime)));

    [Fact]
    public void BuilderShouldBeFullyDeterministicWithSameInputs()
    {
        ReleaseConformanceArtifactV1 first = BuildFromCoreFixture(FixedTime);
        ReleaseConformanceArtifactV1 second = BuildFromCoreFixture(FixedTime);

        string firstJson = JsonSerializer.Serialize(first, WebOptions);
        string secondJson = JsonSerializer.Serialize(second, WebOptions);

        firstJson.ShouldBe(secondJson);
    }

    [Fact]
    public void GenerateAndSaveFixtureArtifactFile()
    {
        // This test generates the committed fixture artifact file at docs/release-evidence/
        // and also validates the generated artifact in the same pass.
        ReleaseConformanceArtifactV1 artifact = BuildFromCoreFixture();
        string json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions(WebOptions) { WriteIndented = true });

        string root = FindRepositoryRoot();
        string dir = Path.Combine(root, "docs", "release-evidence");
        string path = Path.Combine(dir, "release-conformance-artifact-v1-fixture.json");

        Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);

        // Validate the generated file round-trips correctly
        string readBack = File.ReadAllText(path);
        ReleaseConformanceArtifactV1? parsed = JsonSerializer.Deserialize<ReleaseConformanceArtifactV1>(readBack, WebOptions);
        parsed.ShouldNotBeNull();
        IReadOnlyList<string> errors = ReleaseConformanceArtifactV1.ValidateArtifact(parsed!);
        errors.ShouldBeEmpty();
    }

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

    /// <summary>
    /// Simple test-time <see cref="TimeProvider"/> that always returns a fixed time.
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset time) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => time;
    }
}
