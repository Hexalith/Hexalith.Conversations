// <copyright file="ConversationId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// Identifies a Conversations-owned conversation within a tenant scope.
/// </summary>
public sealed record ConversationId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationId"/> class.
    /// </summary>
    /// <param name="value">The opaque durable conversation identifier.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public ConversationId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque durable conversation identifier.
    /// </summary>
    public string Value { get; init; }
}
