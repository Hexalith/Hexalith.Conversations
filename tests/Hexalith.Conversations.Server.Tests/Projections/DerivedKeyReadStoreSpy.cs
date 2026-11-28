// <copyright file="DerivedKeyReadStoreSpy.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Projections;
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

    private readonly Lock _observations = new();
    private readonly List<string> _singleReadKeys = [];
    private readonly List<IReadOnlyList<string>> _bulkReadBatches = [];
    private readonly List<string> _writes = [];

    /// <summary>Gets the number of single-key reads observed.</summary>
    public int GetCalls
    {
        get
        {
            lock (_observations)
            {
                return _singleReadKeys.Count;
            }
        }
    }

    /// <summary>Gets the number of bulk read calls observed.</summary>
    public int BulkGetCalls
    {
        get
        {
            lock (_observations)
            {
                return _bulkReadBatches.Count;
            }
        }
    }

    /// <summary>Gets a snapshot of the exact single-key read keys observed in call order.</summary>
    public IReadOnlyList<string> SingleReadKeys
    {
        get
        {
            lock (_observations)
            {
                return [.. _singleReadKeys];
            }
        }
    }

    /// <summary>Gets a snapshot of the exact key batches requested through bulk reads.</summary>
    public IReadOnlyList<IReadOnlyList<string>> BulkReadBatches
    {
        get
        {
            lock (_observations)
            {
                return [.. _bulkReadBatches];
            }
        }
    }

    /// <summary>Gets a snapshot of every write or erase operation attempted against the store.</summary>
    public IReadOnlyList<string> Writes
    {
        get
        {
            lock (_observations)
            {
                return [.. _writes];
            }
        }
    }

    /// <summary>Gets or sets whether targeted reads use the typed full index as an allocation baseline.</summary>
    public bool MaterializeFullIndexForTargetedRead { get; set; }

    /// <summary>Gets or sets a value indicating whether all reads throw an <see cref="InvalidOperationException"/>.</summary>
    public bool FailReads { get; set; }

    /// <summary>Gets or sets an optional asynchronous delay before single-key reads for concurrency tests.</summary>
    public TimeSpan AsyncReadDelay { get; set; } = TimeSpan.Zero;

    /// <summary>Gets or sets an optional hook that returns a tampered bulk response for provider-fault testing.</summary>
    public Func<IReadOnlyList<string>, bool>? CorruptBulkResponse { get; set; }

    /// <summary>Resets all recorded read and write counters.</summary>
    public void Reset()
    {
        lock (_observations)
        {
            _singleReadKeys.Clear();
            _bulkReadBatches.Clear();
            _writes.Clear();
        }
    }

    /// <inheritdoc/>
    public async Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        Record(_singleReadKeys, key);
        if (AsyncReadDelay > TimeSpan.Zero)
        {
            await Task.Delay(AsyncReadDelay, cancellationToken).ConfigureAwait(false);
        }

        if (FailReads)
        {
            throw new InvalidOperationException("Simulated store read outage.");
        }

        if (MaterializeFullIndexForTargetedRead && typeof(TValue) == typeof(ConversationProjectionIndexLookupResult))
        {
            ReadModelEntry<ConversationProjectionIndexReadModel> index = await _inner
                .GetAsync<ConversationProjectionIndexReadModel>(storeName, key, cancellationToken).ConfigureAwait(false);
            ConversationProjectionIndexLookupResult? result = index.Value is null
                ? null
                : ConversationProjectionIndexLookupConverter.FromFullIndex(
                    index.Value, ConversationProjectionIndexLookupScope.Current!.ConversationId);
            return new ReadModelEntry<TValue>((TValue?)(object?)result, index.ETag);
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
        Record(_singleReadKeys, key);
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
        Record(_bulkReadBatches, keys.ToArray());
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
        Record(_writes, $"save:{key}");
        return _inner.SaveAsync(storeName, key, value, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
        where TValue : class
    {
        Record(_writes, $"try-save:{key}");
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
        Record(_writes, $"try-save-ttl:{key}");
        return _inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
    }

    /// <inheritdoc/>
    public void ValidateBatch(ReadModelBatch batch) => _inner.ValidateBatch(batch);

    /// <inheritdoc/>
    public Task<ReadModelBatchResult> ExecuteAsync(ReadModelBatch batch, CancellationToken cancellationToken = default)
    {
        Record(_writes, "batch");
        return _inner.ExecuteAsync(batch, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TryEraseAsync(string storeName, string key, string etag, CancellationToken cancellationToken = default)
    {
        Record(_writes, $"erase:{key}");
        return _inner.TryEraseAsync(storeName, key, etag, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<(bool Present, string Etag)> TryReadEtagAsync(string storeName, string key, CancellationToken cancellationToken = default)
    {
        Record(_singleReadKeys, key);
        return _inner.TryReadEtagAsync(storeName, key, cancellationToken);
    }

    private void Record<T>(List<T> observations, T observation)
    {
        lock (_observations)
        {
            observations.Add(observation);
        }
    }
}
