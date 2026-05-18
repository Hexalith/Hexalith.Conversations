// <copyright file="CreateConversation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Commands;

/// <summary>
/// Domain command for creating a tenant-scoped conversation from the public create contract.
/// </summary>
/// <param name="PublicCommand">The public create-conversation contract supplied by an adopter boundary.</param>
/// <param name="ConversationId">The Conversations-owned internal conversation identity.</param>
/// <param name="CreatedAt">The deterministic creation timestamp supplied by the boundary.</param>
/// <param name="EventId">The deterministic public event identity supplied by the boundary.</param>
public sealed record CreateConversation(
    CreateConversationCommand PublicCommand,
    ConversationId? ConversationId,
    DateTimeOffset CreatedAt,
    string EventId);
