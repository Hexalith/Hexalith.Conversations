// <copyright file="ConversationParticipant.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;

namespace Hexalith.Conversations.State;

/// <summary>
/// Represents replayed participant membership without Party profile data.
/// </summary>
/// <param name="PartyId">The stable Party reference.</param>
/// <param name="ParticipantType">The supported participant type.</param>
/// <param name="ParticipantRole">The supported participant role.</param>
/// <param name="AddedAt">The deterministic participant-added timestamp.</param>
/// <param name="AddedByPartyId">The stable Party reference for the actor who added the participant.</param>
public sealed record ConversationParticipant(
    PartyId PartyId,
    ParticipantType ParticipantType,
    ParticipantRole ParticipantRole,
    DateTimeOffset AddedAt,
    PartyId AddedByPartyId);
