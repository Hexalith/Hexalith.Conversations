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
/// Verifies the Server boundary as a legitimate EventStore domain-service host (Story 2.1, FR-3).
/// </summary>
/// <remarks>
/// Premise change recorded per AC-4/AC-7: before Story 2.1 the Server was a fail-closed scaffold that
/// referenced <b>no</b> EventStore runtime/host packages, and this guard asserted exactly that. Story 2.1
/// fills the unbuilt host slot with the shared two-line domain-service host, so the guard is re-expressed to
/// <b>permit</b> the <c>Hexalith.EventStore.DomainService</c> host SDK reference (and require it, so a silent
/// removal of the host is caught) while still forbidding the genuinely out-of-bounds dependencies: the
/// <c>Hexalith.EventStore.Server</c> gateway, <c>Hexalith.Tenants.Server</c>, <c>Hexalith.Parties</c>,
/// <c>Hexalith.FrontComposer</c>, and a direct <c>Dapr.Client</c> reference (DAPR arrives transitively via
/// <c>Dapr.AspNetCore</c>; the host must not take a direct gateway-style dependency on it). The change is
/// recorded in <c>docs/release-evidence/at-risk-test-register-v1.{json,md}</c>.
/// </remarks>
public sealed class ServerBoundaryTest
{
    /// <summary>
    /// Ensures the Server assembly references the domain-service host SDK and the Conversations domain/contracts,
    /// without taking a direct dependency on any forbidden gateway/runtime assembly.
    /// </summary>
    [Fact]
    public void ServerAssemblyShouldReferenceDomainServiceHostWithoutForbiddenRuntime()
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

        // Story 2.1: the Server is now a domain-service host — the SDK host reference is required, not forbidden.
        references.ShouldContain("Hexalith.EventStore.DomainService");

        // Still genuinely out of bounds: the gateway, server-side Tenants, Parties, the UI shell, and a direct
        // Dapr.Client dependency (DAPR is consumed transitively through Dapr.AspNetCore, not referenced directly).
        references.ShouldNotContain("Hexalith.EventStore.Server");
        references.ShouldNotContain("Hexalith.Tenants.Server");
        references.ShouldNotContain("Hexalith.Parties");
        references.ShouldNotContain("Hexalith.FrontComposer");
        references.ShouldNotContain("Dapr.Client");
    }

    /// <summary>
    /// Ensures the Server project declares the domain-service host reference explicitly in XML and does not
    /// hide any forbidden gateway/runtime dependency.
    /// </summary>
    [Fact]
    public void ServerProjectFileShouldDeclareDomainServiceHostAndNoForbiddenRuntimeReferences()
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

        // Story 2.1: the two-line host requires the domain-service SDK project reference.
        references.ShouldContain(reference => reference.EndsWith(
            "Hexalith.EventStore.DomainService.csproj",
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
