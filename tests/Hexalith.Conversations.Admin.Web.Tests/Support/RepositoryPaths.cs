// <copyright file="RepositoryPaths.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Admin.Web.Tests.Support;

/// <summary>
/// Resolves repository-relative paths for evidence generation.
/// </summary>
internal static class RepositoryPaths
{
    public static string FindRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Conversations.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Hexalith.Conversations.slnx from the test output folder.");
    }
}
