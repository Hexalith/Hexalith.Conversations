// <copyright file="AbsentNamespaceSource.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Out-of-scope namespace source double; never certifies coverage.</summary>
internal sealed class AbsentNamespaceSource : ISourcePublicationNamespaceSource
{
    public Task<SourcePublicationCut?> ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken = default)
        => Task.FromResult<SourcePublicationCut?>(null);
}
