// <copyright file="ConversationProjectAssignmentOperation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Defines the explicit project-assignment operation for a conversation.
/// </summary>
[JsonConverter(typeof(ConversationProjectAssignmentOperationJsonConverter))]
public sealed record ConversationProjectAssignmentOperation
{
    /// <summary>
    /// Gets the operation for assigning or replacing a project reference.
    /// </summary>
    public static ConversationProjectAssignmentOperation Assign { get; } = new(nameof(Assign));

    /// <summary>
    /// Gets the operation for explicitly clearing the current project reference.
    /// </summary>
    public static ConversationProjectAssignmentOperation Clear { get; } = new(nameof(Clear));

    private static readonly IReadOnlyDictionary<string, ConversationProjectAssignmentOperation> KnownOperations =
        new[] { Assign, Clear }.ToDictionary(operation => operation.Value, StringComparer.Ordinal);

    private ConversationProjectAssignmentOperation(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the canonical operation value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported project-assignment operation.
    /// </summary>
    /// <param name="value">The operation value.</param>
    /// <returns>The supported operation.</returns>
    public static ConversationProjectAssignmentOperation Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownOperations.TryGetValue(value, out ConversationProjectAssignmentOperation? operation)
            ? operation
            : throw new ArgumentException($"Unsupported conversation project assignment operation '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
