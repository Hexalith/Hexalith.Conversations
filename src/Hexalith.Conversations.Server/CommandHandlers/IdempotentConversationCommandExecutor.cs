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
                new ConversationIdempotencyReplayResult(decision.StoredOutcome),
            ConversationIdempotencyDecisionKind.Conflict => Rejection(
                ConversationErrorCode.IdempotencyConflict,
                "idempotency_conflict",
                fingerprint,
                correlationId,
                causationId),
            _ => Rejection(
                ConversationErrorCode.IdempotencyOutcomeUnknown,
                decision.ReasonCode,
                fingerprint,
                correlationId,
                causationId),
        };
    }

    private async ValueTask<DomainResult> ExecuteReservedAsync(
        ConversationCommandFingerprint fingerprint,
        DateTimeOffset now,
        Func<CancellationToken, ValueTask<DomainResult>> mutationAsync,
        Func<DomainResult, ConversationIdempotencyOutcome> outcomeFactory,
        CancellationToken cancellationToken)
    {
        DomainResult result = await mutationAsync(cancellationToken).ConfigureAwait(false);
        ConversationIdempotencyOutcome outcome = outcomeFactory(result);
        await _idempotencyStore.CompleteAsync(fingerprint, outcome, now, cancellationToken).ConfigureAwait(false);
        return result;
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
