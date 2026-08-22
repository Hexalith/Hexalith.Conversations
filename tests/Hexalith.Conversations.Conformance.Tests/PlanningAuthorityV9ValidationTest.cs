// <copyright file="PlanningAuthorityV9ValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Validates the candidate-bound V14 companion publication and historical checkpoints.
/// </summary>
public sealed class PlanningAuthorityV9ValidationTest
{
    private const string ArchitectureAuthority = "conversations-architecture-2026-08-18-v14";
    private const string BaseArchitectureAuthority = "conversations-architecture-2026-08-03-v10";
    private const string ArchitecturePath = "_bmad-output/planning-artifacts/architecture.md";
    private const string BundlePath = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json";
    private const string BaseEpicAuthority = "epic-6-authority-2026-08-03-v10";
    private const string EpicAuthority = "epic-6-authority-2026-08-18-v14";
    private const string V12ArchitectureAuthority = "conversations-architecture-2026-08-04-v12";
    private const string V12EpicAuthority = "epic-6-authority-2026-08-04-v12";
    private const string EpicsPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md";
    private const string GraphPath = "_bmad-output/planning-artifacts/v9-execution-graph-v1.json";
    private const string RemediationPath = "_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json";
    private const string SlicePath = "_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json";
    private const string SprintPath = "_bmad-output/implementation-artifacts/sprint-status.yaml";
    private const string SupersessionPath = "_bmad-output/planning-artifacts/v9-supersession-map-v1.json";
    private const string UxMapPath = "_bmad-output/planning-artifacts/ux-requirement-map.md";
    private const string V9ArchitectureDigest = "4686212387189e78f98de5352d12eb8544d1a9f78c97dfc446266fa3d4d3f3d9";
    private const string V9EpicDigest = "e7d6ea5759c12ab70f21b472656828bb4e5bcce2023d845f06a40cf1373d1c9d";
    private const string V10ArchitectureDigest = "893315bff3f12d7b949dbeae2a2dfbb301023461ad62c0c6066480a87700774b";
    private const string V10EpicDigest = "3c33462d0bc28f9fec36e571d7dcf4a60c77d02c94bd3675528a05d704d07588";
    private const string V11ArchitectureDigest = "a97385c11b92fb95f1acf7f4ba370404c4781855581c81a427ed6895a5a4c4d1";
    private const string V11EpicDigest = "6c9bd7164ef35e4093d69226a5988fe73f5400aab3049911a3298b0987d79f19";
    private const string V12ArchitectureDigest = "3050b326c5759fc51bc0e800944b0a1a591ab1782f6798f12abfdc10051b5796";
    private const string V12EpicDigest = "39f1b51920e4866c47586caf549aafaec5678639b64c64b4b235788fce76e878";
    private const string V13ArchitectureDigest = "c7d5c867385f22e359c5367fe2851fc4f7d016e0f28c04b5283b7b5ad604605a";
    private const string V14ArchitectureDigest = "d33d977fda0776377684439bb7e78769a6b9a0279c293b8a08e44dfad8466dc5";
    private const string V14EpicDigest = "acd5c07c72d5145bb6477877cab21af710beb8cf172ccfde66837992e41c35c1";

