// <copyright file="MinimalModuleAuthoringCostBaselineValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Documentation;

/// <summary>
/// Validates the Story 4.2 SM-2 minimal-module authoring-cost baseline artifacts.
/// </summary>
public sealed class MinimalModuleAuthoringCostBaselineValidationTest
{
    private const string JsonFileName = "minimal-module-authoring-cost-sm2-baseline-v1.json";
    private const string MarkdownFileName = "minimal-module-authoring-cost-sm2-baseline-v1.md";

    private static readonly string[] IncludedCategories =
    [
        "Contracts",
        "Client",
        "domain/core",
        "Server",
        "AppHost",
        "ServiceDefaults",
        "Testing",
        "focused test projects",
    ];

    private static readonly string[] ExcludedCategories =
    [
        "Admin.Web",
        "FrontComposer trust components",
        "publication subscribers",
        "governance workflows",
        "generated output",
        "local developer artifacts",
        "shared platform libraries",
        "sibling submodule source",
    ];

    [Fact]
    public void Sm2BaselineArtifactsShouldExistAndReferenceStory41Sources()
    {
        string directory = ReleaseEvidenceDirectory();
        string jsonPath = Path.Combine(directory, JsonFileName);
        string markdownPath = Path.Combine(directory, MarkdownFileName);

        File.Exists(jsonPath).ShouldBeTrue("The machine-readable SM-2 baseline artifact must be committed.");
        File.Exists(markdownPath).ShouldBeTrue("The human-readable SM-2 baseline artifact must be committed.");

        using JsonDocument doc = LoadBaselineJson();
        JsonElement root = doc.RootElement;

        root.GetProperty("artifact").GetString().ShouldBe("minimal-module-authoring-cost-sm2-baseline");
        root.GetProperty("version").GetInt32().ShouldBe(1);
        root.GetProperty("status").GetString().ShouldBe("accepted");
        root.GetProperty("story5Reference").GetString().ShouldBe("Story 5.3");
        root.GetProperty("oq2Status").GetString().ShouldBe("unconfirmed");

        string rawJson = File.ReadAllText(jsonPath);
        string markdown = File.ReadAllText(markdownPath);

        rawJson.ShouldContain("docs/domain-module-authoring-template.md", Case.Sensitive);
        rawJson.ShouldContain("docs/release-evidence/thin-authoring-template-validation-v1.md", Case.Sensitive);
        markdown.ShouldContain("docs/domain-module-authoring-template.md", Case.Sensitive);
        markdown.ShouldContain("docs/release-evidence/thin-authoring-template-validation-v1.md", Case.Sensitive);
    }

    [Fact]
    public void Sm2BaselineShouldPinAcceptedIncludedAndExcludedCategorySets()
    {
        using JsonDocument doc = LoadBaselineJson();
        JsonElement root = doc.RootElement;

        ReadStringArray(root.GetProperty("includedCategories")).ShouldBe(IncludedCategories, ignoreOrder: false);
        ReadStringArray(root.GetProperty("excludedCategories")).ShouldBe(ExcludedCategories, ignoreOrder: false);

        JsonElement manifest = root.GetProperty("templateMinimal").GetProperty("manifest");

        foreach (JsonElement row in manifest.EnumerateArray())
        {
            IncludedCategories.ShouldContain(
                row.GetProperty("category").GetString()!,
                $"Manifest path '{row.GetProperty("logicalPath").GetString()}' must stay inside the accepted Story 4.1 SM-2 boundary.");
        }
    }

