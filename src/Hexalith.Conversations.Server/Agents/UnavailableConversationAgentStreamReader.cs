// <copyright file="UnavailableConversationAgentStreamReader.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>No authenticated production source reader has been configured.</summary>
public sealed class UnavailableConversationAgentStreamReader : IAuthoritativeEventStreamReader
{
    /// <inheritdoc />
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthoritativeStreamReadResult(null, "source-provider-unconfigured"));
}
