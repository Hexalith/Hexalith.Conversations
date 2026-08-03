// <copyright file="PlanningAuthorityV8ValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Validates the comprehensive Epic 6 v8 planning-authority publication.
/// </summary>
public sealed class PlanningAuthorityV8ValidationTest
{
    private const string ArchitecturePath = "_bmad-output/planning-artifacts/architecture.md";
    private const string CurrentViewPath = "_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md";
    private const string EpicsPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md";
    private const string SprintPath = "_bmad-output/implementation-artifacts/sprint-status.yaml";
    private const string UxMapPath = "_bmad-output/planning-artifacts/ux-requirement-map.md";
    private const string UxSpecificationPath = "_bmad-output/planning-artifacts/ux-design-specification.md";
    private const string CompletedStoryRecordPath = "_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md";
    private const string OverlayVersion = "epic-6-authority-2026-08-01-v8";
    private const string ArchitectureVersion = "conversations-architecture-2026-08-01-v8";
    private const string BeginMarker = "<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN version=epic-6-authority-2026-08-01-v8 supersedes=epic-6-authority-2026-08-01-v7 -->";
    private const string EndMarker = "<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:END version=epic-6-authority-2026-08-01-v8 -->";

    [Fact]
    public void V8ShouldPublishOneCompleteCurrentStorySetWithoutAuthorizingImplementation()
    {
        string epics = Read(EpicsPath);
        string block = ExtractInclusive(epics, BeginMarker, EndMarker);

        CountOccurrences(epics, BeginMarker).ShouldBe(1);
        CountOccurrences(epics, EndMarker).ShouldBe(1);
        Regex.Matches(block, @"^### Story 6\.(\d+):", RegexOptions.Multiline)
            .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .ShouldBe(Enumerable.Range(1, 12));

        string dispositions = ExtractSection(block, "### Current Story Dispositions", "### Topological Dependency Plan");
        Dictionary<string, string> statuses = Regex.Matches(
                dispositions,
                @"^\| (?<story>6\.\d+) \| (?<status>[^|]+) \|",
                RegexOptions.Multiline)
            .ToDictionary(
                match => match.Groups["story"].Value,
                match => match.Groups["status"].Value.Trim(),
                StringComparer.Ordinal);

        statuses.ShouldBe(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["6.1"] = "done",
            ["6.2"] = "done",
            ["6.3"] = "in-progress",
            ["6.4"] = "backlog",
            ["6.5"] = "backlog",
            ["6.6"] = "backlog",
            ["6.7"] = "done",
            ["6.8"] = "in-progress",
            ["6.9"] = "backlog",
            ["6.10"] = "backlog",
            ["6.11"] = "backlog",
            ["6.12"] = "ready-for-dev",
        });

