// <copyright file="InMemoryConversationIdempotencyStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// In-memory idempotency record store used for local deterministic command-flow evidence.
/// </summary>
public sealed class InMemoryConversationIdempotencyStore : IConversationIdempotencyStore
{
    private readonly object _gate = new();
    private readonly Dictionary<ConversationIdempotencyScope, ConversationIdempotencyRecord> _records = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConversationIdempotencyStore"/> class.
    /// </summary>
    public InMemoryConversationIdempotencyStore()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConversationIdempotencyStore"/> class with seed records.
    /// </summary>
    /// <param name="records">The seed records for local evidence tests.</param>
    public InMemoryConversationIdempotencyStore(IEnumerable<ConversationIdempotencyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        foreach (ConversationIdempotencyRecord record in records)
        {
            _records.Add(record.Scope, record);
        }
    }

    /// <inheritdoc />
    public ValueTask<ConversationIdempotencyDecision> ReserveAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(fingerprint);

        lock (_gate)
        {
            if (!_records.TryGetValue(fingerprint.Scope, out ConversationIdempotencyRecord? existing))
            {
                _records.Add(fingerprint.Scope, ConversationIdempotencyRecord.Pending(fingerprint, now, retention));
                return ValueTask.FromResult(ConversationIdempotencyDecision.Reserved(now));
            }

            if (existing.ExpiresAt <= now)
            {
                if (existing.Status == ConversationIdempotencyRecordStatus.Pending)
                {
                    _records[fingerprint.Scope] = ConversationIdempotencyRecord.Pending(fingerprint, now, retention);
                    return ValueTask.FromResult(ConversationIdempotencyDecision.Reserved(now));
                }

                return ValueTask.FromResult(ConversationIdempotencyDecision.RetryableUncertainty("idempotency_record_expired"));
            }

            return ValueTask.FromResult(EvaluateExisting(existing, fingerprint, now));
        }
    }

    /// <inheritdoc />
    public ValueTask CompleteAsync(
        ConversationCommandFingerprint fingerprint,
        ConversationIdempotencyOutcome outcome,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(outcome);

        // P25 review fix: 'Uncertain' is non-terminal by definition; persisting it as Completed produces a sticky NoOp replay.
        if (outcome.Category == IdempotencyOutcomeCategory.Uncertain)
        {
            throw new InvalidOperationException(
                "Cannot complete an idempotency record with an Uncertain outcome category; Uncertain is non-terminal.");
        }

        lock (_gate)
        {
            if (!_records.TryGetValue(fingerprint.Scope, out ConversationIdempotencyRecord? existing))
            {
                throw new InvalidOperationException("Cannot complete an idempotency key that was not reserved.");
            }

            if (existing.RecordVersion != ConversationIdempotencyRecord.CurrentRecordVersion)
            {
                throw new InvalidOperationException("Cannot complete an idempotency record with an unsupported record version.");
            }

            if (existing.Status != ConversationIdempotencyRecordStatus.Pending)
            {
                throw new InvalidOperationException("Cannot complete an idempotency record that is not pending.");
            }

            if (existing.Fingerprint != fingerprint.PayloadFingerprint)
            {
                throw new InvalidOperationException("Cannot complete an idempotency key with a different fingerprint.");
            }

            // P16 review fix: a record whose retention has elapsed must not be silently overwritten with a 'Completed' status
            // because the next Reserve will evict it (P5) and the side effects of the mutation would be lost.
            if (existing.ExpiresAt <= completedAt)
            {
                throw new InvalidOperationException(
                    "Cannot complete an idempotency record whose retention window has already elapsed; the reservation expired before the mutation finished.");
            }

            _records[fingerprint.Scope] = existing.Complete(outcome, completedAt);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask MarkPoisonedAsync(
        ConversationCommandFingerprint fingerprint,
        ConversationIdempotencyOutcome outcome,
        DateTimeOffset poisonedAt,
        DateTimeOffset reservationCreatedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.Category != IdempotencyOutcomeCategory.Uncertain)
        {
            throw new InvalidOperationException("Poisoned idempotency records require an Uncertain outcome.");
        }

        lock (_gate)
        {
            if (_records.TryGetValue(fingerprint.Scope, out ConversationIdempotencyRecord? existing)
                && existing.Status == ConversationIdempotencyRecordStatus.Pending
                && existing.RecordVersion == ConversationIdempotencyRecord.CurrentRecordVersion
                && existing.CreatedAt == reservationCreatedAt
                && existing.Fingerprint == fingerprint.PayloadFingerprint)
            {
                _records[fingerprint.Scope] = existing.Poison(outcome, poisonedAt);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ReleaseAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset reservationCreatedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(fingerprint);

        lock (_gate)
        {
            if (_records.TryGetValue(fingerprint.Scope, out ConversationIdempotencyRecord? existing)
                && existing.Status == ConversationIdempotencyRecordStatus.Pending
                && existing.CreatedAt == reservationCreatedAt
                && existing.Fingerprint == fingerprint.PayloadFingerprint)
            {
                _records.Remove(fingerprint.Scope);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Gets a stable snapshot of current records for local evidence assertions.
    /// </summary>
    /// <returns>The current idempotency records.</returns>
    public IReadOnlyList<ConversationIdempotencyRecord> SnapshotRecords()
    {
        lock (_gate)
        {
            return _records.Values.ToArray();
        }
    }

    private static ConversationIdempotencyDecision EvaluateExisting(
        ConversationIdempotencyRecord existing,
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now)
    {
        if (existing.RecordVersion != ConversationIdempotencyRecord.CurrentRecordVersion)
        {
            return ConversationIdempotencyDecision.RetryableUncertainty("idempotency_record_version_incompatible");
        }

        // P21 review note: clock skew where now < existing.CreatedAt is tolerated; the in-memory fake trusts the caller's now,
        // matching the EventStore command-status semantics that already accept whatever monotonic boundary the host provides.
        // The Reserve path treats expiry (existing.ExpiresAt <= now) as evict-and-replace, so callers escape the lock eventually
        // even if a later 'now' arrives. Producers that supply addedAt from upstream events must validate monotonicity upstream.
        if (existing.ExpiresAt <= now)
        {
            return ConversationIdempotencyDecision.RetryableUncertainty("idempotency_record_expired");
        }

        if (existing.Fingerprint != fingerprint.PayloadFingerprint)
        {
            return ConversationIdempotencyDecision.Conflict();
        }

        return existing.Status switch
        {
            ConversationIdempotencyRecordStatus.Completed => ConversationIdempotencyDecision.Duplicate(existing.Outcome!),
            ConversationIdempotencyRecordStatus.Pending => ConversationIdempotencyDecision.RetryableUncertainty("idempotency_record_pending"),
            ConversationIdempotencyRecordStatus.Poisoned => ConversationIdempotencyDecision.RetryableUncertainty("idempotency_record_poisoned"),
            ConversationIdempotencyRecordStatus.VersionIncompatible => ConversationIdempotencyDecision.RetryableUncertainty("idempotency_record_version_incompatible"),
            _ => ConversationIdempotencyDecision.RetryableUncertainty("idempotency_record_unknown"),
        };
    }
}
