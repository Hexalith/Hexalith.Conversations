using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

// Emulates an older implementation: it intentionally relies on the new interface's refusing default.
internal sealed class ResolutionUnsupportedCompactedStore : IReadModelStore
{
    public int ForbiddenAttempts { get; private set; }
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
    {
        ForbiddenAttempts++;
        throw new Xunit.Sdk.XunitException("Strict read fell back to a resolving legacy store.");
    }
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
        => throw new Xunit.Sdk.XunitException("Strict read attempted mutation.");
    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
        => throw new Xunit.Sdk.XunitException("Strict read attempted mutation.");
}
