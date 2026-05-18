// <copyright file="MessageId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// Identifies a message within a tenant-scoped conversation.
/// </summary>
public sealed record MessageId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageId"/> class.
    /// </summary>
    /// <param name="value">The opaque stable message identifier.</param>
    public MessageId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque stable message identifier.
    /// </summary>
    public string Value { get; init; }
}
