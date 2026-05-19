// <copyright file="ConversationCreated.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>
/// Records the creation of a tenant-scoped conversation as a durable EventStore event.
/// </summary>
/// <remarks>
/// This is the domain-side replay-safe payload. It carries the public
/// <see cref="ConversationEventMetadata"/> verbatim plus the originating idempotency key
/// needed by the replay path. The public adopter-facing publication shape is owned by
/// Story 1.10 and lives under <c>Hexalith.Conversations.Contracts.Events</c>.
/// </remarks>
/// <param name="metadata">The Conversations event metadata. Must not be null.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="projectId">An optional stable project reference.</param>
/// <param name="folderId">An optional stable folder reference.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="idempotencyKey">The caller idempotency key copied from command metadata, when supplied.</param>
/// <param name="providerCorrelation">Optional provider correlation metadata.</param>
public sealed record ConversationCreatedDomainEvent(
    ConversationEventMetadata Metadata,
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    string? Label = null,
    string? IdempotencyKey = null,
    ProviderCorrelationMetadata? ProviderCorrelation = null) : IEventPayload
{
    /// <summary>
    /// Gets the Conversations event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = Metadata ?? throw new ArgumentNullException(nameof(Metadata));

    /// <summary>
    /// Gets the deterministic creation timestamp copied from event metadata.
    /// </summary>
    public DateTimeOffset CreatedAt => Metadata.CommittedAt;
}
