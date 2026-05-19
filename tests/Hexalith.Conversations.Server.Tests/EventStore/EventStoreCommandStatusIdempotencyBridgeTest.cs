// <copyright file="EventStoreCommandStatusIdempotencyBridgeTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.EventStore;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.Tests.EventStore;

/// <summary>
/// Verifies EventStore command status is interpreted as an internal signal, not a public replay contract.
/// P26 review fix (2026-05-19): assert externally-observable contracts (never invents a Conversations outcome,
/// always returns RetryableUncertainty, never exposes EventStore internals through the decision) rather than
/// mirroring the IsTerminal() classifier with [InlineData]. The InlineData-driven test passed regardless of
/// whether IsTerminal() was implemented correctly.
/// </summary>
public sealed class EventStoreCommandStatusIdempotencyBridgeTest
{
    private static readonly DateTimeOffset Now = new(2026, 5, 19, 14, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// The bridge must never return Reserved, Duplicate, or Conflict; EventStore status alone cannot resolve a
    /// Conversations-domain logical outcome.
    /// </summary>
    [Fact]
    public void BridgeNeverInventsConversationsOutcome()
    {
        foreach (CommandStatus status in Enum.GetValues<CommandStatus>())
        {
            ConversationIdempotencyDecision decision =
                EventStoreCommandStatusIdempotencyBridge.Interpret(Status(status));

            decision.Kind.ShouldBe(
                ConversationIdempotencyDecisionKind.RetryableUncertainty,
                $"status={status} must not produce a terminal Conversations outcome");
            decision.StoredOutcome.ShouldBeNull($"status={status} must not invent a stored outcome");
        }
    }

    /// <summary>
    /// A missing command-status record cannot mean "duplicate"; it means "we cannot tell, retry".
    /// </summary>
    [Fact]
    public void MissingStatusReturnsContentSafeRetryableUncertainty()
    {
        ConversationIdempotencyDecision decision = EventStoreCommandStatusIdempotencyBridge.Interpret(null);

        decision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.RetryableUncertainty);
        decision.ReasonCode.ShouldBe("eventstore_command_status_missing");
        decision.StoredOutcome.ShouldBeNull();
    }

    /// <summary>
    /// Pending and terminal statuses produce distinguishable internal reason codes (so the host can distinguish
    /// "still in flight" from "completed but Conversations replay required") without leaking EventStore vocabulary
    /// through the decision's StoredOutcome.
    /// </summary>
    [Fact]
    public void PendingAndTerminalStatusesProduceDistinguishableInternalReasonCodes()
    {
        HashSet<string> pendingReasons = new(StringComparer.Ordinal);
        HashSet<string> terminalReasons = new(StringComparer.Ordinal);

        foreach (CommandStatus status in Enum.GetValues<CommandStatus>())
        {
            ConversationIdempotencyDecision decision =
                EventStoreCommandStatusIdempotencyBridge.Interpret(Status(status));
            if (status.IsTerminal())
            {
                terminalReasons.Add(decision.ReasonCode);
            }
            else
            {
                pendingReasons.Add(decision.ReasonCode);
            }
        }

        pendingReasons.ShouldBe(new[] { "eventstore_command_status_pending" });
        terminalReasons.ShouldBe(new[] { "eventstore_terminal_replay_required" });
    }

    private static CommandStatusRecord Status(CommandStatus status)
        => new(
            status,
            Now,
            AggregateId: "conversation-001",
            EventCount: status == CommandStatus.Completed ? 1 : null,
            RejectionEventType: status == CommandStatus.Rejected ? "ConversationRejectedDomainEvent" : null,
            FailureReason: null,
            TimeoutDuration: status == CommandStatus.TimedOut ? TimeSpan.FromSeconds(30) : null);
}
