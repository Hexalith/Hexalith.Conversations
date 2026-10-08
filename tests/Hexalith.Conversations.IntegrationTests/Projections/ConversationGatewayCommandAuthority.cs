// <copyright file="ConversationGatewayCommandAuthority.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Concurrent;

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Provides exact human command authority for enrolled live gateway test identities.</summary>
internal sealed class ConversationGatewayCommandAuthority : IConversationAgentAuthority
{
    private readonly ConcurrentDictionary<(string Tenant, string Conversation), byte> _enrolled = new();

    /// <summary>Enrolls a conversation in one of the two configured live fixture tenants.</summary>
    /// <param name="tenantId">The configured tenant identity.</param>
    /// <param name="conversationId">The exact conversation identity under test.</param>
    internal void Enroll(string tenantId, string conversationId)
    {
        if (tenantId is not ("tenant-gateway-001" or "tenant-gateway-002"))
        {
            throw new ArgumentException("The live fixture tenant is not configured.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        _ = _enrolled.TryAdd((tenantId, conversationId), 0);
    }

    /// <inheritdoc/>
    public Task<ConversationAgentAuthorization> AuthorizeAsync(string authenticatedPrincipalId, TenantId tenantId,
        ConversationId? conversationId, string operation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool allowed = authenticatedPrincipalId == "party-gateway-actor"
            && operation == "GeneralCommand"
            && conversationId is not null
            && _enrolled.ContainsKey((tenantId.Value, conversationId.Value));
        return Task.FromResult(allowed
            ? new ConversationAgentAuthorization(ConversationAgentsOutcome.Available, tenantId,
                authenticatedPrincipalId, new PartyId("party-gateway-actor"), "gateway-fixture-authority-v1")
            : new ConversationAgentAuthorization(ConversationAgentsOutcome.Denied));
    }
}
