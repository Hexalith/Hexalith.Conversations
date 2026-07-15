// <copyright file="ArchitecturePlanningAuthorityValidationTest.cs" company="ITANEO">
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
/// Story 6.1 — enforces the rebaselined architecture and append-only Epic 6 planning authority.
/// </summary>
public sealed class ArchitecturePlanningAuthorityValidationTest
{
    private const string ArchitectureVersion = "conversations-architecture-2026-07-15-v1";
    private const string BaselineRevision = "f31aa5ada2e37e1ec5f3e4b8e907525b37da863f";
    private const string OverlayVersion = "epic-6-authority-2026-07-15-v1";
    private const int HistoricalEpicPrefixLength = 55536;
    private const string HistoricalEpicPrefixSha256 = "bd437b802513591c4af299ff0997bb694ced40304e1a178c3d53e95f88f0e8a8";

    private static readonly string[] ExpectedHistoricalStories =
    [
        "1.1", "1.2", "1.3", "1.4", "1.5",
        "2.1", "2.2", "2.3", "2.4", "2.5", "2.6", "2.7",
        "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7",
        "4.1", "4.2",
        "5.1", "5.2", "5.3",
    ];

    [Fact]
    public void ArchitectureFrontmatterShouldBindCanonicalCurrentAuthority()
    {
        string architecture = ReadRepositoryFile("_bmad-output/planning-artifacts/architecture.md");
        string frontmatter = ExtractFrontmatter(architecture);

        frontmatter.ShouldContain("status: 'corrective-implementation-only'");
        frontmatter.ShouldContain("rebaselinedAt: '2026-07-15'");
        frontmatter.ShouldContain($"authorityVersion: '{ArchitectureVersion}'");
        frontmatter.ShouldContain("prd: '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md'");
        frontmatter.ShouldContain("addendum: '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md'");
        frontmatter.ShouldContain("sprint-change-proposal-2026-07-15.md");
        frontmatter.ShouldContain("sprint-change-proposal-2026-07-15-submodule-promotion-completion-gate.md");
        frontmatter.ShouldContain($"baselineRevision: '{BaselineRevision}'");
    }

    [Fact]
    public void ArchitectureRegistersExactInitiativeLandingZonesAndDeferredFrSixteen()
    {
        string architecture = ReadRepositoryFile("_bmad-output/planning-artifacts/architecture.md");
        string section = ExtractSection(architecture, "### Initiative Landing-Zone Register", "### Open-Question Disposition Register");
        string[] rows = MarkdownDataRows(section, "FR-");

        rows.Length.ShouldBe(7);
        rows.Select(GetFirstTableCell).ShouldBe(["FR-10", "FR-11", "FR-12", "FR-13", "FR-14", "FR-15", "FR-16"]);
        rows.ShouldAllBe(row => TableCells(row).Length == 4);
        rows.ShouldAllBe(row => TableCells(row).Skip(1).All(cell => !string.IsNullOrWhiteSpace(cell)));

        rows.Single(row => GetFirstTableCell(row) == "FR-10").ShouldContain("EventStore.ServiceDefaults");
        rows.Single(row => GetFirstTableCell(row) == "FR-10").ShouldContain("UseEventStoreDomainService");
        rows.Single(row => GetFirstTableCell(row) == "FR-11").ShouldContain("Hexalith.Commons.TenantAccess");
        rows.Single(row => GetFirstTableCell(row) == "FR-12").ShouldContain("Hexalith.Commons.Http");
        rows.Single(row => GetFirstTableCell(row) == "FR-13").ShouldContain("EventStore.Aspire");
        rows.Single(row => GetFirstTableCell(row) == "FR-14").ShouldContain("Hexalith.Commons.Serialization");
        rows.Single(row => GetFirstTableCell(row) == "FR-15").ShouldContain("Hexalith.Commons.Diagnostics");
        rows.Single(row => GetFirstTableCell(row) == "FR-16").ShouldContain("deferred-non-activated");
    }

