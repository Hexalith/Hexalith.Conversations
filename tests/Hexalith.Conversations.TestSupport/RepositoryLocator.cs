namespace Hexalith.Conversations.TestSupport;

/// <summary>Resolves paths without permitting lexical or symbolic-link escape.</summary>
public static class RepositoryLocator
{
    /// <summary>Resolves a path beneath the exact repository root.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="relativePath">A relative path using forward slashes.</param>
    /// <returns>The contained full path, or an explicit failure.</returns>
    public static GitFact<string> ResolveContainedPath(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", "A nonempty relative path is required.");
        }

        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        if (relativePath.Contains('\\') && Path.DirectorySeparatorChar != '\\')
        {
            // Backslash is a valid literal filename on Unix, not a separator.
            normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        string fullRoot = Path.GetFullPath(root);
        string candidate = Path.GetFullPath(Path.Combine(fullRoot, normalized));
        string prefix = Path.TrimEndingDirectorySeparator(fullRoot) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", relativePath);
        }

        string current = fullRoot;
        foreach (string segment in normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            FileSystemInfo item = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (item.LinkTarget is not null)
            {
                return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", "Symbolic links are not accepted in evidence paths.");
            }
        }

        return GitFact<string>.Ok(candidate);
    }
}
