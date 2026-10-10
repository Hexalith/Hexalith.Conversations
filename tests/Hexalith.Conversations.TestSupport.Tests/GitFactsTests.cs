using System.Security.Cryptography;
using System.Text;
using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

public sealed class GitFactsTests
{
    [Fact]
    public async Task RunnerShouldDrainStdoutAndStderrConcurrently()
    {
        string script = CreateScript("import sys, threading\na=b'x'*262144\nt=threading.Thread(target=lambda: (sys.stderr.buffer.write(a),sys.stderr.buffer.flush()))\nt.start()\nsys.stdout.buffer.write(a)\nsys.stdout.buffer.flush()\nt.join()\n");
        try
        {
            GitFacts facts = new(script);
            GitFact<GitCommandResult> result = await facts.RunAsync(Path.GetTempPath(), ["ignored"], TestContext.Current.CancellationToken);
            result.IsSuccess.ShouldBeTrue(result.Message);
            result.Value!.ExitCode.ShouldBe(0);
            result.Value.Stdout.Length.ShouldBe(262144);
            result.Value.Stderr.Length.ShouldBe(262144);
        }
        finally { File.Delete(script); }
    }

    [Fact]
    public async Task RunnerShouldBoundAndCancelExecution()
    {
        typeof(GitFacts).GetField("CommandLimit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null).ShouldBe(TimeSpan.FromSeconds(30));
        string marker = Path.Combine(Path.GetTempPath(), "gitfacts-child-" + Guid.NewGuid().ToString("N"));
        string child = "import time; time.sleep(1); open(" + System.Text.Json.JsonSerializer.Serialize(marker) + ", 'w').write('alive')";
        string script = CreateScript("import subprocess, sys, time\nsubprocess.Popen([sys.executable, '-c', "
            + System.Text.Json.JsonSerializer.Serialize(child) + "])\ntime.sleep(120)\n");
        try
        {
            GitFacts facts = new(script);
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellation.CancelAfter(TimeSpan.FromMilliseconds(150));
            GitFact<GitCommandResult> cancelled = await facts.RunAsync(Path.GetTempPath(), ["ignored"], cancellation.Token);
            cancelled.Status.ShouldBe(GitFactStatus.Cancelled);
            cancelled.Value.ShouldBeNull();
            GitFact<GitCommandResult> timedOut = await facts.RunAsync(Path.GetTempPath(), ["ignored"], TestContext.Current.CancellationToken, TimeSpan.FromMilliseconds(150));
            timedOut.Status.ShouldBe(GitFactStatus.Timeout);
            timedOut.Value.ShouldBeNull();
            await Task.Delay(TimeSpan.FromMilliseconds(1200), TestContext.Current.CancellationToken);
            File.Exists(marker).ShouldBeFalse("a child process escaped process-tree termination");
        }
        finally { File.Delete(script); File.Delete(marker); }
    }

    [Fact]
    public async Task RunnerShouldUseHardenedEncodingAndRawFacts()
    {
        using GitFixture fixture = new();
        const string unusual = "évidence\\literal.txt";
        byte[] bytes = [0, 1, 2, 255, 13, 10];
        File.WriteAllText(Path.Combine(fixture.Root, "seed.txt"), "seed");
        string before = fixture.Commit();
        File.WriteAllBytes(Path.Combine(fixture.Root, unusual), bytes);
        string after = fixture.Commit();
        string? priorGitDir = Environment.GetEnvironmentVariable("GIT_DIR");
        string? priorCount = Environment.GetEnvironmentVariable("GIT_CONFIG_COUNT");
        string? priorGlobal = Environment.GetEnvironmentVariable("GIT_CONFIG_GLOBAL");
        string? priorKey0 = Environment.GetEnvironmentVariable("GIT_CONFIG_KEY_0");
        string? priorValue0 = Environment.GetEnvironmentVariable("GIT_CONFIG_VALUE_0");
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
            GitDiffEntry entry = diff.Value!.Single();
            entry.Path.ShouldBe(unusual);
            entry.SourceMode.ShouldBe("000000");
            entry.DestinationMode.ShouldBe("100644");
            GitFact<IReadOnlyList<GitTreeEntry>> tree = await facts.ReadTreeAsync(fixture.Root, after, TestContext.Current.CancellationToken);
            tree.IsSuccess.ShouldBeTrue(tree.Message);
            tree.Value!.Single(item => item.Path == unusual).Mode.ShouldBe("100644");
            GitFact<GitBlob> blob = await facts.ReadBlobAsync(fixture.Root, after, unusual, TestContext.Current.CancellationToken);
            blob.IsSuccess.ShouldBeTrue(blob.Message);
            blob.Value!.Content.ShouldBe(bytes);
            blob.Value.Sha256.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(bytes)));
            string malformedScript = CreateScript("import sys\na=sys.argv\n"
                + "sys.stdout.buffer.write((a[2]+'\\n').encode() if '--show-toplevel' in a else "
                + "(b'a'*40+b'\\n' if 'rev-parse' in a else b':000000 100644 '+b'0'*40+b' '+b'1'*40+b' A\\0bad\\xff\\0'))\n");
            try
            {
                GitFact<IReadOnlyList<GitDiffEntry>> malformed = await new GitFacts(malformedScript).ReadDiffAsync(
                    fixture.Root, before, after, TestContext.Current.CancellationToken);
                malformed.Status.ShouldBe(GitFactStatus.Invalid);
                malformed.Code.ShouldBe("GIT_OUTPUT_INVALID");
            }
            finally { File.Delete(malformedScript); }
        }
        finally
        {
            Environment.SetEnvironmentVariable("GIT_DIR", priorGitDir);
            Environment.SetEnvironmentVariable("GIT_CONFIG_COUNT", priorCount);
            Environment.SetEnvironmentVariable("GIT_CONFIG_KEY_0", priorKey0);
            Environment.SetEnvironmentVariable("GIT_CONFIG_VALUE_0", priorValue0);
            Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", priorGlobal);
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
        GitFacts missingGit = new(Path.Combine(fixture.Root, "missing-git"));
        (await missingGit.ResolveRepositoryAsync(fixture.Root, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
        fixture.Git("config", "extensions.worktreeConfig", "false");
        (await facts.ReadBlobAsync(fixture.Root, "HEAD", "missing", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
        string shallowPath = Path.Combine(Path.GetTempPath(), "gitfacts-shallow-" + Guid.NewGuid().ToString("N"));
        string partialPath = Path.Combine(Path.GetTempPath(), "gitfacts-partial-" + Guid.NewGuid().ToString("N"));
        try
        {
            GitFact<GitCommandResult> shallowClone = await facts.RunAsync(Path.GetTempPath(),
                ["clone", "--quiet", "--depth", "1", "file://" + fixture.Root, shallowPath], TestContext.Current.CancellationToken);
            shallowClone.IsSuccess.ShouldBeTrue(shallowClone.Message);
            shallowClone.Value!.ExitCode.ShouldBe(0);
            (await facts.ResolveRevisionAsync(shallowPath, first, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);

            fixture.Git("config", "uploadpack.allowFilter", "true");
            GitFact<GitCommandResult> partialClone = await facts.RunAsync(Path.GetTempPath(),
                ["clone", "--quiet", "--no-checkout", "--filter=blob:none", "--no-local", "file://" + fixture.Root, partialPath], TestContext.Current.CancellationToken);
            partialClone.IsSuccess.ShouldBeTrue(partialClone.Message);
            partialClone.Value!.ExitCode.ShouldBe(0);
            GitFact<GitCommandResult> removeRemote = await facts.RunAsync(partialPath,
                ["remote", "remove", "origin"], TestContext.Current.CancellationToken);
            removeRemote.Value!.ExitCode.ShouldBe(0);
            GitFact<GitBlob> partial = await facts.ReadBlobAsync(partialPath, "HEAD", "seed.txt", TestContext.Current.CancellationToken);
            partial.IsSuccess.ShouldBeFalse();
            partial.Status.ShouldBe(GitFactStatus.Unavailable);
        }
        finally
        {
            if (Directory.Exists(shallowPath)) Directory.Delete(shallowPath, recursive: true);
            if (Directory.Exists(partialPath)) Directory.Delete(partialPath, recursive: true);
        }
    }

    private static string CreateScript(string body)
    {
        string path = Path.Combine(Path.GetTempPath(), "gitfacts-script-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "#!/usr/bin/env python3\n" + body, new UTF8Encoding(false));
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        return path;
    }
}