    [Fact]
    public void ArchitectureShouldResolveEveryOpenQuestionExactlyOnce()
    {
        string architecture = ReadRepositoryFile("_bmad-output/planning-artifacts/architecture.md");
        string section = ExtractSection(architecture, "### Open-Question Disposition Register", "### SM-C2 Versioned Hot-Path Inventory And Gate");
        string[] rows = MarkdownDataRows(section, "OQ-");

        rows.Length.ShouldBe(5);
        rows.Select(GetFirstTableCell).ShouldBe(["OQ-1", "OQ-2", "OQ-3", "OQ-4", "OQ-5"]);
        rows.ShouldAllBe(row => TableCells(row).Length == 4);
        rows.ShouldAllBe(row => TableCells(row).Skip(1).All(cell => !string.IsNullOrWhiteSpace(cell)));
        rows.ShouldAllBe(row => TableCells(row)[1].StartsWith("resolved-", StringComparison.Ordinal));

        rows.Single(row => GetFirstTableCell(row) == "OQ-2").ShouldContain(">=40%");
        rows.Single(row => GetFirstTableCell(row) == "OQ-4").ShouldContain("FR-16 is deferred");
        rows.Single(row => GetFirstTableCell(row) == "OQ-5").ShouldContain("5% P95 regression");
    }

    [Fact]
    public void SmCTwoShouldFreezeNonemptyInventoryAndOneToOneComparablePostResults()
    {
        string architecture = ReadRepositoryFile("_bmad-output/planning-artifacts/architecture.md");
        string section = ExtractSection(architecture, "### SM-C2 Versioned Hot-Path Inventory And Gate", "### Still-Binding Domain And Runtime Decisions");
        string[] rows = MarkdownDataRows(section, "HP-");

        section.ShouldContain("sm-c2-hot-path-inventory-v1");
        rows.Select(GetFirstTableCell).ShouldBe(["HP-CREATE", "HP-APPEND", "HP-LIST", "HP-OPEN"]);
        rows.ShouldAllBe(row => TableCells(row).Length == 5);
        rows.ShouldAllBe(row => !string.IsNullOrWhiteSpace(TableCells(row)[4]));
        section.ShouldContain("one baseline result for every row");
        section.ShouldContain("exactly one disposition and result for every baseline row");
        section.ShouldContain("post P95 <= 1.05 x baseline P95");

        foreach (string semantic in new[] { "workload and data", "concurrency", "environment and runtime", "benchmark tool/version", "warm/cold classification", "repetition policy", "raw-result processing", "measured commit" })
        {
            section.ShouldContain(semantic);
        }
    }

    [Fact]
    public void TargetTreeAndReadinessShouldBeCorrectiveAndPlatformOwned()
    {
        string architecture = ReadRepositoryFile("_bmad-output/planning-artifacts/architecture.md");
        string targetTreeSection = ExtractSection(architecture, "### Corrected Target Directory Structure", "### Historical May 14 Directory Structure (Superseded)");
        string targetTree = ExtractFirstFencedBlock(targetTreeSection);
        string readiness = ExtractSection(architecture, "### Corrective Readiness", "## Project Context Analysis");

        targetTree.ShouldContain("Hexalith.Conversations.Contracts/");
        targetTree.ShouldContain("Hexalith.Conversations.Server/");
        targetTree.ShouldNotContain("Hexalith.Conversations.AppHost");
        targetTree.ShouldNotContain("Hexalith.Conversations.Aspire");
        targetTree.ShouldNotContain("Hexalith.Conversations.ServiceDefaults");

        architecture.ShouldContain("pre-Story-6.2 drift and migration input only");
        architecture.ShouldContain("builder.AddEventStoreDomainService");
        architecture.ShouldContain("app.UseEventStoreDomainService");
        architecture.ShouldContain("must never teach direct `MapEventStoreDomainService()` use");
        readiness.ShouldContain("READY FOR CORRECTIVE IMPLEMENTATION ONLY");
        readiness.ShouldContain("6.1 -> 6.7 -> 6.2");
        readiness.ShouldContain("Story 6.6 is last");
    }

