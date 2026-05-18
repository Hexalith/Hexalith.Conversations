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

        foreach (string projectFile in projectFiles)
        {
            XDocument project = XDocument.Load(projectFile);
            IEnumerable<XElement> referencesWithVersions = project
                .Descendants("PackageReference")
                .Where(reference => reference.Attribute("Version") is not null || reference.Element("Version") is not null);

            referencesWithVersions.ShouldBeEmpty(Path.GetRelativePath(root, projectFile));
        }
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
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props"))
                && Directory.Exists(Path.Combine(directory.FullName, "_bmad-output")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Hexalith.Conversations repository root.");
    }
}
