// <copyright file="ConversationTenantAccessDenialReason.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Describes internal tenant-access denial reasons without protected content.
/// </summary>
public enum ConversationTenantAccessDenialReason
{
    /// <summary>
    /// Access was allowed.
    /// </summary>
    None = 0,

    /// <summary>
    /// No tenant context was available.
    /// </summary>
    MissingTenant = 1,

    /// <summary>
    /// A tenant identifier was blank, prefixed, lossy, or otherwise not canonical.
    /// </summary>
    MalformedTenant = 2,

    /// <summary>
    /// No caller principal was available.
    /// </summary>
    MissingCaller = 3,

    /// <summary>
    /// The local tenant projection had no matching tenant.
    /// </summary>
    UnknownTenant = 4,

    /// <summary>
    /// The tenant is disabled.
    /// </summary>
    TenantDisabled = 5,

    /// <summary>
    /// The caller is not a member of the tenant.
    /// </summary>
    MissingMember = 6,

    /// <summary>
    /// The caller's mapped role is not sufficient.
    /// </summary>
    InsufficientRole = 7,

    /// <summary>
    /// The caller role is outside the closed-world mapping.
    /// </summary>
    UnmappedRole = 8,

    /// <summary>
    /// The tenant status is outside the closed-world mapping.
    /// </summary>
    UnmappedStatus = 9,

    /// <summary>
    /// The projection store was unavailable or threw.
    /// </summary>
    TenantAccessUnavailable = 10,

    /// <summary>
    /// The projection signaled stale or lagging state.
    /// </summary>
    TenantAccessStale = 11,

    /// <summary>
    /// The projection signaled a sequence gap.
    /// </summary>
    TenantAccessGapDetected = 12,

    /// <summary>
    /// The projection signaled a rollback or watermark regression.
    /// </summary>
    TenantAccessRolledBack = 13,

    /// <summary>
    /// Tenant-bearing inputs or projection state did not match.
    /// </summary>
    TenantMismatch = 14,

    /// <summary>
    /// The projection record shape was malformed.
    /// </summary>
    MalformedProjection = 15,

    /// <summary>
    /// The projection state was ambiguous or poisoned.
    /// </summary>
    TenantProjectionPoisoned = 16,
}
