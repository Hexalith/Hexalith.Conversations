// <copyright file="OqTwoTargetInterpretationDecisionValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Validates the approved OQ-2 target interpretation and its immutable historical evidence boundary.
/// </summary>
public sealed class OqTwoTargetInterpretationDecisionValidationTest
{
    private const string DecisionJsonFileName = "oq-2-target-interpretation-decision-v1.json";
    private const string DecisionMarkdownFileName = "oq-2-target-interpretation-decision-v1.md";
    private const string MinimalModuleFileName = "minimal-module-authoring-cost-sm2-baseline-v1.json";
    private const string SuccessMetricReportFileName = "success-metric-report-and-attestation-v1.json";
    private const string ReleaseOwnerDecisionFileName = "success-metric-report-and-attestation-v1-release-owner-decision.json";

    [Fact]
    public void DecisionArtifactsShouldExistAndRecordApprovedResolution()
    {
        string jsonPath = Path.Combine(ReleaseEvidenceDirectory(), DecisionJsonFileName);
        string markdownPath = Path.Combine(ReleaseEvidenceDirectory(), DecisionMarkdownFileName);

        File.Exists(jsonPath).ShouldBeTrue("The machine-readable OQ-2 decision must be committed.");
        File.Exists(markdownPath).ShouldBeTrue("The human-readable OQ-2 decision must be committed.");

        using JsonDocument doc = LoadDecision();
        JsonElement root = doc.RootElement;
        string markdown = File.ReadAllText(markdownPath);

        root.GetProperty("artifact").GetString().ShouldBe("oq-2-target-interpretation-decision");
        root.GetProperty("version").GetInt32().ShouldBe(1);
        root.GetProperty("status").GetString().ShouldBe("approved");
        root.GetProperty("oq2Status").GetString().ShouldBe("resolved-confirmed");
        root.GetProperty("approvedBy").GetString().ShouldBe("Jerome");
        root.GetProperty("approvalReference").GetString().ShouldBe(
            "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-oq-2-target-interpretation.md");

        markdown.ShouldContain("**Status:** approved", Case.Sensitive);
        markdown.ShouldContain("**OQ-2 status:** resolved-confirmed", Case.Sensitive);
        markdown.ShouldContain("**Approved by:** Jerome", Case.Sensitive);
        markdown.ShouldContain("The JSON artifact is authoritative", Case.Sensitive);
    }

    [Fact]
    public void SmOneTargetShouldRecomputeAndBeMetInclusively()
    {
        using JsonDocument decisionDoc = LoadDecision();
        using JsonDocument reportDoc = LoadEvidenceArtifact(SuccessMetricReportFileName);

        JsonElement sm1 = decisionDoc.RootElement.GetProperty("sm1");
        JsonElement reportSm1 = reportDoc.RootElement.GetProperty("successMetrics").GetProperty("sm1");

        int baseline = reportSm1.GetProperty("baselinePlumbingLoc").GetInt32();
        int removed = reportSm1.GetProperty("removedOrExternalizedPlumbingLoc").GetInt32();
        decimal expectedReduction = CalculateReductionPercentage(baseline, removed);

        sm1.GetProperty("baselinePlumbingLoc").GetInt32().ShouldBe(baseline);
        sm1.GetProperty("removedOrExternalizedPlumbingLoc").GetInt32().ShouldBe(removed);
        sm1.GetProperty("reductionPercentage").GetDecimal().ShouldBe(expectedReduction);
        sm1.GetProperty("thresholdPercentage").GetDecimal().ShouldBe(40.0m);
        sm1.GetProperty("comparisonOperator").GetString().ShouldBe(">=");
        sm1.GetProperty("comparisonRule").GetString().ShouldBe("inclusive");
        (expectedReduction >= sm1.GetProperty("thresholdPercentage").GetDecimal()).ShouldBeTrue();
        sm1.GetProperty("result").GetString().ShouldBe("met");
    }

