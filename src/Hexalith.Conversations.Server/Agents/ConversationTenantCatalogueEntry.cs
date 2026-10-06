// <copyright file="ConversationTenantCatalogueEntry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Authoritative source Conversation entry, independent of Agent Calls and content access.</summary>
/// <param name="TenantId">Exact tenant.</param>
/// <param name="ConversationId">Source identity.</param>
/// <param name="CreatedAt">Persisted source creation occurrence.</param>
/// <param name="Active">Current open, undeleted source status.</param>
public sealed record ConversationTenantCatalogueEntry(TenantId TenantId, ConversationId ConversationId,
    DateTimeOffset CreatedAt, bool Active);
