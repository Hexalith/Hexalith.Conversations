using System.Text.Json;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

public sealed class EvidenceBoundaryFaultTests
{
    [Fact]
    public async Task AllInvariantFaultsShouldFailAndRestore()
    {
        using EvidenceFixture fixture = new();
        string proof = fixture.Write("proof.txt", "original proof");
        string control = fixture.Write("control.txt", "proof.txt");
        fixture.Write("bin/proof.txt", "original proof");
        string digest = EvidenceFixture.Hash(proof);
        List<EvidenceMutationRow> rows = [];

        BoundaryEvaluation Manifest(string path, string hash, string? payload = null)
        {
            EvidenceManifest manifest = new();
            manifest.Add(path, hash, "proof");
            return manifest.Verify(fixture.Root, payload ?? manifest.ComputeSignablePayloadHash(), new());
        }
        string Payload()
        {
            EvidenceManifest manifest = new();
            manifest.Add("proof.txt", digest, "proof");
            return manifest.ComputeSignablePayloadHash();
        }
        void Fault(string id, string expected, string file, string mutation, Func<BoundaryEvaluation> consume, string? baseline = null)
        {
            if (baseline is not null) File.WriteAllText(file, baseline);
            consume().Passed.ShouldBeTrue("The unmutated fixture must pass before fault injection.");
            byte[] original = File.ReadAllBytes(file);
            string before = EvidenceFixture.Hash(file);
            BoundaryEvaluation? result = null;
            try
            {
                File.WriteAllText(file, mutation);
                EvidenceFixture.Hash(file).ShouldNotBe(before, "Every fault must mutate actual fixture bytes.");
                result = consume();
                result.Passed.ShouldBeFalse("FAULT_NOT_DETECTED");
                result.Blockers.ShouldContain(row => row.Code == expected, "FAULT_NOT_DETECTED");
            }
            finally { File.WriteAllBytes(file, original); }
            string after = EvidenceFixture.Hash(file);
            after.ShouldBe(before, "FIXTURE_NOT_RESTORED");
            string[] observed = result!.Blockers.Select(row => row.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            rows.Add(new(id, expected, observed, before, after, observed.Contains(expected, StringComparer.Ordinal), before == after));
        }
        Fault("AC-10.2-01-HASH", "EVIDENCE_HASH_MISMATCH", proof, "flipped proof", () => Manifest("proof.txt", digest));
        Fault("AC-10.2-01-PATH", "EVIDENCE_PATH_ESCAPE", control, "../outside.txt", () => Manifest(File.ReadAllText(control), digest));
        Fault("AC-10.2-01-MISSING", "EVIDENCE_FILE_MISSING", control, "missing.txt", () => Manifest(File.ReadAllText(control), digest));
        Fault("AC-10.2-02-OUTPUT", "EVIDENCE_OUTPUT_FORBIDDEN", control, "bin/proof.txt", () => Manifest(File.ReadAllText(control), digest));
        Fault("AC-10.2-03-SIGNABLE", "SIGNABLE_PAYLOAD_MISMATCH", control, new string('0', 64), () => Manifest("proof.txt", digest, File.ReadAllText(control)), baseline: Payload());

        using GitFixture git = new();
        File.WriteAllText(Path.Combine(git.Root, "old.txt"), "unchanged rename bytes");
        string beforeCommit = git.Commit();
        File.Move(Path.Combine(git.Root, "old.txt"), Path.Combine(git.Root, "new.txt"));
        string afterCommit = git.Commit();
        GitFact<IReadOnlyList<GitDiffEntry>> diff = await new GitFacts().ReadDiffAsync(git.Root, beforeCommit, afterCommit, TestContext.Current.CancellationToken);
        diff.IsSuccess.ShouldBeTrue(diff.Message);
        BoundaryEvaluation Paths()
        {
            AssertionLedger ledger = new();
            BoundaryAssertions.ChangedPaths(ledger, "paths", File.ReadAllText(control).Split(','), diff);
            return ledger.Evaluate();
        }
        Fault("AC-10.2-04-SUBSET", "CHANGED_PATH_SET_MISMATCH", control, "new.txt", Paths, baseline: "old.txt,new.txt");
        Fault("AC-10.2-04-SUPERSET", "CHANGED_PATH_SET_MISMATCH", control, "old.txt,new.txt,unexpected.txt", Paths, baseline: "old.txt,new.txt");
        Fault("AC-10.2-05-GITLINK", "EVIDENCE_BOUNDARY_GITLINK", control, "160000", () =>
        {
            string mode = File.ReadAllText(control);
            string objectId = mode == "160000" ? afterCommit : git.Git("hash-object", "-w", Path.Combine(git.Root, "new.txt"));
            git.Git("update-index", "--add", "--cacheinfo", $"{mode},{objectId},deps/module");
            git.Git("commit", "--quiet", "-m", "fixture");
            string linkCommit = git.Git("rev-parse", "HEAD");
            GitFact<IReadOnlyList<GitDiffEntry>> raw = new GitFacts().ReadDiffAsync(git.Root, afterCommit, linkCommit, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            raw.IsSuccess.ShouldBeTrue(raw.Message);
            AssertionLedger ledger = new();
            BoundaryAssertions.NoGitlinks(ledger, "gitlink", raw);
            return ledger.Evaluate();
        }, baseline: "100644");
        Fault("AC-10.2-06-HISTORY", "UNAVAILABLE_HISTORY_PASSED", control, new string('f', 40), () =>
        {
            GitFact<string> history = new GitFacts().ResolveRevisionAsync(git.Root, File.ReadAllText(control), TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            AssertionLedger ledger = new();
            if (!BoundaryAssertions.HistoryAvailable(ledger, "history", history)) ledger.Record("history", true);
            return ledger.Evaluate();
        }, baseline: afterCommit);
        Fault("AC-10.2-06-EMPTY", "ASSERTION_LEDGER_EMPTY", control, "false", () =>
        {
            AssertionLedger ledger = new();
            if (bool.Parse(File.ReadAllText(control))) ledger.Record("consumer", true);
            return ledger.Evaluate();
        }, baseline: "true");
        Fault("AC-10.2-07-ROOT", "ROOT_OF_TRUST_NOT_PINNED", control, "const string Root = \"tampered\";", () =>
        {
            AssertionLedger ledger = new();
            BoundaryAssertions.PinnedRoots(ledger, "root", File.ReadAllText(control), new Dictionary<string, string> { ["Root"] = digest });
            return ledger.Evaluate();
        }, baseline: "const string Root = \"" + digest + "\";");
        Fault("AC-10.2-07-ALLOWLIST", "SIGNED_EVIDENCE_ALLOWLISTED", control, "DOCS\\Release-Evidence\\proof.json", () =>
        {
            AssertionLedger ledger = new();
            BoundaryAssertions.SignedEvidenceIsNotAllowlisted(ledger, "allowlist", fixture.Root, File.ReadAllText(control).Split(',', StringSplitOptions.RemoveEmptyEntries));
            return ledger.Evaluate();
        }, baseline: string.Empty);
        rows.Count.ShouldBe(12);
        TestContext.Current.TestOutputHelper!.WriteLine("STORY102_MUTATIONS:" + JsonSerializer.Serialize(rows,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
