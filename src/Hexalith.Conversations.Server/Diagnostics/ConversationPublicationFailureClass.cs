// <copyright file="ConversationPublicationFailureClass.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Bounded vocabulary for publication failure signal classification.
/// </summary>
public enum ConversationPublicationFailureClass
{
    /// <summary>
    /// No failure class (default guard value — must not be recorded as a signal).
    /// </summary>
    None = 0,

    /// <summary>
    /// Transient infrastructure or validation failure — retry may succeed.
    /// </summary>
    TransientFailure = 1,

    /// <summary>
    /// Subscriber schema version is not supported by the publisher contract.
    /// </summary>
    UnsupportedSchema = 2,

    /// <summary>
    /// Event has been moved to the dead-letter channel after exhausted retries.
    /// </summary>
    DeadLettered = 3,

    /// <summary>
    /// Event requires replay before publication can proceed.
    /// </summary>
    ReplayRequired = 4,

    /// <summary>
    /// Tenant context mismatch or cross-tenant isolation violation detected.
    /// </summary>
    TenantViolation = 5,
}