    [Fact]
    public void SmTwoTargetShouldRecomputeAndRemainEstimateQualified()
    {
        using JsonDocument decisionDoc = LoadDecision();
        using JsonDocument baselineDoc = LoadEvidenceArtifact(MinimalModuleFileName);

        JsonElement sm2 = decisionDoc.RootElement.GetProperty("sm2");
        JsonElement source = baselineDoc.RootElement;
        int templateFiles = source.GetProperty("templateMinimal").GetProperty("fileCount").GetInt32();
        int preInitiativeFiles = source.GetProperty("preInitiativeEquivalent").GetProperty("fileCount").GetInt32();
        int templateLoc = source.GetProperty("templateMinimal").GetProperty("loc").GetInt32();
        int preInitiativeLoc = source.GetProperty("preInitiativeEquivalent").GetProperty("loc").GetInt32();
        decimal expectedFileReduction = CalculateReductionPercentage(preInitiativeFiles, preInitiativeFiles - templateFiles);
        decimal expectedLocReduction = CalculateReductionPercentage(preInitiativeLoc, preInitiativeLoc - templateLoc);

        sm2.GetProperty("templateMinimalFileCount").GetInt32().ShouldBe(templateFiles);
        sm2.GetProperty("preInitiativeFileCount").GetInt32().ShouldBe(preInitiativeFiles);
        sm2.GetProperty("templateMinimalLoc").GetInt32().ShouldBe(templateLoc);
        sm2.GetProperty("preInitiativeLoc").GetInt32().ShouldBe(preInitiativeLoc);
        sm2.GetProperty("fileReductionPercentage").GetDecimal().ShouldBe(expectedFileReduction);
        sm2.GetProperty("locReductionPercentage").GetDecimal().ShouldBe(expectedLocReduction);
        sm2.GetProperty("thresholdPercentage").GetDecimal().ShouldBe(50.0m);
        sm2.GetProperty("comparisonOperator").GetString().ShouldBe(">=");
        sm2.GetProperty("comparisonRule").GetString().ShouldBe("inclusive");
        sm2.GetProperty("decisiveDimension").GetString().ShouldBe("file-count-reduction");
        sm2.GetProperty("supportingDimension").GetString().ShouldBe("loc-reduction");
        sm2.GetProperty("measurementType").GetString().ShouldBe("estimated");
        sm2.GetProperty("confidence").GetString().ShouldBe("low");
        expectedFileReduction.ShouldBe(50.0m);
        (expectedFileReduction >= sm2.GetProperty("thresholdPercentage").GetDecimal()).ShouldBeTrue();
        sm2.GetProperty("result").GetString().ShouldBe("met-on-accepted-estimate");

        string limitations = string.Join(
            ' ',
            sm2.GetProperty("limitations").EnumerateArray().Select(item => item.GetString()));
        limitations.ShouldContain("manifest baseline", Case.Insensitive);
        limitations.ShouldContain("low-confidence estimate", Case.Insensitive);
        limitations.ShouldContain("must not be presented as unconditional or high-confidence proof", Case.Insensitive);
    }

    [Fact]
    public void HistoricalEvidenceBindingsShouldMatchAndRemainPointInTimeEvidence()
    {
        using JsonDocument decisionDoc = LoadDecision();
        JsonElement root = decisionDoc.RootElement;
        JsonElement bindings = root.GetProperty("historicalEvidenceBindings");
        string repositoryRoot = FindRepositoryRoot();

        bindings.GetArrayLength().ShouldBe(6);

        foreach (JsonElement binding in bindings.EnumerateArray())
        {
            string relativePath = binding.GetProperty("path").GetString() ?? string.Empty;
            string expectedHash = binding.GetProperty("sha256").GetString() ?? string.Empty;
            string fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));

            Path.IsPathRooted(relativePath).ShouldBeFalse();
            fullPath.StartsWith(repositoryRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                .ShouldBeTrue($"Historical evidence path '{relativePath}' must stay inside the repository.");
            File.Exists(fullPath).ShouldBeTrue($"Historical evidence file '{relativePath}' must exist.");
            ComputeFileSha256(fullPath).ShouldBe(expectedHash, $"Historical evidence file '{relativePath}' must remain byte-identical.");
            binding.GetProperty("role").GetString().ShouldNotBeNullOrWhiteSpace();
        }

        using JsonDocument historicalReport = LoadEvidenceArtifact(SuccessMetricReportFileName);
        historicalReport.RootElement.GetProperty("successMetrics").GetProperty("sm1").GetProperty("targetStatus").GetString()
            .ShouldBe("unknown-accepted");
        historicalReport.RootElement.GetProperty("successMetrics").GetProperty("sm2").GetProperty("targetStatus").GetString()
            .ShouldBe("unconfirmed-estimate-only");

        using JsonDocument historicalReleaseDecision = LoadEvidenceArtifact(ReleaseOwnerDecisionFileName);
        historicalReleaseDecision.RootElement.GetProperty("status").GetString().ShouldBe("signed");
        historicalReleaseDecision.RootElement.GetProperty("decision").GetProperty("acceptedRiskIds")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ShouldContain("oq-2-target-confirmation");

        string preservationRules = string.Join(
            ' ',
            root.GetProperty("preservationRules").EnumerateArray().Select(item => item.GetString()));
        preservationRules.ShouldContain("point-in-time statement", Case.Insensitive);
        preservationRules.ShouldContain("prospectively supersedes", Case.Insensitive);

        string nonClaims = string.Join(
            ' ',
            root.GetProperty("nonClaims").EnumerateArray().Select(item => item.GetString()));
        nonClaims.ShouldContain("does not re-sign", Case.Insensitive);
        nonClaims.ShouldContain("does not claim high-confidence", Case.Insensitive);
        nonClaims.ShouldContain("does not approve inherited platform controls", Case.Insensitive);
    }

    private static decimal CalculateReductionPercentage(int baseline, int removed)
        => Math.Round((decimal)removed / baseline * 100m, 2, MidpointRounding.AwayFromZero);

    private static string ComputeFileSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static JsonDocument LoadDecision()
        => LoadEvidenceArtifact(DecisionJsonFileName);

    private static JsonDocument LoadEvidenceArtifact(string fileName)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), fileName)));

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
