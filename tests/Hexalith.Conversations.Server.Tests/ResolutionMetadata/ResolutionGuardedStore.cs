using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Fakes;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionGuardedStore(InMemoryReadModelStore inner) : IReadModelStore, IReadModelBatchStore, IReadModelExpiringStore, IReadModelPendingBatchRecoveryStore
{
    public bool EmptyMetadataETag { get; set; }
    public bool AllowSummaryIndexReads { get; set; }
    public bool ForbidLegacyReads { get; set; }
    public bool ForbidWrites { get; set; }
    public int ForbiddenAttempts { get; private set; }
    public Func<Type, string, Task>? AfterRead { get; set; }
    public Func<Type, object, Task>? BeforeSave { get; set; }
    public Func<Type, object, Task>? AfterSave { get; set; }
    public Func<string, object, Task>? BeforeKeySave { get; set; }
    public Func<string, object, Task>? AfterKeySave { get; set; }
    public Func<ReadModelBatch, Task>? BeforeBatch { get; set; }
    public List<Type> Reads { get; } = [];
    public List<string> ReadKeys { get; } = [];

    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
        => ReadAsync<T>(storeName, key, false, cancellationToken);

    public Task<ReadModelEntry<T>> GetCompactedAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
        => ReadAsync<T>(storeName, key, true, cancellationToken);

    private async Task<ReadModelEntry<T>> ReadAsync<T>(string storeName, string key, bool compacted, CancellationToken cancellationToken) where T : class
    {
        Reads.Add(typeof(T));
        ReadKeys.Add(key);
        bool allowed = AllowSummaryIndexReads && typeof(T) == typeof(ConversationProjectionIndexReadModel) && key.StartsWith("projection:conversations-index:", StringComparison.Ordinal)
            || typeof(T) == typeof(ConversationResolutionNativeSelection) && key.StartsWith("projection:conversations-resolution-selection:", StringComparison.Ordinal)
            || typeof(T) == typeof(ConversationResolutionMetadataReadModel) && key.StartsWith("projection:conversations-resolution:", StringComparison.Ordinal)
            || typeof(T) == typeof(ConversationResolutionMetadataProof) && key.StartsWith("projection:conversations-resolution-proof:", StringComparison.Ordinal)
            || typeof(T) == typeof(ConversationProjectionDispatchLedger) && key.StartsWith("projection:conversations-dispatch:", StringComparison.Ordinal);
        if (ForbidLegacyReads && !allowed)
        {
            ForbiddenAttempts++;
            throw new Xunit.Sdk.XunitException("Forbidden detail/index/hydration read.");
        }

        var result = compacted ? await inner.GetCompactedAsync<T>(storeName, key, cancellationToken)
            : await inner.GetAsync<T>(storeName, key, cancellationToken);
        if (AfterRead is not null) await AfterRead(typeof(T), key);
        return EmptyMetadataETag && typeof(T) == typeof(ConversationResolutionMetadataReadModel) ? new(result.Value, string.Empty) : result;
    }

    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        ForbiddenAttempts++;
        throw new Xunit.Sdk.XunitException("Unconditional write is forbidden.");
    }

    public async Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        if (ForbidWrites) { ForbiddenAttempts++; throw new Xunit.Sdk.XunitException("Read attempted a mutation."); }
        if (BeforeSave is not null) await BeforeSave(typeof(T), value);
        if (BeforeKeySave is not null) await BeforeKeySave(key, value);
        bool result = await inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
        if (result && AfterSave is not null) await AfterSave(typeof(T), value);
        if (result && AfterKeySave is not null) await AfterKeySave(key, value);
        return result;
    }

    public async Task<bool> TrySaveWithTimeToLiveAsync<T>(string storeName, string key, T value, string etag, TimeSpan timeToLive, CancellationToken cancellationToken = default) where T : class
    {
        AssertWritesAllowed();
        if (BeforeSave is not null) await BeforeSave(typeof(T), value);
        if (BeforeKeySave is not null) await BeforeKeySave(key, value);
        bool result = await inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
        if (result && AfterSave is not null) await AfterSave(typeof(T), value);
        if (result && AfterKeySave is not null) await AfterKeySave(key, value);
        return result;
    }

    private void AssertWritesAllowed()
    {
        if (ForbidWrites)
        {
            ForbiddenAttempts++;
            throw new Xunit.Sdk.XunitException("Read attempted a mutation.");
        }
    }

    public Task<ReadModelPendingBatchRecoveryResult> RecoverPendingAsync(ReadModelBatchScope scope, string key, CancellationToken cancellationToken = default)
    {
        AssertWritesAllowed();
        return inner.RecoverPendingAsync(scope, key, cancellationToken);
    }

    public async Task<ReadModelBatchResult> ExecuteAsync(ReadModelBatch batch, CancellationToken cancellationToken = default)
    {
        AssertWritesAllowed();
        if (BeforeBatch is not null) await BeforeBatch(batch);
        return await inner.ExecuteAsync(batch, cancellationToken);
    }
}
