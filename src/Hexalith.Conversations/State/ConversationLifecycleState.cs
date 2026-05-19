// <copyright file="ConversationLifecycleState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.State;

/// <summary>
/// Represents the lifecycle state carried by the conversation aggregate.
/// </summary>
public enum ConversationLifecycleState
{
    /// <summary>
    /// The conversation has not been created.
    /// </summary>
    NotCreated = 0,

    /// <summary>
    /// The conversation has been created and is open.
    /// </summary>
    Open = 1,

    /// <summary>
    /// The conversation has been closed.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// The conversation has been archived.
    /// </summary>
    Archived = 3,
}
