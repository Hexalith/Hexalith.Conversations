// <copyright file="DerivedKeyAllowTenantAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Test tenant-access service that allows only the configured tenant and fails closed for all others.
/// </summary>
/// <param name="allowedTenant">The sole authorized tenant.</param>
internal sealed class DerivedKeyAllowTenantAccessService(TenantId allowedTenant) : IConversationTenantAccessService
{
    private readonly TenantId _allowedTenant = allowedTenant ?? throw new ArgumentNullException(nameof(allowedTenant));

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
        if (trustedTenantId != _allowedTenant
            || (routeTenantId is not null && routeTenantId != _allowedTenant)
            || (projectionTenantId is not null && projectionTenantId != _allowedTenant)
            || string.IsNullOrWhiteSpace(callerPrincipalId))
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
