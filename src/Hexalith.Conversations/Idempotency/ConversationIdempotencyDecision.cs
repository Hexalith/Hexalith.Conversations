// <copyright file="ConversationIdempotencyDecision.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Reports the atomic decision for a scoped idempotency key evaluation.
/// </summary>
/// <param name="Kind">The idempotency decision kind.</param>
/// <param name="StoredOutcome">The stored logical outcome for duplicates, when available.</param>
/// <param name="ReasonCode">The safe internal reason code.</param>
public sealed record ConversationIdempotencyDecision(
    ConversationIdempotencyDecisionKind Kind,
    ConversationIdempotencyOutcome? StoredOutcome,
    string ReasonCode)
{
    /// <summary>
    /// Gets the safe internal reason code.
    /// </summary>
    public string ReasonCode { get; } = ValidateRequired(ReasonCode, nameof(ReasonCode));

    /// <summary>
    /// Creates a reserved decision.
    /// </summary>
    /// <returns>The reserved decision.</returns>
    public static ConversationIdempotencyDecision Reserved()
        => new(ConversationIdempotencyDecisionKind.Reserved, StoredOutcome: null, "idempotency_reserved");

    /// <summary>
    /// Creates a duplicate decision.
    /// </summary>
    /// <param name="outcome">The stored logical outcome. Must be non-null and represent a terminal category (Success, NoOp, or Rejection).</param>
    /// <returns>The duplicate decision.</returns>
    public static ConversationIdempotencyDecision Duplicate(ConversationIdempotencyOutcome outcome)
    {
        // P17 review fix: previously Duplicate accepted any (or null) outcome; a buggy store could leak a malformed duplicate.
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.Category == IdempotencyOutcomeCategory.Uncertain)
        {
            throw new ArgumentException(
                "Duplicate idempotency decision requires a terminal outcome category (Success, NoOp, or Rejection); 'Uncertain' is non-terminal.",
                nameof(outcome));
        }

        return new(ConversationIdempotencyDecisionKind.Duplicate, outcome, "idempotency_duplicate");
    }

    /// <summary>
    /// Creates a conflict decision.
    /// </summary>
    /// <returns>The conflict decision.</returns>
    public static ConversationIdempotencyDecision Conflict()
        => new(ConversationIdempotencyDecisionKind.Conflict, StoredOutcome: null, "idempotency_conflict");

    /// <summary>
    /// Creates a retryable uncertainty decision.
    /// </summary>
    /// <param name="reasonCode">The safe internal reason code.</param>
    /// <returns>The retryable uncertainty decision.</returns>
    public static ConversationIdempotencyDecision RetryableUncertainty(string reasonCode)
        => new(ConversationIdempotencyDecisionKind.RetryableUncertainty, StoredOutcome: null, reasonCode);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
