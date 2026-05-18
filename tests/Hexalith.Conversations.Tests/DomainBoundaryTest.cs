// <copyright file="DomainBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests;

/// <summary>
/// Verifies that the domain scaffold remains deterministic and infrastructure-free.
/// </summary>
public sealed class DomainBoundaryTest
{
    /// <summary>
    /// Ensures the domain assembly does not reference server, authorization, or transport packages.
    /// </summary>
    [Fact]
    public void DomainAssemblyShouldNotReferenceServerOrInfrastructureAssemblies()
    {
        string[] references = typeof(ConversationsAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        references.ShouldContain("Hexalith.Conversations.Contracts");
        references.ShouldNotContain("Hexalith.Conversations.Server");
        references.ShouldNotContain("Hexalith.Tenants");
        references.ShouldNotContain("Hexalith.Parties");
        references.ShouldNotContain(name => name.StartsWith("Dapr", StringComparison.Ordinal));
        references.ShouldNotContain(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }
}
