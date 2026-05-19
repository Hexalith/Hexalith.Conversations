// <copyright file="ConversationProjectionLifecycleState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Defines the local conversation lifecycle states tracked by duplicate-safe projection tests.
/// </summary>
public enum ConversationProjectionLifecycleState
{
    /// <summary>
    /// No creation event has been observed.
    /// </summary>
    NotCreated = 0,

    /// <summary>
    /// The conversation is open.
    /// </summary>
    Open = 1,

    /// <summary>
    /// The conversation is closed.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// The conversation is archived.
    /// </summary>
    Archived = 3,
}
