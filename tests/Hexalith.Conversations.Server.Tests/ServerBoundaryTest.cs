// <copyright file="ServerBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

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
        references.ShouldNotContain("Hexalith.EventStore.Server");
        references.ShouldNotContain("Dapr.Client");
    }
}
