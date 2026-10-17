using System.Text;
using System.Reflection;
using Xunit;
using System.Text.Json;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>Checks the frozen evidence readers against independently pinned entry material.</summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class EvidenceBoundaryAdoptionValidationTest(ITestOutputHelper output)
{
    private const string EntrySnapshotSha256 = "a90a2cc9a0fcfdeec6b77a760cbcd68ac7deffb79302273f577bc5eba743265f";
    private const string BaselineCommit = "1915a47123737859e3ac31872fa787fa8c0bd1e3";

    /// <summary>AC-01 preserves the exact 27-reader inventory.</summary>
    [Fact]
    public void ReaderInventoryShouldMatchFrozenSet() => Check("readers");

    /// <summary>AC-02 compares full expressions, fixtures and constants through exact substitutions.</summary>
    [Fact]
    public void MigrationsShouldUseHelperAtEqualStrength() => Check("readers");

    /// <summary>AC-03 permits no entry exemptions.</summary>
    [Fact]
    public void DayOneExemptionInventoryShouldBeEmpty() => Check("exemptions");

    /// <summary>AC-04 bounds both streams and keeps missing history visibly unavailable.</summary>
    [Fact]
    public async Task KnownGitDefectsShouldBeClosedWithoutChangingProjectionProof()
    {
        Check("projection");
        string root = RepositoryLocator.FindRepositoryRoot();
        GitFacts fixture = new("python3", ["-c", "import os; os.write(2,b'e'*131072); os.write(1,b'o'*131072)"], false);
        GitFact<GitCommandResult> output = await fixture.RunAsync(root, [], TestContext.Current.CancellationToken, TimeSpan.FromSeconds(10));
        output.IsSuccess.ShouldBeTrue("GIT_RUNNER_DEADLOCK: " + output.Message);
        output.Value!.Stdout.Length.ShouldBe(131072);
        output.Value.Stderr.Length.ShouldBe(131072);
        GitFact<GitBlob> history = await new GitFacts().ReadBlobAsync(root, new string('f', 40), "missing.txt", TestContext.Current.CancellationToken);
        AssertionLedger ledger = new();
        BoundaryAssertions.HistoryAvailable(ledger, "unavailable-history", history).ShouldBeFalse("UNAVAILABLE_HISTORY_PASSED");
        ledger.Rows.Single().State.ShouldBe("SKIPPED");
        ledger.Evaluate().Passed.ShouldBeFalse();
    }

    /// <summary>AC-05 binds the five runbook sections and exact current commands.</summary>
    [Fact]
    public void RunbookShouldDocumentTheExactContract() => Check("runbook");

    private void Check(string check)
    {
        string version = typeof(EvidenceBoundaryAdoptionValidationTest).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        output.WriteLine("story104_source_revision=" + version);
        string root = RepositoryLocator.FindRepositoryRoot();
        EvidenceManifest.HashFile(Path.Combine(root, "artifacts/v9/10.4/reader-entry-v1.json")).ShouldBe(EntrySnapshotSha256);
        GitFact<GitCommandResult> capture = new GitFacts("python3", includeGitPreamble: false).RunOrchestrationAsync(root,
            ["_bmad/scripts/evidence_boundary_adoption.py", "--repository", root, "--baseline", BaselineCommit, "--check", check], TestContext.Current.CancellationToken, TimeSpan.FromMinutes(20)).GetAwaiter().GetResult();
        capture.IsSuccess.ShouldBeTrue(capture.Code + ": " + capture.Message);
        GitCommandResult process = capture.Value!;
        string text = Encoding.UTF8.GetString(process.Stdout);
        output.WriteLine(text);
        process.ExitCode.ShouldBe(0, text + Encoding.UTF8.GetString(process.Stderr));
        using JsonDocument document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("result").GetString().ShouldBe("PASS");
        document.RootElement.GetProperty("assertionLedger").GetArrayLength().ShouldBeGreaterThan(0);
    }
}
