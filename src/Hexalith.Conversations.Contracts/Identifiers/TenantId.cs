// <copyright file="TenantId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream tenant identity without copying tenant lifecycle or authorization state.
/// </summary>
/// <param name="value">The opaque stable tenant identifier.</param>
/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
[JsonConverter(typeof(TenantIdJsonConverter))]
public sealed record TenantId(string Value)
{
    /// <summary>
    /// Gets the opaque stable tenant identifier.
    /// </summary>
    public string Value { get; } = Validate(Value);

    private static string Validate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
