// <copyright file="PartyId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream Party identity without carrying personal profile data.
/// </summary>
public sealed record PartyId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PartyId"/> class.
    /// </summary>
    /// <param name="value">The opaque stable Party identifier.</param>
    public PartyId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque stable Party identifier.
    /// </summary>
    public string Value { get; init; }
}
