using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Conversations.TestSupport;

/// <summary>Reads bounded, raw Git facts from one exact worktree.</summary>
public sealed class GitFacts
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly TimeSpan CommandLimit = TimeSpan.FromSeconds(30);
    private readonly string _gitExecutable;

    /// <summary>Constructs a Git facts reader.</summary>
    /// <param name="gitExecutable">The Git executable; a fixture executable may be supplied by tests.</param>
    public GitFacts(string gitExecutable = "git") => _gitExecutable = gitExecutable;

    /// <summary>Runs Git with both streams drained concurrently and a 30-second maximum.</summary>
    public async Task<GitFact<GitCommandResult>> RunAsync(string repository, IEnumerable<string> arguments, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled execution.");
        }

        if (timeout.HasValue && (timeout.Value <= TimeSpan.Zero || timeout.Value > CommandLimit))
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "GIT_TIMEOUT_INVALID", "The timeout must be positive and at most 30 seconds.");
        }

        if (string.IsNullOrWhiteSpace(repository) || !Directory.Exists(repository))
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Unavailable, "REPOSITORY_UNAVAILABLE", repository ?? string.Empty);
        }

        ProcessStartInfo start = new(_gitExecutable)
        {
            WorkingDirectory = Path.GetFullPath(repository),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string key in start.Environment.Keys.Where(key => key.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            start.Environment.Remove(key);
        }

        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        start.Environment["GIT_CONFIG_COUNT"] = "0";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_NO_REPLACE_OBJECTS"] = "1";
        start.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        start.ArgumentList.Add("-C");
        start.ArgumentList.Add(start.WorkingDirectory);
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("core.quotepath=false");
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("diff.ignoreSubmodules=none");
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using Process process = Process.Start(start) ?? throw new Win32Exception("Git did not start.");
            using MemoryStream stdout = new();
            using MemoryStream stderr = new();
            using CancellationTokenSource limit = new(timeout ?? CommandLimit);
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, limit.Token);
            Task outTask = process.StandardOutput.BaseStream.CopyToAsync(stdout, linked.Token);
            Task errTask = process.StandardError.BaseStream.CopyToAsync(stderr, linked.Token);
            try
            {
                await Task.WhenAll(outTask, errTask, process.WaitForExitAsync(linked.Token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (InvalidOperationException) when (process.HasExited)
                    {
                        // The child exited between the check and the kill request.
                    }
                }

                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

                // Cancellation of pipe copies is independent of child pipe closure.
                return cancellationToken.IsCancellationRequested
                    ? GitFact<GitCommandResult>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled execution.")
                    : GitFact<GitCommandResult>.Fail(GitFactStatus.Timeout, "GIT_TIMEOUT", "Git exceeded its execution limit.");
            }

            return GitFact<GitCommandResult>.Ok(new(process.ExitCode, stdout.ToArray(), stderr.ToArray()));
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException or DirectoryNotFoundException)
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Unavailable, "GIT_UNAVAILABLE", exception.Message);
        }
    }

    /// <summary>Confirms that a directory is the exact Git worktree requested.</summary>
    public async Task<GitFact<string>> ResolveRepositoryAsync(string repository, CancellationToken cancellationToken = default)
    {
        GitFact<GitCommandResult> result = await RunAsync(repository, ["rev-parse", "--show-toplevel"], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return Forward<string>(result);
        }

        if (result.Value!.ExitCode != 0 || !TryDecode(result.Value.Stdout, out string? root))
        {
            return GitFact<string>.Fail(GitFactStatus.Unavailable, "REPOSITORY_UNAVAILABLE", "Git could not identify an exact worktree.");
        }

        string requested = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repository));
        string reported = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root!.Trim()));
        if (!string.Equals(requested, reported, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return GitFact<string>.Fail(GitFactStatus.Unavailable, "REPOSITORY_UNAVAILABLE", "Git resolved an enclosing repository instead of the requested root.");
        }

        return GitFact<string>.Ok(requested);
    }

    /// <summary>Resolves a revision to a full commit identifier.</summary>
    public async Task<GitFact<string>> ResolveRevisionAsync(string repository, string revision, CancellationToken cancellationToken = default)
    {
        GitFact<string> root = await ResolveRepositoryAsync(repository, cancellationToken).ConfigureAwait(false);
        if (!root.IsSuccess)
        {
            return root;
        }

        if (string.IsNullOrWhiteSpace(revision) || revision.StartsWith('-') || revision.Contains('\0'))
        {
            return GitFact<string>.Fail(GitFactStatus.Invalid, "REVISION_INVALID", "A nonempty Git revision is required.");
        }

        GitFact<GitCommandResult> result = await RunAsync(root.Value!, ["rev-parse", "--verify", "--end-of-options", revision + "^{commit}"], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return Forward<string>(result);
        }

        if (result.Value!.ExitCode != 0 || !TryDecode(result.Value.Stdout, out string? resolved))
        {
            GitFact<GitCommandResult> shallow = await RunAsync(root.Value!, ["rev-parse", "--is-shallow-repository"], cancellationToken).ConfigureAwait(false);
            bool isShallow = shallow.IsSuccess && TryDecode(shallow.Value!.Stdout, out string? text) && text!.Trim() == "true";
            return GitFact<string>.Fail(isShallow ? GitFactStatus.Unavailable : GitFactStatus.Invalid, isShallow ? "HISTORY_UNAVAILABLE" : "REVISION_INVALID", revision);
        }

        string value = resolved!.Trim();
        return IsObjectId(value) ? GitFact<string>.Ok(value) : GitFact<string>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed revision.");
    }

    /// <summary>Reads raw, NUL-delimited diff entries between two resolved commits.</summary>
    public async Task<GitFact<IReadOnlyList<GitDiffEntry>>> ReadDiffAsync(string repository, string before, string after, CancellationToken cancellationToken = default)
    {
        GitFact<string> source = await ResolveRevisionAsync(repository, before, cancellationToken).ConfigureAwait(false);
        if (!source.IsSuccess) return Forward<IReadOnlyList<GitDiffEntry>>(source);
        GitFact<string> destination = await ResolveRevisionAsync(repository, after, cancellationToken).ConfigureAwait(false);
        if (!destination.IsSuccess) return Forward<IReadOnlyList<GitDiffEntry>>(destination);
        GitFact<GitCommandResult> result = await RunAsync(repository, ["diff", "--raw", "--no-abbrev", "-z", "--no-ext-diff", "--no-textconv", "--ignore-submodules=none", source.Value!, destination.Value!, "--"], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Forward<IReadOnlyList<GitDiffEntry>>(result);
        if (result.Value!.ExitCode != 0) return GitFact<IReadOnlyList<GitDiffEntry>>.Fail(GitFactStatus.Unavailable, "HISTORY_UNAVAILABLE", "Git could not read the requested diff.");
        try
        {
            string[] tokens = DecodeTokens(result.Value.Stdout);
            List<GitDiffEntry> entries = [];
            for (int i = 0; i < tokens.Length - 1;)
            {
                string[] fields = tokens[i++].TrimStart(':').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length != 5 || i >= tokens.Length - 1) throw new FormatException("Malformed raw diff header.");
                string original = tokens[i++];
                string? sourcePath = null;
                string path = original;
                if (fields[4].StartsWith('R') || fields[4].StartsWith('C'))
                {
                    if (i >= tokens.Length - 1) throw new FormatException("Missing destination path.");
                    sourcePath = original;
                    path = tokens[i++];
                }

                if (!IsMode(fields[0]) || !IsMode(fields[1]) || !IsObjectId(fields[2]) || !IsObjectId(fields[3])) throw new FormatException("Malformed raw diff mode or object.");
                entries.Add(new(fields[0], fields[1], fields[2], fields[3], fields[4], path, sourcePath));
            }

            return GitFact<IReadOnlyList<GitDiffEntry>>.Ok(entries);
        }
        catch (Exception exception) when (exception is DecoderFallbackException or FormatException)
        {
            return GitFact<IReadOnlyList<GitDiffEntry>>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", exception.Message);
        }
    }

    /// <summary>Reads a commit's recursive raw tree entries.</summary>
    public async Task<GitFact<IReadOnlyList<GitTreeEntry>>> ReadTreeAsync(string repository, string revision, CancellationToken cancellationToken = default)
    {
        GitFact<string> commit = await ResolveRevisionAsync(repository, revision, cancellationToken).ConfigureAwait(false);
        if (!commit.IsSuccess) return Forward<IReadOnlyList<GitTreeEntry>>(commit);
        GitFact<GitCommandResult> result = await RunAsync(repository, ["ls-tree", "-rz", "--full-tree", commit.Value!, "--"], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Forward<IReadOnlyList<GitTreeEntry>>(result);
        if (result.Value!.ExitCode != 0) return GitFact<IReadOnlyList<GitTreeEntry>>.Fail(GitFactStatus.Unavailable, "HISTORY_UNAVAILABLE", "Git could not read the requested tree.");
        try
        {
            List<GitTreeEntry> entries = [];
            foreach (string token in DecodeTokens(result.Value.Stdout)[..^1])
            {
                int tab = token.IndexOf('\t');
                if (tab < 0) throw new FormatException("Malformed tree entry.");
                string[] fields = token[..tab].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length != 3 || !IsMode(fields[0]) || !IsObjectId(fields[2])) throw new FormatException("Malformed tree mode or object.");
                entries.Add(new(fields[0], fields[1], fields[2], token[(tab + 1)..]));
            }

            return GitFact<IReadOnlyList<GitTreeEntry>>.Ok(entries);
        }
        catch (Exception exception) when (exception is DecoderFallbackException or FormatException)
        {
            return GitFact<IReadOnlyList<GitTreeEntry>>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", exception.Message);
        }
    }

    /// <summary>Reads exact historical blob bytes and computes SHA-256.</summary>
    public async Task<GitFact<GitBlob>> ReadBlobAsync(string repository, string revision, string path, CancellationToken cancellationToken = default)
    {
        GitFact<string> contained = RepositoryLocator.ResolveContainedPath(repository, path);
        if (!contained.IsSuccess) return Forward<GitBlob>(contained);
        GitFact<string> commit = await ResolveRevisionAsync(repository, revision, cancellationToken).ConfigureAwait(false);
        if (!commit.IsSuccess) return Forward<GitBlob>(commit);
        GitFact<IReadOnlyList<GitTreeEntry>> tree = await ReadTreeAsync(repository, commit.Value!, cancellationToken).ConfigureAwait(false);
        if (!tree.IsSuccess) return Forward<GitBlob>(tree);
        GitTreeEntry? entry = tree.Value!.SingleOrDefault(item => item.Path == path);
        if (entry is null || entry.Type != "blob") return GitFact<GitBlob>.Fail(GitFactStatus.Invalid, "BLOB_INVALID", path);
        GitFact<GitCommandResult> result = await RunAsync(repository, ["cat-file", "blob", entry.ObjectId], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Forward<GitBlob>(result);
        if (result.Value!.ExitCode != 0) return GitFact<GitBlob>.Fail(GitFactStatus.Unavailable, "HISTORY_UNAVAILABLE", path);
        byte[] bytes = result.Value.Stdout;
        return GitFact<GitBlob>.Ok(new(commit.Value!, path, bytes, Convert.ToHexStringLower(SHA256.HashData(bytes))));
    }

    private static GitFact<T> Forward<T>(GitFact<string> source) => GitFact<T>.Fail(source.Status, source.Code!, source.Message ?? string.Empty);
    private static GitFact<T> Forward<T>(GitFact<GitCommandResult> source) => GitFact<T>.Fail(source.Status, source.Code!, source.Message ?? string.Empty);
    private static GitFact<T> Forward<T>(GitFact<IReadOnlyList<GitTreeEntry>> source) => GitFact<T>.Fail(source.Status, source.Code!, source.Message ?? string.Empty);
    private static bool TryDecode(byte[] bytes, out string? value)
    {
        try { value = StrictUtf8.GetString(bytes); return true; }
        catch (DecoderFallbackException) { value = null; return false; }
    }
    private static string[] DecodeTokens(byte[] bytes)
    {
        if (bytes.Length == 0) return [string.Empty];
        if (bytes[^1] != 0) throw new FormatException("NUL-terminated Git output was incomplete.");
        return StrictUtf8.GetString(bytes).Split('\0');
    }
    private static bool IsMode(string value) => value.Length == 6 && value.All(character => character is >= '0' and <= '7');
    private static bool IsObjectId(string value) => (value.Length == 40 || value.Length == 64) && value.All(Uri.IsHexDigit);
}
