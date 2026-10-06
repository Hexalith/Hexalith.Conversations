// <copyright file="UnavailableConversationDeletionReceiptVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Fail-closed default; no live qualification provider has been supplied.</summary>
public sealed class UnavailableConversationDeletionReceiptVerifier : IConversationDeletionReceiptVerifier
{
    /// <inheritdoc />
    public Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope, RecordConversationDeletionDeliveryCommand command,
        CancellationToken cancellationToken = default) => Task.FromResult(ConversationAgentsOutcome.Unavailable);
}
