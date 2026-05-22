// <copyright file="ConversationPersistenceOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Describes whether a command produced a durable state-changing fact eligible for publication.
/// </summary>
public enum ConversationPersistenceOutcome
{
    /// <summary>
    /// The command succeeded and persistence completed.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// The command was rejected.
    /// </summary>
    RejectedCommand = 1,

    /// <summary>
    /// The idempotency path returned a no-op replay.
    /// </summary>
    NoOpIdempotentReplay = 2,

    /// <summary>
    /// The idempotency path detected a conflict.
    /// </summary>
    IdempotencyConflict = 3,

    /// <summary>
    /// Persistence failed before a durable state change existed.
    /// </summary>
    FailedPersistence = 4,

    /// <summary>
    /// Tenant validation failed.
    /// </summary>
    FailedTenantCheck = 5,

    /// <summary>
    /// Participant validation failed.
    /// </summary>
    FailedParticipantValidation = 6,
}
