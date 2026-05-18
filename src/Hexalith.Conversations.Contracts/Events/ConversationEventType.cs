// <copyright file="ConversationEventType.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Defines the supported public event type vocabulary.
/// </summary>
[JsonConverter(typeof(ConversationEventTypeJsonConverter))]
public sealed record ConversationEventType
{
    public static ConversationEventType ConversationCreated { get; } = new(nameof(ConversationCreated));
    public static ConversationEventType MessageAppended { get; } = new(nameof(MessageAppended));
    public static ConversationEventType ParticipantAdded { get; } = new(nameof(ParticipantAdded));
    public static ConversationEventType FileReferenceAttached { get; } = new(nameof(FileReferenceAttached));
    public static ConversationEventType ConversationMetadataUpdated { get; } = new(nameof(ConversationMetadataUpdated));
    public static ConversationEventType ConversationClosed { get; } = new(nameof(ConversationClosed));
    public static ConversationEventType ConversationArchived { get; } = new(nameof(ConversationArchived));

    private static readonly IReadOnlyDictionary<string, ConversationEventType> KnownTypes =
        new[]
        {
            ConversationCreated,
            MessageAppended,
            ParticipantAdded,
            FileReferenceAttached,
            ConversationMetadataUpdated,
            ConversationClosed,
            ConversationArchived,
        }.ToDictionary(type => type.Value, StringComparer.Ordinal);

    private ConversationEventType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the public event type value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported public event type.
    /// </summary>
    /// <param name="value">The public event type value.</param>
    /// <returns>The matching event type.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or unsupported.</exception>
    public static ConversationEventType Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownTypes.TryGetValue(value, out ConversationEventType? type)
            ? type
            : throw new ArgumentException($"Unsupported conversation event type '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
