using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

/// <summary>Exercises the discovery, contained read and chain seams used by migrated readers.</summary>
public sealed class AdoptionSeamTests
{
    /// <summary>Nested discovery and contained reads preserve evidence bytes.</summary>
    [Fact]
    public void DiscoveryAndReadsShouldUseTheOwningRepository()
    {
        using GitFixture fixture = new();
        string nested = Path.Combine(fixture.Root, "nested");
        Directory.CreateDirectory(nested);
        string path = Path.Combine(nested, "evidence.txt");
        File.WriteAllText(path, "evidence\r\n");
        RepositoryLocator.FindRepositoryRoot(nested).ShouldBe(fixture.Root);
        RepositoryLocator.ReadAllText(path).ShouldBe("evidence\r\n");
        EvidenceManifest.HashFile(path).ShouldBe(EvidenceManifest.HashBytes(File.ReadAllBytes(path)));
        if (!OperatingSystem.IsWindows())
        {
            string link = Path.Combine(fixture.Root, "link.txt");
            File.CreateSymbolicLink(link, path);
            Should.Throw<IOException>(() => RepositoryLocator.ReadAllBytes(link)).Message.ShouldContain("REPOSITORY_PATH_ESCAPE");
        }
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
