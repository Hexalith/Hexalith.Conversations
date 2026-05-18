// <copyright file="ConversationSummaryProjection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Minimal adopter-facing conversation summary contract.
/// </summary>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="freshness">The freshness and trust state.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="participantPartyIds">Stable participant Party references.</param>
public sealed record ConversationSummaryProjection(
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionFreshness Freshness,
    string? Label = null,
    BusinessReference? BusinessReference = null,
    IReadOnlyList<PartyId>? ParticipantPartyIds = null);
