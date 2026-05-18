// <copyright file="MessageId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// Identifies a message within a tenant-scoped conversation.
/// </summary>
/// <param name="value">The opaque stable message identifier.</param>
/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
[JsonConverter(typeof(MessageIdJsonConverter))]
public sealed record MessageId(string Value)
{
    /// <summary>
    /// Gets the opaque stable message identifier.
    /// </summary>
    public string Value { get; } = Validate(Value);

    private static string Validate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
