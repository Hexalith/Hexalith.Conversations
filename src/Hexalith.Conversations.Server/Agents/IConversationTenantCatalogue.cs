// <copyright file="IConversationTenantCatalogue.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Narrow independently authenticated production port; default registration fails closed.</summary>
public interface IConversationTenantCatalogue
{
    /// <summary>Resolves current exact-operation evidence without inferring authority from projections or caller claims.</summary>
    /// <returns>A classified authenticated result.</returns>
    Task<ConversationTenantCatalogueResult> ReadAsync(ConversationActiveCountQuery query,
        CancellationToken cancellationToken = default);
}
