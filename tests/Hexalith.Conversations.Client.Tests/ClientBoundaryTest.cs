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
}
