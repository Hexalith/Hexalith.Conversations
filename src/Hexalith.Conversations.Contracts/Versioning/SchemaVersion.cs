// <copyright file="SchemaVersion.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Versioning;

/// <summary>
/// Represents an explicit positive integer schema version.
/// </summary>
public sealed record SchemaVersion
{
    /// <summary>
    /// Gets the current v1 contract schema version.
    /// </summary>
    public static SchemaVersion Current { get; } = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaVersion"/> class.
    /// </summary>
    /// <param name="value">The positive schema version number.</param>
    public SchemaVersion(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        Value = value;
    }

    /// <summary>
    /// Gets the positive schema version number.
    /// </summary>
    public int Value { get; init; }
}
