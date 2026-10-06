namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Closed, content-safe outcomes of the restricted Agents service contract.</summary>
public enum ConversationAgentsOutcome
{
    /// <summary>The requested current fact is available.</summary>
    Available,
    /// <summary>The exact requested message or initial participant is absent.</summary>
    Absent,
    /// <summary>An approved deletion was persisted.</summary>
    ConversationDeleted,
    /// <summary>The immutable AI participant was explicitly removed.</summary>
    PrincipalRemovedFromConversation,
    /// <summary>Current authority denies this operation.</summary>
    Denied,
    /// <summary>Current authority or a complete source could not be proved.</summary>
    Unavailable,
    /// <summary>Immutable intent, source, type or role conflicts.</summary>
    Conflict,
    /// <summary>Poison evidence prevents checkpoint advancement and readiness.</summary>
    Quarantined,
    /// <summary>The request shape is not supported.</summary>
    Invalid,
}
