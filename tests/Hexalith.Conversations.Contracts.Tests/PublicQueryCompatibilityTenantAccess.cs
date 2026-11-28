// <copyright file="PublicQueryCompatibilityTenantAccess.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>Grants the compatibility test one member caller on its seeded tenant.</summary>
internal sealed class PublicQueryCompatibilityTenantAccess(TenantId tenantId, string member)
    : IConversationTenantAccessService
{
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
        => ValueTask.FromResult(trustedTenantId == tenantId && callerPrincipalId == member
            ? ConversationTenantAccessDecision.Allowed(requirement, tenantId, member)
            : ConversationTenantAccessDecision.Denied(
                requirement, trustedTenantId, callerPrincipalId, ConversationTenantAccessDenialReason.MissingMember));
}
