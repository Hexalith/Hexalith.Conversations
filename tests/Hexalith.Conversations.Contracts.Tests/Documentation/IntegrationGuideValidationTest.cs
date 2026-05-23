// <copyright file="IntegrationGuideValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.RegularExpressions;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts;
using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Diagnostics;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Documentation;

/// <summary>
/// Validates adopter documentation against the shipped contract and client surface.
/// </summary>
public sealed partial class IntegrationGuideValidationTest
{
    private static readonly string[] RequiredGuideFragments =
    [
        "Hexalith.Conversations.Client",
        "Hexalith.Conversations.Contracts",
        "IConversationClient",
        "ConversationClientServiceCollectionExtensions",
        "ConversationClientOptions",
        "ConversationClientContext",
        "CreateConversationCommand",
        "ConversationCreatedResult",
        "AppendMessageCommand",
        "GetConversationQuery",
        "ConversationDetailResult",
        "ConversationError",
        "ConversationErrorCode",
        "ConversationErrorCategory",
        "ConversationErrorClientAction",
        "idempotency_conflict",
        "idempotency_outcome_unknown",
        "ProjectionTrustState",
        "ProjectionFreshnessV1",
        "CallerMetadata",
        "ConversationContractCompatibility.Current",
        "ConversationContractCompatibility.Evaluate",
        "ConversationCorePreconditionCatalog",
        "ConformanceRunResultV1",
        "dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj",
        "Current",
        "Stale",
        "Rebuilding",
        "Unavailable",
        "Forbidden",
        "Redacted",
        "tenant binding",
        "Party identity",
        "auditHandle",
        "schema_version_unsupported",
        "aggregate_not_found",
    ];

    private static readonly string[] ForbiddenGuideFragments =
    [
        "EventStore",
        "stream internals",
        "snapshot",
        "dispatcher",
        "repository",
        "provider payload",
        "provider-session",
        "tenant:",
        "tenant-001",
        "party:",
        "party-actor",
        "conv:",
        "conversation-001",
        "business reference case-",
        "raw exception",
        "C:\\",
        "D:\\",
    ];

    [Fact]
    public void IntegrationGuideShouldBeLinkedAndCoverSupportedWorkflow()
    {
        string root = FindRepositoryRoot();
        string guide = ReadRepositoryFile(root, "docs", "integration-guide.md");
        string rootReadme = ReadRepositoryFile(root, "README.md");

        rootReadme.ShouldContain("docs/integration-guide.md");
        guide.ShouldContain("# Hexalith Conversations Developer Integration Guide");

        foreach (string fragment in RequiredGuideFragments)
        {
            guide.ShouldContain(fragment, Case.Sensitive);
        }
    }

    [Fact]
    public void DocumentedErrorTablesShouldMatchTheCanonicalCatalog()
    {
        string root = FindRepositoryRoot();
        string[] readmePaths =
        [
            Path.Combine(root, "README.md"),
            Path.Combine(root, "src", "Hexalith.Conversations.Contracts", "README.md"),
        ];

        foreach (string readmePath in readmePaths)
        {
            IReadOnlyDictionary<string, string[]> rows = ParseErrorTable(File.ReadAllText(readmePath));

            rows.Keys.ShouldBe(
                ConversationErrorCatalog.All.Select(descriptor => descriptor.Code.Value),
                ignoreOrder: true,
                customMessage: $"{readmePath} must document every canonical error code.");

            foreach (ConversationErrorDescriptor descriptor in ConversationErrorCatalog.All)
            {
                string[] row = rows[descriptor.Code.Value];
                row[1].ShouldBe(descriptor.Category.Value, $"{descriptor.Code.Value} category");
                row[2].ShouldBe(descriptor.IsRetryable.ToString().ToLowerInvariant(), $"{descriptor.Code.Value} retryable");
                row[3].ShouldBe(descriptor.ClientAction.Value, $"{descriptor.Code.Value} client action");
                row[5].ShouldBe(descriptor.Documentation.ToString(), $"{descriptor.Code.Value} documentation");
            }
        }
    }

    [Fact]
    public void IntegrationGuideShouldDocumentCurrentCompatibilityMetadataAndConformance()
    {
        string guide = ReadRepositoryFile(FindRepositoryRoot(), "docs", "integration-guide.md");
        ContractCompatibilityMetadata metadata = ConversationContractCompatibility.Current;

        guide.ShouldContain(metadata.ContractsPackage.PackageId);
        guide.ShouldContain(metadata.ContractsPackage.Version);
        guide.ShouldContain(metadata.ClientPackage.PackageId);
        guide.ShouldContain(metadata.ClientPackage.Version);
        guide.ShouldContain(metadata.Status.Value);
        guide.ShouldContain(metadata.CommandContracts.ContractName);
        guide.ShouldContain(metadata.ProjectionContracts.ContractName);
        guide.ShouldContain(metadata.EventContracts.ContractName);

        typeof(ConformanceRunResultV1).Name.ShouldBe("ConformanceRunResultV1");
        guide.ShouldContain(typeof(ConformanceRunResultV1).Name);
    }

