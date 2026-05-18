// <copyright file="TenantId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream tenant identity without copying tenant lifecycle or authorization state.
/// </summary>
public sealed record TenantId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantId"/> class.
    /// </summary>
    /// <param name="value">The opaque stable tenant identifier.</param>
    public TenantId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque stable tenant identifier.
    /// </summary>
    public string Value { get; init; }
}
