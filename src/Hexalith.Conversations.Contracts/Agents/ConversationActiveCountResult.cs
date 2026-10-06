// <copyright file="ConversationActiveCountResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Authoritative denominator; an unavailable catalog never reports an empty count.</summary>
/// <param name="Outcome">Available only for a complete stable tenant catalog.</param>
/// <param name="Count">Null unless the complete source was verified.</param>
/// <param name="CatalogCheckpoint">Opaque complete catalog checkpoint.</param>
/// <param name="ObservedAt">Catalog observation time.</param>
/// <param name="TenantId">Exact authenticated tenant.</param>
/// <param name="CreatedFromInclusive">Exact requested inclusive UTC creation bound.</param>
/// <param name="SourceContractVersion">Complete restricted owner contract version.</param>
/// <param name="CreatedToExclusive">Exact requested exclusive UTC creation bound.</param>
public sealed record ConversationActiveCountResult(ConversationAgentsOutcome Outcome, long? Count = null,
    string? CatalogCheckpoint = null, DateTimeOffset? ObservedAt = null, TenantId? TenantId = null,
    DateTimeOffset? CreatedFromInclusive = null, DateTimeOffset? CreatedToExclusive = null, int SourceContractVersion = 1);
