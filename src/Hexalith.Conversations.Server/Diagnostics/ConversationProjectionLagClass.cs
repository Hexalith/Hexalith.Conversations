// <copyright file="ConversationProjectionLagClass.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Bounded vocabulary for projection lag signal classification.
/// </summary>
public enum ConversationProjectionLagClass
{
    /// <summary>
    /// No lag class (default guard value — must not be recorded as a signal).
    /// </summary>
    None = 0,

    /// <summary>
    /// Projection lag is within acceptable threshold.
    /// </summary>
    WithinThreshold = 1,

    /// <summary>
    /// Projection lag has breached the configured threshold.
    /// </summary>
    ThresholdBreached = 2,

    /// <summary>
    /// Projection lag is at a critical level indicating data integrity concern.
    /// </summary>
    CriticalLag = 3,

    /// <summary>
    /// Projection is unavailable — lag cannot be determined.
    /// </summary>
    Unavailable = 4,
}
