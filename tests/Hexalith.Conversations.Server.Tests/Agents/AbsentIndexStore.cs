// <copyright file="AbsentIndexStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Out-of-scope index store double; never persists.</summary>
internal sealed class AbsentIndexStore : ISourcePublicationIndexStore
{
    public Task<SourcePublicationIndexState?> ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken = default)
        => Task.FromResult<SourcePublicationIndexState?>(null);

    public Task<bool> TryWriteAsync(SourcePublicationScope scope, long expectedRevision, SourcePublicationIndexState state,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
