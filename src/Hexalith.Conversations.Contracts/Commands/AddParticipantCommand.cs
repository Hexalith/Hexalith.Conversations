// <copyright file="AddParticipantCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests that a stable Party reference be added as a conversation participant.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="participantPartyId">The stable Party reference to add.</param>
/// <param name="participantType">The supported participant type.</param>
/// <param name="participantRole">The supported participant role.</param>
/// <param name="providerCorrelation">Optional provider correlation metadata that is never authority.</param>
public sealed record AddParticipantCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    PartyId ParticipantPartyId,
    ParticipantType ParticipantType,
    ParticipantRole ParticipantRole,
    ProviderCorrelationMetadata? ProviderCorrelation = null);
