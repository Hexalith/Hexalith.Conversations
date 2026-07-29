// <copyright file="IConversationProjectionReadStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Reads derived conversation projections by tenant and conversation identity.
/// </summary>
public interface IConversationProjectionReadStore
{
    /// <summary>
    /// Reads the projected summary/detail pair.
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="conversationId">The conversation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The projection pair, or null when no visible projection exists.</returns>
    /// <exception cref="ConversationProjectionConsistencyException">
    /// Thrown when the detail key, the tenant index and the dispatch ledger do not agree on one completed
    /// generation for this conversation.
    /// </exception>
    ValueTask<ConversationProjectedReadModels?> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the tenant index once and returns its candidate summaries together with the dispatch references
    /// needed to judge cross-key generation state.
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The index snapshot. A tenant with no persisted index yields
    /// <see cref="ConversationProjectionIndexSnapshot.Empty"/> rather than an error: having no conversations is
    /// not a cross-key inconsistency.
    /// </returns>
    /// <exception cref="ConversationProjectionConsistencyException">
    /// Thrown only when the index itself is structurally corrupt, such as naming one conversation twice.
    /// </exception>
    ValueTask<ConversationProjectionIndexSnapshot> ListAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies one already-paged candidate set against its detail keys and dispatch ledgers.
    /// </summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="snapshot">The snapshot the page was taken from, reused so the index is not read twice.</param>
    /// <param name="page">The candidate summaries actually about to be returned.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The identifiers of page rows that do not prove one completed generation across the detail key, the index
    /// entry and the dispatch ledger. Callers must not present those rows; rows absent from this set are proven.
    /// </returns>
    /// <remarks>
    /// The store reports which rows failed instead of throwing, so one conversation mid-dispatch cannot make an
    /// unrelated conversation unreadable. Structural store faults still throw.
    /// </remarks>
    ValueTask<IReadOnlySet<string>> ValidatePageAsync(
        TenantId tenantId,
        ConversationProjectionIndexSnapshot snapshot,
        IReadOnlyList<ConversationSummaryProjectionV1> page,
        CancellationToken cancellationToken = default);
}
