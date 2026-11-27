// <copyright file="ConversationPageValidationRecord.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Minimal subset of <see cref="ConversationProjectedReadModels"/> deserialized during
/// <see cref="ConversationProjectionReadStore.ValidatePageAsync"/> to prove cross-key generation consistency
/// without allocating full detail bodies.
/// </summary>
/// <param name="Summary">The persisted conversation summary.</param>
/// <param name="Detail">The persisted detail header (tenant, conversation, and freshness).</param>
internal sealed record ConversationPageValidationRecord(
    ConversationSummaryProjectionV1 Summary,
    ConversationDetailValidationHeader Detail)
{
    /// <summary>Gets the persisted conversation summary.</summary>
    public ConversationSummaryProjectionV1 Summary { get; } = Summary ?? throw new ArgumentNullException(nameof(Summary));

    /// <summary>Gets the persisted detail header.</summary>
    public ConversationDetailValidationHeader Detail { get; } = Detail ?? throw new ArgumentNullException(nameof(Detail));

    /// <summary>Gets the stable dispatch identity that produced this generation.</summary>
    public string DispatchId { get; init; } = string.Empty;
}