    [Fact]
    public void StillBindingReplayProjectionParticipantIdempotencyAndLegalRulesShouldRemain()
    {
        string architecture = ReadRepositoryFile("_bmad-output/planning-artifacts/architecture.md");
        string section = ExtractSection(architecture, "### Still-Binding Domain And Runtime Decisions", "### Promotion Completion Invariant");

        section.ShouldContain("Mixed-version streams");
        section.ShouldContain("readers/upcasters");
        section.ShouldContain("EventStore history has precedence");
        section.ShouldContain("quarantined");
        section.ShouldContain("rebuild starts from EventStore");
        section.ShouldContain("Parties validation fails closed");
        section.ShouldContain("policy-defined non-personal hydration placeholder");
        section.ShouldContain("same key with a different payload");
        section.ShouldContain("unknown client/provider outcome");
        section.ShouldContain("legal-policy mechanisms");
    }

    [Fact]
    public void EpicPlanShouldPreserveHistoricalPrefixAndContainExactDispositionRows()
    {
        byte[] epicBytes = File.ReadAllBytes(RepositoryPath("_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md"));
        epicBytes.Length.ShouldBeGreaterThan(HistoricalEpicPrefixLength);
        ComputeSha256(epicBytes.AsSpan(0, HistoricalEpicPrefixLength)).ShouldBe(HistoricalEpicPrefixSha256);

        string epics = Encoding.UTF8.GetString(epicBytes);
        epics.IndexOf("EPIC-6-AUTHORITY-OVERLAY:BEGIN", StringComparison.Ordinal).ShouldBe(HistoricalEpicPrefixLength + 1);
        string dispositionSection = ExtractSection(epics, "### Exact Historical Story Dispositions", "### Corrective Initiative-FR Coverage");
        string[] rows = MarkdownDataRows(dispositionSection, string.Empty);

        rows.Length.ShouldBe(24);
        rows.Select(GetFirstTableCell).ShouldBe(ExpectedHistoricalStories);
        rows.ShouldAllBe(row => TableCells(row).Length == 3);
        rows.ShouldAllBe(row => TableCells(row).Skip(1).All(cell => !string.IsNullOrWhiteSpace(cell)));
    }

    [Fact]
    public void EpicOverlayShouldPreserveFullDenominatorAndCorrectiveFrCoverage()
    {
        string epics = ReadRepositoryFile("_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md");
        string overlay = ExtractBetween(epics, "EPIC-6-AUTHORITY-OVERLAY:BEGIN", "EPIC-6-AUTHORITY-OVERLAY:END");
        string requirementSection = ExtractSection(overlay, "### Requirement Authority And Denominators", "### Exact Historical Story Dispositions");
        int[] initiativeFrs = Regex.Matches(requirementSection, @"\bFR-(\d{1,2})\b", RegexOptions.CultureInvariant)
            .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(number => number)
            .ToArray();

        initiativeFrs.ShouldBe(Enumerable.Range(1, 20).ToArray());
        requirementSection.ShouldContain("FR-16 is the only initiative non-activation");
        requirementSection.ShouldContain("all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria");
        requirementSection.ShouldContain("named owner approval, recorded rationale, and compatibility evidence");
        requirementSection.ShouldContain("13,289-LOC SM-1 baseline");

        string coverage = ExtractSection(overlay, "### Corrective Initiative-FR Coverage", "## Epic 6:");
        foreach (string required in new[] { "FR-3", "FR-10", "FR-13", "FR-17", "FR-18", "FR-19", "FR-20" })
        {
            coverage.ShouldContain(required);
        }
    }

