// <copyright file="ProjectId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream project identity without copying project state.
/// </summary>
public sealed record ProjectId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectId"/> class.
    /// </summary>
    /// <param name="value">The opaque stable project identifier.</param>
    public ProjectId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque stable project identifier.
    /// </summary>
    public string Value { get; init; }
}
