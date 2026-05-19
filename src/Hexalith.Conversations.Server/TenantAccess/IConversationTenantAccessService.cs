// <copyright file="IConversationTenantAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Checks tenant access at Conversations server/application boundaries.
/// </summary>
public interface IConversationTenantAccessService
{
    /// <summary>
    /// Checks access before protected conversation state or read projections are touched.
    /// </summary>
    /// <param name="requirement">The required operation class.</param>
    /// <param name="trustedTenantId">The trusted request tenant context.</param>
    /// <param name="callerPrincipalId">The caller principal or user identifier.</param>
    /// <param name="routeTenantId">The route tenant binding when present.</param>
    /// <param name="commandTenantId">The command body tenant binding when present.</param>
    /// <param name="aggregateTenantId">The aggregate or conversation tenant binding when available.</param>
    /// <param name="projectionTenantId">The projection key tenant binding when available.</param>
    /// <param name="idempotencyTenantId">The idempotency context tenant binding when available.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tenant access decision.</returns>
    ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
        ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        TenantId? routeTenantId = null,
        TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null,
        TenantId? projectionTenantId = null,
        TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default);
}
