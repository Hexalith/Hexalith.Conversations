using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>Validates the committed API fixtures against the current exported Contracts surface.</summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class ReleaseBaselineValidationTest
{
    private const string ContractsNamespacePrefix = "Hexalith.Conversations.Contracts";
    private const string SnapshotArtifactFileName = "public-contract-shape-baseline-v1.json";
    private const string CurrentSnapshotArtifactFileName = "public-contract-shape-bounded-read-v4.json";

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
        // The additive current snapshot must reflect the live Contracts surface; the old v1 snapshot is retained.
        int liveExportedTypeCount = typeof(ConformanceRunResultV1).Assembly.GetExportedTypes()
            .Count(t => (t.Namespace ?? string.Empty).StartsWith(ContractsNamespacePrefix, StringComparison.Ordinal));

        using JsonDocument doc = LoadCommittedJson(CurrentSnapshotArtifactFileName);
        int committedTypeCount = doc.RootElement.GetProperty("typeCount").GetInt32();

        committedTypeCount.ShouldBe(
            liveExportedTypeCount,
            "The additive current contract-shape evidence differs from the live public surface.");
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

    [Fact]
    public void CurrentSnapshotTypeCountMatchesItsPayloadAndLiveSurface()
    {
        int liveExportedTypeCount = typeof(ConformanceRunResultV1).Assembly.GetExportedTypes()
            .Count(t => (t.Namespace ?? string.Empty).StartsWith(ContractsNamespacePrefix, StringComparison.Ordinal));
        using JsonDocument currentSnapshotDoc = LoadCommittedJson(CurrentSnapshotArtifactFileName);
        currentSnapshotDoc.RootElement.GetProperty("typeCount").GetInt32()
            .ShouldBe(liveExportedTypeCount, "Current snapshot type count differs from the live exported contract surface.");
        currentSnapshotDoc.RootElement.GetProperty("typeCount").GetInt32()
            .ShouldBe(currentSnapshotDoc.RootElement.GetProperty("types").GetArrayLength());
    }

    private static JsonDocument LoadCommittedJson(string fileName)
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), fileName);
        File.Exists(path).ShouldBeTrue($"Expected committed evidence file at '{path}'.");
        return JsonDocument.Parse(RepositoryLocator.ReadAllText(path));
    }

    private static string ReleaseEvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "docs", "release-evidence");

    private static string FindRepositoryRoot()
        => RepositoryLocator.FindRepositoryRoot();
}
