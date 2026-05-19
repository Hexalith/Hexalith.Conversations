// <copyright file="IdempotentConversationCommandExecutor.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.CommandHandlers;

/// <summary>
/// Coordinates idempotency reservation, replay, conflict, and completion around a guarded command mutation.
/// </summary>
public sealed class IdempotentConversationCommandExecutor
{
    private readonly IConversationIdempotencyStore _idempotencyStore;
    private readonly TimeSpan _retention;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotentConversationCommandExecutor"/> class.
    /// </summary>
    /// <param name="idempotencyStore">The idempotency store.</param>
    /// <param name="retention">The retention duration for new reservations.</param>
    public IdempotentConversationCommandExecutor(
        IConversationIdempotencyStore idempotencyStore,
        TimeSpan? retention = null)
    {
        _idempotencyStore = idempotencyStore ?? throw new ArgumentNullException(nameof(idempotencyStore));
        _retention = retention ?? TimeSpan.FromHours(24);
        if (_retention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retention), "Retention must be positive.");
        }
    }

    /// <summary>
    /// Executes a mutation only when the scoped idempotency key is newly reserved.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="now">The deterministic evaluation timestamp.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    /// <param name="causationId">The safe causation identifier.</param>
    /// <param name="mutationAsync">The guarded mutation delegate.</param>
    /// <param name="outcomeFactory">Maps the mutation result to minimal replay metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The mutation result, duplicate replay result, or typed rejection.</returns>
    public async ValueTask<DomainResult> ExecuteAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now,
        string correlationId,
        string? causationId,
        Func<CancellationToken, ValueTask<DomainResult>> mutationAsync,
        Func<DomainResult, ConversationIdempotencyOutcome> outcomeFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(mutationAsync);
        ArgumentNullException.ThrowIfNull(outcomeFactory);

        ConversationIdempotencyDecision decision = await _idempotencyStore
            .ReserveAsync(fingerprint, now, _retention, cancellationToken)
            .ConfigureAwait(false);

        return decision.Kind switch
        {
            ConversationIdempotencyDecisionKind.Reserved => await ExecuteReservedAsync(
                fingerprint,
                now,
                mutationAsync,
                outcomeFactory,
                cancellationToken).ConfigureAwait(false),
            ConversationIdempotencyDecisionKind.Duplicate when decision.StoredOutcome is not null =>
                ReplayStoredOutcome(decision.StoredOutcome, fingerprint, correlationId, causationId),
            ConversationIdempotencyDecisionKind.Conflict => Rejection(
                ConversationErrorCode.IdempotencyConflict,
                "idempotency_conflict",
                fingerprint,
                correlationId,
                causationId),
            _ => Rejection(
                ConversationErrorCode.IdempotencyOutcomeUnknown,
                CoarsePublicReason(decision.ReasonCode),
                fingerprint,
                correlationId,
                causationId),
        };
    }

    private static DomainResult ReplayStoredOutcome(
        ConversationIdempotencyOutcome outcome,
        ConversationCommandFingerprint fingerprint,
        string correlationId,
        string? causationId)
    {
        // P3 review fix: rejection replay must preserve IsRejection semantics; collapsing to empty-events NoOp
        // diverges from the original caller's pipeline branching on DomainResult.IsRejection.
        return outcome.Category switch
        {
            IdempotencyOutcomeCategory.Rejection when outcome.RejectionCode is not null => Rejection(
                outcome.RejectionCode,
                "idempotency_duplicate_rejection_replay",
                fingerprint,
                correlationId,
                causationId),
            _ => new ConversationIdempotencyReplayResult(outcome),
        };
    }

    // P8 review fix: do not reflect internal lifecycle vocabulary (idempotency_record_expired/poisoned/pending) into the public ReasonCode.
    private static string CoarsePublicReason(string internalReason)
        => internalReason switch
        {
            "idempotency_record_pending" => "idempotency_outcome_unknown",
            "idempotency_record_poisoned" => "idempotency_outcome_unknown",
            "idempotency_record_expired" => "idempotency_outcome_unknown",
            "idempotency_record_version_incompatible" => "idempotency_outcome_unknown",
            "idempotency_duplicate" => "idempotency_outcome_unknown",
            "eventstore_command_status_pending" => "idempotency_outcome_unknown",
            "eventstore_terminal_replay_required" => "idempotency_outcome_unknown",
            _ => "idempotency_outcome_unknown",
        };

    private async ValueTask<DomainResult> ExecuteReservedAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now,
        Func<CancellationToken, ValueTask<DomainResult>> mutationAsync,
        Func<DomainResult, ConversationIdempotencyOutcome> outcomeFactory,
        CancellationToken cancellationToken)
    {
        DomainResult result;
        ConversationIdempotencyOutcome outcome;
        try
        {
            result = await mutationAsync(cancellationToken).ConfigureAwait(false);
            outcome = outcomeFactory(result);
        }
        catch
        {
            // P6 review fix: an exception inside the mutation or outcomeFactory must release the reservation
            // so subsequent retries are not blocked for the entire retention window.
            await TryReleaseAsync(fingerprint, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        // P4 review fix: a retryable rejection (e.g., TenantProjectionStale, ParticipantValidationUnavailable) must not
        // be persisted as a terminal Completed record; subsequent retries should be allowed to re-attempt the mutation.
        if (outcome.IsRetryable || outcome.Category == IdempotencyOutcomeCategory.Uncertain)
        {
            await TryReleaseAsync(fingerprint, cancellationToken).ConfigureAwait(false);
            return result;
        }

        // P15 review fix: capture a fresh completion timestamp instead of reusing the reservation 'now'.
        DateTimeOffset completedAt = DateTimeOffset.UtcNow;
        if (completedAt < now)
        {
            completedAt = now;
        }

        try
        {
            await _idempotencyStore.CompleteAsync(fingerprint, outcome, completedAt, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await TryReleaseAsync(fingerprint, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        return result;
    }

    private async ValueTask TryReleaseAsync(ConversationCommandFingerprint fingerprint, CancellationToken cancellationToken)
    {
        try
        {
            await _idempotencyStore.ReleaseAsync(fingerprint, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort release: a failed release is not worse than leaving the record Pending.
            // The next retry after expiry will replace it via the Reserve eviction path (P5).
        }
    }

    private static DomainResult Rejection(
        ConversationErrorCode code,
        string reasonCode,
        ConversationCommandFingerprint fingerprint,
        string correlationId,
        string? causationId)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new ConversationRejectedDomainEvent(
                code,
                reasonCode,
                fingerprint.Scope.SchemaVersion,
                correlationId,
                causationId),
        });
}
