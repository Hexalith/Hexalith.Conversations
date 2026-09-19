// <copyright file="RepositoryEvidencePathResolver.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Resolves an evidence-bearing module only when the candidate is an exact Git worktree.
/// </summary>
/// <remarks>
/// An uninitialized submodule directory is still inside the parent repository, so invoking Git there silently
/// reads the parent's object database. Evidence checks must reject that fallback. Root workspace checkouts are
/// accepted as a read-only local mirror when the nested submodule has not been materialized.
/// </remarks>
internal static class RepositoryEvidencePathResolver
{
    /// <summary>
    /// Resolves the nested module checkout or its root-workspace sibling and fails closed when neither is a repository.
    /// </summary>
    /// <param name="repositoryRoot">The Conversations repository root.</param>
    /// <param name="moduleRelativePath">The nested module path, such as <c>references/Hexalith.Builds</c>.</param>
    /// <returns>The exact Git worktree that owns the module objects.</returns>
    internal static string Resolve(string repositoryRoot, string moduleRelativePath)
    {
        string nestedCandidate = Path.GetFullPath(Path.Combine(repositoryRoot, moduleRelativePath));
        string moduleName = Path.GetFileName(moduleRelativePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string siblingCandidate = Path.GetFullPath(Path.Combine(repositoryRoot, "..", moduleName));

        foreach (string candidate in new[] { nestedCandidate, siblingCandidate }.Distinct(StringComparer.Ordinal))
        {
            if (IsExactGitWorktree(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Evidence repository '{moduleRelativePath}' is unavailable. Expected an initialized submodule at "
            + $"'{nestedCandidate}' or a root-workspace checkout at '{siblingCandidate}'.");
    }

    private static bool IsExactGitWorktree(string candidate)
    {
        if (!Directory.Exists(candidate))
        {
            return false;
        }

        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = candidate,
        };
        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("--show-toplevel");

        try
        {
            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("git could not be started while resolving evidence repositories.");
            string output = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(milliseconds: 10_000) || process.ExitCode != 0)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                return false;
            }

            string reportedRoot = Path.GetFullPath(output.Trim());
            return string.Equals(
                reportedRoot.TrimEnd(Path.DirectorySeparatorChar),
                candidate.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
