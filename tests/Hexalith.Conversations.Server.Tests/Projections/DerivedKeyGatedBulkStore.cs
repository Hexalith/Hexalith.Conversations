// <copyright file="DerivedKeyGatedBulkStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Fakes;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Test store wrapper that independently gates detail and ledger bulk reads so tests can deterministically
/// verify read ordering during detail restoration and prove no sibling reads leak on failure.
/// </summary>
/// <param name="inner">The backing in-memory read-model store.</param>
internal sealed class DerivedKeyGatedBulkStore(InMemoryReadModelStore inner)
    : IReadModelStore,
      IReadModelBulkStore
{
    private readonly InMemoryReadModelStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>Gets the number of detail bulk reads started.</summary>
    public int DetailBulkCalls { get; private set; }

    /// <summary>Gets the number of ledger bulk reads started.</summary>
    public int LedgerBulkCalls { get; private set; }

    /// <summary>Gets or sets an optional hook invoked before delegating a detail bulk read.</summary>
    public Func<CancellationToken, Task>? OnDetailBulkReadAsync { get; set; }

    /// <summary>Gets or sets an optional hook invoked before delegating a ledger bulk read.</summary>
    public Func<CancellationToken, Task>? OnLedgerBulkReadAsync { get; set; }

    /// <inheritdoc/>
    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
        => _inner.GetAsync<TValue>(storeName, key, cancellationToken);

    /// <inheritdoc/>
    public Task<ReadModelEntry<TValue>> GetCompactedAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
        => _inner.GetCompactedAsync<TValue>(storeName, key, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReadModelBulkEntry<TValue>>> GetManyAsync<TValue>(
        string storeName,
        IReadOnlyList<string> keys,
        int parallelism,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        bool isLedger = keys.Count > 0 && keys[0].StartsWith("projection:conversations-dispatch:", StringComparison.Ordinal);
        if (isLedger)
        {
            LedgerBulkCalls++;
            if (OnLedgerBulkReadAsync is not null)
            {
                await OnLedgerBulkReadAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            DetailBulkCalls++;
            if (OnDetailBulkReadAsync is not null)
            {
                await OnDetailBulkReadAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return await _inner.GetManyAsync<TValue>(storeName, keys, parallelism, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        where TValue : class
        => _inner.SaveAsync(storeName, key, value, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
        where TValue : class
        => _inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
}
