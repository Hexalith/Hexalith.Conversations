// <copyright file="TenantAccessDenialKind.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Commons.TenantAccess;

/// <summary>
/// Domain-neutral tenant-access denial categories.
/// </summary>
public enum TenantAccessDenialKind
{
    /// <summary>No denial occurred.</summary>
    None = 0,

    /// <summary>No tenant binding was supplied.</summary>
    MissingTenant = 1,

    /// <summary>A tenant binding was malformed.</summary>
    MalformedTenant = 2,

    /// <summary>Tenant bindings contradicted each other.</summary>
    TenantMismatch = 3,

    /// <summary>No caller principal was supplied.</summary>
    MissingCaller = 4,

    /// <summary>The tenant projection could not be read safely.</summary>
    TenantAccessUnavailable = 5,

    /// <summary>The tenant projection is stale.</summary>
    TenantAccessStale = 6,

    /// <summary>The tenant projection has a detected sequence gap.</summary>
    TenantAccessGapDetected = 7,

    /// <summary>The tenant projection has rolled back.</summary>
    TenantAccessRolledBack = 8,

    /// <summary>The tenant projection is poisoned or contradictory.</summary>
    TenantProjectionPoisoned = 9,

    /// <summary>The tenant is unknown.</summary>
    UnknownTenant = 10,

    /// <summary>The tenant projection shape is malformed.</summary>
    MalformedProjection = 11,

    /// <summary>The tenant status is not mapped by the module.</summary>
    UnmappedStatus = 12,

    /// <summary>The tenant is disabled.</summary>
    TenantDisabled = 13,

    /// <summary>The caller is not a tenant member.</summary>
    MissingMember = 14,

    /// <summary>The caller role is not mapped by the module.</summary>
    UnmappedRole = 15,

    /// <summary>The caller role is insufficient for the requested operation.</summary>
    InsufficientRole = 16,
}
