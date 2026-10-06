using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Gateway admission is pending until exact persisted source lookup confirms the effect.</summary>
/// <param name="Outcome">Available means gateway admission only, not persisted completion.</param>
/// <param name="CommandMessageId">Canonical command-status identity.</param>
/// <param name="MessageId">Requested deterministic posting identity; a differing returned identity conflicts.</param>
/// <param name="Persisted">True only when independently confirmed in the authoritative source.</param>
public sealed record ConversationAgentCommandResult(ConversationAgentsOutcome Outcome,
    string? CommandMessageId = null, MessageId? MessageId = null, bool Persisted = false);
