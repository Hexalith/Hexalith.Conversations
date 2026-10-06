namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Closed source delivery transitions.</summary>
public enum ConversationDeletionDeliveryAction
{
    /// <summary>Record a distinct retry against an exact receiver target.</summary>
    Attempt,
    /// <summary>Advance the checkpoint only with an authenticated exact acknowledgement.</summary>
    Acknowledge,
    /// <summary>Durably block a changed logical signal without skipping it.</summary>
    Quarantine,
}
