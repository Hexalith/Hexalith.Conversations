// <copyright file="ConversationGatewayTenantAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Supplies the fixture's external read authority while retaining exact tenant and caller checks.</summary>
internal sealed class ConversationGatewayTenantAccessService : IConversationTenantAccessService
{
    /// <inheritdoc />
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
        => ValueTask.FromResult(
            trustedTenantId is not null
            && routeTenantId == trustedTenantId
            && !string.IsNullOrWhiteSpace(callerPrincipalId)
                ? ConversationTenantAccessDecision.Allowed(requirement, trustedTenantId, callerPrincipalId)
                : ConversationTenantAccessDecision.Denied(
                    requirement,
                    trustedTenantId,
                    callerPrincipalId,
                    ConversationTenantAccessDenialReason.TenantMismatch));
}
