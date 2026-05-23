// <copyright file="ConversationTenantDenialClass.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Bounded vocabulary for tenant isolation denial signal classification.
/// </summary>
public enum ConversationTenantDenialClass
{
    /// <summary>
    /// No denial (default guard value — must not be recorded as a signal).
    /// </summary>
    None = 0,

    /// <summary>
    /// Required tenant or caller context was absent.
    /// </summary>
    MissingContext = 1,

    /// <summary>
    /// Tenant was unknown or disabled.
    /// </summary>
    UnknownOrDisabled = 2,

    /// <summary>
    /// Caller role or membership was insufficient.
    /// </summary>
    InsufficientAccess = 3,

    /// <summary>
    /// Tenant projection was unavailable, stale, or rolled back.
    /// </summary>
    ProjectionUnavailable = 4,

    /// <summary>
    /// Tenant context mismatch or malformed projection.
    /// </summary>
    ContextMismatch = 5,
}
