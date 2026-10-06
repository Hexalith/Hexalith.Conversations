// <copyright file="AgentParticipantRemoved.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>Public AgentParticipantRemoved fact for compatible source projections.</summary>
/// <param name="Metadata">Exact source occurrence and identity.</param>
/// <param name="ParticipantPartyId">Permanently removed immutable Party.</param>
public sealed record AgentParticipantRemoved(ConversationEventMetadata Metadata, PartyId ParticipantPartyId);
