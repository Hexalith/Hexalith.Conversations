// <copyright file="ConversationCommandMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Carries tenant, actor, version, correlation, causation, and idempotency metadata for public commands.
/// </summary>
/// <param name="schemaVersion">The command schema version.</param>
/// <param name="tenantId">The tenant binding for the command.</param>
/// <param name="actorPartyId">The stable actor Party reference.</param>
/// <param name="correlationId">The caller correlation identifier.</param>
/// <param name="causationId">The optional causal operation identifier.</param>
/// <param name="idempotencyKey">The optional caller idempotency key.</param>
public sealed record ConversationCommandMetadata(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    PartyId ActorPartyId,
    string CorrelationId,
    string? CausationId = null,
    string? IdempotencyKey = null);
