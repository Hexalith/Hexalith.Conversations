// <copyright file="DerivedKeyDaprFaultStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Integration;

namespace Hexalith.Conversations.AppHost.Tests;

/// <summary>
/// Wraps the production <see cref="DaprReadModelStore"/> over live <c>daprd</c> + Redis to inject controlled
/// partial-write, latency, and outage faults while tracking every attempted write.
/// </summary>
/// <param name="inner">The live production Dapr read-model store.</param>
internal sealed class DerivedKeyDaprFaultStore(DaprReadModelStore inner)
    : DaprReadModelStoreDecorator(inner),
      IReadModelBulkStore,
      IReadModelExpiringStore,
      IReadModelConditionalEraser,
      IReadModelBatchStore,
      IReadModelBatchStagingStore
{
    private readonly Lock _gate = new();
    private readonly List<string> _writes = [];
    private int _getCalls;
    private int _getManyCalls;

    /// <summary>Gets or sets a key whose writes are rejected to simulate partial writes.</summary>
    public string? BlockedWriteKey { get; set; }

    /// <summary>Gets or sets an action to run immediately before calling the live bulk-read adapter.</summary>
    public Func<Task>? BeforeBulkReadAsync { get; set; }

    /// <summary>Gets or sets an artificial read latency injected before delegating to Dapr.</summary>
    public TimeSpan ReadLatency { get; set; } = TimeSpan.Zero;

    /// <summary>Gets the number of single-key reads observed since the last reset.</summary>
    public int GetCalls
    {
        get
        {
            lock (_gate)
            {
                return _getCalls;
            }
        }
    }

    /// <summary>Gets the number of bulk reads observed since the last reset.</summary>
    public int GetManyCalls
    {
        get
        {
            lock (_gate)
            {
                return _getManyCalls;
            }
        }
    }

    /// <summary>Gets a snapshot of all recorded write operations.</summary>
    public IReadOnlyList<string> Writes
    {
        get
        {
            lock (_gate)
            {
                return [.. _writes];
            }
        }
    }

    /// <summary>Clears all recorded read counts and write operations.</summary>
    public void ResetWrites()
    {
        lock (_gate)
        {
            _writes.Clear();
            _getCalls = 0;
            _getManyCalls = 0;
        }
    }

    /// <inheritdoc/>
    public override async Task<ReadModelEntry<T>> GetAsync<T>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where T : class
    {
        lock (_gate)
        {
            _getCalls++;
        }

        if (ReadLatency > TimeSpan.Zero)
        {
            await Task.Delay(ReadLatency, cancellationToken).ConfigureAwait(false);
        }

        return await base.GetAsync<T>(storeName, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<ReadModelEntry<T>> GetCompactedAsync<T>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where T : class
    {
        lock (_gate)
        {
            _getCalls++;
        }

        if (ReadLatency > TimeSpan.Zero)
        {
            await Task.Delay(ReadLatency, cancellationToken).ConfigureAwait(false);
        }

        return await base.GetCompactedAsync<T>(storeName, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    async Task<IReadOnlyList<ReadModelBulkEntry<T>>> IReadModelBulkStore.GetManyAsync<T>(
        string storeName,
        IReadOnlyList<string> keys,
        int parallelism,
        CancellationToken cancellationToken)
        where T : class
    {
        lock (_gate)
        {
            _getManyCalls++;
        }

        if (ReadLatency > TimeSpan.Zero)
        {
            await Task.Delay(ReadLatency, cancellationToken).ConfigureAwait(false);
        }

        if (BeforeBulkReadAsync is { } beforeRead)
        {
            await beforeRead().ConfigureAwait(false);
        }

        return await base.GetManyAsync<T>(storeName, keys, parallelism, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        RecordAndCheckWrite("save", key);
        return base.SaveAsync(storeName, key, value, cancellationToken);
    }

    /// <inheritdoc/>
    public override Task<bool> TrySaveAsync<T>(
        string storeName,
        string key,
        T value,
        string etag,
        CancellationToken cancellationToken = default)
        where T : class
    {
        RecordAndCheckWrite("try-save", key);
        return base.TrySaveAsync(storeName, key, value, etag, cancellationToken);
    }

    /// <inheritdoc/>
    Task<bool> IReadModelExpiringStore.TrySaveWithTimeToLiveAsync<T>(
        string storeName,
        string key,
        T value,
        string etag,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
        where T : class
    {
        RecordAndCheckWrite("try-save-ttl", key);
        return base.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
    }

    /// <inheritdoc/>
    Task<bool> IReadModelConditionalEraser.TryEraseAsync(
        string storeName,
        string key,
        string etag,
        CancellationToken cancellationToken)
    {
        RecordAndCheckWrite("try-erase", key);
        return base.TryEraseAsync(storeName, key, etag, cancellationToken);
    }

    /// <inheritdoc/>
    Task<ReadModelBatchResult> IReadModelBatchStore.ExecuteAsync(
        ReadModelBatch batch,
        CancellationToken cancellationToken)
    {
        RecordAndCheckBatch("batch-execute", batch);
        return base.ExecuteAsync(batch, cancellationToken);
    }

    /// <inheritdoc/>
    Task<ReadModelBatchStagingResult> IReadModelBatchStagingStore.StageAsync(
        ReadModelBatch batch,
        CancellationToken cancellationToken)
    {
        RecordAndCheckBatch("batch-stage", batch);
        return base.StageAsync(batch, cancellationToken);
    }

    /// <inheritdoc/>
    Task<ReadModelBatchStagingResult> IReadModelBatchStagingStore.CommitAsync(
        ReadModelBatch batch,
        CancellationToken cancellationToken)
    {
        RecordAndCheckBatch("batch-commit", batch);
        return base.CommitAsync(batch, cancellationToken);
    }

    /// <inheritdoc/>
    Task<ReadModelBatchStagingResult> IReadModelBatchStagingStore.AbortAsync(
        ReadModelBatch batch,
        CancellationToken cancellationToken)
    {
        RecordAndCheckBatch("batch-abort", batch);
        return base.AbortAsync(batch, cancellationToken);
    }

    private void RecordAndCheckBatch(string operation, ReadModelBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        lock (_gate)
        {
            foreach (ReadModelBatchOperation item in batch.Operations)
            {
                _writes.Add($"{operation}:{item.Key}");
            }
        }

        foreach (ReadModelBatchOperation item in batch.Operations)
        {
            if (string.Equals(item.Key, BlockedWriteKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Simulated batch failure for key '{item.Key}' in batch '{batch.Scope.BatchId}'.");
            }
        }
    }

    private void RecordAndCheckWrite(string operation, string key)
    {
        lock (_gate)
        {
            _writes.Add($"{operation}:{key}");
        }

        if (string.Equals(key, BlockedWriteKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Simulated partial-write failure for key '{key}'.");
        }
    }
}
