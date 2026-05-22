// <copyright file="ConversationReplayOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Replay;

/// <summary>
/// Deterministic replay/version matrix outcomes used by local proof tests.
/// </summary>
public enum ConversationReplayOutcome
{
    /// <summary>
    /// The stream replayed into trusted aggregate state.
    /// </summary>
    Replay,

    /// <summary>
    /// The stream was rejected before trusted state could be returned.
    /// </summary>
    Reject,

    /// <summary>
    /// The stream must be quarantined to the affected tenant/conversation partition.
    /// </summary>
    Quarantine,

    /// <summary>
    /// The derived artifact needs rebuilding before it can be trusted.
    /// </summary>
    Rebuilding,

    /// <summary>
    /// The derived artifact is stale and cannot support trust-bearing decisions.
    /// </summary>
    Stale,

    /// <summary>
    /// The stream shape or derived artifact is invalid.
    /// </summary>
    Invalid,

    /// <summary>
    /// Required replay evidence is unavailable.
    /// </summary>
    Unavailable,
}
