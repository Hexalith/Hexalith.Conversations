// <copyright file="ProjectionReadStorePopulationProofValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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

    private static readonly string[] ExpectedTestBindingPaths =
    [
        "tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs",
        "tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs",
        "tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj",
        "tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/SmC2BaselineReconstructionValidationTest.cs",
        "tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs",
        "tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs",
        "tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionGatewayDispatchLiveTests.cs",
        "tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs",
        "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs",
        "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionDurableEventCoverageTest.cs",
        "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionHandlerTest.cs",
        "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs",
        "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs",
        "tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadStoreFailClosedTest.cs",
        "tests/Hexalith.Conversations.Server.Tests/Projections/ProjectionIndexSnapshotTestExtensions.cs",
    ];

    [Fact]
    public void ProofShouldBindExactProductionRouteKeysAndBoundedOutcomes()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        JsonElement proof = proofDocument.RootElement;
        JsonElement boundary = proof.GetProperty("productionBoundary");

        proof.GetProperty("artifactVersion").GetString().ShouldBe("projection-read-store-population-proof-v2");
        proof.GetProperty("baselineRevision").GetString().ShouldBe("29def441408becfbbbdc5c59b9af14a7717cb21f");

        // The aggregate result is deliberately NOT pinned here. This test binds route keys and bounded
        // outcomes; pinning "fail" made the suite require that AC1 stay unmet, so repairing HP-LIST/HP-OPEN
        // and regenerating the evidence would have turned this red (pass-10 review). The result is derived
        // from the measured rows in PostEvidenceShouldDeriveItsVerdictFromTheRawSamples, and story
        // completion is gated by AFailingProofResultMustBlockStoryCompletion.
        proof.GetProperty("result").GetString().ShouldBeOneOf("pass", "fail");
        boundary.GetProperty("route").GetString().ShouldBe("conversation/conversation-read-model");
        boundary.GetProperty("rebuildSemantics").GetString().ShouldBe("FullReplay");
        boundary.GetProperty("configuredStateStore").GetString().ShouldBe("statestore");
        boundary.GetProperty("detailKeyTemplate").GetString()
            .ShouldBe("projection:conversations:{base64url(tenantId)}:{base64url(conversationId)}");
        boundary.GetProperty("tenantIndexKeyTemplate").GetString()
            .ShouldBe("projection:conversations-index:{base64url(tenantId)}");
        boundary.GetProperty("dispatchLedgerKeyTemplate").GetString()
            .ShouldBe("projection:conversations-dispatch:{sha256(dispatchId)}");
        boundary.GetProperty("detailKey").GetString()
            .ShouldBe("projection:conversations:dGVuYW50LWxpdmUtMDAx:Y29udmVyc2F0aW9uLWxpdmUtMDAx");
        boundary.GetProperty("tenantIndexKey").GetString()
            .ShouldBe("projection:conversations-index:dGVuYW50LWxpdmUtMDAx");
        boundary.GetProperty("dispatchLedgerKey").GetString()
            .ShouldBe("projection:conversations-dispatch:592809543882c311d1172f79e6f9e9887f8f0f0433c2b3505f996200e3116bb1");
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
        scenarios["accepted-append"].GetProperty("reasonCode").GetString().ShouldBe("None");
        scenarios["accepted-append"].GetProperty("detailWrites").GetInt32().ShouldBe(1);
        scenarios["accepted-append"].GetProperty("tenantIndexRows").GetInt32().ShouldBe(1);
        scenarios["accepted-append"].GetProperty("detailQueryState").GetString().ShouldBe("Current");
        scenarios["accepted-append"].GetProperty("listQueryState").GetString().ShouldBe("Current");
        scenarios["accepted-append"].GetProperty("detailLastAppliedEventPosition").GetInt64().ShouldBe(1);
        scenarios["accepted-append"].GetProperty("listLastAppliedEventPosition").GetInt64().ShouldBe(1);
        scenarios["stable-duplicate"].GetProperty("tenantIndexRows").GetInt32().ShouldBe(1);
        scenarios["stable-duplicate"].GetProperty("handlerStatus").GetString().ShouldBe("Completed");
        scenarios["stable-duplicate"].GetProperty("reasonCode").GetString().ShouldBe("None");
        scenarios["stable-duplicate"].GetProperty("detailWrites").GetInt32().ShouldBe(1);
        scenarios["stable-duplicate"].GetProperty("detailQueryState").GetString().ShouldBe("Current");
        scenarios["stable-duplicate"].GetProperty("listQueryState").GetString().ShouldBe("Current");
        scenarios["second-write-failure"].GetProperty("handlerStatus").GetString().ShouldBe("Retryable");
        scenarios["second-write-failure"].GetProperty("reasonCode").GetString().ShouldBe("PartialRetry");
        scenarios["second-write-failure"].GetProperty("falseCurrentObserved").GetBoolean().ShouldBeFalse();
        scenarios["second-write-failure"].GetProperty("detailQueryState").GetString().ShouldBe("Rebuilding");
        scenarios["second-write-failure"].GetProperty("listQueryState").GetString().ShouldBe("Rebuilding");
        scenarios["second-write-retry"].GetProperty("handlerStatus").GetString().ShouldBe("Completed");
        scenarios["second-write-retry"].GetProperty("reasonCode").GetString().ShouldBe("None");
        scenarios["second-write-retry"].GetProperty("tenantIndexRows").GetInt32().ShouldBe(1);
        scenarios["second-write-retry"].GetProperty("detailQueryState").GetString().ShouldBe("Current");
        scenarios["second-write-retry"].GetProperty("listQueryState").GetString().ShouldBe("Current");
        scenarios["unavailable-store"].GetProperty("handlerStatus").GetString().ShouldBe("Indeterminate");
        scenarios["unavailable-store"].GetProperty("rawStorageDetailExposed").GetBoolean().ShouldBeFalse();
        scenarios["unavailable-store"].GetProperty("reasonCode").GetString().ShouldBe("HandlerFailure");
        scenarios["cross-tenant-input"].GetProperty("writes").GetInt32().ShouldBe(0);
        scenarios["cross-tenant-input"].GetProperty("handlerStatus").GetString().ShouldBe("Failed");
        scenarios["cross-tenant-input"].GetProperty("reasonCode").GetString().ShouldBe("HandlerFailure");
        scenarios["cross-tenant-input"].GetProperty("leakageObserved").GetBoolean().ShouldBeFalse();
        // An erased tenant index is deliberately indistinguishable from a never-populated tenant, so the list
        // honestly reports Current-and-empty rather than Rebuilding (the bound live test asserts this state).
        scenarios["derived-state-deletion"].GetProperty("detailQueryState").GetString().ShouldBe("non-current");
        scenarios["derived-state-deletion"].GetProperty("listQueryState").GetString().ShouldBe("Current");
        scenarios["derived-state-deletion"].GetProperty("queryTimeBackfillObserved").GetBoolean().ShouldBeFalse();
        scenarios["full-replay"].GetProperty("handlerStatus").GetString().ShouldBe("Completed");
        scenarios["full-replay"].GetProperty("reasonCode").GetString().ShouldBe("None");
        scenarios["full-replay"].GetProperty("batchOperationCount").GetInt32().ShouldBe(3);
        scenarios["full-replay"].GetProperty("restoredDispatchLedgerKey").GetString()
            .ShouldBe(boundary.GetProperty("dispatchLedgerKey").GetString());
        scenarios["full-replay"].GetProperty("detailQueryState").GetString().ShouldBe("Current");
        scenarios["full-replay"].GetProperty("listQueryState").GetString().ShouldBe("Current");
        scenarios["full-replay"].GetProperty("queryResultsEquivalentToPreDeletion").GetBoolean().ShouldBeTrue();

        JsonElement hosting = proof.GetProperty("hostingEvidence");
        hosting.GetProperty("isPackable").GetBoolean().ShouldBeFalse();
        hosting.GetProperty("isPublishable").GetBoolean().ShouldBeFalse();
        hosting.GetProperty("projectResources").EnumerateArray().Select(value => value.GetString()).ShouldBe(
            ["conversations", "conversations-admin-web", "eventstore"]);
        hosting.GetProperty("conversationsServiceDefaultsRemoved").GetBoolean().ShouldBeTrue();

        JsonElement promotion = proof.GetProperty("eventStorePromotion");
        promotion.GetProperty("commit").GetString().ShouldBe("defb426f0bd9e3bd1247bc7149605b4bb6ef70d0");
        promotion.GetProperty("remoteContainsCommit").GetBoolean().ShouldBeTrue();
        GitIn("references/Hexalith.EventStore", "for-each-ref", "--contains", promotion.GetProperty("commit").GetString()!, "--format=%(refname)", "refs/remotes/")
            .ShouldNotBeNullOrWhiteSpace("the recorded EventStore commit must remain on a locally known remote-tracking ref");
        promotion.GetProperty("submoduleWorktreeClean").GetBoolean().ShouldBeTrue();
        promotion.GetProperty("requiredGitlinkMode").GetString().ShouldBe("160000");
        promotion.GetProperty("requiredUmbrellaGitlinkCommit").GetString().ShouldBe(
            promotion.GetProperty("commit").GetString());

        // The rebinding delta is explicit so the platform additions used by the final implementation
        // remain distinguishable from later unrelated submodule commits.
        JsonElement delta = promotion.GetProperty("promotedCapabilityDelta");
        delta.GetProperty("previouslyRecordedCommit").GetString().ShouldBe("4c63f5d3e8089a85891cdbf8d87ce82ee445354a");
        delta.GetProperty("currentCommit").GetString().ShouldBe(promotion.GetProperty("commit").GetString());
        delta.GetProperty("storyCapabilityCommit").GetString().ShouldBe("bb4c81d4eaf33521afc00bdfa634e1c2e790f796");
        delta.GetProperty("promotedCapabilityFilesChanged")
            .EnumerateArray()
            .Select(value => value.GetString())
            .Order(StringComparer.Ordinal)
            .ShouldBe(
            [
                "src/Hexalith.EventStore.DomainService/DomainProjectionDispatcher.cs",
                "src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs",
                "src/Hexalith.EventStore.DomainService/IAsyncDomainProjectionReconciliationHandler.cs",
                "src/Hexalith.EventStore.Server/Projections/NamedProjectionDispatchCoordinator.cs",
            ]);

        JsonElement promotionGate = promotion.GetProperty("umbrellaMechanicalGate");
        promotionGate.GetProperty("schema").GetString().ShouldBe("submodule-promotion-gate/v1");
        promotionGate.GetProperty("result").GetString().ShouldBe("pass");
        promotionGate.GetProperty("baseline").GetString().ShouldBe("29def441408becfbbbdc5c59b9af14a7717cb21f");
        promotionGate.GetProperty("candidate").GetString().ShouldBe("b261fe209c4ca6c966f4bd2a78a62a2d83ddde08");
        promotionGate.GetProperty("recordedGitlink").GetString().ShouldBe(promotion.GetProperty("commit").GetString());
        promotionGate.GetProperty("recordedMode").GetString().ShouldBe("160000");
        promotionGate.GetProperty("blockers").GetArrayLength().ShouldBe(0);
        promotionGate.GetProperty("warnings").GetArrayLength().ShouldBe(4);

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

        string[] changedGitlinks =
        [
            .. promotionGate.GetProperty("changedGitlinks")
                .EnumerateArray()
                .Select(value => value.GetString()!)
                .Order(StringComparer.Ordinal),
        ];
        changedGitlinks.ShouldBe(
        [
            "references/Hexalith.AI.Tools",
            "references/Hexalith.Builds",
            "references/Hexalith.Commons",
            "references/Hexalith.EventStore",
            "references/Hexalith.FrontComposer",
            "references/Hexalith.Memories",
            "references/Hexalith.Tenants",
        ]);

        Dictionary<string, string> warnings = promotionGate.GetProperty("warnings")
            .EnumerateArray()
            .ToDictionary(
                warning => warning.GetProperty("path").GetString()!,
                warning => warning.GetProperty("code").GetString()!,
                StringComparer.Ordinal);
        warnings.ShouldBe(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["references/Hexalith.AI.Tools"] = "UNDECLARED_GITLINK_CHANGE",
            ["references/Hexalith.Commons"] = "UNDECLARED_GITLINK_CHANGE",
            ["references/Hexalith.FrontComposer"] = "UNDECLARED_GITLINK_CHANGE",
            ["references/Hexalith.Memories"] = "UNDECLARED_GITLINK_CHANGE",
        });

        Dictionary<string, JsonElement> evaluatedByPath = promotionGate.GetProperty("evaluated")
            .EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("path").GetString()!, StringComparer.Ordinal);
        evaluatedByPath.Keys.Order(StringComparer.Ordinal).ShouldBe(changedGitlinks);

        foreach ((string path, JsonElement evaluated) in evaluatedByPath)
        {
            evaluated.GetProperty("initialized").GetBoolean().ShouldBeTrue(path);
            evaluated.GetProperty("clean").GetBoolean().ShouldBeTrue(path);
            JsonElement remoteAvailable = evaluated.GetProperty("remoteAvailable");
            if (declaredScope.Contains(path, StringComparer.Ordinal))
            {
                remoteAvailable.GetBoolean().ShouldBeTrue(path);
                GitIn(path, "for-each-ref", "--contains", evaluated.GetProperty("recordedGitlink").GetString()!, "--format=%(refname)", "refs/remotes/")
                    .ShouldNotBeNullOrWhiteSpace($"{path} must contain the recorded commit on a locally known remote-tracking ref");
            }
            else
            {
                remoteAvailable.ValueKind.ShouldBe(JsonValueKind.Null, path);
            }

            evaluated.GetProperty("recordedMode").GetString().ShouldBe("160000", path);
            evaluated.GetProperty("recordedGitlink").GetString().ShouldBe(evaluated.GetProperty("head").GetString(), path);
        }
    }

    /// <summary>
    /// Re-derives the recorded promotion from the working tree so the evidence cannot go quietly stale.
    /// </summary>
    /// <remarks>
    /// A gate result cannot name the commit that contains it, so the evidence pins the last revision that moved
    /// a gitlink or production source. That pin is only worth something if a later revision moving a root
    /// gitlink turns it red, which is what this test enforces.
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
        Git("diff", "--name-only", $"{candidate}..HEAD", "--", "src/")
            .ShouldBeEmpty("no production source may move after the recorded promotion candidate");
        Git("status", "--porcelain=v1", "--", "src/")
            .ShouldBeEmpty("the proof cannot bind a candidate while production source changes remain uncommitted");

        // Scoped to the DECLARED promotion paths, not to every evaluated gitlink. The gate evaluates all
        // seven changed gitlinks, but four of them (AI.Tools, Commons, FrontComposer, Memories) are
        // deliberately undeclared and disclosed as non-blocking warnings — binding their worktrees here
        // turned this module's own conformance suite red on drift the story has already accepted, and on
        // any stray untracked file in a sibling worktree (pass-10 review).
        HashSet<string> declaredPaths =
        [
            .. promotionGate.GetProperty("declaredScope")
                .EnumerateArray()
                .Select(entry => entry.GetProperty("path").GetString()!),
        ];

        declaredPaths.ShouldNotBeEmpty("the recorded gate must declare the promotion scope it evaluated");

        foreach (JsonElement evaluated in promotionGate.GetProperty("evaluated").EnumerateArray())
        {
            string path = evaluated.GetProperty("path").GetString()!;
            string recordedGitlink = evaluated.GetProperty("recordedGitlink").GetString()!;
            Git("rev-parse", $"HEAD:{path}").ShouldBe(recordedGitlink, path);

            if (!declaredPaths.Contains(path))
            {
                continue;
            }

            // The committed gitlink alone is not enough: a submodule worktree checked out away from it, or
            // dirty, changes every compile input while the umbrella diff stays empty. The worktree state is
            // re-derived live on every run rather than trusted from the recorded JSON.
            GitIn(path, "rev-parse", "HEAD").ShouldBe(
                recordedGitlink,
                $"the {path} worktree must be checked out at the recorded gitlink");
            GitIn(path, "status", "--porcelain").ShouldBeEmpty(
                $"the {path} worktree must be clean so measurements bind the recorded promotion");
        }

        Git("rev-parse", "HEAD:references/Hexalith.EventStore")
            .ShouldBe(promotion.GetProperty("requiredUmbrellaGitlinkCommit").GetString());
    }

    /// <summary>
    /// Mechanically links the SM-C2 proof result to story completion: a 430-green conformance run must never
    /// be readable as "AC1 met" while the bound proof records <c>fail</c>. While the story record is still
    /// being worked the failing proof is the disclosed open blocker; the moment the record leaves
    /// <c>in-progress</c> this guard demands a passing proof (pass-7 review decision, 2026-07-30).
    /// </summary>
    [Fact]
    public void AFailingProofResultMustBlockStoryCompletion()
    {
        string storyPath = Path.Combine(
            FindRepositoryRoot(),
            "_bmad-output",
            "implementation-artifacts",
            "6-2-migrate-conversations-to-platform-owned-hosting.md");
        File.Exists(storyPath).ShouldBeTrue(storyPath);
        string story = File.ReadAllText(storyPath).Replace("\r\n", "\n", StringComparison.Ordinal);
        Match status = Regex.Match(story, "^status: '([a-z-]+)'$", RegexOptions.Multiline);
        status.Success.ShouldBeTrue("the Story 6.2 record must declare a frontmatter status");
        // `review` is allowed: the documented process is "Dev moves story to 'review', then runs
        // code-review", so excluding it made this completion guard block the story from entering the very
        // status its own workflow requires (pass-10 review). The guard is completion-scoped by design.
        if (status.Groups[1].Value is "backlog" or "ready-for-dev" or "in-progress" or "review")
        {
            return;
        }

        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        proofDocument.RootElement.GetProperty("result").GetString().ShouldBe(
            "pass",
            "a story past in-progress may not carry a failing SM-C2 proof; regenerate the evidence with every hot path within the frozen threshold");
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

    [Fact]
    public void Ac5AndAc6ClaimsShouldBeBoundToPassingMachineReadableRuns()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        JsonElement proof = proofDocument.RootElement;
        Dictionary<string, XunitRun> runs = ValidateRunArtifacts(proof.GetProperty("runArtifacts"));

        foreach (JsonElement scenario in proof.GetProperty("dispatchEvidence").EnumerateArray())
        {
            AssertScenarioRunPassed(scenario, runs);
        }

        JsonElement gateway = proof.GetProperty("gatewayBoundaryEvidence");
        foreach (JsonElement scenario in gateway.GetProperty("scenarios").EnumerateArray())
        {
            AssertScenarioRunPassed(scenario, runs);
        }

        XunitRun gatewayRun = runs["gateway-boundary"];
        gateway.GetProperty("run").GetProperty("passed").GetInt32().ShouldBe(gatewayRun.Passed);
        gateway.GetProperty("run").GetProperty("failed").GetInt32().ShouldBe(gatewayRun.Failed);
        gateway.GetProperty("run").GetProperty("skipped").GetInt32().ShouldBe(gatewayRun.Skipped);
    }

    private static string Git(params string[] arguments)
        => RunGit(FindRepositoryRoot(), arguments);

    private static string GitIn(string relativePath, params string[] arguments)
        => RunGit(Path.Combine(FindRepositoryRoot(), relativePath), arguments);

    private static string RunGit(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("git could not be started.");
        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(milliseconds: 120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("git did not complete within 120 seconds.");
        }

        string standardOutput = standardOutputTask.GetAwaiter().GetResult();
        string standardError = standardErrorTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed in {workingDirectory} with exit code {process.ExitCode}.{Environment.NewLine}{standardError}");
        }

        return standardOutput.Trim();
    }

    [Fact]
    public void ProofSourceAndSignedV1BindingsShouldRemainByteIdentical()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        JsonElement proof = proofDocument.RootElement;

        JsonElement sourceBoundary = proof.GetProperty("sourceBoundary");
        ValidateBindings(sourceBoundary.GetProperty("productionBindings"));
        ValidateBindings(sourceBoundary.GetProperty("testBindings"));
        ValidateBindings(sourceBoundary.GetProperty("platformBindings"));
        ValidateSourceBoundary(proof, sourceBoundary);
        ValidateBindings(proof.GetProperty("immutableSignedV1Bindings"));

        using JsonDocument decisionDocument = LoadEvidence("success-metric-report-and-attestation-v1-release-owner-decision.json");
        JsonElement attestation = decisionDocument.RootElement.GetProperty("sourceAttestation");
        attestation.GetProperty("artifactSha256").GetString().ShouldBe(
            "062ca0c7bc94279007077bda59eae867d21c12da2ffc0b59a0f389b99067e0fe");
        attestation.GetProperty("summarySha256").GetString().ShouldBe(
            "aa7e52c11ce36fc2c9ea953e275c654e7f312016c990cb20be16666d87f9a2cd");
    }

    [Fact]
    public void SmC2PostShouldUseIdenticalEnvelopeAndRecordEveryMechanicalP95Result()
    {
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        using JsonDocument baselineDocument = LoadEvidence(BaselineFileName);
        using JsonDocument postDocument = LoadEvidence(PostFileName);
        JsonElement proofPerformance = proofDocument.RootElement.GetProperty("performanceEvidence");
        JsonElement promotionGate = proofDocument.RootElement
            .GetProperty("eventStorePromotion")
            .GetProperty("umbrellaMechanicalGate");
        JsonElement baseline = baselineDocument.RootElement;
        JsonElement post = postDocument.RootElement;

        string postSourceCommit = post.GetProperty("sourceCommit").GetString()!;
        Git("rev-parse", $"{postSourceCommit}^{{commit}}").ShouldBe(postSourceCommit);
        Git("merge-base", "--is-ancestor", postSourceCommit, "HEAD").ShouldBeEmpty();
        postSourceCommit.ShouldBe(promotionGate.GetProperty("candidate").GetString());
        DateTimeOffset.Parse(post.GetProperty("capturedAtUtc").GetString()!, System.Globalization.CultureInfo.InvariantCulture)
            .ShouldBeGreaterThanOrEqualTo(DateTimeOffset.Parse(
                Git("show", "-s", "--format=%cI", postSourceCommit),
                System.Globalization.CultureInfo.InvariantCulture));
        Git("rev-parse", $"{postSourceCommit}:references/Hexalith.EventStore")
            .ShouldBe(post.GetProperty("eventStoreWorktreeBaseCommit").GetString());

        ValidateBinding(proofPerformance.GetProperty("baseline"));
        ValidateBinding(proofPerformance.GetProperty("post"));
        proofPerformance.GetProperty("rowsTotal").GetInt32().ShouldBe(4);

        post.GetProperty("fixture").GetRawText().ShouldBe(baseline.GetProperty("fixture").GetRawText());
        post.GetProperty("environment").GetRawText().ShouldBe(baseline.GetProperty("environment").GetRawText());
        post.GetProperty("command").GetString().ShouldBe(baseline.GetProperty("command").GetString());
        post.GetProperty("workloadManifest").GetRawText().ShouldBe(baseline.GetProperty("workloadManifest").GetRawText());

        Dictionary<string, double[]> baselineRunnerSamples = ReadBenchmarkRunnerSamples(baseline.GetProperty("runArtifact"));
        Dictionary<string, double[]> postRunnerSamples = ReadBenchmarkRunnerSamples(post.GetProperty("runArtifact"));

        Dictionary<string, JsonElement> baselineRows = RowsByHotPath(baseline);
        Dictionary<string, JsonElement> postRows = RowsByHotPath(post);
        baselineRows.Keys.Order(StringComparer.Ordinal).ShouldBe(ExpectedHotPaths);
        postRows.Keys.Order(StringComparer.Ordinal).ShouldBe(ExpectedHotPaths);

        int rowsPassing = 0;
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
            baselineRaw.ShouldBe(baselineRunnerSamples[hotPath]);
            postRaw.ShouldBe(postRunnerSamples[hotPath]);
            baselineRaw.ShouldAllBe(value => value > 0);
            postRaw.ShouldAllBe(value => value > 0);
            baselineRow.GetProperty("p95Microseconds").GetDouble().ShouldBe(baselineP95, tolerance: 0.0000005);
            postRow.GetProperty("p95Microseconds").GetDouble().ShouldBe(postP95, tolerance: 0.0000005);
            postRow.GetProperty("baselineP95Microseconds").GetDouble().ShouldBe(baselineP95, tolerance: 0.0000005);
            postRow.GetProperty("maximumAllowedP95Microseconds").GetDouble()
                .ShouldBe(baselineP95 * 1.05, tolerance: 0.0000005);
            bool passed = postP95 <= baselineP95 * 1.05;
            postRow.GetProperty("result").GetString().ShouldBe(passed ? "pass" : "fail");
            if (passed)
            {
                rowsPassing++;
            }
        }

        // Derived, never pinned: the verdict follows the measured rows. Pinning rowsPassing == 2 and
        // result == "fail" made the conformance suite mechanically require that the SM-C2 regression stay
        // unrepaired — fixing HP-LIST/HP-OPEN would have turned five assertions red (pass-10 review).
        string expectedResult = rowsPassing == ExpectedHotPaths.Length ? "pass" : "fail";

        post.GetProperty("rowsPassing").GetInt32().ShouldBe(rowsPassing);
        post.GetProperty("rowsTotal").GetInt32().ShouldBe(ExpectedHotPaths.Length);
        post.GetProperty("result").GetString().ShouldBe(expectedResult);
        proofPerformance.GetProperty("rowsPassing").GetInt32().ShouldBe(rowsPassing);
        proofPerformance.GetProperty("result").GetString().ShouldBe(expectedResult);
    }

    [Fact]
    public void MarkdownShouldPresentTheAuthoritativeJsonBoundary()
    {
        string markdown = File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), ProofMarkdownFileName));

        // Derived from the authoritative JSON rather than pinned, so repairing SM-C2 does not turn this red.
        using JsonDocument proofDocument = LoadEvidence(ProofJsonFileName);
        string proofResult = proofDocument.RootElement.GetProperty("result").GetString()!;

        markdown.ShouldContain($"**Result:** {proofResult}", Case.Sensitive);
        markdown.ShouldContain("`conversation/conversation-read-model`", Case.Sensitive);
        markdown.ShouldContain(
            "`projection:conversations:{base64url(tenantId)}:{base64url(conversationId)}`",
            Case.Sensitive);
        markdown.ShouldContain("`projection:conversations-index:{base64url(tenantId)}`", Case.Sensitive);
        markdown.ShouldContain("defb426f0bd9e3bd1247bc7149605b4bb6ef70d0", Case.Sensitive);
        markdown.ShouldContain("b261fe209c4ca6c966f4bd2a78a62a2d83ddde08", Case.Sensitive);
        markdown.ShouldContain("zero blockers and four undeclared-gitlink warnings", Case.Sensitive);
        markdown.ShouldContain("Gateway production boundary (ADR 0003 Verification 1-2)", Case.Sensitive);
        if (proofResult == "fail")
        {
            markdown.ShouldContain("SM-C2 remains an open release blocker", Case.Sensitive);
        }

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

    private static Dictionary<string, double[]> ReadBenchmarkRunnerSamples(JsonElement binding)
    {
        ValidateBinding(binding);
        string path = Path.Combine(FindRepositoryRoot(), binding.GetProperty("path").GetString()!);
        XDocument report = XDocument.Load(path);
        XElement assembly = report.Descendants("assembly").Single();
        assembly.Attribute("passed")!.Value.ShouldBe("1");
        assembly.Attribute("failed")!.Value.ShouldBe("0");
        assembly.Attribute("skipped")!.Value.ShouldBe("0");
        string output = report.Descendants("output").Single().Value;
        var rows = new Dictionary<string, double[]>(StringComparer.Ordinal);
        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith("SM-C2|", StringComparison.Ordinal))
            {
                continue;
            }

            string[] parts = line.Split('|');
            parts.Length.ShouldBe(4, line);
            string hotPath = parts[1];
            const string RawPrefix = "raw-microseconds=";
            parts[2].ShouldStartWith(RawPrefix);
            double[] samples =
            [
                .. parts[2][RawPrefix.Length..]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
            ];
            const string P95Prefix = "p95-microseconds=";
            parts[3].ShouldStartWith(P95Prefix);
            double.Parse(parts[3][P95Prefix.Length..], System.Globalization.CultureInfo.InvariantCulture)
                .ShouldBe(P95(samples), tolerance: 0.0000005);
            rows.Add(hotPath, samples);
        }

        rows.Keys.Order(StringComparer.Ordinal).ShouldBe(ExpectedHotPaths);
        return rows;
    }

    private static void ValidateBindings(JsonElement bindings)
    {
        foreach (JsonElement binding in bindings.EnumerateArray())
        {
            ValidateBinding(binding);
        }
    }

    private static void ValidateSourceBoundary(JsonElement proof, JsonElement sourceBoundary)
    {
        string baseline = proof.GetProperty("baselineRevision").GetString()!;
        string candidate = proof.GetProperty("eventStorePromotion")
            .GetProperty("umbrellaMechanicalGate")
            .GetProperty("candidate")
            .GetString()!;

        var changedProduction = new HashSet<string>(StringComparer.Ordinal);
        var removedProduction = new HashSet<string>(StringComparer.Ordinal);
        // --no-renames keeps every rename as an explicit A+D pair so a vacated production path can never
        // silently drop out of both the changed and removed sets.
        foreach (string line in Git("diff", "--no-renames", "--name-status", $"{baseline}..{candidate}", "--", "src/")
                     .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] fields = line.Split('\t');
            fields.Length.ShouldBeGreaterThanOrEqualTo(2, line);
            string path = fields[^1];
            if (fields[0].StartsWith('D'))
            {
                removedProduction.Add(path);
            }
            else
            {
                changedProduction.Add(path);
            }
        }

        string[] recordedProduction =
        [
            .. sourceBoundary.GetProperty("productionBindings")
                .EnumerateArray()
                .Select(binding => binding.GetProperty("path").GetString()!)
                .Order(StringComparer.Ordinal),
        ];
        recordedProduction.ShouldBe(changedProduction.Order(StringComparer.Ordinal));

        string[] recordedRemoved =
        [
            .. sourceBoundary.GetProperty("removedProductionPaths")
                .EnumerateArray()
                .Select(path => path.GetString()!)
                .Order(StringComparer.Ordinal),
        ];
        recordedRemoved.ShouldBe(removedProduction.Order(StringComparer.Ordinal));

        string[] recordedTests =
        [
            .. sourceBoundary.GetProperty("testBindings")
                .EnumerateArray()
                .Select(binding => binding.GetProperty("path").GetString()!)
                .Order(StringComparer.Ordinal),
        ];
        recordedTests.ShouldBe(ExpectedTestBindingPaths.Order(StringComparer.Ordinal));
    }

    private static Dictionary<string, XunitRun> ValidateRunArtifacts(JsonElement artifacts)
    {
        var result = new Dictionary<string, XunitRun>(StringComparer.Ordinal);
        foreach (JsonElement artifact in artifacts.EnumerateArray())
        {
            ValidateBinding(artifact);
            string id = artifact.GetProperty("id").GetString()!;
            string path = Path.Combine(FindRepositoryRoot(), artifact.GetProperty("path").GetString()!);
            XDocument document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
            XElement assembly = document.Descendants("assembly").Single();
            string[] passedTests =
            [
                .. assembly.Descendants("test")
                    .Where(test => string.Equals((string?)test.Attribute("result"), "Pass", StringComparison.Ordinal))
                    .Select(test => (string?)test.Attribute("name"))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .Order(StringComparer.Ordinal),
            ];
            var run = new XunitRun(
                int.Parse(assembly.Attribute("passed")!.Value, System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(assembly.Attribute("failed")!.Value, System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(assembly.Attribute("skipped")!.Value, System.Globalization.CultureInfo.InvariantCulture),
                passedTests);
            artifact.GetProperty("passed").GetInt32().ShouldBe(run.Passed, id);
            artifact.GetProperty("failed").GetInt32().ShouldBe(run.Failed, id);
            artifact.GetProperty("skipped").GetInt32().ShouldBe(run.Skipped, id);

            // Count consistency is not enough: every bound artifact must itself be failure-free, or a
            // honestly-transcribed red run could still anchor the proof.
            run.Failed.ShouldBe(0, $"{id} must contain no failing test");
            run.Skipped.ShouldBe(0, $"{id} must contain no skipped test");
            result.Add(id, run);
        }

        result.Keys.Order(StringComparer.Ordinal).ShouldBe(
            ["deterministic-dispatch", "gateway-boundary", "population-boundary"]);
        return result;
    }

    private static void AssertScenarioRunPassed(JsonElement scenario, IReadOnlyDictionary<string, XunitRun> runs)
    {
        string artifactId = scenario.GetProperty("runArtifactId").GetString()!;
        string testCase = scenario.GetProperty("testCase").GetString()!;
        runs.ContainsKey(artifactId).ShouldBeTrue(artifactId);
        runs[artifactId].PassedTests.ShouldContain(testCase, $"{testCase} must be a passing test in {artifactId}");
        AssertBoundTestStillExists(testCase);
    }

    /// <summary>
    /// The bound run artifacts are committed snapshots, so deleting, renaming, or skipping the test a
    /// scenario claims to rest on changed no assertion in this class (pass-10 review). The method the
    /// artifact names must still be declared in the test sources for the binding to mean anything.
    /// </summary>
    /// <param name="testCase">The fully qualified test name recorded by the scenario.</param>
    private static void AssertBoundTestStillExists(string testCase)
    {
        string methodName = testCase[(testCase.LastIndexOf('.') + 1)..];
        methodName.ShouldNotBeNullOrWhiteSpace(testCase);

        string testsRoot = Path.Combine(FindRepositoryRoot(), "tests");
        bool declared = Directory
            .EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Any(file => File.ReadAllText(file).Contains(methodName, StringComparison.Ordinal));

        declared.ShouldBeTrue(
            $"the bound run artifact names {testCase}, but no test source declares {methodName}; a renamed or "
            + "deleted bound test must not leave the committed artifact silently authoritative");
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

    private sealed record XunitRun(int Passed, int Failed, int Skipped, IReadOnlyCollection<string> PassedTests);
}
