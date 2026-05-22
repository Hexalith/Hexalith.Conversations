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
    /// <summary>
    /// Gets the event type for a conversation creation.
    /// </summary>
    public static ConversationEventType ConversationCreated { get; } = new(nameof(ConversationCreated));

    /// <summary>
    /// Gets the event type for a message appended to a conversation.
    /// </summary>
    public static ConversationEventType MessageAppended { get; } = new(nameof(MessageAppended));

    /// <summary>
    /// Gets the event type for a participant added to a conversation.
    /// </summary>
    public static ConversationEventType ParticipantAdded { get; } = new(nameof(ParticipantAdded));

    /// <summary>
    /// Gets the event type for a file reference attached to a conversation.
    /// </summary>
    public static ConversationEventType FileReferenceAttached { get; } = new(nameof(FileReferenceAttached));

    /// <summary>
    /// Gets the event type for conversation metadata updates.
    /// </summary>
    public static ConversationEventType ConversationMetadataUpdated { get; } = new(nameof(ConversationMetadataUpdated));

    /// <summary>
    /// Gets the event type for a conversation closure.
    /// </summary>
    public static ConversationEventType ConversationClosed { get; } = new(nameof(ConversationClosed));

    /// <summary>
    /// Gets the event type for a conversation archival.
    /// </summary>
    public static ConversationEventType ConversationArchived { get; } = new(nameof(ConversationArchived));

    /// <summary>
    /// Gets the event type for bounded conversation lifecycle changes.
    /// </summary>
    public static ConversationEventType ConversationLifecycleChanged { get; } = new(nameof(ConversationLifecycleChanged));

    /// <summary>
    /// Gets the event type for a governed retention policy set operation.
    /// </summary>
    public static ConversationEventType RetentionPolicySet { get; } = new(nameof(RetentionPolicySet));

    /// <summary>
    /// Gets the event type for a governed retention policy replacement operation.
    /// </summary>
    public static ConversationEventType RetentionPolicyReplaced { get; } = new(nameof(RetentionPolicyReplaced));

    /// <summary>
    /// Gets the event type for a governed sensitivity mark operation.
    /// </summary>
    public static ConversationEventType ConversationContentMarkedSensitive { get; } = new(nameof(ConversationContentMarkedSensitive));

    /// <summary>
    /// Gets the event type for a governed message-content redaction operation.
    /// </summary>
    public static ConversationEventType MessageContentRedacted { get; } = new(nameof(MessageContentRedacted));

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
            ConversationLifecycleChanged,
            RetentionPolicySet,
            RetentionPolicyReplaced,
            ConversationContentMarkedSensitive,
            MessageContentRedacted,
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
    /// Resolves a supported public event type. Matching is case-sensitive on canonical PascalCase values.
    /// </summary>
    /// <param name="value">The public event type value.</param>
    /// <returns>The matching event type.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or not a canonical supported value.</exception>
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
