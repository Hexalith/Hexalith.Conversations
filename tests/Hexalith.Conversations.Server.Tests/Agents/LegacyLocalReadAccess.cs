// <copyright file="LegacyLocalReadAccess.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Synthetic exact local human read grant for the legacy read boundary; never production authority.</summary>
internal sealed class LegacyLocalReadAccess : IConversationTenantAccessService
{
    /// <inheritdoc />
    public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId, string? callerPrincipalId, TenantId? routeTenantId = null, TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null, TenantId? projectionTenantId = null, TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ConversationTenantAccessDecision.Allowed(requirement, F.Tenant, "human"));
}
