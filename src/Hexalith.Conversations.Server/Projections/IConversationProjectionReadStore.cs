// <copyright file="IConversationProjectionReadStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Reads derived conversation projections by tenant and conversation identity.
/// </summary>
public interface IConversationProjectionReadStore
{
    /// <summary>
    /// Reads the projected summary/detail pair.
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="conversationId">The conversation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The projection pair, or null when no visible projection exists.</returns>
    ValueTask<ConversationProjectedReadModels?> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default);
}
