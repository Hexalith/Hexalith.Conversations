using System.Text.Json;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

public sealed class BoundaryAssertionsTests
{
    private const string Story101RecordSha256 = "334858d730c96c0a5a6647c514e631cfe4907756846bf2e0147e11937e221404";
    private const string Story101CandidateCommit = "79f4b126ca66d419a584855936ce56a1266cce67";
    private static readonly string[] SupersessionAllowlist = [];

    [Fact]
    public async Task ChangedPathsShouldUseExactSetEquality()
    {
        using GitFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.Root, "old.txt"), "rename proof");
        string before = fixture.Commit();
        File.Move(Path.Combine(fixture.Root, "old.txt"), Path.Combine(fixture.Root, "new.txt"));
        File.WriteAllText(Path.Combine(fixture.Root, "added.txt"), "added");
        string after = fixture.Commit();
        GitFact<IReadOnlyList<GitDiffEntry>> diff = await new GitFacts().ReadDiffAsync(fixture.Root, before, after, TestContext.Current.CancellationToken);
        diff.IsSuccess.ShouldBeTrue(diff.Message);
        diff.Value!.ShouldContain(row => row.Status == "R100" && row.SourcePath == "old.txt");
        AssertionLedger equal = new();
        BoundaryAssertions.ChangedPaths(equal, "paths", ["old.txt", "new.txt", "added.txt", "new.txt"], diff).ShouldBeTrue();
        equal.Evaluate().Passed.ShouldBeTrue();
        foreach (string[] expected in new[] { new[] { "new.txt", "added.txt" }, new[] { "old.txt", "new.txt", "added.txt", "extra.txt" } })
        {
            AssertionLedger ledger = new();
            BoundaryAssertions.ChangedPaths(ledger, "paths", expected, diff).ShouldBeFalse();
            ledger.Evaluate().Blockers.Single().Code.ShouldBe("CHANGED_PATH_SET_MISMATCH");
            ledger.Evaluate().Blockers.Single().Message.ShouldContain(expected.Length == 2 ? "unexpected: [old.txt]" : "Missing: [extra.txt]");
        }
        AssertionLedger ordinal = new();
        BoundaryAssertions.ChangedPaths(ordinal, "paths", ["OLD.txt", "new.txt", "added.txt"], diff).ShouldBeFalse();
    }

    [Fact]
    public async Task GitlinksShouldBeDetectedFromRawModes()
    {
        using GitFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.Root, "seed.txt"), "seed");
        string before = fixture.Commit();
        File.WriteAllText(Path.Combine(fixture.Root, "decoy-160000.txt"), "content 160000");
        // This fixed blob has a real Git object ID containing 160000, independently of its mode.
        File.WriteAllText(Path.Combine(fixture.Root, "object-id-decoy.txt"), "object id decoy 1265113\n");
        fixture.Git("add", "decoy-160000.txt", "object-id-decoy.txt");
        fixture.Git("update-index", "--add", "--cacheinfo", $"160000,{before},deps/module");
        fixture.Git("commit", "--quiet", "-m", "fixture");
        string after = fixture.Git("rev-parse", "HEAD");
        GitFact<IReadOnlyList<GitDiffEntry>> diff = await new GitFacts().ReadDiffAsync(fixture.Root, before, after, TestContext.Current.CancellationToken);
        diff.IsSuccess.ShouldBeTrue(diff.Message);
        AssertionLedger ledger = new();
        BoundaryAssertions.NoGitlinks(ledger, "gitlinks", diff).ShouldBeFalse();
        ledger.Evaluate().Blockers.Single().ShouldBe(new BoundaryBlocker("EVIDENCE_BOUNDARY_GITLINK", "deps/module", "Raw modes 000000 -> 160000."));

        GitDiffEntry decoy = diff.Value!.Single(row => row.Path == "object-id-decoy.txt");
        decoy.DestinationObject.ShouldBe("f836126ebb131e3a160000a4f33e98866ec3ba4f");
        AssertionLedger clean = new();
        BoundaryAssertions.NoGitlinks(clean, "decoys", GitFact<IReadOnlyList<GitDiffEntry>>.Ok(
            diff.Value!.Where(row => row.Path != "deps/module").ToArray())).ShouldBeTrue();
        clean.Evaluate().Passed.ShouldBeTrue();
        AssertionLedger deleted = new();
        BoundaryAssertions.NoGitlinks(deleted, "deleted", GitFact<IReadOnlyList<GitDiffEntry>>.Ok([
            new("160000", "000000", before, new string('0', 40), "D", "deps/deleted", null)])).ShouldBeFalse();
    }

    [Fact]
    public void InertRootDeclarationsShouldBeRejected()
    {
        using Stream fixture = typeof(BoundaryAssertionsTests).Assembly.GetManifestResourceStream(
            "Hexalith.Conversations.TestSupport.Tests.Fixtures.story102-root-declarations.json")!;
        using JsonDocument cases = JsonDocument.Parse(fixture);
        Dictionary<string, string> roots = new() { ["Root"] = "abcdef" };
        foreach (JsonElement row in cases.RootElement.EnumerateArray())
        {
            string source = row.GetProperty("source").GetString()!;
            bool expected = row.GetProperty("pinned").GetBoolean();
            AssertionLedger ledger = new();
            BoundaryAssertions.PinnedRoots(ledger, "roots", source, roots).ShouldBe(expected, row.GetProperty("id").GetString());
            ledger.Evaluate().Passed.ShouldBe(expected);
            if (!expected) ledger.Evaluate().Blockers.Single().Code.ShouldBe("ROOT_OF_TRUST_NOT_PINNED");
        }
    }

    [Fact]
    public void ConditionalSyntaxAndCSharpLineBoundariesShouldBeRejected()
    {
        Dictionary<string, string> roots = new() { ["Root"] = "abcdef" };
        foreach (string newline in new[] { "\r\n", "\r", "\n", "\u0085", "\u2028", "\u2029" })
        {
            foreach (string whitespace in new[] { " ", "\t", "\v", "\f", "\u00a0", "\u2003", "\u3000", "\ufeff", "\u001a" })
            {
                foreach (string directive in new[] { "if(false)", "if!true", "if" + whitespace + "false" })
                {
                    string source = "class C {" + newline + "// boundary" + newline
                        + whitespace + "#" + whitespace + directive + newline
                        + "const string Root = \"abcdef\";" + newline + "#endif" + newline + "}";
                    AssertionLedger ledger = new();
                    BoundaryAssertions.PinnedRoots(ledger, "roots", source, roots).ShouldBeFalse();
                    ledger.Evaluate().Blockers.Single().Code.ShouldBe("ROOT_OF_TRUST_NOT_PINNED");
                }
            }
            AssertionLedger active = new();
            BoundaryAssertions.PinnedRoots(active, "roots", "// comment" + newline + "const string Root = \"abcdef\";", roots)
                .ShouldBeTrue();
        }
    }

    [Fact]
    public void ConditionalRegionsShouldNotSpliceLiteralDeclarations()
    {
        const string compiledRoot = "abcdef"
#if(true)
            + "-changed"
#endif
            ;
        compiledRoot.ShouldBe("abcdef-changed");
        foreach (string newline in new[] { "\r\n", "\r", "\n", "\u0085", "\u2028", "\u2029" })
        {
            string source = "const string Root = \"abcdef\"" + newline
                + "#if(true)" + newline + "+ \"-changed\"" + newline + "#endif" + newline + ";";
            AssertionLedger ledger = new();
            BoundaryAssertions.PinnedRoots(ledger, "roots", source, new Dictionary<string, string> { ["Root"] = "abcdef" })
                .ShouldBeFalse();
        }
    }

    [Fact]
    public async Task RootsOfTrustAndSignedEvidenceShouldRemainPinned()
    {
        string root = EvidenceFixture.FindRepositoryRoot();
        const string sourcePath = "tests/Hexalith.Conversations.TestSupport.Tests/BoundaryAssertionsTests.cs";
        GitFact<string> source = RepositoryLocator.ResolveContainedPath(root, sourcePath);
        source.IsSuccess.ShouldBeTrue(source.Message);
        Dictionary<string, string> constants = new(StringComparer.Ordinal)
        {
            [nameof(Story101RecordSha256)] = Story101RecordSha256,
            [nameof(Story101CandidateCommit)] = Story101CandidateCommit,
        };
        AssertionLedger ledger = new();
        BoundaryAssertions.PinnedRoots(ledger, "roots", File.ReadAllText(source.Value!), constants).ShouldBeTrue();
        string recordPath = Path.Combine(root, "docs/release-evidence/story-10.1-final-record-v2.json");
        BoundaryAssertions.Equal(ledger, "predecessor-digest", Story101RecordSha256, EvidenceFixture.Hash(recordPath)).ShouldBeTrue();
        using JsonDocument record = JsonDocument.Parse(File.ReadAllBytes(recordPath));
        BoundaryAssertions.Equal(ledger, "predecessor-candidate", Story101CandidateCommit,
            record.RootElement.GetProperty("candidate").GetProperty("commit").GetString()).ShouldBeTrue();
        GitFact<string> candidate = await new GitFacts().ResolveRevisionAsync(root, Story101CandidateCommit, TestContext.Current.CancellationToken);
        BoundaryAssertions.HistoryAvailable(ledger, "predecessor-history", candidate).ShouldBeTrue();
        candidate.Value.ShouldBe(Story101CandidateCommit);
        SupersessionAllowlist.ShouldBeEmpty();
        BoundaryAssertions.SignedEvidenceIsNotAllowlisted(ledger, "allowlist", root, SupersessionAllowlist).ShouldBeTrue();
        ledger.Evaluate().Passed.ShouldBeTrue();

        using EvidenceFixture fixture = new();
        AssertionLedger removed = new();
        BoundaryAssertions.PinnedRoots(removed, "removed-root", "// const string Story101RecordSha256 = \"" + Story101RecordSha256 + "\";", constants).ShouldBeFalse();
        removed.Evaluate().Blockers.ShouldContain(row => row.Code == "ROOT_OF_TRUST_NOT_PINNED");
        foreach (string path in new[] { "docs/release-evidence/proof.json", "DOCS\\Release-Evidence\\proof.json", "docs/other/../release-evidence/proof.json",
            "docs/release-evidence", "DOCS\\Release-Evidence", "docs", "DOCS", "docs/other/..", ".", "docs/**", "docs/release-*/proof.json", "other/?", "other/[abc]" })
        {
            AssertionLedger exempted = new();
            BoundaryAssertions.SignedEvidenceIsNotAllowlisted(exempted, "exempted", fixture.Root, [path]).ShouldBeFalse();
            exempted.Evaluate().Blockers.Single().Code.ShouldBe("SIGNED_EVIDENCE_ALLOWLISTED");
        }
        TestContext.Current.TestOutputHelper!.WriteLine("STORY102_ROOTS:" + JsonSerializer.Serialize(new { sourcePath, constants, allowlist = SupersessionAllowlist }));
    }
}
