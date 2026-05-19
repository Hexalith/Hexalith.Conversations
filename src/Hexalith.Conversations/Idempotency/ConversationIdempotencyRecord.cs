// <copyright file="ConversationIdempotencyRecord.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Represents the minimal versioned metadata retained for a scoped idempotency key.
/// </summary>
/// <param name="Scope">The scoped key.</param>
/// <param name="Fingerprint">The bounded canonical fingerprint.</param>
/// <param name="Status">The internal lifecycle status.</param>
/// <param name="CreatedAt">The reservation timestamp.</param>
/// <param name="UpdatedAt">The last update timestamp.</param>
/// <param name="ExpiresAt">The retention expiry timestamp.</param>
/// <param name="RecordVersion">The idempotency record schema version.</param>
/// <param name="Outcome">The terminal logical outcome, when available.</param>
public sealed record ConversationIdempotencyRecord(
    ConversationIdempotencyScope Scope,
    ConversationPayloadFingerprint Fingerprint,
    ConversationIdempotencyRecordStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset ExpiresAt,
    int RecordVersion,
    ConversationIdempotencyOutcome? Outcome = null)
{
    /// <summary>
    /// Gets the current idempotency record schema version.
    /// </summary>
    public const int CurrentRecordVersion = 1;

    /// <summary>
    /// Gets the scoped key.
    /// </summary>
    public ConversationIdempotencyScope Scope { get; } = Scope ?? throw new ArgumentNullException(nameof(Scope));

    /// <summary>
    /// Gets the bounded canonical fingerprint.
    /// </summary>
    public ConversationPayloadFingerprint Fingerprint { get; } =
        Fingerprint ?? throw new ArgumentNullException(nameof(Fingerprint));

    /// <summary>
    /// Gets the terminal logical outcome, when available.
    /// </summary>
    public ConversationIdempotencyOutcome? Outcome { get; init; } = ValidateOutcome(Status, Outcome);

    /// <summary>
    /// Creates a pending reservation record.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="now">The reservation timestamp.</param>
    /// <param name="retention">The record retention duration.</param>
    /// <returns>The pending reservation record.</returns>
    public static ConversationIdempotencyRecord Pending(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now,
        TimeSpan retention)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        if (retention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retention), "Retention must be positive.");
        }

        return new ConversationIdempotencyRecord(
            fingerprint.Scope,
            fingerprint.PayloadFingerprint,
            ConversationIdempotencyRecordStatus.Pending,
            now,
            now,
            now.Add(retention),
            CurrentRecordVersion);
    }

    /// <summary>
    /// Creates a completed copy of the record.
    /// </summary>
    /// <param name="outcome">The terminal logical outcome.</param>
    /// <param name="completedAt">The completion timestamp.</param>
    /// <returns>The completed record.</returns>
    public ConversationIdempotencyRecord Complete(ConversationIdempotencyOutcome outcome, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return this with
        {
            Status = ConversationIdempotencyRecordStatus.Completed,
            UpdatedAt = completedAt,
            Outcome = outcome,
        };
    }

    /// <inheritdoc />
    public override string ToString()
        => "IdempotencyRecord { "
            + $"Tenant = {Scope.TenantId.Value}, "
            + $"Command = {Scope.CommandType.Value}, "
            + $"ScopeKind = {Scope.ScopeKind}, "
            + $"ScopeValue = {Scope.ScopeValue}, "
            + $"Key = {Scope.IdempotencyKey}, "
            + $"Fingerprint = {Fingerprint.Algorithm}:{Fingerprint.Value}, "
            + $"Status = {Status}, "
            + $"Version = {RecordVersion}, "
            + $"HasOutcome = {Outcome is not null} "
            + "}";

    private static ConversationIdempotencyOutcome? ValidateOutcome(
        ConversationIdempotencyRecordStatus status,
        ConversationIdempotencyOutcome? outcome)
        => status == ConversationIdempotencyRecordStatus.Completed && outcome is null
            ? throw new ArgumentException("Completed idempotency records require an outcome.", nameof(outcome))
            : outcome;
}