    /// <summary>
    /// Proves V9-V13 remain byte-identical and V14 closes the authority chain.
    /// </summary>
    [Fact]
    public void V14AuthorityShouldPreservePriorBlocksAndPublishEpic16()
    {
        string epics = Read(EpicsPath);
        string architecture = Read(ArchitecturePath);
        string v9Epic = ExtractMarkerBlock(
            epics,
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN",
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:END");
        string v9Architecture = ExtractMarkerBlock(
            architecture,
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V9:BEGIN",
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V9:END");
        string v10Epic = ExtractMarkerBlock(
            epics,
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V10:BEGIN",
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V10:END");
        string v10Architecture = ExtractMarkerBlock(
            architecture,
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V10:BEGIN",
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V10:END");
        string v11Epic = ExtractMarkerBlock(
            epics,
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:BEGIN",
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:END");
        string v11Architecture = ExtractMarkerBlock(
            architecture,
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V11:BEGIN",
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V11:END");
        string v12Epic = ExtractMarkerBlock(
            epics,
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V12:BEGIN",
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V12:END");
        string v12Architecture = ExtractMarkerBlock(
            architecture,
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V12:BEGIN",
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V12:END");
        string v13Architecture = ExtractMarkerBlock(
            architecture,
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V13:BEGIN",
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V13:END");
        string v14Epic = ExtractMarkerBlock(
            epics,
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V14:BEGIN",
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V14:END");
        string v14Architecture = ExtractMarkerBlock(
            architecture,
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V14:BEGIN",
            "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V14:END");

        Encoding.UTF8.GetByteCount(v9Epic).ShouldBe(188677);
        Sha256(Encoding.UTF8.GetBytes(v9Epic)).ShouldBe(V9EpicDigest);
        Encoding.UTF8.GetByteCount(v9Architecture).ShouldBe(18270);
        Sha256(Encoding.UTF8.GetBytes(v9Architecture)).ShouldBe(V9ArchitectureDigest);
        Encoding.UTF8.GetByteCount(v10Epic).ShouldBe(8746);
        Sha256(Encoding.UTF8.GetBytes(v10Epic)).ShouldBe(V10EpicDigest);
        Encoding.UTF8.GetByteCount(v10Architecture).ShouldBe(3846);
        Sha256(Encoding.UTF8.GetBytes(v10Architecture)).ShouldBe(V10ArchitectureDigest);
        Encoding.UTF8.GetByteCount(v11Epic).ShouldBe(5474);
        Sha256(Encoding.UTF8.GetBytes(v11Epic)).ShouldBe(V11EpicDigest);
        Encoding.UTF8.GetByteCount(v11Architecture).ShouldBe(3042);
        Sha256(Encoding.UTF8.GetBytes(v11Architecture)).ShouldBe(V11ArchitectureDigest);
        Encoding.UTF8.GetByteCount(v12Epic).ShouldBe(6207);
        Sha256(Encoding.UTF8.GetBytes(v12Epic)).ShouldBe(V12EpicDigest);
        Encoding.UTF8.GetByteCount(v12Architecture).ShouldBe(6075);
        Sha256(Encoding.UTF8.GetBytes(v12Architecture)).ShouldBe(V12ArchitectureDigest);
        Encoding.UTF8.GetByteCount(v13Architecture).ShouldBe(17857);
        Sha256(Encoding.UTF8.GetBytes(v13Architecture)).ShouldBe(V13ArchitectureDigest);
        Encoding.UTF8.GetByteCount(v14Epic).ShouldBe(11093);
        Sha256(Encoding.UTF8.GetBytes(v14Epic)).ShouldBe(V14EpicDigest);
        Encoding.UTF8.GetByteCount(v14Architecture).ShouldBe(3873);
        Sha256(Encoding.UTF8.GetBytes(v14Architecture)).ShouldBe(V14ArchitectureDigest);
        CountOccurrences(
            v11Epic,
            "### Story 7.1 V11 Schema-Checkpoint Amendment: Authorize A Non-Story Slice").ShouldBe(1);
        CountOccurrences(v10Epic, "Story 10.3 V10 Amendment").ShouldBe(1);
        CountOccurrences(v10Epic, "Story 10.4 V10 Amendment").ShouldBe(1);
        v10Epic.ShouldContain("AC-10.4-09");
        v10Epic.ShouldContain("V9-EVIDENCE-WORKFLOWS-v2");
        v10Epic.ShouldContain("V9-EVIDENCE-GUIDANCE-v2");
        v10Epic.ShouldContain("Global implementation hold:** `ACTIVE`");
        v10Architecture.ShouldContain("BMAD `6.10.1n46`");
        v10Architecture.ShouldContain("Every other v9 obligation remains unchanged");
        v11Epic.ShouldContain("Story 7.1 V11 Schema-Checkpoint Amendment");
        v11Epic.ShouldContain("7.1-SCHEMAS");
        v11Epic.ShouldContain("epic-6-retrospective: done");
        v11Architecture.ShouldContain("kind\n`checkpoint`");
        v11Architecture.ShouldContain("There is no scoped exception state");
        v12Epic.ShouldContain("### E6-REMEDIATION: Own A1-A3 Before Independent IR-0");
        v12Epic.ShouldContain("A1-A3 are all required before IR-0 may run");
        v12Architecture.ShouldContain("PC-PUBLICATION -> E6-REMEDIATION -> IR-0");
        v12Architecture.ShouldContain("The graph has exactly 33 nodes");
        v14Epic.ShouldContain("### Story 16.1:");
        v14Epic.ShouldContain("### Story 16.2:");
        v14Epic.ShouldContain("### Story 16.3:");
        v14Epic.ShouldContain("DC-9");
        v14Epic.ShouldContain("DC-10");
        v14Epic.ShouldContain("DC-11");
        v14Architecture.ShouldContain("38 nodes and 61");
        v14Architecture.ShouldContain("global execution predicate");
    }

    /// <summary>
    /// Proves every companion and bundle row binds one committed planning candidate.
    /// </summary>
    [Fact]
    public void AuthorityBundleShouldBindEveryCompanionWithoutSelfReference()
    {
        using JsonDocument bundleDocument = JsonDocument.Parse(Read(BundlePath));
        JsonElement bundle = bundleDocument.RootElement;
        string candidate = bundle.GetProperty("planningCandidate").GetString()!;
        candidate.ShouldMatch("^[0-9a-f]{40}$");
        bundle.GetProperty("authorities").GetProperty("epic").GetString().ShouldBe(EpicAuthority);
        bundle.GetProperty("authorities").GetProperty("architecture").GetString().ShouldBe(ArchitectureAuthority);
        bundle.GetProperty("implementationHold").GetString().ShouldBe("ACTIVE");
        bundle.GetProperty("epic5ActionA5").GetString().ShouldBe("open");

        JsonElement[] artifacts = bundle.GetProperty("artifacts").EnumerateArray().ToArray();

        // Exact bundle membership is pinned so a companion cannot silently join or leave.
        artifacts.Length.ShouldBe(101);
        string[] paths = artifacts.Select(row => row.GetProperty("path").GetString()!).ToArray();
        paths.ShouldBe(paths.OrderBy(path => path, StringComparer.Ordinal));
        paths.Distinct(StringComparer.Ordinal).Count().ShouldBe(paths.Length);
        paths.ShouldNotContain(BundlePath);
        paths.ShouldAllBe(path => !path.Contains("implementation-readiness-report", StringComparison.OrdinalIgnoreCase));
        paths.ShouldAllBe(path => !path.EndsWith("implementation-hold-v1.json", StringComparison.Ordinal));
        foreach (JsonElement artifact in artifacts)
        {
            string path = artifact.GetProperty("path").GetString()!;
            byte[] expectedBytes = artifact.GetProperty("source").GetString() == "candidate"
                ? ReadCandidateBytes(candidate, path)
                : ReadBytes(path);
            artifact.GetProperty("sha256").GetString().ShouldBe(Sha256(expectedBytes), path);
            if (path.EndsWith(".json", StringComparison.Ordinal)
                && (path.Contains("/story-contracts/", StringComparison.Ordinal)
                    || path.Contains("/inventories/", StringComparison.Ordinal)
                    || path.Contains("/resolved-customization/", StringComparison.Ordinal)
                    || path == GraphPath
                    || path == SlicePath
                    || path == RemediationPath
                    || path == SupersessionPath))
            {
                using JsonDocument companion = JsonDocument.Parse(Read(path));
                JsonElement candidateProperty = path.Contains("/story-contracts/", StringComparison.Ordinal)
                    || path == SlicePath
                    || path == RemediationPath
                    ? companion.RootElement.GetProperty("authority").GetProperty("planningCandidate")
                    : companion.RootElement.GetProperty("planningCandidate");
                candidateProperty.GetString().ShouldBe(candidate, path);
            }
        }

        JsonElement sliceArtifact = artifacts.Single(row => row.GetProperty("path").GetString() == SlicePath);
        sliceArtifact.GetProperty("role").GetString().ShouldBe("story-slice-authority");
        JsonElement remediationArtifact = artifacts.Single(row => row.GetProperty("path").GetString() == RemediationPath);
        remediationArtifact.GetProperty("role").GetString().ShouldBe("pre-ir0-remediation-authority");
        artifacts.Single(row => row.GetProperty("path").GetString() == "_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json")
            .GetProperty("role").GetString().ShouldBe("checkpoint-authority");
        artifacts.Single(row => row.GetProperty("path").GetString() == "_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json")
            .GetProperty("role").GetString().ShouldBe("checkpoint-authority");
        artifacts.Single(row => row.GetProperty("path").GetString() == "_bmad-output/implementation-artifacts/spec-epic-16-planning-authority-publication.md")
            .GetProperty("role").GetString().ShouldBe("implementation-spec");
        JsonElement baseStoryArtifact = artifacts.Single(
            row => row.GetProperty("path").GetString() == "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json");
        baseStoryArtifact.GetProperty("role").GetString().ShouldBe("base-story-contract");

        foreach (string validatorPath in new[]
        {
            "_bmad/scripts/tests/test_publish_v9_planning_authority.py",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs",
            "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs",
            "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
        })
        {
            paths.ShouldContain(validatorPath);
        }

        bundle.GetProperty("gitlinks").EnumerateArray().Select(row => row.GetProperty("path").GetString()).ShouldBe(new[]
        {
            "references/Hexalith.AI.Tools",
            "references/Hexalith.Builds",
            "references/Hexalith.Commons",
            "references/Hexalith.EventStore",
            "references/Hexalith.Folders",
            "references/Hexalith.FrontComposer",
            "references/Hexalith.Memories",
            "references/Hexalith.Parties",
            "references/Hexalith.Projects",
            "references/Hexalith.Tenants",
        });

        string digestPayload = string.Concat(
            artifacts
                .OrderBy(row => row.GetProperty("path").GetString(), StringComparer.Ordinal)
                .Select(row => $"{row.GetProperty("sha256").GetString()}  {row.GetProperty("path").GetString()}\n"));
        bundle.GetProperty("bundleDigest").GetString().ShouldBe(Sha256(Encoding.UTF8.GetBytes(digestPayload)));
    }

    /// <summary>
    /// Proves the generated story contracts and graph are complete, non-vacuous, and acyclic.
    /// </summary>
    [Fact]
    public void StoryContractsAndExecutionGraphShouldBeCompleteAndAcyclic()
    {
        string contractsDirectory = Path.Combine(FindRepositoryRoot(), "_bmad-output/planning-artifacts/v9/story-contracts");
        string[] contractPaths = Directory.GetFiles(contractsDirectory, "*.json").OrderBy(path => path, StringComparer.Ordinal).ToArray();
        contractPaths.Length.ShouldBe(30);
        Dictionary<string, JsonElement> contracts = contractPaths.ToDictionary(
            path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone().GetProperty("storyId").GetString()!,
            path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone(),
            StringComparer.Ordinal);
        contracts.Keys.ShouldContain("10.3");
        contracts.Keys.ShouldContain("10.4");
        contracts["10.3"].GetProperty("scenarios").GetArrayLength().ShouldBe(8);
        contracts["10.4"].GetProperty("scenarios").GetArrayLength().ShouldBe(9);
        contracts["10.3"].GetProperty("scenarios").EnumerateArray()
            .Select(row => row.GetProperty("resultSemantics").GetProperty("notApplicableAllowed").GetBoolean())
            .ShouldBe([true, false, false, false, false, false, false, false]);
        contracts.Where(pair => pair.Key != "10.3")
            .SelectMany(pair => pair.Value.GetProperty("scenarios").EnumerateArray())
            .ShouldAllBe(row => !row.GetProperty("resultSemantics").GetProperty("notApplicableAllowed").GetBoolean());
        contracts["10.4"].GetProperty("scenarios")[8].GetProperty("id").GetString().ShouldBe("AC-10.4-09");
        foreach ((string storyId, JsonElement contract) in contracts)
        {
            bool isV14Amended = new[] { "12.1", "13.1", "14.1", "15.1", "16.1", "16.2", "16.3" }.Contains(storyId, StringComparer.Ordinal);
            contract.GetProperty("schemaVersion").GetString().ShouldBe(
                isV14Amended
                    ? "hexalith.conversations.v14-story-contract.v1"
                    : "hexalith.conversations.story-contract.v1");
            bool isV14 = storyId.StartsWith("16.", StringComparison.Ordinal);
            contract.GetProperty("authority").GetProperty("epic").GetString().ShouldBe(isV14 ? EpicAuthority : BaseEpicAuthority);
            contract.GetProperty("authority").GetProperty("architecture").GetString().ShouldBe(isV14 ? ArchitectureAuthority : BaseArchitectureAuthority);
            JsonElement[] scenarios = contract.GetProperty("scenarios").EnumerateArray().ToArray();
            scenarios.Length.ShouldBeGreaterThan(0, storyId);
            scenarios.Select(row => row.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count().ShouldBe(scenarios.Length);
            scenarios.ShouldAllBe(row => !string.IsNullOrWhiteSpace(row.GetProperty("command").GetString()));
            scenarios.ShouldAllBe(row => row.GetProperty("resultSemantics").GetProperty("expected").GetString() == "PASS");
            contract.GetProperty("finalRecord").GetProperty("summary").GetProperty("required").GetInt32().ShouldBe(scenarios.Length);
        }

        contracts["14.3"].GetProperty("scenarios").EnumerateArray()
            .Single(row => row.GetProperty("id").GetString() == "AC-14.3-02")
            .GetProperty("resultSemantics").GetProperty("expected").GetString().ShouldBe("PASS");
        contracts["10.4"].GetProperty("scenarios").EnumerateArray()
            .Single(row => row.GetProperty("id").GetString() == "AC-10.4-08")
            .GetProperty("contract").GetString()!.ShouldContain("summary `9/9/0/0/0/0`");
        foreach (string storyId in new[] { "16.1", "16.2", "16.3" })
        {
            contracts[storyId].GetProperty("scenarios").GetArrayLength().ShouldBe(6);
            contracts[storyId].GetProperty("finalRecord").GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(0);
            contracts[storyId].GetProperty("finalRecord").GetProperty("summary").GetProperty("notRun").GetInt32().ShouldBe(0);
        }

        using JsonDocument graphDocument = JsonDocument.Parse(Read(GraphPath));
        graphDocument.RootElement.GetProperty("edges").GetArrayLength().ShouldBe(61);
        Dictionary<string, string[]> graph = graphDocument.RootElement.GetProperty("nodes")
            .EnumerateArray()
            .ToDictionary(
                node => node.GetProperty("id").GetString()!,
                node => node.GetProperty("predecessors").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                StringComparer.Ordinal);
        graph.ShouldContainKey("IR-0");
        graph.ShouldContainKey("RG-15");
        graph.ShouldContainKey("6.2");
        graph.ShouldContainKey("7.1-SCHEMAS");
        graph.ShouldContainKey("E6-REMEDIATION");
        graph.ShouldContainKey("E6-CURRENT-PROOF");
        graph.ShouldContainKey("E6-CURRENT-CANDIDATE");
        graph.Count.ShouldBe(38);
        graph.Keys.Count(key => Regex.IsMatch(key, @"^(?:[7-9]|1[0-6])\.\d+$")).ShouldBe(30);
        graph["7.1-SCHEMAS"].ShouldBe(["6.2", "IR-0"]);
        graph["E6-REMEDIATION"].ShouldBe(["PC-PUBLICATION"]);
        graph["E6-CURRENT-PROOF"].ShouldBe(["E6-REMEDIATION"]);
        graph["E6-CURRENT-CANDIDATE"].ShouldBe(["E6-CURRENT-PROOF", "E6-REMEDIATION"]);
        graph["IR-0"].ShouldBe(["E6-REMEDIATION"]);
        graph["7.1"].ShouldBe(["6.2", "7.1-SCHEMAS", "IR-0"]);
        graph["7.2"].ShouldBe(["7.1"]);
        graph["12.1"].ShouldBe(["16.3", "6.2", "IR-0"]);
        graph["16.1"].ShouldBe(["7.4", "IR-0"]);
        graph["16.2"].ShouldBe(["16.1"]);
        graph["16.3"].ShouldBe(["16.2"]);
        graph.Values.ShouldAllBe(predecessors => predecessors.SequenceEqual(predecessors.OrderBy(value => value, StringComparer.Ordinal)));
        foreach (string storyId in contracts.Keys)
        {
            Ancestors(graph, storyId).ShouldContain("IR-0", $"{storyId} must remain downstream of IR-0.");
        }

        HasCycle(graph).ShouldBeFalse();
    }

    /// <summary>
    /// Proves the closed v11 sidecar has one-way digest order and no completion authority.
    /// </summary>
    [Fact]
    public void StorySliceAuthorityShouldBindBaseContractAndCanonicalAmendment()
    {
        using JsonDocument sidecarDocument = JsonDocument.Parse(Read(SlicePath));
        JsonElement sidecar = sidecarDocument.RootElement;
        sidecar.GetProperty("schemaVersion").GetString().ShouldBe("hexalith.conversations.story-slice-authority.v1");
        sidecar.GetProperty("sliceId").GetString().ShouldBe("7.1-SCHEMAS");
        sidecar.GetProperty("storyId").GetString().ShouldBe("7.1");
        sidecar.GetProperty("authority").GetProperty("epic").GetString().ShouldBe("epic-6-authority-2026-08-04-v11");
        sidecar.GetProperty("authority").GetProperty("architecture").GetString().ShouldBe("conversations-architecture-2026-08-04-v11");
        sidecar.GetProperty("authority").GetProperty("authorityBundlePath").GetString().ShouldBe(BundlePath);
        sidecar.TryGetProperty("bundleDigest", out _).ShouldBeFalse();

        JsonElement baseContract = sidecar.GetProperty("baseStoryContract");
        string baseContractPath = baseContract.GetProperty("path").GetString()!;
        baseContract.GetProperty("sha256").GetString().ShouldBe(Sha256(ReadBytes(baseContractPath)));
        baseContract.GetProperty("epic").GetString().ShouldBe(BaseEpicAuthority);
        baseContract.GetProperty("architecture").GetString().ShouldBe(BaseArchitectureAuthority);

        string v11Epic = ExtractMarkerBlock(
            Read(EpicsPath),
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:BEGIN",
            "<!-- EPIC-6-AUTHORITY-OVERLAY-V11:END");
        string amendment = ExtractSection(
            v11Epic,
            "### Story 7.1 V11 Schema-Checkpoint Amendment: Authorize A Non-Story Slice",
            "### V11 Publication, Hold, And Retrospective State");
        sidecar.GetProperty("amendmentSectionSha256").GetString()
            .ShouldBe(Sha256(Encoding.UTF8.GetBytes(amendment)));
        sidecar.GetProperty("predecessors").EnumerateArray().Select(value => value.GetString())
            .ShouldBe(["6.2", "IR-0"]);
        sidecar.GetProperty("holdRequirement").GetProperty("effectiveState").GetString().ShouldBe("LIFTED");
        sidecar.GetProperty("completionEffect").GetProperty("storyDoneAllowed").GetBoolean().ShouldBeFalse();
        sidecar.GetProperty("completionEffect").GetProperty("finalRecordProduced").GetBoolean().ShouldBeFalse();
        sidecar.GetProperty("completionEffect").GetProperty("successorUnlocked").GetBoolean().ShouldBeFalse();
        sidecar.GetProperty("acceptance").GetProperty("command").GetString().ShouldBe(
            "python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml");

        Sha256(ReadBytes("_bmad/schemas/v9-story-contract-v1.schema.json"))
            .ShouldBe("33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0");
        Read(BundlePath).ShouldNotContain("\"LIFTED\"");
        Regex.IsMatch(Read(SprintPath), @"^  [^\n]*7\.1-SCHEMAS[^\n]*:", RegexOptions.Multiline).ShouldBeFalse();

        string view = Read("_bmad-output/planning-artifacts/epic-6-current-execution-view-v2.md");
        CountOccurrences(
            view,
            "| 7.1-SCHEMAS | checkpoint | Closed Story 7.1 schema contracts | 6.2, IR-0 | 1 |").ShouldBe(1);
        Regex.Matches(view, @"^\| 7\.1 \|.*$", RegexOptions.Multiline)
            .Select(match => match.Value).ShouldBe([
                "| 7.1 | story | Define the final-record schema and deterministic generator core | 6.2, 7.1-SCHEMAS, IR-0 | 6 |",
            ]);
        Regex.Matches(view, @"^\| 7\.2 \|.*$", RegexOptions.Multiline)
            .Select(match => match.Value).ShouldBe([
                "| 7.2 | story | Derive test, path, candidate, submodule, and gitlink facts | 7.1 | 11 |",
            ]);
    }

    /// <summary>
    /// Proves the V12 checkpoint owns exactly A1-A3 and preserves A4-A6 plus the active hold.
    /// </summary>
    [Fact]
    public void RemediationAuthorityShouldBindExactActionsRoutesGitlinksAndCompletionEffect()
    {
        using JsonDocument document = JsonDocument.Parse(Read(RemediationPath));
        JsonElement authority = document.RootElement;
        authority.GetProperty("schemaVersion").GetString()
            .ShouldBe("hexalith.conversations.v12-pre-ir0-remediation-authority.v1");
        authority.GetProperty("checkpointId").GetString().ShouldBe("E6-REMEDIATION");
        authority.GetProperty("authority").GetProperty("epic").GetString().ShouldBe(V12EpicAuthority);
        authority.GetProperty("authority").GetProperty("architecture").GetString().ShouldBe(V12ArchitectureAuthority);
        authority.GetProperty("authority").GetProperty("implementationHold").GetString().ShouldBe("ACTIVE");
        authority.GetProperty("predecessors").EnumerateArray().Select(value => value.GetString())
            .ShouldBe(["PC-PUBLICATION"]);
        authority.GetProperty("successor").GetString().ShouldBe("IR-0");

        JsonElement[] actions = authority.GetProperty("actionInventory").EnumerateArray().ToArray();
        actions.Select(row => row.GetProperty("id").GetString()).ShouldBe(["A1", "A2", "A3", "A4", "A5", "A6"]);
        actions.Take(3).ShouldAllBe(row => row.GetProperty("checkpointOwned").GetBoolean());
        actions.Skip(3).ShouldAllBe(row => !row.GetProperty("checkpointOwned").GetBoolean());
        actions.ShouldAllBe(row => row.GetProperty("status").GetString() == "open");
        authority.GetProperty("activeRoutePaths").GetArrayLength().ShouldBe(12);
        authority.GetProperty("rootGitlinkPaths").GetArrayLength().ShouldBe(10);
        authority.GetProperty("resultSemantics").GetProperty("ledgerRequired").GetBoolean().ShouldBeTrue();
        authority.GetProperty("resultSemantics").GetProperty("skipsAllowed").GetBoolean().ShouldBeFalse();
        authority.GetProperty("assertions").GetArrayLength().ShouldBeGreaterThan(0);

        JsonElement effect = authority.GetProperty("completionEffect");
        effect.GetProperty("ir0RerunAllowed").GetBoolean().ShouldBeTrue();
        effect.GetProperty("holdLifted").GetBoolean().ShouldBeFalse();
        effect.GetProperty("successorStarted").GetBoolean().ShouldBeFalse();
        effect.GetProperty("releaseAuthorized").GetBoolean().ShouldBeFalse();
        Read(BundlePath).ShouldNotContain("implementation-readiness-report");
        Regex.IsMatch(Read(SprintPath), @"^  [^\n]*E6-REMEDIATION[^\n]*:", RegexOptions.Multiline).ShouldBeFalse();
    }

    /// <summary>
    /// Proves route parity, aliases, and resolved project guidance remain complete.
    /// </summary>
    [Fact]
    public void WorkflowAndGuidanceInventoriesShouldMatchRoutesAndResolvedCustomization()
    {
        using JsonDocument workflows = JsonDocument.Parse(Read("_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v3.json"));
        JsonElement[] rows = workflows.RootElement.GetProperty("rows").EnumerateArray().ToArray();
        rows.Length.ShouldBe(12);
        rows.Select(row => row.GetProperty("logicalBody").GetString()).Distinct(StringComparer.Ordinal).Count().ShouldBe(6);
        foreach (IGrouping<string?, JsonElement> pair in rows.GroupBy(row => row.GetProperty("logicalBody").GetString(), StringComparer.Ordinal))
        {
            JsonElement[] twins = pair.ToArray();
            twins.Length.ShouldBe(2);
            twins[0].GetProperty("sha256").GetString().ShouldBe(twins[1].GetProperty("sha256").GetString(), pair.Key);
        }

        foreach ((string alias, string target) in new[] { ("bmad-dev-auto", "bmad-build-auto"), ("bmad-quick-dev", "bmad-build") })
        {
            foreach (string tree in new[] { ".agents", ".claude" })
            {
                string content = Read($"{tree}/skills/{alias}/SKILL.md");
                CountOccurrences(content, $"invoke `{target}` exactly once").ShouldBe(2);
                content.ShouldNotContain("verify_evidence_boundary.py");
            }
        }

        using JsonDocument guidance = JsonDocument.Parse(Read("_bmad-output/planning-artifacts/v9/inventories/evidence-guidance-v2.json"));
        guidance.RootElement.GetProperty("rows").GetArrayLength().ShouldBe(4);
        foreach (string skill in new[] { "bmad-build", "bmad-build-auto", "bmad-review" })
        {
            string resolved = Read($"_bmad-output/planning-artifacts/v9/resolved-customization/{skill}.json");
            resolved.ShouldContain("docs/runbooks/evidence-boundary-validation.md");
            resolved.ShouldContain("planningCandidate");
        }
    }

    /// <summary>
    /// Proves supersession, UX, sprint, hold, and open-action denominators remain exact.
    /// </summary>
    [Fact]
    public void SupersessionUxAndSprintProjectionsShouldPreserveEveryFrozenBoundary()
    {
        using JsonDocument supersessionDocument = JsonDocument.Parse(Read(SupersessionPath));
        JsonElement supersession = supersessionDocument.RootElement;
        JsonElement[] dispositions = supersession.GetProperty("storyDispositions").EnumerateArray().ToArray();
        dispositions.Length.ShouldBe(9);
        dispositions.Count(row => row.GetProperty("sourceStory").GetString() == "6.10"
            && row.GetProperty("successorEpic").GetInt32() == 10).ShouldBe(1);
        JsonElement ledger = supersession.GetProperty("obligationLedger");
        ledger.GetProperty("inventoryId").GetString().ShouldBe("V9-V8-OBLIGATION-LEDGER-v1");
        ledger.GetProperty("sha256").GetString().ShouldBe("4dbffda456c4f40055985f303ed9d10d8e7839573e2486c4d01ca5508dca8f87");
        ledger.GetProperty("acceptanceCriteriaRows").GetInt32().ShouldBe(66);
        ledger.GetProperty("totalRows").GetInt32().ShouldBe(156);
        JsonElement[] obligations = ledger.GetProperty("rows").EnumerateArray().ToArray();
        obligations.Length.ShouldBe(156);
        obligations.Select(row => row.GetProperty("ordinal").GetInt32()).ShouldBe(Enumerable.Range(1, 156));
        obligations.Select(row => row.GetProperty("sourceId").GetString()).Distinct(StringComparer.Ordinal).Count().ShouldBe(156);
        string ledgerPayload = string.Concat(
            obligations.Select(row => $"{row.GetProperty("sourceId").GetString()}|{row.GetProperty("canonicalBinding").GetString()}\n"));
        ledger.GetProperty("sha256").GetString().ShouldBe(Sha256(Encoding.UTF8.GetBytes(ledgerPayload)));
        JsonElement[] storyTen = obligations.Where(row => row.GetProperty("sourceId").GetString()!.StartsWith("V8-6.10-AC", StringComparison.Ordinal)).ToArray();
        storyTen.Length.ShouldBe(10);
        storyTen.Single(row => row.GetProperty("sourceId").GetString() == "V8-6.10-AC9")
            .GetProperty("effectiveBindings").EnumerateArray().Select(value => value.GetString()).ShouldContain("AC-10.4-09");

        JsonElement denominators = supersession.GetProperty("preservationDenominators");
        foreach ((string name, int expected) in new[]
        {
            ("functionalRequirements", 124),
            ("nonFunctionalRequirements", 77),
            ("uxDecisions", 52),
            ("uxAcceptanceCriteria", 28),
        })
        {
            denominators.GetProperty(name).GetProperty("required").GetInt32().ShouldBe(expected);
            denominators.GetProperty(name).GetProperty("mapped").GetInt32().ShouldBe(expected);
        }

        string ux = Read(UxMapPath);
        using JsonDocument bundleDocument = JsonDocument.Parse(Read(BundlePath));
        ux.ShouldContain($"planningCandidate: {bundleDocument.RootElement.GetProperty("planningCandidate").GetString()}");
        Regex.Matches(ux, @"^\| (UX-DR\d+) \|", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value).ShouldBe(Enumerable.Range(1, 52).Select(value => $"UX-DR{value}"));
        Regex.Matches(ux, @"^\| (AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}) \|", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value).ShouldBe(new[]
            {
                "AC-SAFE-001", "AC-SAFE-002", "AC-SAFE-003", "AC-SAFE-004", "AC-SAFE-005", "AC-SAFE-006", "AC-SAFE-007", "AC-SAFE-008",
                "AC-RESP-001", "AC-RESP-002", "AC-RESP-003", "AC-RESP-004", "AC-RESP-005", "AC-RESP-006", "AC-RESP-007", "AC-RESP-008",
                "AC-RESP-009", "AC-RESP-010", "AC-RESP-011", "AC-RESP-012", "AC-RESP-013", "AC-RESP-014", "AC-RESP-015",
                "AC-A11Y-001", "AC-A11Y-002", "AC-LEAK-001", "AC-MOB-001", "AC-PERF-001",
            });
        ux.ShouldContain("currentDisposition: preserved-not-activated");

        string sprint = Read(SprintPath);
        Regex.Matches(sprint, @"^  (?:[7-9]|1[0-6])-\d+-[^:]+: backlog$", RegexOptions.Multiline).Count.ShouldBe(30);
        Regex.Matches(sprint, @"^last_updated: [^\n]+$", RegexOptions.Multiline).Count.ShouldBe(1);
        Regex.IsMatch(sprint, @"^  [^:\n]*7\.1-SCHEMAS[^:\n]*:", RegexOptions.Multiline).ShouldBeFalse();
        sprint.ShouldContain("last_updated: 2026-08-19");
        sprint.ShouldContain("# V14 PLANNING PUBLICATION:");
        sprint.ShouldContain("A2 and A3 are done, A4-A6 remain open");
        sprint.ShouldContain("GLOBAL IMPLEMENTATION HOLD remains ACTIVE");
        sprint.ShouldContain("  epic-6-retrospective: done");
        Regex.Matches(sprint, "^  - id: \"(epic-6-retro-item-[^\"]+)\"$", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value).ShouldBe(new[]
            {
                "epic-6-retro-item-24-produce-an-additive-epic-6-completion-su",
                "epic-6-retro-item-25-restore-the-submodule-promotion-and-evid",
                "epic-6-retro-item-26-harden-planning-authority-verification-t",
                "epic-6-retro-item-27-create-approved-successor-work-for-a-dur",
                "epic-6-retro-item-28-create-approved-successor-work-for-deter",
                "epic-6-retro-item-29-add-explicit-preflight-diagnostics-for-a",
            });
        string action = sprint[sprint.IndexOf("Promote the Story 5.3 evidence-boundary", StringComparison.Ordinal)..];
        action.ShouldContain("status: open");
    }

    private static bool HasCycle(Dictionary<string, string[]> graph)
    {
        HashSet<string> visiting = new(StringComparer.Ordinal);
        HashSet<string> visited = new(StringComparer.Ordinal);

        bool Visit(string node)
        {
            if (!visiting.Add(node))
            {
                return true;
            }

            if (visited.Contains(node))
            {
                visiting.Remove(node);
                return false;
            }

            foreach (string predecessor in graph[node])
            {
                if (!graph.ContainsKey(predecessor) || Visit(predecessor))
                {
                    return true;
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }

        return graph.Keys.Any(Visit);
    }

    private static HashSet<string> Ancestors(Dictionary<string, string[]> graph, string node)
    {
        HashSet<string> ancestors = new(StringComparer.Ordinal);
        Stack<string> pending = new(graph[node]);
        while (pending.TryPop(out string? predecessor))
        {
            if (!ancestors.Add(predecessor))
            {
                continue;
            }

            foreach (string next in graph[predecessor])
            {
                pending.Push(next);
            }
        }

        return ancestors;
    }

    private static string ExtractSection(string content, string heading, string nextHeading)
    {
        int start = content.IndexOf(heading, StringComparison.Ordinal);
        int end = content.IndexOf(nextHeading, start + heading.Length, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        end.ShouldBeGreaterThan(start);
        return content[start..end].TrimEnd();
    }

    private static string ExtractMarkerBlock(string content, string begin, string end)
    {
        CountOccurrences(content, begin).ShouldBe(1);
        CountOccurrences(content, end).ShouldBe(1);
        int start = content.IndexOf(begin, StringComparison.Ordinal);
        int endStart = content.IndexOf(end, start, StringComparison.Ordinal);
        int close = content.IndexOf("-->", endStart, StringComparison.Ordinal) + 3;
        return content[start..close];
    }

    private static int CountOccurrences(string content, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = content.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static byte[] ReadBytes(string relativePath) => File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), relativePath));

    private static byte[] ReadCandidateBytes(string candidate, string relativePath)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = FindRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("show");
        startInfo.ArgumentList.Add($"{candidate}:{relativePath}");
        using Process process = Process.Start(startInfo)!;
        using MemoryStream output = new();
        process.StandardOutput.BaseStream.CopyTo(output);
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, process.StandardError.ReadToEnd());
        return output.ToArray();
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));

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

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
