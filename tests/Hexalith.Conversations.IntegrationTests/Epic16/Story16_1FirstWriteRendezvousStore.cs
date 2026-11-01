// <copyright file="Story16_1FirstWriteRendezvousStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// Delegates to the live Dapr-backed store, holding the raced first create at a rendezvous so replicas collide on it.
/// </summary>
/// <param name="inner">The live Dapr-backed read-model store.</param>
/// <param name="rendezvous">The first-write rendezvous shared by the racing replicas.</param>
internal sealed class Story16_1FirstWriteRendezvousStore(IReadModelStore inner, Story16_1FirstWriteRendezvous rendezvous) : IReadModelStore
{
    /// <inheritdoc />
    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
        where TValue : class
        => inner.GetAsync<TValue>(storeName, key, cancellationToken);

    /// <inheritdoc />
    public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        where TValue : class
        => inner.SaveAsync(storeName, key, value, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
        where TValue : class
    {
        bool raced = await rendezvous.ArriveAsync(key, etag, cancellationToken).ConfigureAwait(false);
        bool saved = await inner.TrySaveAsync(storeName, key, value, etag, cancellationToken).ConfigureAwait(false);
        if (raced)
        {
            rendezvous.Record(saved);
        }

        return saved;
    }
}
