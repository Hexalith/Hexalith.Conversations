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
/// <remarks>
/// The constructor normalizes <paramref name="Value"/> by trimming surrounding whitespace and
/// converting to ordinal lowercase. Normalization closes substitution bypasses where two PartyIds
/// differing only by case or whitespace would otherwise be treated as distinct identities
/// (for example, when comparing against provider correlation values).
/// </remarks>
/// <param name="Value">The opaque stable Party identifier.</param>
/// <exception cref="ArgumentException">Thrown when <paramref name="Value"/> is empty or whitespace.</exception>
[JsonConverter(typeof(PartyIdJsonConverter))]
public sealed record PartyId(string Value)
{
    /// <summary>
    /// Gets the opaque stable Party identifier in normalized form (trimmed, ordinal lowercase).
    /// </summary>
    public string Value { get; } = Normalize(Value);

    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().ToLowerInvariant();
    }
}