    [Fact]
    public void Sm2BaselineManifestShouldReconcileToRecordedTotals()
    {
        using JsonDocument doc = LoadBaselineJson();
        JsonElement templateMinimal = doc.RootElement.GetProperty("templateMinimal");
        JsonElement[] manifest = templateMinimal.GetProperty("manifest").EnumerateArray().ToArray();
        JsonElement[] categoryTotals = templateMinimal.GetProperty("categoryTotals").EnumerateArray().ToArray();

        manifest.ShouldNotBeEmpty("The SM-2 baseline must expose exact manifest rows, not only prose totals.");

        int manifestFileCount = manifest.Length;
        int manifestLoc = manifest.Sum(row => row.GetProperty("loc").GetInt32());

        templateMinimal.GetProperty("fileCount").GetInt32().ShouldBe(manifestFileCount);
        templateMinimal.GetProperty("loc").GetInt32().ShouldBe(manifestLoc);

        foreach (string category in IncludedCategories)
        {
            JsonElement categoryTotal = categoryTotals.Single(t => t.GetProperty("category").GetString() == category);
            JsonElement[] categoryRows = manifest.Where(row => row.GetProperty("category").GetString() == category).ToArray();

            categoryTotal.GetProperty("fileCount").GetInt32().ShouldBe(categoryRows.Length, $"File count mismatch for category '{category}'.");
            categoryTotal.GetProperty("loc").GetInt32().ShouldBe(categoryRows.Sum(row => row.GetProperty("loc").GetInt32()), $"LOC mismatch for category '{category}'.");
        }
    }

    [Fact]
    public void Sm2BaselineShouldExposeStableStory53Fields()
    {
        using JsonDocument doc = LoadBaselineJson();
        JsonElement root = doc.RootElement;

        root.TryGetProperty("templateMinimal", out _).ShouldBeTrue();
        root.TryGetProperty("preInitiativeEquivalent", out _).ShouldBeTrue();
        root.TryGetProperty("comparison", out _).ShouldBeTrue();
        root.TryGetProperty("oq2Status", out _).ShouldBeTrue();
        root.TryGetProperty("sourceArtifactReferences", out _).ShouldBeTrue();
        string targetAssumption = root.GetProperty("comparison").GetProperty("targetAssumption").GetString()
            ?? throw new InvalidOperationException("comparison.targetAssumption must be a string.");
        targetAssumption.ShouldContain(">=50% fewer files", Case.Sensitive);

        JsonElement templateMinimal = root.GetProperty("templateMinimal");
        templateMinimal.GetProperty("fileCount").GetInt32().ShouldBeGreaterThan(0);
        templateMinimal.GetProperty("loc").GetInt32().ShouldBeGreaterThan(0);

        JsonElement preInitiative = root.GetProperty("preInitiativeEquivalent");
        preInitiative.GetProperty("measurementType").GetString().ShouldBe("estimated");
        preInitiative.GetProperty("confidence").GetString().ShouldBe("low");
        preInitiative.GetProperty("fileCount").GetInt32().ShouldBeGreaterThan(templateMinimal.GetProperty("fileCount").GetInt32());
        preInitiative.GetProperty("loc").GetInt32().ShouldBeGreaterThan(templateMinimal.GetProperty("loc").GetInt32());
    }

    [Fact]
    public void Sm2BaselineComparisonShouldBeDerivedFromRecordedValues()
    {
        using JsonDocument doc = LoadBaselineJson();
        JsonElement root = doc.RootElement;
        JsonElement templateMinimal = root.GetProperty("templateMinimal");
        JsonElement preInitiative = root.GetProperty("preInitiativeEquivalent");
        JsonElement comparison = root.GetProperty("comparison");

        int templateFiles = templateMinimal.GetProperty("fileCount").GetInt32();
        int templateLoc = templateMinimal.GetProperty("loc").GetInt32();
        int preInitiativeFiles = preInitiative.GetProperty("fileCount").GetInt32();
        int preInitiativeLoc = preInitiative.GetProperty("loc").GetInt32();

        comparison.GetProperty("templateMinimalFileCount").GetInt32().ShouldBe(templateFiles);
        comparison.GetProperty("templateMinimalLoc").GetInt32().ShouldBe(templateLoc);
        comparison.GetProperty("preInitiativeFileCount").GetInt32().ShouldBe(preInitiativeFiles);
        comparison.GetProperty("preInitiativeLoc").GetInt32().ShouldBe(preInitiativeLoc);

        decimal expectedFileReduction = CalculateReductionPercentage(preInitiativeFiles, templateFiles);
        decimal expectedLocReduction = CalculateReductionPercentage(preInitiativeLoc, templateLoc);

        comparison.GetProperty("fileReductionPercentage").GetDecimal().ShouldBe(expectedFileReduction);
        comparison.GetProperty("locReductionPercentage").GetDecimal().ShouldBe(expectedLocReduction);
        comparison.GetProperty("targetStatus").GetString().ShouldBe("unconfirmed-estimate-only");
    }

