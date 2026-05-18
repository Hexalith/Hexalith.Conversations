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
/// <param name="committedAt">The committed timestamp for the public contract.</param>
/// <param name="actorPartyId">The stable actor Party reference.</param>
/// <param name="causationId">The optional causation identifier.</param>
public sealed record ConversationEventMetadata(
    SchemaVersion SchemaVersion,
    string EventId,
    ConversationEventType EventType,
    TenantId TenantId,
    ConversationId ConversationId,
    string CorrelationId,
    DateTimeOffset CommittedAt,
    PartyId ActorPartyId,
    string? CausationId = null)
{
    /// <summary>
    /// Gets the event schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    /// <summary>
    /// Gets the public event identity chosen by the producer.
    /// </summary>
    public string EventId { get; } = ValidateRequired(EventId);

    /// <summary>
    /// Gets the public Conversations event type.
    /// </summary>
    public ConversationEventType EventType { get; } = RequireNonNull(EventType, nameof(EventType));

    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the tenant-scoped conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = RequireNonNull(ConversationId, nameof(ConversationId));

    /// <summary>
    /// Gets the correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId);

    /// <summary>
    /// Gets the committed timestamp for the public contract.
    /// </summary>
    public DateTimeOffset CommittedAt { get; } = ValidateTimestamp(CommittedAt);

    /// <summary>
    /// Gets the stable actor Party reference.
    /// </summary>
    public PartyId ActorPartyId { get; } = RequireNonNull(ActorPartyId, nameof(ActorPartyId));

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
