// <copyright file="ConversationProjectionIndexLookupTarget.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Holds the pre-encoded UTF-8 wire representations of a target conversation identifier for targeted
/// <see cref="System.Text.Json.Utf8JsonReader"/> index lookups.
/// </summary>
internal sealed class ConversationProjectionIndexLookupTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionIndexLookupTarget"/> class.
    /// </summary>
    /// <param name="conversationId">The raw conversation identifier value.</param>
    internal ConversationProjectionIndexLookupTarget(string conversationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ConversationId = conversationId;
        RawIdUtf8 = Encoding.UTF8.GetBytes(conversationId);
        PrefixedIdUtf8 = Encoding.UTF8.GetBytes($"conv:{conversationId}");
    }

    /// <summary>Gets the raw conversation identifier value (used as the dispatch map key).</summary>
    internal string ConversationId { get; }

    /// <summary>Gets the UTF-8 encoded raw conversation identifier value.</summary>
    internal byte[] RawIdUtf8 { get; }

    /// <summary>Gets the UTF-8 encoded prefixed wire representation (<c>conv:&lt;id&gt;</c>).</summary>
    internal byte[] PrefixedIdUtf8 { get; }
}
