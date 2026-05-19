// <copyright file="ConversationIdempotencyDecisionKind.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Defines the possible decisions when evaluating a scoped idempotency key.
/// </summary>
public enum ConversationIdempotencyDecisionKind
{
    /// <summary>
    /// The caller reserved the key and may perform the business mutation.
    /// </summary>
    Reserved = 0,

    /// <summary>
    /// The caller matched a terminal completed record and may replay the stored logical outcome.
    /// </summary>
    Duplicate = 1,

    /// <summary>
    /// The caller reused the scoped key with incompatible command meaning.
    /// </summary>
    Conflict = 2,

    /// <summary>
    /// The key is pending, expired, poisoned, stale, or otherwise uncertain and should be retried safely.
    /// </summary>
    RetryableUncertainty = 3,
}
