// <copyright file="ConversationProjectedReadModels.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Carries summary and detail projections from the same materialization pass.
/// </summary>
/// <param name="Summary">The projected conversation summary.</param>
/// <param name="Detail">The projected conversation detail.</param>
public sealed record ConversationProjectedReadModels(
    ConversationSummaryProjectionV1 Summary,
    ConversationDetailProjectionV1 Detail)
{
    /// <summary>
    /// Gets the projected conversation summary.
    /// </summary>
    public ConversationSummaryProjectionV1 Summary { get; } = Summary ?? throw new ArgumentNullException(nameof(Summary));

    /// <summary>
    /// Gets the projected conversation detail.
    /// </summary>
    public ConversationDetailProjectionV1 Detail { get; } = Detail ?? throw new ArgumentNullException(nameof(Detail));
}
