// <copyright file="IConversationIdempotencyStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Reserves and completes scoped Conversations idempotency records atomically.
/// </summary>
public interface IConversationIdempotencyStore
{
    /// <summary>
    /// Reserves the scoped idempotency key or returns the existing key decision.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="now">The evaluation timestamp.</param>
    /// <param name="retention">The retention duration for newly reserved records.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The idempotency decision.</returns>
    ValueTask<ConversationIdempotencyDecision> ReserveAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now,
        TimeSpan retention,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an existing reservation with a terminal logical outcome.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="outcome">The terminal logical outcome. Must not have <see cref="IdempotencyOutcomeCategory.Uncertain"/>.</param>
    /// <param name="completedAt">The completion timestamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask CompleteAsync(
        ConversationCommandFingerprint fingerprint,
        ConversationIdempotencyOutcome outcome,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an existing reservation without persisting a terminal outcome. Use when the mutation produced a retryable
    /// rejection or threw an exception, so a subsequent retry can re-acquire the scoped key without waiting for retention expiry.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask ReleaseAsync(
        ConversationCommandFingerprint fingerprint,
        CancellationToken cancellationToken = default);
}
