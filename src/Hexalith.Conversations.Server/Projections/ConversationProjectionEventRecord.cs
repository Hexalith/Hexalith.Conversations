// <copyright file="ConversationProjectionEventRecord.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.EventStore.Client.Subscriptions;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Carries a public conversation event and its safe source position for projection materialization.
/// </summary>
/// <param name="Position">The positive source position used for gap detection.</param>
/// <param name="Event">The public conversation event contract.</param>
/// <remarks>
/// Records produced by <see cref="ConversationProjectionEventDecoder"/> also carry the persisted binding
/// (event type, serialization format and payload SHA-256) that AD-8 duplicate tracking compares. A record built
/// directly from an in-memory event has no persisted bytes, so its binding is derived from the event's own
/// canonical JSON instead; the replay identity and selected time always come from the event itself.
/// </remarks>
public sealed record ConversationProjectionEventRecord(long Position, object Event)
{
    /// <summary>
    /// Gets the positive source position used for gap detection.
    /// </summary>
    public long Position { get; } = ValidatePosition(Position);

    /// <summary>
    /// Gets the public conversation event contract.
    /// </summary>
    public object Event { get; } = Event ?? throw new ArgumentNullException(nameof(Event));

    /// <summary>Gets the persisted event type discriminator, when the record was decoded from an envelope.</summary>
    internal string? EventTypeName { get; init; }

    /// <summary>Gets the persisted serialization format, when the record was decoded from an envelope.</summary>
    internal string? SerializationFormat { get; init; }

    /// <summary>Gets the lowercase-hex SHA-256 of the exact persisted payload bytes, when decoded from an envelope.</summary>
    internal string? PayloadSha256 { get; init; }

    /// <summary>Resolves the full AD-8 binding of this record for the supplied selected replay time.</summary>
    /// <param name="selectedAt">The validated selected replay time.</param>
    /// <returns>The binding duplicate tracking compares.</returns>
    internal ConversationReplayEventBinding ResolveBinding(DateTimeOffset selectedAt)
        => new(
            EventTypeName ?? Event.GetType().FullName ?? Event.GetType().Name,
            Position,
            SerializationFormat ?? "json",
            PayloadSha256 ?? EventStorePayloadFingerprint.Compute(
                JsonSerializer.SerializeToUtf8Bytes(Event, Event.GetType(), ConversationProjectionEventDecoder.EventJsonOptions)),
            selectedAt.UtcTicks);

    private static long ValidatePosition(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }
}
