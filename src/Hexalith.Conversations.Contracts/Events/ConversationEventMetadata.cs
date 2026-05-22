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
/// <param name="correlationId">The correlation identifier.</param>
/// <param name="occurredAt">The timestamp for the persisted conversation fact.</param>
/// <param name="actorPartyId">The stable actor Party reference.</param>
/// <param name="causationId">The optional causation identifier.</param>
public sealed record ConversationEventMetadata(
    SchemaVersion SchemaVersion,
    string EventId,
    ConversationEventType EventType,
    TenantId TenantId,
    ConversationId ConversationId,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    PartyId ActorPartyId,
    string? CausationId = null)
{
    /// <summary>
    /// Gets the event schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; init; } = RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    /// <summary>
    /// Gets the public event identity chosen by the producer.
    /// </summary>
    public string EventId { get; init; } = ValidateRequired(EventId);

    /// <summary>
    /// Gets the public Conversations event type.
    /// </summary>
    public ConversationEventType EventType { get; init; } = RequireNonNull(EventType, nameof(EventType));

    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; init; } = RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the tenant-scoped conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; init; } = RequireNonNull(ConversationId, nameof(ConversationId));

    /// <summary>
    /// Gets the correlation identifier.
    /// </summary>
    public string CorrelationId { get; init; } = ValidateRequired(CorrelationId);

    /// <summary>
    /// Gets the timestamp for the persisted conversation fact.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = ValidateTimestamp(OccurredAt);

    /// <summary>
    /// Gets the stable actor Party reference.
    /// </summary>
    public PartyId ActorPartyId { get; init; } = RequireNonNull(ActorPartyId, nameof(ActorPartyId));

    /// <summary>
    /// Gets the optional causation identifier.
    /// </summary>
    public string? CausationId { get; init; } = CausationId;

    /// <summary>
    /// Gets the stable default deduplication key for at-least-once publication consumers.
    /// </summary>
    public string DeduplicationKey
        => FormattableString.Invariant($"tenant:{TenantId.Value}|conv:{ConversationId.Value}|{EventId}|{SchemaVersion.Value}");

    /// <summary>
    /// Gets the legacy source-compatible alias for the public occurrence timestamp.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset CommittedAt => OccurredAt;

    private static string ValidateRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static T RequireNonNull<T>(T value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset value)
    {
        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.");
        }

        if (value.Year < 2000 || value.Year > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must fall within the plausible business range (year 2000-9999).");
        }

        return value;
    }
}
