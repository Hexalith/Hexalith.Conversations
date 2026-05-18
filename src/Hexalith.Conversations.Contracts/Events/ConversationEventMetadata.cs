// <copyright file="ConversationEventMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Carries public event metadata without exposing persistence substrate mechanics.
/// </summary>
/// <param name="schemaVersion">The event schema version.</param>
/// <param name="eventId">The public event identity chosen by the producer.</param>
/// <param name="eventType">The public Conversations event type.</param>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="actorPartyId">The optional stable actor Party reference.</param>
/// <param name="correlationId">The correlation identifier.</param>
/// <param name="causationId">The optional causation identifier.</param>
/// <param name="committedAt">The committed timestamp for the public contract.</param>
public sealed record ConversationEventMetadata(
    SchemaVersion SchemaVersion,
    string EventId,
    ConversationEventType EventType,
    TenantId TenantId,
    ConversationId ConversationId,
    string CorrelationId,
    DateTimeOffset CommittedAt,
    PartyId? ActorPartyId = null,
    string? CausationId = null)
{
    /// <summary>
    /// Gets the public event identity chosen by the producer.
    /// </summary>
    public string EventId { get; } = ValidateRequired(EventId);

    /// <summary>
    /// Gets the correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId);

    /// <summary>
    /// Gets the committed timestamp for the public contract.
    /// </summary>
    public DateTimeOffset CommittedAt { get; } = ValidateTimestamp(CommittedAt);

    private static string ValidateRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset value)
        => value <= DateTimeOffset.MinValue
            ? throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.")
            : value;
}
