// <copyright file="ReleaseBaselineValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Reflection;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.1 (AC1/AC2/AC4) — validates the COMMITTED behavior-preservation evidence on disk:
/// <c>docs/release-evidence/release-baseline-v1.json</c> (the FR-20 named baseline record) and
/// <c>docs/release-evidence/public-contract-shape-baseline-v1.json</c> (the Story 5.1 contract-shape diff baseline).
/// </summary>
/// <remarks>
/// <para>
/// The companion <see cref="PublicContractShapeSnapshotGenerationTest"/> exercises the in-memory snapshot
/// (determinism, six-area coverage, content-safety, round-trip). It does NOT guard the files that are actually
/// committed: the baseline record is never read back, and the snapshot generator overwrites the on-disk file on
/// every run, so it can never <em>fail</em> on drift. This test closes those gaps by mirroring the
/// <see cref="ConformanceManifestValidationTest"/> pattern — read the committed artifacts and validate them.
/// </para>
/// <para>
/// The load-bearing guard is <see cref="CommittedSnapshotTypeCountShouldMatchTheLiveExportedContractSurface"/>:
/// if any public type is added to or removed from <c>Hexalith.Conversations.Contracts</c> without regenerating the
/// baseline, the committed Story 5.1 reference would silently go stale. Failing fast here is exactly the FR-20 /
/// Story 5.1 behavior-preservation protection ("public contract shapes unchanged or explicitly approved").
/// </para>
/// </remarks>
public sealed class ReleaseBaselineValidationTest
{
    private const string ContractsNamespacePrefix = "Hexalith.Conversations.Contracts";
    private const string SnapshotArtifactFileName = "public-contract-shape-baseline-v1.json";

    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    // AC1 — the 14 *ConformanceSuiteTest classes that make up the oracle, per the story statement.
    private static readonly string[] ExpectedSuiteClasses =
    [
        "AdopterConformanceSuiteTest",
        "BuyerAcceptanceConformanceSuiteTest",
        "ConformanceStatusConformanceSuiteTest",
        "ContractValidationConformanceSuiteTest",
        "EventSchemaEvolutionConformanceSuiteTest",
        "IdempotencyConformanceSuiteTest",
        "PlatformEvidenceSeparationConformanceSuiteTest",
        "ProviderPortabilityConformanceSuiteTest",
        "RedactionConformanceSuiteTest",
        "ReleaseScopeConformanceSuiteTest",
        "SecondAdopterConformanceSuiteTest",
        "TelemetryCardinalityConformanceSuiteTest",
        "TelemetryRedactionConformanceSuiteTest",
        "TenantIsolationConformanceSuiteTest",
    ];

    // Forbidden substrate/host fragments — same vocabulary the existing release-evidence content-safety scans use.
    // Applied ONLY to the captured contract surface (the snapshot's `types` payload), never to the human-authored
    // headers/prose, which legitimately use words like "snapshot" (mirrors PublicContractShapeSnapshotGenerationTest).
    private static readonly string[] ForbiddenFragments =
    [
        "EventStore",
        "snapshot",
        "SignalR",
        "dispatcher",
        "repository",
        "provider payload",
        "raw exception",
        "C:\\",
        "D:\\",
    ];

    // --- AC1 / AC4: committed baseline record ---

    [Fact]
    public void CommittedBaselineRecordShouldExistAndDescribeAGreenAllPassOracle()
    {
        using JsonDocument doc = LoadCommittedJson("release-baseline-v1.json");
        JsonElement root = doc.RootElement;

        root.GetProperty("artifactKind").GetString().ShouldBe("release-conformance-baseline");
        root.GetProperty("version").GetString().ShouldBe("v1");

        JsonElement oracle = root.GetProperty("conformanceOracle");
        oracle.GetProperty("suiteClassCount").GetInt32().ShouldBe(14);
        oracle.GetProperty("conformanceSuiteResult").GetString().ShouldBe("all-pass");

        JsonElement projectTotal = root.GetProperty("projectTotalAtBaseline");
        projectTotal.GetProperty("failed").GetInt32().ShouldBe(0);
        projectTotal.GetProperty("skipped").GetInt32().ShouldBe(0);
        projectTotal.GetProperty("result").GetString().ShouldBe("green");
    }

