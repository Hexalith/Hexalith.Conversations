// <copyright file="ResponsibilityBoundaryValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Documentation;

/// <summary>
/// Validates the responsibility boundary document against FR104 requirements.
/// </summary>
public sealed partial class ResponsibilityBoundaryValidationTest
{
    private static readonly string[] RequiredSections =
    [
        "## Overview",
        "## What Conversations Owns",
        "## Responsibility Boundaries",
        "## Inherited Platform Controls",
        "## Requirement Mapping",
        "## Related Documentation",
    ];

    private static readonly string[] RequiredAdjacentSystemTerms =
    [
        "chatbot",
        "llm provider",
        "legal-hold",
        "attachment",
        "identity",
        "tenant lifecycle",
        "project",
        "folder",
        "Parties",
        "platform",
    ];

    private static readonly string[] RequiredOwnedConceptTerms =
    [
        "ConversationId",
        "PartyId",
        "idempotent",
        "projection",
        "EventStore",
        "fail-closed",
    ];

    private static readonly string[] RequiredBoundaryStructureTerms =
    [
        "source of truth",
        "failure semantics",
        "evidence",
        "handoff",
    ];

    private static readonly string[] RequiredInheritedControlSystems =
    [
        "EventStore",
        "Tenants",
        "Parties",
        "Dapr",
        "Aspire",
        "platform",
    ];

    private static readonly string[] ForbiddenBoundaryDocFragments =
    [
        "other-tenant",
        "redacted content",
        "provider-a",
        "provider-session",
        "provider payload",
        "tenant:",
        "party:",
        "conv:",
        "raw exception",
        "C:\\",
        "D:\\",
    ];

    private static readonly string[] OwnershipViolationPhrases =
    [
        "Conversations owns the tenant lifecycle",
        "Conversations manages Party personal data",
        "Conversations controls EventStore",
        "Conversations owns attachment",
        "Conversations manages authentication",
        "Conversations controls legal-hold",
        "Conversations owns the project lifecycle",
        "Conversations owns the folder lifecycle",
        "Conversations bypasses",
        "Conversations substitutes",
    ];

    [Fact]
    public void ResponsibilityBoundaryDocument_Exists_AtExpectedPath()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "responsibility-boundaries.md");
        File.Exists(path).ShouldBeTrue($"Expected responsibility boundary document at '{path}'.");
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_ContainsAllRequiredSections()
    {
        string doc = ReadBoundaryDoc();

        foreach (string section in RequiredSections)
        {
            doc.ShouldContain(section, Case.Sensitive);
        }
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_MentionsAll10AdjacentSystems()
    {
        string doc = ReadBoundaryDoc();
        string docLower = doc.ToLowerInvariant();

        foreach (string term in RequiredAdjacentSystemTerms)
        {
            docLower.ShouldContain(term.ToLowerInvariant());
        }

        bool mentionsTenants = docLower.Contains("hexalith.tenants") || docLower.Contains("tenant lifecycle");
        mentionsTenants.ShouldBeTrue("Document must mention Hexalith.Tenants or tenant lifecycle.");

        bool mentionsParties = doc.Contains("Parties") || doc.Contains("PartyId") || doc.Contains("Hexalith.Parties");
        mentionsParties.ShouldBeTrue("Document must mention Parties, PartyId, or Hexalith.Parties.");
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_MentionsConversationsOwnedConcepts()
    {
        string doc = ReadBoundaryDoc();

        foreach (string term in RequiredOwnedConceptTerms)
        {
            doc.ShouldContain(term, Case.Sensitive);
        }
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_MentionsBoundaryStructure()
    {
        string docLower = ReadBoundaryDoc().ToLowerInvariant();

        foreach (string term in RequiredBoundaryStructureTerms)
        {
            docLower.ShouldContain(term.ToLowerInvariant());
        }
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_MentionsInheritedControls()
    {
        string doc = ReadBoundaryDoc();

        foreach (string system in RequiredInheritedControlSystems)
        {
            doc.ShouldContain(system, Case.Sensitive);
        }
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_MentionsRequirementFR104()
    {
        string doc = ReadBoundaryDoc();
        doc.ShouldContain("FR104", Case.Sensitive);
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_RelatedLinksAreWellFormed()
    {
        string root = FindRepositoryRoot();
        string docPath = Path.Combine(root, "docs", "responsibility-boundaries.md");
        string docDir = Path.GetDirectoryName(docPath)!;
        string doc = File.ReadAllText(docPath);

        MatchCollection links = MarkdownLinkTargetRegex().Matches(doc);
        links.Count.ShouldBeGreaterThan(0, "Document must contain at least one markdown link.");

        foreach (Match link in links)
        {
            string target = link.Groups["target"].Value.Trim();

            if (target.StartsWith('#'))
            {
                continue;
            }

            if (target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Uri.TryCreate(target, UriKind.Absolute, out Uri? uri).ShouldBeTrue($"'{target}' must be an absolute HTTPS URI.");
                uri!.Scheme.ShouldBe(Uri.UriSchemeHttps);
                continue;
            }

            string filePart = target.Contains('#') ? target[..target.IndexOf('#')] : target;
            string absolutePath = Path.GetFullPath(Path.Combine(docDir, filePart));
            File.Exists(absolutePath).ShouldBeTrue(
                $"Link target '{target}' must exist at '{absolutePath}'.");
        }
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_FreeTextPassesContentSafety()
    {
        string doc = ReadBoundaryDoc();

        foreach (string forbidden in ForbiddenBoundaryDocFragments)
        {
            doc.ShouldNotContain(forbidden, Case.Insensitive);
        }
    }

    [Fact]
    public void ResponsibilityBoundaryDocument_DoesNotClaimOwnershipOfAdjacentSystems()
    {
        string doc = ReadBoundaryDoc();

        foreach (string phrase in OwnershipViolationPhrases)
        {
            doc.ShouldNotContain(phrase, Case.Insensitive);
        }
    }

    [Fact]
    public void IntegrationGuide_LinksToResponsibilityBoundaries()
    {
        string root = FindRepositoryRoot();
        string guide = File.ReadAllText(Path.Combine(root, "docs", "integration-guide.md"));
        guide.ShouldContain("responsibility-boundaries.md");
    }

    private static string ReadBoundaryDoc()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "responsibility-boundaries.md");
        File.Exists(path).ShouldBeTrue($"Expected responsibility boundary document at '{path}'.");
        return File.ReadAllText(path);
    }

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

    [GeneratedRegex(@"\[(?:[^\]]*)\]\((?<target>[^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLinkTargetRegex();
}
