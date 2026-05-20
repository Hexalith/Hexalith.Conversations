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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotentConversationCommandExecutor"/> class.
    /// </summary>
    /// <param name="idempotencyStore">The idempotency store.</param>
    /// <param name="retention">The retention duration for new reservations.</param>
    /// <param name="timeProvider">The deterministic clock provider.</param>
    public IdempotentConversationCommandExecutor(
        IConversationIdempotencyStore idempotencyStore,
        TimeSpan? retention = null,
        TimeProvider? timeProvider = null)
    {
        _idempotencyStore = idempotencyStore ?? throw new ArgumentNullException(nameof(idempotencyStore));
        _retention = retention ?? TimeSpan.FromHours(24);
        _timeProvider = timeProvider ?? TimeProvider.System;
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
                decision.ReservationCreatedAt ?? now,
                correlationId,
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
                outcome.OriginalReasonCode!,
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
            "idempotency_duplicate_rejection_replay" => "idempotency_outcome_unknown",
            "eventstore_command_status_pending" => "idempotency_outcome_unknown",
            "eventstore_terminal_replay_required" => "idempotency_outcome_unknown",
            _ => "idempotency_outcome_unknown",
        };

    private async ValueTask<DomainResult> ExecuteReservedAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now,
        DateTimeOffset reservationCreatedAt,
        string correlationId,
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
            await TryMarkPoisonedAsync(
                fingerprint,
                CreateUncertainOutcome(fingerprint, correlationId),
                now,
                reservationCreatedAt,
                CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        if (outcome.IsRetryable || outcome.Category == IdempotencyOutcomeCategory.Uncertain)
        {
            ConversationIdempotencyOutcome uncertainty = outcome.Category == IdempotencyOutcomeCategory.Uncertain
                ? outcome
                : CreateUncertainOutcome(fingerprint, correlationId);
            await TryMarkPoisonedAsync(
                fingerprint,
                uncertainty,
                now,
                reservationCreatedAt,
                cancellationToken).ConfigureAwait(false);
            return result;
        }

        DateTimeOffset completedAt = _timeProvider.GetUtcNow();
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
            await TryMarkPoisonedAsync(
                fingerprint,
                CreateUncertainOutcome(fingerprint, correlationId),
                completedAt,
                reservationCreatedAt,
                CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        return result;
    }

    private async ValueTask TryMarkPoisonedAsync(
        ConversationCommandFingerprint fingerprint,
        ConversationIdempotencyOutcome outcome,
        DateTimeOffset poisonedAt,
        DateTimeOffset reservationCreatedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            await _idempotencyStore
                .MarkPoisonedAsync(fingerprint, outcome, poisonedAt, reservationCreatedAt, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Best-effort poison: a failed poison is not worse than leaving the record Pending.
        }
    }

    private static ConversationIdempotencyOutcome CreateUncertainOutcome(
        ConversationCommandFingerprint fingerprint,
        string correlationId)
        => ConversationIdempotencyOutcome.Uncertain(
            fingerprint.Scope.SchemaVersion,
            fingerprint.Scope.TenantId,
            fingerprint.Scope.CommandType,
            new Hexalith.Conversations.Contracts.Identifiers.ConversationId(fingerprint.Scope.ScopeValue),
            ConversationAuditHandle.FromServerBoundary(fingerprint, correlationId),
            ConversationAuditHandle.FromServerBoundary(fingerprint, correlationId));

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
