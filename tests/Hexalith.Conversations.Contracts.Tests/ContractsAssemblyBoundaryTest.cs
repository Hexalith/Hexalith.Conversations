// <copyright file="ContractsAssemblyBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts;

using Shouldly;

using Xunit;

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
        "Hexalith.FrontComposer",
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
}
