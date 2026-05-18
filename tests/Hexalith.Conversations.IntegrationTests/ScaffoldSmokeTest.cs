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
        "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj",
        "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj",
        "src/Hexalith.Conversations.ServiceDefaults/Hexalith.Conversations.ServiceDefaults.csproj",
        "tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj",
        "tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj",
        "tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj",
        "tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj",
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

        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations/Hexalith.Conversations.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            "Hexalith.EventStore/src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj",
            "Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            "src/Hexalith.Conversations/Hexalith.Conversations.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj",
            "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
            "src/Hexalith.Conversations/Hexalith.Conversations.csproj");
        AssertProjectReferences(
            root,
            "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj",
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

        string[] actualReferences = [.. project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => NormalizePath(Path.GetRelativePath(root, Path.GetFullPath(path, projectDirectory))))
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
}
