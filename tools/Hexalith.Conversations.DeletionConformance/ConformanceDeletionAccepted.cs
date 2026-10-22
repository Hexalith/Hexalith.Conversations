using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>One conformance acceptance; it records no destructive Agents effect.</summary>
/// <param name="Signal">The complete original source binding.</param>
/// <param name="DeliveryAttemptId">The first accepted transport attempt.</param>
/// <param name="Acknowledgement">The immutable qualification receipt at this event's position.</param>
public sealed record ConformanceDeletionAccepted(ConversationDeletionSignal Signal, string DeliveryAttemptId,
    ConversationDeletionAcknowledgement Acknowledgement) : IEventPayload;
