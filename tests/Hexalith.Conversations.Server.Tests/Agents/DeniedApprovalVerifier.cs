// <copyright file="DeniedApprovalVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Out-of-scope approval verifier double; denies every approval.</summary>
internal sealed class DeniedApprovalVerifier : IConversationDeletionApprovalVerifier
{
    public Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope, ApproveConversationDeletionCommand command,
        CancellationToken cancellationToken = default) => Task.FromResult(ConversationAgentsOutcome.Denied);
}
