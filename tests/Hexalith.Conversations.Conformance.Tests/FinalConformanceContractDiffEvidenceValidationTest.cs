// <copyright file="FinalConformanceContractDiffEvidenceValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Linq;
using System.Text.Json;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 5.1 (AC4/AC5) — validates the final conformance and public contract-shape diff evidence consumed by Story 5.3.
/// </summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class FinalConformanceContractDiffEvidenceValidationTest
{
    private const string JsonArtifactFileName = "final-conformance-contract-diff-v1.json";
    private const string MarkdownArtifactFileName = "final-conformance-contract-diff-v1.md";

    private static readonly string[] RequiredBaselineReferences =
    [
        "docs/release-evidence/release-baseline-v1.json",
        "docs/release-evidence/release-baseline-v1.md",
        "docs/release-evidence/public-contract-shape-baseline-v1.json",
    ];

    [Fact]
    public void JsonAndMarkdownArtifactsShouldExistAndBeInternallyConsistent()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        string markdown = LoadMarkdownArtifact();

        root.GetProperty("artifact").GetString().ShouldBe("final-conformance-contract-diff");
        root.GetProperty("version").GetString().ShouldBe("1");
        root.GetProperty("status").GetString().ShouldBe("pass");
        root.GetProperty("story").GetString().ShouldBe("5.1");
        root.GetProperty("story5Reference").GetString().ShouldBe("Story 5.3");

        markdown.ShouldContain($"**Status:** {root.GetProperty("status").GetString()}");
        markdown.ShouldContain($"**Generated:** {root.GetProperty("generatedAtUtc").GetString()}");
        markdown.ShouldContain(JsonArtifactFileName);
        markdown.ShouldContain("365 total, 365 passed, 0 errors, 0 failed, 0 skipped, 0 not run");
        markdown.ShouldContain("Diff status: empty");
    }

    [Fact]
    public void JsonShouldReferenceStoryOneOneBaselinesAndExactFinalConformanceCounts()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        JsonElement conformance = root.GetProperty("conformanceRun");

        conformance.GetProperty("total").GetInt32().ShouldBe(365);
        conformance.GetProperty("passed").GetInt32().ShouldBe(365);
        conformance.GetProperty("errors").GetInt32().ShouldBe(0);
        conformance.GetProperty("failed").GetInt32().ShouldBe(0);
        conformance.GetProperty("skipped").GetInt32().ShouldBe(0);
        conformance.GetProperty("suiteClassCount").GetInt32().ShouldBe(14);

        string[] suiteClassNames = conformance.GetProperty("suiteClassNames")
            .EnumerateArray()
            .Select(name => name.GetString() ?? string.Empty)
            .ToArray();
        suiteClassNames.Length.ShouldBe(14);
        suiteClassNames.Distinct(StringComparer.Ordinal).Count().ShouldBe(14, "Suite class names must be unique; a duplicate signals a silently renamed or dropped suite.");
        suiteClassNames.ShouldAllBe(name => name.EndsWith("ConformanceSuiteTest", StringComparison.Ordinal));

        conformance.GetProperty("fallbackCommand").GetString()
            .ShouldBe("tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests");

        JsonElement baselineReferences = root.GetProperty("baselineReferences");
        foreach (string expectedPath in RequiredBaselineReferences)
        {
            baselineReferences.GetRawText().ShouldContain(expectedPath);
            File.Exists(Path.Combine(FindRepositoryRoot(), expectedPath)).ShouldBeTrue($"Expected baseline reference '{expectedPath}' to exist.");
        }

        JsonElement facts = baselineReferences.GetProperty("baselineFacts");
        facts.GetProperty("suiteClassCount").GetInt32().ShouldBe(14);
        facts.GetProperty("baselineSuiteTests").GetInt32().ShouldBe(214);
        facts.GetProperty("baselineProjectTests").GetInt32().ShouldBe(248);
        facts.GetProperty("exportedPublicContractTypes").GetInt32().ShouldBe(196);
    }

    [Fact]
    public void ContractShapeDiffShouldBeEmptyOrCarryApprovalReferences()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement diff = doc.RootElement.GetProperty("contractShapeDiff");

        diff.GetProperty("baselineArtifact").GetString().ShouldBe("docs/release-evidence/public-contract-shape-baseline-v1.json");
        diff.GetProperty("baselineTypeCount").GetInt32().ShouldBe(196);
        diff.GetProperty("finalTypeCount").GetInt32().ShouldBe(196);
        diff.GetProperty("comparison").GetString().ShouldBe("byte-for-byte JSON comparison");

        string? status = diff.GetProperty("diffStatus").GetString();
        if (string.Equals(status, "empty", StringComparison.Ordinal))
        {
            diff.GetProperty("changedEntries").GetArrayLength().ShouldBe(0);
            diff.GetProperty("approvalReferences").GetArrayLength().ShouldBe(0);
        }
        else
        {
            diff.GetProperty("changedEntries").GetArrayLength().ShouldBeGreaterThan(0);
            diff.GetProperty("approvalReferences").GetArrayLength().ShouldBeGreaterThan(0);
        }
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
            value.ShouldNotContain("obj/", Case.Insensitive, $"Evidence value at {path} must not cite build output as source-of-truth evidence.");
            value.ShouldNotContain("/generated/", Case.Insensitive, $"Evidence value at {path} must not cite generated output as source-of-truth evidence.");

            if (!path.EndsWith(".fallbackCommand", StringComparison.Ordinal))
            {
                value.ShouldNotContain("bin/", Case.Insensitive, $"Only the fallback executable command may cite a bin/ path as execution evidence.");
            }
        }

        string markdown = LoadMarkdownArtifact();
        markdown.ShouldNotContain("/home/", Case.Insensitive);
        markdown.ShouldNotContain("/tmp/", Case.Insensitive);
        markdown.ShouldNotContain("obj/", Case.Insensitive);
    }

    private static JsonDocument LoadStoryArtifact()
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), JsonArtifactFileName);
        File.Exists(path).ShouldBeTrue($"Expected Story 5.1 JSON artifact at '{path}'.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string LoadMarkdownArtifact()
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), MarkdownArtifactFileName);
        File.Exists(path).ShouldBeTrue($"Expected Story 5.1 Markdown artifact at '{path}'.");
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
