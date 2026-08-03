// <copyright file="PlanningAuthorityV9ValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Validates the candidate-bound v9 companion publication and v10 evidence-boundary correction.
/// </summary>
public sealed class PlanningAuthorityV9ValidationTest
{
    private const string ArchitectureAuthority = "conversations-architecture-2026-08-03-v10";
    private const string ArchitecturePath = "_bmad-output/planning-artifacts/architecture.md";
    private const string BundlePath = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json";
    private const string EpicAuthority = "epic-6-authority-2026-08-03-v10";
    private const string EpicsPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md";
    private const string GraphPath = "_bmad-output/planning-artifacts/v9-execution-graph-v1.json";
    private const string SprintPath = "_bmad-output/implementation-artifacts/sprint-status.yaml";
    private const string SupersessionPath = "_bmad-output/planning-artifacts/v9-supersession-map-v1.json";
    private const string UxMapPath = "_bmad-output/planning-artifacts/ux-requirement-map.md";
    private const string V9ArchitectureDigest = "4686212387189e78f98de5352d12eb8544d1a9f78c97dfc446266fa3d4d3f3d9";
    private const string V9EpicDigest = "e7d6ea5759c12ab70f21b472656828bb4e5bcce2023d845f06a40cf1373d1c9d";

    /// <summary>
    /// Proves the v9 prefixes remain byte-identical and the v10 scope is narrow.
    /// </summary>
    [Fact]
    public void V10AuthorityShouldPreserveV9AndAmendOnlyStoriesTenThreeAndTenFour()
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

        Encoding.UTF8.GetByteCount(v9Epic).ShouldBe(188677);
        Sha256(Encoding.UTF8.GetBytes(v9Epic)).ShouldBe(V9EpicDigest);
        Encoding.UTF8.GetByteCount(v9Architecture).ShouldBe(18270);
        Sha256(Encoding.UTF8.GetBytes(v9Architecture)).ShouldBe(V9ArchitectureDigest);
        CountOccurrences(v10Epic, "Story 10.3 V10 Amendment").ShouldBe(1);
        CountOccurrences(v10Epic, "Story 10.4 V10 Amendment").ShouldBe(1);
        v10Epic.ShouldContain("AC-10.4-09");
        v10Epic.ShouldContain("V9-EVIDENCE-WORKFLOWS-v2");
        v10Epic.ShouldContain("V9-EVIDENCE-GUIDANCE-v2");
        v10Epic.ShouldContain("Global implementation hold:** `ACTIVE`");
        v10Architecture.ShouldContain("BMAD `6.10.1n46`");
        v10Architecture.ShouldContain("Every other v9 obligation remains unchanged");
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
        artifacts.Length.ShouldBeGreaterThan(40);
        string[] paths = artifacts.Select(row => row.GetProperty("path").GetString()!).ToArray();
        paths.Distinct(StringComparer.Ordinal).Count().ShouldBe(paths.Length);
        paths.ShouldNotContain(BundlePath);
        foreach (JsonElement artifact in artifacts)
        {
            string path = artifact.GetProperty("path").GetString()!;
            artifact.GetProperty("sha256").GetString().ShouldBe(Sha256(ReadBytes(path)), path);
            if (path.EndsWith(".json", StringComparison.Ordinal)
                && (path.Contains("/story-contracts/", StringComparison.Ordinal)
                    || path.Contains("/inventories/", StringComparison.Ordinal)
                    || path.Contains("/resolved-customization/", StringComparison.Ordinal)
                    || path == GraphPath
                    || path == SupersessionPath))
            {
                using JsonDocument companion = JsonDocument.Parse(Read(path));
                companion.RootElement.GetProperty("planningCandidate").GetString().ShouldBe(candidate, path);
            }
        }

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
        contractPaths.Length.ShouldBe(27);
        Dictionary<string, JsonElement> contracts = contractPaths.ToDictionary(
            path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone().GetProperty("storyId").GetString()!,
            path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone(),
            StringComparer.Ordinal);
        contracts.Keys.ShouldContain("10.3");
        contracts.Keys.ShouldContain("10.4");
        contracts["10.3"].GetProperty("scenarios").GetArrayLength().ShouldBe(8);
        contracts["10.4"].GetProperty("scenarios").GetArrayLength().ShouldBe(9);
        contracts["10.4"].GetProperty("scenarios")[8].GetProperty("id").GetString().ShouldBe("AC-10.4-09");
        foreach ((string storyId, JsonElement contract) in contracts)
        {
            JsonElement[] scenarios = contract.GetProperty("scenarios").EnumerateArray().ToArray();
            scenarios.Length.ShouldBeGreaterThan(0, storyId);
            scenarios.Select(row => row.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count().ShouldBe(scenarios.Length);
            scenarios.ShouldAllBe(row => !string.IsNullOrWhiteSpace(row.GetProperty("command").GetString()));
        }

        using JsonDocument graphDocument = JsonDocument.Parse(Read(GraphPath));
        Dictionary<string, string[]> graph = graphDocument.RootElement.GetProperty("nodes")
            .EnumerateArray()
            .ToDictionary(
                node => node.GetProperty("id").GetString()!,
                node => node.GetProperty("predecessors").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                StringComparer.Ordinal);
        graph.ShouldContainKey("IR-0");
        graph.ShouldContainKey("RG-15");
        graph.ShouldContainKey("6.2");
        graph.Keys.Count(key => Regex.IsMatch(key, @"^(?:[7-9]|1[0-5])\.\d+$")).ShouldBe(27);
        HasCycle(graph).ShouldBeFalse();
    }

    /// <summary>
    /// Proves route parity, aliases, and resolved project guidance remain complete.
    /// </summary>
    [Fact]
    public void WorkflowAndGuidanceInventoriesShouldMatchRoutesAndResolvedCustomization()
    {
        using JsonDocument workflows = JsonDocument.Parse(Read("_bmad-output/planning-artifacts/v9/inventories/evidence-workflows-v2.json"));
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
        JsonElement[] obligations = supersession.GetProperty("v8AcceptanceObligations").EnumerateArray().ToArray();
        obligations.Length.ShouldBe(66);
        JsonElement[] storyTen = obligations.Where(row => row.GetProperty("sourceId").GetString()!.StartsWith("V8-6.10-AC", StringComparison.Ordinal)).ToArray();
        storyTen.Length.ShouldBe(10);
        storyTen.Single(row => row.GetProperty("sourceId").GetString() == "V8-6.10-AC9")
            .GetProperty("bindings").EnumerateArray().Select(value => value.GetString()).ShouldContain("AC-10.4-09");

        string ux = Read(UxMapPath);
        Regex.Matches(ux, @"^\| UX-DR\d+ \|", RegexOptions.Multiline).Count.ShouldBe(52);
        Regex.Matches(ux, @"^\| AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3} \|", RegexOptions.Multiline).Count.ShouldBe(28);
        ux.ShouldContain("currentDisposition: preserved-not-activated");

        string sprint = Read(SprintPath);
        Regex.Matches(sprint, @"^  (?:[7-9]|1[0-5])-\d+-[^:]+: backlog$", RegexOptions.Multiline).Count.ShouldBe(27);
        sprint.ShouldContain("GLOBAL IMPLEMENTATION HOLD remains ACTIVE");
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
