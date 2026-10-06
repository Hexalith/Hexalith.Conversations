// <copyright file="IConversationDeletionReceiptVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Authenticates current source-worker permission, exact receiver target and receipt authenticity independently.
/// Available certifies authenticity; the aggregate compares immutable signal fields and quarantines changed evidence.
/// A Conflict or unauthenticated receipt cannot authorize an acknowledgement; default registration fails closed.</summary>
public interface IConversationDeletionReceiptVerifier
{
    /// <summary>Resolves current exact-operation evidence without inferring authority from projections or caller claims.</summary>
    /// <returns>A classified authenticated result.</returns>
    Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope,
        RecordConversationDeletionDeliveryCommand command, CancellationToken cancellationToken = default);
}