    [Fact]
    public void BaselineCommitShouldBeAFullFortyCharacterHexShaOnMain()
    {
        using JsonDocument doc = LoadCommittedJson("release-baseline-v1.json");
        JsonElement root = doc.RootElement;

        string? commit = root.GetProperty("baselineCommit").GetString();
        commit.ShouldNotBeNull();
        commit!.Length.ShouldBe(40);
        commit.ShouldAllBe(c => Uri.IsHexDigit(c) && !char.IsUpper(c));

        root.GetProperty("branch").GetString().ShouldBe("main");
    }

    // --- AC1 / FR-20: no conformance suite silently added or dropped ---

    [Fact]
    public void BaselineEnumeratedSuiteClassesShouldMatchTheActualSuiteClassesInTheAssembly()
    {
        HashSet<string> actualSuiteClasses = DiscoverSuiteTestClassNames();
        actualSuiteClasses.Count.ShouldBe(14, "Exactly 14 *ConformanceSuiteTest classes must exist (FR-20: no suite silently added or dropped).");
        actualSuiteClasses.ShouldBe(ExpectedSuiteClasses.ToHashSet(), ignoreOrder: true);

        using JsonDocument doc = LoadCommittedJson("release-baseline-v1.json");
        IReadOnlyList<string> enumerated = doc.RootElement
            .GetProperty("conformanceOracle")
            .GetProperty("suiteClasses")
            .EnumerateArray()
            .Select(e => e.GetProperty("class").GetString()!)
            .ToList();

        enumerated.Count.ShouldBe(14);
        enumerated.ToHashSet().ShouldBe(actualSuiteClasses, ignoreOrder: true);

        foreach (string enumeratedClass in enumerated)
        {
            actualSuiteClasses.ShouldContain(enumeratedClass, $"Baseline enumerates suite class '{enumeratedClass}' that no longer exists in the assembly.");
        }
    }

    [Fact]
    public void BaselineSurvivabilityClassificationShouldAccountForAllFourteenSuites()
    {
        using JsonDocument doc = LoadCommittedJson("release-baseline-v1.json");
        JsonElement survivability = doc.RootElement.GetProperty("oracleSurvivability");

        IEnumerable<string> publicSurfaceOnly = survivability.GetProperty("publicSurfaceOnly")
            .EnumerateArray().Select(e => e.GetString()!);
        IEnumerable<string> internallyCoupled = survivability.GetProperty("internallyCoupled")
            .EnumerateArray().Select(e => e.GetProperty("suite").GetString()!);

        HashSet<string> classified = publicSurfaceOnly.Concat(internallyCoupled).ToHashSet();
        classified.ShouldBe(ExpectedSuiteClasses.ToHashSet(), ignoreOrder: true);
    }

    // --- AC2 / AC4: committed contract-shape snapshot ---

