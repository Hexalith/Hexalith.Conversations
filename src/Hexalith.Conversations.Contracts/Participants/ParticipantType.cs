// <copyright file="ParticipantType.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Participants;

/// <summary>
/// Defines the supported participant type vocabulary.
/// </summary>
[JsonConverter(typeof(ParticipantTypeJsonConverter))]
public sealed record ParticipantType
{
    /// <summary>
    /// Gets the human participant type.
    /// </summary>
    public static ParticipantType Human { get; } = new("Human");

    /// <summary>
    /// Gets the AI agent participant type.
    /// </summary>
    public static ParticipantType AiAgent { get; } = new("AIAgent");

    /// <summary>
    /// Gets the LLM participant type.
    /// </summary>
    public static ParticipantType Llm { get; } = new("LLM");

    private static readonly IReadOnlyDictionary<string, ParticipantType> KnownTypes =
        new[] { Human, AiAgent, Llm }.ToDictionary(type => type.Value, StringComparer.Ordinal);

    private ParticipantType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the canonical participant type value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported participant type.
    /// </summary>
    /// <param name="value">The participant type value.</param>
    /// <returns>The supported participant type.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is unsupported.</exception>
    public static ParticipantType Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownTypes.TryGetValue(value, out ParticipantType? type)
            ? type
            : throw new ArgumentException($"Unsupported participant type '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
