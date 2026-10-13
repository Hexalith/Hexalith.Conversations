using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

public sealed class AssertionLedgerTests
{
    [Fact]
    public async Task UnavailableHistoryShouldSkipAndNeverPass()
    {
        using GitFixture fixture = new();
        File.WriteAllText(Path.Combine(fixture.Root, "seed.txt"), "seed");
        fixture.Commit();
        GitFact<string> missing = await new GitFacts().ResolveRevisionAsync(fixture.Root, new string('f', 40), TestContext.Current.CancellationToken);
        missing.IsSuccess.ShouldBeFalse();
        AssertionLedger ledger = new();
        BoundaryAssertions.HistoryAvailable(ledger, "history", missing).ShouldBeFalse();
        ledger.Results.ShouldBeEmpty();
        ledger.Rows.Single().State.ShouldBe("SKIPPED");
        ledger.Skips.ShouldContainKey("history");
        ledger.Evaluate().Passed.ShouldBeFalse();
        ledger.Evaluate().Blockers.ShouldContain(row => row.Code == "ASSERTION_LEDGER_EMPTY");
        ledger.Record("history", true); // A deliberately non-compliant consumer tries to claim the skipped check passed.
        ledger.Evaluate().Passed.ShouldBeFalse();
        ledger.Evaluate().Blockers.ShouldContain(row => row.Code == "UNAVAILABLE_HISTORY_PASSED");
        new AssertionLedger().Evaluate().Blockers.Single().Code.ShouldBe("ASSERTION_LEDGER_EMPTY");
    }

    [Fact]
    public void DuplicateSkipShouldPreserveTheFirstDiagnostic()
    {
        AssertionLedger ledger = new();
        ledger.Skip("history", "first revision", "HISTORY_UNAVAILABLE", "first diagnostic");
        Should.Throw<ArgumentException>(() => ledger.Skip("history", "second revision", "REVISION_INVALID", "second diagnostic"));
        ledger.Results.ShouldBeEmpty();
        ledger.Rows.Single().ShouldBe(new AssertionLedgerRow("history", "first revision", "SKIPPED"));
        ledger.Skips.Single().Value.ShouldBe(new BoundaryBlocker("HISTORY_UNAVAILABLE", "first revision", "first diagnostic"));
        ledger.Evaluate().Blockers.ShouldContain(ledger.Skips["history"]);
    }

    [Fact]
    public void EveryNonSuccessGitStatusShouldRemainVisibleAndBlockFinalPass()
    {
        foreach (GitFactStatus status in Enum.GetValues<GitFactStatus>().Where(status => status != GitFactStatus.Success))
        {
            AssertionLedger ledger = new();
            BoundaryAssertions.True(ledger, "evaluated", true);
            GitFact<IReadOnlyList<GitDiffEntry>> fact = GitFact<IReadOnlyList<GitDiffEntry>>.Fail(status, "GIT_UNAVAILABLE", "fixture");
            BoundaryAssertions.ChangedPaths(ledger, "paths", [], fact).ShouldBeFalse();
            BoundaryAssertions.NoGitlinks(ledger, "modes", fact).ShouldBeFalse();
            ledger.Results.Count.ShouldBe(1);
            ledger.Rows.Count(row => row.State == "SKIPPED").ShouldBe(2);
            ledger.Evaluate().Result.ShouldBe("BLOCKED");
        }
        AssertionLedger uncoded = new();
        uncoded.Record("evaluated", true);
        BoundaryAssertions.HistoryAvailable(uncoded, "uncoded-history",
            new GitFact<string>(GitFactStatus.Unavailable, null, null, null)).ShouldBeFalse();
        uncoded.Rows.Single(row => row.Id == "uncoded-history").State.ShouldBe("SKIPPED");
        uncoded.Evaluate().Blockers.Single().Code.ShouldBe("HISTORY_UNAVAILABLE");
        AssertionLedger failed = new();
        failed.Record("failed", false);
        failed.Evaluate().Result.ShouldBe("FAIL");
        AssertionLedger successful = new();
        successful.Record("passed", true);
        successful.Evaluate().Passed.ShouldBeTrue();
    }
}
