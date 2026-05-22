// <copyright file="IConversationTemporalEventSource.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Replay;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Reads server-owned conversation history for temporal reconstruction after tenant authorization.
/// </summary>
public interface IConversationTemporalEventSource
{
    /// <summary>
    /// Reads retained ordered events for one conversation.
    /// </summary>
    /// <param name="tenantId">The trusted tenant scope.</param>
    /// <param name="conversationId">The conversation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe temporal source result.</returns>
    ValueTask<ConversationTemporalEventSourceResult> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default);
}
