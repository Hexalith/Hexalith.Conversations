// <copyright file="SuccessMetricReportAndAttestationValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    private const string ReleaseOwnerDecisionFileName = "success-metric-report-and-attestation-v1-release-owner-decision.json";
    private const string OqTwoDecisionFileName = "oq-2-target-interpretation-decision-v1.json";
    private const string SignedEvidenceDirectory = "docs/release-evidence/";

    /// <summary>
    /// The signed source identity is pinned here rather than read from evidence alone. A commit read
    /// out of the manifest can be replaced with an unresolvable value, which would silently route every
    /// superseded-artifact check into the unavailable-history path and disable the binding entirely.
    /// </summary>
    private const string SignedV1SourceCommit = "c6670fac7347ecd7240f7bab7e5e23147c8dfc65";

    /// <summary>
    /// Root of trust for the compensating manifest guard. Without pinning these in source, a coordinated
    /// edit of the report plus the decision that declares its hash would satisfy every assertion.
    /// </summary>
    private const string ReleaseOwnerDecisionSha256 = "8091f6c26251420242a491cad100472dc1604a7163cc9d8df51bb1c742844856";

    private const string OqTwoDecisionSha256 = "06281924d9760f05f638c4a74661de9cd973f88c773d7ad3263ee25a830a3e06";

    /// <summary>Bounded wait so a blocked git (index.lock, credential prompt, hook) fails instead of hanging the suite.</summary>
    private const int GitTimeout = 60_000;

    /// <summary>
    /// Signed v1 source artifacts that later corrective authority lawfully supersedes. Every other declared
    /// artifact must still equal its manifest hash in the working tree: the historical fallback compares two
    /// values that were both derived from the signed commit, so it can never fail for a drifted file and is
    /// not a substitute for current-content equality. Keep this list exact and reviewed.
    /// </summary>
    private static readonly string[] SupersededByCorrectiveAuthority =
    [
        "_bmad-output/planning-artifacts/architecture.md",
    ];

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

    private static readonly string[] AllowedSmOneDispositions =
    [
        "consumed",
        "promoted-adopted",
        "reduced-to-thin-facade",
        "retained",
        "deferred",
        "residual",
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
    public void SignedReleaseOwnerDecisionShouldStillBindTheImmutableV1ReportAndSourceIdentity()
    {
        // Terminate the trust chain in source. Without this, editing the report and the decision that
        // declares its hash together satisfies every assertion in the suite.
        ComputeFileSha256(Path.Combine(ReleaseEvidenceDirectory(), ReleaseOwnerDecisionFileName))
            .ShouldBe(ReleaseOwnerDecisionSha256, "The signed release-owner decision must remain byte-identical to the pinned record.");
        ComputeFileSha256(Path.Combine(ReleaseEvidenceDirectory(), OqTwoDecisionFileName))
            .ShouldBe(OqTwoDecisionSha256, "The OQ-2 target-interpretation decision must remain byte-identical to the pinned record.");

        using JsonDocument decisionDoc = LoadEvidenceArtifact(ReleaseOwnerDecisionFileName);
        JsonElement signedSource = decisionDoc.RootElement.GetProperty("sourceAttestation");

        decisionDoc.RootElement.GetProperty("status").GetString().ShouldBe("signed");
        signedSource.GetProperty("artifact").GetString().ShouldBe(SignedEvidenceDirectory + JsonArtifactFileName);
        signedSource.GetProperty("summary").GetString().ShouldBe(SignedEvidenceDirectory + MarkdownArtifactFileName);

        ComputeFileSha256(Path.Combine(ReleaseEvidenceDirectory(), JsonArtifactFileName))
            .ShouldBe(
                signedSource.GetProperty("artifactSha256").GetString(),
                "The signed v1 report must remain byte-identical to the record the release owner signed.");
        ComputeFileSha256(Path.Combine(ReleaseEvidenceDirectory(), MarkdownArtifactFileName))
            .ShouldBe(
                signedSource.GetProperty("summarySha256").GetString(),
                "The signed v1 summary must remain byte-identical to the record the release owner signed.");

        using JsonDocument reportDoc = LoadStoryArtifact();
        reportDoc.RootElement.GetProperty("attestation").GetProperty("signablePayloadHash").GetString()
            .ShouldBe(
                signedSource.GetProperty("signablePayloadHash").GetString(),
                "The signed decision and the v1 report must agree on the signable payload hash.");

        DeclaredV1SourceCommit().Length.ShouldBe(40);
    }

    [Fact]
    public void SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity()
    {
        using JsonDocument doc = LoadStoryArtifact();
        string root = FindRepositoryRoot();

        // Provenance for superseded inputs comes from the immutable signed decision, never from the
        // baseline revision of whatever workflow happens to be running this suite.
        string declaredSourceCommit = DeclaredV1SourceCommit();
        declaredSourceCommit.ShouldBe(
            SignedV1SourceCommit,
            "The signed decision must declare the pinned v1 source identity; a substituted commit would disable the historical binding.");

        JsonElement sourceArtifacts = doc.RootElement.GetProperty("sourceArtifacts");
        sourceArtifacts.GetArrayLength().ShouldBeGreaterThanOrEqualTo(18);

        int supersededVerifiedAgainstHistory = 0;

        foreach (JsonElement entry in sourceArtifacts.EnumerateArray())
        {
            string path = entry.GetProperty("path").GetString() ?? string.Empty;
            string sha256 = (entry.GetProperty("sha256").GetString() ?? string.Empty).ToLowerInvariant();
            string fullPath = Path.GetFullPath(Path.Combine(root, path));

            path.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(path).ShouldBeFalse($"Source artifact path '{path}' must be repository-relative.");
            fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                .ShouldBeTrue($"Source artifact path '{path}' must stay inside the repository root.");
            File.Exists(fullPath).ShouldBeTrue($"Source artifact '{path}' must exist.");

            sha256.Length.ShouldBe(64);
            sha256.ShouldAllBe(character => Uri.IsHexDigit(character));

            path.ShouldNotContain("bin/", Case.Insensitive);
            path.ShouldNotContain("obj/", Case.Insensitive);
            path.ShouldNotContain("/generated/", Case.Insensitive);

            if (string.Equals(ComputeFileSha256(fullPath), sha256, StringComparison.Ordinal))
            {
                continue;
            }

            // Current-content equality is the default for every declared artifact. Only an explicitly
            // reviewed supersession may drift, and only to its content at the signed source identity.
            // Signed release evidence can never appear in that list.
            string normalizedPath = NormalizeRepositoryRelativePath(path);
            normalizedPath.StartsWith(SignedEvidenceDirectory, StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
                $"Signed release evidence '{path}' must remain byte-identical to the signed v1 manifest.");
            SupersededByCorrectiveAuthority.ShouldContain(
                normalizedPath,
                $"Source artifact '{path}' drifted from the signed v1 manifest but is not a reviewed supersession. Add it to SupersededByCorrectiveAuthority only with recorded corrective authority.");

            TryReadGitBlobSha256(SignedV1SourceCommit, path, out string historicalSha256).ShouldBeTrue(
                $"Superseded source artifact '{path}' must exist at signed source commit {SignedV1SourceCommit}.");
            historicalSha256.ShouldBe(
                sha256,
                $"Superseded source artifact '{path}' must match the signed v1 manifest hash at commit {SignedV1SourceCommit}.");
            supersededVerifiedAgainstHistory++;
        }

        // A run that verified nothing against history must not look like a run that verified everything.
        supersededVerifiedAgainstHistory.ShouldBe(
            SupersededByCorrectiveAuthority.Length,
            "Every reviewed supersession must actually be verified against the signed source commit.");
    }

    [Fact]
    public void SupersessionAllowlistShouldStayNarrowAndExcludeSignedEvidence()
    {
        SupersededByCorrectiveAuthority.Length.ShouldBeLessThanOrEqualTo(
            4,
            "The supersession allowlist is a reviewed exception list, not a general escape hatch.");

        foreach (string path in SupersededByCorrectiveAuthority)
        {
            path.ShouldBe(NormalizeRepositoryRelativePath(path), "Allowlist entries must already be normalized.");
            path.StartsWith(SignedEvidenceDirectory, StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
                $"Signed release evidence '{path}' may never be superseded in place.");
            File.Exists(Path.Combine(FindRepositoryRoot(), path)).ShouldBeTrue($"Allowlisted supersession '{path}' must exist.");
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

        string[] expectedAreaIds = inventory.GetProperty("areas")
            .EnumerateArray()
            .Where(area =>
            {
                string classification = area.GetProperty("classification").GetString() ?? string.Empty;
                return string.Equals(classification, "Consume", StringComparison.Ordinal)
                    || string.Equals(classification, "Promote", StringComparison.Ordinal);
            })
            .Select(area => area.GetProperty("areaId").GetString() ?? string.Empty)
            .OrderBy(static areaId => areaId, StringComparer.Ordinal)
            .ToArray();

        string[] actualAreaIds = rowDispositions
            .EnumerateArray()
            .Select(row => row.GetProperty("areaId").GetString() ?? string.Empty)
            .OrderBy(static areaId => areaId, StringComparer.Ordinal)
            .ToArray();

        actualAreaIds.ShouldBe(expectedAreaIds);

        int rowBaseline = 0;
        int rowCurrent = 0;
        int rowReduced = 0;
        foreach (JsonElement row in rowDispositions.EnumerateArray())
        {
            row.GetProperty("areaId").GetString().ShouldNotBeNullOrWhiteSpace();
            AllowedSmOneDispositions.ShouldContain(row.GetProperty("disposition").GetString() ?? string.Empty);
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
    public void SmOneRowEvidenceShouldBePartOfSignedSourceManifest()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        string[] sourcePaths = root.GetProperty("sourceArtifacts")
            .EnumerateArray()
            .Select(artifact => artifact.GetProperty("path").GetString() ?? string.Empty)
            .ToArray();
        string[] evidenceBundle = root.GetProperty("attestation")
            .GetProperty("evidenceBundle")
            .EnumerateArray()
            .Select(path => path.GetString() ?? string.Empty)
            .ToArray();

        evidenceBundle.ShouldBe(sourcePaths);

        JsonElement rows = root.GetProperty("successMetrics").GetProperty("sm1").GetProperty("rowDispositions");
        foreach (JsonElement row in rows.EnumerateArray())
        {
            foreach (JsonElement evidence in row.GetProperty("evidence").EnumerateArray())
            {
                string evidencePath = (evidence.GetString() ?? string.Empty).Split('#')[0];
                sourcePaths.ShouldContain(evidencePath, $"SM-1 row evidence '{evidencePath}' must be included in the signed source manifest.");
            }
        }
    }

    [Fact]
    public void SignablePayloadHashShouldMatchSourceArtifactManifest()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        StringBuilder manifest = new();

        foreach (JsonElement artifact in root.GetProperty("sourceArtifacts").EnumerateArray())
        {
            manifest.Append(artifact.GetProperty("path").GetString());
            manifest.Append('\t');
            manifest.Append(artifact.GetProperty("sha256").GetString());
            manifest.Append('\t');
            manifest.Append(artifact.GetProperty("role").GetString());
            manifest.Append('\n');
        }

        string actualHash = ComputeTextSha256(manifest.ToString());
        root.GetProperty("attestation").GetProperty("signablePayloadHash").GetString().ShouldBe(actualHash);
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
        JsonElement decisionFields = attestation.GetProperty("decisionFields");
        decisionFields.GetProperty("releaseOwnerDecision").GetString().ShouldBe("pending");
        decisionFields.GetProperty("residualRiskAcceptance").GetString().ShouldBe("pending");
        decisionFields.GetProperty("platformControlDependencyAcknowledgement").GetString().ShouldBe("pending");
        decisionFields.GetProperty("notes").ValueKind.ShouldBe(JsonValueKind.Null);

        string statement = attestation.GetProperty("statement").GetString() ?? string.Empty;
        statement.ShouldNotContain("CISO sign-off", Case.Insensitive);
        statement.ShouldNotContain("SOC2", Case.Insensitive);
        statement.ShouldNotContain("ISO 27001 attestation", Case.Insensitive);
        statement.ShouldNotContain("pen-test approval", Case.Insensitive);
    }

    [Fact]
    public void FinalDiffShouldMatchIntendedEvidenceBoundaryAndExcludeSubmoduleGitlinks()
    {
        using JsonDocument doc = LoadStoryArtifact();
        JsonElement root = doc.RootElement;
        JsonElement finalDiffBoundary = root.GetProperty("validation").GetProperty("finalDiffBoundary");
        string baselineCommit = finalDiffBoundary.GetProperty("baselineCommit").GetString() ?? string.Empty;
        string storyDoneCommit = finalDiffBoundary.GetProperty("storyDoneCommit").GetString() ?? string.Empty;
        string[] expectedChangedFiles = finalDiffBoundary
            .GetProperty("expectedChangedFiles")
            .EnumerateArray()
            .Select(path => path.GetString() ?? string.Empty)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        AssertCommitIdentity(baselineCommit, nameof(baselineCommit));
        AssertCommitIdentity(storyDoneCommit, nameof(storyDoneCommit));

        // The recorded boundary is historical. A shallow or partial clone cannot resolve it, and missing
        // history must not be reported as a boundary violation — but it must also not be reported as a
        // pass. Skipping keeps an unverified run visibly distinct from a verified one.
        if (!GitRevisionIsAvailable(baselineCommit) || !GitRevisionIsAvailable(storyDoneCommit))
        {
            Assert.Skip(
                $"Cannot verify the recorded evidence boundary: history for {baselineCommit}..{storyDoneCommit} is unavailable in this clone (shallow, partial, or non-repository checkout).");
        }

        string[] actualChangedFiles = RunGit("diff", "--name-only", $"{baselineCommit}..{storyDoneCommit}")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        actualChangedFiles.ShouldBe(expectedChangedFiles);

        // Isolate the mode columns instead of substring-matching the whole raw diff: a blob hash or a
        // file name can legitimately contain "160000".
        foreach (string rawLine in RunGit("diff", "--raw", $"{baselineCommit}..{storyDoneCommit}")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Raw format: :<srcmode> <dstmode> <srcsha> <dstsha> <status>\t<path>
            string[] fields = rawLine.TrimStart(':').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            fields.Length.ShouldBeGreaterThanOrEqualTo(2, $"Unexpected git raw diff line: '{rawLine}'.");
            fields[0].ShouldNotBe("160000", $"Recorded evidence boundary must exclude submodule gitlinks: '{rawLine}'.");
            fields[1].ShouldNotBe("160000", $"Recorded evidence boundary must exclude submodule gitlinks: '{rawLine}'.");
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

    /// <summary>
    /// Normalizes a declared repository-relative path so a spelling difference cannot move an artifact out
    /// of the signed-evidence strictness branch.
    /// </summary>
    private static string NormalizeRepositoryRelativePath(string path)
    {
        string normalized = path.Replace('\\', '/').Trim();

        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized.TrimStart('/');
    }

    private static string ComputeFileSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string ComputeTextSha256(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static void AssertCommitIdentity(string commit, string name)
    {
        commit.ShouldNotBeNullOrWhiteSpace($"{name} must be recorded.");
        commit.Length.ShouldBe(40, $"{name} must be a full 40-character commit id so it cannot resolve ambiguously.");
        commit.ShouldAllBe(character => Uri.IsHexDigit(character));
    }

    private static string DeclaredV1SourceCommit()
    {
        using JsonDocument decisionDoc = LoadEvidenceArtifact(ReleaseOwnerDecisionFileName);
        string commit = decisionDoc.RootElement
            .GetProperty("sourceAttestation")
            .GetProperty("sourceCommit")
            .GetString() ?? string.Empty;

        AssertCommitIdentity(commit, "The signed release-owner decision source commit");
        return commit;
    }

    private static bool GitRevisionIsAvailable(string revision)
        => TryRunGit(out _, "rev-parse", "--verify", "--quiet", revision + "^{commit}");

    private static bool TryReadGitBlobSha256(string revision, string repositoryRelativePath, out string sha256)
    {
        sha256 = string.Empty;

        if (!TryStartGit(CreateGitStartInfo("cat-file", "blob", $"{revision}:{repositoryRelativePath}"), out Process? process, out _))
        {
            return false;
        }

        using Process started = process;
        using MemoryStream buffer = new();

        // Drain stderr concurrently: reading stdout to completion first deadlocks whenever git fills
        // the stderr pipe (warnings are emitted per file and can exceed the pipe buffer).
        Task<string> errorTask = started.StandardError.ReadToEndAsync();
        started.StandardOutput.BaseStream.CopyTo(buffer);
        WaitForGitExit(started, "cat-file", "blob", $"{revision}:{repositoryRelativePath}");
        errorTask.Wait(GitTimeout);

        if (started.ExitCode != 0)
        {
            return false;
        }

        sha256 = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
        return true;
    }

    private static string RunGit(params string[] arguments)
    {
        bool succeeded = TryRunGit(out string output, out string error, arguments);
        succeeded.ShouldBeTrue($"git {string.Join(' ', arguments)} failed: {error}");
        return output;
    }

    private static bool TryRunGit(out string output, params string[] arguments)
        => TryRunGit(out output, out _, arguments);

    private static bool TryRunGit(out string output, out string error, params string[] arguments)
    {
        output = string.Empty;

        if (!TryStartGit(CreateGitStartInfo(arguments), out Process? process, out error))
        {
            return false;
        }

        using Process started = process;
        Task<string> outputTask = started.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = started.StandardError.ReadToEndAsync();
        WaitForGitExit(started, arguments);
        output = outputTask.Wait(GitTimeout) ? outputTask.Result : string.Empty;
        error = errorTask.Wait(GitTimeout) ? errorTask.Result : string.Empty;
        return started.ExitCode == 0;
    }

    private static bool TryStartGit(ProcessStartInfo startInfo, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Process? process, out string error)
    {
        process = null;
        error = string.Empty;

        try
        {
            process = Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            // git is absent from PATH (source tarball, minimal container). Report unavailable history
            // rather than letting an unhandled exception escape the graceful-degradation path.
            error = $"git could not be started: {exception.Message}";
            return false;
        }

        if (process is null)
        {
            error = "git could not be started.";
            return false;
        }

        return true;
    }

    private static void WaitForGitExit(Process process, params string[] arguments)
    {
        if (!process.WaitForExit(GitTimeout))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Already exited between the timeout and the kill.
            }

            throw new TimeoutException($"git {string.Join(' ', arguments)} did not complete within {GitTimeout}ms.");
        }
    }

    private static ProcessStartInfo CreateGitStartInfo(params string[] arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "git",
            WorkingDirectory = FindRepositoryRoot(),
            RedirectStandardError = true,
            RedirectStandardOutput = true,

            // Decode git output as UTF-8 rather than the ambient console codepage, which on Windows would
            // corrupt non-ASCII paths before they are ever compared.
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        // Emit raw UTF-8 paths instead of octal-escaped, locale-decoded ones, so a non-ASCII changed path
        // compares correctly against the recorded boundary.
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("core.quotepath=false");

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
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
