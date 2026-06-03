// <copyright file="ConversationProjectionReadStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Production <see cref="IConversationProjectionReadStore"/> that reads persisted conversation read models
/// through the shared EventStore <see cref="IReadModelStore"/> by stable, tenant-scoped key.
/// </summary>
/// <remarks>
/// This closes the production read-store binding deferred from Story 2.3: the five query/governance services
/// that require <see cref="IConversationProjectionReadStore"/> now resolve from the real host. The read side
/// is intentionally thin — authorization, freshness gating, and the fail-closed shapes stay in
/// <see cref="ConversationProjectionReadService"/> and <see cref="Queries.ConversationQueryHandler"/>, which
/// authorize before any store read. A different tenant resolves to a different key, so cross-tenant reads are
/// impossible by construction.
/// </remarks>
public sealed class ConversationProjectionReadStore : IConversationProjectionReadStore
{
    private readonly IReadModelStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionReadStore"/> class.
    /// </summary>
    /// <param name="store">The shared read-model store.</param>
    public ConversationProjectionReadStore(IReadModelStore store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <inheritdoc/>
    public async ValueTask<ConversationProjectedReadModels?> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);

        ReadModelEntry<ConversationProjectedReadModels> entry = await _store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(tenantId, conversationId),
                cancellationToken)
            .ConfigureAwait(false);

        return entry.Value;
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        // Single tenant-scoped index read — never a per-conversation fan-out (NFR2, no N+1).
        ReadModelEntry<ConversationProjectionIndexReadModel> entry = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(tenantId),
                cancellationToken)
            .ConfigureAwait(false);

        return entry.Value?.Summaries ?? [];
    }
}
