// <copyright file="LegacyLocalReadStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Local projected source fixture for the existing detail read boundary; never live storage.</summary>
internal sealed class LegacyLocalReadStore(ConversationProjectedReadModels models) : IConversationProjectionReadStore
{
    /// <inheritdoc />
    public ValueTask<ConversationProjectedReadModels?> ReadAsync(TenantId tenantId, ConversationId conversationId,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<ConversationProjectedReadModels?>(models);

    /// <inheritdoc />
    public ValueTask<ConversationProjectionIndexSnapshot> ListAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Local fixture covers only the existing detail boundary.");

    /// <inheritdoc />
    public ValueTask<IReadOnlySet<string>> ValidatePageAsync(TenantId tenantId, ConversationProjectionIndexSnapshot snapshot,
        IReadOnlyList<ConversationSummaryProjectionV1> page, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Local fixture covers only the existing detail boundary.");
}
