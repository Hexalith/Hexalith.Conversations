// <copyright file="ConversationTemporalEventSourceState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Defines non-public temporal source read states.
/// </summary>
public enum ConversationTemporalEventSourceState
{
    Available,
    Rebuilding,
    Unavailable,
    OutsideCoverage,
}