    [Fact]
    public void EpicOverlayAndGeneratedContextShouldBeVersionAndStoryEquivalent()
    {
        string epics = ReadRepositoryFile("_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md");
        string context = ReadRepositoryFile("_bmad-output/implementation-artifacts/epic-6-context.md");

        epics.ShouldContain($"version={OverlayVersion}");
        context.ShouldContain($"overlay_version: '{OverlayVersion}'");
        context.ShouldContain($"architecture_version: '{ArchitectureVersion}'");
        context.ShouldStartWith("---");
        context.ShouldContain("# Epic 6 Context:");

        for (int story = 1; story <= 7; story++)
        {
            epics.ShouldContain($"### Story 6.{story}:");
            context.ShouldContain($"### 6.{story} ");
        }

        foreach (string semantic in new[]
        {
            "FR-16 is the only non-activation",
            "13,289 LOC",
            "104 Feature-FRs",
            "77 Feature-NFRs",
            "52 UX decisions",
            "post P95 <= 1.05 x baseline P95",
            "6.1 -> 6.7 -> 6.2",
            "Never initialize, update, or traverse nested submodules",
            "6.6 is last",
        })
        {
            context.ShouldContain(semantic);
        }
    }

    [Fact]
    public void PromotionCompletionInvariantShouldBeScopedToDeclaredRootGitlinks()
    {
        string architecture = ReadRepositoryFile("_bmad-output/planning-artifacts/architecture.md");
        string section = ExtractSection(architecture, "### Promotion Completion Invariant", "### Corrective Readiness");

        section.ShouldContain("exact root `references/...` paths");
        section.ShouldContain("clean including untracked files");
        section.ShouldContain("availability policy");
        section.ShouldContain("mode-`160000` gitlink");
        section.ShouldContain("root `.gitmodules`");
        section.ShouldContain("never initializes or traverses nested submodules");
        section.ShouldContain("unrelated state as warnings");
    }

    [Fact]
    public void NamedPlatformLandingZonesShouldExposeSignatureCompatiblePublicApis()
    {
        AssertPublicStaticMethod(
            "references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs",
            "WebApplicationBuilder",
            "AddEventStoreDomainService",
            "this WebApplicationBuilder builder");
        AssertPublicStaticMethod(
            "references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs",
            "WebApplication",
            "UseEventStoreDomainService",
            "this WebApplication app");
        AssertPublicStaticMethod(
            "references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs",
            "WebApplication",
            "MapDefaultEndpoints",
            "this WebApplication app");
        AssertPublicStaticMethod(
            "references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainTelemetryExtensions.cs",
            "WebApplicationBuilder",
            "AddEventStoreDomainTelemetry",
            "this WebApplicationBuilder builder",
            "string domain");
        AssertPublicStaticMethod(
            "references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs",
            "HexalithEventStoreResources",
            "AddHexalithEventStore",
            "this IDistributedApplicationBuilder builder");
        AssertPublicStaticMethod(
            "references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs",
            "IResourceBuilder<ProjectResource>",
            "AddEventStoreDomainModule");

        AssertPublicStaticMethod(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/TenantAccessRegistration.cs",
            "IServiceCollection",
            "AddTenantAccess",
            "this IServiceCollection services");
        AssertPublicType(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/TenantAccessProjectionHandler.cs",
            "sealed class",
            "TenantAccessProjectionHandler");
        AssertPublicStaticMethod(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.Http/HttpClientRegistration.cs",
            "IHttpClientBuilder",
            "AddTypedHttpClient",
            "this IServiceCollection services");
        AssertPublicStaticMethod(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/PolymorphicTypeRegistry.cs",
            "PolymorphicTypeRegistry",
            "Create",
            "IEnumerable<PolymorphicTypeRegistration> registrations");
        AssertPublicStaticMethod(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.Serialization/JsonSerializationOptions.cs",
            "JsonSerializerOptions",
            "CreateWeb");
        AssertPublicType(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryMeter.cs",
            "sealed class",
            "BoundedTelemetryMeter");
        AssertPublicInstanceMethod(
            "references/Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryMeter.cs",
            "BoundedTelemetryCounter",
            "CreateCounter",
            "BoundedTelemetryCounterDefinition definition");
    }

