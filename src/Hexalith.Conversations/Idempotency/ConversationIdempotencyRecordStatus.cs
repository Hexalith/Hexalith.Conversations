// <copyright file="ConversationIdempotencyRecordStatus.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Defines the internal lifecycle state of a scoped idempotency record.
/// </summary>
public enum ConversationIdempotencyRecordStatus
{
    /// <summary>
    /// The record has reserved the key but no terminal logical outcome is available yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The record contains a terminal logical outcome.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// The record is known to be unsafe and must resolve from authoritative history or return uncertainty.
    /// </summary>
    Poisoned = 2,

    /// <summary>
    /// The record version cannot be interpreted by this implementation.
    /// </summary>
    VersionIncompatible = 3,
}
