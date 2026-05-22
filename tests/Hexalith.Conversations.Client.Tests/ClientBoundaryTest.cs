// <copyright file="ClientBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Client;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Client.Tests;

/// <summary>
/// Verifies that the adopter client does not expose server implementation details.
/// </summary>
public sealed class ClientBoundaryTest
{
    /// <summary>
    /// Ensures the client assembly references contracts but not server infrastructure.
    /// </summary>
    [Fact]
    public void ClientAssemblyShouldReferenceContractsOnlyFromConversationsProjects()
    {
        string[] references = typeof(ClientAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        references.ShouldContain("Hexalith.Conversations.Contracts");
        references.ShouldNotContain("Hexalith.Conversations.Server");
        references.ShouldNotContain("Hexalith.EventStore");
        references.ShouldNotContain(name => name.StartsWith("Dapr", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures the client package only adds Microsoft HTTP/DI abstractions for transport registration.
    /// </summary>
    [Fact]
    public void ClientAssemblyShouldOnlyReferenceAllowedMicrosoftTransportAssemblies()
    {
        string[] references = typeof(ClientAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        string[] microsoftReferences = references
            .Where(name => name.StartsWith("Microsoft.", StringComparison.Ordinal))
            .ToArray();
        string[] allowedReferences =
        [
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Http",
        ];

        microsoftReferences.ShouldAllBe(name => allowedReferences.Contains(name, StringComparer.Ordinal));
        references.ShouldNotContain(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures the public client surface does not promote raw HTTP fallback usage.
    /// </summary>
    [Fact]
    public void ClientAssemblyShouldNotExposeRawHttpFallbackSurface()
    {
        Type[] publicTypes = typeof(ClientAssemblyMarker).Assembly.GetExportedTypes();
        string[] publicSurface = publicTypes
            .SelectMany(type => new[]
            {
                type.FullName ?? type.Name,
            }.Concat(type.GetMembers().Select(member => $"{type.FullName}.{member.Name}")))
            .ToArray();

        publicSurface.ShouldNotContain(name => name.Contains("RawHttp", StringComparison.OrdinalIgnoreCase));
        publicSurface.ShouldNotContain(name => name.Contains("HttpResponseMessage", StringComparison.OrdinalIgnoreCase));
        publicSurface.ShouldNotContain(name => name.Contains("Fallback", StringComparison.OrdinalIgnoreCase));
    }
}
