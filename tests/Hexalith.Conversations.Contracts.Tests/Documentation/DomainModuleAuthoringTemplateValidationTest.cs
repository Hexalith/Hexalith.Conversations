// <copyright file="DomainModuleAuthoringTemplateValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Xml.Linq;

using Shouldly;

using Xunit;

using Hexalith.Conversations.TestSupport;

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

    [Fact]
    public void GuidanceShouldDefineOneNonShippingTestAppHost()
    {
        string root = FindRepositoryRoot();
        string template = ReadRepositoryFile(root, "docs", "domain-module-authoring-template.md");
        using JsonDocument bundle = ReadGuidanceBundle(root);
        JsonElement host = bundle.RootElement.GetProperty("testAppHost");

        host.GetProperty("count").GetInt32().ShouldBe(1);
        host.GetProperty("isPackable").GetBoolean().ShouldBeFalse();
        host.GetProperty("isPublishable").GetBoolean().ShouldBeFalse();
        host.GetProperty("repositoryOnly").GetBoolean().ShouldBeTrue();
        host.GetProperty("countedInAuthoringCost").GetBoolean().ShouldBeTrue();

        template.ShouldContain("Exactly one test AppHost per module.", Case.Sensitive);
        template.ShouldContain("| `IsPackable` | `false` |", Case.Sensitive);
        template.ShouldContain("| `IsPublishable` | `false` |", Case.Sensitive);
        template.ShouldContain("Repository-only; never shipped to adopters.", Case.Sensitive);
        template.ShouldContain("counts in authoring cost (SM-2)", Case.Sensitive);

        // The repository's own test AppHost must obey the same project rules.
        string appHostProject = ReadRepositoryFile(
            root, "src", "Hexalith.Conversations.AppHost", "Hexalith.Conversations.AppHost.csproj");
        appHostProject.ShouldContain("<IsPackable>false</IsPackable>", Case.Sensitive);
        appHostProject.ShouldContain("<IsPublishable>false</IsPublishable>", Case.Sensitive);
    }

    [Fact]
    public void GuidanceShouldProhibitPlatformCapabilityOwnership()
    {
        string root = FindRepositoryRoot();
        string template = ReadRepositoryFile(root, "docs", "domain-module-authoring-template.md");
        using JsonDocument bundle = ReadGuidanceBundle(root);

        string[] prohibited = bundle.RootElement.GetProperty("prohibited")
            .EnumerateArray().Select(item => item.GetProperty("id").GetString()!).ToArray();
        string[] platformOwned = bundle.RootElement.GetProperty("platformOwned")
            .EnumerateArray().Select(item => item.GetProperty("id").GetString()!).ToArray();
        string[] moduleOwned = bundle.RootElement.GetProperty("moduleOwned")
            .EnumerateArray().Select(item => item.GetProperty("id").GetString()!).ToArray();

        prohibited.ShouldBe(
        [
            "reusable-aspire-library",
            "module-service-defaults",
            "module-dapr-implementation",
            "module-event-subscription",
            "module-projection-query-runtime",
            "module-publication",
            "module-health",
            "module-telemetry-runtime",
        ]);
        platformOwned.ShouldBe(
        [
            "service-defaults",
            "dapr-wiring",
            "event-subscription",
            "projection-query-runtime",
            "publication",
            "health",
            "telemetry",
        ]);
        moduleOwned.ShouldBe(["contracts", "domain-behavior", "module-registration", "module-tests"]);

        template.ShouldContain("A reusable module Aspire library is prohibited.", Case.Sensitive);
        template.ShouldContain("A module must never reimplement or reassign a platform-owned capability.", Case.Sensitive);
        template.ShouldNotContain("| `ServiceDefaults` |", Case.Sensitive);
    }

    [Fact]
    public void MinimalFixtureShouldContainOneNonShippingAppHostAndNoReusableAspireLibrary()
    {
        const string FixtureRoot = "tests/fixtures/Hexalith.Conversations.MinimalModule";
        string root = FindRepositoryRoot();
        string fixture = Path.Combine([root, .. FixtureRoot.Split('/')]);
        Directory.Exists(fixture).ShouldBeTrue("Expected the minimal-module fixture to exist.");

        Dictionary<string, string> defaults = ReadProperties(Path.Combine(fixture, "Directory.Build.props"));
        string[] projects = Directory.EnumerateFiles(fixture, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        projects.Length.ShouldBe(4, "The fixture has exactly Contracts, Server, Tests and one AppHost project.");

        XElement[] appHosts = projects
            .Select(path => XDocument.Load(path).Root!)
            .Where(project => (project.Attribute("Sdk")?.Value ?? string.Empty).StartsWith("Aspire.AppHost", StringComparison.Ordinal))
            .ToArray();
        appHosts.Length.ShouldBe(1, "APPHOST_BOUNDARY_INVALID: exactly one test AppHost is allowed.");

        // Effective properties: the project value wins, else the fixture Directory.Build.props value.
        string appHostPath = projects.Single(path => Path.GetFileName(path).EndsWith(".AppHost.csproj", StringComparison.Ordinal));
        Dictionary<string, string> appHost = ReadProperties(appHostPath);
        string Effective(string name) => appHost.TryGetValue(name, out string? own) ? own : defaults.GetValueOrDefault(name, string.Empty);
        Effective("IsPackable").ShouldBe("false", "APPHOST_BOUNDARY_INVALID: the test AppHost must not be packable.");
        Effective("IsPublishable").ShouldBe("false", "APPHOST_BOUNDARY_INVALID: the test AppHost must not be publishable.");
        Path.GetRelativePath(fixture, appHostPath).Replace('\\', '/').ShouldStartWith("tests/", Case.Sensitive, "APPHOST_BOUNDARY_INVALID: the test AppHost is repository-only.");
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(appHostPath)!, "AppHost.cs")).ShouldContain("AddAspireDaprDomainModule");

        foreach (string path in projects)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            XElement project = XDocument.Load(path).Root!;
            string[] references = project.Descendants("ProjectReference").Select(item => item.Attribute("Include")!.Value).ToArray();
            string[] packages = project.Descendants("PackageReference").Select(item => item.Attribute("Include")!.Value).ToArray();
            bool isAppHost = path == appHostPath;

            references.ShouldNotContain(
                reference => reference.Contains("ServiceDefaults", StringComparison.Ordinal),
                $"REUSABLE_ASPIRE_LIBRARY_PRESENT: {name} references a ServiceDefaults project.");
            references.ShouldNotContain(
                reference => reference.Contains("Hexalith.EventStore.Server.csproj", StringComparison.Ordinal),
                $"REUSABLE_ASPIRE_LIBRARY_PRESENT: {name} references the platform server runtime.");
            if (!isAppHost)
            {
                name.ShouldNotContain("Aspire", Case.Sensitive, "REUSABLE_ASPIRE_LIBRARY_PRESENT: a module Aspire library is prohibited.");
                name.ShouldNotContain("ServiceDefaults", Case.Sensitive, "REUSABLE_ASPIRE_LIBRARY_PRESENT: a module ServiceDefaults project is prohibited.");
                references.ShouldNotContain(
                    reference => reference.Contains("Hexalith.EventStore.Aspire", StringComparison.Ordinal),
                    $"REUSABLE_ASPIRE_LIBRARY_PRESENT: {name} references the platform Aspire helper outside the test AppHost.");
                packages.ShouldNotContain(
                    package => package.StartsWith("Aspire.", StringComparison.Ordinal)
                        || package.StartsWith("CommunityToolkit.Aspire", StringComparison.Ordinal)
                        || package.StartsWith("Dapr.", StringComparison.Ordinal),
                    $"REUSABLE_ASPIRE_LIBRARY_PRESENT: {name} owns platform Aspire or Dapr wiring.");
            }
        }
    }

    private static Dictionary<string, string> ReadProperties(string path)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (XElement group in XDocument.Load(path).Root!.Elements("PropertyGroup").Where(group => group.Attribute("Condition") is null))
        {
            foreach (XElement property in group.Elements().Where(property => property.Attribute("Condition") is null))
            {
                values[property.Name.LocalName] = property.Value.Trim();
            }
        }

        return values;
    }

    private static JsonDocument ReadGuidanceBundle(string root)
        => JsonDocument.Parse(ReadRepositoryFile(root, "docs", "release-evidence", "thin-authoring-guidance-v2.json"));

    private static string ReadRepositoryFile(string root, params string[] pathParts)
    {
        string path = Path.Combine([root, .. pathParts]);
        File.Exists(path).ShouldBeTrue($"Expected repository file '{path}' to exist.");
        return RepositoryLocator.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
        => RepositoryLocator.FindRepositoryRoot();
}