        block.ShouldContain("AUTHORITY CORRECTION ONLY — NOT READY");
        block.ShouldContain("No remaining Epic 6 implementation work may start or resume");
        block.ShouldContain("does not implement Stories 6.3-6.6 or 6.8-6.12");
        block.ShouldContain("completed Stories 6.1, 6.2, and 6.7");
        block.ShouldContain("Every v1-v7 byte above remains immutable historical authority");
        Regex.IsMatch(
                block,
                @"v6 ceiling and disclosure model\s+is preserved only as immutable context",
                RegexOptions.IgnoreCase)
            .ShouldBeTrue("The v6 exception must remain historical context only.");
    }

    [Fact]
    public void CurrentViewShouldBeHashBoundAndStoryEquivalentToV8()
    {
        string epics = Read(EpicsPath);
        string view = Read(CurrentViewPath);
        string frontmatter = ExtractFrontmatter(view);
        string block = ExtractInclusive(epics, BeginMarker, EndMarker);
        byte[] historicalEpics = PrefixBeforeAppendedOverlay(ReadBytes(EpicsPath), "<!-- EPIC-6-AUTHORITY-OVERLAY-V9:BEGIN");
        byte[] historicalArchitecture = PrefixBeforeAppendedOverlay(ReadBytes(ArchitecturePath), "<!-- ARCHITECTURE-EXECUTION-OVERLAY-V9:BEGIN");

        AssertYaml(frontmatter, "overlay_version", OverlayVersion);
        AssertYaml(frontmatter, "architecture_version", ArchitectureVersion);
        AssertYaml(frontmatter, "status", "authority-correction-only-not-ready");
        AssertYaml(frontmatter, "generator_version", "1.0.0");
        AssertYaml(frontmatter, "source_epics_sha256", Sha256(historicalEpics));
        AssertYaml(frontmatter, "source_v8_block_sha256", Sha256(Encoding.UTF8.GetBytes(block)));
        AssertYaml(frontmatter, "source_architecture_sha256", Sha256(historicalArchitecture));
        AssertYaml(frontmatter, "source_sprint_status_sha256", "3ef082f8b11a9eb9b33e11516e72ac4b7b43d0d817da7d9f86a532ffcc190ee1");
        AssertYaml(frontmatter, "completed_story_6_2_record", CompletedStoryRecordPath);
        AssertYaml(frontmatter, "completed_story_6_2_record_sha256", Sha256(ReadBytes(CompletedStoryRecordPath)));

        foreach (string evidencePath in new[]
        {
            "docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json",
            "docs/release-evidence/projection-read-store-population-proof-v2.json",
            "docs/release-evidence/sm-c2-hot-path-baseline-v1.json",
            "docs/release-evidence/sm-c2-hot-path-post-v1.json",
        })
        {
            view.ShouldContain($"path: '{evidencePath}'");
            view.ShouldContain($"sha256:{Sha256(ReadBytes(evidencePath))}");
        }

        foreach (int story in Enumerable.Range(1, 12))
        {
            string headingPrefix = $"### Story 6.{story}:";
            ExtractHeadingSection(view, headingPrefix)
                .ShouldBe(ExtractHeadingSection(block, headingPrefix), $"Story 6.{story} must be an exact projection of v8.");
            CountOccurrences(view, headingPrefix).ShouldBe(1);
        }

        view.ShouldContain("6.2-H1 Baseline and authority");
        view.ShouldContain("6.2-H2 Runtime and projection migration");
        view.ShouldContain("6.2-H3 Candidate evidence and closure");
        view.ShouldContain("navigation aids only");
        view.ShouldContain("Hand editing or semantic drift is a conformance failure");
    }

    [Fact]
    public void V8DependencyGraphShouldBeClosedAndAcyclic()
    {
        string block = ExtractInclusive(Read(EpicsPath), BeginMarker, EndMarker);
        string topology = ExtractSection(block, "### Topological Dependency Plan", "### High-Risk BDD Scenario Catalogue");
        string edgeBlock = ExtractFencedBlock(topology, "text");
        MatchCollection matches = Regex.Matches(
            edgeBlock,
            @"^(?<from>6\.\d+) -> (?:completion of )?(?<to>6\.\d+)$",
            RegexOptions.Multiline);

        matches.Count.ShouldBe(24);
        HashSet<string> stories = Enumerable.Range(1, 12)
            .Select(story => $"6.{story}")
            .ToHashSet(StringComparer.Ordinal);
        Dictionary<string, List<string>> graph = stories.ToDictionary(
            story => story,
            _ => new List<string>(),
            StringComparer.Ordinal);
        HashSet<string> edges = new(StringComparer.Ordinal);

        foreach (Match match in matches)
        {
            string from = match.Groups["from"].Value;
            string to = match.Groups["to"].Value;
            stories.ShouldContain(from);
            stories.ShouldContain(to);
            graph[from].Add(to);
            edges.Add($"{from}->{to}").ShouldBeTrue("Duplicate dependency edges are forbidden.");
        }

        HasCycle(graph).ShouldBeFalse("The current Epic 6 dependency graph must be acyclic.");
        foreach (string required in new[]
        {
            "6.1->6.7", "6.7->6.2", "6.2->6.8", "6.8->6.10", "6.9->6.10",
            "6.8->6.12", "6.9->6.3", "6.10->6.3", "6.12->6.3", "6.2->6.11",
            "6.10->6.5", "6.11->6.6", "6.12->6.6",
        })
        {
            edges.ShouldContain(required);
        }
    }

    [Fact]
    public void MetricAndReadinessAuthorityShouldBeConsistentAndOutcomeNeutral()
    {
        string epicsBlock = ExtractInclusive(Read(EpicsPath), BeginMarker, EndMarker);
        string architecture = ExtractHeadingSection(Read(ArchitecturePath), "### 2026-08-01 Implementation Readiness Authority Correction");
        string view = Read(CurrentViewPath);

        foreach (string source in new[] { epicsBlock, architecture, view })
        {
            source.ShouldContain("post P95 <= 1.05 x baseline P95");
            source.ShouldContain("HP-CREATE");
            source.ShouldContain("HP-APPEND");
            source.ShouldContain("HP-LIST");
            source.ShouldContain("HP-OPEN");
            source.ShouldNotContain("If 6.11 has not landed");
            source.ShouldNotContain("Story 6.6 re-measures against the recorded ceiling");
        }

        string storySix = ExtractHeadingSection(epicsBlock, "### Story 6.6:");
        string normalizedStorySix = Regex.Replace(storySix, @"\s+", " ");
        normalizedStorySix.ShouldContain("complete actual result is published unchanged");
        normalizedStorySix.ShouldContain("assessor is not instructed or modified to return a particular verdict");
        normalizedStorySix.ShouldContain("remains blocked unless the preserved assessment result is `READY`");
        normalizedStorySix.ShouldContain("`NOT READY` or an incomplete assessment leaves Story 6.6 and Epic 6 open");
    }

    [Fact]
    public void UxPlanningShouldHaveCanonicalProvenanceAndZeroGapPreservationMapping()
    {
        string specification = Read(UxSpecificationPath);
        string map = Read(UxMapPath);

        specification.ShouldContain("_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md");
        specification.ShouldContain("_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md");
        specification.ShouldNotContain("  - _bmad-output/planning-artifacts/prd.md");
        specification.ShouldContain("Preservation-only UX authority");
        specification.ShouldContain("### Preserved Historical/Future Activation Sequence");
        specification.ShouldNotContain("### Implementation Roadmap");

        MatchCollection decisions = Regex.Matches(
            map,
            @"^\| UX-DR(?<id>\d+) \|.*\| preserved-not-activated; Stories 8\.1-8\.2 preservation contract \| Historical:",
            RegexOptions.Multiline);
        decisions.Select(match => int.Parse(match.Groups["id"].Value, System.Globalization.CultureInfo.InvariantCulture))
            .ShouldBe(Enumerable.Range(1, 52));
        map.ShouldNotContain("Primary Epics / Stories");

        string[] sourceCriteria = Regex.Matches(
                specification,
                @"\bAC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}\b")
            .Select(match => match.Value)
            .ToArray();
        sourceCriteria.Length.ShouldBe(sourceCriteria.Distinct(StringComparer.Ordinal).Count(), "UX acceptance identifiers must be unique at source.");

        string inventory = ExtractHeadingSection(map, "## Generated Acceptance-Criterion Inventory");
        string[] mappedCriteria = Regex.Matches(
                inventory,
                @"^\| (?<id>AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}) \|",
                RegexOptions.Multiline)
            .Select(match => match.Groups["id"].Value)
            .ToArray();
        mappedCriteria.Length.ShouldBe(mappedCriteria.Distinct(StringComparer.Ordinal).Count(), "Mapped UX acceptance identifiers must be unique.");
        mappedCriteria.OrderBy(value => value, StringComparer.Ordinal)
            .ShouldBe(sourceCriteria.OrderBy(value => value, StringComparer.Ordinal));
        inventory.ShouldContain("preserved-not-activated; Stories 8.1-8.2 preservation contract");

        foreach (string futureStoryDeliverable in new[]
        {
            "docs/release-evidence/ux-preservation-disposition-v1.schema.json",
            "docs/release-evidence/ux-preservation-disposition-v1.json",
            "docs/release-evidence/ux-preservation-disposition-v1.md",
            "tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs",
        })
        {
            File.Exists(Path.Combine(FindRepositoryRoot(), futureStoryDeliverable))
                .ShouldBeFalse($"{futureStoryDeliverable} belongs to future Story 6.4 implementation, not v8 publication.");
        }
    }

    [Fact]
    public void SprintAndActiveGuidanceShouldPreserveStatusesAndEnforceTheHold()
    {
        string sprint = Read(SprintPath);
        sprint.ShouldContain("GLOBAL IMPLEMENTATION HOLD remains ACTIVE");
        sprint.ShouldContain("IR-0 was not run");

        Dictionary<string, string> statuses = Regex.Matches(
                sprint,
                @"^  (?<story>6-\d+-[^:]+): (?<status>\S+)$",
                RegexOptions.Multiline)
            .ToDictionary(
                match => match.Groups["story"].Value,
                match => match.Groups["status"].Value,
                StringComparer.Ordinal);
        statuses.Count.ShouldBe(3);
        statuses["6-1-rebaseline-architecture-and-planning-authority"].ShouldBe("done");
        statuses["6-2-migrate-conversations-to-platform-owned-hosting"].ShouldBe("done");
        statuses["6-7-mechanically-block-incomplete-submodule-promotions-from-completion"].ShouldBe("done");
        Regex.Matches(sprint, @"^  (?:[7-9]|1[0-5])-\d+-[^:]+: backlog$", RegexOptions.Multiline).Count.ShouldBe(27);
        sprint.ShouldContain("epic-6: done");

        foreach (string path in new[]
        {
            "_bmad-output/implementation-artifacts/spec-6-3-create-complete-preservation-traceability-manifest.md",
            "_bmad-output/implementation-artifacts/6-8-generate-the-final-story-record-mechanically-from-measured-state.md",
            "_bmad-output/implementation-artifacts/6-12-version-projection-proofs-without-rewriting-completed-history.md",
        })
        {
            string guidance = Read(path);
            guidance.ShouldContain(OverlayVersion);
            guidance.ShouldContain(ArchitectureVersion);
            guidance.ShouldContain("Global hold");
        }
    }

    private static bool HasCycle(Dictionary<string, List<string>> graph)
    {
        HashSet<string> visiting = new(StringComparer.Ordinal);
        HashSet<string> visited = new(StringComparer.Ordinal);

        bool Visit(string node)
        {
            if (visiting.Contains(node))
            {
                return true;
            }

            if (!visited.Add(node))
            {
                return false;
            }

            visiting.Add(node);
            foreach (string successor in graph[node])
            {
                if (Visit(successor))
                {
                    return true;
                }
            }

            visiting.Remove(node);
            return false;
        }

        return graph.Keys.Any(Visit);
    }

    private static string ExtractFencedBlock(string content, string language)
    {
        Match match = Regex.Match(content, $"```{Regex.Escape(language)}\\n(?<body>.*?)\\n```", RegexOptions.Singleline);
        match.Success.ShouldBeTrue($"Missing {language} fenced block.");
        return match.Groups["body"].Value;
    }

    private static string ExtractFrontmatter(string content)
    {
        Match match = Regex.Match(content, @"\A---\r?\n(?<body>.*?)\r?\n---\r?\n", RegexOptions.Singleline);
        match.Success.ShouldBeTrue("Missing YAML frontmatter.");
        return match.Groups["body"].Value;
    }

    private static string ExtractHeadingSection(string content, string headingPrefix)
    {
        Match heading = Regex.Match(content, $"^{Regex.Escape(headingPrefix)}.*$", RegexOptions.Multiline);
        heading.Success.ShouldBeTrue($"Missing heading {headingPrefix}.");
        int level = heading.Value.TakeWhile(character => character == '#').Count();
        Match next = Regex.Match(
            content[(heading.Index + heading.Length)..],
            $@"^#{{1,{level}}} ",
            RegexOptions.Multiline);
        int end = next.Success ? heading.Index + heading.Length + next.Index : content.Length;
        return content[heading.Index..end].TrimEnd();
    }

    private static string ExtractInclusive(string content, string start, string end)
    {
        CountOccurrences(content, start).ShouldBe(1);
        CountOccurrences(content, end).ShouldBe(1);
        int startIndex = content.IndexOf(start, StringComparison.Ordinal);
        int endIndex = content.IndexOf(end, startIndex, StringComparison.Ordinal) + end.Length;
        return content[startIndex..endIndex];
    }

    private static string ExtractSection(string content, string heading, string nextHeading)
    {
        int start = content.IndexOf(heading, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"Missing heading {heading}.");
        int end = content.IndexOf(nextHeading, start, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start, $"Missing next heading {nextHeading}.");
        return content[start..end];
    }

    private static void AssertYaml(string frontmatter, string key, string value)
    {
        Regex.IsMatch(frontmatter, $@"^{Regex.Escape(key)}:\s*['""]?{Regex.Escape(value)}['""]?\s*$", RegexOptions.Multiline)
            .ShouldBeTrue($"Expected {key}: {value}.");
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

    private static byte[] PrefixBeforeAppendedOverlay(byte[] content, string marker)
    {
        byte[] markerBytes = Encoding.UTF8.GetBytes(marker);
        int markerIndex = content.AsSpan().IndexOf(markerBytes);
        markerIndex.ShouldBeGreaterThan(0, $"Missing appended overlay marker {marker}.");
        content[markerIndex - 1].ShouldBe((byte)'\n', "An appended overlay must be separated by one blank line.");
        return content[..(markerIndex - 1)];
    }

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
