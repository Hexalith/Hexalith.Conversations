// <copyright file="ConversationLifecycleStatus.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Defines the bounded public lifecycle state vocabulary.
/// </summary>
[JsonConverter(typeof(ConversationLifecycleStatusJsonConverter))]
public sealed record ConversationLifecycleStatus
{
    /// <summary>
    /// Gets the open lifecycle state.
    /// </summary>
    public static ConversationLifecycleStatus Open { get; } = new(nameof(Open));

    /// <summary>
    /// Gets the closed lifecycle state.
    /// </summary>
    public static ConversationLifecycleStatus Closed { get; } = new(nameof(Closed));

    /// <summary>
    /// Gets the archived lifecycle state.
    /// </summary>
    public static ConversationLifecycleStatus Archived { get; } = new(nameof(Archived));

    private static readonly IReadOnlyDictionary<string, ConversationLifecycleStatus> KnownStates =
        new[]
        {
            Open,
            Closed,
            Archived,
        }.ToDictionary(state => state.Value, StringComparer.Ordinal);

    private ConversationLifecycleStatus(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the public lifecycle state value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported lifecycle state.
    /// </summary>
    /// <param name="value">The lifecycle state value.</param>
    /// <returns>The matching lifecycle state.</returns>
    public static ConversationLifecycleStatus Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownStates.TryGetValue(value, out ConversationLifecycleStatus? state)
            ? state
            : throw new ArgumentException($"Unsupported conversation lifecycle state '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
