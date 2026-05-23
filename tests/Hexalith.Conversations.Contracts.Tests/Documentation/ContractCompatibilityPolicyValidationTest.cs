// <copyright file="ContractCompatibilityPolicyValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Documentation;

/// <summary>
/// Validates the FR81 contract compatibility and deprecation policy against the shipped metadata.
/// </summary>
public sealed class ContractCompatibilityPolicyValidationTest
{
    private static readonly string[] RequiredSurfaces =
    [
        "commands",
        "projections",
        "published events",
        "typed errors",
        "version discovery",
        "contracts package",
        ".NET client package",
    ];

    private static readonly string[] RequiredPolicyCategories =
    [
        "additive",
        "breaking",
        "deprecated",
        "unsupported",
        "waiver-dependent",
    ];

    private static readonly string[] ForbiddenPolicyFragments =
    [
        "EventStore",
        "storage envelope",
        "stream topology",
        "snapshot",
        "storage offset",
        "internal projection topology",
        "provider payload",
        "raw exception",
        "tenant:",
        "party:",
        "conv:",
        "redacted content",
        "C:\\",
        "D:\\",
    ];

    [Fact]
    public void PolicyShouldExistAndBeLinkedFromAdopterFacingDocs()
    {
        string root = FindRepositoryRoot();
        string policy = ReadRepositoryFile(root, "docs", "release-evidence", "contract-compatibility-policy.md");
        string relativePolicyPath = "docs/release-evidence/contract-compatibility-policy.md";

        ReadRepositoryFile(root, "README.md").ShouldContain(relativePolicyPath);
        ReadRepositoryFile(root, "docs", "integration-guide.md").ShouldContain("release-evidence/contract-compatibility-policy.md");
        ReadRepositoryFile(root, "src", "Hexalith.Conversations.Contracts", "README.md")
            .ShouldContain("../../docs/release-evidence/contract-compatibility-policy.md");

        policy.ShouldContain("FR81");
        foreach (string surface in RequiredSurfaces)
        {
            policy.ShouldContain(surface, Case.Insensitive);
        }
    }

    [Fact]
    public void PolicySummaryShouldExposeStableReleaseEvidenceClassifications()
    {
        string policy = ReadRepositoryFile(FindRepositoryRoot(), "docs", "release-evidence", "contract-compatibility-policy.md");

        foreach (string category in RequiredPolicyCategories)
        {
            policy.ShouldContain($"`{category}`", Case.Sensitive);
        }

        policy.ShouldContain("POLICY-FR81-COMPAT-ADD");
        policy.ShouldContain("POLICY-FR81-COMPAT-BREAK");
        policy.ShouldContain("POLICY-FR81-COMPAT-DEPRECATE");
        policy.ShouldContain("POLICY-FR81-COMPAT-UNSUPPORTED");
        policy.ShouldContain("POLICY-FR81-COMPAT-WAIVER");
        policy.ShouldContain("ConversationContractCompatibility.Current");
        policy.ShouldContain(nameof(ContractVersionInfo));
        policy.ShouldContain(nameof(ContractCompatibilityResult));
        policy.ShouldContain(nameof(ConversationErrorCatalog));
    }

    [Fact]
    public void DocumentedMetadataShouldMatchCurrentCompatibilityMetadata()
    {
        string policy = ReadRepositoryFile(FindRepositoryRoot(), "docs", "release-evidence", "contract-compatibility-policy.md");
        ContractCompatibilityMetadata metadata = ConversationContractCompatibility.Current;

        policy.ShouldContain($"Active schema version: `{metadata.SchemaVersion.Value}`");
        policy.ShouldContain($"Minimum supported command schema version: `{metadata.CommandContracts.MinimumSupportedSchemaVersion.Value}`");
        policy.ShouldContain($"Minimum supported projection schema version: `{metadata.ProjectionContracts.MinimumSupportedSchemaVersion.Value}`");
        policy.ShouldContain($"Minimum supported published-event schema version: `{metadata.EventContracts.MinimumSupportedSchemaVersion.Value}`");
        policy.ShouldContain($"Compatibility status: `{metadata.Status.Value}`");
        policy.ShouldContain($"Contracts package: `{metadata.ContractsPackage.PackageId}` `{metadata.ContractsPackage.Version}`");
        policy.ShouldContain($".NET client package: `{metadata.ClientPackage.PackageId}` `{metadata.ClientPackage.Version}`");
    }

    [Fact]
    public void CompatibilityEvaluationScenariosShouldAlignWithPolicyCategoriesAndSafeDiagnostics()
    {
        ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
                CommandSchemaVersion: "1",
                ProjectionSchemaVersion: "1",
                EventSchemaVersion: "1",
                ContractsPackageVersion: "1.0.0",
                ClientPackageVersion: "1.0.0"))
            .Status.ShouldBe(ContractCompatibilityStatus.Supported);

        ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(ContractsPackageVersion: "0.9.0"))
            .Status.ShouldBe(ContractCompatibilityStatus.Deprecated);

        ContractCompatibilityResult unsupportedSchema = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(CommandSchemaVersion: "2"));
        unsupportedSchema.Status.ShouldBe(ContractCompatibilityStatus.Unsupported);
        unsupportedSchema.Error!.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        unsupportedSchema.Error.Category.ShouldBe(ConversationErrorCategory.Versioning);
        unsupportedSchema.Error.ClientAction.ShouldBe(ConversationErrorClientAction.UseSupportedVersion);
        unsupportedSchema.Error.SafeFieldDiagnostics!.Values.ShouldNotContain("2");
        unsupportedSchema.Remediations.Select(r => r.GuidanceCode).ShouldContain("use-supported-v1-package");

        ContractCompatibilityResult unsupportedPackage = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(ClientPackageVersion: "2.0.0"));
        unsupportedPackage.Status.ShouldBe(ContractCompatibilityStatus.Unsupported);
        unsupportedPackage.Error!.SafeFieldDiagnostics!.Values.ShouldNotContain("2.0.0");
        unsupportedPackage.Remediations.Select(r => r.GuidanceCode).ShouldContain("use-supported-v1-package");

        ContractCompatibilityResult invalidSchema = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(EventSchemaVersion: "latest"));
        invalidSchema.Status.ShouldBe(ContractCompatibilityStatus.Invalid);
        invalidSchema.Error!.SafeFieldDiagnostics!.Values.ShouldNotContain("latest");

        ContractCompatibilityResult invalidPackage = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(ContractsPackageVersion: "latest"));
        invalidPackage.Status.ShouldBe(ContractCompatibilityStatus.Invalid);
        invalidPackage.Error!.SafeFieldDiagnostics!.Values.ShouldNotContain("latest");
    }

    [Fact]
    public void PolicyTextShouldRemainContentSafe()
    {
        string policy = ReadRepositoryFile(FindRepositoryRoot(), "docs", "release-evidence", "contract-compatibility-policy.md");

        foreach (string forbidden in ForbiddenPolicyFragments)
        {
            policy.ShouldNotContain(forbidden, Case.Insensitive);
        }

        string compatibilityJson = JsonSerializer.Serialize(
            ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(CommandSchemaVersion: "2")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        compatibilityJson.ShouldContain("schema_version_unsupported");
        compatibilityJson.ShouldContain("use-supported-version");
    }

    private static string ReadRepositoryFile(string root, params string[] pathParts)
    {
        string path = Path.Combine([root, .. pathParts]);
        File.Exists(path).ShouldBeTrue($"Expected repository file '{path}' to exist.");
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
}
