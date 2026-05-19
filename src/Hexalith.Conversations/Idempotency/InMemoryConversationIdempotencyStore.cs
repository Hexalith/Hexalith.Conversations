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
                return ValueTask.FromResult(ConversationIdempotencyDecision.Reserved());
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

        lock (_gate)
        {
            if (!_records.TryGetValue(fingerprint.Scope, out ConversationIdempotencyRecord? existing))
            {
                throw new InvalidOperationException("Cannot complete an idempotency key that was not reserved.");
            }

            if (existing.Fingerprint != fingerprint.PayloadFingerprint)
            {
                throw new InvalidOperationException("Cannot complete an idempotency key with a different fingerprint.");
            }

            _records[fingerprint.Scope] = existing.Complete(outcome, completedAt);
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