    [Fact]
    public void GuideExamplesShouldReferenceExistingClientAndContractMembers()
    {
        string guide = ReadRepositoryFile(FindRepositoryRoot(), "docs", "integration-guide.md");
        string snippets = string.Join(Environment.NewLine, CSharpCodeFenceRegex().Matches(guide).Select(m => m.Groups["code"].Value));
        snippets.ShouldNotBeNullOrWhiteSpace("The integration guide must include validated C# snippets.");

        Type[] exportedTypes =
        [
            .. typeof(ContractsAssemblyMarker).Assembly.GetExportedTypes(),
            .. typeof(ClientAssemblyMarker).Assembly.GetExportedTypes(),
        ];

        foreach (string typeName in RequiredGuideFragments.Where(fragment => fragment.EndsWith("Result", StringComparison.Ordinal)
            || fragment.EndsWith("Command", StringComparison.Ordinal)
            || fragment.EndsWith("Options", StringComparison.Ordinal)
            || fragment.EndsWith("Context", StringComparison.Ordinal)
            || fragment is "IConversationClient" or "ConversationError" or "ProjectionTrustState" or "ProjectionFreshnessV1"
                or "ConversationCorePreconditionCatalog" or "ConformanceRunResultV1"))
        {
            exportedTypes.Select(type => type.Name).ShouldContain(typeName);
        }

        snippets.ShouldContain(nameof(IConversationClient.CreateConversationAsync));
        snippets.ShouldContain(nameof(IConversationClient.AppendMessageAsync));
        snippets.ShouldContain(nameof(IConversationClient.GetConversationAsync));
        snippets.ShouldContain(nameof(ConversationClientContext.ToCommandMetadata));
        snippets.ShouldContain(nameof(ConversationClientContext.ToGetConversationQuery));
        snippets.ShouldContain(nameof(ConversationContractCompatibility.Evaluate));
        snippets.ShouldContain(nameof(ProjectionFreshnessV1.AllowsTrustBearingDecision));
        snippets.ShouldContain(nameof(ConversationCorePreconditionCatalog.All));
        snippets.ShouldContain(nameof(ConversationErrorCode.IdempotencyConflict));
        snippets.ShouldContain(nameof(ConversationErrorCode.IdempotencyOutcomeUnknown));
        snippets.ShouldContain(nameof(ProjectionTrustState.Current));
    }

    [Fact]
    public void IntegrationGuideShouldKeepExamplesAndDocumentationPointersSafe()
    {
        string guide = ReadRepositoryFile(FindRepositoryRoot(), "docs", "integration-guide.md");

        foreach (string forbidden in ForbiddenGuideFragments)
        {
            guide.ShouldNotContain(forbidden, Case.Insensitive);
        }

        MatchCollection links = HttpLinkRegex().Matches(guide);
        links.Count.ShouldBeGreaterThan(0, "The guide must include HTTPS documentation pointers.");
        foreach (Match link in links)
        {
            Uri.TryCreate(link.Value, UriKind.Absolute, out Uri? uri).ShouldBeTrue($"{link.Value} must be an absolute URI.");
            uri!.Scheme.ShouldBe(Uri.UriSchemeHttps);
        }

        foreach (ConversationErrorDescriptor descriptor in ConversationErrorCatalog.All)
        {
            descriptor.Documentation.Scheme.ShouldBe(Uri.UriSchemeHttps);
        }
    }

    private static string ReadRepositoryFile(string root, params string[] pathParts)
    {
        string path = Path.Combine([root, .. pathParts]);
        File.Exists(path).ShouldBeTrue($"Expected repository file '{path}' to exist.");
        return File.ReadAllText(path);
    }

    private static IReadOnlyDictionary<string, string[]> ParseErrorTable(string markdown)
    {
        Dictionary<string, string[]> rows = new(StringComparer.Ordinal);

        foreach (string line in markdown.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith("| `", StringComparison.Ordinal))
            {
                continue;
            }

            string[] columns = line.Trim('|').Split('|').Select(column => column.Trim().Trim('`')).ToArray();
            if (columns.Length == 6 && ConversationErrorCode.Parse(columns[0]) is { } code)
            {
                rows[code.Value] = columns;
            }
        }

        return rows;
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

    [GeneratedRegex(@"```csharp\s*(?<code>.*?)```", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CSharpCodeFenceRegex();

    [GeneratedRegex(@"https?://[^\s)`>""]+", RegexOptions.CultureInvariant)]
    private static partial Regex HttpLinkRegex();
}
