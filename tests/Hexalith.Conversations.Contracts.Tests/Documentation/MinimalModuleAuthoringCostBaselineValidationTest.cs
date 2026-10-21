// <copyright file="MinimalModuleAuthoringCostBaselineValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.Contracts.Tests.Documentation;

/// <summary>
/// Validates the Story 4.2 SM-2 minimal-module authoring-cost baseline artifacts.
/// </summary>
/// <param name="output">The measured receipt output.</param>
public sealed class MinimalModuleAuthoringCostBaselineValidationTest(ITestOutputHelper output)
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

        string rawJson = RepositoryLocator.ReadAllText(jsonPath);
        string markdown = RepositoryLocator.ReadAllText(markdownPath);

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
        string markdown = RepositoryLocator.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), MarkdownFileName));

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
        string rawJson = RepositoryLocator.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), JsonFileName));
        string markdown = RepositoryLocator.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), MarkdownFileName));

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

    /// <summary>Independently counts every authored fixture source, including both AppHost files.</summary>
    [Fact]
    public async Task SmTwoV2ShouldCountEveryHandAuthoredFixtureFileIncludingAppHost()
    {
        string root = FindRepositoryRoot();
        const string fixture = "tests/fixtures/Hexalith.Conversations.MinimalModule/";
        string[] expected =
        [
            "Directory.Build.props",
            "Hexalith.Conversations.MinimalModule.slnx",
            "README.md",
            "src/Hexalith.Conversations.MinimalModule.Contracts/Hexalith.Conversations.MinimalModule.Contracts.csproj",
            "src/Hexalith.Conversations.MinimalModule.Contracts/CreateConversation.cs",
            "src/Hexalith.Conversations.MinimalModule.Contracts/ConversationCreated.cs",
            "src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj",
            "src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs",
            "src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs",
            "tests/Hexalith.Conversations.MinimalModule.Tests/Hexalith.Conversations.MinimalModule.Tests.csproj",
            "tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs",
            "tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj",
            "tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs",
        ];
        using JsonDocument document = LoadV2Json();
        JsonElement measurement = document.RootElement.GetProperty("measurement");
        JsonElement[] rows = measurement.GetProperty("paths").EnumerateArray().ToArray();
        string[] actual = rows.Select(row => row.GetProperty("path").GetString()!).ToArray();
        foreach (string path in expected.Where(path => path.Contains(".AppHost/", StringComparison.Ordinal)))
        {
            actual.ShouldContain(fixture + path, "SM2_APPHOST_COST_OMITTED");
        }
        actual.Distinct(StringComparer.Ordinal).Count().ShouldBe(13, "SM2_INCLUSION_RULE_VIOLATION");
        actual.ShouldBe(expected.Select(path => fixture + path).ToArray(), ignoreOrder: false,
            customMessage: "SM2_INCLUSION_RULE_VIOLATION");
        string[] authored = Directory.EnumerateFiles(Path.Combine(root, fixture), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !path.Split('/').Any(part => part is "bin" or "obj" or ".git")
                && !path.EndsWith("/packages.lock.json", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToArray();
        authored.ShouldBe(actual.Order(StringComparer.Ordinal).ToArray(), "SM2_SOURCE_INVENTORY_DRIFT");
        GitFacts git = new();
        string candidate = document.RootElement.GetProperty("candidate").GetProperty("commit").GetString()!;
        int physical = 0;
        int nonblank = 0;
        EvidenceManifest manifest = new();
        foreach (JsonElement row in rows)
        {
            string path = row.GetProperty("path").GetString()!;
            row.GetProperty("included").GetBoolean().ShouldBeTrue("SM2_INCLUSION_RULE_VIOLATION");
            row.GetProperty("category").GetString().ShouldNotBeNullOrWhiteSpace("SM2_INCLUSION_RULE_VIOLATION");
            row.GetProperty("reason").GetString().ShouldNotBeNullOrWhiteSpace("SM2_INCLUSION_RULE_VIOLATION");
            byte[] bytes = RepositoryLocator.ReadAllBytes(Path.Combine(root, path));
            string[] lines = RepositoryLocator.ReadAllLines(Path.Combine(root, path));
            int countedNonblank = lines.Count(line => !string.IsNullOrWhiteSpace(line));
            row.GetProperty("bytes").GetInt32().ShouldBe(bytes.Length, "SM2_MEASUREMENT_INVALID");
            row.GetProperty("physicalLoc").GetInt32().ShouldBe(lines.Length, "SM2_MEASUREMENT_INVALID");
            row.GetProperty("nonblankLoc").GetInt32().ShouldBe(countedNonblank, "SM2_MEASUREMENT_INVALID");
            GitFact<GitBlob> blob = await git.ReadBlobAsync(root, candidate, path, TestContext.Current.CancellationToken);
            blob.IsSuccess.ShouldBeTrue("CHECKPOINT_CANDIDATE_MISMATCH");
            blob.Value!.Content.ShouldBe(bytes, "SM2_MEASUREMENT_INVALID");
            manifest.Add(path, row.GetProperty("sha256").GetString()!, "authored-source");
            physical += lines.Length;
            nonblank += countedNonblank;
        }
        AssertionLedger ledger = new();
        manifest.Verify(root, manifest.ComputeSignablePayloadHash(), ledger);
        ledger.Evaluate().Result.ShouldBe("PASS", "SM2_MEASUREMENT_INVALID");
        measurement.GetProperty("fileCount").GetInt32().ShouldBe(expected.Length, "SM2_MEASUREMENT_INVALID");
        measurement.GetProperty("physicalLoc").GetInt32().ShouldBe(physical, "SM2_MEASUREMENT_INVALID");
        measurement.GetProperty("nonblankLoc").GetInt32().ShouldBe(nonblank, "SM2_MEASUREMENT_INVALID");
        WriteV2Receipt(document.RootElement);
        output.WriteLine($"story113_measured_totals={expected.Length}/{physical}/{nonblank}");
    }

    /// <summary>Verifies immutable historical bytes against independent literal pins.</summary>
    [Fact]
    public void HistoricalBaselinesShouldRemainByteIdentical()
    {
        string root = FindRepositoryRoot();
        EvidenceManifest manifest = new();
        manifest.Add("docs/release-evidence/consume-promote-keep-inventory-v1.json",
            "20bbedb5d0aa1dcd35729aac6a8500f7cf75d8f0c0719b8e1364da5e19fa22b7", "SM1");
        manifest.Add("docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json",
            "14de3c86628cb4f900008be42d086a7699ae0869b3b791f92c810ab921192e03", "SM2-v1");
        manifest.Add("docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md",
            "1cb37b3ddbc9186748c7556a87e5c08feb15d8dee30f4a1b0362bcd5536297fd", "SM2-v1");
        manifest.Add("docs/release-evidence/oq-2-target-interpretation-decision-v1.json",
            "06281924d9760f05f638c4a74661de9cd973f88c773d7ad3263ee25a830a3e06", "SM2-v1");
        AssertionLedger ledger = new();
        manifest.Verify(root, manifest.ComputeSignablePayloadHash(), ledger);
        ledger.Evaluate().Result.ShouldBe("PASS", "SM1_BASELINE_MUTATED or SM2_V1_HISTORY_MUTATED");
        using JsonDocument sm1 = JsonDocument.Parse(RepositoryLocator.ReadAllBytes(
            Path.Combine(root, "docs/release-evidence/consume-promote-keep-inventory-v1.json")));
        sm1.RootElement.GetProperty("plumbingBaselineLoc").GetInt32().ShouldBe(13289, "SM1_BASELINE_MUTATED");
        using JsonDocument evidence = LoadV2Json();
        WriteV2Receipt(evidence.RootElement);
    }

    private static JsonDocument LoadV2Json()
        => JsonDocument.Parse(RepositoryLocator.ReadAllBytes(Path.Combine(ReleaseEvidenceDirectory(),
            "minimal-module-authoring-cost-sm2-v2.json")));

    private void WriteV2Receipt(JsonElement evidence)
    {
        string version = typeof(MinimalModuleAuthoringCostBaselineValidationTest).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        output.WriteLine("story113_source_revision=" + version);
        output.WriteLine("story113_candidate=" + evidence.GetProperty("candidate").GetProperty("commit").GetString());
        byte[] bytes = RepositoryLocator.ReadAllBytes(Path.Combine(ReleaseEvidenceDirectory(),
            "minimal-module-authoring-cost-sm2-v2.json"));
        output.WriteLine("story113_evidence_sha256=" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    private static JsonDocument LoadBaselineJson()
        => JsonDocument.Parse(RepositoryLocator.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), JsonFileName)));

    private static string[] ReadStringArray(JsonElement element)
        => element.EnumerateArray().Select(item => item.GetString()!).ToArray();

    private static decimal CalculateReductionPercentage(int original, int reduced)
        => Math.Round(((decimal)original - reduced) / original * 100m, 2, MidpointRounding.AwayFromZero);

    private static string ReleaseEvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "docs", "release-evidence");

    private static string FindRepositoryRoot()
        => RepositoryLocator.FindRepositoryRoot();
}
