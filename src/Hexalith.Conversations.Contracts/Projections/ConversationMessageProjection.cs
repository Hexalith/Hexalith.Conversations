// <copyright file="ConversationMessageProjection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Minimal adopter-facing conversation message read contract.
/// </summary>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="messageId">The stable message identity.</param>
/// <param name="authorPartyId">The stable Party reference for the author.</param>
/// <param name="text">The message text visible to the caller.</param>
/// <param name="createdAt">The public message creation timestamp.</param>
/// <param name="freshness">The freshness and trust state.</param>
public sealed record ConversationMessageProjection(
    TenantId TenantId,
    ConversationId ConversationId,
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    DateTimeOffset CreatedAt,
    ProjectionFreshness Freshness);
