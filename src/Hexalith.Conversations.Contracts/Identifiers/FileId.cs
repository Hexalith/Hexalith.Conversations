// <copyright file="FileId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream file identity without carrying file bytes.
/// </summary>
public sealed record FileId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileId"/> class.
    /// </summary>
    /// <param name="value">The opaque stable file identifier.</param>
    public FileId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque stable file identifier.
    /// </summary>
    public string Value { get; init; }
}
