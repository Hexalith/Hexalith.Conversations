// <copyright file="RepositoryTestContextTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.IntegrationTests.Testing;

/// <summary>
/// Verifies shared repository fixtures used by integration tests.
/// </summary>
public sealed class RepositoryTestContextTest
{
    /// <summary>
    /// Ensures the repository context resolves the solution and source/test folders.
    /// </summary>
    [Fact]
    public void LocateShouldFindRepositoryRoot()
    {
        RepositoryTestContext context = RepositoryTestContext.Locate();

        File.Exists(context.SolutionPath).ShouldBeTrue();
        Directory.Exists(context.SourceDirectory).ShouldBeTrue();
        Directory.Exists(context.TestDirectory).ShouldBeTrue();
    }
}

