// <copyright file="Story16_3FailureStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Integration;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>Injects a controlled persistence failure after the production pending marker is durable.</summary>
/// <param name="inner">The configured production Dapr adapter.</param>
internal sealed class Story16_3FailureStore(DaprReadModelStore inner) : DaprReadModelStoreDecorator(inner)
{
    private string? _blockedKey;

    /// <summary>Gets the number of attempted writes refused by the injection.</summary>
    public int Failures { get; private set; }

    /// <summary>Blocks the exact selected detail key until recovery is explicitly enabled.</summary>
    public void Block(string key) => _blockedKey = key;

    /// <summary>Restores the configured SDK store without changing any persisted value.</summary>
    public void Release() => _blockedKey = null;

    /// <inheritdoc />
    public override Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        FailIfSelected(key);
        return base.SaveAsync(storeName, key, value, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        FailIfSelected(key);
        return base.TrySaveAsync(storeName, key, value, etag, cancellationToken);
    }

    private void FailIfSelected(string key)
    {
        if (key == _blockedKey)
        {
            Failures++;
            // ConversationAsyncProjectionHandler classifies ArgumentException as terminal HandlerFailure
            // (InvalidOperationException is retryable and other exceptions indeterminate). This lane therefore
            // proves recovery of handler-terminal work, not of a transient store outage.
            throw new ArgumentException("Controlled first projection persistence failure.");
        }
    }
}
