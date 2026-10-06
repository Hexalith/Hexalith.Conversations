using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>A message as currently visible, after edits, deletion and redaction.</summary>
/// <param name="MessageId">The unchanged message identity.</param>
/// <param name="AuthorPartyId">The stable author.</param>
/// <param name="Text">Current visible content; null when deleted or redacted.</param>
/// <param name="CreatedAt">Persisted original occurrence time.</param>
/// <param name="EditedAt">Persisted latest edit time.</param>
/// <param name="Deleted">Whether this message was deleted.</param>
/// <param name="Redacted">Whether current policy redacts content.</param>
/// <param name="Provenance">Persisted original and editing provenance.</param>
public sealed record ConversationAgentMessage(MessageId MessageId, PartyId AuthorPartyId, string? Text,
    DateTimeOffset CreatedAt, DateTimeOffset? EditedAt, bool Deleted, bool Redacted,
    AgentMessageProvenance? Provenance);
