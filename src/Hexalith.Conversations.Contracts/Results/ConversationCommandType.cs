// <copyright file="ConversationCommandType.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Results;

/// <summary>
/// Defines the supported public command type vocabulary for result contracts.
/// </summary>
[JsonConverter(typeof(ConversationCommandTypeJsonConverter))]
public sealed record ConversationCommandType
{
    /// <summary>
    /// Gets the command type for creating a new conversation.
    /// </summary>
    public static ConversationCommandType CreateConversationCommand { get; } = new(nameof(CreateConversationCommand));

    /// <summary>
    /// Gets the command type for appending a message to a conversation.
    /// </summary>
    public static ConversationCommandType AppendMessageCommand { get; } = new(nameof(AppendMessageCommand));

    /// <summary>
    /// Gets the command type for adding a participant to a conversation.
    /// </summary>
    public static ConversationCommandType AddParticipantCommand { get; } = new(nameof(AddParticipantCommand));

    /// <summary>
    /// Gets the command type for attaching a file reference to a conversation.
    /// </summary>
    public static ConversationCommandType AttachFileReferenceCommand { get; } = new(nameof(AttachFileReferenceCommand));

    /// <summary>
    /// Gets the command type for updating conversation metadata.
    /// </summary>
    public static ConversationCommandType UpdateConversationMetadataCommand { get; } = new(nameof(UpdateConversationMetadataCommand));

    /// <summary>
    /// Gets the command type for closing a conversation.
    /// </summary>
    public static ConversationCommandType CloseConversationCommand { get; } = new(nameof(CloseConversationCommand));

    /// <summary>
    /// Gets the command type for archiving a conversation.
    /// </summary>
    public static ConversationCommandType ArchiveConversationCommand { get; } = new(nameof(ArchiveConversationCommand));

    /// <summary>
    /// Gets the command type for setting or replacing a governed retention policy.
    /// </summary>
    public static ConversationCommandType SetConversationRetentionPolicyCommand { get; } = new(nameof(SetConversationRetentionPolicyCommand));

    /// <summary>
    /// Gets the command type for marking governed conversation content as sensitive.
    /// </summary>
    public static ConversationCommandType MarkConversationContentSensitiveCommand { get; } =
        new(nameof(MarkConversationContentSensitiveCommand));

    private static readonly IReadOnlyDictionary<string, ConversationCommandType> KnownTypes =
        new[]
        {
            CreateConversationCommand,
            AppendMessageCommand,
            AddParticipantCommand,
            AttachFileReferenceCommand,
            UpdateConversationMetadataCommand,
            CloseConversationCommand,
            ArchiveConversationCommand,
            SetConversationRetentionPolicyCommand,
            MarkConversationContentSensitiveCommand,
        }.ToDictionary(type => type.Value, StringComparer.Ordinal);

    private ConversationCommandType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the public command type value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported public command type. Matching is case-sensitive on canonical PascalCase values.
    /// </summary>
    /// <param name="value">The public command type value.</param>
    /// <returns>The matching command type.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or not a canonical supported value.</exception>
    public static ConversationCommandType Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownTypes.TryGetValue(value, out ConversationCommandType? type)
            ? type
            : throw new ArgumentException($"Unsupported conversation command type '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
