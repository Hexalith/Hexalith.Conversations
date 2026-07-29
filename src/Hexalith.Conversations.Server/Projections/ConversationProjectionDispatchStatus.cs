// <copyright file="ConversationProjectionDispatchStatus.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Durable completion states for a Conversations projection dispatch.</summary>
public enum ConversationProjectionDispatchStatus
{
    /// <summary>The dispatch is visible only as incomplete work.</summary>
    Pending = 0,

    /// <summary>Both projection keys were persisted for this stable dispatch identity.</summary>
    Completed = 1,
}
