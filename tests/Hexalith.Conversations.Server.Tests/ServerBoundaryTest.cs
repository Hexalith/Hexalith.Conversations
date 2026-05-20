// <copyright file="ServerBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Xml.Linq;

using Hexalith.Conversations.Server;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests;

/// <summary>
/// Verifies the starter server boundary references only scaffold-safe Conversations projects.
/// </summary>
public sealed class ServerBoundaryTest
{
    /// <summary>
    /// Ensures server scaffold dependencies are explicit and do not yet include EventStore runtime packages.
    /// </summary>
    [Fact]
    public void ServerAssemblyShouldReferenceContractsAndDomainWithoutEventStoreRuntime()
    {
        string[] references = typeof(ServerAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        references.ShouldContain("Hexalith.Conversations.Contracts");
        references.ShouldContain("Hexalith.Conversations");
        references.ShouldContain("Hexalith.Tenants.Client");
        references.ShouldContain("Hexalith.Tenants.Contracts");
        references.ShouldNotContain("Hexalith.EventStore.Server");
        references.ShouldNotContain("Hexalith.Tenants.Server");
        references.ShouldNotContain("Hexalith.Parties");
        references.ShouldNotContain("Hexalith.FrontComposer");
        references.ShouldNotContain("Dapr.Client");
    }

    /// <summary>
    /// Ensures server project references stay explicit in XML and do not hide forbidden runtime dependencies.
    /// </summary>
    [Fact]
    public void ServerProjectFileShouldNotDeclareForbiddenRuntimeReferences()
    {
        string projectPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Hexalith.Conversations.Server",
            "Hexalith.Conversations.Server.csproj");

        XDocument project = XDocument.Load(projectPath);
        string[] references = project
            .Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference" or "FrameworkReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        references.ShouldContain(reference => reference.EndsWith(
            "Hexalith.Conversations.Contracts.csproj",
            StringComparison.Ordinal));
        references.ShouldContain(reference => reference.EndsWith(
            "Hexalith.Conversations.csproj",
            StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Hexalith.EventStore.Server", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Hexalith.Tenants.Server", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Hexalith.Parties", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Hexalith.FrontComposer", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains("Dapr.Client", StringComparison.Ordinal));
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
