// <copyright file="PreservationTraceabilityManifestValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

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
/// Independently validates the complete v2 preservation inventory, bindings, governance, and projection.
/// </summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class PreservationTraceabilityManifestValidationTest
{
    private const string ManifestPath = "docs/release-evidence/preservation-traceability-manifest-v2.json";
    private const string MarkdownPath = "docs/release-evidence/preservation-traceability-manifest-v2.md";
    private const string DispositionPath = "docs/release-evidence/preservation-non-activation-disposition-v2.json";
    private const string SchemaPath = "docs/release-evidence/preservation-traceability-manifest-v2.schema.json";
    private const string PrdPath = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md";
    private const string UxMapPath = "_bmad-output/planning-artifacts/ux-requirement-map.md";
    private const string UxSpecificationPath = "_bmad-output/planning-artifacts/ux-design-specification.md";
    private const string ContractBaselinePath = "docs/release-evidence/public-contract-shape-baseline-v1.json";
    private const string TierDecisionPath = "docs/release-evidence/conformance-oracle-tiering-decision-v2.json";

    private static readonly IReadOnlyDictionary<string, string> FrozenV1Hashes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["docs/release-evidence/manifest.schema.json"] = "a7b22c8ec7eca96ed75b831a3e37e938c163468f46a0ac7d0f53e8f8ab7a99de",
        ["docs/release-evidence/conformance-manifest-v1-fixture.json"] = "a26e44fbe0a19bea522864d654e2e38901e74d3e482f8f56f49bcfb35c59ee3f",
        ["docs/release-evidence/release-baseline-v1.json"] = "a3f0b4a76aa99226dfb6a7d9a0c930f30705c4d4f8d8c32f97a5b3124a335932",
        ["docs/release-evidence/release-baseline-v1.md"] = "183b392e8090619f2a40c7defe72679718c53c2832e3b6961b5308ba62e9f8f4",
        [ContractBaselinePath] = "ebfc2f67e90ecc8a7734719c6e2673b6e8392ab2cae9956a8e98b7bf769acfca",
        ["docs/release-evidence/success-metric-report-and-attestation-v1.json"] = "062ca0c7bc94279007077bda59eae867d21c12da2ffc0b59a0f389b99067e0fe",
        ["docs/release-evidence/success-metric-report-and-attestation-v1.md"] = "aa7e52c11ce36fc2c9ea953e275c654e7f312016c990cb20be16666d87f9a2cd",
        ["docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.json"] = "8091f6c26251420242a491cad100472dc1604a7163cc9d8df51bb1c742844856",
        ["docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.md"] = "a73077c0b5416c5085796c2e808a45efe09f5eb6a4ddf852214ecc93a9209e0b",
    };

    private static readonly IReadOnlyDictionary<string, string> ExpectedControlOwners = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CTRL-MODULE-PLATFORM-OWNERSHIP"] = "shared",
        ["CTRL-CANONICAL-HOST-SHAPE"] = "platform",
        ["CTRL-TEST-ONLY-APPHOST"] = "module",
        ["CTRL-PROJECTION-POPULATION"] = "module",
        ["CTRL-SM-C2-V6"] = "release-governance",
        ["CTRL-PROMOTION-GATE"] = "release-governance",
        ["CTRL-FINAL-RECORD-GATE"] = "release-governance",
        ["CTRL-IMMUTABLE-V1"] = "release-governance",
        ["CTRL-CONTRACTS-SURFACE"] = "module",
        ["CTRL-CLIENT-SURFACE"] = "module",
        ["CTRL-ROUTE-WIRE-BEHAVIOR"] = "module",
        ["CTRL-EVENT-WIRE-BEHAVIOR"] = "module",
        ["CTRL-ERROR-SEMANTICS"] = "module",
        ["CTRL-PACKAGE-VERSION-BEHAVIOR"] = "platform",
        ["CTRL-ORACLE-TIERING"] = "release-governance",
    };

    private static readonly string[] UxAcceptanceSections =
    [
        "Design System Acceptance Criteria",
        "2.3 Success Criteria",
        "Safety Acceptance Criteria",
        "Responsive Acceptance Criteria",
    ];

    /// <summary>
    /// Verifies the v2 schema is separate from v1, closed, and declares the required governance contracts.
    /// </summary>
    [Fact]
    public void V2SchemaShouldBeSeparateClosedAndGoverned()
    {
        using JsonDocument schema = LoadJson(SchemaPath);
        JsonElement root = schema.RootElement;

        root.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        root.GetProperty("properties").EnumerateObject().Select(property => property.Name).ShouldContain("obligations");
        root.GetProperty("$defs").EnumerateObject().Select(property => property.Name).ShouldContain("dispositionDocument");
        root.GetProperty("$defs").GetProperty("obligation").GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        root.GetProperty("$defs").GetProperty("dispositionDecision").GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        root.GetProperty("$defs").GetProperty("obligation").GetProperty("required").EnumerateArray()
            .Select(value => value.GetString()).ShouldContain("closure");

        ComputeFileSha256(FullPath("docs/release-evidence/manifest.schema.json"))
            .ShouldBe(FrozenV1Hashes["docs/release-evidence/manifest.schema.json"]);
    }

    /// <summary>
    /// Recomputes the PRD and UX denominators and their source-text hashes from authority bytes.
    /// </summary>
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

    /// <summary>
    /// Recomputes public surfaces and every executable conformance assertion from the built assemblies.
    /// </summary>
    [Fact]
    public void PublicSurfacesAndConformanceAssertionsShouldHaveZeroGap()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        JsonElement[] rows = manifest.RootElement.GetProperty("obligations").EnumerateArray().ToArray();

        HashSet<string> expectedContracts = typeof(ConformanceManifestV1).Assembly.GetExportedTypes()
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("Hexalith.Conversations.Contracts", StringComparison.Ordinal))
            .Select(type => $"CONTRACT-{type.Namespace}.{type.Name}")
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> actualContracts = RowsOfKind(rows, "public-contract");
        actualContracts.ShouldBe(expectedContracts, ignoreOrder: true);

        HashSet<string> expectedClient = typeof(IConversationClient).Assembly.GetExportedTypes()
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("Hexalith.Conversations.Client", StringComparison.Ordinal))
            .Select(type => $"CLIENT-{type.Name.Split('`')[0]}")
            .ToHashSet(StringComparer.Ordinal);
        RowsOfKind(rows, "public-client").ShouldBe(expectedClient, ignoreOrder: true);

        HashSet<string> expectedAssertions = typeof(PreservationTraceabilityManifestValidationTest).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<FactAttribute>() is not null || method.GetCustomAttribute<TheoryAttribute>() is not null)
            .Select(method => $"ASSERT-{method.DeclaringType!.Name}.{method.Name}")
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> actualAssertions = RowsOfKind(rows, "conformance-assertion");
        actualAssertions.ShouldBe(expectedAssertions, ignoreOrder: true);
        actualAssertions.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Validates repository containment, hashes, closure shape, governance roles, and immutable v1 bytes.
    /// </summary>
    [Fact]
    public void BindingsClosuresAndFrozenV1BytesShouldValidateIndependently()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement root = manifest.RootElement;

        foreach (JsonElement binding in root.GetProperty("sourceBindings").EnumerateArray())
        {
            string path = binding.GetProperty("path").GetString()!;
            AssertContainedFile(path);
            ComputeFileSha256(FullPath(path)).ShouldBe(binding.GetProperty("sha256").GetString(), path);
        }

        foreach (JsonElement binding in root.GetProperty("identityBindings").EnumerateArray())
        {
            string path = binding.GetProperty("path").GetString()!;
            AssertContainedFile(path);
            ComputeFileSha256(FullPath(path)).ShouldBe(binding.GetProperty("sha256").GetString(), path);
            binding.GetProperty("identity").GetString().ShouldNotBeNullOrWhiteSpace();
        }

        IReadOnlyDictionary<string, JsonElement> decisions = disposition.RootElement.GetProperty("decisions")
            .EnumerateArray().ToDictionary(row => row.GetProperty("dispositionId").GetString()!, StringComparer.Ordinal);
        foreach (JsonElement row in root.GetProperty("obligations").EnumerateArray())
        {
            JsonElement closure = row.GetProperty("closure");
            string closureKind = closure.GetProperty("kind").GetString()!;
            if (closureKind == "governed-disposition")
            {
                string dispositionId = closure.GetProperty("dispositionId").GetString()!;
                decisions.ShouldContainKey(dispositionId);
                JsonElement decision = decisions[dispositionId];
                decision.GetProperty("obligationId").GetString().ShouldBe(row.GetProperty("id").GetString());
                HashSet<string?> roles =
                [
                    decision.GetProperty("evidenceOwner").GetString(),
                    decision.GetProperty("controlOwner").GetString(),
                    decision.GetProperty("approver").GetString(),
                ];
                roles.Count.ShouldBe(3);
                continue;
            }

            closureKind.ShouldBe("evidence");
            foreach (JsonElement evidence in closure.GetProperty("evidence").EnumerateArray())
            {
                string path = evidence.GetProperty("path").GetString()!;
                AssertContainedFile(path);
                path.ShouldNotStartWith("references/");
                path.ShouldNotContain("/bin/");
                path.ShouldNotContain("/obj/");
                path.ShouldNotBe(row.GetProperty("source").GetProperty("path").GetString());
                ComputeFileSha256(FullPath(path)).ShouldBe(evidence.GetProperty("sha256").GetString(), path);
                string authorityPath = evidence.GetProperty("authorityPath").GetString()!;
                ComputeFileSha256(FullPath(authorityPath)).ShouldBe(evidence.GetProperty("authoritySha256").GetString(), authorityPath);
            }
        }

        ComputeFileSha256(FullPath(DispositionPath))
            .ShouldBe(root.GetProperty("governanceInputs").GetProperty("dispositionSha256").GetString());
        foreach ((string path, string expectedHash) in FrozenV1Hashes)
        {
            ComputeFileSha256(FullPath(path)).ShouldBe(expectedHash, $"Frozen v1 path '{path}' changed.");
        }
    }

    /// <summary>
    /// Validates current-control ownership and preserves the unfinished Story 6.9 state without manufacturing triage.
    /// </summary>
    [Fact]
    public void CurrentControlsAndTierPrerequisiteShouldStayTruthful()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        using JsonDocument decision = LoadJson(TierDecisionPath);
        JsonElement root = manifest.RootElement;
        JsonElement[] controls = root.GetProperty("obligations").EnumerateArray()
            .Where(row => row.GetProperty("kind").GetString() == "current-control")
            .ToArray();

        controls.Length.ShouldBe(ExpectedControlOwners.Count);
        foreach (JsonElement control in controls)
        {
            string id = control.GetProperty("id").GetString()!;
            ExpectedControlOwners.ShouldContainKey(id);
            control.GetProperty("controlOwner").GetString().ShouldBe(ExpectedControlOwners[id]);
        }

        JsonElement tiering = root.GetProperty("tiering");
        ComputeFileSha256(FullPath(TierDecisionPath)).ShouldBe(tiering.GetProperty("decisionSha256").GetString());
        tiering.GetProperty("bothTiersReleaseGated").GetBoolean().ShouldBeTrue();
        JsonElement triageResults = decision.RootElement.GetProperty("triageResults");
        if (triageResults.ValueKind == JsonValueKind.Null)
        {
            tiering.GetProperty("triageStatus").GetString().ShouldBe("pending-story-6.9");
            tiering.GetProperty("portableStructuralEvidence").ValueKind.ShouldBe(JsonValueKind.Null);
            root.GetProperty("obligations").EnumerateArray()
                .Where(row => row.GetProperty("kind").GetString() == "conformance-assertion")
                .ShouldAllBe(row => row.GetProperty("tier").GetString() == "pending-story-6.9");
            root.GetProperty("status").GetString().ShouldBe("pending-prerequisites");
        }
        else
        {
            tiering.GetProperty("triageStatus").GetString().ShouldBe("triaged");
            tiering.GetProperty("triageSha256").GetString().ShouldNotBeNullOrWhiteSpace();
            tiering.GetProperty("portableStructuralEvidence").ValueKind.ShouldBe(JsonValueKind.Object);
        }
    }

    /// <summary>
    /// Reconstructs the reviewer projection from JSON and requires byte parity.
    /// </summary>
    [Fact]
    public void MarkdownProjectionShouldBeByteExact()
    {
        using JsonDocument manifest = LoadJson(ManifestPath);
        File.ReadAllText(FullPath(MarkdownPath), Encoding.UTF8).ShouldBe(RenderMarkdown(manifest.RootElement));
    }

    /// <summary>
    /// Proves independent validation rejects representative denominator, hash, ownership, tier, and mutation faults.
    /// </summary>
    [Fact]
    public void FaultInjectedCandidatesShouldFailWithStableDiagnostics()
    {
        string json = File.ReadAllText(FullPath(ManifestPath), Encoding.UTF8);

        JsonObject deleted = JsonNode.Parse(json)!.AsObject();
        deleted["obligations"]!.AsArray().RemoveAt(0);
        ValidateCandidate(deleted).ShouldContain("DENOMINATOR_GAP");

        JsonObject duplicated = JsonNode.Parse(json)!.AsObject();
        JsonArray duplicatedRows = duplicated["obligations"]!.AsArray();
        duplicatedRows.Add(duplicatedRows[0]!.DeepClone());
        ValidateCandidate(duplicated).ShouldContain("DUPLICATE_OBLIGATION");

        JsonObject sourceMutation = JsonNode.Parse(json)!.AsObject();
        JsonObject firstSource = sourceMutation["obligations"]![0]!["source"]!.AsObject();
        firstSource["textSha256"] = new string('0', 64);
        ValidateCandidate(sourceMutation).ShouldContain("SOURCE_TEXT_HASH_MISMATCH");

        JsonObject ownerMutation = JsonNode.Parse(json)!.AsObject();
        JsonObject ownerRow = ownerMutation["obligations"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(row => row["id"]!.GetValue<string>() == "CTRL-CANONICAL-HOST-SHAPE");
        ownerRow["controlOwner"] = "module";
        ValidateCandidate(ownerMutation).ShouldContain("CONTROL_OWNERSHIP_REVERSAL");

        JsonObject tierMutation = JsonNode.Parse(json)!.AsObject();
        JsonObject tierRow = tierMutation["obligations"]!.AsArray()
            .Select(node => node!.AsObject())
            .First(row => row["kind"]!.GetValue<string>() == "conformance-assertion");
        tierRow.Remove("tier");
        ValidateCandidate(tierMutation).ShouldContain("TIER_REQUIRED");

        JsonObject evidenceMutation = JsonNode.Parse(json)!.AsObject();
        JsonObject evidenceRow = evidenceMutation["obligations"]!.AsArray()
            .Select(node => node!.AsObject())
            .First(row => row["closure"]!["kind"]!.GetValue<string>() == "evidence");
        evidenceRow["closure"]!["evidence"]![0]!["sha256"] = new string('0', 64);
        ValidateCandidate(evidenceMutation).ShouldContain("EVIDENCE_HASH_MISMATCH");

        JsonObject mutationIds = JsonNode.Parse(json)!.AsObject();
        mutationIds["mutationGovernance"]!["changedIds"]!.AsArray().RemoveAt(0);
        ValidateCandidate(mutationIds).ShouldContain("MUTATION_CHANGED_IDS_MISMATCH");
    }

    private static HashSet<string> ValidateCandidate(JsonObject candidate)
    {
        HashSet<string> diagnostics = new(StringComparer.Ordinal);
        IReadOnlyDictionary<string, string> expectedHashes = ExtractRequirementAndUxSourceHashes();
        HashSet<string> expectedIds = ExpectedAllIds();
        JsonArray rows = candidate["obligations"]!.AsArray();
        string[] ids = rows.Select(row => row!["id"]!.GetValue<string>()).ToArray();
        if (ids.Length != expectedIds.Count || expectedIds.Except(ids, StringComparer.Ordinal).Any())
        {
            diagnostics.Add("DENOMINATOR_GAP");
        }

        if (ids.Length != ids.Distinct(StringComparer.Ordinal).Count())
        {
            diagnostics.Add("DUPLICATE_OBLIGATION");
        }

        foreach (JsonObject row in rows.Select(node => node!.AsObject()))
        {
            string id = row["id"]!.GetValue<string>();
            if (expectedHashes.TryGetValue(id, out string? expectedHash)
                && row["source"]!["textSha256"]!.GetValue<string>() != expectedHash)
            {
                diagnostics.Add("SOURCE_TEXT_HASH_MISMATCH");
            }

            if (ExpectedControlOwners.TryGetValue(id, out string? expectedOwner)
                && row["controlOwner"]!.GetValue<string>() != expectedOwner)
            {
                diagnostics.Add("CONTROL_OWNERSHIP_REVERSAL");
            }

            if (row["kind"]!.GetValue<string>() == "conformance-assertion" && row["tier"] is null)
            {
                diagnostics.Add("TIER_REQUIRED");
            }

            if (row["closure"]!["kind"]!.GetValue<string>() == "evidence")
            {
                foreach (JsonNode? evidenceNode in row["closure"]!["evidence"]!.AsArray())
                {
                    JsonObject evidence = evidenceNode!.AsObject();
                    string path = evidence["path"]!.GetValue<string>();
                    if (!File.Exists(FullPath(path)) || ComputeFileSha256(FullPath(path)) != evidence["sha256"]!.GetValue<string>())
                    {
                        diagnostics.Add("EVIDENCE_HASH_MISMATCH");
                    }
                }
            }
        }

        string[] changedIds = candidate["mutationGovernance"]!["changedIds"]!.AsArray()
            .Select(node => node!.GetValue<string>()).ToArray();
        if (!changedIds.SequenceEqual(ids, StringComparer.Ordinal))
        {
            diagnostics.Add("MUTATION_CHANGED_IDS_MISMATCH");
        }

        return diagnostics;
    }

    private static HashSet<string> ExpectedAllIds()
    {
        HashSet<string> ids = ExtractRequirementAndUxSourceHashes().Keys.ToHashSet(StringComparer.Ordinal);
        ids.UnionWith(typeof(ConformanceManifestV1).Assembly.GetExportedTypes()
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("Hexalith.Conversations.Contracts", StringComparison.Ordinal))
            .Select(type => $"CONTRACT-{type.Namespace}.{type.Name}"));
        ids.UnionWith(typeof(IConversationClient).Assembly.GetExportedTypes()
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("Hexalith.Conversations.Client", StringComparison.Ordinal))
            .Select(type => $"CLIENT-{type.Name.Split('`')[0]}"));
        ids.UnionWith(ExpectedControlOwners.Keys);
        ids.UnionWith(typeof(PreservationTraceabilityManifestValidationTest).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<FactAttribute>() is not null || method.GetCustomAttribute<TheoryAttribute>() is not null)
            .Select(method => $"ASSERT-{method.DeclaringType!.Name}.{method.Name}"));
        return ids;
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

                string block = string.Join('\n', prdLines[index..end]).Trim();
                result[initiative.Groups[1].Value] = ComputeTextSha256(Normalize(block));
            }

            Match feature = featurePattern.Match(prdLines[index]);
            if (feature.Success)
            {
                result[feature.Groups[1].Value] = ComputeTextSha256(Normalize(feature.Groups["text"].Value));
            }
        }

        string[] uxMapLines = File.ReadAllLines(FullPath(UxMapPath), Encoding.UTF8);
        Regex uxDecisionPattern = new("^\\| (UX-DR[0-9]+) \\| (?<section>[^|]+) \\| (?<summary>[^|]+) \\|", RegexOptions.CultureInvariant);
        foreach (string line in uxMapLines)
        {
            Match match = uxDecisionPattern.Match(line);
            if (match.Success)
            {
                string text = $"{match.Groups["section"].Value.Trim()}: {match.Groups["summary"].Value.Trim()}";
                result[match.Groups[1].Value] = ComputeTextSha256(Normalize(text));
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
                string normalized = Normalize(uxLines[index][2..].Trim());
                string hash = ComputeTextSha256(normalized);
                result[$"UX-AC-{Slug(section)}-{ordinal:00}-{hash[..12]}"] = hash;
            }
        }

        return result;
    }

    private static HashSet<string> RowsOfKind(IEnumerable<JsonElement> rows, string kind)
        => rows.Where(row => row.GetProperty("kind").GetString() == kind)
            .Select(row => row.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

    private static bool IsRequirementOrUx(string? kind)
        => kind is "initiative-fr" or "feature-fr" or "feature-nfr" or "ux-decision" or "ux-acceptance";

    private static string RenderMarkdown(JsonElement manifest)
    {
        List<string> lines =
        [
            "# Preservation Traceability Manifest V2",
            string.Empty,
            "> Generated from `preservation-traceability-manifest-v2.json`. The JSON is authoritative; do not edit this projection.",
            string.Empty,
            $"- Manifest version: `{manifest.GetProperty("manifestVersion").GetString()}`",
            $"- Status: `{manifest.GetProperty("status").GetString()}`",
            $"- Architecture authority: `{manifest.GetProperty("authorityVersions").GetProperty("architecture").GetString()}`",
            $"- Epic authority: `{manifest.GetProperty("authorityVersions").GetProperty("epic").GetString()}`",
            $"- Story 6.9 triage: `{manifest.GetProperty("tiering").GetProperty("triageStatus").GetString()}`",
            "- Supersession boundary: v2 adds complete traceability while every v1 byte remains immutable.",
            string.Empty,
            "## Inventory Summary",
            string.Empty,
            "| Kind | Expected | Actual | Governed disposition |",
            "| --- | ---: | ---: | ---: |",
        ];
        foreach (JsonElement summary in manifest.GetProperty("summaries").EnumerateArray())
        {
            lines.Add($"| {EscapeMarkdown(summary.GetProperty("kind").GetString())} | {summary.GetProperty("expected").GetInt32()} | {summary.GetProperty("actual").GetInt32()} | {summary.GetProperty("unresolved").GetInt32()} |");
        }

        lines.AddRange([string.Empty, "## Authority Bindings", string.Empty, "| Path | SHA-256 | Role |", "| --- | --- | --- |"]);
        foreach (JsonElement binding in manifest.GetProperty("sourceBindings").EnumerateArray())
        {
            lines.Add($"| `{binding.GetProperty("path").GetString()}` | `{binding.GetProperty("sha256").GetString()}` | {binding.GetProperty("role").GetString()} |");
        }

        lines.AddRange([string.Empty, "## Obligations", string.Empty]);
        foreach (IGrouping<string, JsonElement> group in manifest.GetProperty("obligations").EnumerateArray()
                     .GroupBy(row => row.GetProperty("kind").GetString()!))
        {
            lines.AddRange(
            [
                $"### {group.Key}",
                string.Empty,
                "| ID | Control owner | Closure | Tier | Source | Source text SHA-256 |",
                "| --- | --- | --- | --- | --- | --- |",
            ]);
            foreach (JsonElement row in group)
            {
                JsonElement source = row.GetProperty("source");
                string tier = row.TryGetProperty("tier", out JsonElement tierElement) ? tierElement.GetString()! : "n/a";
                lines.Add(
                    $"| `{EscapeMarkdown(row.GetProperty("id").GetString())}` | {row.GetProperty("controlOwner").GetString()} | "
                    + $"{row.GetProperty("closure").GetProperty("kind").GetString()} | {tier} | "
                    + $"`{source.GetProperty("path").GetString()}:{source.GetProperty("line").GetInt32()}` | `{source.GetProperty("textSha256").GetString()}` |");
            }

            lines.Add(string.Empty);
        }

        return string.Join('\n', lines).TrimEnd() + "\n";
    }

    private static string EscapeMarkdown(string? value)
        => (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string Normalize(string value)
        => Regex.Replace(value, "\\s+", " ", RegexOptions.CultureInvariant).Trim();

    private static string Slug(string value)
        => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-", RegexOptions.CultureInvariant).Trim('-');

    private static void AssertContainedFile(string repositoryPath)
    {
        Path.IsPathRooted(repositoryPath).ShouldBeFalse();
        string root = RepositoryRoot();
        string fullPath = Path.GetFullPath(Path.Combine(root, repositoryPath));
        fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal).ShouldBeTrue(repositoryPath);
        File.Exists(fullPath).ShouldBeTrue(repositoryPath);
    }

    private static JsonDocument LoadJson(string repositoryPath)
        => JsonDocument.Parse(File.ReadAllBytes(FullPath(repositoryPath)));

    private static string FullPath(string repositoryPath)
        => Path.Combine(RepositoryRoot(), repositoryPath.Replace('/', Path.DirectorySeparatorChar));

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

        throw new DirectoryNotFoundException("Could not locate Hexalith.Conversations.slnx.");
    }

    private static string ComputeFileSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string ComputeTextSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
