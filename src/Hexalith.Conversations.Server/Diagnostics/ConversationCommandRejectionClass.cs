// <copyright file="ConversationCommandRejectionClass.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Bounded vocabulary for command rejection signal classification.
/// </summary>
public enum ConversationCommandRejectionClass
{
    /// <summary>
    /// No rejection (default guard value — must not be recorded as a signal).
    /// </summary>
    None = 0,

    /// <summary>
    /// Command schema or semantic validation failed.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// Tenant binding was missing or malformed.
    /// </summary>
    TenantBinding = 2,

    /// <summary>
    /// Tenant isolation check denied the request.
    /// </summary>
    TenantIsolation = 3,

    /// <summary>
    /// Tenant projection was unavailable or stale.
    /// </summary>
    TenantProjectionUnavailable = 4,

    /// <summary>
    /// Idempotency key conflict or missing.
    /// </summary>
    Idempotency = 5,

    /// <summary>
    /// Audit recording was unavailable or pairing required.
    /// </summary>
    AuditUnavailable = 6,

    /// <summary>
    /// Governance policy blocked the command.
    /// </summary>
    PolicyRejection = 7,

    /// <summary>
    /// Infrastructure or provider dependency unavailable.
    /// </summary>
    Infrastructure = 8,
}
