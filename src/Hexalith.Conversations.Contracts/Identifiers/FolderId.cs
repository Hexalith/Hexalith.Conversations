// <copyright file="FolderId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// References an upstream folder identity without copying folder state.
/// </summary>
/// <param name="value">The opaque stable folder identifier.</param>
/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
[JsonConverter(typeof(FolderIdJsonConverter))]
public sealed record FolderId(string Value)
{
    /// <summary>
    /// Gets the opaque stable folder identifier.
    /// </summary>
    public string Value { get; } = Validate(Value);

    private static string Validate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