    [Fact]
    public void CommittedSnapshotShouldExistAndDeclareItsAssemblyAndTypeCount()
    {
        using JsonDocument doc = LoadCommittedJson(SnapshotArtifactFileName);
        JsonElement root = doc.RootElement;

        root.GetProperty("artifactKind").GetString().ShouldBe("public-contract-shape-baseline");
        root.GetProperty("assembly").GetString().ShouldBe("Hexalith.Conversations.Contracts");

        int declaredCount = root.GetProperty("typeCount").GetInt32();
        int actualTypesInPayload = root.GetProperty("types").GetArrayLength();
        declaredCount.ShouldBe(actualTypesInPayload, "Snapshot 'typeCount' header must match the number of captured types.");
        declaredCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CommittedSnapshotTypeCountShouldMatchTheLiveExportedContractSurface()
    {
        // Drift guard: the committed Story 5.1 baseline must reflect the live public Contracts surface. If a public
        // type was added/removed without regenerating the baseline, this fails — regenerate via
        // PublicContractShapeSnapshotGenerationTest before relying on the baseline for a behavior-preservation diff.
        int liveExportedTypeCount = typeof(ConformanceRunResultV1).Assembly.GetExportedTypes()
            .Count(t => (t.Namespace ?? string.Empty).StartsWith(ContractsNamespacePrefix, StringComparison.Ordinal));

        using JsonDocument doc = LoadCommittedJson(SnapshotArtifactFileName);
        int committedTypeCount = doc.RootElement.GetProperty("typeCount").GetInt32();

        committedTypeCount.ShouldBe(
            liveExportedTypeCount,
            "Committed contract-shape snapshot is stale relative to the live Hexalith.Conversations.Contracts surface. "
            + "Regenerate it with: dotnet test tests/Hexalith.Conversations.Conformance.Tests "
            + "--filter \"FullyQualifiedName~PublicContractShapeSnapshotGenerationTest\".");
    }

    [Fact]
    public void CommittedSnapshotCapturedSurfaceShouldPassContentSafetyScan()
    {
        // Scan the COMMITTED bytes of the captured `types` payload (not the human-authored header prose, which
        // legitimately contains the word "snapshot"). Proves the file on disk — not just an in-memory rebuild — is clean.
        using JsonDocument doc = LoadCommittedJson(SnapshotArtifactFileName);
        string typesJson = doc.RootElement.GetProperty("types").GetRawText();

        foreach (string fragment in ForbiddenFragments)
        {
            typesJson.ShouldNotContain(fragment, Case.Insensitive, $"Committed snapshot's captured surface must not contain forbidden fragment '{fragment}'.");
        }
    }

    // --- AC1/AC2 cross-artifact consistency ---

    [Fact]
    public void BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface()
    {
        int liveExportedTypeCount = typeof(ConformanceRunResultV1).Assembly.GetExportedTypes()
            .Count(t => (t.Namespace ?? string.Empty).StartsWith(ContractsNamespacePrefix, StringComparison.Ordinal));

        using JsonDocument snapshotDoc = LoadCommittedJson(SnapshotArtifactFileName);
        int committedSnapshotTypeCount = snapshotDoc.RootElement.GetProperty("typeCount").GetInt32();

        using JsonDocument baselineDoc = LoadCommittedJson("release-baseline-v1.json");
        JsonElement pointer = baselineDoc.RootElement.GetProperty("publicContractShapeSnapshot");

        pointer.GetProperty("artifact").GetString().ShouldBe(SnapshotArtifactFileName);
        pointer.GetProperty("assembly").GetString().ShouldBe("Hexalith.Conversations.Contracts");

        int reportedCount = pointer.GetProperty("exportedPublicTypeCount").GetInt32();
        reportedCount.ShouldBe(committedSnapshotTypeCount, "Baseline-reported type count disagrees with the committed snapshot.");
        reportedCount.ShouldBe(liveExportedTypeCount, "Baseline-reported type count disagrees with the live exported contract surface.");

        // The pointer must reference a file that actually exists alongside the baseline.
        File.Exists(Path.Combine(ReleaseEvidenceDirectory(), pointer.GetProperty("artifact").GetString()!)).ShouldBeTrue();
    }

    // --- Helpers ---

    private static HashSet<string> DiscoverSuiteTestClassNames()
        => typeof(ReleaseBaselineValidationTest).Assembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("ConformanceSuiteTest", StringComparison.Ordinal))
            .Select(t => t.Name)
            .ToHashSet();

    private static JsonDocument LoadCommittedJson(string fileName)
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), fileName);
        File.Exists(path).ShouldBeTrue($"Expected committed evidence file at '{path}'.");
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