    [Fact]
    public void Sm2BaselineMarkdownShouldStayAlignedWithMachineReadableTotals()
    {
        using JsonDocument doc = LoadBaselineJson();
        JsonElement root = doc.RootElement;
        JsonElement templateMinimal = root.GetProperty("templateMinimal");
        JsonElement preInitiative = root.GetProperty("preInitiativeEquivalent");
        JsonElement comparison = root.GetProperty("comparison");
        string markdown = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), MarkdownFileName));

        markdown.ShouldContain($"**{templateMinimal.GetProperty("fileCount").GetInt32()}**", Case.Sensitive);
        markdown.ShouldContain($"**{templateMinimal.GetProperty("loc").GetInt32()}**", Case.Sensitive);
        markdown.ShouldContain(preInitiative.GetProperty("fileCount").GetInt32().ToString(CultureInfo.InvariantCulture), Case.Sensitive);
        markdown.ShouldContain(preInitiative.GetProperty("loc").GetInt32().ToString("N0", CultureInfo.InvariantCulture), Case.Sensitive);
        markdown.ShouldContain(comparison.GetProperty("fileReductionPercentage").GetDecimal().ToString("0.00", CultureInfo.InvariantCulture) + "%", Case.Sensitive);
        markdown.ShouldContain(comparison.GetProperty("locReductionPercentage").GetDecimal().ToString("0.00", CultureInfo.InvariantCulture) + "%", Case.Sensitive);
        markdown.ShouldContain($"OQ-2 remains `{root.GetProperty("oq2Status").GetString()}`", Case.Sensitive);
    }

    [Fact]
    public void Sm2BaselineShouldNotUseBuildOutputGeneratedOutputOrLocalPathsAsEvidence()
    {
        using JsonDocument doc = LoadBaselineJson();
        JsonElement root = doc.RootElement;
        string rawJson = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), JsonFileName));
        string markdown = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), MarkdownFileName));

        foreach (string raw in new[] { rawJson, markdown })
        {
            raw.ShouldNotContain("/home/", Case.Insensitive);
            raw.ShouldNotContain("/tmp/", Case.Insensitive);
            Regex.IsMatch(raw, @"[A-Za-z]:\\").ShouldBeFalse("Release evidence must not cite absolute local machine paths.");
        }

        foreach (JsonElement row in root.GetProperty("templateMinimal").GetProperty("manifest").EnumerateArray())
        {
            string path = row.GetProperty("logicalPath").GetString()!;
            path.ShouldNotContain("bin/", Case.Insensitive);
            path.ShouldNotContain("obj/", Case.Insensitive);
            path.ShouldNotContain("\\bin\\", Case.Insensitive);
            path.ShouldNotContain("\\obj\\", Case.Insensitive);
            path.ShouldNotContain("generated", Case.Insensitive);
            path.ShouldNotContain("Admin.Web", Case.Insensitive);
            path.ShouldNotContain("FrontComposer", Case.Insensitive);
        }
    }

    private static JsonDocument LoadBaselineJson()
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), JsonFileName)));

    private static string[] ReadStringArray(JsonElement element)
        => element.EnumerateArray().Select(item => item.GetString()!).ToArray();

    private static decimal CalculateReductionPercentage(int original, int reduced)
        => Math.Round(((decimal)original - reduced) / original * 100m, 2, MidpointRounding.AwayFromZero);

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
