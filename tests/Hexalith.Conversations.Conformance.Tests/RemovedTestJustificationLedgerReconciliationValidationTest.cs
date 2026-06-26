// <copyright file="RemovedTestJustificationLedgerReconciliationValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Linq;
using System.Text.Json;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 5.2 (AC7) — validates the removed-test justification ledger reconciliation consumed by Story 5.3.
/// </summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class RemovedTestJustificationLedgerReconciliationValidationTest
{
    private const string JsonArtifactFileName = "removed-test-justification-ledger-reconciliation-v1.json";
    private const string MarkdownArtifactFileName = "removed-test-justification-ledger-reconciliation-v1.md";
    private const string AtRiskRegisterFileName = "at-risk-test-register-v1.json";
    private const string InventoryFileName = "consume-promote-keep-inventory-v1.json";
    private const string FinalConformanceFileName = "final-conformance-contract-diff-v1.json";
    private const string ConformanceProjectPath = "tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj";
    private const string ServerUsingPrefix = "using Hexalith.Conversations." + "Server.";

    private static readonly string[] RequiredRootProperties =
    [
        "artifact",
        "version",
        "status",
        "story",
        "generatedAtUtc",
        "sourceArtifacts",
        "removedTests",
        "reexpressedNeverDeleteTests",
        "conformanceSuiteContinuity",
        "projectReferenceDisposition",
        "inventoryChangeLogReconciliation",
        "contractShapeImpact",
        "validation",
        "environmentLimitations",
        "story5Reference",
    ];

    private static readonly string[] StructuralDispositionSections =
    [
        "story21StructuralDispositions",
        "story22StructuralDispositions",
        "story23StructuralDispositions",
        "story24StructuralDispositions",
        "story25StructuralDispositions",
        "story26StructuralDispositions",
        "story27StructuralDispositions",
        "story33StructuralDispositions",
    ];

    [Fact]
    public void JsonAndMarkdownArtifactsShouldExistAndExposeStoryFiveThreeFields()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        string markdown = LoadMarkdownArtifact();

        foreach (string propertyName in RequiredRootProperties)
        {
            root.TryGetProperty(propertyName, out _).ShouldBeTrue($"Story 5.2 artifact must expose '{propertyName}' for Story 5.3.");
        }

        root.GetProperty("artifact").GetString().ShouldBe("removed-test-justification-ledger-reconciliation");
        root.GetProperty("version").GetString().ShouldBe("1");
        root.GetProperty("status").GetString().ShouldBe("pass-with-residual-coupling");
        root.GetProperty("story").GetString().ShouldBe("5.2");
        root.GetProperty("story5Reference").GetString().ShouldBe("Story 5.3");

        markdown.ShouldContain($"**Status:** {root.GetProperty("status").GetString()}");
        markdown.ShouldContain($"**Generated:** {root.GetProperty("generatedAtUtc").GetString()}");
        markdown.ShouldContain(JsonArtifactFileName);
        markdown.ShouldContain("Residual Server reference retained");
    }

    [Fact]
    public void SourceArtifactsShouldReferenceDurableRepositoryFiles()
    {
        using JsonDocument doc = LoadStoryArtifact();
        string root = FindRepositoryRoot();
        JsonElement sourceArtifacts = doc.RootElement.GetProperty("sourceArtifacts");

        string[] requiredSourceArtifactProperties =
        [
            "atRiskTestRegisterJson",
            "atRiskTestRegisterMarkdown",
            "consumePromoteKeepInventoryJson",
            "consumePromoteKeepInventoryMarkdown",
            "classificationChangeProcedure",
            "releaseBaselineMarkdown",
            "story51FinalEvidenceJson",
            "story51FinalEvidenceMarkdown",
        ];

        foreach (string propertyName in requiredSourceArtifactProperties)
        {
            string artifactPath = sourceArtifacts.GetProperty(propertyName).GetString() ?? string.Empty;
            artifactPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(artifactPath).ShouldBeFalse($"Source artifact '{propertyName}' must be repository-relative.");
            File.Exists(Path.Combine(root, artifactPath)).ShouldBeTrue($"Source artifact '{propertyName}' must exist at '{artifactPath}'.");
        }
    }

    [Fact]
    public void EveryAtRiskRegisterEntryShouldBeAccountedFor()
    {
        using JsonDocument storyDoc = LoadStoryArtifact();
        using JsonDocument registerDoc = LoadEvidenceArtifact(AtRiskRegisterFileName);

        Dictionary<string, string> registerClassifications = registerDoc.RootElement.GetProperty("tests")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("file").GetString() ?? string.Empty,
                entry => entry.GetProperty("classification").GetString() ?? string.Empty,
                StringComparer.Ordinal);

        JsonElement removedTests = storyDoc.RootElement.GetProperty("removedTests");
        string[] reconciledEntries = removedTests
            .EnumerateArray()
            .Select(entry => entry.GetProperty("source").GetProperty("sourceFileOrAssertionGroup").GetString() ?? string.Empty)
            .ToArray();

        reconciledEntries.Length.ShouldBeGreaterThanOrEqualTo(registerClassifications.Count);
        foreach ((string registerEntry, string classification) in registerClassifications)
        {
            reconciledEntries.ShouldContain(registerEntry, $"At-risk register entry '{registerEntry}' must be represented in Story 5.2.");

            JsonElement reconciledEntry = removedTests.EnumerateArray()
                .Single(entry => string.Equals(
                    entry.GetProperty("source").GetProperty("sourceFileOrAssertionGroup").GetString(),
                    registerEntry,
                    StringComparison.Ordinal));

            reconciledEntry.GetProperty("classification").GetString().ShouldBe(classification);
        }

        foreach (JsonElement entry in removedTests.EnumerateArray())
        {
            entry.GetProperty("classification").GetString().ShouldNotBeNullOrWhiteSpace();
            entry.GetProperty("actualDisposition").GetString().ShouldNotBeNullOrWhiteSpace();
            entry.GetProperty("rationale").GetString().ShouldNotBeNullOrWhiteSpace();
            entry.GetProperty("replacementOrOffsetEvidence").GetString().ShouldNotBeNullOrWhiteSpace();
            entry.GetProperty("greenAfterChange").GetBoolean().ShouldBeTrue();
        }
    }

    [Fact]
    public void EveryStructuralDispositionSectionShouldBeAccountedFor()
    {
        using JsonDocument storyDoc = LoadStoryArtifact();
        using JsonDocument registerDoc = LoadEvidenceArtifact(AtRiskRegisterFileName);

        JsonElement sourceSections = storyDoc.RootElement.GetProperty("sourceArtifacts").GetProperty("structuralDispositionSections");
        JsonElement reconciledRows = storyDoc.RootElement.GetProperty("sourceArtifacts").GetProperty("structuralDispositions");

        foreach (string sectionName in StructuralDispositionSections)
        {
            int expectedCount = registerDoc.RootElement.GetProperty(sectionName).GetArrayLength();
            JsonElement sourceSection = sourceSections.EnumerateArray()
                .Single(section => string.Equals(section.GetProperty("name").GetString(), sectionName, StringComparison.Ordinal));

            sourceSection.GetProperty("count").GetInt32().ShouldBe(expectedCount);

            int reconciledCount = reconciledRows.EnumerateArray()
                .Count(row => string.Equals(row.GetProperty("section").GetString(), sectionName, StringComparison.Ordinal));

            reconciledCount.ShouldBe(expectedCount, $"All rows from '{sectionName}' must be represented.");
        }
    }

    [Fact]
    public void ActualRemovalsShouldBeDeadPlumbingAndNeverDeleteRowsShouldStillExist()
    {
        using JsonDocument doc = LoadStoryArtifact();
        string root = FindRepositoryRoot();
        JsonElement artifact = doc.RootElement;

        JsonElement removedTests = artifact.GetProperty("removedTests");
        JsonElement actualRemovals = artifact.GetProperty("validation").GetProperty("actualRemovalCount");
        int actualRemovalCount = removedTests.EnumerateArray().Count(entry => entry.GetProperty("actuallyRemoved").GetBoolean());
        actualRemovals.GetInt32().ShouldBe(actualRemovalCount);
        actualRemovalCount.ShouldBeGreaterThan(0);

        foreach (JsonElement entry in removedTests.EnumerateArray().Where(entry => entry.GetProperty("actuallyRemoved").GetBoolean()))
        {
            string classification = entry.GetProperty("classification").GetString() ?? string.Empty;
            string disposition = entry.GetProperty("actualDisposition").GetString() ?? string.Empty;

            bool justified = classification.Contains("plumbing-only", StringComparison.Ordinal)
                || disposition.Contains("dead-plumbing", StringComparison.Ordinal)
                || disposition.Contains("plumbing-only", StringComparison.Ordinal);

            justified.ShouldBeTrue($"Actual removal '{entry.GetProperty("id").GetString()}' must be justified as dead/plumbing-only.");
            entry.GetProperty("owningStory").GetString().ShouldNotBeNullOrWhiteSpace();
        }

        JsonElement neverDeleteRows = artifact.GetProperty("reexpressedNeverDeleteTests");
        neverDeleteRows.GetArrayLength().ShouldBeGreaterThan(0);

        foreach (JsonElement row in neverDeleteRows.EnumerateArray())
        {
            row.GetProperty("presentInCurrentTree").GetBoolean().ShouldBeTrue();
            row.GetProperty("includedInConformanceProject").GetBoolean().ShouldBeTrue();

            string reExpression = row.GetProperty("reExpression").GetString() ?? string.Empty;
            File.Exists(Path.Combine(root, reExpression)).ShouldBeTrue($"Re-expression '{reExpression}' must exist.");
        }

        File.Exists(Path.Combine(root, "tests/Hexalith.Conversations.Conformance.Tests/GovernanceAuditPairingSafetyNetConformanceTest.cs"))
            .ShouldBeTrue("GovernanceAuditPairingSafetyNetConformanceTest must remain present.");
    }

    [Fact]
    public void ConformanceSuiteContinuityShouldMatchCurrentTreeAndStoryFiveOneEvidence()
    {
        using JsonDocument storyDoc = LoadStoryArtifact();
        using JsonDocument finalDoc = LoadEvidenceArtifact(FinalConformanceFileName);

        string root = FindRepositoryRoot();
        string[] currentSuiteClasses = Directory
            .EnumerateFiles(Path.Combine(root, "tests", "Hexalith.Conversations.Conformance.Tests"), "*ConformanceSuiteTest.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        JsonElement continuity = storyDoc.RootElement.GetProperty("conformanceSuiteContinuity");
        string[] artifactSuiteClasses = continuity.GetProperty("currentVerification").GetProperty("suiteClassNames")
            .EnumerateArray()
            .Select(name => name.GetString() ?? string.Empty)
            .Order(StringComparer.Ordinal)
            .ToArray();

        artifactSuiteClasses.ShouldBe(currentSuiteClasses);
        artifactSuiteClasses.Length.ShouldBe(14);
        artifactSuiteClasses.Distinct(StringComparer.Ordinal).Count().ShouldBe(14);
        artifactSuiteClasses.ShouldAllBe(name => name.EndsWith("ConformanceSuiteTest", StringComparison.Ordinal));

        JsonElement finalConformance = finalDoc.RootElement.GetProperty("conformanceRun");
        continuity.GetProperty("story51FinalFacts").GetProperty("total").GetInt32().ShouldBe(finalConformance.GetProperty("total").GetInt32());
        continuity.GetProperty("story51FinalFacts").GetProperty("passed").GetInt32().ShouldBe(finalConformance.GetProperty("passed").GetInt32());
        continuity.GetProperty("baselineFacts").GetProperty("baselineSuiteTests").GetInt32().ShouldBe(214);
        continuity.GetProperty("baselineFacts").GetProperty("baselineProjectTests").GetInt32().ShouldBe(248);
        continuity.GetProperty("baselineFacts").GetProperty("exportedPublicContractTypes").GetInt32().ShouldBe(196);
    }

    [Fact]
    public void ProjectReferenceDispositionShouldMatchCurrentProjectAndServerUsingInventory()
    {
        using JsonDocument doc = LoadStoryArtifact();
        string root = FindRepositoryRoot();
        JsonElement disposition = doc.RootElement.GetProperty("projectReferenceDisposition");

        string projectFile = File.ReadAllText(Path.Combine(root, ConformanceProjectPath));
        projectFile.ShouldContain("src\\Hexalith.Conversations.Server\\Hexalith.Conversations.Server.csproj");
        disposition.GetProperty("decision").GetString().ShouldBe("retain-residual-coupling");
        disposition.GetProperty("currentReferenceState").GetString().ShouldBe("retained");

        string[] liveServerUsingFiles = Directory
            .EnumerateFiles(Path.Combine(root, "tests", "Hexalith.Conversations.Conformance.Tests"), "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => File.ReadLines(path).Any(line => line.TrimStart().StartsWith(ServerUsingPrefix, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Dictionary<string, string[]> liveServerUsingNamespaces = Directory
            .EnumerateFiles(Path.Combine(root, "tests", "Hexalith.Conversations.Conformance.Tests"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => new
            {
                File = Path.GetRelativePath(root, path).Replace('\\', '/'),
                Namespaces = File.ReadLines(path)
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith(ServerUsingPrefix, StringComparison.Ordinal))
                    .Select(line => line["using ".Length..].TrimEnd(';'))
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
            })
            .Where(item => item.Namespaces.Length > 0)
            .ToDictionary(item => item.File, item => item.Namespaces, StringComparer.Ordinal);

        JsonElement residualCouplings = disposition.GetProperty("residualCouplingInventory");
        string[] artifactFiles = residualCouplings
            .EnumerateArray()
            .Select(row => row.GetProperty("file").GetString() ?? string.Empty)
            .Order(StringComparer.Ordinal)
            .ToArray();

        artifactFiles.ShouldBe(liveServerUsingFiles);
        foreach (JsonElement row in residualCouplings.EnumerateArray())
        {
            string file = row.GetProperty("file").GetString() ?? string.Empty;
            string[] artifactNamespaces = row.GetProperty("namespaces")
                .EnumerateArray()
                .Select(ns => ns.GetString() ?? string.Empty)
                .Order(StringComparer.Ordinal)
                .ToArray();

            artifactNamespaces.ShouldBe(liveServerUsingNamespaces[file], $"Residual coupling namespaces for '{file}' must match the current source imports.");
            row.GetProperty("rationale").GetString().ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void InventoryChangeLogAndContractShapeImpactShouldBePreserved()
    {
        using JsonDocument storyDoc = LoadStoryArtifact();
        using JsonDocument inventoryDoc = LoadEvidenceArtifact(InventoryFileName);

        JsonElement reconciliation = storyDoc.RootElement.GetProperty("inventoryChangeLogReconciliation");
        string[] sourceIds = inventoryDoc.RootElement.GetProperty("changeLog")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("entryId").GetString() ?? string.Empty)
            .ToArray();

        string[] artifactIds = reconciliation.GetProperty("sourceChangeLogEntryIds")
            .EnumerateArray()
            .Select(entry => entry.GetString() ?? string.Empty)
            .ToArray();

        artifactIds.ShouldBe(sourceIds);
        reconciliation.GetProperty("newInventoryChangeRequired").GetBoolean().ShouldBeFalse();

        JsonElement contractShapeImpact = storyDoc.RootElement.GetProperty("contractShapeImpact");
        contractShapeImpact.GetProperty("expected").GetString().ShouldBe("none");
        contractShapeImpact.GetProperty("publicContractShapeBaselineDiff").GetString().ShouldBe("empty");
    }

    [Fact]
    public void EvidenceShouldNotUseBuildArtifactsGeneratedOutputOrLocalPathsAsSourceOfTruth()
    {
        using JsonDocument doc = LoadStoryArtifact();
        List<(string Path, string Value)> strings = [];
        CollectStringValues(doc.RootElement, "$", strings);

        foreach ((string path, string value) in strings)
        {
            value.ShouldNotContain("/home/", Case.Insensitive, $"Evidence value at {path} must not cite a local absolute path.");
            value.ShouldNotContain("/tmp/", Case.Insensitive, $"Evidence value at {path} must not cite a local temporary path.");
            value.ShouldNotContain("C:\\", Case.Insensitive, $"Evidence value at {path} must not cite a local drive path.");
            value.ShouldNotContain("D:\\", Case.Insensitive, $"Evidence value at {path} must not cite a local drive path.");
            value.ShouldNotContain("bin/", Case.Insensitive, $"Evidence value at {path} must not cite build output as source-of-truth evidence.");
            value.ShouldNotContain("obj/", Case.Insensitive, $"Evidence value at {path} must not cite build output as source-of-truth evidence.");
            value.ShouldNotContain("/generated/", Case.Insensitive, $"Evidence value at {path} must not cite generated output as source-of-truth evidence.");
            value.ShouldNotContain("working directory", Case.Insensitive, $"Evidence value at {path} must not cite a mutable working directory as source-of-truth evidence.");
        }

        string markdown = LoadMarkdownArtifact();
        markdown.ShouldNotContain("/home/", Case.Insensitive);
        markdown.ShouldNotContain("/tmp/", Case.Insensitive);
        markdown.ShouldNotContain("bin/", Case.Insensitive);
        markdown.ShouldNotContain("obj/", Case.Insensitive);
    }

    private static JsonDocument LoadStoryArtifact()
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), JsonArtifactFileName);
        File.Exists(path).ShouldBeTrue($"Expected Story 5.2 JSON artifact at '{path}'.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static JsonDocument LoadEvidenceArtifact(string fileName)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), fileName)));

    private static string LoadMarkdownArtifact()
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), MarkdownArtifactFileName);
        File.Exists(path).ShouldBeTrue($"Expected Story 5.2 Markdown artifact at '{path}'.");
        return File.ReadAllText(path);
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

    private static void CollectStringValues(JsonElement element, string path, List<(string Path, string Value)> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    CollectStringValues(property.Value, $"{path}.{property.Name}", values);
                }

                break;
            case JsonValueKind.Array:
                int index = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    CollectStringValues(item, $"{path}[{index}]", values);
                    index++;
                }

                break;
            case JsonValueKind.String:
                values.Add((path, element.GetString() ?? string.Empty));
                break;
        }
    }
}
