// <copyright file="DerivedKeyReadStoreSpy.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Fakes;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Records every read, bulk read, and write attempted against an underlying <see cref="InMemoryReadModelStore"/>
/// so Story 12.3 tests can prove exact bounded read counts and zero read-side repairs.
/// </summary>
/// <param name="inner">The backing in-memory read-model store.</param>
internal sealed class DerivedKeyReadStoreSpy(InMemoryReadModelStore inner)
    : IReadModelStore,
      IReadModelBulkStore,
      IReadModelExpiringStore,
      IReadModelBatchStore,
      IReadModelConditionalEraser
{
    private readonly InMemoryReadModelStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>Gets the number of single-key reads observed.</summary>
    public int GetCalls { get; private set; }

    /// <summary>Gets the number of bulk read calls observed.</summary>
    public int BulkGetCalls { get; private set; }

    /// <summary>Gets the exact single-key read keys observed in call order.</summary>
    public List<string> SingleReadKeys { get; } = [];

    /// <summary>Gets the exact key batches requested through bulk reads.</summary>
    public List<IReadOnlyList<string>> BulkReadBatches { get; } = [];

    /// <summary>Gets every write or erase operation attempted against the store.</summary>
    public List<string> Writes { get; } = [];

    /// <summary>Gets or sets a value indicating whether all reads throw an <see cref="InvalidOperationException"/>.</summary>
    public bool FailReads { get; set; }

    /// <summary>Gets or sets an optional asynchronous delay before single-key reads for concurrency tests.</summary>
    public TimeSpan AsyncReadDelay { get; set; } = TimeSpan.Zero;

    /// <summary>Gets or sets an optional hook that returns a tampered bulk response for provider-fault testing.</summary>
    public Func<IReadOnlyList<string>, bool>? CorruptBulkResponse { get; set; }

    /// <summary>Resets all recorded read and write counters.</summary>
    public void Reset()
    {
        GetCalls = 0;
        BulkGetCalls = 0;
        SingleReadKeys.Clear();
        BulkReadBatches.Clear();
        Writes.Clear();
    }

    /// <inheritdoc/>
    public async Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        GetCalls++;
        SingleReadKeys.Add(key);
        if (AsyncReadDelay > TimeSpan.Zero)
        {
            await Task.Delay(AsyncReadDelay, cancellationToken).ConfigureAwait(false);
        }

        if (FailReads)
        {
            throw new InvalidOperationException("Simulated store read outage.");
        }

        return await _inner.GetAsync<TValue>(storeName, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<ReadModelEntry<TValue>> GetCompactedAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        GetCalls++;
        SingleReadKeys.Add(key);
        if (FailReads)
        {
            throw new InvalidOperationException("Simulated store read outage.");
        }

        return _inner.GetCompactedAsync<TValue>(storeName, key, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReadModelBulkEntry<TValue>>> GetManyAsync<TValue>(
        string storeName,
        IReadOnlyList<string> keys,
        int parallelism,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        BulkGetCalls++;
        BulkReadBatches.Add([.. keys]);
        if (FailReads)
        {
            throw new InvalidOperationException("Simulated store bulk-read outage.");
        }

        IReadOnlyList<ReadModelBulkEntry<TValue>> entries = await _inner
            .GetManyAsync<TValue>(storeName, keys, parallelism, cancellationToken)
            .ConfigureAwait(false);
        if (CorruptBulkResponse is not null && CorruptBulkResponse(keys))
        {
            return [new ReadModelBulkEntry<TValue>("projection:conversations:wrong:key", null, null)];
        }

        return entries;
    }

    /// <inheritdoc/>
    public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        where TValue : class
    {
        Writes.Add($"save:{key}");
        return _inner.SaveAsync(storeName, key, value, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
        where TValue : class
    {
        Writes.Add($"try-save:{key}");
        return _inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveWithTimeToLiveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        Writes.Add($"try-save-ttl:{key}");
        return _inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
    }

    /// <inheritdoc/>
    public void ValidateBatch(ReadModelBatch batch) => _inner.ValidateBatch(batch);

    /// <inheritdoc/>
    public Task<ReadModelBatchResult> ExecuteAsync(ReadModelBatch batch, CancellationToken cancellationToken = default)
    {
        Writes.Add("batch");
        return _inner.ExecuteAsync(batch, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TryEraseAsync(string storeName, string key, string etag, CancellationToken cancellationToken = default)
    {
        Writes.Add($"erase:{key}");
        return _inner.TryEraseAsync(storeName, key, etag, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<(bool Present, string Etag)> TryReadEtagAsync(string storeName, string key, CancellationToken cancellationToken = default)
    {
        GetCalls++;
        SingleReadKeys.Add(key);
        return _inner.TryReadEtagAsync(storeName, key, cancellationToken);
    }
}
