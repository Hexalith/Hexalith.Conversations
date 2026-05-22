// <copyright file="ConversationReplayResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.State;

namespace Hexalith.Conversations.Replay;

/// <summary>
/// Result of deterministic aggregate replay.
/// </summary>
/// <param name="Outcome">The replay/version matrix outcome.</param>
/// <param name="State">The replayed state when replay succeeded.</param>
/// <param name="ErrorCode">The safe typed error code for negative outcomes.</param>
/// <param name="DiagnosticCode">The safe bounded diagnostic code.</param>
public sealed record ConversationReplayResult(
    ConversationReplayOutcome Outcome,
    ConversationState? State = null,
    ConversationErrorCode? ErrorCode = null,
    string? DiagnosticCode = null)
{
    /// <summary>
    /// Creates a successful replay result.
    /// </summary>
    /// <param name="state">The trusted replayed state.</param>
    /// <returns>A replay result.</returns>
    public static ConversationReplayResult Replayed(ConversationState state)
        => new(ConversationReplayOutcome.Replay, state ?? throw new ArgumentNullException(nameof(state)));

    /// <summary>
    /// Creates a typed rejection result without trusted state.
    /// </summary>
    /// <param name="errorCode">The safe error code.</param>
    /// <param name="diagnosticCode">The bounded diagnostic code.</param>
    /// <returns>A replay result.</returns>
    public static ConversationReplayResult Rejected(ConversationErrorCode errorCode, string diagnosticCode)
        => new(ConversationReplayOutcome.Reject, null, errorCode, diagnosticCode);
}
