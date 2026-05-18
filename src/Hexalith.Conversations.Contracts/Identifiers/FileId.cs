// <copyright file="FileId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream file identity without carrying file bytes.
/// </summary>
/// <param name="value">The opaque stable file identifier.</param>
/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
[JsonConverter(typeof(FileIdJsonConverter))]
public sealed record FileId(string Value)
{
    /// <summary>
    /// Gets the opaque stable file identifier.
    /// </summary>
    public string Value { get; } = Validate(Value);

    private static string Validate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
