// <copyright file="ConversationTenantAccessRequirement.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Defines the tenant-scoped operation class being authorized.
/// </summary>
public enum ConversationTenantAccessRequirement
{
    /// <summary>
    /// Read-only access to conversation projections or safe metadata.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Write access before aggregate loading or command dispatch.
    /// </summary>
    Write = 1,

    /// <summary>
    /// Administrative access for audit-sensitive or operational metadata.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Governed mutation access for retention, redaction, sensitivity, archival, and audit-policy changes.
    /// </summary>
    Governance = 3,
}
