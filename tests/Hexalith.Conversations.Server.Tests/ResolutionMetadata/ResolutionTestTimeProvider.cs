namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionTestTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
