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
/// </summary>
public sealed class EventStoreCommandStatusIdempotencyBridgeTest
{
    /// <summary>
    /// Pending EventStore command statuses become retryable uncertainty.
    /// </summary>
    [Theory]
    [InlineData(CommandStatus.Received)]
    [InlineData(CommandStatus.Processing)]
    [InlineData(CommandStatus.EventsStored)]
    [InlineData(CommandStatus.EventsPublished)]
    public void PendingStatusShouldReturnRetryableUncertainty(CommandStatus status)
    {
        ConversationIdempotencyDecision decision = EventStoreCommandStatusIdempotencyBridge.Interpret(Status(status));

        decision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.RetryableUncertainty);
        decision.ReasonCode.ShouldBe("eventstore_command_status_pending");
    }

    /// <summary>
    /// Terminal EventStore status alone is not enough to replay a Conversations logical outcome.
    /// </summary>
    [Theory]
    [InlineData(CommandStatus.Completed)]
    [InlineData(CommandStatus.Rejected)]
    [InlineData(CommandStatus.PublishFailed)]
    [InlineData(CommandStatus.TimedOut)]
    public void TerminalStatusShouldRequireConversationReplay(CommandStatus status)
    {
        ConversationIdempotencyDecision decision = EventStoreCommandStatusIdempotencyBridge.Interpret(Status(status));

        decision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.RetryableUncertainty);
        decision.ReasonCode.ShouldBe("eventstore_terminal_replay_required");
        decision.StoredOutcome.ShouldBeNull();
    }

    private static CommandStatusRecord Status(CommandStatus status)
        => new(
            status,
            new DateTimeOffset(2026, 5, 19, 14, 30, 0, TimeSpan.Zero),
            AggregateId: "conversation-001",
            EventCount: status == CommandStatus.Completed ? 1 : null,
            RejectionEventType: status == CommandStatus.Rejected ? "ConversationRejectedDomainEvent" : null,
            FailureReason: null,
            TimeoutDuration: status == CommandStatus.TimedOut ? TimeSpan.FromSeconds(30) : null);
}
