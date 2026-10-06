using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Durable posting provenance; it never grants authorization.</summary>
/// <param name="AgentCallTraceReference">The stable Agent Call reference.</param>
/// <param name="AiGenerated">Whether the original content was AI generated.</param>
/// <param name="HumanEdited">Whether a human edited the submitted content.</param>
/// <param name="EditedByPartyId">The editing human Party, required exactly when HumanEdited.</param>
public sealed record AgentMessageProvenance(string AgentCallTraceReference, bool AiGenerated,
    bool HumanEdited, PartyId? EditedByPartyId = null);
