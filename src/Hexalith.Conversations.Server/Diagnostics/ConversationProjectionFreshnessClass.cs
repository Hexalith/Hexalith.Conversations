// <copyright file="ConversationProjectionFreshnessClass.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Bounded vocabulary for projection freshness signal classification.
/// </summary>
public enum ConversationProjectionFreshnessClass
{
    /// <summary>
    /// No freshness class (default guard value — must not be recorded as a signal).
    /// </summary>
    None = 0,

    /// <summary>
    /// Projection is current and within freshness threshold.
    /// </summary>
    Current = 1,

    /// <summary>
    /// Projection is stale beyond the accepted freshness threshold.
    /// </summary>
    Stale = 2,

    /// <summary>
    /// Projection is actively rebuilding.
    /// </summary>
    Rebuilding = 3,

    /// <summary>
    /// Projection is unavailable or the store is unreachable.
    /// </summary>
    Unavailable = 4,

    /// <summary>
    /// Projection has been partially rebuilt (reserved for future use in v1).
    /// </summary>
    PartiallyRebuilt = 5,
}
