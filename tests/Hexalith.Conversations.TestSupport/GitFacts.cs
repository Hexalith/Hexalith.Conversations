using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Conversations.TestSupport;

/// <summary>Reads bounded, raw Git facts from one exact worktree.</summary>
public sealed class GitFacts
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly TimeSpan CommandLimit = TimeSpan.FromSeconds(30);
    private const int OutputByteLimit = 16 * 1024 * 1024;
    private readonly string _gitExecutable;
    private readonly IReadOnlyList<string> _executablePrefixArguments;
    private readonly bool _includeGitPreamble;

    /// <summary>Constructs a Git facts reader.</summary>
    /// <param name="gitExecutable">The Git executable; a fixture executable may be supplied by tests.</param>
    /// <param name="executablePrefixArguments">Arguments placed before the requested command for process fixtures.</param>
    /// <param name="includeGitPreamble">Whether to add isolated Git working-directory and configuration arguments.</param>
    public GitFacts(string gitExecutable = "git", IReadOnlyList<string>? executablePrefixArguments = null, bool includeGitPreamble = true)
    {
        _gitExecutable = gitExecutable;
        _executablePrefixArguments = executablePrefixArguments ?? [];
        _includeGitPreamble = includeGitPreamble;
    }

    /// <summary>Captures a command synchronously through the bounded concurrent runner.</summary>
    /// <param name="repository">The exact command working directory.</param>
    /// <param name="arguments">Individual command arguments.</param>
    /// <param name="executable">Git by default; explicit process fixtures may name another executable.</param>
    /// <returns>Raw completion; unavailable execution throws with its stable code.</returns>
    public static GitCommandResult Capture(string repository, IEnumerable<string> arguments, string executable = "git")
    {
        GitFact<GitCommandResult> fact = new GitFacts(executable, includeGitPreamble: executable == "git")
            .RunAsync(repository, arguments).GetAwaiter().GetResult();
        if (!fact.IsSuccess) throw new InvalidOperationException(fact.Code + ": " + fact.Message);
        return fact.Value!;
    }

    /// <summary>Captures decoded command streams while preserving the process exit code.</summary>
    public static (int ExitCode, string Output, string Error) CaptureText(string repository, IEnumerable<string> arguments, string executable = "git")
    {
        GitCommandResult result = Capture(repository, arguments, executable);
        return (result.ExitCode, StrictUtf8.GetString(result.Stdout), StrictUtf8.GetString(result.Stderr));
    }

    /// <summary>Captures command completion with both streams drained concurrently and a 30-second maximum. A successful capture may have a nonzero exit code; callers must check <see cref="GitCommandResult.ExitCode"/> before treating its output as a Git fact.</summary>
    public async Task<GitFact<GitCommandResult>> RunAsync(string repository, IEnumerable<string> arguments, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        if (arguments is null)
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "GIT_ARGUMENT_INVALID", "Git arguments must be non-null.");
        }
        string[] commandArguments = arguments.ToArray();
        if (commandArguments.Any(argument => argument is null))
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "GIT_ARGUMENT_INVALID", "Git arguments must be non-null.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled execution.");
        }

        if (timeout.HasValue && (timeout.Value <= TimeSpan.Zero || timeout.Value > CommandLimit))
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "GIT_TIMEOUT_INVALID", "The timeout must be positive and at most 30 seconds.");
        }

        if (repository is not null && repository.Contains('\0'))
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", "The repository path contains NUL.");
        }
        if (string.IsNullOrEmpty(repository) || !Directory.Exists(repository))
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Unavailable, "REPOSITORY_UNAVAILABLE", repository ?? string.Empty);
        }

        // setsid makes the command and all descendants one killable Unix process group.
        // A Git process may exit while a child still holds its redirected pipes open.
        string workingDirectory;
        try { workingDirectory = Path.GetFullPath(repository); }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", exception.Message);
        }
        string executable = _gitExecutable;
        if (!OperatingSystem.IsWindows())
        {
            string? resolved = ResolveUnixExecutable(_gitExecutable);
            if (resolved is null)
                return GitFact<GitCommandResult>.Fail(GitFactStatus.Unavailable, "GIT_UNAVAILABLE", $"Executable not found: {_gitExecutable}");
            executable = resolved;
        }
        ProcessStartInfo start = new(OperatingSystem.IsWindows() ? executable : "setsid")
        {
            WorkingDirectory = workingDirectory,
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
        start.Environment["GIT_GRAFT_FILE"] = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        start.Environment["GIT_NO_LAZY_FETCH"] = "1";
        start.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        if (!OperatingSystem.IsWindows()) start.ArgumentList.Add(executable);
        foreach (string prefixArgument in _executablePrefixArguments) start.ArgumentList.Add(prefixArgument);
        if (_includeGitPreamble)
        {
            start.ArgumentList.Add("-C");
            start.ArgumentList.Add(start.WorkingDirectory);
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("core.quotepath=false");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("diff.ignoreSubmodules=none");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("diff.renames=true");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("diff.renameLimit=0");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("core.commitGraph=false");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("advice.graftFileDeprecated=false");
        }
        foreach (string argument in commandArguments)
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            // Start can be delayed by an executable fixture. One deadline covers startup and
            // execution; cleanup uses separate bounded waits. Kill late starts after a timeout.
            using CancellationTokenSource limit = new(timeout ?? CommandLimit);
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, limit.Token);
            Task<Process?> startup = Task.Run(() => Process.Start(start));
            Process process;
            try
            {
                process = await startup.WaitAsync(linked.Token).ConfigureAwait(false)
                    ?? throw new Win32Exception("Git did not start.");
            }
            catch (OperationCanceledException)
            {
                _ = startup.ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion && task.Result is { } late)
                    {
                        TerminateTree(late, null);
                        late.Dispose();
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                return cancellationToken.IsCancellationRequested
                    ? GitFact<GitCommandResult>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled startup.")
                    : GitFact<GitCommandResult>.Fail(GitFactStatus.Timeout, "GIT_TIMEOUT", "Git startup exceeded its execution limit.");
            }
            using (process)
            {
                using GitProcessJob? job = OperatingSystem.IsWindows() ? new GitProcessJob(process) : null;
                using MemoryStream stdout = new();
                using MemoryStream stderr = new();
                int remaining = OutputByteLimit;
                int overflow = 0;
                Task outTask = CaptureAsync(process.StandardOutput.BaseStream, stdout);
                Task errTask = CaptureAsync(process.StandardError.BaseStream, stderr);
                try
                {
                    await Task.WhenAll(outTask, errTask, process.WaitForExitAsync(linked.Token)).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    TerminateTree(process, job);
                    try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
                    catch (TimeoutException) { }
                    try { await Task.WhenAll(outTask, errTask).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                    catch (TimeoutException) { }
                    if (Volatile.Read(ref overflow) != 0)
                        return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_LIMIT_EXCEEDED", "Git output exceeded 16 MiB.");
                    return cancellationToken.IsCancellationRequested
                        ? GitFact<GitCommandResult>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled execution.")
                        : GitFact<GitCommandResult>.Fail(GitFactStatus.Timeout, "GIT_TIMEOUT", "Git exceeded its execution limit.");
                }
                catch (Exception exception) when (exception is IOException or ObjectDisposedException)
                {
                    TerminateTree(process, job);
                    try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
                    catch (TimeoutException) { }
                    return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "GIT_CAPTURE_INVALID", exception.Message);
                }

                if (Volatile.Read(ref overflow) != 0)
                {
                    TerminateTree(process, job);
                    return GitFact<GitCommandResult>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_LIMIT_EXCEEDED", "Git output exceeded 16 MiB.");
                }
                GitCommandResult completed = new(process.ExitCode, stdout.ToArray(), stderr.ToArray());
                TerminateTree(process, job);
                return GitFact<GitCommandResult>.Ok(completed);

                async Task CaptureAsync(Stream input, Stream output)
                {
                    byte[] buffer = new byte[8192];
                    while (true)
                    {
                        int count = await input.ReadAsync(buffer, linked.Token).ConfigureAwait(false);
                        if (count == 0) return;
                        if (Interlocked.Add(ref remaining, -count) < 0)
                        {
                            Interlocked.Exchange(ref overflow, 1);
                            linked.Cancel();
                            return;
                        }
                        await output.WriteAsync(buffer.AsMemory(0, count), linked.Token).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException or DirectoryNotFoundException or ArgumentException or UnauthorizedAccessException)
        {
            return GitFact<GitCommandResult>.Fail(GitFactStatus.Unavailable, "GIT_UNAVAILABLE", exception.Message);
        }
    }

    private static void TerminateTree(Process process, GitProcessJob? job)
    {
        if (!OperatingSystem.IsWindows())
        {
            // Negative PID targets the session's process group even when its leader exited.
            _ = KillProcessGroup(-process.Id, 9);
            return;
        }
        job?.Terminate();
        try { process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (Win32Exception) { }
    }

    private static string? ResolveUnixExecutable(string executable)
    {
        if (executable.Contains(Path.DirectorySeparatorChar))
            return IsUnixExecutable(executable) ? Path.GetFullPath(executable) : null;
        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            if (directory.Length == 0) continue;
            string candidate = Path.Combine(directory, executable);
            if (IsUnixExecutable(candidate)) return Path.GetFullPath(candidate);
        }
        return null;
    }

    private static bool IsUnixExecutable(string path)
    {
        if (OperatingSystem.IsWindows()) return false;
        try
        {
            return File.Exists(path)
                && (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int KillProcessGroup(int pid, int signal);

    /// <summary>Confirms that a directory is the exact Git worktree requested.</summary>
    public async Task<GitFact<string>> ResolveRepositoryAsync(string repository, CancellationToken cancellationToken = default)
    {
        if (repository is not null && repository.Contains('\0'))
            return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", "The repository path contains NUL.");
        if (string.IsNullOrEmpty(repository) || !Directory.Exists(repository))
            return GitFact<string>.Fail(GitFactStatus.Unavailable, "REPOSITORY_UNAVAILABLE", repository ?? string.Empty);
        GitFact<string> safeRoot = RepositoryLocator.ResolveHistoricalPath(repository, "repository-root-probe");
        if (!safeRoot.IsSuccess) return Forward<string>(safeRoot);
        GitFact<GitCommandResult> result = await RunAsync(repository, ["rev-parse", "--show-toplevel"], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return Forward<string>(result);
        }

        if (result.Value!.ExitCode != 0)
        {
            return GitFact<string>.Fail(GitFactStatus.Unavailable, "REPOSITORY_UNAVAILABLE", "Git could not identify an exact worktree. " + ErrorText(result.Value.Stderr));
        }
        if (!TryDecode(result.Value.Stdout, out string? root) || root is null || !root.EndsWith('\n'))
            return GitFact<string>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed worktree path.");

        string requested;
        string reported;
        try
        {
            requested = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repository));
            reported = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root[..^1]));
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return GitFact<string>.Fail(GitFactStatus.Invalid, "REPOSITORY_PATH_ESCAPE", exception.Message);
        }
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

        if (result.Value!.ExitCode != 0)
        {
            GitFact<GitCommandResult> shallow = await RunAsync(root.Value!, ["rev-parse", "--is-shallow-repository"], cancellationToken).ConfigureAwait(false);
            if (!shallow.IsSuccess) return Forward<string>(shallow);
            if (shallow.Value!.ExitCode != 0 || !TryDecode(shallow.Value.Stdout, out string? text)
                || (text != "true\n" && text != "false\n"))
                return GitFact<string>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed shallow status.");
            bool isShallow = text == "true\n";
            if (isShallow)
            {
                GitFact<bool> proven = await IsProvenShallowAncestorAsync(root.Value!, revision, cancellationToken).ConfigureAwait(false);
                if (!proven.IsSuccess) return GitFact<string>.Fail(proven.Status, proven.Code!, proven.Message ?? string.Empty);
                isShallow = proven.Value;
            }
            return GitFact<string>.Fail(isShallow ? GitFactStatus.Unavailable : GitFactStatus.Invalid, isShallow ? "HISTORY_UNAVAILABLE" : "REVISION_INVALID", revision);
        }

        if (!TryDecode(result.Value.Stdout, out string? resolved))
            return GitFact<string>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned malformed revision bytes.");

        string value = resolved!;
        return value.EndsWith('\n') && IsObjectId(value[..^1]) && !value[..^1].All(character => character == '0')
            ? GitFact<string>.Ok(value[..^1])
            : GitFact<string>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed revision.");
    }

    /// <summary>Reads raw, NUL-delimited diff entries between two resolved commits.</summary>
    public async Task<GitFact<IReadOnlyList<GitDiffEntry>>> ReadDiffAsync(string repository, string before, string after, CancellationToken cancellationToken = default)
    {
        GitFact<string> root = await ResolveRepositoryAsync(repository, cancellationToken).ConfigureAwait(false);
        if (!root.IsSuccess) return Forward<IReadOnlyList<GitDiffEntry>>(root);
        GitFact<string> source = await ResolveRevisionAsync(root.Value!, before, cancellationToken).ConfigureAwait(false);
        if (!source.IsSuccess) return Forward<IReadOnlyList<GitDiffEntry>>(source);
        GitFact<string> destination = await ResolveRevisionAsync(root.Value!, after, cancellationToken).ConfigureAwait(false);
        if (!destination.IsSuccess) return Forward<IReadOnlyList<GitDiffEntry>>(destination);
        GitFact<GitCommandResult> result = await RunAsync(root.Value!, ["diff", "--raw", "--no-abbrev", "-z", "--no-ext-diff", "--no-textconv", "--ignore-submodules=none", "--find-renames=50%", source.Value!, destination.Value!, "--"], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Forward<IReadOnlyList<GitDiffEntry>>(result);
        if (result.Value!.ExitCode != 0) return GitFact<IReadOnlyList<GitDiffEntry>>.Fail(GitFactStatus.Unavailable, "HISTORY_UNAVAILABLE", "Git could not read the requested diff. " + ErrorText(result.Value.Stderr));
        try
        {
            string[] tokens = DecodeTokens(result.Value.Stdout);
            List<GitDiffEntry> entries = [];
            HashSet<string> paths = new(StringComparer.Ordinal);
            HashSet<string> sources = new(StringComparer.Ordinal);
            for (int i = 0; i < tokens.Length - 1;)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string header = tokens[i++];
                if (!header.StartsWith(':') || header.StartsWith("::", StringComparison.Ordinal)) throw new FormatException("Malformed raw diff header.");
                string[] fields = header[1..].Split(' ', StringSplitOptions.None);
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

                if (!IsDiffMode(fields[0]) || !IsDiffMode(fields[1]) || !IsObjectId(fields[2]) || !IsObjectId(fields[3])
                    || !IsDiffStatus(fields[4]) || !IsDiffRelation(fields) || string.IsNullOrEmpty(original) || string.IsNullOrEmpty(path)
                    || (sourcePath is not null && sourcePath == path)
                    || !RepositoryLocator.ResolveHistoricalPath(root.Value!, original).IsSuccess
                    || !RepositoryLocator.ResolveHistoricalPath(root.Value!, path).IsSuccess
                    || (fields[0] != "000000" && !sources.Add(original))
                    || !paths.Add(path)) throw new FormatException("Malformed raw diff mode, status, object, or path.");
                entries.Add(new(fields[0], fields[1], fields[2], fields[3], fields[4], path, sourcePath));
            }

            return GitFact<IReadOnlyList<GitDiffEntry>>.Ok(entries);
        }
        catch (Exception exception) when (exception is DecoderFallbackException or FormatException)
        {
            return GitFact<IReadOnlyList<GitDiffEntry>>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", exception.Message);
        }
        catch (OperationCanceledException)
        {
            return GitFact<IReadOnlyList<GitDiffEntry>>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled diff parsing.");
        }
    }

    /// <summary>Reads a commit's recursive raw tree entries.</summary>
    public async Task<GitFact<IReadOnlyList<GitTreeEntry>>> ReadTreeAsync(string repository, string revision, CancellationToken cancellationToken = default)
    {
        GitFact<string> root = await ResolveRepositoryAsync(repository, cancellationToken).ConfigureAwait(false);
        if (!root.IsSuccess) return Forward<IReadOnlyList<GitTreeEntry>>(root);
        GitFact<string> commit = await ResolveRevisionAsync(root.Value!, revision, cancellationToken).ConfigureAwait(false);
        if (!commit.IsSuccess) return Forward<IReadOnlyList<GitTreeEntry>>(commit);
        GitFact<GitCommandResult> result = await RunAsync(root.Value!, ["ls-tree", "-rz", "--full-tree", commit.Value!, "--"], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Forward<IReadOnlyList<GitTreeEntry>>(result);
        if (result.Value!.ExitCode != 0) return GitFact<IReadOnlyList<GitTreeEntry>>.Fail(GitFactStatus.Unavailable, "HISTORY_UNAVAILABLE", "Git could not read the requested tree. " + ErrorText(result.Value.Stderr));
        try
        {
            return GitFact<IReadOnlyList<GitTreeEntry>>.Ok(ParseTree(root.Value!, result.Value.Stdout, cancellationToken));
        }
        catch (Exception exception) when (exception is DecoderFallbackException or FormatException)
        {
            return GitFact<IReadOnlyList<GitTreeEntry>>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", exception.Message);
        }
        catch (OperationCanceledException)
        {
            return GitFact<IReadOnlyList<GitTreeEntry>>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled tree parsing.");
        }
    }

    private static IReadOnlyList<GitTreeEntry> ParseTree(string repository, byte[] bytes, CancellationToken cancellationToken)
    {
        List<GitTreeEntry> entries = [];
        HashSet<string> paths = new(StringComparer.Ordinal);
        foreach (string token in DecodeTokens(bytes)[..^1])
        {
            cancellationToken.ThrowIfCancellationRequested();
            int tab = token.IndexOf('\t');
            if (tab < 0) throw new FormatException("Malformed tree entry.");
            string[] fields = token[..tab].Split(' ', StringSplitOptions.None);
            string path = token[(tab + 1)..];
            if (fields.Length != 3 || !IsTreeMode(fields[0], fields[1]) || !IsObjectId(fields[2])
                || fields[2].All(character => character == '0') || string.IsNullOrEmpty(path)
                || !RepositoryLocator.ResolveHistoricalPath(repository, path).IsSuccess || !paths.Add(path))
                throw new FormatException("Malformed or duplicate tree entry.");
            entries.Add(new(fields[0], fields[1], fields[2], path));
        }
        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
            {
                if (paths.Contains(path[..slash])) throw new FormatException("A tree file is the ancestor of another path.");
            }
        }
        return entries;
    }

    /// <summary>Reads a historical regular file, rejecting symlink and gitlink modes.</summary>
    public async Task<GitFact<GitBlob>> ReadRegularBlobAsync(string repository, string revision, string path, CancellationToken cancellationToken = default)
    {
        GitFact<IReadOnlyList<GitTreeEntry>> tree = await ReadTreeAsync(repository, revision, cancellationToken).ConfigureAwait(false);
        if (!tree.IsSuccess) return Forward<GitBlob>(tree);
        GitTreeEntry? entry = tree.Value!.SingleOrDefault(entry => entry.Path == path);
        if (entry is null || entry.Mode is not ("100644" or "100755"))
            return GitFact<GitBlob>.Fail(GitFactStatus.Invalid, "BLOB_INVALID", "A historical regular file is required: " + path);
        return await ReadBlobAsync(repository, revision, path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads exact historical blob bytes and computes SHA-256.</summary>
    public async Task<GitFact<GitBlob>> ReadBlobAsync(string repository, string revision, string path, CancellationToken cancellationToken = default)
    {
        GitFact<string> root = await ResolveRepositoryAsync(repository, cancellationToken).ConfigureAwait(false);
        if (!root.IsSuccess) return Forward<GitBlob>(root);
        GitFact<string> contained = RepositoryLocator.ResolveHistoricalPath(root.Value!, path);
        if (!contained.IsSuccess) return Forward<GitBlob>(contained);
        GitFact<string> commit = await ResolveRevisionAsync(root.Value!, revision, cancellationToken).ConfigureAwait(false);
        if (!commit.IsSuccess) return Forward<GitBlob>(commit);
        GitFact<GitCommandResult> tree = await RunAsync(root.Value!,
            ["ls-tree", "-z", "--full-tree", commit.Value!, "--", ":(literal)" + path], cancellationToken).ConfigureAwait(false);
        if (!tree.IsSuccess) return Forward<GitBlob>(tree);
        if (tree.Value!.ExitCode != 0)
            return GitFact<GitBlob>.Fail(GitFactStatus.Unavailable, "HISTORY_UNAVAILABLE", path + ": " + ErrorText(tree.Value.Stderr));
        GitTreeEntry entry;
        try
        {
            IReadOnlyList<GitTreeEntry> entries = ParseTree(root.Value!, tree.Value.Stdout, cancellationToken);
            if (entries.Count != 1 || entries[0].Path != path || entries[0].Type != "blob")
                return GitFact<GitBlob>.Fail(GitFactStatus.Invalid, "BLOB_INVALID", path);
            entry = entries[0];
        }
        catch (Exception exception) when (exception is DecoderFallbackException or FormatException)
        {
            return GitFact<GitBlob>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", exception.Message);
        }
        catch (OperationCanceledException)
        {
            return GitFact<GitBlob>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled blob tree parsing.");
        }
        GitFact<GitCommandResult> result = await RunAsync(root.Value!, ["cat-file", "blob", entry.ObjectId], cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Forward<GitBlob>(result);
        if (result.Value!.ExitCode != 0) return GitFact<GitBlob>.Fail(GitFactStatus.Unavailable, "HISTORY_UNAVAILABLE", path + ": " + ErrorText(result.Value.Stderr));
        byte[] bytes = result.Value.Stdout;
        using IncrementalHash objectHash = IncrementalHash.CreateHash(entry.ObjectId.Length == 40 ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256);
        objectHash.AppendData(Encoding.ASCII.GetBytes("blob " + bytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0"));
        objectHash.AppendData(bytes);
        if (!string.Equals(Convert.ToHexStringLower(objectHash.GetHashAndReset()), entry.ObjectId, StringComparison.Ordinal))
            return GitFact<GitBlob>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Historical blob bytes do not match the committed object ID.");
        return GitFact<GitBlob>.Ok(new(commit.Value!, path, bytes, Convert.ToHexStringLower(SHA256.HashData(bytes))));
    }

    private static GitFact<T> Forward<T>(GitFact<string> source) => GitFact<T>.Fail(source.Status, source.Code!, source.Message ?? string.Empty);
    private static GitFact<T> Forward<T>(GitFact<GitCommandResult> source) => GitFact<T>.Fail(source.Status, source.Code!, source.Message ?? string.Empty);
    private static GitFact<T> Forward<T>(GitFact<IReadOnlyList<GitTreeEntry>> source) => GitFact<T>.Fail(source.Status, source.Code!, source.Message ?? string.Empty);
    private async Task<GitFact<bool>> IsProvenShallowAncestorAsync(string root, string revision, CancellationToken cancellationToken)
    {
        int depth = 0;
        int parentNumber = 1;
        bool objectRevision = IsObjectId(revision.ToLowerInvariant());
        bool numberedParent = false;
        if (objectRevision) { }
        else if (revision.StartsWith("HEAD~", StringComparison.Ordinal))
        {
            string count = revision[5..];
            if (count.Length == 0) depth = 1;
            else if (count[0] == '0' || !count.All(char.IsAsciiDigit)
                || !int.TryParse(count, out depth) || depth == 0) return GitFact<bool>.Ok(false);
        }
        else if (revision.StartsWith("HEAD^", StringComparison.Ordinal))
        {
            string suffix = revision[5..];
            if (suffix.Length == 0) depth = 1;
            else if (suffix.All(character => character == '^')) depth = suffix.Length + 1;
            else if (suffix.Length > 0 && suffix[0] != '0' && suffix.All(char.IsAsciiDigit)
                && int.TryParse(suffix, out int parsedParent) && parsedParent > 0)
            {
                if (parsedParent == 1) depth = 1;
                else { parentNumber = parsedParent; numberedParent = true; }
            }
            else return GitFact<bool>.Ok(false);
        }
        else return GitFact<bool>.Ok(false);

        GitFact<GitCommandResult> chain = await RunAsync(root, ["rev-list", "--first-parent", "HEAD"], cancellationToken).ConfigureAwait(false);
        if (!chain.IsSuccess) return Forward<bool>(chain);
        if (chain.Value!.ExitCode != 0 || !TryDecode(chain.Value.Stdout, out string? chainText)
            || chainText is null || !chainText.EndsWith('\n'))
            return GitFact<bool>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed ancestor chain.");
        string[] commits = chainText[..^1].Split('\n');
        if (commits.Length == 0 || commits.Any(commit => !IsObjectId(commit) || commit.All(character => character == '0')))
            return GitFact<bool>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed ancestor chain.");
        if (!objectRevision && !numberedParent && depth < commits.Length) return GitFact<bool>.Ok(false);
        string boundaryId = numberedParent ? commits[0] : commits[^1];

        GitFact<GitCommandResult> location = await RunAsync(root, ["rev-parse", "--git-path", "shallow"], cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess) return Forward<bool>(location);
        if (location.Value!.ExitCode != 0 || !TryDecode(location.Value.Stdout, out string? locationText)
            || locationText is null || !locationText.EndsWith('\n') || locationText[..^1].Contains('\n'))
            return GitFact<bool>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed shallow path.");
        try
        {
            string shallowPath = Path.GetFullPath(locationText[..^1], root);
            using FileStream shallow = File.OpenRead(shallowPath);
            using MemoryStream buffer = new();
            byte[] chunk = new byte[8192];
            int count;
            while ((count = await shallow.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + count > OutputByteLimit)
                    return GitFact<bool>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_LIMIT_EXCEEDED", "The shallow boundary file exceeded 16 MiB.");
                buffer.Write(chunk, 0, count);
            }
            string[] boundaries = StrictUtf8.GetString(buffer.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (!boundaries.Contains(boundaryId, StringComparer.Ordinal)) return GitFact<bool>.Ok(false);
        }
        catch (OperationCanceledException)
        {
            return GitFact<bool>.Fail(GitFactStatus.Cancelled, "GIT_CANCELLED", "The caller cancelled the shallow boundary read.");
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            return GitFact<bool>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", exception.Message);
        }
        GitFact<GitCommandResult> boundary = await RunAsync(root, ["cat-file", "-p", boundaryId], cancellationToken).ConfigureAwait(false);
        if (!boundary.IsSuccess) return Forward<bool>(boundary);
        if (boundary.Value!.ExitCode != 0 || !TryDecode(boundary.Value.Stdout, out string? commitText) || commitText is null)
            return GitFact<bool>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed shallow commit.");
        List<string> parents = [];
        foreach (string line in commitText.Split('\n').TakeWhile(line => line.Length != 0))
        {
            if (!line.StartsWith("parent ", StringComparison.Ordinal)) continue;
            if (!IsObjectId(line[7..]) || line[7..].All(character => character == '0'))
                return GitFact<bool>.Fail(GitFactStatus.Invalid, "GIT_OUTPUT_INVALID", "Git returned a malformed parent header.");
            parents.Add(line[7..]);
        }
        if (objectRevision) return GitFact<bool>.Ok(parents.Contains(revision.ToLowerInvariant(), StringComparer.Ordinal));
        return GitFact<bool>.Ok(parents.Count >= parentNumber);
    }
    private static bool TryDecode(byte[] bytes, out string? value)
    {
        try { value = StrictUtf8.GetString(bytes); return true; }
        catch (DecoderFallbackException) { value = null; return false; }
    }
    private static string ErrorText(byte[] bytes) => Encoding.UTF8.GetString(bytes).TrimEnd();
    private static string[] DecodeTokens(byte[] bytes)
    {
        if (bytes.Length == 0) return [string.Empty];
        if (bytes[^1] != 0) throw new FormatException("NUL-terminated Git output was incomplete.");
        return StrictUtf8.GetString(bytes).Split('\0');
    }
    private static bool IsDiffMode(string value) => value is "000000" or "100644" or "100755" or "120000" or "160000";
    private static bool IsTreeMode(string mode, string type) => ((mode is "100644" or "100755" or "120000") && type == "blob")
        || (mode == "160000" && type == "commit");
    private static bool IsDiffStatus(string value) => value is "A" or "D" or "M" or "T"
        || (value.Length == 4 && value[0] == 'R' && value[1..].All(char.IsAsciiDigit)
            && int.Parse(value[1..], System.Globalization.CultureInfo.InvariantCulture) <= 100);
    private static bool IsDiffRelation(string[] fields)
    {
        bool sourceAbsent = fields[0] == "000000" && fields[2].All(character => character == '0');
        bool destinationAbsent = fields[1] == "000000" && fields[3].All(character => character == '0');
        if (fields[2].Length != fields[3].Length) return false;
        if ((fields[0] == "000000") != fields[2].All(character => character == '0')
            || (fields[1] == "000000") != fields[3].All(character => character == '0')) return false;
        int sourceType = DiffType(fields[0]);
        int destinationType = DiffType(fields[1]);
        return fields[4][0] switch
        {
            'A' => sourceAbsent && !destinationAbsent,
            'D' => !sourceAbsent && destinationAbsent,
            'M' => !sourceAbsent && !destinationAbsent && sourceType == destinationType
                && (fields[0] != fields[1] || fields[2] != fields[3]),
            'T' => !sourceAbsent && !destinationAbsent && sourceType != destinationType,
            'R' => !sourceAbsent && !destinationAbsent && sourceType == destinationType
                && int.Parse(fields[4][1..], System.Globalization.CultureInfo.InvariantCulture) >= 50
                && (fields[2] != fields[3] || fields[4] == "R100"),
            _ => false,
        };
    }
    private static int DiffType(string mode) => mode switch
    {
        "100644" or "100755" => 1,
        "120000" => 2,
        "160000" => 3,
        _ => 0,
    };
    private static bool IsObjectId(string value) => (value.Length == 40 || value.Length == 64)
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
