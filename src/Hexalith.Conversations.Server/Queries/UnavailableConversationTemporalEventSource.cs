// <copyright file="UnavailableConversationTemporalEventSource.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Default temporal source used until host infrastructure wires a concrete implementation.
/// </summary>
public sealed class UnavailableConversationTemporalEventSource : IConversationTemporalEventSource
{
    public ValueTask<ConversationTemporalEventSourceResult> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ConversationTemporalEventSourceResult.Unavailable());
    }
}
