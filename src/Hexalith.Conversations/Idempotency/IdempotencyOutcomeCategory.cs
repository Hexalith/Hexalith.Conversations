// <copyright file="IdempotencyOutcomeCategory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Defines the stable logical outcome categories stored for idempotent replay.
/// </summary>
public enum IdempotencyOutcomeCategory
{
    /// <summary>
    /// The command produced one or more successful domain events.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The command completed without a state mutation.
    /// </summary>
    NoOp = 1,

    /// <summary>
    /// The command produced a typed domain rejection.
    /// </summary>
    Rejection = 2,

    /// <summary>
    /// The command terminal state is not yet known to this boundary.
    /// </summary>
    Uncertain = 3,
}
