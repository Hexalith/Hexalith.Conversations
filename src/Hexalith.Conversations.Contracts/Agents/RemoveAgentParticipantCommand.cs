using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Removes only the authenticated service's immutable AI Member participant.</summary>
/// <param name="Metadata">Tenant and immutable actor binding.</param>
/// <param name="ConversationId">The exact source Conversation.</param>
/// <param name="ParticipantPartyId">Must equal the independently admitted immutable Agent Party.</param>
/// <param name="OperationTimestamp">Deterministic persisted occurrence time.</param>
public sealed record RemoveAgentParticipantCommand(ConversationCommandMetadata Metadata,
    ConversationId ConversationId, PartyId ParticipantPartyId, DateTimeOffset OperationTimestamp);
