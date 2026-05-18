// <copyright file="PartyId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream Party identity without carrying personal profile data.
/// </summary>
/// <param name="value">The opaque stable Party identifier.</param>
/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
[JsonConverter(typeof(PartyIdJsonConverter))]
public sealed record PartyId(string Value)
{
    /// <summary>
    /// Gets the opaque stable Party identifier.
    /// </summary>
    public string Value { get; } = Validate(Value);

    private static string Validate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
