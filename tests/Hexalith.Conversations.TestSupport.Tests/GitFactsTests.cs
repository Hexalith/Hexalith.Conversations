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
        const string script = "import sys,threading\na=b'out\\x00'*65536\nb=b'err\\xff'*65536\nt=threading.Thread(target=lambda:(sys.stderr.buffer.write(b),sys.stderr.buffer.flush()))\nt.start()\nsys.stdout.buffer.write(a)\nsys.stdout.buffer.flush()\nt.join()";
        GitFact<GitCommandResult> result = await PythonFacts(script).RunAsync(Path.GetTempPath(), [], TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.Message);
        result.Value!.ExitCode.ShouldBe(0);
        byte[] expectedOut = Enumerable.Range(0, 65536).SelectMany(_ => new byte[] { 111, 117, 116, 0 }).ToArray();
        byte[] expectedError = Enumerable.Range(0, 65536).SelectMany(_ => new byte[] { 101, 114, 114, 255 }).ToArray();
        result.Value.Stdout.SequenceEqual(expectedOut).ShouldBeTrue();
        result.Value.Stderr.SequenceEqual(expectedError).ShouldBeTrue();
        GitFact<GitCommandResult> overflow = await PythonFacts("import sys\nsys.stdout.buffer.write(b'x'*(17*1024*1024))\nsys.stdout.flush()").RunAsync(
            Path.GetTempPath(), [], TestContext.Current.CancellationToken);
        overflow.Status.ShouldBe(GitFactStatus.Invalid);
        overflow.Code.ShouldBe("GIT_OUTPUT_LIMIT_EXCEEDED");
        string lateWriter = "import os,subprocess,sys\nsubprocess.Popen([sys.executable,'-c',"
            + JsonSerializer.Serialize("import sys\nsys.stdout.buffer.write(b'x'*(17*1024*1024))\nsys.stdout.flush()")
            + "],stderr=subprocess.DEVNULL)";
        GitFact<GitCommandResult> afterParentExit = await PythonFacts(lateWriter).RunAsync(
            Path.GetTempPath(), [], TestContext.Current.CancellationToken);
        afterParentExit.Status.ShouldBe(GitFactStatus.Invalid);
        afterParentExit.Code.ShouldBe("GIT_OUTPUT_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task RunnerShouldConsumeOneShotArgumentsOnce()
    {
        Queue<string> pending = new(["first", "second"]);
        IEnumerable<string> OneShot()
        {
            while (pending.TryDequeue(out string? argument))
                yield return argument;
        }

        GitFact<GitCommandResult> result = await PythonFacts("import sys\nsys.stdout.write('|'.join(sys.argv[1:]))")
            .RunAsync(Path.GetTempPath(), OneShot(), TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.Message);
        result.Value!.ExitCode.ShouldBe(0);
        result.Value.Stdout.ShouldBe(Encoding.UTF8.GetBytes("first|second"));
        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunnerShouldTerminateDescendantAfterSuccessfulParentExit()
    {
        string pidFile = Path.Combine(Path.GetTempPath(), "gitfacts-success-child-" + Guid.NewGuid().ToString("N"));
        string child = "import time\ntime.sleep(120)";
        string script = "import os,subprocess,sys\np=subprocess.Popen([sys.executable,'-c'," + JsonSerializer.Serialize(child)
            + "],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)\nopen(" + JsonSerializer.Serialize(pidFile)
            + ",'w').write(str(p.pid))";
        try
        {
            GitFact<GitCommandResult> result = await PythonFacts(script).RunAsync(
                Path.GetTempPath(), [], TestContext.Current.CancellationToken);
            result.IsSuccess.ShouldBeTrue(result.Message);
            result.Value!.ExitCode.ShouldBe(0);
            await WaitForFileAsync(pidFile);
            await WaitForExitAsync(ReadProcessIds(pidFile)[0]);
        }
        finally { File.Delete(pidFile); }
    }

    [Fact]
    public async Task RunnerShouldTerminateOrphanedGrandchildAfterSuccessfulParentExit()
    {
        string pidFile = Path.Combine(Path.GetTempPath(), "gitfacts-grandchild-" + Guid.NewGuid().ToString("N"));
        string grandchild = "import time\ntime.sleep(120)";
        string child = "import subprocess,sys\np=subprocess.Popen([sys.executable,'-c',"
            + JsonSerializer.Serialize(grandchild)
            + "],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)\nopen("
            + JsonSerializer.Serialize(pidFile) + ",'w').write(str(p.pid))";
        string parent = "import os,subprocess,sys,time\nsubprocess.Popen([sys.executable,'-c',"
            + JsonSerializer.Serialize(child)
            + "],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)\nfor _ in range(100):\n"
            + " if os.path.exists(" + JsonSerializer.Serialize(pidFile) + "): break\n time.sleep(.01)";
        try
        {
            GitFact<GitCommandResult> result = await PythonFacts(parent).RunAsync(
                Path.GetTempPath(), [], TestContext.Current.CancellationToken);
            result.IsSuccess.ShouldBeTrue(result.Message);
            result.Value!.ExitCode.ShouldBe(0);
            await WaitForFileAsync(pidFile);
            await WaitForExitAsync(ReadProcessIds(pidFile)[0]);
        }
        finally { File.Delete(pidFile); }
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
            GitFact<GitCommandResult> defaultTimedOut = await PythonFacts("import time\ntime.sleep(120)")
                .RunAsync(Path.GetTempPath(), [], TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken);
            defaultTimedOut.Status.ShouldBe(GitFactStatus.Timeout);
            defaultTimedOut.Code.ShouldBe("GIT_TIMEOUT");
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
        Directory.CreateDirectory(Path.Combine(fixture.Root, "nested"));
        File.WriteAllBytes(Path.Combine(fixture.Root, "nested", "path.txt"), bytes);
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
            ["GIT_OBJECT_DIRECTORY"] = Environment.GetEnvironmentVariable("GIT_OBJECT_DIRECTORY"),
            ["GIT_GRAFT_FILE"] = Environment.GetEnvironmentVariable("GIT_GRAFT_FILE"),
        };
        try
        {
            Environment.SetEnvironmentVariable("GIT_DIR", Path.Combine(Path.GetTempPath(), "wrong-repository"));
            Environment.SetEnvironmentVariable("GIT_CONFIG_COUNT", "1");
            Environment.SetEnvironmentVariable("GIT_CONFIG_KEY_0", "core.quotePath");
            Environment.SetEnvironmentVariable("GIT_CONFIG_VALUE_0", "true");
            Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", Path.Combine(Path.GetTempPath(), "missing-git-config"));
            Environment.SetEnvironmentVariable("GIT_OBJECT_DIRECTORY", Path.Combine(Path.GetTempPath(), "wrong-object-directory"));
            Environment.SetEnvironmentVariable("GIT_GRAFT_FILE", Path.Combine(Path.GetTempPath(), "wrong-graft-file"));
            GitFacts facts = new();
            string envScript = "import json,os,sys\nkeys=['GIT_DIR','GIT_OBJECT_DIRECTORY','GIT_CONFIG_COUNT','GIT_CONFIG_KEY_0','GIT_CONFIG_VALUE_0','GIT_CONFIG_GLOBAL','GIT_CONFIG_NOSYSTEM','GIT_TERMINAL_PROMPT','GIT_NO_REPLACE_OBJECTS','GIT_NO_LAZY_FETCH','GIT_GRAFT_FILE']\nprint(json.dumps({k:os.environ.get(k) for k in keys}))";
            GitFact<GitCommandResult> isolated = await PythonFacts(envScript).RunAsync(
                fixture.Root, [], TestContext.Current.CancellationToken);
            isolated.IsSuccess.ShouldBeTrue(isolated.Message);
            using (JsonDocument environment = JsonDocument.Parse(isolated.Value!.Stdout))
            {
                JsonElement values = environment.RootElement;
                values.GetProperty("GIT_DIR").ValueKind.ShouldBe(JsonValueKind.Null);
                values.GetProperty("GIT_OBJECT_DIRECTORY").ValueKind.ShouldBe(JsonValueKind.Null);
                values.GetProperty("GIT_CONFIG_KEY_0").ValueKind.ShouldBe(JsonValueKind.Null);
                values.GetProperty("GIT_CONFIG_VALUE_0").ValueKind.ShouldBe(JsonValueKind.Null);
                values.GetProperty("GIT_CONFIG_COUNT").GetString().ShouldBe("0");
                values.GetProperty("GIT_CONFIG_NOSYSTEM").GetString().ShouldBe("1");
                values.GetProperty("GIT_TERMINAL_PROMPT").GetString().ShouldBe("0");
                values.GetProperty("GIT_NO_REPLACE_OBJECTS").GetString().ShouldBe("1");
                values.GetProperty("GIT_NO_LAZY_FETCH").GetString().ShouldBe("1");
                values.GetProperty("GIT_GRAFT_FILE").GetString().ShouldBe(OperatingSystem.IsWindows() ? "NUL" : "/dev/null");
                values.GetProperty("GIT_CONFIG_GLOBAL").GetString().ShouldBe(OperatingSystem.IsWindows() ? "NUL" : "/dev/null");
            }
            (await facts.ResolveRevisionAsync(fixture.Root, after, TestContext.Current.CancellationToken)).Value.ShouldBe(after);
            File.WriteAllText(Path.Combine(fixture.Root, ".git", "info", "grafts"), after + "\n");
            (await facts.ResolveRevisionAsync(fixture.Root, "HEAD~1", TestContext.Current.CancellationToken)).Value.ShouldBe(before);
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
            tree.Value!.Single(item => item.Path == "nested/path.txt").Mode.ShouldBe("100644");
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
            string zeroRevision = "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else b'0'*40+b'\\n')";
            (await PythonFacts(zeroRevision).ResolveRevisionAsync(
                fixture.Root, after, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            foreach (string raw in new[]
            {
                "b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' R999\\0old\\0new\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' A\\0path\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'a'*40+b' M\\0path\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' R49\\0old\\0new\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'a'*40+b' R075\\0old\\0new\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' R75\\0old\\0new\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' M075\\0old\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'a'*40+b' R100\\0same\\0same\\0'",
                "b':100644 100644 '+b'a'*40+b' '+b'a'*40+b' C100\\0old\\0copy\\0'",
                "b':000000 100644 '+b'0'*40+b' '+b'a'*40+b' A\\0../escape\\0'",
                "b':000000 100644 '+b'0'*40+b' '+b'a'*40+b' A\\0/absolute\\0'",
                "b':000000 100644 '+b'0'*40+b' '+b'a'*40+b' A\\0empty//segment\\0'",
                "(b':000000 100644 '+b'0'*40+b' '+b'a'*40+b' A\\0duplicate\\0')*2",
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
                + "(b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' R075\\0old\\0new\\0' if 'diff' in a else b'')))";
            GitFact<IReadOnlyList<GitDiffEntry>> validScore = await PythonFacts(scoredRename).ReadDiffAsync(
                fixture.Root, before, after, TestContext.Current.CancellationToken);
            validScore.IsSuccess.ShouldBeTrue(validScore.Message);
            GitDiffEntry scoredEntry = validScore.Value!.Single();
            scoredEntry.Status.ShouldBe("R075");
            string exactRename = "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                + "(b'a'*40+b'\\n' if '--verify' in a else "
                + "(b':100644 100644 '+b'a'*40+b' '+b'b'*40+b' R100\\0old\\0new\\0' if 'diff' in a else b'')))";
            (await PythonFacts(exactRename).ReadDiffAsync(
                fixture.Root, before, after, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
            scoredEntry.SourcePath.ShouldBe("old");
            scoredEntry.Path.ShouldBe("new");
            foreach ((string sourceMode, string destinationMode, string status) in new[]
            {
                ("000000", "100755", "A"), ("000000", "120000", "A"),
                ("000000", "160000", "A"), ("100644", "000000", "D"),
                ("100644", "120000", "T"), ("100644", "160000", "T"),
                ("100644", "100755", "M"),
            })
            {
                string sourceId = sourceMode == "000000" ? new string('0', 40) : new string('a', 40);
                string destinationId = destinationMode == "000000" ? new string('0', 40) : new string('b', 40);
                string raw = $":{sourceMode} {destinationMode} {sourceId} {destinationId} {status}\0path\0";
                string modeScript = "import sys\na=sys.argv[1:]\n"
                    + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                    + "(b'a'*40+b'\\n' if '--verify' in a else ("
                    + JsonSerializer.Serialize(raw) + ".encode() if 'diff' in a else b'')))";
                GitFact<IReadOnlyList<GitDiffEntry>> modeFact = await PythonFacts(modeScript).ReadDiffAsync(
                    fixture.Root, before, after, TestContext.Current.CancellationToken);
                modeFact.IsSuccess.ShouldBeTrue(modeFact.Message);
                modeFact.Value!.Single().SourceMode.ShouldBe(sourceMode);
                modeFact.Value!.Single().DestinationMode.ShouldBe(destinationMode);
            }
            foreach (string malformedRaw in new[]
            {
                $":100644 160000 {new string('a', 40)} {new string('b', 40)} M\0path\0",
                $":100644 100644 {new string('a', 40)} {new string('b', 40)} T\0path\0",
            })
            {
                string modeScript = "import sys\na=sys.argv[1:]\n"
                    + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                    + "(b'a'*40+b'\\n' if '--verify' in a else ("
                    + JsonSerializer.Serialize(malformedRaw) + ".encode() if 'diff' in a else b'')))";
                (await PythonFacts(modeScript).ReadDiffAsync(
                    fixture.Root, before, after, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            }
            foreach (string malformedTree in new[]
            {
                $"100644  blob {new string('a', 40)}\tpath\0",
                $"100644 blob {new string('0', 40)}\tpath\0",
                $"100644 blob {new string('a', 40)}\tpath\0100644 blob {new string('b', 40)}\tpath/child\0",
                $"100644 blob {new string('a', 40)}\t../escape\0",
            })
            {
                string treeScript = "import sys\na=sys.argv[1:]\n"
                    + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                    + "(b'a'*40+b'\\n' if '--verify' in a else ("
                    + JsonSerializer.Serialize(malformedTree) + ".encode() if 'ls-tree' in a else b'')))";
                (await PythonFacts(treeScript).ReadTreeAsync(
                    fixture.Root, after, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            }
            byte[] gitObject = [.. Encoding.ASCII.GetBytes("blob 5\0"), .. Encoding.UTF8.GetBytes("small")];
            string blobObjectId = Convert.ToHexStringLower(SHA1.HashData(gitObject));
            string BlobScript(string content) => "import sys\na=sys.argv[1:]\n"
                + "sys.stdout.buffer.write((" + rootLiteral + "+'\\n').encode() if '--show-toplevel' in a else "
                + "(b'a'*40+b'\\n' if '--verify' in a else "
                + "(b'100644 blob " + blobObjectId + "\\tsmall.txt\\0' if 'ls-tree' in a and ':(literal)small.txt' in a "
                + "else (b'x'*(17*1024*1024) if 'ls-tree' in a else "
                + JsonSerializer.Serialize(content) + ".encode() if 'cat-file' in a else b''))))";
            GitFact<GitBlob> narrowBlob = await PythonFacts(BlobScript("small")).ReadBlobAsync(
                fixture.Root, after, "small.txt", TestContext.Current.CancellationToken);
            narrowBlob.IsSuccess.ShouldBeTrue(narrowBlob.Message);
            narrowBlob.Value!.Content.ShouldBe(Encoding.UTF8.GetBytes("small"));
            GitFact<GitBlob> corruptBlob = await PythonFacts(BlobScript("wrong")).ReadBlobAsync(
                fixture.Root, after, "small.txt", TestContext.Current.CancellationToken);
            corruptBlob.Status.ShouldBe(GitFactStatus.Invalid);
            corruptBlob.Code.ShouldBe("GIT_OUTPUT_INVALID");
            string renameSource = Path.Combine(fixture.Root, "rename-source.txt");
            File.WriteAllText(renameSource, "content to rename");
            string secondRenameSource = Path.Combine(fixture.Root, "second-rename-source.txt");
            File.WriteAllText(secondRenameSource, "second content to rename");
            string renameBefore = fixture.Commit();
            File.Move(renameSource, Path.Combine(fixture.Root, "rename-destination.txt"));
            File.Move(secondRenameSource, Path.Combine(fixture.Root, "second-rename-destination.txt"));
            string renameAfter = fixture.Commit();
            fixture.Git("config", "diff.renames", "false");
            fixture.Git("config", "diff.renameLimit", "1");
            GitFact<GitCommandResult> pinnedLimit = await facts.RunAsync(
                fixture.Root, ["config", "--get", "diff.renameLimit"], TestContext.Current.CancellationToken);
            pinnedLimit.IsSuccess.ShouldBeTrue(pinnedLimit.Message);
            pinnedLimit.Value!.Stdout.ShouldBe(Encoding.UTF8.GetBytes("0\n"));
            GitFact<IReadOnlyList<GitDiffEntry>> renamed = await facts.ReadDiffAsync(
                fixture.Root, renameBefore, renameAfter, TestContext.Current.CancellationToken);
            renamed.IsSuccess.ShouldBeTrue(renamed.Message);
            renamed.Value!.Count.ShouldBe(2);
            renamed.Value.All(item => item.Status == "R100").ShouldBeTrue();
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
        (await facts.RunAsync(fixture.Root, null!, TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
        (await facts.ResolveRevisionAsync(fixture.Root, "missing-revision", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
        (await facts.ResolveRepositoryAsync(Path.Combine(fixture.Root, "absent"), TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
        (await facts.ReadBlobAsync(Path.Combine(fixture.Root, "absent"), "HEAD", "../escape", TestContext.Current.CancellationToken))
            .Code.ShouldBe("REPOSITORY_UNAVAILABLE");
        Directory.CreateDirectory(Path.Combine(fixture.Root, "nested"));
        (await facts.ResolveRepositoryAsync(Path.Combine(fixture.Root, "nested"), TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
        RepositoryLocator.ResolveContainedPath(fixture.Root, "../escape").Code.ShouldBe("REPOSITORY_PATH_ESCAPE");
        RepositoryLocator.ResolveContainedPath(fixture.Root, "bad\0path").Status.ShouldBe(GitFactStatus.Invalid);
        GitFact<string> missingGit = await new GitFacts(Path.Combine(fixture.Root, "missing-git"))
            .ResolveRepositoryAsync(fixture.Root, TestContext.Current.CancellationToken);
        missingGit.Status.ShouldBe(GitFactStatus.Unavailable);
        missingGit.Code.ShouldBe("GIT_UNAVAILABLE");
        (await facts.RunAsync("bad\0root", [], TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
        (await facts.ReadBlobAsync(fixture.Root, "HEAD", "missing", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
        if (!OperatingSystem.IsWindows())
        {
            string nonExecutable = Path.Combine(fixture.Root, "non-executable-git");
            File.WriteAllText(nonExecutable, "#!/bin/sh\nexit 0\n");
            File.SetUnixFileMode(nonExecutable, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            GitFact<string> nonExecutableGit = await new GitFacts(nonExecutable)
                .ResolveRepositoryAsync(fixture.Root, TestContext.Current.CancellationToken);
            nonExecutableGit.Status.ShouldBe(GitFactStatus.Unavailable);
            nonExecutableGit.Code.ShouldBe("GIT_UNAVAILABLE");
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
            (await facts.ResolveRevisionAsync(shallowPath, first.ToUpperInvariant(), TestContext.Current.CancellationToken))
                .Status.ShouldBe(GitFactStatus.Unavailable);
            (await facts.ResolveRevisionAsync(shallowPath, "HEAD~1", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
            (await facts.ResolveRevisionAsync(shallowPath, "HEAD~", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Unavailable);
            (await facts.ResolveRevisionAsync(shallowPath, "misspelled", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            (await facts.ResolveRevisionAsync(shallowPath, "HEAD~bogus", TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            (await facts.ResolveRevisionAsync(shallowPath, new string('f', 40), TestContext.Current.CancellationToken)).Status.ShouldBe(GitFactStatus.Invalid);
            string largeShallow = Path.Combine(Path.GetTempPath(), "gitfacts-large-shallow-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (FileStream file = File.Create(largeShallow)) file.SetLength(16 * 1024 * 1024 + 1);
                string rootLiteral = JsonSerializer.Serialize(fixture.Root);
                string fake = "import sys\na=sys.argv[1:]\n"
                    + "if '--show-toplevel' in a: print(" + rootLiteral + ")\n"
                    + "elif '--verify' in a: sys.exit(1)\n"
                    + "elif '--is-shallow-repository' in a: print('true')\n"
                    + "elif 'rev-list' in a: print('a'*40)\n"
                    + "elif '--git-path' in a: print(" + JsonSerializer.Serialize(largeShallow) + ")\n";
                GitFact<string> overLimit = await PythonFacts(fake).ResolveRevisionAsync(
                    fixture.Root, "HEAD~1", TestContext.Current.CancellationToken);
                overLimit.Status.ShouldBe(GitFactStatus.Invalid);
                overLimit.Code.ShouldBe("GIT_OUTPUT_LIMIT_EXCEEDED");
            }
            finally { File.Delete(largeShallow); }
            using (GitFixture rootFixture = new())
            {
                File.WriteAllText(Path.Combine(rootFixture.Root, "only.txt"), "only");
                rootFixture.Commit();
                string rootClone = Path.Combine(Path.GetTempPath(), "gitfacts-root-shallow-" + Guid.NewGuid().ToString("N"));
                try
                {
                    string rootUri = new Uri(rootFixture.Root + Path.DirectorySeparatorChar).AbsoluteUri;
                    (await facts.RunAsync(Path.GetTempPath(), ["clone", "--quiet", "--depth", "1", rootUri, rootClone],
                        TestContext.Current.CancellationToken)).Value!.ExitCode.ShouldBe(0);
                    (await facts.ResolveRevisionAsync(rootClone, "HEAD~1", TestContext.Current.CancellationToken))
                        .Status.ShouldBe(GitFactStatus.Invalid);
                }
                finally { GitFixture.DeleteTree(rootClone); }
            }
            using (GitFixture mergeFixture = new())
            {
                File.WriteAllText(Path.Combine(mergeFixture.Root, "base.txt"), "base");
                mergeFixture.Commit();
                string mainBranch = mergeFixture.Git("branch", "--show-current");
                mergeFixture.Git("branch", "side");
                File.WriteAllText(Path.Combine(mergeFixture.Root, "main.txt"), "main");
                mergeFixture.Commit();
                mergeFixture.Git("checkout", "--quiet", "side");
                File.WriteAllText(Path.Combine(mergeFixture.Root, "side.txt"), "side");
                mergeFixture.Commit();
                mergeFixture.Git("checkout", "--quiet", mainBranch);
                mergeFixture.Git("merge", "--quiet", "--no-ff", "-m", "fixture merge", "side");
                string mergeClone = Path.Combine(Path.GetTempPath(), "gitfacts-merge-shallow-" + Guid.NewGuid().ToString("N"));
                try
                {
                    string mergeUri = new Uri(mergeFixture.Root + Path.DirectorySeparatorChar).AbsoluteUri;
                    (await facts.RunAsync(Path.GetTempPath(), ["clone", "--quiet", "--depth", "1", mergeUri, mergeClone],
                        TestContext.Current.CancellationToken)).Value!.ExitCode.ShouldBe(0);
                    (await facts.ResolveRevisionAsync(mergeClone, "HEAD^2", TestContext.Current.CancellationToken))
                        .Status.ShouldBe(GitFactStatus.Unavailable);
                    (await facts.ResolveRevisionAsync(mergeClone, "HEAD^3", TestContext.Current.CancellationToken))
                        .Status.ShouldBe(GitFactStatus.Invalid);
                }
                finally { GitFixture.DeleteTree(mergeClone); }
            }
            using (GitFixture octopusFixture = new())
            {
                File.WriteAllText(Path.Combine(octopusFixture.Root, "base.txt"), "base");
                octopusFixture.Commit();
                string mainBranch = octopusFixture.Git("branch", "--show-current");
                octopusFixture.Git("branch", "side-one");
                octopusFixture.Git("branch", "side-two");
                File.WriteAllText(Path.Combine(octopusFixture.Root, "main.txt"), "main");
                octopusFixture.Commit();
                octopusFixture.Git("checkout", "--quiet", "side-one");
                File.WriteAllText(Path.Combine(octopusFixture.Root, "one.txt"), "one");
                octopusFixture.Commit();
                octopusFixture.Git("checkout", "--quiet", "side-two");
                File.WriteAllText(Path.Combine(octopusFixture.Root, "two.txt"), "two");
                octopusFixture.Commit();
                octopusFixture.Git("checkout", "--quiet", mainBranch);
                octopusFixture.Git("merge", "--quiet", "--no-ff", "-m", "octopus merge", "side-one", "side-two");
                string octopusClone = Path.Combine(Path.GetTempPath(), "gitfacts-octopus-shallow-" + Guid.NewGuid().ToString("N"));
                try
                {
                    string octopusUri = new Uri(octopusFixture.Root + Path.DirectorySeparatorChar).AbsoluteUri;
                    (await facts.RunAsync(Path.GetTempPath(), ["clone", "--quiet", "--depth", "1", octopusUri, octopusClone],
                        TestContext.Current.CancellationToken)).Value!.ExitCode.ShouldBe(0);
                    (await facts.ResolveRevisionAsync(octopusClone, "HEAD^3", TestContext.Current.CancellationToken))
                        .Status.ShouldBe(GitFactStatus.Unavailable);
                    (await facts.ResolveRevisionAsync(octopusClone, "HEAD^4", TestContext.Current.CancellationToken))
                        .Status.ShouldBe(GitFactStatus.Invalid);
                }
                finally { GitFixture.DeleteTree(octopusClone); }
            }
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
            GitFixture.DeleteTree(shallowPath);
            GitFixture.DeleteTree(partialPath);
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
