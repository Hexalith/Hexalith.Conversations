// <copyright file="DomainModuleAuthoringTemplateValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Documentation;

/// <summary>
/// Validates the reusable domain-module authoring template and its live Conversations evidence.
/// </summary>
public sealed class DomainModuleAuthoringTemplateValidationTest
{
    private static readonly string[] RequiredCapabilityFragments =
    [
        "shared host",
        "aggregate",
        "query/cursor",
        "read model",
        "projection",
        "tenant access",
        "typed client",
        "Aspire/Dapr",
        "ServiceDefaults",
        "serialization",
        "telemetry",
        "testing/evidence",
    ];

    private static readonly string[] RequiredSourceAnchors =
    [
        "src/Hexalith.Conversations.Server/Program.cs",
        "src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs",
        "src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs",
        "src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs",
        "src/Hexalith.Conversations.Server/Queries/ConversationListCursor.cs",
        "src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs",
        "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs",
        "src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs",
        "src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs",
        "src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs",
        "src/Hexalith.Conversations.ServiceDefaults/ConversationsServiceDefaults.cs",
        "src/Hexalith.Conversations.Contracts/Serialization/ConversationsJsonContext.cs",
        "src/Hexalith.Conversations.Server/Diagnostics/ConversationTelemetryDefinitions.cs",
        "docs/release-evidence/promote-adopt-runbook.md",
        "docs/release-evidence/consume-promote-keep-inventory-v1.md",
        "docs/release-evidence/release-baseline-v1.md",
    ];

    private static readonly string[] RequiredAdoptionPatterns =
    [
        "builder.AddEventStoreDomainService(domainAssembly, serverAssembly)",
        "app.UseEventStoreDomainService()",
        "EventStoreAggregate<TState>",
        "static `Handle(command, state)`",
        "AddEventStoreQueryCursorCodec(...)",
        "QueryCursorScope",
        "AddEventStoreReadModelStore()",
        "IReadModelStore",
        "ReadModelWritePolicy",
        "IDomainProjectionHandler",
        "services.AddTenantAccess<...>(static services => services.AddHexalithTenants())",
        "HttpClientRegistration.AddTypedHttpClient",
        "AddAspireDaprDomainModule(new AspireDaprDomainModuleOptions(...))",
        "AddHexalithServiceDefaults(...)",
        "JsonSerializationOptions.CreateWeb([...])",
        "BoundedTelemetryMeter",
        "BoundedTelemetryCounterDefinition",
    ];

    private static readonly string[] RequiredReleaseGateObligations =
    [
        "Fail-closed tenant access",
        "Idempotency boundaries",
        "Governance/audit pairing",
        "Redaction and non-disclosure",
        "Projection freshness",
        "Provider portability",
        "Content-safe telemetry",
        "Public contract-shape stability",
    ];

    private static readonly string[] OptionalScopeFragments =
    [
        "Admin.Web",
        "FrontComposer trust components",
        "Publication subscribers",
        "Governance workflows",
        "Excluded from SM-2 minimal skeleton",
        "Exclude these optional categories from the SM-2 baseline",
        "generated output",
        "local developer artifacts",
    ];

    [Fact]
    public void ThinAuthoringTemplateAndValidationEvidenceShouldExistAndPinLiveAnchors()
    {
        string root = FindRepositoryRoot();
        string template = ReadRepositoryFile(root, "docs", "domain-module-authoring-template.md");
        string validation = ReadRepositoryFile(root, "docs", "release-evidence", "thin-authoring-template-validation-v1.md");

        template.ShouldContain("# Hexalith Domain Module Authoring Template");
        validation.ShouldContain("# Thin Authoring Template Validation v1");
        validation.ShouldContain("Story 4.2 handoff");
        validation.ShouldContain("FR-16 public DTO metadata adoption");
        template.ShouldContain("Story 3.7 metadata disposition");

        foreach (string fragment in RequiredCapabilityFragments)
        {
            template.ShouldContain(fragment, Case.Insensitive);
            validation.ShouldContain(fragment, Case.Insensitive);
        }

        foreach (string anchor in RequiredSourceAnchors)
        {
            validation.ShouldContain(anchor, Case.Sensitive);
        }
    }

