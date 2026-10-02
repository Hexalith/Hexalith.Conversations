// <copyright file="UxPreservationDispositionValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Validates the Story 8.1 preservation disposition against its canonical sources.
/// </summary>
public sealed class UxPreservationDispositionValidationTest
{
    private const string DispositionPath = "docs/release-evidence/ux-preservation-disposition-v1.json";
    private const string MarkdownPath = "docs/release-evidence/ux-preservation-disposition-v1.md";
    private const string SchemaPath = "docs/release-evidence/ux-preservation-disposition-v1.schema.json";
    private const string SpecificationPath = "_bmad-output/planning-artifacts/ux-design-specification.md";
    private const string MapPath = "_bmad-output/planning-artifacts/ux-requirement-map.md";
    private const string SpecPath = "_bmad-output/implementation-artifacts/spec-8-1-generate-the-versioned-ux-disposition-contract.md";
    private const string Status = "preserved-not-activated";
    private static readonly HashSet<string> AllowedCandidatePaths = new(StringComparer.Ordinal)
    {
        SpecPath,
        "_bmad-output/implementation-artifacts/epic-8-context.md",
        "_bmad-output/implementation-artifacts/sprint-status.yaml",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/generate_ux_preservation_disposition.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
        "_bmad/scripts/tests/test_generate_ux_preservation_disposition.py",
        "docs/runbooks/story-final-record-generation.md",
        "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV8ValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs",
        SchemaPath,
        DispositionPath,
        MarkdownPath,
        "docs/release-evidence/story-8.1-final-record-v2.json",
        "docs/release-evidence/story-8.1-final-record-v2.md",
    };

    /// <summary>
    /// Proves the two source paths, versions, and exact current byte digests.
    /// </summary>
    [Fact]
    public void SourcesShouldBindCanonicalPathsVersionsAndHashes()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement root = disposition.RootElement;
        root.GetProperty("schemaVersion").GetString().ShouldBe("hexalith.conversations.ux-preservation-disposition.v1");
        JsonElement.ArrayEnumerator sources = root.GetProperty("sources").EnumerateArray();
        JsonElement[] rows = sources.ToArray();
        rows.Length.ShouldBe(2);
        string[] paths = [SpecificationPath, MapPath];
        string[] versionKeys = ["preservationAuthorityVersion", "authorityVersion"];
        for (int index = 0; index < paths.Length; index++)
        {
            string source = Read(paths[index]);
            rows[index].GetProperty("path").GetString().ShouldBe(paths[index]);
            rows[index].GetProperty("sha256").GetString().ShouldBe(Sha256(ReadBytes(paths[index])));
            string version = Regex.Match(source, $@"^{versionKeys[index]}:\s*(\S+)$", RegexOptions.Multiline).Groups[1].Value;
            version.ShouldNotBeNullOrWhiteSpace();
            rows[index].GetProperty("version").GetString().ShouldBe(version);
        }

