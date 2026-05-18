// <copyright file="ParticipantAdded.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a participant Party reference was added to a conversation.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="participantPartyId">The stable Party reference added as participant.</param>
public sealed record ParticipantAdded(
    ConversationEventMetadata Metadata,
    PartyId ParticipantPartyId);
