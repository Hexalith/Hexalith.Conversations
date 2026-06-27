// <copyright file="SuccessMetricReportAndAttestationValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Linq;
using System.Text.Json;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 5.3 (AC6) — validates the final success-metric report and signable attestation consumed by release review.
/// </summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class SuccessMetricReportAndAttestationValidationTest
{
    private const string JsonArtifactFileName = "success-metric-report-and-attestation-v1.json";
    private const string MarkdownArtifactFileName = "success-metric-report-and-attestation-v1.md";
    private const string InventoryFileName = "consume-promote-keep-inventory-v1.json";
    private const string MinimalModuleFileName = "minimal-module-authoring-cost-sm2-baseline-v1.json";
    private const string FinalConformanceFileName = "final-conformance-contract-diff-v1.json";
    private const string RemovedTestLedgerFileName = "removed-test-justification-ledger-reconciliation-v1.json";

    private static readonly string[] RequiredRootProperties =
    [
        "artifact",
        "version",
        "status",
        "story",
        "generatedAtUtc",
        "baselineCommit",
        "sourceArtifacts",
        "successMetrics",
        "behaviorPreservation",
        "removedTestLedger",
        "residualRisks",
        "attestation",
        "validation",
        "environmentLimitations",
        "story5Reference",
    ];

    private static readonly string[] RequiredResidualRiskIds =
    [
        "oq-2-target-confirmation",
        "projection-read-store-population",
        "conformance-tests-server-coupling",
        "inherited-platform-controls",
        "environment-limitations",
    ];

    [Fact]
    public void JsonAndMarkdownArtifactsShouldExistAndExposeRequiredFields()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        string markdown = LoadMarkdownArtifact();

        foreach (string propertyName in RequiredRootProperties)
        {
            root.TryGetProperty(propertyName, out _).ShouldBeTrue($"Story 5.3 artifact must expose '{propertyName}'.");
        }

        root.GetProperty("artifact").GetString().ShouldBe("success-metric-report-and-attestation");
        root.GetProperty("version").GetString().ShouldBe("1");
        root.GetProperty("status").GetString().ShouldBe("ready-for-signature");
        root.GetProperty("story").GetString().ShouldBe("5.3");
        root.GetProperty("story5Reference").GetString().ShouldBe("Epic 5 final release-evidence attestation");

        markdown.ShouldContain($"**Status:** {root.GetProperty("status").GetString()}");
        markdown.ShouldContain($"**Generated:** {root.GetProperty("generatedAtUtc").GetString()}");
        markdown.ShouldContain(JsonArtifactFileName);
        markdown.ShouldContain("JSON artifact is authoritative");
    }

    [Fact]
    public void SourceArtifactsShouldBeRepositoryRelativeExistingFilesWithHashes()
    {
        using JsonDocument doc = LoadStoryArtifact();
        string root = FindRepositoryRoot();
        JsonElement sourceArtifacts = doc.RootElement.GetProperty("sourceArtifacts");

        sourceArtifacts.GetArrayLength().ShouldBeGreaterThanOrEqualTo(18);

        foreach (JsonElement entry in sourceArtifacts.EnumerateArray())
        {
            string path = entry.GetProperty("path").GetString() ?? string.Empty;
            string sha256 = entry.GetProperty("sha256").GetString() ?? string.Empty;

            path.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(path).ShouldBeFalse($"Source artifact path '{path}' must be repository-relative.");
            File.Exists(Path.Combine(root, path)).ShouldBeTrue($"Source artifact '{path}' must exist.");

            sha256.Length.ShouldBe(64);
            sha256.ShouldAllBe(character => Uri.IsHexDigit(character));

            path.ShouldNotContain("bin/", Case.Insensitive);
            path.ShouldNotContain("obj/", Case.Insensitive);
            path.ShouldNotContain("/generated/", Case.Insensitive);
        }
    }

    [Fact]
    public void SmOneFactsShouldMatchAcceptedInventoryAndRowDispositionMath()
    {
        using JsonDocument reportDoc = LoadStoryArtifact();
        using JsonDocument inventoryDoc = LoadEvidenceArtifact(InventoryFileName);

        JsonElement sm1 = reportDoc.RootElement.GetProperty("successMetrics").GetProperty("sm1");
        JsonElement inventory = inventoryDoc.RootElement;

        sm1.GetProperty("sourceTotalLoc").GetInt32().ShouldBe(inventory.GetProperty("sourceTotalLoc").GetInt32());
        sm1.GetProperty("baselinePlumbingLoc").GetInt32().ShouldBe(inventory.GetProperty("plumbingBaselineLoc").GetInt32());
        sm1.GetProperty("consumeLoc").GetInt32().ShouldBe(inventory.GetProperty("plumbingDerivation").GetProperty("consumeSubtotal").GetInt32());
        sm1.GetProperty("promoteLoc").GetInt32().ShouldBe(inventory.GetProperty("plumbingDerivation").GetProperty("promoteSubtotal").GetInt32());
        sm1.GetProperty("keepLoc").GetInt32().ShouldBe(22480);
        sm1.GetProperty("changeLogEntryIds").GetArrayLength().ShouldBe(inventory.GetProperty("changeLog").GetArrayLength());
        sm1.GetProperty("targetStatus").GetString().ShouldBe("unknown-accepted");

        JsonElement rowDispositions = sm1.GetProperty("rowDispositions");
        rowDispositions.GetArrayLength().ShouldBe(13);

        int rowBaseline = 0;
        int rowCurrent = 0;
        int rowReduced = 0;
        foreach (JsonElement row in rowDispositions.EnumerateArray())
        {
            row.GetProperty("areaId").GetString().ShouldNotBeNullOrWhiteSpace();
            row.GetProperty("disposition").GetString().ShouldNotBeNullOrWhiteSpace();
            row.GetProperty("evidence").GetArrayLength().ShouldBeGreaterThan(0);

            int baseline = row.GetProperty("baselineLoc").GetInt32();
            int current = row.GetProperty("currentModuleOwnedPlumbingLoc").GetInt32();
            int reduced = row.GetProperty("removedOrExternalizedPlumbingLoc").GetInt32();

            (current + reduced).ShouldBe(baseline, $"SM-1 row '{row.GetProperty("areaId").GetString()}' must reconcile to its frozen baseline LOC.");
            rowBaseline += baseline;
            rowCurrent += current;
            rowReduced += reduced;
        }

        rowBaseline.ShouldBe(sm1.GetProperty("baselinePlumbingLoc").GetInt32());
        rowCurrent.ShouldBe(sm1.GetProperty("currentModuleOwnedPlumbingLoc").GetInt32());
        rowReduced.ShouldBe(sm1.GetProperty("removedOrExternalizedPlumbingLoc").GetInt32());
        Math.Round(rowReduced * 100.0 / rowBaseline, 2).ShouldBe(sm1.GetProperty("reductionPercentage").GetDouble());
    }

    [Fact]
    public void SmTwoFactsShouldMatchStoryFourTwoEvidenceWithoutClosingOqTwo()
    {
        using JsonDocument reportDoc = LoadStoryArtifact();
        using JsonDocument sm2Doc = LoadEvidenceArtifact(MinimalModuleFileName);

        JsonElement sm2 = reportDoc.RootElement.GetProperty("successMetrics").GetProperty("sm2");
        JsonElement source = sm2Doc.RootElement;

        sm2.GetProperty("templateMinimalFileCount").GetInt32().ShouldBe(source.GetProperty("templateMinimal").GetProperty("fileCount").GetInt32());
        sm2.GetProperty("templateMinimalLoc").GetInt32().ShouldBe(source.GetProperty("templateMinimal").GetProperty("loc").GetInt32());
        sm2.GetProperty("preInitiativeFileCount").GetInt32().ShouldBe(source.GetProperty("preInitiativeEquivalent").GetProperty("fileCount").GetInt32());
        sm2.GetProperty("preInitiativeLoc").GetInt32().ShouldBe(source.GetProperty("preInitiativeEquivalent").GetProperty("loc").GetInt32());
        sm2.GetProperty("fileReductionPercentage").GetDouble().ShouldBe(source.GetProperty("comparison").GetProperty("fileReductionPercentage").GetDouble());
        sm2.GetProperty("locReductionPercentage").GetDouble().ShouldBe(source.GetProperty("comparison").GetProperty("locReductionPercentage").GetDouble());
        sm2.GetProperty("oq2Status").GetString().ShouldBe(source.GetProperty("oq2Status").GetString());
        sm2.GetProperty("targetStatus").GetString().ShouldBe("unconfirmed-estimate-only");
    }

    [Fact]
    public void BehaviorPreservationAndRemovedTestFactsShouldMatchSourceArtifacts()
    {
        using JsonDocument reportDoc = LoadStoryArtifact();
        using JsonDocument finalDoc = LoadEvidenceArtifact(FinalConformanceFileName);
        using JsonDocument ledgerDoc = LoadEvidenceArtifact(RemovedTestLedgerFileName);

        JsonElement behavior = reportDoc.RootElement.GetProperty("behaviorPreservation");
        JsonElement finalConformance = finalDoc.RootElement.GetProperty("conformanceRun");
        JsonElement finalContractDiff = finalDoc.RootElement.GetProperty("contractShapeDiff");

        behavior.GetProperty("story51").GetProperty("status").GetString().ShouldBe(finalDoc.RootElement.GetProperty("status").GetString());
        behavior.GetProperty("story51").GetProperty("conformance").GetProperty("total").GetInt32().ShouldBe(finalConformance.GetProperty("total").GetInt32());
        behavior.GetProperty("story51").GetProperty("conformance").GetProperty("passed").GetInt32().ShouldBe(finalConformance.GetProperty("passed").GetInt32());
        behavior.GetProperty("story51").GetProperty("releaseGateSuiteClassCount").GetInt32().ShouldBe(finalConformance.GetProperty("suiteClassCount").GetInt32());
        behavior.GetProperty("story51").GetProperty("contractShape").GetProperty("baselineTypeCount").GetInt32().ShouldBe(finalContractDiff.GetProperty("baselineTypeCount").GetInt32());
        behavior.GetProperty("story51").GetProperty("contractShape").GetProperty("finalTypeCount").GetInt32().ShouldBe(finalContractDiff.GetProperty("finalTypeCount").GetInt32());
        behavior.GetProperty("story51").GetProperty("contractShape").GetProperty("diffStatus").GetString().ShouldBe("empty");
        behavior.GetProperty("story51").GetProperty("contractShape").GetProperty("baselineGitDiff").GetString().ShouldBe("empty");

        JsonElement ledger = reportDoc.RootElement.GetProperty("removedTestLedger");
        JsonElement sourceValidation = ledgerDoc.RootElement.GetProperty("validation");
        JsonElement sourceContinuity = ledgerDoc.RootElement.GetProperty("conformanceSuiteContinuity");

        ledger.GetProperty("status").GetString().ShouldBe(ledgerDoc.RootElement.GetProperty("status").GetString());
        ledger.GetProperty("actualDeadPlumbingRemovalCount").GetInt32().ShouldBe(sourceValidation.GetProperty("actualRemovalCount").GetInt32());
        ledger.GetProperty("currentConformance").GetProperty("total").GetInt32().ShouldBe(sourceValidation.GetProperty("fullConformance").GetProperty("total").GetInt32());
        ledger.GetProperty("currentConformance").GetProperty("passed").GetInt32().ShouldBe(sourceValidation.GetProperty("fullConformance").GetProperty("passed").GetInt32());
        ledger.GetProperty("releaseGateSuiteClassCount").GetInt32().ShouldBe(sourceContinuity.GetProperty("currentVerification").GetProperty("suiteClassCount").GetInt32());
        ledger.GetProperty("releaseGateSuiteMissing").GetArrayLength().ShouldBe(0);
        ledger.GetProperty("residualCoupling").GetProperty("decision").GetString().ShouldBe("retain-residual-coupling");
    }

    [Fact]
    public void ResidualRisksAndAttestationShouldBeSignableButUnsigned()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        JsonElement risks = root.GetProperty("residualRisks");

        string[] riskIds = risks
            .EnumerateArray()
            .Select(risk => risk.GetProperty("id").GetString() ?? string.Empty)
            .ToArray();

        foreach (string requiredRiskId in RequiredResidualRiskIds)
        {
            riskIds.ShouldContain(requiredRiskId);
        }

        foreach (JsonElement risk in risks.EnumerateArray())
        {
            risk.GetProperty("status").GetString().ShouldNotBeNullOrWhiteSpace();
            risk.GetProperty("owner").GetString().ShouldNotBeNullOrWhiteSpace();
            risk.GetProperty("decision").GetString().ShouldNotBeNullOrWhiteSpace();
            risk.GetProperty("evidence").GetArrayLength().ShouldBeGreaterThan(0);
            (risk.TryGetProperty("requiredBefore", out _) || risk.TryGetProperty("acceptedBy", out _)).ShouldBeTrue();
        }

        JsonElement attestation = root.GetProperty("attestation");
        attestation.GetProperty("signatureStatus").GetString().ShouldBe("ready-for-signature");
        attestation.GetProperty("decision").GetString().ShouldBe("pending");
        attestation.GetProperty("signablePayloadHash").GetString().ShouldNotBeNullOrWhiteSpace();
        attestation.GetProperty("evidenceBundle").GetArrayLength().ShouldBeGreaterThan(0);
        attestation.GetProperty("signer").ValueKind.ShouldBe(JsonValueKind.Null);
        attestation.GetProperty("signedAtUtc").ValueKind.ShouldBe(JsonValueKind.Null);
        attestation.GetProperty("approvalReference").ValueKind.ShouldBe(JsonValueKind.Null);

        attestation.GetProperty("signatureStatus").GetString().ShouldNotBe("signed");
        string statement = attestation.GetProperty("statement").GetString() ?? string.Empty;
        statement.ShouldNotContain("CISO sign-off", Case.Insensitive);
        statement.ShouldNotContain("SOC2", Case.Insensitive);
        statement.ShouldNotContain("ISO 27001 attestation", Case.Insensitive);
        statement.ShouldNotContain("pen-test approval", Case.Insensitive);
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
            value.ShouldNotContain("mutable working", Case.Insensitive, $"Evidence value at {path} must not cite mutable working directories as source-of-truth evidence.");

            if (!path.EndsWith(".command", StringComparison.Ordinal)
                && !path.EndsWith(".fallbackCommand", StringComparison.Ordinal))
            {
                value.ShouldNotContain("bin/", Case.Insensitive, $"Only validation command strings may cite a bin/ executable fallback.");
            }
        }

        string markdown = LoadMarkdownArtifact();
        markdown.ShouldNotContain("/home/", Case.Insensitive);
        markdown.ShouldNotContain("/tmp/", Case.Insensitive);
        markdown.ShouldNotContain("obj/", Case.Insensitive);
        markdown.ShouldNotContain("/generated/", Case.Insensitive);
    }

    private static JsonDocument LoadStoryArtifact()
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), JsonArtifactFileName);
        File.Exists(path).ShouldBeTrue($"Expected Story 5.3 JSON artifact at '{path}'.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static JsonDocument LoadEvidenceArtifact(string fileName)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), fileName)));

    private static string LoadMarkdownArtifact()
    {
        string path = Path.Combine(ReleaseEvidenceDirectory(), MarkdownArtifactFileName);
        File.Exists(path).ShouldBeTrue($"Expected Story 5.3 Markdown artifact at '{path}'.");
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
