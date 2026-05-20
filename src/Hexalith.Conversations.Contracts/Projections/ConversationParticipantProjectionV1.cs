// <copyright file="ConversationParticipantProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Carries a participant's stable Party reference without hydrated personal data.
/// </summary>
/// <param name="participantPartyId">The stable Party identity.</param>
/// <param name="participantType">The participant type.</param>
/// <param name="participantRole">The participant role.</param>
public sealed record ConversationParticipantProjectionV1(
    PartyId ParticipantPartyId,
    ParticipantType ParticipantType,
    ParticipantRole ParticipantRole)
{
    /// <summary>
    /// Gets the stable Party identity.
    /// </summary>
    public PartyId ParticipantPartyId { get; } = ParticipantPartyId ?? throw new ArgumentNullException(nameof(ParticipantPartyId));

    /// <summary>
    /// Gets the participant type.
    /// </summary>
    public ParticipantType ParticipantType { get; } = ParticipantType ?? throw new ArgumentNullException(nameof(ParticipantType));

    /// <summary>
    /// Gets the participant role.
    /// </summary>
    public ParticipantRole ParticipantRole { get; } = ParticipantRole ?? throw new ArgumentNullException(nameof(ParticipantRole));
}
