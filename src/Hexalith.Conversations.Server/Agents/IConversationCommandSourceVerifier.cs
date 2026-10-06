// <copyright file="IConversationCommandSourceVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Certifies the complete SDK-supplied command prefix at the authenticated transport boundary.</summary>
/// <remarks>The owning EventStore actor must authenticate the exact tenant/domain/stream, complete source
/// revision and event bytes, and serialize compare/append at that same owner. Caller-claimed revisions,
/// uncertified snapshots, a projection or a gateway read during that actor's command do not provide proof.
/// This port does not deliver the missing production SDK transport attestation.</remarks>
public interface IConversationCommandSourceVerifier
{
    /// <summary>Verifies the bound source without re-entering the actor or querying storage.</summary>
    /// <param name="request">The exact authenticated SDK transport request.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>An authenticated complete prefix or Unavailable.</returns>
    Task<AuthoritativeStreamReadResult> VerifyAsync(DomainServiceRequest request, CancellationToken cancellationToken = default);
}
