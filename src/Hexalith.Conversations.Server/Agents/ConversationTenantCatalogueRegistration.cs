using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Explicit installed namespace selection; registration alone supplies neither coverage nor read authority.</summary>
/// <param name="Scope">Installed complete Conversation namespace.</param>
public sealed record ConversationTenantCatalogueRegistration(SourcePublicationScope Scope);
