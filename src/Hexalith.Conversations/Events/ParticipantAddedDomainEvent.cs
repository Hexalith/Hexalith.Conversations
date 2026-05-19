// <copyright file="ParticipantAddedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>
/// Records participant membership using stable Party attribution only.
/// </summary>
/// <param name="metadata">The Conversations event metadata. Must not be null.</param>
/// <param name="participantPartyId">The stable Party reference added as participant.</param>
/// <param name="participantType">The supported participant type.</param>
/// <param name="participantRole">The supported participant role.</param>
public sealed record ParticipantAddedDomainEvent(
    ConversationEventMetadata Metadata,
    PartyId ParticipantPartyId,
    ParticipantType ParticipantType,
    ParticipantRole ParticipantRole) : IEventPayload
{
    /// <summary>
    /// Gets the Conversations event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = Metadata ?? throw new ArgumentNullException(nameof(Metadata));

    /// <summary>
    /// Gets the stable Party reference added as participant.
    /// </summary>
    public PartyId ParticipantPartyId { get; } = ParticipantPartyId ?? throw new ArgumentNullException(nameof(ParticipantPartyId));

    /// <summary>
    /// Gets the supported participant type.
    /// </summary>
    public ParticipantType ParticipantType { get; } = ParticipantType ?? throw new ArgumentNullException(nameof(ParticipantType));

    /// <summary>
    /// Gets the supported participant role.
    /// </summary>
    public ParticipantRole ParticipantRole { get; } = ParticipantRole ?? throw new ArgumentNullException(nameof(ParticipantRole));

    /// <summary>
    /// Gets the deterministic participant-added timestamp copied from event metadata.
    /// </summary>
    public DateTimeOffset AddedAt => Metadata.CommittedAt;
}