    [Fact]
    public void ThinAuthoringTemplateShouldPinEverySharedCapabilityToConcreteAdoptionPattern()
    {
        string template = ReadRepositoryFile(FindRepositoryRoot(), "docs", "domain-module-authoring-template.md");

        foreach (string pattern in RequiredAdoptionPatterns)
        {
            template.ShouldContain(pattern, Case.Sensitive);
        }
    }

    [Fact]
    public void ThinAuthoringTemplateValidationShouldCarryReleaseGateObligationsForward()
    {
        string root = FindRepositoryRoot();
        string template = ReadRepositoryFile(root, "docs", "domain-module-authoring-template.md");
        string validation = ReadRepositoryFile(root, "docs", "release-evidence", "thin-authoring-template-validation-v1.md");

        // The obligations must be carried forward by both artifacts so that an author reading
        // only the template still sees the full release-gate plan (AC-5).
        foreach (string obligation in RequiredReleaseGateObligations)
        {
            validation.ShouldContain(obligation, Case.Insensitive);
            template.ShouldContain(obligation, Case.Insensitive);
        }
    }

    [Fact]
    public void ThinAuthoringTemplateShouldDefineMinimalSkeletonAndOptionalExclusions()
    {
        string root = FindRepositoryRoot();
        string template = ReadRepositoryFile(root, "docs", "domain-module-authoring-template.md");
        string validation = ReadRepositoryFile(root, "docs", "release-evidence", "thin-authoring-template-validation-v1.md");
        string combined = template + Environment.NewLine + validation;

        foreach (string fragment in OptionalScopeFragments)
        {
            combined.ShouldContain(fragment, Case.Insensitive);
        }

        template.ShouldContain("Included in SM-2 baseline", Case.Sensitive);
        validation.ShouldContain("Story 4.2 can measure minimal-module authoring cost without redefining scope", Case.Sensitive);
    }

    [Fact]
    public void ThinAuthoringTemplateShouldPreserveFr16MetadataDisposition()
    {
        string root = FindRepositoryRoot();
        string template = ReadRepositoryFile(root, "docs", "domain-module-authoring-template.md");
        string validation = ReadRepositoryFile(root, "docs", "release-evidence", "thin-authoring-template-validation-v1.md");
        string combined = template + Environment.NewLine + validation;

        combined.ShouldContain("It is not a blanket requirement for public domain DTOs", Case.Sensitive);
        combined.ShouldContain("Do not require public DTOs to reference EventStore metadata interfaces", Case.Sensitive);
        combined.ShouldContain("keep metadata behind an adapter or defer it", Case.Insensitive);

        combined.ShouldNotContain("public DTOs must reference EventStore metadata interfaces", Case.Insensitive);
        combined.ShouldNotContain("public DTOs must implement EventStore metadata interfaces", Case.Insensitive);
    }

    [Fact]
    public void ThinAuthoringTemplateEvidenceShouldNotUseBuildOutputAsSourceOfTruth()
    {
        string root = FindRepositoryRoot();
        string template = ReadRepositoryFile(root, "docs", "domain-module-authoring-template.md");
        string validation = ReadRepositoryFile(root, "docs", "release-evidence", "thin-authoring-template-validation-v1.md");

        string combined = template + Environment.NewLine + validation;

        combined.ShouldNotContain("obj/", Case.Insensitive);
        combined.ShouldNotContain("bin/", Case.Insensitive);
        combined.ShouldNotContain("\\obj\\", Case.Insensitive);
        combined.ShouldNotContain("\\bin\\", Case.Insensitive);
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
