// <copyright file="ConversationTenantCatalogueResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Authenticated complete stable catalogue; a partial feed cannot report zero.</summary>
/// <param name="Outcome">Available only for an authenticated complete catalogue.</param>
/// <param name="Entries">Every tenant Conversation including those with no Agent Calls.</param>
/// <param name="Checkpoint">Complete stable feed checkpoint.</param>
/// <param name="ObservedAt">Observation occurrence.</param>
/// <param name="Complete">Whether the catalogue owner certified complete enumeration.</param>
public sealed record ConversationTenantCatalogueResult(ConversationAgentsOutcome Outcome,
    IReadOnlyList<ConversationTenantCatalogueEntry>? Entries = null, string? Checkpoint = null,
    DateTimeOffset? ObservedAt = null, bool Complete = false);
