// <copyright file="DerivedKeyRecoverableRebuildBatchStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Enables the SDK's durable manifest retention for the interrupted-rebuild test.</summary>
/// <param name="store">The SDK batch store that persists the original rebuild manifest.</param>
internal sealed class DerivedKeyRecoverableRebuildBatchStore(IReadModelBatchStore store) : IReadModelBatchStore
{
    /// <inheritdoc/>
    public void ValidateBatch(ReadModelBatch batch) => store.ValidateBatch(batch);

    /// <inheritdoc/>
    public Task<ReadModelBatchResult> ExecuteAsync(ReadModelBatch batch, CancellationToken cancellationToken = default)
    {
        return store.ExecuteAsync(new ReadModelBatch(batch.Scope, batch.Operations)
        {
            RetainManifestForRecovery = true,
            DiscoverableRecovery = true,
        }, cancellationToken);
    }
}
