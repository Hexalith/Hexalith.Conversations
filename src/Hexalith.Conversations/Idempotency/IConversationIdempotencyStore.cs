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
    /// Completes an existing current-version pending reservation with a terminal logical outcome.
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
    /// Marks an existing pending reservation as poisoned with a retryable uncertainty outcome.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="outcome">The retryable uncertainty outcome to persist.</param>
    /// <param name="poisonedAt">The poison timestamp.</param>
    /// <param name="reservationCreatedAt">The reservation timestamp token returned by <see cref="ReserveAsync"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask MarkPoisonedAsync(
        ConversationCommandFingerprint fingerprint,
        ConversationIdempotencyOutcome outcome,
        DateTimeOffset poisonedAt,
        DateTimeOffset reservationCreatedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an existing reservation without persisting an outcome when the caller still owns the pending reservation.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="reservationCreatedAt">The reservation timestamp token returned by <see cref="ReserveAsync"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask ReleaseAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset reservationCreatedAt,
        CancellationToken cancellationToken = default);
}
