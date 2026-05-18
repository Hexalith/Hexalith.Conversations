// <copyright file="FolderId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream folder identity without copying folder state.
/// </summary>
public sealed record FolderId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FolderId"/> class.
    /// </summary>
    /// <param name="value">The opaque stable folder identifier.</param>
    public FolderId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque stable folder identifier.
    /// </summary>
    public string Value { get; init; }
}
