// <copyright file="DerivedKeyDaprTenantAccess.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.AppHost.Tests;

/// <summary>
/// Tenant access guard for live Dapr+Redis fault tests allowing only explicitly configured tenants.
/// </summary>
/// <param name="allowedTenants">The authorized tenant identifiers.</param>
internal sealed class DerivedKeyDaprTenantAccess(params TenantId[] allowedTenants) : IConversationTenantAccessService
{
    private readonly HashSet<TenantId> _allowed = [.. allowedTenants];

    /// <inheritdoc/>
    public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
        ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        TenantId? routeTenantId = null,
        TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null,
        TenantId? projectionTenantId = null,
        TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (trustedTenantId is null
            || !_allowed.Contains(trustedTenantId)
            || string.IsNullOrWhiteSpace(callerPrincipalId)
            || (routeTenantId is not null && routeTenantId != trustedTenantId)
            || (projectionTenantId is not null && projectionTenantId != trustedTenantId)
            || (aggregateTenantId is not null && aggregateTenantId != trustedTenantId))
        {
            return ValueTask.FromResult(ConversationTenantAccessDecision.Denied(
                requirement,
                trustedTenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.TenantMismatch));
        }

        return ValueTask.FromResult(ConversationTenantAccessDecision.Allowed(
            requirement,
            trustedTenantId,
            callerPrincipalId));
    }
}
