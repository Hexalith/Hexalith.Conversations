// <copyright file="ScaffoldSmokeTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Xml.Linq;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.IntegrationTests;

/// <summary>
/// Verifies the root scaffold shape and package-management guardrails.
/// </summary>
public sealed class ScaffoldSmokeTest
{
    private static readonly string[] ExpectedProjectPaths =
    [
        "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
        "src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj",
        "src/Hexalith.Conversations/Hexalith.Conversations.csproj",
        "src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj",
        "src/Hexalith.Conversations.Admin.Web/Hexalith.Conversations.Admin.Web.csproj",
        "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj",
        "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj",
        "src/Hexalith.Conversations.ServiceDefaults/Hexalith.Conversations.ServiceDefaults.csproj",
        "tests/Hexalith.Conversations.Admin.Web.Tests/Hexalith.Conversations.Admin.Web.Tests.csproj",
        "tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj",
        "tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj",
        "tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj",
        "tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj",
        "tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj",
        "tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj",
    ];

    /// <summary>
    /// Ensures the repository pins the requested .NET SDK version.
    /// </summary>
    [Fact]
    public void GlobalJsonShouldPinRequestedSdkVersion()
    {
        string root = FindRepositoryRoot();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "global.json")));

        JsonElement sdk = document.RootElement.GetProperty("sdk");
        sdk.GetProperty("version").GetString().ShouldBe("10.0.300");
        sdk.GetProperty("rollForward").GetString().ShouldBe("latestPatch");
    }

    /// <summary>
    /// Ensures the expected projects and solution entry point exist.
    /// </summary>
    [Fact]
    public void ScaffoldProjectsAndSolutionShouldExist()
    {
        string root = FindRepositoryRoot();

        File.Exists(Path.Combine(root, "Hexalith.Conversations.slnx")).ShouldBeTrue();

        foreach (string projectPath in ExpectedProjectPaths)
        {
            File.Exists(Path.Combine(root, projectPath)).ShouldBeTrue(projectPath);
        }
    }

    /// <summary>
    /// Ensures the solution project list stays aligned with the source and test projects on disk.
    /// </summary>
    [Fact]
    public void SolutionShouldIncludeEverySourceAndTestProjectOnDisk()
    {
        string root = FindRepositoryRoot();
        string solutionPath = Path.Combine(root, "Hexalith.Conversations.slnx");
        XDocument solution = XDocument.Load(solutionPath);

        string[] solutionProjectPaths = [.. solution
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizePath)
            .Where(path => path.StartsWith("src/", StringComparison.Ordinal)
                || path.StartsWith("tests/", StringComparison.Ordinal))
            .Order(StringComparer.OrdinalIgnoreCase)];

        string[] diskProjectPaths = [.. Directory
            .GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
            .Select(path => NormalizePath(Path.GetRelativePath(root, path)))
            .Order(StringComparer.OrdinalIgnoreCase)];

        solutionProjectPaths.ShouldBe(diskProjectPaths);
    }

    /// <summary>
    /// Ensures package versions are kept in central package management files.
    /// </summary>
    [Fact]
    public void ProjectPackageReferencesShouldNotDeclareInlineVersions()
    {
        string root = FindRepositoryRoot();
        string[] projectFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
            .ToArray();

        projectFiles.ShouldNotBeEmpty("expected at least one csproj under src/ or tests/ to inspect");

        int totalPackageReferences = 0;
        foreach (string projectFile in projectFiles)
        {
            XDocument project = XDocument.Load(projectFile);
            XElement[] packageReferences = [.. project.Descendants("PackageReference")];
            totalPackageReferences += packageReferences.Length;

            IEnumerable<XElement> referencesWithVersions = packageReferences
                .Where(reference => reference.Attribute("Version") is not null || reference.Element("Version") is not null);

            referencesWithVersions.ShouldBeEmpty(Path.GetRelativePath(root, projectFile));
        }

        totalPackageReferences.ShouldBeGreaterThan(
            0,
            "no PackageReference entries were inspected across any csproj — the inline-version check would pass vacuously");
    }

    /// <summary>
    /// Ensures scaffold project references preserve the approved dependency direction.
    /// </summary>
    [Fact]
    public void ProjectReferencesShouldFollowScaffoldBoundaryDirection()
    {
        string root = FindRepositoryRoot();
        string commonsRoot = ResolveExpectedModuleReferenceRoot(root, "Hexalith.Commons", "src/libraries/Hexalith.Commons.Http");
        string eventStoreRoot = ResolveExpectedModuleReferenceRoot(root, "Hexalith.EventStore", "src/Hexalith.EventStore.Contracts");
        string tenantsRoot = ResolveExpectedModuleReferenceRoot(root, "Hexalith.Tenants", "src/Hexalith.Tenants.Contracts");

        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            // Story 3.6 (FR-14): Contracts consumes the promoted shared serialization bases while preserving
            // the public contract boundary and wire shape.
            $"{commonsRoot}/src/libraries/Hexalith.Commons.Serialization/Hexalith.Commons.Serialization.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj",
            $"{commonsRoot}/src/libraries/Hexalith.Commons.Http/Hexalith.Commons.Http.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations/Hexalith.Conversations.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            $"{eventStoreRoot}/src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj",
            $"{eventStoreRoot}/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            "src/Hexalith.Conversations/Hexalith.Conversations.csproj",
            $"{eventStoreRoot}/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj",
            // Story 2.1 (FR-3): the Server is now the shared two-line domain-service host, so it references the
            // domain-service host SDK. Deliberate premise change recorded in the FR-20 ledger
            // (docs/release-evidence/at-risk-test-register-v1.*, story21StructuralDispositions).
            $"{eventStoreRoot}/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj",
            // Stories 3.2, 3.3, 3.5, and 3.6 promoted shared Commons helpers consumed by Server.
            $"{commonsRoot}/src/libraries/Hexalith.Commons.Diagnostics/Hexalith.Commons.Diagnostics.csproj",
            $"{commonsRoot}/src/libraries/Hexalith.Commons.Publication/Hexalith.Commons.Publication.csproj",
            $"{commonsRoot}/src/libraries/Hexalith.Commons.Serialization/Hexalith.Commons.Serialization.csproj",
            $"{commonsRoot}/src/libraries/Hexalith.Commons.TenantAccess/Hexalith.Commons.TenantAccess.csproj",
            $"{tenantsRoot}/src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj",
            $"{tenantsRoot}/src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            "src/Hexalith.Conversations/Hexalith.Conversations.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Admin.Web/Hexalith.Conversations.Admin.Web.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj",
            // Story 3.5 (FR-13): AppHost consumes shared Aspire/Dapr hosting helpers.
            $"{commonsRoot}/src/libraries/Hexalith.Commons.Aspire/Hexalith.Commons.Aspire.csproj",
            $"{eventStoreRoot}/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj",
            "src/Hexalith.Conversations.Admin.Web/Hexalith.Conversations.Admin.Web.csproj",
            "src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj");
    }

    /// <summary>
    /// Ensures scaffold projects do not take dependencies that would smuggle implementation behavior into Story 1.1.
    /// </summary>
    [Fact]
    public void ScaffoldProjectsShouldNotReferenceForbiddenInfrastructurePackages()
    {
        string root = FindRepositoryRoot();

        AssertNoForbiddenReferences(
            root,
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            "Dapr",
            "Hexalith.EventStore",
            "Hexalith.FrontComposer",
            "Hexalith.Parties",
            "Hexalith.Tenants",
            "Microsoft.AspNetCore");
        AssertNoForbiddenReferences(
            root,
            "src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj",
            "Dapr",
            "Hexalith.EventStore",
            "Hexalith.FrontComposer",
            "Hexalith.Parties",
            "Hexalith.Tenants");
        AssertNoForbiddenReferences(
            root,
            "src/Hexalith.Conversations/Hexalith.Conversations.csproj",
            "Dapr",
            "Hexalith.EventStore",
            "Hexalith.FrontComposer",
            "Hexalith.Parties",
            "Hexalith.Tenants",
            "Microsoft.AspNetCore");
        AssertNoForbiddenReferences(
            root,
            "src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj",
            "Dapr",
            "Hexalith.EventStore.Server");
    }

    /// <summary>
    /// Ensures local development documentation preserves bootstrap safety constraints.
    /// </summary>
    [Fact]
    public void ReadmeShouldDocumentSmokeValidationSafety()
    {
        string root = FindRepositoryRoot();
        string readme = File.ReadAllText(Path.Combine(root, "README.md"));

        readme.ShouldContain("Aspire runtime launch", Case.Insensitive);
        readme.ShouldContain("Dapr sidecars", Case.Insensitive);
        readme.ShouldContain("nested submodule initialization", Case.Insensitive);
        readme.ShouldContain("provider credentials", Case.Insensitive);
        readme.ShouldContain("tenant seed data", Case.Insensitive);
        readme.ShouldContain("production secrets", Case.Insensitive);
        readme.ShouldContain("external cloud resources", Case.Insensitive);
    }

    private static void AssertProjectReferences(string root, string projectPath, params string[] expectedReferencePaths)
    {
        XDocument project = XDocument.Load(Path.Combine(root, projectPath));
        string projectDirectory = Path.GetDirectoryName(Path.Combine(root, projectPath))
            ?? throw new InvalidOperationException($"Could not locate project directory for {projectPath}.");
        IReadOnlyDictionary<string, string> projectReferenceRoots = ResolveKnownProjectReferenceRoots(root);

        string[] actualReferences = [.. project
            .Descendants("ProjectReference")
            .Where(reference => IsProjectReferenceConditionActive(
                reference.Attribute("Condition")?.Value,
                projectReferenceRoots))
            .Select(reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => ResolveProjectReferencePath(root, projectDirectory, path, projectReferenceRoots))
            .Order(StringComparer.OrdinalIgnoreCase)];
        string[] expectedReferences = [.. expectedReferencePaths
            .Select(NormalizePath)
            .Order(StringComparer.OrdinalIgnoreCase)];

        actualReferences.ShouldBe(expectedReferences, projectPath);
    }

    private static void AssertNoForbiddenReferences(string root, string projectPath, params string[] forbiddenPrefixes)
    {
        XDocument project = XDocument.Load(Path.Combine(root, projectPath));
        string[] references = [.. project
            .Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "FrameworkReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .Where(reference => !string.IsNullOrWhiteSpace(reference))];

        foreach (string forbiddenPrefix in forbiddenPrefixes)
        {
            references.ShouldNotContain(
                reference => reference.StartsWith(forbiddenPrefix, StringComparison.Ordinal),
                $"{projectPath} should not reference {forbiddenPrefix} during scaffold-only coverage.");
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props"))
                && File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Hexalith.Conversations repository root.");
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static IReadOnlyDictionary<string, string> ResolveKnownProjectReferenceRoots(string root)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["HexalithCommonsRoot"] = ResolveModuleRoot(root, "Hexalith.Commons", "src/libraries/Hexalith.Commons.Http"),
            ["HexalithEventStoreRoot"] = ResolveModuleRoot(root, "Hexalith.EventStore", "src/Hexalith.EventStore.Contracts"),
            ["HexalithTenantsRoot"] = ResolveModuleRoot(root, "Hexalith.Tenants", "src/Hexalith.Tenants.Contracts"),
        };

    private static string ResolveExpectedModuleReferenceRoot(string root, string moduleName, string markerPath)
    {
        string moduleRoot = ResolveModuleRoot(root, moduleName, markerPath);

        return string.IsNullOrWhiteSpace(moduleRoot)
            ? moduleName
            : NormalizePath(Path.GetRelativePath(root, moduleRoot));
    }

    private static string ResolveModuleRoot(string root, string moduleName, string markerPath)
    {
        string[] candidates =
        [
            Path.GetFullPath(Path.Combine(root, "..", moduleName)),
            Path.GetFullPath(Path.Combine(root, moduleName)),
        ];

        return candidates.FirstOrDefault(candidate => Directory.Exists(Path.Combine(candidate, markerPath)))
            ?? string.Empty;
    }

    private static bool IsProjectReferenceConditionActive(string? condition, IReadOnlyDictionary<string, string> projectReferenceRoots)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return true;
        }

        foreach (KeyValuePair<string, string> projectReferenceRoot in projectReferenceRoots)
        {
            if (string.Equals(condition, $"'$({projectReferenceRoot.Key})' != ''", StringComparison.Ordinal))
            {
                return !string.IsNullOrWhiteSpace(projectReferenceRoot.Value);
            }

            if (string.Equals(condition, $"'$({projectReferenceRoot.Key})' == ''", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(projectReferenceRoot.Value);
            }
        }

        return true;
    }

    private static string ResolveProjectReferencePath(
        string root,
        string projectDirectory,
        string includePath,
        IReadOnlyDictionary<string, string> projectReferenceRoots)
    {
        // MSBuild Include paths use Windows backslash separators; normalize to the forward slash so that
        // Path.GetFullPath collapses the "../" segments on every platform (on Linux a literal backslash is not a
        // path separator, so an un-normalized Include leaves "Client/../Contracts" uncollapsed).
        string resolvedPath = includePath.Replace('\\', '/');

        foreach (KeyValuePair<string, string> projectReferenceRoot in projectReferenceRoots)
        {
            resolvedPath = resolvedPath.Replace(
                $"$({projectReferenceRoot.Key})",
                projectReferenceRoot.Value,
                StringComparison.Ordinal);
        }

        return NormalizePath(Path.GetRelativePath(root, Path.GetFullPath(resolvedPath, projectDirectory)));
    }
}
