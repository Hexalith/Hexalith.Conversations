// <copyright file="ParticipantAdded.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a participant Party reference was added to a conversation.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="participantPartyId">The stable Party reference added as participant.</param>
/// <param name="participantType">The supported participant type.</param>
/// <param name="participantRole">The supported participant role.</param>
public sealed record ParticipantAdded(
    ConversationEventMetadata Metadata,
    PartyId ParticipantPartyId,
    ParticipantType ParticipantType,
    ParticipantRole ParticipantRole);
