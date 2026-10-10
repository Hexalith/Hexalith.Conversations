using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

public sealed class TestSupportSurfaceTests
{
    [Fact]
    public void RequiredHelpersShouldExistExactlyOnce()
    {
        string[] expected = ["RepositoryLocator", "GitFacts", "EvidenceManifest", "BoundaryAssertions", "AssertionLedger"];
        Type[] types = typeof(GitFacts).Assembly.GetExportedTypes();
        foreach (string name in expected)
        {
            types.Count(type => type.Name == name).ShouldBe(1);
        }

        typeof(GitFacts).Assembly.GetReferencedAssemblies().Any(name => name.Name!.StartsWith("Hexalith.Conversations", StringComparison.Ordinal)).ShouldBeFalse();
        EvidenceManifest manifest = new();
        manifest.Add("proof.json", new string('a', 64));
        AssertionLedger ledger = new();
        BoundaryAssertions.Equal(ledger, "proof", new string('a', 64), manifest.Digests["proof.json"]).ShouldBeTrue();
        ledger.Results.Count.ShouldBe(1);
    }
}
