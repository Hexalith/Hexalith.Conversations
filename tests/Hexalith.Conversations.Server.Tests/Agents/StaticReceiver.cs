// <copyright file="StaticReceiver.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Receiver double returning one fixed current target.</summary>
internal sealed class StaticReceiver(string? target) : IConversationDeletionReceiver
{
    public Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken cancellationToken = default) => Task.FromResult(target);

    public Task<ConversationDeletionReceiverResult> LookupAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<ConversationDeletionReceiverResult> SubmitAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
