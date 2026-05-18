// <copyright file="ContractsAssemblyBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts;

using Shouldly;

using Xunit;
using System.Xml.Linq;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies that public contracts remain infrastructure-free.
/// </summary>
public sealed class ContractsAssemblyBoundaryTest
{
    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Dapr",
        "Hexalith.EventStore",
        "Hexalith.Folders",
        "Hexalith.FrontComposer",
        "Hexalith.Parties",
        "Hexalith.Projects",
        "Hexalith.Tenants",
        "Microsoft.AspNetCore",
        "System.Net.Http",
    ];

    /// <summary>
    /// Ensures the contracts assembly does not reference infrastructure packages.
    /// </summary>
    [Fact]
    public void ContractsAssemblyShouldNotReferenceInfrastructureAssemblies()
    {
        string[] references = typeof(ContractsAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        foreach (string forbiddenPrefix in ForbiddenReferencePrefixes)
        {
            references.ShouldNotContain(name => name.StartsWith(forbiddenPrefix, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Ensures the contracts project file does not declare forbidden package or project references.
    /// </summary>
    [Fact]
    public void ContractsProjectFileShouldNotDeclareForbiddenReferences()
    {
        string projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Hexalith.Conversations.Contracts",
            "Hexalith.Conversations.Contracts.csproj"));

        XDocument project = XDocument.Load(projectPath);
        string[] references = project
            .Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference" or "FrameworkReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        foreach (string forbiddenPrefix in ForbiddenReferencePrefixes)
        {
            references.ShouldNotContain(reference => reference.Contains(forbiddenPrefix, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Ensures production contract source files do not import forbidden infrastructure namespaces.
    /// </summary>
    [Fact]
    public void ContractsSourceFilesShouldNotImportForbiddenNamespaces()
    {
        string sourceRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Hexalith.Conversations.Contracts"));

        string[] sourceFiles = Directory
            .GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            foreach (string forbiddenPrefix in ForbiddenReferencePrefixes)
            {
                source.ShouldNotContain($"using {forbiddenPrefix}", Case.Insensitive, sourceFile);
            }
        }
    }
}