        using JsonDocument schema = LoadJson(SchemaPath);
        schema.RootElement.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        schema.RootElement.GetProperty("properties").GetProperty("decisions").GetProperty("items")
            .GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        schema.RootElement.GetProperty("properties").GetProperty("acceptanceCriteria").GetProperty("items")
            .GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        root.GetProperty("renderedMarkdownSha256").GetString().ShouldBe(Sha256(ReadBytes(MarkdownPath)));
    }

    /// <summary>
    /// Proves all 52 decisions have one row each, in canonical source order.
    /// </summary>
    [Fact]
    public void DecisionsShouldProjectTheFrozenInventory()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement[] rows = disposition.RootElement.GetProperty("decisions").EnumerateArray().ToArray();
        string[] expected = Enumerable.Range(1, 52).Select(number => $"UX-DR{number}").ToArray();
        Match[] source = Regex.Matches(Read(MapPath), @"^\| (UX-DR\d+) \| ([^|]+) \| ([^|]+) \| ([^|]+) \| ([^|]+) \|$", RegexOptions.Multiline)
            .Cast<Match>().ToArray();
        source.Select(match => match.Groups[1].Value).ShouldBe(expected);
        rows.Select(row => row.GetProperty("id").GetString()).ShouldBe(expected);
        for (int index = 0; index < rows.Length; index++)
        {
            AssertRow(rows[index], MapPath);
            rows[index].GetProperty("rationale").GetString().ShouldBe(source[index].Groups[3].Value.Trim());
            rows[index].GetProperty("evidenceOrControl").GetString().ShouldBe($"{MapPath}#ux-decision-inventory");
            AssertMapping(rows[index], source[index].Groups[5].Value.Trim());
        }
    }

    /// <summary>
    /// Proves all 28 acceptance criteria have one row each, in both sources' order.
    /// </summary>
    [Fact]
    public void AcceptanceCriteriaShouldProjectTheFrozenInventory()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement[] rows = disposition.RootElement.GetProperty("acceptanceCriteria").EnumerateArray().ToArray();
        string[] expected =
        [
            .. Enumerable.Range(1, 8).Select(number => $"AC-SAFE-{number:000}"),
            .. Enumerable.Range(1, 15).Select(number => $"AC-RESP-{number:000}"),
            "AC-A11Y-001", "AC-A11Y-002", "AC-LEAK-001", "AC-MOB-001", "AC-PERF-001",
        ];
        string[] map = Regex.Matches(Read(MapPath), @"^\| (AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}) \|", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value).ToArray();
        string[] source = Regex.Matches(Read(SpecificationPath), @"^- \*\*(AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}):\*\*", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value).ToArray();
        map.ShouldBe(expected);
        source.ShouldBe(expected);
        rows.Select(row => row.GetProperty("id").GetString()).ShouldBe(expected);
        Match[] requirements = Regex.Matches(Read(SpecificationPath), @"^- \*\*(AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}):\*\* (.+)$", RegexOptions.Multiline)
            .Cast<Match>().ToArray();
        Match[] mapRows = Regex.Matches(Read(MapPath), @"^\| (AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}) \| ([^|]+) \| ([^|]+) \| ([^|]+) \|$", RegexOptions.Multiline)
            .Cast<Match>().ToArray();
        mapRows.Select(match => match.Groups[1].Value).ShouldBe(expected);
        for (int index = 0; index < rows.Length; index++)
        {
            AssertRow(rows[index], SpecificationPath);
            rows[index].GetProperty("rationale").GetString().ShouldBe(requirements[index].Groups[2].Value);
            string heading = mapRows[index].Groups[2].Value.Trim().ToLowerInvariant().Replace(' ', '-');
            rows[index].GetProperty("evidenceOrControl").GetString().ShouldBe($"{SpecificationPath}#{heading}");
            AssertMapping(rows[index], mapRows[index].Groups[4].Value.Trim());
        }
    }

    /// <summary>
    /// Proves every disposition and historical reference remains non-activating.
    /// </summary>
    [Fact]
    public void DispositionsShouldRemainPreservedAndHistorical()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement root = disposition.RootElement;
        root.GetProperty("status").GetString().ShouldBe(Status);
        string banner = root.GetProperty("preservationBanner").GetString()!;
        banner.ShouldContain("not activated");
        banner.ShouldContain("non-current");
        Read(MarkdownPath).ShouldContain(banner);
        root.GetProperty("historicalProvenance").GetProperty("classification").GetString().ShouldBe("non-current");
        root.GetProperty("historicalProvenance").GetProperty("currentImplementationOwner").GetBoolean().ShouldBeFalse();
        foreach (JsonElement row in root.GetProperty("decisions").EnumerateArray().Concat(root.GetProperty("acceptanceCriteria").EnumerateArray()))
        {
            row.GetProperty("status").GetString().ShouldBe(Status);
            row.GetProperty("owner").GetString().ShouldBe("Stories 8.1-8.2 preservation contract");
            foreach (JsonElement mapping in row.GetProperty("historicalMappings").EnumerateArray())
            {
                mapping.GetProperty("classification").GetString().ShouldBe("historical-provenance");
                mapping.GetProperty("current").GetBoolean().ShouldBeFalse();
            }
        }
    }

    /// <summary>
    /// Proves the Story 8.1 candidate changes no production UI or runtime path.
    /// </summary>
    [Fact]
    public void CandidateShouldContainNoProductionUiChange()
    {
        string spec = Read(SpecPath);
        string baseline = Regex.Match(spec, @"^baseline_commit:\s*'?([0-9a-f]{40})'?\s*$", RegexOptions.Multiline).Groups[1].Value;
        baseline.Length.ShouldBe(40);
        string[] committed = Git("diff", "--name-only", $"{baseline}..HEAD").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string path in committed)
        {
            AllowedCandidatePaths.Contains(path).ShouldBeTrue($"UX_PRODUCTION_CHANGE_FORBIDDEN: {path}");
        }
    }

    private static void AssertRow(JsonElement row, string sourcePath)
    {
        foreach (string field in new[] { "id", "status", "owner", "rationale", "sourcePath", "sourceSha256", "evidenceOrControl", "historicalMappings", "compatibility", "disclosureSafety" })
        {
            row.TryGetProperty(field, out _).ShouldBeTrue($"Missing required row field {field}.");
        }

        row.GetProperty("sourcePath").GetString().ShouldBe(sourcePath);
        row.GetProperty("sourceSha256").GetString().ShouldBe(Sha256(ReadBytes(sourcePath)));
        row.GetProperty("rationale").GetString().ShouldNotBeNullOrWhiteSpace();
        row.GetProperty("owner").GetString().ShouldNotBeNullOrWhiteSpace();
        row.GetProperty("compatibility").GetString().ShouldBe(sourcePath == MapPath
            ? "Preserved obligation; future activation requires separate authorization."
            : "Preserved acceptance obligation; no feature-delivery claim.");
        row.GetProperty("disclosureSafety").GetString().ShouldBe("No product disclosure or UI implementation is authorized.");
    }

    private static void AssertMapping(JsonElement row, string expected)
    {
        JsonElement[] mappings = row.GetProperty("historicalMappings").EnumerateArray().ToArray();
        mappings.Length.ShouldBe(1);
        mappings[0].GetProperty("reference").GetString().ShouldBe(expected);
        mappings[0].GetProperty("classification").GetString().ShouldBe("historical-provenance");
        mappings[0].GetProperty("current").GetBoolean().ShouldBeFalse();
    }

    private static JsonDocument LoadJson(string relativePath) => JsonDocument.Parse(ReadBytes(relativePath));

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(FindRoot(), relativePath), Encoding.UTF8);

    private static byte[] ReadBytes(string relativePath) => File.ReadAllBytes(Path.Combine(FindRoot(), relativePath));

    private static string Sha256(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string FindRoot()
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

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }

    private static string Git(params string[] arguments)
    {
        ProcessStartInfo start = new("git")
        {
            WorkingDirectory = FindRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, error);
        return output;
    }
}
