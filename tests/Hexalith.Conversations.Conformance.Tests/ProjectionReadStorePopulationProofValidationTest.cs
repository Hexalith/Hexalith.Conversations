// <copyright file="ProjectionReadStorePopulationProofValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Mechanically validates the Story 6.2 production-boundary projection proof and its bound evidence.
/// </summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class ProjectionReadStorePopulationProofValidationTest
{
    private const string ProofJsonFileName = "projection-read-store-population-proof-v2.json";
    private const string ProofMarkdownFileName = "projection-read-store-population-proof-v2.md";
    private const string BaselineFileName = "sm-c2-hot-path-baseline-v1.json";
    private const string PostFileName = "sm-c2-hot-path-post-v1.json";

    private static readonly string[] ExpectedHotPaths = ["HP-APPEND", "HP-CREATE", "HP-LIST", "HP-OPEN"];

    [Fact]
    public void ProofShouldBindExactProductionRouteKeysAndBoundedOutcomes()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        JsonElement proof = proofDocument.RootElement;
        JsonElement boundary = proof.GetProperty("productionBoundary");

        proof.GetProperty("artifactVersion").GetString().ShouldBe("projection-read-store-population-proof-v2");
        proof.GetProperty("baselineRevision").GetString().ShouldBe("29def441408becfbbbdc5c59b9af14a7717cb21f");
        proof.GetProperty("result").GetString().ShouldBe("pass");
        boundary.GetProperty("route").GetString().ShouldBe("conversation/conversation-read-model");
        boundary.GetProperty("rebuildSemantics").GetString().ShouldBe("FullReplay");
        boundary.GetProperty("configuredStateStore").GetString().ShouldBe("statestore");
        boundary.GetProperty("detailKeyTemplate").GetString().ShouldBe("projection:conversations:{tenantId}:{conversationId}");
        boundary.GetProperty("tenantIndexKeyTemplate").GetString().ShouldBe("projection:conversations-index:{tenantId}");
        boundary.GetProperty("detailKey").GetString().ShouldBe("projection:conversations:tenant-live-001:conversation-live-001");
        boundary.GetProperty("tenantIndexKey").GetString().ShouldBe("projection:conversations-index:tenant-live-001");
        boundary.GetProperty("queryBackfill").GetBoolean().ShouldBeFalse();

        Dictionary<string, JsonElement> scenarios = proof.GetProperty("dispatchEvidence")
            .EnumerateArray()
            .ToDictionary(element => element.GetProperty("scenario").GetString()!, StringComparer.Ordinal);
        scenarios.Keys.Order(StringComparer.Ordinal).ShouldBe(
        [
            "accepted-append",
            "cross-tenant-input",
            "derived-state-deletion",
            "full-replay",
            "second-write-failure",
            "second-write-retry",
            "stable-duplicate",
            "unavailable-store",
        ]);
        scenarios["accepted-append"].GetProperty("handlerStatus").GetString().ShouldBe("Completed");
        scenarios["stable-duplicate"].GetProperty("tenantIndexRows").GetInt32().ShouldBe(1);
        scenarios["second-write-failure"].GetProperty("handlerStatus").GetString().ShouldBe("Retryable");
        scenarios["second-write-failure"].GetProperty("reasonCode").GetString().ShouldBe("PartialRetry");
        scenarios["second-write-failure"].GetProperty("falseCurrentObserved").GetBoolean().ShouldBeFalse();
        scenarios["unavailable-store"].GetProperty("handlerStatus").GetString().ShouldBe("Indeterminate");
        scenarios["unavailable-store"].GetProperty("rawStorageDetailExposed").GetBoolean().ShouldBeFalse();
        scenarios["cross-tenant-input"].GetProperty("writes").GetInt32().ShouldBe(0);
        scenarios["full-replay"].GetProperty("batchOperationCount").GetInt32().ShouldBe(2);
        scenarios["full-replay"].GetProperty("queryResultsEquivalentToPreDeletion").GetBoolean().ShouldBeTrue();

        JsonElement hosting = proof.GetProperty("hostingEvidence");
        hosting.GetProperty("isPackable").GetBoolean().ShouldBeFalse();
        hosting.GetProperty("isPublishable").GetBoolean().ShouldBeFalse();
        hosting.GetProperty("projectResources").EnumerateArray().Select(value => value.GetString()).ShouldBe(
            ["conversations", "conversations-admin-web", "eventstore"]);
        hosting.GetProperty("conversationsServiceDefaultsRemoved").GetBoolean().ShouldBeTrue();

        JsonElement promotion = proof.GetProperty("eventStorePromotion");
        promotion.GetProperty("commit").GetString().ShouldBe("c8c7003052a7f811d3b821f3442379ca5f3a9c65");
        promotion.GetProperty("remoteContainsCommit").GetBoolean().ShouldBeTrue();
        promotion.GetProperty("submoduleWorktreeClean").GetBoolean().ShouldBeTrue();
        promotion.GetProperty("requiredGitlinkMode").GetString().ShouldBe("160000");
        promotion.GetProperty("requiredUmbrellaGitlinkCommit").GetString().ShouldBe(
            promotion.GetProperty("commit").GetString());

        // The gitlink moved from 0eb3657 to c8c7003 after the first capture. The delta is recorded so the
        // rebinding is auditable, and the claim that matters -- neither promoted-capability file changed --
        // is asserted here rather than left as prose.
        JsonElement delta = promotion.GetProperty("promotedCapabilityDelta");
        delta.GetProperty("previouslyRecordedCommit").GetString().ShouldBe("0eb365797d06207e42b517375664f46405a7ad7d");
        delta.GetProperty("currentCommit").GetString().ShouldBe(promotion.GetProperty("commit").GetString());
        delta.GetProperty("promotedCapabilityFilesChanged").GetArrayLength().ShouldBe(0);

        JsonElement promotionGate = promotion.GetProperty("umbrellaMechanicalGate");
        promotionGate.GetProperty("schema").GetString().ShouldBe("submodule-promotion-gate/v1");
        promotionGate.GetProperty("result").GetString().ShouldBe("pass");
        promotionGate.GetProperty("baseline").GetString().ShouldBe("29def441408becfbbbdc5c59b9af14a7717cb21f");
        promotionGate.GetProperty("candidate").GetString().ShouldBe("c398ea2167ed7b9a2ae7cab256637882db6cca82");
        promotionGate.GetProperty("recordedGitlink").GetString().ShouldBe(promotion.GetProperty("commit").GetString());
        promotionGate.GetProperty("recordedMode").GetString().ShouldBe("160000");
        promotionGate.GetProperty("blockers").GetArrayLength().ShouldBe(0);
        promotionGate.GetProperty("warnings").GetArrayLength().ShouldBe(0);

        string[] declaredScope =
        [
            .. promotionGate.GetProperty("declaredScope")
                .EnumerateArray()
                .Select(entry => entry.GetProperty("path").GetString()!)
                .Order(StringComparer.Ordinal),
        ];
        declaredScope.ShouldBe(
            ["references/Hexalith.Builds", "references/Hexalith.EventStore", "references/Hexalith.Tenants"],
            "the approved scope expansion declares exactly three root gitlinks");
        promotionGate.GetProperty("declaredScope")
            .EnumerateArray()
            .ShouldAllBe(entry => entry.GetProperty("requireRemote").GetBoolean());

        // Declared scope must equal what actually moved. A declaration that omits a changed gitlink is the
        // Story 6.7 "undisclosed scope" defect; one that adds an unchanged path proves nothing.
        string[] changedGitlinks =
        [
            .. promotionGate.GetProperty("changedGitlinks")
                .EnumerateArray()
                .Select(value => value.GetString()!)
                .Order(StringComparer.Ordinal),
        ];
        changedGitlinks.ShouldBe(declaredScope);

        foreach (JsonElement evaluated in promotionGate.GetProperty("evaluated").EnumerateArray())
        {
            string path = evaluated.GetProperty("path").GetString()!;
            evaluated.GetProperty("initialized").GetBoolean().ShouldBeTrue(path);
            evaluated.GetProperty("clean").GetBoolean().ShouldBeTrue(path);
            evaluated.GetProperty("remoteAvailable").GetBoolean().ShouldBeTrue(path);
            evaluated.GetProperty("recordedMode").GetString().ShouldBe("160000", path);
            evaluated.GetProperty("recordedGitlink").GetString().ShouldBe(evaluated.GetProperty("head").GetString(), path);
        }
    }

    /// <summary>
    /// Re-derives the recorded promotion from the working tree so the evidence cannot go quietly stale.
    /// </summary>
    /// <remarks>
    /// A gate result cannot name the commit that contains it, so the evidence pins the last revision that moved
    /// a gitlink or production source. That pin is only worth something if a later revision moving a declared
    /// gitlink turns it red, which is what this test enforces. Story 6.7 review pass 2 recorded the opposite
    /// failure: completion evidence that corresponded to no single revision, with nothing able to notice.
    /// </remarks>
    [Fact]
    public void RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        JsonElement promotion = proofDocument.RootElement.GetProperty("eventStorePromotion");
        JsonElement promotionGate = promotion.GetProperty("umbrellaMechanicalGate");
        string candidate = promotionGate.GetProperty("candidate").GetString()!;

        Git("merge-base", "--is-ancestor", candidate, "HEAD")
            .ShouldBe(string.Empty, $"recorded candidate {candidate} must be an ancestor of HEAD");

        Git("diff", "--name-only", $"{candidate}..HEAD", "--", "references/")
            .ShouldBeEmpty("no root gitlink may move after the recorded promotion candidate");

        foreach (JsonElement evaluated in promotionGate.GetProperty("evaluated").EnumerateArray())
        {
            string path = evaluated.GetProperty("path").GetString()!;
            Git("rev-parse", $"HEAD:{path}")
                .ShouldBe(evaluated.GetProperty("recordedGitlink").GetString(), path);
        }

        Git("rev-parse", "HEAD:references/Hexalith.EventStore")
            .ShouldBe(promotion.GetProperty("requiredUmbrellaGitlinkCommit").GetString());
    }

    /// <summary>
    /// Asserts the ADR 0003 Verification 1-2 gateway lane is recorded as crossed, not narrowed.
    /// </summary>
    [Fact]
    public void GatewayBoundaryEvidenceShouldCrossTheCoordinatorAndTheDaprStateStore()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        JsonElement gateway = proofDocument.RootElement.GetProperty("gatewayBoundaryEvidence");

        // Task T2 allowed a named-owner justification that would have narrowed ADR 0003's own verification
        // wording. It was not taken, so the evidence must not read as though it were.
        gateway.GetProperty("resolution").GetString().ShouldBe("strengthened-fixture");
        gateway.GetProperty("residualGap").GetString().ShouldBe("none");
        gateway.GetProperty("satisfies").EnumerateArray().Select(value => value.GetString()).ShouldContain("ADR 0003 Verification 1");
        gateway.GetProperty("satisfies").EnumerateArray().Select(value => value.GetString()).ShouldContain("ADR 0003 Verification 2");

        gateway.GetProperty("deliveryDriver").GetString()
            .ShouldBe("Hexalith.EventStore.Server.Projections.IProjectionUpdateOrchestrator.UpdateProjectionAsync");
        string[] crossed = [.. gateway.GetProperty("componentsCrossed").EnumerateArray().Select(value => value.GetString()!)];
        crossed.ShouldContain("Hexalith.EventStore.Server.Projections.ProjectionUpdateOrchestrator");
        crossed.ShouldContain("Hexalith.EventStore.Server.Projections.NamedProjectionDispatchCoordinator");
        crossed.ShouldContain("Hexalith.Conversations.Server.Projections.ConversationAsyncProjectionHandler");

        JsonElement store = gateway.GetProperty("integrationStateStore");
        store.GetProperty("configuredReadModelStoreType").GetString()
            .ShouldBe("Hexalith.EventStore.Client.Projections.DaprReadModelStore");
        store.GetProperty("inMemoryFakeUsed").GetBoolean().ShouldBeFalse();
        store.GetProperty("componentName").GetString().ShouldBe("statestore");
        store.GetProperty("componentType").GetString().ShouldBe("state.redis");

        // A non-zero refresh interval would let UpdateProjectionAsync return without dispatching, so the
        // persistence assertions could pass against an empty store.
        gateway.GetProperty("immediateDelivery").GetProperty("projectionRefreshIntervalMs").GetInt32().ShouldBe(0);
        gateway.GetProperty("routeDiscovery").GetProperty("discoveredNamedProjectionType").GetString()
            .ShouldBe("conversation-read-model");

        Dictionary<string, JsonElement> scenarios = gateway.GetProperty("scenarios")
            .EnumerateArray()
            .ToDictionary(element => element.GetProperty("scenario").GetString()!, StringComparer.Ordinal);
        scenarios.Keys.Order(StringComparer.Ordinal).ShouldBe(["gateway-accepted-append", "gateway-duplicate-delivery"]);
        scenarios["gateway-accepted-append"].GetProperty("detailPersisted").GetBoolean().ShouldBeTrue();
        scenarios["gateway-accepted-append"].GetProperty("tenantIndexPersisted").GetBoolean().ShouldBeTrue();
        scenarios["gateway-accepted-append"].GetProperty("crossKeyGenerationAgreement").GetBoolean().ShouldBeTrue();
        scenarios["gateway-accepted-append"].GetProperty("productionQueryAsserted").GetBoolean().ShouldBeTrue();
        scenarios["gateway-duplicate-delivery"].GetProperty("secondDeliveryChangedPersistedState").GetBoolean().ShouldBeFalse();
        scenarios["gateway-duplicate-delivery"].GetProperty("tenantIndexRows").GetInt32().ShouldBe(1);

        // AC5 is not satisfied by a skipped test.
        JsonElement run = gateway.GetProperty("run");
        run.GetProperty("passed").GetInt32().ShouldBe(2);
        run.GetProperty("failed").GetInt32().ShouldBe(0);
        run.GetProperty("skipped").GetInt32().ShouldBe(0);
    }

    private static string Git(params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = FindRepositoryRoot(),
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("git could not be started.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(milliseconds: 120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("git did not complete within 120 seconds.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.{Environment.NewLine}{standardError}");
        }

        return standardOutput.Trim();
    }

    [Fact]
    public void ProofSourceAndSignedV1BindingsShouldRemainByteIdentical()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        JsonElement proof = proofDocument.RootElement;

        ValidateBindings(proof.GetProperty("sourceBindings"));
        ValidateBindings(proof.GetProperty("immutableSignedV1Bindings"));

        using JsonDocument decisionDocument = LoadEvidence("success-metric-report-and-attestation-v1-release-owner-decision.json");
        JsonElement attestation = decisionDocument.RootElement.GetProperty("sourceAttestation");
        attestation.GetProperty("artifactSha256").GetString().ShouldBe(
            "062ca0c7bc94279007077bda59eae867d21c12da2ffc0b59a0f389b99067e0fe");
        attestation.GetProperty("summarySha256").GetString().ShouldBe(
            "aa7e52c11ce36fc2c9ea953e275c654e7f312016c990cb20be16666d87f9a2cd");
    }

    [Fact]
    public void SmC2PostShouldUseIdenticalEnvelopeAndKeepEveryP95WithinFivePercent()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        using JsonDocument baselineDocument = LoadEvidence(BaselineFileName);
        using JsonDocument postDocument = LoadEvidence(PostFileName);
        JsonElement proofPerformance = proofDocument.RootElement.GetProperty("performanceEvidence");
        JsonElement baseline = baselineDocument.RootElement;
        JsonElement post = postDocument.RootElement;

        ValidateBinding(proofPerformance.GetProperty("baseline"));
        ValidateBinding(proofPerformance.GetProperty("post"));
        proofPerformance.GetProperty("rowsPassing").GetInt32().ShouldBe(4);
        proofPerformance.GetProperty("rowsTotal").GetInt32().ShouldBe(4);
        proofPerformance.GetProperty("result").GetString().ShouldBe("pass");

        JsonElement baselineFixture = baseline.GetProperty("fixture");
        JsonElement postFixture = post.GetProperty("fixture");
        foreach (string propertyName in new[]
                 {
                     "sha256",
                     "warmupRepetitions",
                     "repetitions",
                     "operationsPerSample",
                     "concurrency",
                     "processing",
                 })
        {
            postFixture.GetProperty(propertyName).GetRawText().ShouldBe(baselineFixture.GetProperty(propertyName).GetRawText());
        }

        Dictionary<string, JsonElement> baselineRows = RowsByHotPath(baseline);
        Dictionary<string, JsonElement> postRows = RowsByHotPath(post);
        baselineRows.Keys.Order(StringComparer.Ordinal).ShouldBe(ExpectedHotPaths);
        postRows.Keys.Order(StringComparer.Ordinal).ShouldBe(ExpectedHotPaths);

        foreach (string hotPath in ExpectedHotPaths)
        {
            JsonElement baselineRow = baselineRows[hotPath];
            JsonElement postRow = postRows[hotPath];
            double[] baselineRaw = ReadRawSamples(baselineRow);
            double[] postRaw = ReadRawSamples(postRow);
            double baselineP95 = P95(baselineRaw);
            double postP95 = P95(postRaw);

            baselineRaw.Length.ShouldBe(30);
            postRaw.Length.ShouldBe(30);
            baselineRaw.ShouldAllBe(value => value > 0);
            postRaw.ShouldAllBe(value => value > 0);
            baselineRow.GetProperty("p95Microseconds").GetDouble().ShouldBe(baselineP95, tolerance: 0.0000005);
            postRow.GetProperty("p95Microseconds").GetDouble().ShouldBe(postP95, tolerance: 0.0000005);
            postRow.GetProperty("baselineP95Microseconds").GetDouble().ShouldBe(baselineP95, tolerance: 0.0000005);
            postP95.ShouldBeLessThanOrEqualTo(baselineP95 * 1.05);
            postRow.GetProperty("result").GetString().ShouldBe("pass");
        }
    }

    [Fact]
    public void MarkdownShouldPresentTheAuthoritativeJsonBoundary()
    {
        string markdown = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), ProofMarkdownFileName));

        markdown.ShouldContain("**Result:** pass", Case.Sensitive);
        markdown.ShouldContain("`conversation/conversation-read-model`", Case.Sensitive);
        markdown.ShouldContain("`projection:conversations:{tenantId}:{conversationId}`", Case.Sensitive);
        markdown.ShouldContain("`projection:conversations-index:{tenantId}`", Case.Sensitive);
        markdown.ShouldContain("c8c7003052a7f811d3b821f3442379ca5f3a9c65", Case.Sensitive);
        markdown.ShouldContain("c398ea2167ed7b9a2ae7cab256637882db6cca82", Case.Sensitive);
        markdown.ShouldContain("no blockers and no warnings", Case.Sensitive);
        markdown.ShouldContain("Gateway production boundary (ADR 0003 Verification 1-2)", Case.Sensitive);
        markdown.ShouldContain("The companion JSON is authoritative", Case.Sensitive);
    }

    private static Dictionary<string, JsonElement> RowsByHotPath(JsonElement document)
        => document.GetProperty("rows")
            .EnumerateArray()
            .ToDictionary(row => row.GetProperty("hotPathId").GetString()!, StringComparer.Ordinal);

    private static double[] ReadRawSamples(JsonElement row)
        => [.. row.GetProperty("rawMicrosecondsPerOperation").EnumerateArray().Select(value => value.GetDouble())];

    private static double P95(IEnumerable<double> values)
    {
        double[] ordered = [.. values.Order()];
        int index = (int)Math.Ceiling(0.95 * ordered.Length) - 1;
        return ordered[Math.Max(0, index)];
    }

    private static void ValidateBindings(JsonElement bindings)
    {
        foreach (JsonElement binding in bindings.EnumerateArray())
        {
            ValidateBinding(binding);
        }
    }

    private static void ValidateBinding(JsonElement binding)
    {
        string repositoryRoot = FindRepositoryRoot();
        string relativePath = binding.GetProperty("path").GetString() ?? string.Empty;
        string fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));

        Path.IsPathRooted(relativePath).ShouldBeFalse();
        fullPath.StartsWith(repositoryRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal).ShouldBeTrue();
        File.Exists(fullPath).ShouldBeTrue(relativePath);
        ComputeSha256(fullPath).ShouldBe(binding.GetProperty("sha256").GetString(), relativePath);
    }

    private static string ComputeSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static JsonDocument LoadEvidence(string fileName)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), fileName)));

    private static string ReleaseEvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "docs", "release-evidence");

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
