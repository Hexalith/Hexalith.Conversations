// <copyright file="SchemaVersion.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Versioning;

/// <summary>
/// Represents an explicit positive integer schema version.
/// </summary>
/// <param name="value">The positive schema version number.</param>
/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than one.</exception>
[JsonConverter(typeof(SchemaVersionJsonConverter))]
public sealed record SchemaVersion(int Value)
{
    /// <summary>
    /// Gets the current v1 contract schema version.
    /// </summary>
    public static SchemaVersion Current { get; } = new(1);

    /// <summary>
    /// Gets the positive schema version number.
    /// </summary>
    public int Value { get; } = Validate(Value);

    private static int Validate(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }
}
