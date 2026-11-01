// <copyright file="StaticAuthority.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Out-of-scope authority double returning one fixed classified result.</summary>
internal sealed class StaticAuthority(ConversationAgentAuthorization result) : IConversationAgentAuthority
{
    public List<(string Principal, TenantId Tenant, ConversationId? Conversation, string Operation)> Calls { get; } = [];

    public Exception? Failure { get; set; }

    public Task<ConversationAgentAuthorization> AuthorizeAsync(string authenticatedPrincipalId, TenantId tenantId,
        ConversationId? conversationId, string operation, CancellationToken cancellationToken = default)
    {
        Calls.Add((authenticatedPrincipalId, tenantId, conversationId, operation));
        return Failure is null ? Task.FromResult(result) : Task.FromException<ConversationAgentAuthorization>(Failure);
    }
}
