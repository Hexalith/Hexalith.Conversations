// <copyright file="EventStoreCommandStatusIdempotencyBridge.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Idempotency;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.EventStore;

/// <summary>
/// Interprets EventStore command status as an internal idempotency signal.
/// </summary>
public static class EventStoreCommandStatusIdempotencyBridge
{
    /// <summary>
    /// Converts command-status state into a non-disclosing idempotency decision.
    /// </summary>
    /// <param name="status">The EventStore command status, when available.</param>
    /// <returns>The idempotency decision.</returns>
    public static ConversationIdempotencyDecision Interpret(CommandStatusRecord? status)
    {
        if (status is null)
        {
            return ConversationIdempotencyDecision.RetryableUncertainty("eventstore_command_status_missing");
        }

        return status.Status.IsTerminal()
            ? ConversationIdempotencyDecision.RetryableUncertainty("eventstore_terminal_replay_required")
            : ConversationIdempotencyDecision.RetryableUncertainty("eventstore_command_status_pending");
    }
}
