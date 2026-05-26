// <copyright file="ConversationProjectChanged.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a conversation project assignment changed.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="previousProjectId">The previous stable project reference, when assigned.</param>
/// <param name="currentProjectId">The current stable project reference, when assigned.</param>
public sealed record ConversationProjectChanged(
    ConversationEventMetadata Metadata,
    ProjectId? PreviousProjectId = null,
    ProjectId? CurrentProjectId = null)
{
    /// <summary>
    /// Gets the public project-assignment timestamp from metadata.
    /// </summary>
    public DateTimeOffset ChangedAt => Metadata.CommittedAt;
}
