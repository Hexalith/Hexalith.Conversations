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
        ((IDictionary<string, string>)manifest.Digests).IsReadOnly.ShouldBeTrue();
        ((IDictionary<string, bool>)ledger.Results).IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void FilesystemRootShouldContainRelativeChildren()
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;
        GitFact<string> contained = RepositoryLocator.ResolveContainedPath(root, "story101-root-probe");
        contained.IsSuccess.ShouldBeTrue(contained.Message);
        contained.Value.ShouldBe(Path.GetFullPath(Path.Combine(root, "story101-root-probe")));
    }
}
