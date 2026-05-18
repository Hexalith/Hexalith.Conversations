// <copyright file="ConversationEventMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Carries public event metadata without exposing persistence substrate mechanics.
/// </summary>
/// <param name="schemaVersion">The event schema version.</param>
/// <param name="eventType">The public Conversations event type.</param>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="actorPartyId">The optional stable actor Party reference.</param>
/// <param name="correlationId">The correlation identifier.</param>
/// <param name="causationId">The optional causation identifier.</param>
/// <param name="committedAt">The committed timestamp for the public contract.</param>
public sealed record ConversationEventMetadata(
    SchemaVersion SchemaVersion,
    string EventType,
    TenantId TenantId,
    ConversationId ConversationId,
    PartyId? ActorPartyId,
    string CorrelationId,
    string? CausationId,
    DateTimeOffset CommittedAt);
