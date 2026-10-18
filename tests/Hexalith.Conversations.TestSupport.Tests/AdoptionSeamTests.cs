using System.Text;
using System.Text.Json;
using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

/// <summary>Exercises the discovery, contained read and chain seams used by migrated readers.</summary>
public sealed class AdoptionSeamTests
{
    /// <summary>Nested discovery and contained reads preserve evidence bytes.</summary>
    [Fact]
    public void DiscoveryAndReadsShouldUseTheOwningRepository()
    {
        string root = RepositoryLocator.FindRepositoryRoot();
        string nested = Path.Combine(root, "artifacts", "adoption-seam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(nested);
        try
        {
            string path = Path.Combine(nested, "evidence.txt");
            File.WriteAllText(path, "evidence\r\n");
            RepositoryLocator.FindRepositoryRoot(nested).ShouldBe(root);
            RepositoryLocator.ReadAllText(path).ShouldBe("evidence\r\n");
            EvidenceManifest.HashFile(path).ShouldBe(EvidenceManifest.HashBytes(File.ReadAllBytes(path)));
            if (!OperatingSystem.IsWindows())
            {
                string link = Path.Combine(nested, "link.txt");
                File.CreateSymbolicLink(link, path);
                Should.Throw<IOException>(() => RepositoryLocator.ReadAllBytes(link)).Message.ShouldContain("REPOSITORY_PATH_ESCAPE");
                Should.Throw<IOException>(() => EvidenceManifest.HashFile(link)).Message.ShouldContain("REPOSITORY_PATH_ESCAPE");
            }
            using GitFixture external = new();
            string escaped = Path.Combine(external.Root, "evidence.txt");
            File.WriteAllText(escaped, "outside");
            RepositoryLocator.FindRepositoryRoot(external.Root).ShouldBe(external.Root);
            Should.Throw<IOException>(() => RepositoryLocator.ReadAllBytes(escaped)).Message.ShouldContain("REPOSITORY_PATH_ESCAPE");
            Should.Throw<IOException>(() => EvidenceManifest.HashFile(escaped)).Message.ShouldContain("REPOSITORY_PATH_ESCAPE");
        }
        finally { Directory.Delete(nested, recursive: true); }
    }

    /// <summary>Requested paths cannot select a sibling checkout as their own trust root.</summary>
    [Fact]
    public void ContainedReadsShouldRejectSiblingCheckoutsAndTraversal()
    {
        using GitFixture owner = new();
        using GitFixture sibling = new();
        string path = Path.Combine(sibling.Root, "evidence.txt");
        File.WriteAllText(path, "sibling");
        Should.Throw<IOException>(() => RepositoryLocator.ReadAllBytes(path)).Message.ShouldContain("REPOSITORY_PATH_ESCAPE");
        Should.Throw<IOException>(() => RepositoryLocator.ReadAllText(path, null, repositoryRoot: owner.Root));
        Should.Throw<IOException>(() => RepositoryLocator.ReadAllLines(path, null, repositoryRoot: owner.Root));
        Should.Throw<IOException>(() => EvidenceManifest.HashFile(path, owner.Root));
        string traversal = Path.Combine(owner.Root, "..", Path.GetFileName(sibling.Root), "evidence.txt");
        Should.Throw<IOException>(() => RepositoryLocator.ContainedFile(traversal, owner.Root));
    }

    /// <summary>A moving HEAD cannot change the blob after its regular-mode check.</summary>
    [Fact]
    public async Task RegularBlobReadsShouldResolveMutableRevisionsOnce()
    {
        using GitFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.Root, "evidence.txt"), "regular bytes");
        string regular = fixture.Commit();
        File.WriteAllText(Path.Combine(fixture.Root, "link-target"), "target.txt");
        string blob = fixture.Git("hash-object", "-w", "link-target");
        fixture.Git("update-index", "--cacheinfo", $"120000,{blob},evidence.txt");
        fixture.Git("commit", "--quiet", "-m", "fixture");
        string symbolic = fixture.Git("rev-parse", "HEAD");
        fixture.Git("update-ref", "HEAD", regular);
        string script = "import pathlib,subprocess,sys; args=sys.argv[1:]; result=subprocess.run(['git',*args],capture_output=True); "
            + "move=args[0]=='ls-tree' and not pathlib.Path('race-fired').exists(); "
            + $"subprocess.run(['git','update-ref','HEAD','{symbolic}'],check=True) if move else None; "
            + "pathlib.Path('race-fired').touch() if move else None; sys.stdout.buffer.write(result.stdout); sys.stderr.buffer.write(result.stderr); sys.exit(result.returncode)";
        GitFacts facts = new("python3", ["-c", script], false);
        GitFact<GitBlob> result = await facts.ReadRegularBlobAsync(fixture.Root, "HEAD", "evidence.txt", TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.Message);
        result.Value!.Revision.ShouldBe(regular);
        System.Text.Encoding.UTF8.GetString(result.Value.Content).ShouldBe("regular bytes");
        File.Exists(Path.Combine(fixture.Root, "race-fired")).ShouldBeTrue();
        fixture.Git("rev-parse", "HEAD").ShouldBe(symbolic);
    }

    /// <summary>Orchestration can exceed a Git command's budget while sharing bounded capture and cancellation.</summary>
    [Fact]
    public async Task OrchestrationShouldHaveAnExplicitIndependentBudget()
    {
        using GitFixture fixture = new();
        GitFacts orchestration = new("python3", ["-c", "import time; time.sleep(31); print('completed')"], false);
        GitFact<GitCommandResult> result = await orchestration.RunOrchestrationAsync(fixture.Root, [], TestContext.Current.CancellationToken, TimeSpan.FromSeconds(35));
        result.IsSuccess.ShouldBeTrue(result.Message);
        result.Value!.ExitCode.ShouldBe(0);
        (await new GitFacts().RunAsync(fixture.Root, [], TestContext.Current.CancellationToken, TimeSpan.FromSeconds(31))).Code.ShouldBe("GIT_TIMEOUT_INVALID");
        (await new GitFacts().RunOrchestrationAsync(fixture.Root, [], TestContext.Current.CancellationToken)).Code.ShouldBe("GIT_TIMEOUT_INVALID");
        (await orchestration.RunOrchestrationAsync(fixture.Root, [], TestContext.Current.CancellationToken, TimeSpan.FromMinutes(21))).Code.ShouldBe("GIT_TIMEOUT_INVALID");
        using CancellationTokenSource cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancelled.CancelAfter(TimeSpan.FromMilliseconds(100));
        (await orchestration.RunOrchestrationAsync(fixture.Root, [], cancelled.Token)).Code.ShouldBe("GIT_CANCELLED");
        GitFacts overflow = new("python3", ["-c", "import os; os.write(1,b'o'*(17*1024*1024))"], false);
        (await overflow.RunOrchestrationAsync(fixture.Root, [], TestContext.Current.CancellationToken)).Code.ShouldBe("GIT_OUTPUT_LIMIT_EXCEEDED");
    }

    /// <summary>Regular evidence reads reject raw symbolic-link and gitlink tree modes.</summary>
    [Fact]
    public async Task RegularBlobReadsShouldPreserveHistoricalModeChecks()
    {
        using GitFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.Root, "regular.txt"), "regular");
        string regular = fixture.Commit();
        File.WriteAllText(Path.Combine(fixture.Root, "link.txt"), "regular.txt");
        string blob = fixture.Git("hash-object", "-w", "link.txt");
        fixture.Git("update-index", "--add", "--cacheinfo", $"120000,{blob},link.txt");
        fixture.Git("commit", "--quiet", "-m", "fixture");
        string symbolic = fixture.Git("rev-parse", "HEAD");
        GitFacts facts = new();
        (await facts.ReadBlobAsync(fixture.Root, symbolic, "link.txt", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await facts.ReadRegularBlobAsync(fixture.Root, symbolic, "link.txt", TestContext.Current.CancellationToken)).Code.ShouldBe("BLOB_INVALID");
        fixture.Git("update-index", "--add", "--cacheinfo", $"160000,{regular},nested");
        fixture.Git("commit", "--quiet", "-m", "fixture");
        (await facts.ReadRegularBlobAsync(fixture.Root, "HEAD", "nested", TestContext.Current.CancellationToken)).Code.ShouldBe("BLOB_INVALID");
        (await facts.ReadRegularBlobAsync(fixture.Root, regular, "regular.txt", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    /// <summary>A mutable revision cannot change between historical mode and byte reads.</summary>
    [Fact]
    public async Task RegularBlobReadsShouldPinMovingHeadBeforeCheckingModeAndBytes()
    {
        using GitFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.Root, "evidence.txt"), "regular evidence");
        string regular = fixture.Commit();
        File.WriteAllText(Path.Combine(fixture.Root, "evidence.txt"), "target.txt");
        string linkBlob = fixture.Git("hash-object", "-w", "evidence.txt");
        fixture.Git("update-index", "--cacheinfo", $"120000,{linkBlob},evidence.txt");
        fixture.Git("commit", "--quiet", "--reuse-message", regular);
        string symbolic = fixture.Git("rev-parse", "HEAD");
        fixture.Git("update-ref", "HEAD", regular);
        string marker = Path.Combine(fixture.Root, "head-moved");
        string script = "import pathlib,subprocess,sys\na=sys.argv[1:]\n"
            + "r=subprocess.run(['git',*a],capture_output=True)\n"
            + "marker=pathlib.Path(" + JsonSerializer.Serialize(marker) + ")\n"
            + "if 'ls-tree' in a and not marker.exists():\n"
            + " subprocess.run(['git','update-ref','HEAD'," + JsonSerializer.Serialize(symbolic) + "],check=True)\n"
            + " marker.write_text('moved')\n"
            + "sys.stdout.buffer.write(r.stdout)\nsys.stderr.buffer.write(r.stderr)\nsys.exit(r.returncode)";
        string python = OperatingSystem.IsWindows() ? "python" : "python3";
        GitFact<GitBlob> observed = await new GitFacts(python, ["-c", script], includeGitPreamble: false).ReadRegularBlobAsync(
            fixture.Root, "HEAD", "evidence.txt", TestContext.Current.CancellationToken);
        observed.IsSuccess.ShouldBeTrue(observed.Message);
        observed.Value!.Revision.ShouldBe(regular);
        observed.Value.Content.ShouldBe(Encoding.UTF8.GetBytes("regular evidence"));
        File.Exists(marker).ShouldBeTrue();
        fixture.Git("rev-parse", "HEAD").ShouldBe(symbolic);
    }

    /// <summary>Malformed chain regions fail with exact stable codes and nonempty ledgers.</summary>
    [Theory]
    [InlineData("valid", "PASS", "")]
    [InlineData("nested", "FAIL", "EVIDENCE_CHAIN_MARKER_INVALID")]
    [InlineData("duplicate", "FAIL", "EVIDENCE_CHAIN_MARKER_INVALID")]
    [InlineData("unterminated", "FAIL", "EVIDENCE_CHAIN_MARKER_INVALID")]
    [InlineData("changed", "FAIL", "EVIDENCE_CHAIN_TABLE_INVALID")]
    [InlineData("missing", "FAIL", "EVIDENCE_CHAIN_TABLE_INVALID")]
    [InlineData("reordered", "FAIL", "EVIDENCE_CHAIN_TABLE_INVALID")]
    public void ChainValidationShouldBeTableDriven(string mutation, string result, string code)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(code);
        string[] rows = ["| 10.1 | 10.2 |", "| 10.2 | 10.3 |"];
        const string Begin = "<!-- BEGIN -->";
        const string End = "<!-- END -->";
        string source = Begin + "\n" + string.Join('\n', rows) + "\n" + End;
        source = mutation switch
        {
            "nested" => Begin + source,
            "duplicate" => source + source,
            "unterminated" => source.Replace(End, string.Empty, StringComparison.Ordinal),
            "changed" => source.Replace("10.3", "10.4", StringComparison.Ordinal),
            "missing" => source.Replace(rows[0], string.Empty, StringComparison.Ordinal),
            "reordered" => Begin + "\n" + string.Join('\n', rows.Reverse()) + "\n" + End,
            _ => source,
        };
        AssertionLedger ledger = new();
        BoundaryAssertions.ChainTable(ledger, "chain", source, Begin, End, rows).ShouldBe(result == "PASS");
        ledger.Rows.ShouldNotBeEmpty();
        ledger.Evaluate().Result.ShouldBe(result);
        ledger.Evaluate().Blockers.Select(row => row.Code).ShouldBe(code.Length == 0 ? [] : new[] { code });
    }
}
