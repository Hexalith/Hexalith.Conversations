// <copyright file="RepositoryTestContext.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Provides repository paths for tests that need to inspect project shape.
/// </summary>
/// <param name="RootDirectory">The repository root directory.</param>
public sealed record RepositoryTestContext(string RootDirectory)
{
    /// <summary>
    /// Gets the solution file path.
    /// </summary>
    public string SolutionPath => Path.Combine(RootDirectory, "Hexalith.Conversations.slnx");

    /// <summary>
    /// Gets the source directory path.
    /// </summary>
    public string SourceDirectory => Path.Combine(RootDirectory, "src");

    /// <summary>
    /// Gets the test directory path.
    /// </summary>
    public string TestDirectory => Path.Combine(RootDirectory, "tests");

    /// <summary>
    /// Locates the repository root from the current test execution directory.
    /// </summary>
    /// <returns>The repository test context.</returns>
    public static RepositoryTestContext Locate()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props"))
                && File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
            {
                return new RepositoryTestContext(directory.FullName);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Hexalith.Conversations repository root.");
    }
}

