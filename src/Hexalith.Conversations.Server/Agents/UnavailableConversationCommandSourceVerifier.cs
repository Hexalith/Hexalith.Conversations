// <copyright file="UnavailableConversationCommandSourceVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>The current SDK transport has no configured authenticated complete-prefix attestation.</summary>
public sealed class UnavailableConversationCommandSourceVerifier : IConversationCommandSourceVerifier
{
    /// <inheritdoc />
    public Task<AuthoritativeStreamReadResult> VerifyAsync(DomainServiceRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthoritativeStreamReadResult(null, "authenticated-command-source-provider-unconfigured"));
}
