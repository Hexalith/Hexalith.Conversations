// <copyright file="PreservationTraceabilityManifestValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Independently validates the unapproved rc.2 preservation successor without rewriting approved rc.1.
/// </summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class PreservationTraceabilityManifestValidationTest
{
    private const string ManifestPath = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json";
    private const string MarkdownPath = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.md";
    private const string SchemaPath = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.schema.json";
    private const string DigestPath = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.sha256";
    private const string Rc1Path = "docs/release-evidence/preservation-traceability-manifest-v3.json";
    private const string Rc1ApprovalPath = "docs/release-evidence/preservation-traceability-manifest-v3-owner-approval.json";
    private const string PrdPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md";
    private const string UxMapPath = "_bmad-output/planning-artifacts/ux-requirement-map.md";
    private const string UxSpecificationPath = "_bmad-output/planning-artifacts/ux-design-specification.md";
    private const string Rc1Sha256 = "a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc";
    private const string Rc1ApprovalSha256 = "23e07e8339d8822c35ef665d44a60238e237c4c29e37560d6f475585dd27df5a";
    private const string BaseCommit = "2d2ae57db1fdcc164fe01ac4b1d99af15c31b324";
    private const string Rc2PublicationCommit = "5ad5d3c0fb57c6ce5f5bbd4567ae9b5a5397e60d";
    private const string Rc2LogProvenanceCommit = "df482b4e652907e100f763615a8a8c4370565066";
    private const string V24PublicationCommit = "20e2cdd2b37e6387055b241c7ee45fc54e836742";
    private const string CandidateDigestAlgorithm = "sha256-path-mode-hash-size-overlay-v2";
    private const string CandidateKind = "base-plus-content-addressed-overlay";
    private const string SourceInputMode = "100644";
    private const string XmlResultsPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-remediated.xml";
    private const string RunReceiptPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-run-receipt.json";
    private const string SemanticResultsPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-semantic-results.json";
    private const string BuildLogPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-build-remediated.log";
    private const string BuildReceiptPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-build-receipt.json";
    private const string ToolchainPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-dotnet-info.txt";
    private const string RestoreReceiptPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-receipt.json";
    private const string RestoreLogPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore.log";
    private const string DetachedIndexPath = "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.json";
    private const string DetachedDigestPath = "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.sha256";

    private static readonly string[] SourceInputPaths =
    [
        ".agents/skills/bmad-build-auto/step-04-review.md",
        ".agents/skills/bmad-build/step-05-present.md",
        ".agents/skills/bmad-build/step-oneshot.md",
        ".agents/skills/bmad-code-review/steps/step-04-present.md",
        ".claude/skills/bmad-build-auto/step-04-review.md",
        ".claude/skills/bmad-build/step-05-present.md",
        ".claude/skills/bmad-build/step-oneshot.md",
        ".claude/skills/bmad-code-review/steps/step-04-present.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
        "_bmad/scripts/generate_preservation_traceability_manifest_v3.py",
        "_bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py",
        "_bmad/scripts/tests/test_generate_preservation_traceability_manifest.py",
        "_bmad/scripts/tests/test_verify_submodule_promotion.py",
        "docs/release-evidence/projection-read-store-population-proof-v2-binding-resolution.json",
        "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs",
    ];

    private static readonly string[] AssessmentWorkflowRecordPaths =
    [
        "_bmad-output/implementation-artifacts/spec-remediate-preservation-traceability-v3-conformance.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/.memlog.md",
    ];

    private static readonly string[] GeneratedOutputPaths =
    [
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-build-receipt.json",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-dotnet-info.txt",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-dependency-inventory.json",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-receipt.json",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore.log",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-build-remediated.log",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-remediated.xml",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-run-receipt.json",
        "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-semantic-results.json",
        "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.json",
        "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.sha256",
        "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json",
        "docs/release-evidence/preservation-traceability-manifest-v3-rc2.md",
        "docs/release-evidence/preservation-traceability-manifest-v3-rc2.schema.json",
        "docs/release-evidence/preservation-traceability-manifest-v3-rc2.sha256",
    ];

    private static readonly string[] UxAcceptanceSections =
    [
        "Design System Acceptance Criteria",
        "2.3 Success Criteria",
        "Safety Acceptance Criteria",
        "Responsive Acceptance Criteria",
    ];

    [Fact]
    public void V2SchemaShouldBeSeparateClosedAndGoverned()
    {
        using JsonDocument schema = LoadJson(SchemaPath);
        JsonElement root = schema.RootElement;
        root.GetProperty("$schema").GetString().ShouldBe("https://json-schema.org/draft/2020-12/schema");
        root.GetProperty("$id").GetString().ShouldBe(Path.GetFileName(SchemaPath));
        root.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        root.GetProperty("properties").GetProperty("manifestVersion").GetProperty("const").GetString().ShouldBe("3.0.0-rc.2");
        root.GetProperty("properties").GetProperty("approval").GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        root.GetProperty("properties").GetProperty("lineage").GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        ComputeFileSha256(FullPath(Rc1Path)).ShouldBe(Rc1Sha256);
        ComputeFileSha256(FullPath(Rc1ApprovalPath)).ShouldBe(Rc1ApprovalSha256);
        string generator = File.ReadAllText(FullPath("_bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py"), Encoding.UTF8);
        generator.ShouldNotContain("--bootstrap");
        generator.ShouldContain("--require-green");
        generator.ShouldContain("--run-conformance");
        generator.ShouldContain("--run-build");
        generator.ShouldContain("validated_receipt");
        generator.ShouldContain("conformance-semantic-results.json");
        JsonElement overlaySchema = root.GetProperty("properties").GetProperty("sourceBinding").GetProperty("properties").GetProperty("ownerReviewOverlay");
        overlaySchema.GetProperty("properties").TryGetProperty("committed", out _).ShouldBeFalse();
        overlaySchema.GetProperty("properties").GetProperty("candidateKind").GetProperty("const").GetString().ShouldBe(CandidateKind);
        JsonElement commandSummarySchema = root.GetProperty("$defs").GetProperty("commandBinding").GetProperty("properties").GetProperty("summary");
        commandSummarySchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        commandSummarySchema.GetProperty("required").EnumerateArray().Select(row => row.GetString()).ShouldBe(
            ["total", "passed", "failed", "skipped", "notRun", "testFramework", "runtime", "targetFramework"]);
    }

    [Fact]
    public void RequirementAndUxDenominatorsShouldMatchIndependentExtraction()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        IReadOnlyDictionary<string, string> expected = ExtractRequirementAndUxSourceHashes();
        JsonElement[] actualRows = manifest.RootElement.GetProperty("obligations").EnumerateArray()
            .Where(row => IsRequirementOrUx(row.GetProperty("kind").GetString()))
            .ToArray();
        actualRows.Length.ShouldBe(expected.Count);
        actualRows.Select(row => row.GetProperty("id").GetString()!).ShouldBe(expected.Keys, ignoreOrder: true);
        foreach (JsonElement row in actualRows)
        {
            string id = row.GetProperty("id").GetString()!;
            row.GetProperty("source").GetProperty("textSha256").GetString().ShouldBe(expected[id], id);
        }

        actualRows.Count(row => row.GetProperty("kind").GetString() == "initiative-fr").ShouldBe(20);
        actualRows.Count(row => row.GetProperty("kind").GetString() == "feature-fr").ShouldBe(104);
        actualRows.Count(row => row.GetProperty("kind").GetString() == "feature-nfr").ShouldBe(77);
        actualRows.Count(row => row.GetProperty("kind").GetString() == "ux-decision").ShouldBe(52);
        actualRows.Count(row => row.GetProperty("kind").GetString() == "ux-acceptance").ShouldBe(52);
    }

    [Fact]
    public void PublicSurfacesAndConformanceAssertionsShouldHaveZeroGap()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        JsonElement[] rows = manifest.RootElement.GetProperty("obligations").EnumerateArray().ToArray();
        HashSet<string> expectedContracts = typeof(ConformanceManifestV1).Assembly.GetExportedTypes()
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("Hexalith.Conversations.Contracts", StringComparison.Ordinal))
            .Select(type => $"CONTRACT-{type.Namespace}.{type.Name}").ToHashSet(StringComparer.Ordinal);
        RowsOfKind(rows, "public-contract").ShouldBe(expectedContracts, ignoreOrder: true);
        HashSet<string> expectedClients = typeof(IConversationClient).Assembly.GetExportedTypes()
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("Hexalith.Conversations.Client", StringComparison.Ordinal))
            .Select(type => $"CLIENT-{type.Name.Split('`')[0]}").ToHashSet(StringComparer.Ordinal);
        RowsOfKind(rows, "public-client").ShouldBe(expectedClients, ignoreOrder: true);
        HashSet<string> expectedAssertions = typeof(PreservationTraceabilityManifestValidationTest).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<FactAttribute>() is not null || method.GetCustomAttribute<TheoryAttribute>() is not null)
            .Select(method => $"ASSERT-{method.DeclaringType!.Name}.{method.Name}").ToHashSet(StringComparer.Ordinal);
        RowsOfKind(rows, "conformance-assertion").ShouldBe(expectedAssertions, ignoreOrder: true);
        expectedAssertions.Count.ShouldBe(446);
    }

    [Fact]
    public void BindingsClosuresAndFrozenV1BytesShouldValidateIndependently()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        JsonElement root = manifest.RootElement;
        JsonElement overlay = root.GetProperty("sourceBinding").GetProperty("ownerReviewOverlay");
        string[] boundSourcePaths = overlay.GetProperty("bindings").EnumerateArray().Select(row => row.GetProperty("path").GetString()!).ToArray();
        boundSourcePaths.ShouldBe(SourceInputPaths);
        overlay.GetProperty("sourceInputPathInventorySha256").GetString().ShouldBe(ComputePathInventorySha256(SourceInputPaths));
        overlay.GetProperty("assessmentWorkflowRecords").EnumerateArray().Select(row => row.GetString()!).ShouldBe(AssessmentWorkflowRecordPaths);
        AssessmentWorkflowRecordPaths.ShouldAllBe(path => IsRegularBlobAtRevision(path, Rc2PublicationCommit));
        overlay.GetProperty("assessmentWorkflowRecordPathInventorySha256").GetString().ShouldBe(ComputePathInventorySha256(AssessmentWorkflowRecordPaths));
        overlay.GetProperty("assessmentRecordsExcludedFromCandidateDigest").GetBoolean().ShouldBeTrue();
        overlay.GetProperty("candidateDigestAlgorithm").GetString().ShouldBe(CandidateDigestAlgorithm);
        overlay.GetProperty("candidateKind").GetString().ShouldBe(CandidateKind);
        string manifestBaseCommit = overlay.GetProperty("baseCommit").GetString()!;
        manifestBaseCommit.ShouldBe(BaseCommit);
        string manifestRevision = GitText("log", "--diff-filter=A", "--format=%H", "--reverse", "--", ManifestPath)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Single();
        manifestRevision.ShouldBe(Rc2PublicationCommit);
        foreach (JsonElement sourceBinding in overlay.GetProperty("bindings").EnumerateArray())
        {
            ValidateBindingAtRevision(sourceBinding, manifestRevision);
        }

        string candidateDigest = ComputeCandidateDigest(overlay.GetProperty("bindings"));
        overlay.GetProperty("candidateDigest").GetString().ShouldBe(candidateDigest);
        overlay.GetProperty("generatedOutputs").EnumerateArray().Select(row => row.GetString()!).ShouldBe(GeneratedOutputPaths);
        overlay.GetProperty("generatedOutputPathInventorySha256").GetString().ShouldBe(ComputePathInventorySha256(GeneratedOutputPaths));
        string[] publicationOutputs = GeneratedOutputPaths
            .Except([BuildLogPath, RestoreLogPath], StringComparer.Ordinal)
            .ToArray();
        publicationOutputs.ShouldAllBe(path => IsRegularBlobAtRevision(path, Rc2PublicationCommit));
        IsRegularBlobAtRevision(DetachedIndexPath, Rc2PublicationCommit).ShouldBeTrue();
        IsRegularBlobAtRevision(DetachedDigestPath, Rc2PublicationCommit).ShouldBeTrue();

        GitText("rev-parse", $"{BaseCommit}^{{commit}}").ShouldBe(BaseCommit);
        GitText("rev-parse", $"{Rc2PublicationCommit}^{{commit}}").ShouldBe(Rc2PublicationCommit);
        GitText("rev-parse", $"{Rc2PublicationCommit}^").ShouldBe(BaseCommit);
        HashSet<string> changedPaths = GitPaths("diff", "--name-only", BaseCommit, Rc2PublicationCommit, "--");
        HashSet<string> expectedPaths = SourceInputPaths
            .Concat(AssessmentWorkflowRecordPaths)
            .Concat(publicationOutputs)
            .ToHashSet(StringComparer.Ordinal);
        changedPaths.Count.ShouldBe(36);
        changedPaths.Except(expectedPaths).ShouldBeEmpty("unexpected rc.2 publication paths must fail closed");
        expectedPaths.Except(changedPaths).ShouldBeEmpty("missing rc.2 publication paths must fail closed");

        foreach (JsonElement binding in root.GetProperty("artifactBindings").EnumerateArray())
        {
            string path = binding.GetProperty("path").GetString()!;
            ValidateBindingAtRevision(binding, HistoricalRevisionFor(path));
        }

        root.GetProperty("artifactBindings").EnumerateArray()
            .Single(row => row.GetProperty("role").GetString() == "rc2-remediation-semantic-conformance-result")
            .GetProperty("path").GetString().ShouldBe(SemanticResultsPath);
        root.GetProperty("artifactBindings").EnumerateArray()
            .Single(row => row.GetProperty("role").GetString() == "rc2-candidate-restore-receipt")
            .GetProperty("path").GetString().ShouldBe("_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-receipt.json");
        root.GetProperty("artifactBindings").EnumerateArray()
            .Single(row => row.GetProperty("role").GetString() == "rc2-candidate-build-receipt")
            .GetProperty("path").GetString().ShouldBe(BuildReceiptPath);
        root.GetProperty("artifactBindings").EnumerateArray()
            .Single(row => row.GetProperty("role").GetString() == "non-approval-historical-binding-resolution")
            .GetProperty("path").GetString().ShouldBe("docs/release-evidence/projection-read-store-population-proof-v2-binding-resolution.json");
        root.GetProperty("artifactBindings").EnumerateArray()
            .ShouldNotContain(row => row.GetProperty("path").GetString()!.EndsWith("conformance-remediated.xml", StringComparison.Ordinal));
        root.GetProperty("artifactBindings").EnumerateArray()
            .ShouldNotContain(row => row.GetProperty("path").GetString()!.EndsWith("conformance-run-receipt.json", StringComparison.Ordinal));

        JsonElement[] obligations = root.GetProperty("obligations").EnumerateArray().ToArray();
        obligations.Length.ShouldBe(969);
        obligations.Select(row => row.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count().ShouldBe(969);
        obligations.ShouldAllBe(row => !row.GetProperty("orphan").GetBoolean() && row.GetProperty("closure").ValueKind == JsonValueKind.Object);
        root.GetProperty("governedDispositions").GetArrayLength().ShouldBe(277);
        root.GetProperty("zeroOrphanProof").GetProperty("closureCount").GetInt32().ShouldBe(969);
        root.GetProperty("zeroOrphanProof").GetProperty("orphanIds").GetArrayLength().ShouldBe(0);
        JsonElement lineage = root.GetProperty("lineage");
        lineage.GetProperty("predecessorVersion").GetString().ShouldBe("3.0.0-rc.1");
        lineage.GetProperty("predecessor").GetProperty("sha256").GetString().ShouldBe(Rc1Sha256);
        lineage.GetProperty("predecessorApproval").GetProperty("sha256").GetString().ShouldBe(Rc1ApprovalSha256);
        ComputeBlobSha256(Rc2PublicationCommit, Rc1Path).ShouldBe(Rc1Sha256);
        ComputeBlobSha256(Rc2PublicationCommit, Rc1ApprovalPath).ShouldBe(Rc1ApprovalSha256);
        string[] digestLines = ReadTextAtRevision(DigestPath, Rc2PublicationCommit)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        digestLines.Length.ShouldBe(3);
        foreach (string line in digestLines)
        {
            string[] parts = line.Split("  ", 2, StringSplitOptions.None);
            parts.Length.ShouldBe(2);
            ComputeBlobSha256(Rc2PublicationCommit, $"docs/release-evidence/{parts[1]}").ShouldBe(parts[0], parts[1]);
        }
    }

    [Fact]
    public void CurrentControlsAndTierPrerequisiteShouldStayTruthful()
    {
        using JsonDocument manifest = LoadJsonAtRevision(ManifestPath, Rc2PublicationCommit);
        using JsonDocument rc1 = LoadJsonAtRevision(Rc1Path, Rc2PublicationCommit);
        using JsonDocument runReceipt = LoadJsonAtRevision(RunReceiptPath, Rc2PublicationCommit);
        using JsonDocument semanticResults = LoadJsonAtRevision(SemanticResultsPath, Rc2PublicationCommit);
        using JsonDocument restoreReceipt = LoadJsonAtRevision(RestoreReceiptPath, Rc2PublicationCommit);
        using JsonDocument buildReceipt = LoadJsonAtRevision(BuildReceiptPath, Rc2PublicationCommit);
        JsonElement root = manifest.RootElement;
        root.GetProperty("status").GetString().ShouldBe("pending-owner-approval");
        root.GetProperty("approval").GetProperty("state").GetString().ShouldBe("pending");
        root.GetProperty("categoryMappings").GetArrayLength().ShouldBe(7);
        root.GetProperty("obligationSummary").GetProperty("releaseActivatedLegacyRequirementCount").GetInt32().ShouldBe(0);
        root.GetProperty("obligations").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "FR-16")
            .GetProperty("releaseActivationState").GetString().ShouldBe("deferred-not-active");
        JsonElement gates = root.GetProperty("gateStates");
        foreach ((string gate, string state) in new[] { ("FR-20", "PENDING"), ("SM-C1", "PENDING"), ("SM-C2", "FAILED"), ("OQ-1", "BLOCKED"), ("implementationHold", "ACTIVE") })
        {
            gates.GetProperty(gate).GetProperty("state").GetString().ShouldBe(state);
        }

        gates.GetProperty("implementationHold").GetProperty("releaseAllowed").GetBoolean().ShouldBeFalse();
        JsonElement current = root.GetProperty("testDenominator").GetProperty("currentCandidate");
        JsonElement summary = semanticResults.RootElement.GetProperty("summary");
        current.GetProperty("testCount").GetInt32().ShouldBe(summary.GetProperty("total").GetInt32());
        current.GetProperty("passed").GetInt32().ShouldBe(summary.GetProperty("passed").GetInt32());
        current.GetProperty("failed").GetInt32().ShouldBe(summary.GetProperty("failed").GetInt32());
        current.GetProperty("skipped").GetInt32().ShouldBe(summary.GetProperty("skipped").GetInt32());
        current.GetProperty("notRun").GetInt32().ShouldBe(summary.GetProperty("notRun").GetInt32());
        current.GetProperty("pendingAdditionCount").GetInt32().ShouldBe(89);
        current.GetProperty("testIds").GetRawText().ShouldBe(rc1.RootElement.GetProperty("testDenominator").GetProperty("currentCandidate").GetProperty("testIds").GetRawText());
        semanticResults.RootElement.GetProperty("testResults").EnumerateArray().Select(row => row.GetProperty("id").GetString())
            .ShouldBe(current.GetProperty("testIds").EnumerateArray().Select(row => row.GetString()));
        current.GetProperty("failedTestIds").EnumerateArray().Select(row => row.GetString())
            .ShouldBe(semanticResults.RootElement.GetProperty("testResults").EnumerateArray()
                .Where(row => row.GetProperty("result").GetString() == "Fail").Select(row => row.GetProperty("id").GetString()));

        string candidateDigest = root.GetProperty("sourceBinding").GetProperty("ownerReviewOverlay").GetProperty("candidateDigest").GetString()!;
        semanticResults.RootElement.GetProperty("candidateDigest").GetString().ShouldBe(candidateDigest);
        semanticResults.RootElement.GetProperty("candidateDigestAlgorithm").GetString().ShouldBe(CandidateDigestAlgorithm);
        current.GetProperty("sourceState").GetString().ShouldBe($"{BaseCommit}+content-addressed-overlay-sha256:{candidateDigest}");
        JsonElement build = root.GetProperty("buildBindings").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "owner-review-overlay-conformance-build");
        JsonElement semanticAssembly = semanticResults.RootElement.GetProperty("assembly");
        ValidateBuildReceipt(buildReceipt.RootElement, candidateDigest, restoreReceipt.RootElement, semanticAssembly, historical: true).ShouldBeEmpty();
        ValidateRunReceipt(runReceipt.RootElement, candidateDigest, semanticResults.RootElement, historical: true).ShouldBeEmpty();
        build.GetProperty("conformanceAssemblySha256").GetString().ShouldBe(semanticAssembly.GetProperty("sha256").GetString());
        build.GetProperty("assemblySourceRevisionId").GetString().ShouldBe(candidateDigest);
        semanticAssembly.GetProperty("sourceRevisionId").GetString().ShouldBe(candidateDigest);
        restoreReceipt.RootElement.GetProperty("candidateDigest").GetString().ShouldBe(candidateDigest);
        JsonElement toolchain = restoreReceipt.RootElement.GetProperty("toolchain");
        JsonElement restore = restoreReceipt.RootElement.GetProperty("restore");
        ValidateBindingAtRevision(toolchain.GetProperty("capture"), HistoricalRevisionFor(ToolchainPath));
        ValidateBindingAtRevision(toolchain.GetProperty("globalJson"), HistoricalRevisionFor(toolchain.GetProperty("globalJson").GetProperty("path").GetString()!));
        ValidateBindingAtRevision(restore.GetProperty("log"), HistoricalRevisionFor(RestoreLogPath));
        ValidateBindingAtRevision(restore.GetProperty("dependencyInventory"), HistoricalRevisionFor(restore.GetProperty("dependencyInventory").GetProperty("path").GetString()!));
        string toolchainText = ReadTextAtRevision(ToolchainPath, Rc2PublicationCommit);
        string sdkVersion = Regex.Match(toolchainText, "(?m)^ Version:\\s+([^\\s]+)$").Groups[1].Value;
        string msbuildVersion = Regex.Match(toolchainText, "(?m)^ MSBuild version:\\s+([^\\s]+)$").Groups[1].Value;
        toolchain.GetProperty("sdkVersion").GetString().ShouldBe(sdkVersion);
        toolchain.GetProperty("msbuildVersion").GetString().ShouldBe(msbuildVersion);
        string sdkCommit = Regex.Match(toolchainText, "(?m)^\\.NET SDK:\\s*\\n Version:\\s+[^\\s]+\\s*\\n Commit:\\s+([^\\s]+)$").Groups[1].Value;
        toolchain.GetProperty("sdkCommit").GetString().ShouldBe(sdkCommit);
        build.GetProperty("dotnetSdk").GetString().ShouldBe(sdkVersion);
        build.GetProperty("sdkCommit").GetString().ShouldBe(sdkCommit);
        build.GetProperty("msbuild").GetString().ShouldBe(msbuildVersion);
        ValidateBindingAtRevision(build.GetProperty("toolchainCapture"), Rc2PublicationCommit);
        build.GetProperty("toolchainCapture").GetProperty("path").GetString().ShouldBe(ToolchainPath);
        build.GetProperty("toolchainSdkVersion").GetString().ShouldBe(sdkVersion);
        build.GetProperty("toolchainMsbuildVersion").GetString().ShouldBe(msbuildVersion);
        build.GetProperty("restoreReceiptSha256").GetString().ShouldBe(ComputeBlobSha256(Rc2PublicationCommit, RestoreReceiptPath));
        build.GetProperty("dependencyGraphSha256").GetString().ShouldBe(restore.GetProperty("dependencyInventory").GetProperty("sha256").GetString());
        build.GetProperty("buildReceiptSha256").GetString().ShouldBe(ComputeBlobSha256(Rc2PublicationCommit, BuildReceiptPath));
        string currentCandidate = GitText("rev-parse", "--verify", "HEAD^{commit}");
        string informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        ValidateCurrentCandidateIdentity(informationalVersion, currentCandidate, v24IsAncestor: true).ShouldBeEmpty();
        GitExitCode("merge-base", "--is-ancestor", V24PublicationCommit, currentCandidate).ShouldBe(0);
        ParseBuildLogAtRevision(BuildLogPath, Rc2LogProvenanceCommit).ShouldBe((
            build.GetProperty("result").GetString()!,
            build.GetProperty("warnings").GetInt32(),
            build.GetProperty("errors").GetInt32()));

        root.GetProperty("obligations").EnumerateArray().Select(CanonicalObligationProjection)
            .ShouldBe(rc1.RootElement.GetProperty("obligations").EnumerateArray().Select(CanonicalObligationProjection));
        JsonNode.Parse(root.GetProperty("governedDispositions").GetRawText())!.ToJsonString()
            .ShouldBe(JsonNode.Parse(rc1.RootElement.GetProperty("governedDispositions").GetRawText())!.ToJsonString());
        JsonNode.Parse(root.GetProperty("categoryMappings").GetRawText())!.ToJsonString()
            .ShouldBe(JsonNode.Parse(rc1.RootElement.GetProperty("categoryMappings").GetRawText())!.ToJsonString());
        JsonElement proof = root.GetProperty("testDenominator").GetProperty("preservationProof");
        proof.GetProperty("originalV1RetainedCount").GetInt32().ShouldBe(214);
        proof.GetProperty("approvedCumulativeRetainedCount").GetInt32().ShouldBe(384);
    }

    [Fact]
    public void MarkdownProjectionShouldBeByteExact()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        string expected = RenderMarkdown(manifest.RootElement, ComputeFileSha256(FullPath(ManifestPath)));
        File.ReadAllText(FullPath(MarkdownPath), Encoding.UTF8).Replace("\r\n", "\n", StringComparison.Ordinal).ShouldBe(expected);
    }

    [Fact]
    public void FaultInjectedCandidatesShouldFailWithStableDiagnostics()
    {
        JsonObject canonical = JsonNode.Parse(File.ReadAllText(FullPath(ManifestPath), Encoding.UTF8))!.AsObject();
        AssertMutation(canonical, candidate => candidate["obligations"]!.AsArray().RemoveAt(0), "MISSING_IDENTITY");
        AssertMutation(canonical, candidate => candidate["obligations"]!.AsArray().Add(candidate["obligations"]![0]!.DeepClone()), "DUPLICATE_IDENTITY");
        AssertMutation(canonical, candidate => candidate["sourceBinding"]!["ownerReviewOverlay"]!["bindings"]![0]!["sha256"] = new string('0', 64), "SOURCE_HASH_MISMATCH");
        AssertMutation(canonical, candidate => candidate["sourceBinding"]!["ownerReviewOverlay"]!["bindings"]![0]!["mode"] = "100755", "SOURCE_MODE_MISMATCH");
        AssertMutation(canonical, candidate => candidate["sourceBinding"]!["ownerReviewOverlay"]!["bindings"]![0]!["path"] = "../outside-repository", "UNSAFE_REPOSITORY_PATH");
        AssertMutation(canonical, candidate => candidate["lineage"]!["predecessor"]!["sha256"] = new string('0', 64), "RC1_LINEAGE_MISMATCH");
        AssertMutation(canonical, candidate => candidate["obligations"]!.AsArray().First(row => row!["kind"]!.GetValue<string>() == "feature-fr")!["releaseActivationState"] = "active-initiative-scope", "LEGACY_ACTIVATION");
        AssertMutation(canonical, candidate => candidate["gateStates"]!["SM-C2"]!["state"] = "PASSED", "GATE_STATE_RELAXED");
        AssertMutation(canonical, candidate => candidate["testDenominator"]!["currentCandidate"]!["testIds"]!.AsArray().RemoveAt(0), "TEST_IDENTITY_DRIFT");
        AssertMutation(canonical, candidate => candidate["obligations"]!.AsArray().First(row => row!["controlOwner"] is not null)!["controlOwner"] = "mutated-owner", "OBLIGATION_OWNER_DRIFT");
        AssertMutation(canonical, candidate => candidate["obligations"]!.AsArray().First(row => row!["tier"] is not null)!["tier"] = "mutated-tier", "OBLIGATION_TIER_DRIFT");
        AssertMutation(canonical, candidate => candidate["obligations"]!.AsArray().First(row => row!["closure"]!["dispositionId"] is not null)!["closure"]!["dispositionId"] = "DISP-MUTATED", "CLOSURE_TARGET_DRIFT");
        AssertMutation(canonical, candidate => candidate["governedDispositions"]![0]!["obligationId"] = "FR-MUTATED", "DISPOSITION_LINKAGE_DRIFT");
        AssertMutation(canonical, candidate => candidate["categoryMappings"]![0]!["requirementIds"]![0] = "FR-MUTATED", "CATEGORY_REMAP");
        AssertMutation(
            canonical,
            candidate =>
            {
                JsonObject command = candidate["commandBindings"]![5]!.AsObject();
                command["result"] = "pass";
                command["summary"]!["passed"] = 0;
                command["summary"]!["failed"] = 0;
                command["summary"]!["skipped"] = 0;
                command["summary"]!["notRun"] = 473;
                command["summary"]!["invented"] = true;
            },
            "COMMAND_SUMMARY_INVALID");

        JsonObject receipt = JsonNode.Parse(File.ReadAllText(FullPath(RunReceiptPath), Encoding.UTF8))!.AsObject();
        AssertReceiptMutation(receipt, candidate => candidate["candidateDigest"] = new string('0', 64), "RUN_RECEIPT_CANDIDATE_MISMATCH");
        AssertReceiptMutation(receipt, candidate => candidate["preRunManifest"]!["sha256"] = "invalid", "RUN_RECEIPT_MANIFEST_MISMATCH");
        AssertReceiptMutation(receipt, candidate => candidate["assembly"]!["sourceRevisionId"] = new string('0', 64), "RUN_RECEIPT_ASSEMBLY_MISMATCH");
        AssertReceiptMutation(receipt, candidate => candidate["runner"]!["command"]![0] = "mutated-dotnet", "RUN_RECEIPT_RUNNER_MISMATCH");
        AssertReceiptMutation(
            receipt,
            candidate => candidate["runner"]!["exitCode"] = candidate["runner"]!["exitCode"]!.GetValue<int>() == 0 ? 1 : 0,
            "RUN_RECEIPT_RUNNER_MISMATCH");
        AssertReceiptMutation(
            receipt,
            candidate => candidate["runner"]!["result"] = candidate["runner"]!["result"]!.GetValue<string>() == "pass" ? "fail" : "pass",
            "RUN_RECEIPT_RUNNER_MISMATCH");
        AssertReceiptMutation(
            receipt,
            candidate => candidate["testResults"]![0]!["result"] = candidate["testResults"]![0]!["result"]!.GetValue<string>() == "Pass" ? "Fail" : "Pass",
            "RUN_RECEIPT_COUNTER_MISMATCH");
        AssertReceiptMutation(
            receipt,
            candidate => candidate["summary"]!["passed"] = candidate["summary"]!["passed"]!.GetValue<int>() + 1,
            "RUN_RECEIPT_COUNTER_MISMATCH");
        AssertReceiptMutation(receipt, candidate => candidate["xml"]!["sha256"] = new string('0', 64), "RUN_RECEIPT_XML_MISMATCH");
        AssertReceiptMutation(receipt, candidate => candidate["assembly"]!["sha256"] = new string('0', 64), "RUN_RECEIPT_ASSEMBLY_MISMATCH");
        JsonObject buildReceipt = JsonNode.Parse(File.ReadAllText(FullPath(BuildReceiptPath), Encoding.UTF8))!.AsObject();
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["candidateDigest"] = new string('0', 64), "BUILD_RECEIPT_CANDIDATE_MISMATCH");
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["sequence"]![0] = "executed-exact-build-command", "BUILD_RECEIPT_ORDER_MISMATCH");
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["preBuild"]!["restoreReceipt"]!["sha256"] = new string('0', 64), "BUILD_RECEIPT_PREBUILD_MISMATCH");
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["preBuild"]!["toolchainCapture"]!["sha256"] = new string('0', 64), "BUILD_RECEIPT_PREBUILD_MISMATCH");
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["build"]!["command"]![0] = "mutated-dotnet", "BUILD_RECEIPT_COMMAND_MISMATCH");
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["build"]!["exitCode"] = 1, "BUILD_RECEIPT_RESULT_MISMATCH");
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["build"]!["log"]!["sha256"] = new string('0', 64), "BUILD_RECEIPT_LOG_MISMATCH");
        AssertBuildReceiptMutation(buildReceipt, candidate => candidate["assembly"]!["sha256"] = new string('0', 64), "BUILD_RECEIPT_ASSEMBLY_MISMATCH");
        string currentCandidate = GitText("rev-parse", "--verify", "HEAD^{commit}");
        ValidateCurrentCandidateIdentity("1.0.0", currentCandidate, v24IsAncestor: true)
            .ShouldContain("CURRENT_CANDIDATE_REVISION_COUNT_MISMATCH");
        ValidateCurrentCandidateIdentity($"1.0.0+{new string('0', 40)}", currentCandidate, v24IsAncestor: true)
            .ShouldContain("CURRENT_CANDIDATE_IDENTITY_MISMATCH");
        ValidateCurrentCandidateIdentity($"1.0.0+{currentCandidate}", currentCandidate, v24IsAncestor: false)
            .ShouldContain("CURRENT_CANDIDATE_V24_ANCESTRY_MISSING");
        ValidateRealFilesystemFaults();
    }

    private static void ValidateRealFilesystemFaults()
    {
        string fixtureRoot = Path.Combine(Path.GetTempPath(), $"rc2-evidence-fault-{Guid.NewGuid():N}");
        Directory.CreateDirectory(fixtureRoot);
        try
        {
            RunProcessInDirectory(fixtureRoot, "git", ["init", "--quiet"]).ExitCode.ShouldBe(0);
            RunProcessInDirectory(fixtureRoot, "git", ["config", "user.name", "Fixture"]).ExitCode.ShouldBe(0);
            RunProcessInDirectory(fixtureRoot, "git", ["config", "user.email", "fixture@example.invalid"]).ExitCode.ShouldBe(0);
            string regular = Path.Combine(fixtureRoot, "regular.txt");
            File.WriteAllText(regular, "unchanged", Encoding.UTF8);
            RunProcessInDirectory(fixtureRoot, "git", ["add", "regular.txt"]).ExitCode.ShouldBe(0);
            RunProcessInDirectory(fixtureRoot, "git", ["commit", "--quiet", "-m", "fixture: regular"]).ExitCode.ShouldBe(0);
            string commit = RunProcessInDirectory(fixtureRoot, "git", ["rev-parse", "HEAD"]).Output.Trim();
            TryValidateRepositoryRegularFile("regular.txt", out string regularDiagnostic, fixtureRoot).ShouldBeTrue(regularDiagnostic);

            string symlink = Path.Combine(fixtureRoot, "symlink.txt");
            File.CreateSymbolicLink(symlink, regular);
            TryValidateRepositoryRegularFile("symlink.txt", out string symlinkDiagnostic, fixtureRoot).ShouldBeFalse();
            symlinkDiagnostic.ShouldContain("symlink");

            string directory = Path.Combine(fixtureRoot, "directory");
            Directory.CreateDirectory(directory);
            TryValidateRepositoryRegularFile("directory", out string directoryDiagnostic, fixtureRoot).ShouldBeFalse();
            directoryDiagnostic.ShouldContain("regular file");

            if (OperatingSystem.IsLinux())
            {
                string fifo = Path.Combine(fixtureRoot, "fifo");
                (int exitCode, _, string error) = RunProcessInDirectory(fixtureRoot, "mkfifo", [fifo]);
                exitCode.ShouldBe(0, error);
                TryValidateRepositoryRegularFile("fifo", out string fifoDiagnostic, fixtureRoot).ShouldBeFalse();
                fifoDiagnostic.ShouldContain("regular file");
            }

            File.ReadAllText(regular, Encoding.UTF8).ShouldBe("unchanged");
            if (!OperatingSystem.IsWindows())
            {
                string regularPath = regular;
                byte[] regularBytes = File.ReadAllBytes(regularPath);
                UnixFileMode executable = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                UnixFileMode regularMode = File.GetUnixFileMode(regularPath) & ~executable;
                File.SetUnixFileMode(regularPath, regularMode);
                string regularStage = RunProcessInDirectory(fixtureRoot, "git", ["ls-files", "--stage", "--", "regular.txt"]).Output;
                TryValidateCandidateSourceMode("regular.txt", SourceInputMode, commit, out string initialModeDiagnostic, fixtureRoot)
                    .ShouldBeTrue(initialModeDiagnostic);
                try
                {
                    RunProcessInDirectory(fixtureRoot, "git", ["update-index", "--chmod=+x", "--", "regular.txt"]).ExitCode.ShouldBe(0);
                    GetWorktreeGitMode("regular.txt", fixtureRoot).ShouldBe(SourceInputMode);
                    TryValidateCandidateSourceMode("regular.txt", SourceInputMode, commit, out string indexOnlyModeDiagnostic, fixtureRoot)
                        .ShouldBeFalse();
                    indexOnlyModeDiagnostic.ShouldContain("stage/declared/worktree mode mismatch");
                    indexOnlyModeDiagnostic.ShouldContain("stage=100755");
                }
                finally
                {
                    RunProcessInDirectory(fixtureRoot, "git", ["update-index", "--chmod=-x", "--", "regular.txt"]).ExitCode.ShouldBe(0);
                    File.WriteAllBytes(regularPath, regularBytes);
                    File.SetUnixFileMode(regularPath, regularMode);
                }

                RunProcessInDirectory(fixtureRoot, "git", ["ls-files", "--stage", "--", "regular.txt"]).Output.ShouldBe(regularStage);
                File.ReadAllBytes(regularPath).ShouldBe(regularBytes);
                GetWorktreeGitMode("regular.txt", fixtureRoot).ShouldBe(SourceInputMode);

                try
                {
                    RunProcessInDirectory(fixtureRoot, "git", ["rm", "--cached", "--", "regular.txt"]).ExitCode.ShouldBe(0);
                    File.WriteAllBytes(regularPath, regularBytes);
                    File.SetUnixFileMode(regularPath, regularMode);
                    TryValidateCandidateSourceMode("regular.txt", SourceInputMode, commit, out string removedStageDiagnostic, fixtureRoot)
                        .ShouldBeFalse();
                    removedStageDiagnostic.ShouldContain("index entry missing for baseline-present path");
                }
                finally
                {
                    File.WriteAllBytes(regularPath, regularBytes);
                    File.SetUnixFileMode(regularPath, regularMode);
                    RunProcessInDirectory(fixtureRoot, "git", ["add", "--", "regular.txt"]).ExitCode.ShouldBe(0);
                }

                RunProcessInDirectory(fixtureRoot, "git", ["ls-files", "--stage", "--", "regular.txt"]).Output.ShouldBe(regularStage);
                File.ReadAllBytes(regularPath).ShouldBe(regularBytes);
                GetWorktreeGitMode("regular.txt", fixtureRoot).ShouldBe(SourceInputMode);

                string stagedLink = Path.Combine(fixtureRoot, "staged-link");
                File.CreateSymbolicLink(stagedLink, Path.Combine(fixtureRoot, "regular.txt"));
                RunProcessInDirectory(fixtureRoot, "git", ["add", "staged-link"]).ExitCode.ShouldBe(0);
                File.Delete(stagedLink);
                File.WriteAllText(stagedLink, "regular worktree bytes", Encoding.UTF8);
                TryValidateRepositoryRegularFile("staged-link", out string symlinkModeDiagnostic, fixtureRoot).ShouldBeFalse();
                symlinkModeDiagnostic.ShouldContain("Git mode");
                symlinkModeDiagnostic.ShouldContain("120000");

                RunProcessInDirectory(fixtureRoot, "git", ["update-index", "--add", "--cacheinfo", $"160000,{commit},staged-gitlink"]).ExitCode.ShouldBe(0);
                File.WriteAllText(Path.Combine(fixtureRoot, "staged-gitlink"), "regular worktree bytes", Encoding.UTF8);
                TryValidateRepositoryRegularFile("staged-gitlink", out string gitlinkModeDiagnostic, fixtureRoot).ShouldBeFalse();
                gitlinkModeDiagnostic.ShouldContain("Git mode");
                gitlinkModeDiagnostic.ShouldContain("160000");
            }
        }
        finally
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }

        Directory.Exists(fixtureRoot).ShouldBeFalse("fault fixtures must be isolated and cleaned up");
    }

    private static void AssertMutation(JsonObject canonical, Action<JsonObject> mutate, string diagnostic)
    {
        JsonObject candidate = canonical.DeepClone().AsObject();
        mutate(candidate);
        ValidateCandidate(candidate).ShouldContain(diagnostic);
    }

    private static void AssertReceiptMutation(JsonObject canonical, Action<JsonObject> mutate, string diagnostic)
    {
        JsonObject candidate = canonical.DeepClone().AsObject();
        mutate(candidate);
        using JsonDocument manifest = LoadJson(ManifestPath);
        using JsonDocument semantic = LoadJson(SemanticResultsPath);
        string digest = manifest.RootElement.GetProperty("sourceBinding").GetProperty("ownerReviewOverlay").GetProperty("candidateDigest").GetString()!;
        using JsonDocument candidateDocument = JsonDocument.Parse(candidate.ToJsonString());
        ValidateRunReceipt(candidateDocument.RootElement, digest, semantic.RootElement, historical: true).ShouldContain(diagnostic);
    }

    private static void AssertBuildReceiptMutation(JsonObject canonical, Action<JsonObject> mutate, string diagnostic)
    {
        JsonObject candidate = canonical.DeepClone().AsObject();
        mutate(candidate);
        using JsonDocument manifest = LoadJson(ManifestPath);
        using JsonDocument restore = LoadJson(RestoreReceiptPath);
        using JsonDocument semantic = LoadJson(SemanticResultsPath);
        string digest = manifest.RootElement.GetProperty("sourceBinding").GetProperty("ownerReviewOverlay").GetProperty("candidateDigest").GetString()!;
        using JsonDocument candidateDocument = JsonDocument.Parse(candidate.ToJsonString());
        ValidateBuildReceipt(candidateDocument.RootElement, digest, restore.RootElement, semantic.RootElement.GetProperty("assembly"), historical: true)
            .ShouldContain(diagnostic);
    }

    private static HashSet<string> ValidateCandidate(JsonObject candidate)
    {
        var diagnostics = new HashSet<string>(StringComparer.Ordinal);
        using JsonDocument canonicalDocument = LoadJson(ManifestPath);
        JsonElement canonical = canonicalDocument.RootElement;
        using JsonDocument rc1Document = LoadJson(Rc1Path);
        JsonElement rc1 = rc1Document.RootElement;
        JsonArray obligations = candidate["obligations"]!.AsArray();
        string[] ids = obligations.Select(row => row!["id"]!.GetValue<string>()).ToArray();
        if (ids.Length != 969)
        {
            diagnostics.Add("MISSING_IDENTITY");
        }

        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            diagnostics.Add("DUPLICATE_IDENTITY");
        }

        string candidateBaseCommit = candidate["sourceBinding"]!["ownerReviewOverlay"]!["baseCommit"]!.GetValue<string>();
        foreach (JsonNode? binding in candidate["sourceBinding"]!["ownerReviewOverlay"]!["bindings"]!.AsArray())
        {
            string path = binding!["path"]!.GetValue<string>();
            if (!TryValidateRepositoryRegularFile(path, out _))
            {
                diagnostics.Add("UNSAFE_REPOSITORY_PATH");
                continue;
            }

            if (ComputeFileSha256(FullPath(path)) != binding["sha256"]!.GetValue<string>())
            {
                diagnostics.Add("SOURCE_HASH_MISMATCH");
            }

            string declaredMode = binding["mode"]?.GetValue<string>() ?? "<absent>";
            if (!TryValidateCandidateSourceMode(path, declaredMode, candidateBaseCommit, out _))
            {
                diagnostics.Add("SOURCE_MODE_MISMATCH");
            }
        }

        if (candidate["lineage"]!["predecessor"]!["sha256"]!.GetValue<string>() != Rc1Sha256)
        {
            diagnostics.Add("RC1_LINEAGE_MISMATCH");
        }

        if (obligations.Any(row => row!["kind"]!.GetValue<string>() is "feature-fr" or "feature-nfr" or "ux-decision" or "ux-acceptance"
            && row["releaseActivationState"]!.GetValue<string>() != "pending-not-inferred"))
        {
            diagnostics.Add("LEGACY_ACTIVATION");
        }

        foreach ((string gate, string state) in new[] { ("FR-20", "PENDING"), ("SM-C1", "PENDING"), ("SM-C2", "FAILED"), ("OQ-1", "BLOCKED"), ("implementationHold", "ACTIVE") })
        {
            if (candidate["gateStates"]![gate]!["state"]!.GetValue<string>() != state)
            {
                diagnostics.Add("GATE_STATE_RELAXED");
            }
        }

        if (candidate["testDenominator"]!["currentCandidate"]!["testIds"]!.AsArray().ToJsonString()
            != canonical.GetProperty("testDenominator").GetProperty("currentCandidate").GetProperty("testIds").GetRawText())
        {
            diagnostics.Add("TEST_IDENTITY_DRIFT");
        }

        Dictionary<string, JsonNode> rc1Obligations = rc1.GetProperty("obligations").EnumerateArray()
            .ToDictionary(row => row.GetProperty("id").GetString()!, row => JsonNode.Parse(row.GetRawText())!, StringComparer.Ordinal);
        foreach (JsonNode? obligation in obligations)
        {
            if (obligation is null || !rc1Obligations.TryGetValue(obligation["id"]!.GetValue<string>(), out JsonNode? expected))
            {
                continue;
            }

            if (NodeText(obligation["controlOwner"]) != NodeText(expected["controlOwner"]))
            {
                diagnostics.Add("OBLIGATION_OWNER_DRIFT");
            }

            if (NodeText(obligation["tier"]) != NodeText(expected["tier"]))
            {
                diagnostics.Add("OBLIGATION_TIER_DRIFT");
            }

            if (CanonicalClosureProjection(obligation["closure"]) != CanonicalClosureProjection(expected["closure"]))
            {
                diagnostics.Add("CLOSURE_TARGET_DRIFT");
            }
        }

        Dictionary<string, string> rc1DispositionLinks = rc1.GetProperty("governedDispositions").EnumerateArray()
            .ToDictionary(row => row.GetProperty("dispositionId").GetString()!, row => row.GetProperty("obligationId").GetString()!, StringComparer.Ordinal);
        if (candidate["governedDispositions"]!.AsArray().Any(row => row is null
            || !rc1DispositionLinks.TryGetValue(row["dispositionId"]!.GetValue<string>(), out string? obligationId)
            || obligationId != row["obligationId"]!.GetValue<string>()))
        {
            diagnostics.Add("DISPOSITION_LINKAGE_DRIFT");
        }

        Dictionary<string, string> rc1Mappings = rc1.GetProperty("categoryMappings").EnumerateArray()
            .ToDictionary(row => row.GetProperty("category").GetString()!, row => JsonNode.Parse(row.GetRawText())!.ToJsonString(), StringComparer.Ordinal);
        if (candidate["categoryMappings"]!.AsArray().Any(row => row is null
            || !rc1Mappings.TryGetValue(row["category"]!.GetValue<string>(), out string? expected)
            || expected != row.ToJsonString()))
        {
            diagnostics.Add("CATEGORY_REMAP");
        }

        HashSet<string> allowedSummaryProperties =
        [
            "total", "passed", "failed", "skipped", "notRun", "testFramework", "runtime", "targetFramework", "startedAtUtc", "finishedAtUtc",
        ];
        foreach (JsonNode? commandNode in candidate["commandBindings"]!.AsArray())
        {
            JsonObject command = commandNode!.AsObject();
            if (command["summary"] is not JsonObject summary)
            {
                continue;
            }

            bool exactProperties = summary.Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal).IsSubsetOf(allowedSummaryProperties)
                && new[] { "total", "passed", "failed", "skipped", "notRun", "testFramework", "runtime", "targetFramework" }
                    .All(summary.ContainsKey);
            int total = summary["total"]?.GetValue<int>() ?? -1;
            int passed = summary["passed"]?.GetValue<int>() ?? -1;
            int failed = summary["failed"]?.GetValue<int>() ?? -1;
            int skipped = summary["skipped"]?.GetValue<int>() ?? -1;
            int notRun = summary["notRun"]?.GetValue<int>() ?? -1;
            bool green = passed == total && failed == 0 && skipped == 0 && notRun == 0;
            if (!exactProperties || total != passed + failed + skipped + notRun || (command["result"]!.GetValue<string>() == "pass") != green)
            {
                diagnostics.Add("COMMAND_SUMMARY_INVALID");
            }
        }

        return diagnostics;
    }

    private static HashSet<string> ValidateBuildReceipt(
        JsonElement receipt,
        string candidateDigest,
        JsonElement restoreReceipt,
        JsonElement semanticAssembly,
        bool historical = false)
    {
        var diagnostics = new HashSet<string>(StringComparer.Ordinal);
        if (receipt.GetProperty("schemaVersion").GetString() != "1.0.0"
            || receipt.GetProperty("candidateDigestAlgorithm").GetString() != CandidateDigestAlgorithm
            || receipt.GetProperty("candidateDigest").GetString() != candidateDigest)
        {
            diagnostics.Add("BUILD_RECEIPT_CANDIDATE_MISMATCH");
        }

        string[] expectedSequence =
        [
            "validated-restore-evidence",
            "executed-exact-build-command",
            "validated-post-build-assembly",
        ];
        if (!receipt.GetProperty("sequence").EnumerateArray().Select(row => row.GetString()!).SequenceEqual(expectedSequence))
        {
            diagnostics.Add("BUILD_RECEIPT_ORDER_MISMATCH");
        }

        JsonElement preBuild = receipt.GetProperty("preBuild");
        JsonElement restore = restoreReceipt.GetProperty("restore");
        const string dependencyInventoryPath = "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-dependency-inventory.json";
        bool preBuildMatches = (historical
                ? BindingMatchesAtRevision(preBuild.GetProperty("restoreReceipt"), RestoreReceiptPath, "pre-build-candidate-restore-receipt", Rc2PublicationCommit)
                : BindingMatches(preBuild.GetProperty("restoreReceipt"), RestoreReceiptPath, "pre-build-candidate-restore-receipt"))
            && (historical
                ? BindingMatchesAtRevision(preBuild.GetProperty("toolchainCapture"), ToolchainPath, "pre-build-candidate-toolchain-capture", Rc2PublicationCommit)
                : BindingMatches(preBuild.GetProperty("toolchainCapture"), ToolchainPath, "pre-build-candidate-toolchain-capture"))
            && (historical
                ? BindingMatchesAtRevision(preBuild.GetProperty("dependencyInventory"), dependencyInventoryPath, "pre-build-candidate-restore-dependency-inventory", Rc2PublicationCommit)
                : BindingMatches(preBuild.GetProperty("dependencyInventory"), dependencyInventoryPath, "pre-build-candidate-restore-dependency-inventory"))
            && JsonNode.Parse(preBuild.GetProperty("restoreCommand").GetRawText())!.ToJsonString()
                == JsonNode.Parse(restore.GetProperty("command").GetRawText())!.ToJsonString()
            && preBuild.GetProperty("restoreExitCode").GetInt32() == 0
            && preBuild.GetProperty("restoreResult").GetString() == "pass";
        if (!preBuildMatches)
        {
            diagnostics.Add("BUILD_RECEIPT_PREBUILD_MISMATCH");
        }

        string[] expectedCommand =
        [
            "dotnet",
            "build",
            "tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj",
            "-c",
            "Release",
            "-t:Rebuild",
            "--no-restore",
            "-m:1",
            "-p:NuGetAudit=false",
            "-p:MinVerVersionOverride=1.0.0",
            $"-p:SourceRevisionId={candidateDigest}",
        ];
        JsonElement build = receipt.GetProperty("build");
        if (!build.GetProperty("command").EnumerateArray().Select(row => row.GetString()!).SequenceEqual(expectedCommand))
        {
            diagnostics.Add("BUILD_RECEIPT_COMMAND_MISMATCH");
        }

        (string Result, int Warnings, int Errors) parsed = historical
            ? ParseBuildLogAtRevision(BuildLogPath, Rc2LogProvenanceCommit)
            : ParseBuildLog(BuildLogPath);
        if (build.GetProperty("exitCode").GetInt32() != 0
            || build.GetProperty("result").GetString() != "pass"
            || build.GetProperty("warnings").GetInt32() != parsed.Warnings
            || build.GetProperty("errors").GetInt32() != parsed.Errors)
        {
            diagnostics.Add("BUILD_RECEIPT_RESULT_MISMATCH");
        }

        if (!(historical
            ? BindingMatchesAtRevision(build.GetProperty("log"), BuildLogPath, "candidate-build-log", Rc2LogProvenanceCommit)
            : BindingMatches(build.GetProperty("log"), BuildLogPath, "candidate-build-log")))
        {
            diagnostics.Add("BUILD_RECEIPT_LOG_MISMATCH");
        }

        JsonElement assembly = receipt.GetProperty("assembly");
        string assemblyPath = assembly.GetProperty("path").GetString()!;
        bool assemblyIdentityMatches = Regex.IsMatch(assembly.GetProperty("sha256").GetString() ?? string.Empty, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant)
            && assembly.GetProperty("bytes").GetInt64() > 0
            && assembly.GetProperty("sourceRevisionId").GetString() == candidateDigest
            && JsonNode.Parse(assembly.GetRawText())!.ToJsonString() == JsonNode.Parse(semanticAssembly.GetRawText())!.ToJsonString();
        if (!historical)
        {
            assemblyIdentityMatches = assemblyIdentityMatches
                && TryValidateRepositoryRegularFile(assemblyPath, out _)
                && ComputeFileSha256(FullPath(assemblyPath)) == assembly.GetProperty("sha256").GetString()
                && new FileInfo(FullPath(assemblyPath)).Length == assembly.GetProperty("bytes").GetInt64();
        }

        if (!assemblyIdentityMatches)
        {
            diagnostics.Add("BUILD_RECEIPT_ASSEMBLY_MISMATCH");
        }

        return diagnostics;
    }

    private static bool BindingMatches(JsonElement binding, string path, string role)
        => binding.GetProperty("path").GetString() == path
            && binding.GetProperty("role").GetString() == role
            && TryValidateRepositoryRegularFile(path, out _)
            && binding.GetProperty("sha256").GetString() == ComputeFileSha256(FullPath(path))
            && binding.GetProperty("bytes").GetInt64() == new FileInfo(FullPath(path)).Length;

    private static bool BindingMatchesAtRevision(JsonElement binding, string path, string role, string revision)
    {
        if (binding.GetProperty("path").GetString() != path
            || binding.GetProperty("role").GetString() != role
            || !IsRegularBlobAtRevision(path, revision))
        {
            return false;
        }

        byte[] content = ReadGitBlobBytes(revision, path);
        return binding.GetProperty("sha256").GetString() == ComputeBytesSha256(content)
            && binding.GetProperty("bytes").GetInt64() == content.LongLength;
    }

    private static HashSet<string> ValidateCurrentCandidateIdentity(
        string informationalVersion,
        string candidateCommit,
        bool v24IsAncestor)
    {
        var diagnostics = new HashSet<string>(StringComparer.Ordinal);
        MatchCollection revisions = Regex.Matches(
            informationalVersion,
            "(?<![0-9a-f])[0-9a-f]{40}(?![0-9a-f])",
            RegexOptions.CultureInvariant);
        if (revisions.Count != 1)
        {
            diagnostics.Add("CURRENT_CANDIDATE_REVISION_COUNT_MISMATCH");
        }
        else if (revisions[0].Value != candidateCommit)
        {
            diagnostics.Add("CURRENT_CANDIDATE_IDENTITY_MISMATCH");
        }

        if (!v24IsAncestor)
        {
            diagnostics.Add("CURRENT_CANDIDATE_V24_ANCESTRY_MISSING");
        }

        return diagnostics;
    }

    private static HashSet<string> ValidateRunReceipt(JsonElement receipt, string candidateDigest, JsonElement semantic, bool historical = false)
    {
        var diagnostics = new HashSet<string>(StringComparer.Ordinal);
        if (receipt.GetProperty("schemaVersion").GetString() != "1.0.0"
            || receipt.GetProperty("candidateDigestAlgorithm").GetString() != CandidateDigestAlgorithm
            || receipt.GetProperty("candidateDigest").GetString() != candidateDigest)
        {
            diagnostics.Add("RUN_RECEIPT_CANDIDATE_MISMATCH");
        }

        JsonElement preRunManifest = receipt.GetProperty("preRunManifest");
        if (preRunManifest.GetProperty("path").GetString() != ManifestPath
            || preRunManifest.GetProperty("role").GetString() != "pre-run-final-manifest"
            || !Regex.IsMatch(preRunManifest.GetProperty("sha256").GetString() ?? string.Empty, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant)
            || preRunManifest.GetProperty("bytes").GetInt64() <= 0)
        {
            diagnostics.Add("RUN_RECEIPT_MANIFEST_MISMATCH");
        }

        JsonElement receiptAssembly = receipt.GetProperty("assembly");
        string assemblyPath = receiptAssembly.GetProperty("path").GetString()!;
        bool receiptAssemblyMatches = assemblyPath == semantic.GetProperty("assembly").GetProperty("path").GetString()
            && Regex.IsMatch(receiptAssembly.GetProperty("sha256").GetString() ?? string.Empty, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant)
            && receiptAssembly.GetProperty("bytes").GetInt64() > 0
            && receiptAssembly.GetProperty("sourceRevisionId").GetString() == candidateDigest
            && JsonNode.Parse(receiptAssembly.GetRawText())!.ToJsonString()
                == JsonNode.Parse(semantic.GetProperty("assembly").GetRawText())!.ToJsonString();
        if (!historical)
        {
            receiptAssemblyMatches = receiptAssemblyMatches
                && TryValidateRepositoryRegularFile(assemblyPath, out _)
                && ComputeFileSha256(FullPath(assemblyPath)) == receiptAssembly.GetProperty("sha256").GetString()
                && new FileInfo(FullPath(assemblyPath)).Length == receiptAssembly.GetProperty("bytes").GetInt64();
        }

        if (!receiptAssemblyMatches)
        {
            diagnostics.Add("RUN_RECEIPT_ASSEMBLY_MISMATCH");
        }

        JsonElement xml = receipt.GetProperty("xml");
        string xmlPath = xml.GetProperty("path").GetString()!;
        byte[] historicalXml = historical ? ReadGitBlobBytes(Rc2PublicationCommit, XmlResultsPath) : [];
        bool xmlMatches = historical
            ? IsRegularBlobAtRevision(XmlResultsPath, Rc2PublicationCommit)
                && ComputeBytesSha256(historicalXml) == xml.GetProperty("sha256").GetString()
                && historicalXml.LongLength == xml.GetProperty("bytes").GetInt64()
            : xmlPath == XmlResultsPath
                && TryValidateRepositoryRegularFile(xmlPath, out _)
                && ComputeFileSha256(FullPath(xmlPath)) == xml.GetProperty("sha256").GetString()
                && new FileInfo(FullPath(xmlPath)).Length == xml.GetProperty("bytes").GetInt64();
        if (xmlPath != XmlResultsPath || !xmlMatches)
        {
            diagnostics.Add("RUN_RECEIPT_XML_MISMATCH");
        }

        JsonElement[] rows = receipt.GetProperty("testResults").EnumerateArray().ToArray();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Pass"] = 0,
            ["Fail"] = 0,
            ["Skip"] = 0,
            ["Not Run"] = 0,
            ["NotRun"] = 0,
        };
        foreach (JsonElement row in rows)
        {
            string result = row.GetProperty("result").GetString()!;
            if (!counts.TryGetValue(result, out int count))
            {
                diagnostics.Add("RUN_RECEIPT_COUNTER_MISMATCH");
                continue;
            }

            counts[result] = count + 1;
        }

        JsonElement summary = receipt.GetProperty("summary");
        if (summary.GetProperty("total").GetInt32() != rows.Length
            || summary.GetProperty("passed").GetInt32() != counts["Pass"]
            || summary.GetProperty("failed").GetInt32() != counts["Fail"]
            || summary.GetProperty("skipped").GetInt32() != counts["Skip"]
            || summary.GetProperty("notRun").GetInt32() != counts["Not Run"] + counts["NotRun"]
            || JsonNode.Parse(summary.GetRawText())!.ToJsonString() != JsonNode.Parse(semantic.GetProperty("summary").GetRawText())!.ToJsonString()
            || JsonNode.Parse(receipt.GetProperty("testResults").GetRawText())!.ToJsonString()
                != JsonNode.Parse(semantic.GetProperty("testResults").GetRawText())!.ToJsonString())
        {
            diagnostics.Add("RUN_RECEIPT_COUNTER_MISMATCH");
        }

        JsonElement runner = receipt.GetProperty("runner");
        string[] expectedCommand =
        [
            "dotnet",
            "exec",
            "tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll",
            "-result-xml",
            Path.Combine(Path.GetTempPath(), $"hexalith-conversations-rc2-{candidateDigest}.xml"),
        ];
        bool green = runner.GetProperty("exitCode").GetInt32() == 0
            && summary.GetProperty("failed").GetInt32() == 0
            && summary.GetProperty("skipped").GetInt32() == 0
            && summary.GetProperty("notRun").GetInt32() == 0;
        if (!runner.GetProperty("command").EnumerateArray().Select(row => row.GetString()!).SequenceEqual(expectedCommand)
            || runner.GetProperty("result").GetString() != (green ? "pass" : "fail")
            || ((runner.GetProperty("exitCode").GetInt32() == 0) == (summary.GetProperty("failed").GetInt32() > 0)))
        {
            diagnostics.Add("RUN_RECEIPT_RUNNER_MISMATCH");
        }

        return diagnostics;
    }

    private static IReadOnlyDictionary<string, string> ExtractRequirementAndUxSourceHashes()
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        string[] prdLines = File.ReadAllLines(FullPath(PrdPath), Encoding.UTF8);
        Regex initiativePattern = new("^#### (FR-(?<ordinal>[0-9]+)):", RegexOptions.CultureInvariant);
        Regex featurePattern = new("^- \\*\\*(Feature-(?:FR|NFR)[0-9]+):\\*\\*\\s*(?<text>.+)$", RegexOptions.CultureInvariant);
        for (int index = 0; index < prdLines.Length; index++)
        {
            Match initiative = initiativePattern.Match(prdLines[index]);
            if (initiative.Success)
            {
                int end = index + 1;
                while (end < prdLines.Length && !prdLines[end].StartsWith("#### FR-", StringComparison.Ordinal) && !prdLines[end].StartsWith("## 7.", StringComparison.Ordinal))
                {
                    end++;
                }

                result[initiative.Groups[1].Value] = ComputeTextSha256(Normalize(string.Join('\n', prdLines[index..end]).Trim()));
            }

            Match feature = featurePattern.Match(prdLines[index]);
            if (feature.Success)
            {
                result[feature.Groups[1].Value] = ComputeTextSha256(Normalize(feature.Groups["text"].Value));
            }
        }

        Regex uxDecisionPattern = new("^\\| (UX-DR[0-9]+) \\| (?<section>[^|]+) \\| (?<summary>[^|]+) \\|", RegexOptions.CultureInvariant);
        foreach (string line in File.ReadAllLines(FullPath(UxMapPath), Encoding.UTF8))
        {
            Match match = uxDecisionPattern.Match(line);
            if (match.Success)
            {
                result[match.Groups[1].Value] = ComputeTextSha256(Normalize($"{match.Groups["section"].Value.Trim()}: {match.Groups["summary"].Value.Trim()}"));
            }
        }

        string[] uxLines = File.ReadAllLines(FullPath(UxSpecificationPath), Encoding.UTF8);
        foreach (string section in UxAcceptanceSections)
        {
            int heading = Array.FindIndex(uxLines, line => line == $"### {section}");
            heading.ShouldBeGreaterThanOrEqualTo(0);
            int ordinal = 0;
            for (int index = heading + 1; index < uxLines.Length && !uxLines[index].StartsWith("### ", StringComparison.Ordinal); index++)
            {
                if (!uxLines[index].StartsWith("- ", StringComparison.Ordinal))
                {
                    continue;
                }

                ordinal++;
                string hash = ComputeTextSha256(Normalize(uxLines[index][2..].Trim()));
                result[$"UX-AC-{Slug(section)}-{ordinal:00}-{hash[..12]}"] = hash;
            }
        }

        return result;
    }

    private static string RenderMarkdown(JsonElement manifest, string jsonSha256)
    {
        JsonElement current = manifest.GetProperty("testDenominator").GetProperty("currentCandidate");
        string candidateDigest = manifest.GetProperty("sourceBinding").GetProperty("ownerReviewOverlay").GetProperty("candidateDigest").GetString()!;
        return "# Preservation Traceability Manifest v3 — Remediated Successor\n\n"
            + $"- Version: `{manifest.GetProperty("manifestVersion").GetString()}`\n"
            + "- Status: `pending-owner-approval`\n"
            + $"- Source candidate: `{CandidateDigestAlgorithm}:{candidateDigest}`\n"
            + $"- Canonical JSON SHA-256: `{jsonSha256}`\n"
            + "- Predecessor: approved immutable `3.0.0-rc.1`\n"
            + "- Successor approval: `pending`\n\n"
            + "The successor retains exactly 473 fully qualified test IDs, the 214-test v1 floor, the 384-test approved cumulative floor, "
            + $"89 pending additions, seven category mappings, 969 obligation identities, and 277 governed dispositions. The bound Conformance run is {current.GetProperty("passed").GetInt32()}/{current.GetProperty("testCount").GetInt32()} with {current.GetProperty("failed").GetInt32()} failed, {current.GetProperty("skipped").GetInt32()} skipped, and {current.GetProperty("notRun").GetInt32()} not-run tests.\n\n"
            + "Gate state remains fail-closed: FR-20 `PENDING`, SM-C1 `PENDING`, SM-C2 `FAILED`, OQ-1 `BLOCKED`, implementation hold `ACTIVE`. Green validation is not approval and does not activate legacy requirements, grants, release authority, waiver, signature, or ownership.\n";
    }

    private static string CanonicalObligationProjection(JsonElement row)
    {
        var projection = new JsonObject
        {
            ["id"] = row.GetProperty("id").GetString(),
            ["kind"] = row.GetProperty("kind").GetString(),
            ["preservationState"] = row.GetProperty("preservationState").GetString(),
            ["releaseActivationState"] = row.GetProperty("releaseActivationState").GetString(),
            ["closure"] = StripClosureBindingHashes(JsonNode.Parse(row.GetProperty("closure").GetRawText())!),
            ["orphan"] = row.GetProperty("orphan").GetBoolean(),
        };
        if (row.TryGetProperty("controlOwner", out JsonElement owner))
        {
            projection["controlOwner"] = owner.GetString();
        }

        if (row.TryGetProperty("tier", out JsonElement tier))
        {
            projection["tier"] = tier.GetString();
        }

        return projection.ToJsonString();
    }

    private static string CanonicalClosureProjection(JsonNode? node)
        => node is null ? "<absent>" : StripClosureBindingHashes(node.DeepClone()).ToJsonString();

    private static JsonNode StripClosureBindingHashes(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (string property in jsonObject.Select(pair => pair.Key).ToArray())
            {
                if (property.EndsWith("sha256", StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject.Remove(property);
                }
                else if (jsonObject[property] is JsonNode child)
                {
                    StripClosureBindingHashes(child);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? child in jsonArray)
            {
                if (child is not null)
                {
                    StripClosureBindingHashes(child);
                }
            }
        }

        return node;
    }

    private static string ComputeCandidateDigest(JsonElement bindings)
    {
        var material = new StringBuilder($"overlay-v2\nbaseCommit\t{BaseCommit}\n");
        foreach (JsonElement row in bindings.EnumerateArray())
        {
            material.Append(row.GetProperty("path").GetString()!.Normalize(NormalizationForm.FormC))
                .Append('\t').Append(row.GetProperty("mode").GetString())
                .Append('\t').Append(row.GetProperty("sha256").GetString())
                .Append('\t').Append(row.GetProperty("bytes").GetInt64()).Append('\n');
        }

        return ComputeTextSha256(material.ToString());
    }

    private static string ComputePathInventorySha256(IEnumerable<string> paths)
        => ComputeTextSha256(string.Concat(paths.Order(StringComparer.Ordinal).Select(path => path.Normalize(NormalizationForm.FormC) + "\n")));

    private static HashSet<string> GitPaths(params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = RepositoryRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, error);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);
    }

    private static string GitText(params string[] arguments)
    {
        (int exitCode, string output, string error) = RunGit(arguments);
        exitCode.ShouldBe(0, error);
        return output.Trim();
    }

    private static int GitExitCode(params string[] arguments)
        => RunGit(arguments).ExitCode;

    private static (int ExitCode, string Output, string Error) RunGit(IEnumerable<string> arguments)
        => RunProcess("git", arguments);

    private static (int ExitCode, string Output, string Error) RunProcess(string fileName, IEnumerable<string> arguments)
        => RunProcessInDirectory(RepositoryRoot(), fileName, arguments);

    private static (int ExitCode, string Output, string Error) RunProcessInDirectory(
        string workingDirectory,
        string fileName,
        IEnumerable<string> arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(fileName)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }

    private static (string Result, int Warnings, int Errors) ParseBuildLog(string path)
    {
        string text = File.ReadAllText(FullPath(path), Encoding.UTF8);
        return ParseBuildLogText(text);
    }

    private static (string Result, int Warnings, int Errors) ParseBuildLogAtRevision(string path, string revision)
        => ParseBuildLogText(ReadTextAtRevision(path, revision));

    private static (string Result, int Warnings, int Errors) ParseBuildLogText(string text)
    {
        MatchCollection warnings = Regex.Matches(text, "(?m)^\\s*([0-9]+) Warning\\(s\\)\\s*$");
        MatchCollection errors = Regex.Matches(text, "(?m)^\\s*([0-9]+) Error\\(s\\)\\s*$");
        warnings.Count.ShouldBeGreaterThan(0);
        errors.Count.ShouldBeGreaterThan(0);
        int warningCount = int.Parse(warnings[warnings.Count - 1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        int errorCount = int.Parse(errors[errors.Count - 1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        string result = text.Contains("Build succeeded.", StringComparison.Ordinal) && errorCount == 0 ? "pass" : "fail";
        return (result, warningCount, errorCount);
    }

    private static string GetWorktreeGitMode(string repositoryPath, string? repositoryRoot = null)
    {
        if (OperatingSystem.IsWindows())
        {
            return SourceInputMode;
        }

        string root = Path.GetFullPath(repositoryRoot ?? RepositoryRoot());
        UnixFileMode mode = File.GetUnixFileMode(Path.GetFullPath(Path.Combine(root, repositoryPath)));
        UnixFileMode executable = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        return (mode & executable) == 0 ? "100644" : "100755";
    }

    private static bool TryValidateCandidateSourceMode(
        string repositoryPath,
        string declaredMode,
        string baseCommit,
        out string diagnostic,
        string? repositoryRoot = null)
    {
        string root = Path.GetFullPath(repositoryRoot ?? RepositoryRoot());
        string worktreeMode = GetWorktreeGitMode(repositoryPath, root);
        if (declaredMode != SourceInputMode || worktreeMode != SourceInputMode)
        {
            diagnostic = $"candidate source declared/worktree mode mismatch for {repositoryPath}: "
                + $"expected {SourceInputMode}, declared={declaredMode}, worktree={worktreeMode}";
            return false;
        }

        if (!TryGetBaselineSourcePresence(repositoryPath, baseCommit, root, out bool baselinePresent, out diagnostic))
        {
            return false;
        }

        (int exitCode, string output, string error) = RunProcessInDirectory(
            root,
            "git",
            ["ls-files", "--stage", "-z", "--", repositoryPath.Replace('\\', '/')]);
        if (exitCode != 0)
        {
            diagnostic = $"unable to inspect candidate source index mode for {repositoryPath}: {error.Trim()}";
            return false;
        }

        string[] stages = output.Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (stages.Length == 0)
        {
            if (baselinePresent)
            {
                diagnostic = $"candidate source index entry missing for baseline-present path {repositoryPath} at {baseCommit}";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        string[] modes = stages.Select(line => line.Split(' ', 2, StringSplitOptions.None)[0]).ToArray();
        string normalizedPath = repositoryPath.Replace('\\', '/');
        string[] entryParts = stages[0].Split('\t', 2, StringSplitOptions.None);
        string[] metadata = entryParts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (stages.Length != 1
            || entryParts.Length != 2
            || metadata.Length != 3
            || metadata[2] != "0"
            || entryParts[1] != normalizedPath
            || modes[0] != SourceInputMode
            || modes[0] != declaredMode
            || modes[0] != worktreeMode)
        {
            diagnostic = $"candidate source stage/declared/worktree mode mismatch for {repositoryPath}: "
                + $"expected one stage-0 {SourceInputMode} row, stages={stages.Length}, stage={string.Join(',', modes)}, "
                + $"declared={declaredMode}, worktree={worktreeMode}";
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    private static bool TryGetBaselineSourcePresence(
        string repositoryPath,
        string baseCommit,
        string repositoryRoot,
        out bool present,
        out string diagnostic)
    {
        string normalizedPath = repositoryPath.Replace('\\', '/');
        (int treeExit, string treeOutput, string treeError) = RunProcessInDirectory(
            repositoryRoot,
            "git",
            ["ls-tree", "--full-tree", "-z", baseCommit, "--", normalizedPath]);
        if (treeExit != 0)
        {
            present = false;
            diagnostic = $"unable to inspect candidate source baseline tree for {repositoryPath}: {treeError.Trim()}";
            return false;
        }

        (int objectExit, _, _) = RunProcessInDirectory(
            repositoryRoot,
            "git",
            ["cat-file", "-e", $"{baseCommit}:{normalizedPath}"]);
        string[] treeRows = treeOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool objectExists = objectExit == 0;
        if (objectExists)
        {
            string[] entryParts = treeRows.Length == 1
                ? treeRows[0].Split('\t', 2, StringSplitOptions.None)
                : [];
            if (treeRows.Length != 1 || entryParts.Length != 2 || entryParts[1] != normalizedPath)
            {
                present = false;
                diagnostic = $"candidate source baseline presence mismatch for {repositoryPath}: "
                    + $"cat-file exists but ls-tree rows={treeRows.Length}";
                return false;
            }

            present = true;
            diagnostic = string.Empty;
            return true;
        }

        if (treeRows.Length != 0)
        {
            present = false;
            diagnostic = $"candidate source baseline presence mismatch for {repositoryPath}: "
                + $"cat-file missing but ls-tree rows={treeRows.Length}";
            return false;
        }

        present = false;
        diagnostic = string.Empty;
        return true;
    }

    private static string NodeText(JsonNode? node)
        => node?.ToJsonString() ?? "<absent>";

    private static HashSet<string> RowsOfKind(IEnumerable<JsonElement> rows, string kind)
        => rows.Where(row => row.GetProperty("kind").GetString() == kind).Select(row => row.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal);

    private static bool IsRequirementOrUx(string? kind)
        => kind is "initiative-fr" or "feature-fr" or "feature-nfr" or "ux-decision" or "ux-acceptance";

    private static void ValidateBinding(JsonElement binding)
    {
        string path = binding.GetProperty("path").GetString()!;
        TryValidateRepositoryRegularFile(path, out string diagnostic).ShouldBeTrue(diagnostic);
        string fullPath = FullPath(path);
        ComputeFileSha256(fullPath).ShouldBe(binding.GetProperty("sha256").GetString(), path);
        new FileInfo(fullPath).Length.ShouldBe(binding.GetProperty("bytes").GetInt64(), path);
    }

    private static void ValidateBindingAtRevision(JsonElement binding, string revision)
    {
        string path = binding.GetProperty("path").GetString()!;
        Path.IsPathRooted(path).ShouldBeFalse(path);
        path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment == "." || segment == "..")
            .ShouldBeFalse(path);

        (int treeExitCode, string treeOutput, string treeError) = RunGit(
            ["ls-tree", "--full-tree", revision, "--", path.Replace('\\', '/')]);
        treeExitCode.ShouldBe(0, treeError);
        string[] treeRows = treeOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        treeRows.Length.ShouldBe(1, $"{path} must have exactly one tree entry at {revision}");
        treeRows[0].ShouldStartWith(
            "100644 blob ",
            Case.Sensitive,
            $"{path} must be a regular non-executable blob at {revision}");

        byte[] bytes = ReadGitBlobBytes(revision, path);
        ComputeBytesSha256(bytes)
            .ShouldBe(binding.GetProperty("sha256").GetString(), path);
        bytes.LongLength.ShouldBe(binding.GetProperty("bytes").GetInt64(), path);
    }

    private static bool IsRegularBlobAtRevision(string repositoryPath, string revision)
    {
        (int exitCode, string output, _) = RunGit(
            ["ls-tree", "--full-tree", revision, "--", repositoryPath.Replace('\\', '/')]);
        if (exitCode != 0)
        {
            return false;
        }

        string[] rows = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return rows.Length == 1 && rows[0].StartsWith("100644 blob ", StringComparison.Ordinal);
    }

    private static string HistoricalRevisionFor(string repositoryPath)
        => repositoryPath is BuildLogPath or RestoreLogPath
            ? Rc2LogProvenanceCommit
            : Rc2PublicationCommit;

    private static byte[] ReadGitBlobBytes(string revision, string repositoryPath)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = RepositoryRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        process.StartInfo.ArgumentList.Add("show");
        process.StartInfo.ArgumentList.Add($"{revision}:{repositoryPath.Replace('\\', '/')}");
        process.Start();
        using var output = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(output);
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, error);
        return output.ToArray();
    }

    private static string ReadTextAtRevision(string repositoryPath, string revision)
        => Encoding.UTF8.GetString(ReadGitBlobBytes(revision, repositoryPath));

    private static string ComputeBlobSha256(string revision, string repositoryPath)
        => ComputeBytesSha256(ReadGitBlobBytes(revision, repositoryPath));

    private static bool TryValidateRepositoryRegularFile(string repositoryPath, out string diagnostic, string? repositoryRoot = null)
    {
        diagnostic = string.Empty;
        if (Path.IsPathRooted(repositoryPath))
        {
            diagnostic = $"absolute repository path is forbidden: {repositoryPath}";
            return false;
        }

        string[] segments = repositoryPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            diagnostic = $"repository path traversal is forbidden: {repositoryPath}";
            return false;
        }

        string root = Path.GetFullPath(repositoryRoot ?? RepositoryRoot());
        string fullPath = Path.GetFullPath(Path.Combine(root, Path.Combine(segments)));
        string relative = Path.GetRelativePath(root, fullPath);
        if (Path.IsPathRooted(relative)
            || relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            diagnostic = $"resolved repository path escapes the root: {repositoryPath}";
            return false;
        }

        string current = root;
        for (int index = 0; index < segments.Length; index++)
        {
            current = Path.Combine(current, segments[index]);
            if (index == segments.Length - 1 && Directory.Exists(current))
            {
                diagnostic = $"repository evidence path is not a regular file: {repositoryPath}";
                return false;
            }

            FileSystemInfo info = index == segments.Length - 1 ? new FileInfo(current) : new DirectoryInfo(current);
            info.Refresh();
            if (info.LinkTarget is not null || (info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint)))
            {
                diagnostic = $"symlink/reparse-point repository path is forbidden: {repositoryPath}";
                return false;
            }

            if (!info.Exists)
            {
                diagnostic = $"repository path does not exist: {repositoryPath}";
                return false;
            }

            if (index < segments.Length - 1 && info is not DirectoryInfo)
            {
                diagnostic = $"non-directory repository path component: {repositoryPath}";
                return false;
            }
        }

        FileInfo file = new(fullPath);
        if (!file.Exists || file.Attributes.HasFlag(FileAttributes.Directory) || file.Attributes.HasFlag(FileAttributes.Device))
        {
            diagnostic = $"repository evidence path is not a regular file: {repositoryPath}";
            return false;
        }

        if (!OperatingSystem.IsWindows())
        {
            (int exitCode, string output, string error) = RunProcess("stat", ["--format=%F", "--", fullPath]);
            if (exitCode != 0 || output.Trim() != "regular file")
            {
                diagnostic = $"repository evidence path has non-regular file type: {repositoryPath}; {error.Trim()}";
                return false;
            }
        }

        (int gitExitCode, string gitOutput, string gitError) = RunProcessInDirectory(
            root,
            "git",
            ["ls-files", "--stage", "--", repositoryPath.Replace('\\', '/')]);
        if (gitExitCode != 0)
        {
            diagnostic = $"unable to inspect Git mode for {repositoryPath}: {gitError.Trim()}";
            return false;
        }

        string[] stages = gitOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (stages.Length > 0)
        {
            string[] modes = stages.Select(line => line.Split(' ', 2, StringSplitOptions.None)[0]).Distinct(StringComparer.Ordinal).ToArray();
            if (stages.Length != 1 || modes.Length != 1 || modes[0] is not ("100644" or "100755"))
            {
                diagnostic = $"unexpected Git mode for repository evidence {repositoryPath}: {string.Join(',', modes)}";
                return false;
            }
        }

        return true;
    }

    private static bool IsRepositoryRegularFile(string repositoryPath)
        => TryValidateRepositoryRegularFile(repositoryPath, out _);

    private static string Normalize(string value)
        => Regex.Replace(value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim(), "\\s+", " ");

    private static string Slug(string value)
        => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    private static JsonDocument LoadJson(string repositoryPath)
    {
        TryValidateRepositoryRegularFile(repositoryPath, out string diagnostic).ShouldBeTrue(diagnostic);
        return JsonDocument.Parse(File.ReadAllText(FullPath(repositoryPath), Encoding.UTF8));
    }

    private static JsonDocument LoadJsonAtRevision(string repositoryPath, string revision)
        => JsonDocument.Parse(ReadGitBlobBytes(revision, repositoryPath));

    private static string FullPath(string repositoryPath)
        => Path.GetFullPath(Path.Combine(RepositoryRoot(), repositoryPath));

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static string ComputeFileSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string ComputeBytesSha256(byte[] content)
        => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string ComputeTextSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
