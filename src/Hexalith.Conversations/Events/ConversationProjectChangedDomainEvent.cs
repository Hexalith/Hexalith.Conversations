// <copyright file="ConversationProjectChangedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>
/// Records a Conversations-owned project assignment change using stable project references only.
/// </summary>
/// <param name="metadata">The Conversations event metadata. Must not be null.</param>
/// <param name="previousProjectId">The previous stable project reference, when assigned.</param>
/// <param name="currentProjectId">The current stable project reference, when assigned.</param>
public sealed record ConversationProjectChangedDomainEvent(
    ConversationEventMetadata Metadata,
    ProjectId? PreviousProjectId = null,
    ProjectId? CurrentProjectId = null) : IEventPayload
{
    /// <summary>
    /// Gets the Conversations event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = Metadata ?? throw new ArgumentNullException(nameof(Metadata));

    /// <summary>
    /// Gets the deterministic project-change timestamp copied from event metadata.
    /// </summary>
    public DateTimeOffset ChangedAt => Metadata.CommittedAt;
}
