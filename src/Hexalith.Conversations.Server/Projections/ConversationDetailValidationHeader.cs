// <copyright file="ConversationDetailValidationHeader.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Minimal header subset of <see cref="ConversationDetailProjectionV1"/> deserialized during list page
/// verification so full message timelines, participants, and evidence arrays are not allocated per row.
/// </summary>
/// <param name="TenantId">The detail record's tenant identity.</param>
/// <param name="ConversationId">The detail record's conversation identity.</param>
/// <param name="Freshness">The detail record's server-computed freshness metadata.</param>
internal sealed record ConversationDetailValidationHeader(
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionFreshnessV1 Freshness)
{
    /// <summary>Gets the detail record's tenant identity.</summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>Gets the detail record's conversation identity.</summary>
    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    /// <summary>Gets the detail record's server-computed freshness metadata.</summary>
    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));
}
