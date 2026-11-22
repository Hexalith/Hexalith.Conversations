using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Durable posting provenance; it never grants authorization.</summary>
/// <param name="AgentCallTraceReference">The stable Agent Call reference.</param>
/// <param name="AiGenerated">Whether the original content was AI generated.</param>
/// <param name="HumanEdited">Whether a human edit is recorded. New Agent posts start with false.</param>
/// <param name="EditedByPartyId">The human Party recorded by an authenticated edit, required when HumanEdited.</param>
public sealed record AgentMessageProvenance(string AgentCallTraceReference, bool AiGenerated,
    bool HumanEdited, PartyId? EditedByPartyId = null);
