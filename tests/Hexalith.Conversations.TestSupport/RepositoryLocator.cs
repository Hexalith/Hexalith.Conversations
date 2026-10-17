namespace Hexalith.Conversations.TestSupport;

/// <summary>Resolves paths without permitting lexical or symbolic-link escape.</summary>
public static class RepositoryLocator
{
    /// <summary>Finds the nearest repository marker above the supplied directory.</summary>
    /// <param name="startDirectory">An optional discovery origin; the test output directory is the default.</param>
    /// <returns>The existing repository root; discovery never falls back to the current directory.</returns>
    public static string FindRepositoryRoot(string? startDirectory = null)
    {
        for (DirectoryInfo? directory = new(startDirectory ?? AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
                return directory.FullName;
        }
        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    /// <summary>Resolves an existing evidence read beneath its owning repository.</summary>
    public static string ContainedFile(string path)
    {
        string full = Path.GetFullPath(path);
        string root = FindRepositoryRoot();
        GitFact<string> fact = ResolveContainedPath(root, Path.GetRelativePath(root, full).Replace('\\', '/'));
        if (!fact.IsSuccess) throw new IOException(fact.Code + ": " + fact.Message);
        return fact.Value!;
    }

    /// <summary>Reads contained evidence bytes.</summary>
    public static byte[] ReadAllBytes(string path) => File.ReadAllBytes(ContainedFile(path));

    /// <summary>Reads contained evidence text with the caller's original encoding.</summary>
    public static string ReadAllText(string path, System.Text.Encoding? encoding = null)
        => File.ReadAllText(ContainedFile(path), encoding ?? System.Text.Encoding.UTF8);

    /// <summary>Reads contained evidence lines with the caller's original encoding.</summary>
    public static string[] ReadAllLines(string path, System.Text.Encoding? encoding = null)
        => File.ReadAllLines(ContainedFile(path), encoding ?? System.Text.Encoding.UTF8);

    /// <summary>Resolves a path beneath the exact repository root.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="relativePath">A relative path using forward slashes.</param>
    /// <returns>The contained full path, or an explicit failure.</returns>
    public static GitFact<string> ResolveContainedPath(string root, string relativePath)
        => Resolve(root, relativePath, checkWorktreeLinks: true);

    /// <summary>Validates a committed path without consulting the current worktree entry.</summary>
    internal static GitFact<string> ResolveHistoricalPath(string root, string relativePath)
        => Resolve(root, relativePath, checkWorktreeLinks: false);

    private static GitFact<string> Resolve(string root, string relativePath, bool checkWorktreeLinks)
    {
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(relativePath)
            || root.Contains('\0') || relativePath.Contains('\0') || Path.IsPathRooted(relativePath)
            || relativePath.Split('/').Any(segment => segment is "" or "." or "..")
            || (OperatingSystem.IsWindows() && relativePath.Contains('\\')))
        {
            return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", "A nonempty relative path is required.");
        }
        try
        {
            string fullRoot = Path.GetFullPath(root);
            if (!Directory.Exists(fullRoot) || HasLinkedAncestor(fullRoot))
                return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", "The repository root is missing or symbolic.");
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string candidate = Path.GetFullPath(Path.Combine(fullRoot, normalized));
            string prefix = Path.EndsInDirectorySeparator(fullRoot) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", relativePath);
            if (checkWorktreeLinks)
            {
                string current = fullRoot;
                foreach (string segment in normalized.Split(Path.DirectorySeparatorChar))
                {
                    current = Path.Combine(current, segment);
                    FileSystemInfo item = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
                    if (item.LinkTarget is not null)
                        return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", "Symbolic links are not accepted in evidence paths.");
                }
            }
            return GitFact<string>.Ok(candidate);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", exception.Message);
        }
    }

    private static bool HasLinkedAncestor(string path)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            if (new DirectoryInfo(current).LinkTarget is not null) return true;
            if (Path.GetPathRoot(current) == current) break;
        }
        return false;
    }
}
