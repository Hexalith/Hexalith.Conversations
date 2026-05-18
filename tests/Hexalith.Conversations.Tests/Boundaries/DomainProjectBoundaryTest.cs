// <copyright file="DomainProjectBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Xml.Linq;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Boundaries;

/// <summary>
/// Verifies the domain project references only the narrow write-side domain libraries it needs.
/// </summary>
public sealed class DomainProjectBoundaryTest
{
    private static readonly string ProjectPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Hexalith.Conversations", "Hexalith.Conversations.csproj"));

    /// <summary>
    /// The domain project references contracts plus the local write-side aggregate libraries.
    /// </summary>
    [Fact]
    public void DomainProjectShouldReferenceOnlyExpectedDomainLibraries()
    {
        XDocument project = XDocument.Load(ProjectPath);

        string[] references = project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        references.ShouldContain(reference => reference.Contains(@"Hexalith.Conversations.Contracts\", StringComparison.Ordinal));
        references.ShouldContain(reference => reference.Contains(@"Hexalith.EventStore.Client\", StringComparison.Ordinal));
        references.ShouldContain(reference => reference.Contains(@"Hexalith.EventStore.Contracts\", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains(@"Hexalith.Conversations.Server\", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains(@"Hexalith.Tenants\", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains(@"Hexalith.Parties\", StringComparison.Ordinal));
        references.ShouldNotContain(reference => reference.Contains(@"Hexalith.FrontComposer\", StringComparison.Ordinal));
    }

    /// <summary>
    /// The domain project does not add transport, UI, hosting, or provider SDK packages.
    /// </summary>
    [Fact]
    public void DomainProjectShouldNotReferenceForbiddenRuntimePackages()
    {
        XDocument project = XDocument.Load(ProjectPath);

        string[] packageReferences = project
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        packageReferences.ShouldNotContain(package => package.StartsWith("Dapr", StringComparison.Ordinal));
        packageReferences.ShouldNotContain(package => package.StartsWith("Aspire", StringComparison.Ordinal));
        packageReferences.ShouldNotContain(package => package.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        packageReferences.ShouldNotContain(package => package.Contains("OpenAI", StringComparison.OrdinalIgnoreCase));
        packageReferences.ShouldNotContain(package => package.Contains("Azure.AI", StringComparison.OrdinalIgnoreCase));
    }
}
