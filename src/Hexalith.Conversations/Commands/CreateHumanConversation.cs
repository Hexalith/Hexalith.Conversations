// <copyright file="CreateHumanConversation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Commands;

/// <summary>
/// Human pilot command for creating a tenant-scoped conversation from the public create contract.
/// </summary>
/// <param name="PublicCommand">The public create-conversation contract supplied by an adopter boundary.</param>
/// <param name="ConversationId">The Conversations-owned internal conversation identity.</param>
/// <param name="CreatedAt">The timestamp supplied by the API for admission. Domain execution assigns the event occurrence.</param>
/// <param name="EventId">The deterministic public event identity supplied by the boundary.</param>
public sealed record CreateHumanConversation(
    CreateConversationCommand PublicCommand,
    ConversationId? ConversationId,
    DateTimeOffset CreatedAt,
    string EventId);
