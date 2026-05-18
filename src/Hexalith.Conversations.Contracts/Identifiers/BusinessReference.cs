// <copyright file="BusinessReference.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// Carries an adopter-owned external business reference without making it conversation identity.
/// </summary>
public sealed record BusinessReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessReference"/> class.
    /// </summary>
    /// <param name="system">The owning business system or namespace.</param>
    /// <param name="value">The business reference value in the owning system.</param>
    public BusinessReference(string system, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        System = system;
        Value = value;
    }

    /// <summary>
    /// Gets the owning business system or namespace.
    /// </summary>
    public string System { get; init; }

    /// <summary>
    /// Gets the business reference value in the owning system.
    /// </summary>
    public string Value { get; init; }
}
