namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Ordered source checkpoint/backfill and exact acknowledgement lookup.</summary>
/// <param name="Outcome">Quarantine blocks readiness and never advances the checkpoint.</param>
/// <param name="Signal">The immutable approval entry retained through outages.</param>
/// <param name="DeliveryRevision">Current delivery compare revision.</param>
/// <param name="AcknowledgedSourceRevision">Zero until a verified receiver acknowledgement.</param>
/// <param name="Acknowledgement">Original accepted receipt, including lost-ack recovery.</param>
/// <param name="LastAttemptId">Latest recorded independent delivery attempt.</param>
/// <param name="TargetVersion">Latest delivery target.</param>
/// <param name="PoisonCode">Only safe poison metadata; no changed payload.</param>
public sealed record ConversationDeletionSourceResult(ConversationAgentsOutcome Outcome,
    ConversationDeletionSignal? Signal = null, long DeliveryRevision = 0, long AcknowledgedSourceRevision = 0,
    ConversationDeletionAcknowledgement? Acknowledgement = null, string? LastAttemptId = null,
    string? TargetVersion = null, string? PoisonCode = null);
