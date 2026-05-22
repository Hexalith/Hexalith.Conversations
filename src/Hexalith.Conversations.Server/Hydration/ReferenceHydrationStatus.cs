// <copyright file="ReferenceHydrationStatus.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Internal adapter outcome vocabulary for read-time reference hydration.
/// </summary>
public enum ReferenceHydrationStatus
{
    Current,
    Stale,
    Rebuilding,
    Unavailable,
    Timeout,
    Throttled,
    Forbidden,
    NotFound,
    Gone,
    Deleted,
    CrossTenantDenied,
    PolicyFiltered,
    Erased,
    Redacted,
}
