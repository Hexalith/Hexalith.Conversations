using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Explicit existing per-tenant enrollment binding; never creates or borrows a Party.</summary>
/// <param name="TenantId">Exact worker tenant.</param>
/// <param name="AuthenticatedPrincipalId">Current authenticated machine identity.</param>
/// <param name="ServicePartyId">The separately enrolled dedicated Conversations service Party.</param>
public sealed record ConversationDeletionWorkerRegistration(TenantId TenantId, string AuthenticatedPrincipalId, PartyId ServicePartyId);
