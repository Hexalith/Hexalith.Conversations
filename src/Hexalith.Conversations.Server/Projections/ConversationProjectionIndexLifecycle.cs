// <copyright file="ConversationProjectionIndexLifecycle.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// The lifecycle carried by a version 2 tenant conversation index, the sole Conversations lifecycle record (AD-7).
/// </summary>
/// <remarks>
/// Zero is deliberately undefined: a version 2 record without a defined lifecycle is contradictory, never
/// inferred. Erase states (<c>Erasing</c>/<c>Erased</c>) arrive with the first producer that writes them.
/// </remarks>
internal enum ConversationProjectionIndexLifecycle
{
    /// <summary>
    /// The index may not cover every conversation of the tenant: it was created by a live delivery to an absent
    /// or version 1 index, and only an explicit rebuild may make it ready.
    /// </summary>
    Rebuilding = 1,

    /// <summary>
    /// The index covers every conversation of the tenant. Only <c>TenantCreated</c> creates it, empty.
    /// </summary>
    Ready = 2,
}
