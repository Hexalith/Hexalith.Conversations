// <copyright file="AgentParticipantRemovedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>Durable AgentParticipantRemovedDomainEvent fact replayed from the authoritative stream.</summary>
/// <param name="Metadata">Source occurrence and identity.</param>
/// <param name="ParticipantPartyId">Immutable removed Agent Party.</param>
public sealed record AgentParticipantRemovedDomainEvent(ConversationEventMetadata Metadata, PartyId ParticipantPartyId) : IEventPayload;
