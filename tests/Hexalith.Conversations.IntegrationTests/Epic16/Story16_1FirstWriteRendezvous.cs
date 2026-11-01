// <copyright file="Story16_1FirstWriteRendezvous.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// Holds the first create of one state key until every participating replica has reached it, so their empty-ETag
/// compare-and-swaps race through the live state store, and records how each attempt ended.
/// </summary>
/// <param name="key">The state key whose first write is raced.</param>
/// <param name="participants">The number of replicas that must arrive before any first write proceeds.</param>
internal sealed class Story16_1FirstWriteRendezvous(string key, int participants)
{
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock _gate = new();
    private int _arrivals;
    private int _won;
    private int _lost;

    /// <summary>Gets the number of replicas that attempted the first create.</summary>
    public int Arrivals => Volatile.Read(ref _arrivals);

    /// <summary>Gets the number of first creates the state store accepted.</summary>
    public int Won => Volatile.Read(ref _won);

    /// <summary>Gets the number of first creates the state store refused as a conflict.</summary>
    public int Lost => Volatile.Read(ref _lost);

    /// <summary>Waits, for a first create of the raced key, until every participant has arrived.</summary>
    /// <param name="stateKey">The key being written.</param>
    /// <param name="etag">The expected ETag; empty for a create.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True when this attempt is part of the raced first create.</returns>
    public async Task<bool> ArriveAsync(string stateKey, string etag, CancellationToken cancellationToken)
    {
        if (etag.Length != 0 || !string.Equals(stateKey, key, StringComparison.Ordinal))
        {
            return false;
        }

        lock (_gate)
        {
            if (_released.Task.IsCompleted)
            {
                return false;
            }

            if (++_arrivals == participants)
            {
                _released.TrySetResult();
            }
        }

        await _released.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>Records how a raced first create ended.</summary>
    /// <param name="saved">Whether the state store accepted it.</param>
    public void Record(bool saved)
    {
        if (saved)
        {
            Interlocked.Increment(ref _won);
        }
        else
        {
            Interlocked.Increment(ref _lost);
        }
    }
}
