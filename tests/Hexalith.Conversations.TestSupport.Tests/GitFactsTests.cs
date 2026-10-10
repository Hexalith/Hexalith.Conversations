using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

public sealed class GitFactsTests
{
    private static string Python => OperatingSystem.IsWindows() ? "python" : "python3";

    [Fact]
    public async Task RunnerShouldDrainStdoutAndStderrConcurrently()
    {
        const string script = "import sys,threading\na=b'x'*262144\nt=threading.Thread(target=lambda:(sys.stderr.buffer.write(a),sys.stderr.buffer.flush()))\nt.start()\nsys.stdout.buffer.write(a)\nsys.stdout.buffer.flush()\nt.join()";
        GitFact<GitCommandResult> result = await PythonFacts(script).RunAsync(Path.GetTempPath(), [], TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.Message);
        result.Value!.ExitCode.ShouldBe(0);
        result.Value.Stdout.Length.ShouldBe(262144);
        result.Value.Stderr.Length.ShouldBe(262144);
        GitFact<GitCommandResult> overflow = await PythonFacts("import sys\nsys.stdout.buffer.write(b'x'*(17*1024*1024))\nsys.stdout.flush()").RunAsync(
            Path.GetTempPath(), [], TestContext.Current.CancellationToken);
        overflow.Status.ShouldBe(GitFactStatus.Invalid);
        overflow.Code.ShouldBe("GIT_OUTPUT_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task RunnerShouldBoundAndCancelExecution()
    {
        typeof(GitFacts).GetField("CommandLimit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null).ShouldBe(TimeSpan.FromSeconds(30));
        string pidFile = Path.Combine(Path.GetTempPath(), "gitfacts-pid-" + Guid.NewGuid().ToString("N"));
        string child = "import time\ntime.sleep(120)";
        string script = "import os,subprocess,sys\np=subprocess.Popen([sys.executable,'-c'," + JsonSerializer.Serialize(child)
            + "])\nopen(" + JsonSerializer.Serialize(pidFile) + ",'w').write(str(os.getpid())+','+str(p.pid))";
        try
        {
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            Task<GitFact<GitCommandResult>> running = PythonFacts(script).RunAsync(Path.GetTempPath(), [], cancellation.Token);
            await WaitForFileAsync(pidFile);
            int[] started = ReadProcessIds(pidFile);
            await WaitForExitAsync(started[0]);
            cancellation.Cancel();
            GitFact<GitCommandResult> cancelled = await running;
            cancelled.Status.ShouldBe(GitFactStatus.Cancelled);
            cancelled.Value.ShouldBeNull();
            await WaitForExitAsync(started[1]);
            File.Delete(pidFile);
            Task<GitFact<GitCommandResult>> timingOut = PythonFacts(script).RunAsync(
                Path.GetTempPath(), [], TestContext.Current.CancellationToken, TimeSpan.FromSeconds(2));
            await WaitForFileAsync(pidFile);
            int[] timedOutStarted = ReadProcessIds(pidFile);
            await WaitForExitAsync(timedOutStarted[0]);
            GitFact<GitCommandResult> timedOut = await timingOut;
            timedOut.Status.ShouldBe(GitFactStatus.Timeout);
            await WaitForExitAsync(timedOutStarted[1]);
        }
        finally { File.Delete(pidFile); }
    }

    [Fact]
    public async Task RunnerShouldUseHardenedEncodingAndRawFacts()
    {
        using GitFixture fixture = new();
        const string unicode = "évidence.txt";
        byte[] bytes = [0, 1, 2, 255, 13, 10];
        File.WriteAllText(Path.Combine(fixture.Root, "seed.txt"), "seed");
        string before = fixture.Commit();
        File.WriteAllBytes(Path.Combine(fixture.Root, unicode), bytes);
        string? backslash = OperatingSystem.IsWindows() ? null : "literal\\backslash.txt";
        if (backslash is not null) File.WriteAllBytes(Path.Combine(fixture.Root, backslash), bytes);
        string after = fixture.Commit();
        Dictionary<string, string?> ambient = new()
        {
            ["GIT_DIR"] = Environment.GetEnvironmentVariable("GIT_DIR"),
            ["GIT_CONFIG_COUNT"] = Environment.GetEnvironmentVariable("GIT_CONFIG_COUNT"),
            ["GIT_CONFIG_KEY_0"] = Environment.GetEnvironmentVariable("GIT_CONFIG_KEY_0"),
            ["GIT_CONFIG_VALUE_0"] = Environment.GetEnvironmentVariable("GIT_CONFIG_VALUE_0"),
            ["GIT_CONFIG_GLOBAL"] = Environment.GetEnvironmentVariable("GIT_CONFIG_GLOBAL"),
        };
        try
        {
            Environment.SetEnvironmentVariable("GIT_DIR", Path.Combine(Path.GetTempPath(), "wrong-repository"));
            Environment.SetEnvironmentVariable("GIT_CONFIG_COUNT", "1");
            Environment.SetEnvironmentVariable("GIT_CONFIG_KEY_0", "core.quotePath");
            Environment.SetEnvironmentVariable("GIT_CONFIG_VALUE_0", "true");
            Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", Path.Combine(Path.GetTempPath(), "missing-git-config"));
            GitFacts facts = new();
            (await facts.ResolveRevisionAsync(fixture.Root, after, TestContext.Current.CancellationToken)).Value.ShouldBe(after);
            GitFact<IReadOnlyList<GitDiffEntry>> diff = await facts.ReadDiffAsync(fixture.Root, before, after, TestContext.Current.CancellationToken);
            diff.IsSuccess.ShouldBeTrue(diff.Message);
            GitDiffEntry entry = diff.Value!.Single(item => item.Path == unicode);
            entry.SourceMode.ShouldBe("000000");
            entry.DestinationMode.ShouldBe("100644");
            if (backslash is not null) diff.Value!.Single(item => item.Path == backslash).DestinationMode.ShouldBe("100644");
            GitFact<IReadOnlyList<GitTreeEntry>> tree = await facts.ReadTreeAsync(fixture.Root, after, TestContext.Current.CancellationToken);
            tree.IsSuccess.ShouldBeTrue(tree.Message);
            GitTreeEntry historical = tree.Value!.Single(item => item.Path == unicode);
            historical.Mode.ShouldBe("100644");
            string replacementPath = Path.Combine(fixture.Root, "replacement.txt");
            File.WriteAllText(replacementPath, "replacement", Encoding.UTF8);
            string replacementId = fixture.Git("hash-object", "-w", replacementPath);
            fixture.Git("replace", historical.ObjectId, replacementId);
            GitFact<GitBlob> blob = await facts.ReadBlobAsync(fixture.Root, after, unicode, TestContext.Current.CancellationToken);
            blob.IsSuccess.ShouldBeTrue(blob.Message);
            blob.Value!.Content.ShouldBe(bytes);
            blob.Value.Sha256.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(bytes)));
            if (!OperatingSystem.IsWindows())
            {
                File.Delete(Path.Combine(fixture.Root, unicode));
                File.CreateSymbolicLink(Path.Combine(fixture.Root, unicode), replacementPath);
                GitFact<GitBlob> historicalBlob = await facts.ReadBlobAsync(fixture.Root, after, unicode, TestContext.Current.CancellationToken);
                historicalBlob.IsSuccess.ShouldBeTrue(historicalBlob.Message);
                historicalBlob.Value!.Content.ShouldBe(bytes);
            }
            string rootLiteral = JsonSerializer.Serialize(fixture.Root);
            string fake = "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                + "(b'a'*40+b'\\n' if '--verify' in a else "
                + "(b'000000 100644 '+b'0'*40+b' '+b'1'*40+b' A\\0bad\\0' if 'diff' in a else b'')))";
            GitFact<IReadOnlyList<GitDiffEntry>> malformed = await PythonFacts(fake).ReadDiffAsync(
                fixture.Root, before, after, TestContext.Current.CancellationToken);
            malformed.Status.ShouldBe(GitFactStatus.Invalid);
            malformed.Code.ShouldBe("GIT_OUTPUT_INVALID");
            string duplicateTree = "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                + "(b'a'*40+b'\\n' if '--verify' in a else "
                + "(b'100644 blob '+b'1'*40+b'\\tpath\\0')*2 if 'ls-tree' in a else b''))";
            GitFact<IReadOnlyList<GitTreeEntry>> duplicate = await PythonFacts(duplicateTree).ReadTreeAsync(
                fixture.Root, after, TestContext.Current.CancellationToken);
            duplicate.Status.ShouldBe(GitFactStatus.Invalid);
            string malformedUtf8 = "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                + "(b'a'*40+b'\\n' if '--verify' in a else "
                + "(b':000000 100644 '+b'0'*40+b' '+b'1'*40+b' A\\0bad\\xff\\0' if 'diff' in a else b'')))";
            GitFact<IReadOnlyList<GitDiffEntry>> invalidEncoding = await PythonFacts(malformedUtf8).ReadDiffAsync(
                fixture.Root, before, after, TestContext.Current.CancellationToken);
            invalidEncoding.Status.ShouldBe(GitFactStatus.Invalid);
            string malformedRevision = "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else b' '+b'a'*40+b'\\n')";
            (await PythonFacts(malformedRevision).ResolveRevisionAsync(
                fixture.Root, after, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            foreach (string raw in new[]
            {
                "b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' R999\\0old\\0new\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' A\\0path\\0'",
            })
            {
                string malformedDiff = "import sys\na=sys.argv[1:]\n"
                    + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                    + "(b'a'*40+b'\\n' if '--verify' in a else (" + raw + " if 'diff' in a else b'')))";
                (await PythonFacts(malformedDiff).ReadDiffAsync(
                    fixture.Root, before, after, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            }
            string scoredRename = "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                + "(b'a'*40+b'\\n' if '--verify' in a else "
                + "(b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' R75\\0old\\0new\\0' if 'diff' in a else b'')))";
            GitFact<IReadOnlyList<GitDiffEntry>> validScore = await PythonFacts(scoredRename).ReadDiffAsync(
                fixture.Root, before, after, TestContext.Current.CancellationToken);
            validScore.IsSuccess.ShouldBeTrue(validScore.Message);
            GitDiffEntry scoredEntry = validScore.Value!.Single();
            scoredEntry.Status.ShouldBe("R75");
            scoredEntry.SourcePath.ShouldBe("old");
            scoredEntry.Path.ShouldBe("new");
            string renameSource = Path.Combine(fixture.Root, "rename-source.txt");
            File.WriteAllText(renameSource, "content to rename");
            string renameBefore = fixture.Commit();
            File.Move(renameSource, Path.Combine(fixture.Root, "rename-destination.txt"));
            string renameAfter = fixture.Commit();
            fixture.Git("config", "diff.renames", "false");
            GitFact<IReadOnlyList<GitDiffEntry>> renamed = await facts.ReadDiffAsync(
                fixture.Root, renameBefore, renameAfter, TestContext.Current.CancellationToken);
            renamed.IsSuccess.ShouldBeTrue(renamed.Message);
            renamed.Value!.Single().Status.ShouldBe("R100");
        }
        finally
        {
            foreach (KeyValuePair<string, string?> variable in ambient)
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
    }

    [Fact]
    public async Task UnavailableHistoryShouldBeTypedNotSuccessful()
    {
        using GitFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.Root, "seed.txt"), "seed");
        string first = fixture.Commit();
        File.WriteAllText(Path.Combine(fixture.Root, "seed.txt"), "changed");
        fixture.Commit();
        GitFacts facts = new();
        (await facts.ResolveRevisionAsync(fixture.Root, "missing-revision", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
        (await facts.ResolveRepositoryAsync(Path.Combine(fixture.Root, "absent"), TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
        Directory.CreateDirectory(Path.Combine(fixture.Root, "nested"));
        (await facts.ResolveRepositoryAsync(Path.Combine(fixture.Root, "nested"), TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
        RepositoryLocator.ResolveContainedPath(fixture.Root, "../escape").Code.ShouldBe("REPOSITORY_PATH_ESCAPE");
        RepositoryLocator.ResolveContainedPath(fixture.Root, "bad\0path").Status.ShouldBe(GitFactStatus.Invalid);
        (await new GitFacts(Path.Combine(fixture.Root, "missing-git")).ResolveRepositoryAsync(fixture.Root, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
        (await facts.ReadBlobAsync(fixture.Root, "HEAD", "missing", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
        if (!OperatingSystem.IsWindows())
        {
            string outside = Path.Combine(Path.GetTempPath(), "gitfacts-outside-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outside);
            string link = Path.Combine(fixture.Root, "escape");
            Directory.CreateSymbolicLink(link, outside);
            RepositoryLocator.ResolveContainedPath(fixture.Root, "escape/file").Code.ShouldBe("REPOSITORY_PATH_ESCAPE");
            string rootLink = Path.Combine(Path.GetTempPath(), "gitfacts-root-" + Guid.NewGuid().ToString("N"));
            Directory.CreateSymbolicLink(rootLink, fixture.Root);
            RepositoryLocator.ResolveContainedPath(rootLink, "seed.txt").Code.ShouldBe("REPOSITORY_PATH_ESCAPE");
            File.Delete(rootLink);
            string ancestorLink = Path.Combine(Path.GetTempPath(), "gitfacts-ancestor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateSymbolicLink(ancestorLink, Path.GetDirectoryName(fixture.Root)!);
            RepositoryLocator.ResolveContainedPath(Path.Combine(ancestorLink, Path.GetFileName(fixture.Root)), "seed.txt")
                .Code.ShouldBe("REPOSITORY_PATH_ESCAPE");
            File.Delete(ancestorLink);
            Directory.Delete(outside);
        }
        string shallowPath = Path.Combine(Path.GetTempPath(), "gitfacts-shallow-" + Guid.NewGuid().ToString("N"));
        string partialPath = Path.Combine(Path.GetTempPath(), "gitfacts-partial-" + Guid.NewGuid().ToString("N"));
        string sourceUri = new Uri(fixture.Root + Path.DirectorySeparatorChar).AbsoluteUri;
        try
        {
            GitFact<GitCommandResult> shallowClone = await facts.RunAsync(Path.GetTempPath(),
                ["clone", "--quiet", "--depth", "1", sourceUri, shallowPath], TestContext.Current.CancellationToken);
            shallowClone.Value!.ExitCode.ShouldBe(0);
            (await facts.ResolveRevisionAsync(shallowPath, first, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
            (await facts.ResolveRevisionAsync(shallowPath, "HEAD~1", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
            (await facts.ResolveRevisionAsync(shallowPath, "misspelled", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            (await facts.ResolveRevisionAsync(shallowPath, "HEAD~bogus", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            (await facts.ResolveRevisionAsync(shallowPath, new string('f', 40), TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            fixture.Git("config", "uploadpack.allowFilter", "true");
            GitFact<GitCommandResult> partialClone = await facts.RunAsync(Path.GetTempPath(),
                ["clone", "--quiet", "--no-checkout", "--filter=blob:none", "--no-local", sourceUri, partialPath], TestContext.Current.CancellationToken);
            partialClone.Value!.ExitCode.ShouldBe(0);
            GitFact<GitBlob> partial = await facts.ReadBlobAsync(partialPath, "HEAD", "seed.txt", TestContext.Current.CancellationToken);
            partial.IsSuccess.ShouldBeFalse();
            partial.Status.ShouldBe(GitFactStatus.Unavailable);

            string probeFile = Path.Combine(Path.GetTempPath(), "gitfacts-probe-" + Guid.NewGuid().ToString("N"));
            try
            {
                string rootLiteral = JsonSerializer.Serialize(fixture.Root);
                string fake = "import sys,time\na=sys.argv[1:]\n"
                    + "if '--show-toplevel' in a: print(" + rootLiteral + ")\n"
                    + "elif '--verify' in a: sys.exit(1)\n"
                    + "elif '--is-shallow-repository' in a: open(" + JsonSerializer.Serialize(probeFile)
                    + ",'w').close(); time.sleep(120)";
                using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                Task<GitFact<string>> probing = PythonFacts(fake).ResolveRevisionAsync(fixture.Root, "missing", cancellation.Token);
                await WaitForFileAsync(probeFile);
                cancellation.Cancel();
                (await probing).Status.ShouldBe(GitFactStatus.Cancelled);
            }
            finally { File.Delete(probeFile); }
        }
        finally
        {
            if (Directory.Exists(shallowPath)) Directory.Delete(shallowPath, recursive: true);
            if (Directory.Exists(partialPath)) Directory.Delete(partialPath, recursive: true);
        }
    }

    private static GitFacts PythonFacts(string source) => new(Python, ["-c", source], includeGitPreamble: false);

    private static int[] ReadProcessIds(string path) => File.ReadAllText(path).Split(',')
        .Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

    private static async Task WaitForFileAsync(string path)
    {
        for (int attempt = 0; attempt < 100 && !File.Exists(path); attempt++)
            await Task.Delay(50, TestContext.Current.CancellationToken);
        File.Exists(path).ShouldBeTrue("the descendant must have started before cancellation");
    }

    private static async Task WaitForExitAsync(int pid)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                if (process.HasExited) return;
            }
            catch (ArgumentException) { return; }
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        throw new ShouldAssertException("a started descendant survived termination");
    }
}