    private static void AssertPublicStaticMethod(string relativePath, string returnType, string methodName, params string[] parameterFragments)
        => AssertPublicMethod(relativePath, returnType, methodName, isStatic: true, parameterFragments);

    private static void AssertPublicInstanceMethod(string relativePath, string returnType, string methodName, params string[] parameterFragments)
        => AssertPublicMethod(relativePath, returnType, methodName, isStatic: false, parameterFragments);

    private static void AssertPublicMethod(string relativePath, string returnType, string methodName, bool isStatic, params string[] parameterFragments)
    {
        string source = ReadRepositoryFile(relativePath);
        string staticToken = isStatic ? @"static\s+" : string.Empty;
        string pattern = $@"\bpublic\s+{staticToken}{Regex.Escape(returnType)}\s+{Regex.Escape(methodName)}(?:<[^>]+>)?\s*\((?<parameters>[\s\S]*?)\)\s*(?:where\b|\{{|=>)";
        MatchCollection matches = Regex.Matches(source, pattern, RegexOptions.CultureInvariant);
        matches.Count.ShouldBeGreaterThan(0, $"Expected public {(isStatic ? "static " : string.Empty)}{returnType} {methodName}(...) in {relativePath}.");

        matches.Any(match => parameterFragments.All(fragment => match.Groups["parameters"].Value.Contains(fragment, StringComparison.Ordinal)))
            .ShouldBeTrue($"No public signature for {methodName} in {relativePath} contained required parameters: {string.Join(", ", parameterFragments)}.");
    }

    private static void AssertPublicType(string relativePath, string typeKind, string typeName)
    {
        string source = ReadRepositoryFile(relativePath);
        Regex.IsMatch(source, $@"\bpublic\s+{Regex.Escape(typeKind)}\s+{Regex.Escape(typeName)}(?:<|\b)", RegexOptions.CultureInvariant)
            .ShouldBeTrue($"Expected public {typeKind} {typeName} in {relativePath}.");
    }

    private static string ExtractFrontmatter(string content)
    {
        content.ShouldStartWith("---\n");
        int end = content.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(4);
        return content[4..end];
    }

    private static string ExtractSection(string content, string startHeading, string nextHeading)
        => ExtractBetween(content, startHeading, nextHeading);

    private static string ExtractBetween(string content, string startMarker, string endMarker)
    {
        int start = content.IndexOf(startMarker, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"Missing start marker '{startMarker}'.");
        int end = content.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start, $"Missing end marker '{endMarker}' after '{startMarker}'.");
        return content[start..end];
    }

    private static string ExtractFirstFencedBlock(string content)
    {
        int start = content.IndexOf("```text\n", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        start += "```text\n".Length;
        int end = content.IndexOf("\n```", start, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start);
        return content[start..end];
    }

    private static string[] MarkdownDataRows(string section, string firstCellPrefix)
        => section.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("|", StringComparison.Ordinal))
            .Where(line =>
            {
                string first = GetFirstTableCell(line);
                return !string.Equals(first, "Story", StringComparison.Ordinal)
                    && !string.Equals(first, "Requirement", StringComparison.Ordinal)
                    && !string.Equals(first, "ID", StringComparison.Ordinal)
                    && !string.Equals(first, "Hot-path ID", StringComparison.Ordinal)
                    && !first.StartsWith("---", StringComparison.Ordinal)
                    && first.StartsWith(firstCellPrefix, StringComparison.Ordinal);
            })
            .ToArray();

    private static string GetFirstTableCell(string row)
        => TableCells(row)[0];

    private static string[] TableCells(string row)
        => row.Trim().Trim('|').Split('|', StringSplitOptions.TrimEntries);

    private static string ComputeSha256(ReadOnlySpan<byte> bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string ReadRepositoryFile(string relativePath)
        => File.ReadAllText(RepositoryPath(relativePath));

    private static string RepositoryPath(string relativePath)
        => Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

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
