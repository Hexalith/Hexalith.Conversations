// <copyright file="ConformanceManifestContractTest.cs" company="ITANEO">
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
/// Verifies the manifest lifecycle vocabulary, manifest record types, <see cref="ConformanceManifestValidator"/>,
/// JSON shape, round-trip, additive tolerance, and the committed fixture file.
/// </summary>
public sealed class ConformanceManifestContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset FixedTime = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);

    // --- ConformanceManifestLifecycleStage ---

    [Fact]
    public void LifecycleStageShouldCoverExactlySixNfr1Values()
    {
        string[] expected =
        [
            "design-review",
            "automated-test",
            "load-performance-test",
            "operational-drill",
            "release-evidence",
            "accessibility-validation",
        ];
        ConformanceManifestLifecycleStage.All.Count.ShouldBe(6);
        ConformanceManifestLifecycleStage.All.Select(s => s.Value).ShouldBe(expected);
    }

    [Fact]
    public void LifecycleStageShouldRejectSynonyms()
    {
        foreach (string synonym in new[] { "test", "testing", "design", "ops", "review", "load-test" })
        {
            Should.Throw<ArgumentException>(() => ConformanceManifestLifecycleStage.Parse(synonym));
        }
    }

    [Fact]
    public void LifecycleStageShouldRoundTripAllSixValues()
    {
        foreach (ConformanceManifestLifecycleStage stage in ConformanceManifestLifecycleStage.All)
        {
            ConformanceManifestLifecycleStage parsed = ConformanceManifestLifecycleStage.Parse(stage.Value);
            parsed.ShouldBe(stage);
        }
    }

    [Fact]
    public void LifecycleStageShouldSerializeAsClosedVocabularyToken()
    {
        string json = JsonSerializer.Serialize(ConformanceManifestLifecycleStage.ReleaseEvidence, WebOptions);
        json.ShouldBe("\"release-evidence\"");

        ConformanceManifestLifecycleStage? parsed = JsonSerializer.Deserialize<ConformanceManifestLifecycleStage>("\"release-evidence\"", WebOptions);
        parsed.ShouldBe(ConformanceManifestLifecycleStage.ReleaseEvidence);
    }

    [Fact]
    public void LifecycleStageShouldRejectUnknownJsonTokens()
    {
        Should.Throw<Exception>(() => JsonSerializer.Deserialize<ConformanceManifestLifecycleStage>("\"unknown-stage\"", WebOptions));
    }

    // --- ConformanceManifestRowV1 construction validation ---

    [Fact]
    public void ManifestRowShouldRejectNullTestId()
        => Should.Throw<ArgumentException>(() => BuildRow(testId: null!));

    [Fact]
    public void ManifestRowShouldRejectEmptyTestId()
        => Should.Throw<ArgumentException>(() => BuildRow(testId: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectEmptyTestName()
        => Should.Throw<ArgumentException>(() => BuildRow(testName: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectEmptyRequirementId()
        => Should.Throw<ArgumentException>(() => BuildRow(requirementId: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectEmptyPassCriteria()
        => Should.Throw<ArgumentException>(() => BuildRow(passCriteria: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectEmptyEvidenceHandle()
        => Should.Throw<ArgumentException>(() => BuildRow(evidenceHandle: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectEmptyOwner()
        => Should.Throw<ArgumentException>(() => BuildRow(owner: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectEmptyEnvironment()
        => Should.Throw<ArgumentException>(() => BuildRow(environment: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectEmptyMeasurementMethod()
        => Should.Throw<ArgumentException>(() => BuildRow(measurementMethod: string.Empty));

    [Fact]
    public void ManifestRowShouldRejectNullLifecycleStage()
        => Should.Throw<ArgumentNullException>(
            () => new ConformanceManifestRowV1(
                "test-id-001",
                "Test name",
                "FR83",
                null,
                null,
                "Pass criteria",
                ReleaseGateStatus.Pass,
                null,
                "automated-test",
                "local-ci",
                "evidence-handle",
                "release-engineer",
                null!,
                FixedTime));

    [Fact]
    public void ManifestRowShouldRejectNonUtcTimestamp()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => BuildRow(registeredAt: new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.FromHours(1))));

    [Fact]
    public void ManifestRowShouldAcceptNullReleaseGateId()
    {
        ConformanceManifestRowV1 row = BuildRow(releaseGateId: null);
        row.ReleaseGateId.ShouldBeNull();
    }

    [Fact]
    public void ManifestRowShouldAcceptNullWaiverReferenceEvenWhenStatusIsWaived()
    {
        // Constructor does NOT enforce waiver reference; validator does
        ConformanceManifestRowV1 row = BuildRow(releaseDecisionStatus: ReleaseGateStatus.Waived, waiverReference: null);
        row.WaiverReference.ShouldBeNull();
    }

    [Fact]
    public void ManifestRowShouldRejectUnsafeFreeTextInTestName()
        => Should.Throw<ArgumentException>(() => BuildRow(testName: "EventStore snapshot C:\\path\\raw exception"));

    // --- ConformanceManifestChangeV1 construction validation ---

    [Fact]
    public void ManifestChangeShouldRejectNullChangeId()
        => Should.Throw<ArgumentException>(() => BuildChange(changeId: null!));

    [Fact]
    public void ManifestChangeShouldRejectEmptyChangeId()
        => Should.Throw<ArgumentException>(() => BuildChange(changeId: string.Empty));

    [Fact]
    public void ManifestChangeShouldRejectEmptyChangeSummary()
        => Should.Throw<ArgumentException>(() => BuildChange(changeSummary: string.Empty));

    [Fact]
    public void ManifestChangeShouldRejectEmptyAffectedRequirementIds()
        => Should.Throw<ArgumentException>(() => BuildChange(affectedIds: []));

    [Fact]
    public void ManifestChangeShouldRejectNonUtcTimestamp()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => BuildChange(changedAt: new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.FromHours(2))));

    [Fact]
    public void ManifestChangeShouldRejectEmptyChangedBy()
        => Should.Throw<ArgumentException>(() => BuildChange(changedBy: string.Empty));

    // --- ConformanceManifestV1 construction validation ---

    [Fact]
    public void ManifestV1ShouldRejectNullSchemaVersion()
        => Should.Throw<ArgumentNullException>(
            () => new ConformanceManifestV1(
                null!,
                "v1-fixture",
                "local-test-release",
                FixedTime,
                [BuildRow()],
                []));

    [Fact]
    public void ManifestV1ShouldRejectEmptyManifestVersion()
        => Should.Throw<ArgumentException>(() => BuildManifest(manifestVersion: string.Empty));

    [Fact]
    public void ManifestV1ShouldRejectEmptyReleaseReference()
        => Should.Throw<ArgumentException>(() => BuildManifest(releaseReference: string.Empty));

    [Fact]
    public void ManifestV1ShouldRejectEmptyEntriesList()
        => Should.Throw<ArgumentException>(() => BuildManifest(entries: []));

    [Fact]
    public void ManifestV1ShouldRejectNullEntryInList()
    {
        ConformanceManifestRowV1[] entriesWithNull = [null!];
        Should.Throw<ArgumentException>(() => BuildManifest(entries: entriesWithNull));
    }

    [Fact]
    public void ManifestV1ShouldRejectNullChangeLog()
        => Should.Throw<ArgumentNullException>(
            () => new ConformanceManifestV1(
                SchemaVersion.Current,
                "v1-fixture",
                "local-test-release",
                FixedTime,
                [BuildRow()],
                null!));

    [Fact]
    public void ManifestV1ShouldRejectNonUtcGeneratedAtUtc()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => BuildManifest(generatedAt: new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.FromHours(1))));

    // --- ConformanceManifestValidator ---

    [Fact]
    public void ValidatorShouldReturnEmptyListForValidManifest()
    {
        ConformanceManifestV1 manifest = BuildManifest();
        IReadOnlyList<string> errors = ConformanceManifestValidator.ValidateManifest(manifest);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidatorShouldReturnDuplicateTestIdErrorForDuplicateTestIds()
    {
        ConformanceManifestRowV1 row1 = BuildRow(testId: "duplicate-test");
        ConformanceManifestRowV1 row2 = BuildRow(testId: "duplicate-test");

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
    public void ValidatorShouldReturnMissingWaiverReferenceForWaivedWithoutRef()
    {
        ConformanceManifestRowV1 waivedRow = BuildRow(
            releaseDecisionStatus: ReleaseGateStatus.Waived,
            waiverReference: null);

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

    // --- JSON shape, round-trip, additive tolerance ---

    [Fact]
    public void ManifestRowShouldSerializeToStableCamelCaseWebJson()
    {
        ConformanceManifestRowV1 row = BuildRow();
        string json = JsonSerializer.Serialize(row, WebOptions);

        json.ShouldContain("\"testId\":");
        json.ShouldContain("\"testName\":");
        json.ShouldContain("\"requirementId\":");
        json.ShouldContain("\"passCriteria\":");
        json.ShouldContain("\"lifecycleStage\":");
        json.ShouldContain("\"releaseDecisionStatus\":");
        json.ShouldNotContain("\"TestId\"", Case.Sensitive);
    }

    [Fact]
    public void ManifestRowShouldRoundTripLosslessly()
    {
        ConformanceManifestRowV1 row = BuildRow();
        string json = JsonSerializer.Serialize(row, WebOptions);
        ConformanceManifestRowV1? parsed = JsonSerializer.Deserialize<ConformanceManifestRowV1>(json, WebOptions);

        parsed.ShouldNotBeNull();
        parsed!.TestId.ShouldBe(row.TestId);
        parsed.TestName.ShouldBe(row.TestName);
        parsed.RequirementId.ShouldBe(row.RequirementId);
        parsed.LifecycleStage.ShouldBe(row.LifecycleStage);
        parsed.ReleaseDecisionStatus.ShouldBe(row.ReleaseDecisionStatus);
        parsed.Owner.ShouldBe(row.Owner);
        parsed.Environment.ShouldBe(row.Environment);
        parsed.RegisteredAtUtc.ShouldBe(row.RegisteredAtUtc);
    }

    [Fact]
    public void ManifestRowShouldTolerateAdditiveJson()
    {
        ConformanceManifestRowV1 row = BuildRow();
        string json = JsonSerializer.Serialize(row, WebOptions);
        JsonNode node = JsonNode.Parse(json)!;
        node["futureField"] = "ignored";

        ConformanceManifestRowV1? parsed = JsonSerializer.Deserialize<ConformanceManifestRowV1>(node.ToJsonString(), WebOptions);
        parsed.ShouldNotBeNull();
        parsed!.TestId.ShouldBe(row.TestId);
    }

    [Fact]
    public void ManifestV1ShouldRoundTripLosslessly()
    {
        ConformanceManifestV1 manifest = BuildManifest();
        string json = JsonSerializer.Serialize(manifest, WebOptions);
        ConformanceManifestV1? parsed = JsonSerializer.Deserialize<ConformanceManifestV1>(json, WebOptions);

        parsed.ShouldNotBeNull();
        parsed!.ManifestVersion.ShouldBe(manifest.ManifestVersion);
        parsed.ReleaseReference.ShouldBe(manifest.ReleaseReference);
        parsed.Entries.Count.ShouldBe(manifest.Entries.Count);
        parsed.ChangeLog.Count.ShouldBe(manifest.ChangeLog.Count);
        parsed.GeneratedAtUtc.ShouldBe(manifest.GeneratedAtUtc);
        parsed.SchemaVersion.ShouldBe(manifest.SchemaVersion);
    }

    [Fact]
    public void ManifestV1ShouldTolerateAdditiveJson()
    {
        ConformanceManifestV1 manifest = BuildManifest();
        string json = JsonSerializer.Serialize(manifest, WebOptions);
        JsonNode node = JsonNode.Parse(json)!;
        node["futureField"] = "ignored";

        ConformanceManifestV1? parsed = JsonSerializer.Deserialize<ConformanceManifestV1>(node.ToJsonString(), WebOptions);
        parsed.ShouldNotBeNull();
        parsed!.Entries.Count.ShouldBe(manifest.Entries.Count);
    }

    // --- Fixture file validation ---

    [Fact]
    public void FixtureFileShouldExistAndDeserializeWithoutError()
    {
        string path = GetFixturePath();
        File.Exists(path).ShouldBeTrue($"Expected fixture file at '{path}'.");

        string json = File.ReadAllText(path);
        ConformanceManifestV1? manifest = JsonSerializer.Deserialize<ConformanceManifestV1>(json, WebOptions);
        manifest.ShouldNotBeNull();
    }

    [Fact]
    public void FixtureFileShouldPassValidateManifestWithZeroDiagnostics()
    {
        string json = File.ReadAllText(GetFixturePath());
        ConformanceManifestV1 manifest = JsonSerializer.Deserialize<ConformanceManifestV1>(json, WebOptions)!;

        IReadOnlyList<string> errors = ConformanceManifestValidator.ValidateManifest(manifest);
        errors.ShouldBeEmpty($"Fixture validation errors: {string.Join(", ", errors)}");
    }

    [Fact]
    public void FixtureFileShouldContainAtLeastThreeEntries()
    {
        string json = File.ReadAllText(GetFixturePath());
        ConformanceManifestV1 manifest = JsonSerializer.Deserialize<ConformanceManifestV1>(json, WebOptions)!;

        manifest.Entries.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void FixtureFileShouldContainV1TelemetryAndRenderedUiWaiverEvidence()
    {
        string json = File.ReadAllText(GetFixturePath());
        ConformanceManifestV1 manifest = JsonSerializer.Deserialize<ConformanceManifestV1>(json, WebOptions)!;
        string[] testIds = manifest.Entries.Select(e => e.TestId).ToArray();

        testIds.ShouldContain("story-6-8a-operational-telemetry-redaction");
        testIds.ShouldContain("story-6-8b-operational-telemetry-cardinality");
        testIds.ShouldContain("story-3-8-rendered-ui-verification-waiver");

        ConformanceManifestRowV1 waiver = manifest.Entries.Single(e => e.TestId == "story-3-8-rendered-ui-verification-waiver");
        waiver.ReleaseDecisionStatus.ShouldBe(ReleaseGateStatus.Waived);
        waiver.WaiverReference.ShouldBe("waiver-story-3-8-investigation-workspace-ui-host");
        waiver.RequirementId.ShouldBe("NFR69");

        manifest.ChangeLog.Select(c => c.ChangeId)
            .ShouldContain("v1-2026-05-24-telemetry-and-ui-waiver");
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

        string json = File.ReadAllText(GetFixturePath());
        foreach (string fragment in forbidden)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"Fixture must not contain forbidden fragment '{fragment}'.");
        }
    }

    // --- Helpers ---

    private static string GetFixturePath()
    {
        string root = FindRepositoryRoot();
        return Path.Combine(root, "docs", "release-evidence", "conformance-manifest-v1-fixture.json");
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

    private static ConformanceManifestRowV1 BuildRow(
        string testId = "test-row-001",
        string testName = "Manifest row construction test",
        string requirementId = "FR83",
        string? carryForwardRef = null,
        ReleaseGateId? releaseGateId = null,
        string passCriteria = "Test passes when the manifest row is constructed without error",
        ReleaseGateStatus? releaseDecisionStatus = null,
        string? waiverReference = null,
        string measurementMethod = "automated-manifest-validation-test",
        string environment = "local-ci",
        string evidenceHandle = "conformance-manifest-v1-fixture",
        string owner = "release-engineer",
        ConformanceManifestLifecycleStage? lifecycleStage = null,
        DateTimeOffset? registeredAt = null)
        => new(
            testId,
            testName,
            requirementId,
            carryForwardRef,
            releaseGateId,
            passCriteria,
            releaseDecisionStatus ?? ReleaseGateStatus.Pass,
            waiverReference,
            measurementMethod,
            environment,
            evidenceHandle,
            owner,
            lifecycleStage ?? ConformanceManifestLifecycleStage.ReleaseEvidence,
            registeredAt ?? FixedTime);

    private static ConformanceManifestChangeV1 BuildChange(
        string changeId = "change-001",
        string changeSummary = "Initial manifest version created for fixture",
        IReadOnlyList<string>? affectedIds = null,
        DateTimeOffset? changedAt = null,
        string changedBy = "release-engineer")
        => new(
            changeId,
            changeSummary,
            affectedIds ?? (IReadOnlyList<string>)["FR83", "FR84"],
            changedAt ?? FixedTime,
            changedBy);

    private static ConformanceManifestV1 BuildManifest(
        SchemaVersion? schemaVersion = null,
        string manifestVersion = "v1-fixture",
        string releaseReference = "local-test-release",
        DateTimeOffset? generatedAt = null,
        IReadOnlyList<ConformanceManifestRowV1>? entries = null,
        IReadOnlyList<ConformanceManifestChangeV1>? changeLog = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            manifestVersion,
            releaseReference,
            generatedAt ?? FixedTime,
            entries ?? [BuildRow()],
            changeLog ?? []);
}
