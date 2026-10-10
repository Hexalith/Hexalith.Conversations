using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

internal sealed class GitFixture : IDisposable
{
    private readonly GitFacts _facts = new();

    internal GitFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "conversations-gitfacts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Git("init", "--quiet");
        Git("config", "user.name", "Fixture");
        Git("config", "user.email", "fixture@example.invalid");
    }

    internal string Root { get; }

    internal string Git(params string[] arguments)
    {
        GitFact<GitCommandResult> result = _facts.RunAsync(Root, arguments).GetAwaiter().GetResult();
        result.IsSuccess.ShouldBeTrue(result.Message);
        result.Value!.ExitCode.ShouldBe(0, System.Text.Encoding.UTF8.GetString(result.Value.Stderr));
        return System.Text.Encoding.UTF8.GetString(result.Value.Stdout).Trim();
    }

    internal string Commit()
    {
        Git("add", "--all");
        Git("commit", "--quiet", "-m", "fixture");
        return Git("rev-parse", "HEAD");
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
