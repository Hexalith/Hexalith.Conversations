// <copyright file="AddParticipantCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests that a stable Party reference be added as a conversation participant.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="participantPartyId">The stable Party reference to add.</param>
public sealed record AddParticipantCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    PartyId ParticipantPartyId);
