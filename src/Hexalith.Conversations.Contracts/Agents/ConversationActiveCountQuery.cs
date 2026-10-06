using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Counts currently open Conversations created in the half-open UTC creation window.</summary>
/// <param name="TenantId">The exact tenant.</param>
/// <param name="CreatedFromInclusive">Inclusive creation time, independent of Agent Calls.</param>
/// <param name="CreatedToExclusive">Exclusive creation time.</param>
public sealed record ConversationActiveCountQuery(TenantId TenantId, DateTimeOffset CreatedFromInclusive,
    DateTimeOffset CreatedToExclusive);
