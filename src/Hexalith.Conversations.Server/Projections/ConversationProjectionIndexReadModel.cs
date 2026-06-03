// <copyright file="ConversationProjectionIndexReadModel.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Cross-conversation, per-tenant summary index persisted as a single state-store value so the list boundary
/// reads the tenant's visible summaries with one store read rather than a per-conversation fan-out (NFR2).
/// </summary>
/// <remarks>
/// This is a Server-internal persistence shape; the persisted elements are the existing public
/// <see cref="ConversationSummaryProjectionV1"/> Contracts type, so the public contract-shape baseline is
/// unaffected. The index is maintained through <see cref="Hexalith.EventStore.Client.Projections.ReadModelWritePolicy"/>'s
/// idempotent reload-and-merge path (dedup by conversation identity, newest generation wins).
/// </remarks>
public sealed class ConversationProjectionIndexReadModel
{
    /// <summary>
    /// Gets the visible conversation summaries for the tenant scope.
    /// </summary>
    public IReadOnlyList<ConversationSummaryProjectionV1> Summaries { get; init; } = [];
}
