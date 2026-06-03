// <copyright file="ConformanceManifestValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Verifies <see cref="ConformanceManifestValidator"/> against the committed fixture manifest,
/// duplicate detection, waiver enforcement, content-safety, stable serialization, and lifecycle stage coverage.
/// </summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class ConformanceManifestValidationTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset FixedTime = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FixtureManifestShouldPassValidateManifestWithZeroErrors()
    {
        ConformanceManifestV1 manifest = LoadFixture();
        IReadOnlyList<string> errors = ConformanceManifestValidator.ValidateManifest(manifest);
        errors.ShouldBeEmpty($"Fixture validation errors: {string.Join(", ", errors)}");
    }

    [Fact]
    public void ManifestWithDuplicateTestIdShouldReturnDuplicateTestIdError()
    {
        ConformanceManifestRowV1 row1 = BuildRow("dup-test");
        ConformanceManifestRowV1 row2 = BuildRow("dup-test");

        ConformanceManifestV1 manifest = new(
            SchemaVersion.Current,
            "v1-fixture",
            "local-test-release",
            FixedTime,
            [row1, row2],
            []);

        IReadOnlyList<string> errors = ConformanceManifestValidator.ValidateManifest(manifest);
        errors.ShouldContain("duplicate-test-id");
    }

    [Fact]
    public void ManifestWithWaivedEntryMissingWaiverReferenceShouldReturnMissingWaiverReferenceError()
    {
        ConformanceManifestRowV1 waivedRow = new(
            "waived-test",
            "Waived test without reference",
            "FR83",
            null,
            null,
            "Pass criteria",
            ReleaseGateStatus.Waived,
            null,
            "automated-manifest-validation-test",
            "local-ci",
            "evidence-handle",
            "release-engineer",
            ConformanceManifestLifecycleStage.ReleaseEvidence,
            FixedTime);

        ConformanceManifestV1 manifest = new(
            SchemaVersion.Current,
            "v1-fixture",
            "local-test-release",
            FixedTime,
            [waivedRow],
            []);

        IReadOnlyList<string> errors = ConformanceManifestValidator.ValidateManifest(manifest);
        errors.ShouldContain("missing-waiver-reference");
    }

    [Fact]
    public void AllFixtureEntriesShouldPassContentSafetyScan()
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

        ConformanceManifestV1 manifest = LoadFixture();
        string json = JsonSerializer.Serialize(manifest, WebOptions);

        foreach (string fragment in forbidden)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"Manifest JSON must not contain forbidden fragment '{fragment}'.");
        }
    }

    [Fact]
    public void ManifestShouldSerializeToStableCamelCaseJsonAndRoundTripDeterministically()
    {
        ConformanceManifestV1 manifest = BuildSingleEntryManifest();

        string first = JsonSerializer.Serialize(manifest, WebOptions);
        string second = JsonSerializer.Serialize(BuildSingleEntryManifest(), WebOptions);

        first.ShouldBe(second);
        first.ShouldContain("\"manifestVersion\":");
        first.ShouldContain("\"releaseReference\":");
        first.ShouldContain("\"entries\":");

        ConformanceManifestV1? roundTripped = JsonSerializer.Deserialize<ConformanceManifestV1>(first, WebOptions);
        roundTripped.ShouldNotBeNull();
        roundTripped!.ManifestVersion.ShouldBe(manifest.ManifestVersion);
        roundTripped.ReleaseReference.ShouldBe(manifest.ReleaseReference);
        roundTripped.Entries.Count.ShouldBe(manifest.Entries.Count);
        roundTripped.GeneratedAtUtc.ShouldBe(manifest.GeneratedAtUtc);
    }

    [Fact]
    public void LifecycleStageAllShouldReturnExactlySixStagesMatchingNfr1()
    {
        IReadOnlyList<ConformanceManifestLifecycleStage> all = ConformanceManifestLifecycleStage.All;

        all.Count.ShouldBe(6);
        all.ShouldContain(ConformanceManifestLifecycleStage.DesignReview);
        all.ShouldContain(ConformanceManifestLifecycleStage.AutomatedTest);
        all.ShouldContain(ConformanceManifestLifecycleStage.LoadPerformanceTest);
        all.ShouldContain(ConformanceManifestLifecycleStage.OperationalDrill);
        all.ShouldContain(ConformanceManifestLifecycleStage.ReleaseEvidence);
        all.ShouldContain(ConformanceManifestLifecycleStage.AccessibilityValidation);
    }

    // --- Helpers ---

    private static ConformanceManifestV1 LoadFixture()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "release-evidence", "conformance-manifest-v1-fixture.json");

        path.ShouldSatisfyAllConditions(
            () => File.Exists(path).ShouldBeTrue($"Expected fixture file at '{path}'."));

        string json = File.ReadAllText(path);
        ConformanceManifestV1? manifest = JsonSerializer.Deserialize<ConformanceManifestV1>(json, WebOptions);
        manifest.ShouldNotBeNull();
        return manifest!;
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

    private static ConformanceManifestRowV1 BuildRow(string testId = "test-validation-001")
        => new(
            testId,
            "Manifest validation test row",
            "FR83",
            null,
            null,
            "Validation returns zero errors for a valid manifest",
            ReleaseGateStatus.Pass,
            null,
            "automated-manifest-validation-test",
            "local-ci",
            "conformance-manifest-v1-fixture",
            "release-engineer",
            ConformanceManifestLifecycleStage.ReleaseEvidence,
            FixedTime);

    private static ConformanceManifestV1 BuildSingleEntryManifest()
        => new(
            SchemaVersion.Current,
            "v1-fixture",
            "local-test-release",
            FixedTime,
            [BuildRow()],
            []);
}
