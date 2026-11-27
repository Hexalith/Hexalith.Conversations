// <copyright file="ConversationProjectionIndexLookupResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Targeted single-conversation projection extracted from a persisted tenant index without deserializing
/// unrelated summaries or dispatch references.
/// </summary>
[JsonConverter(typeof(ConversationProjectionIndexLookupConverter))]
internal sealed class ConversationProjectionIndexLookupResult
{
    /// <summary>Gets the first matching summary for the requested conversation, when present.</summary>
    internal ConversationSummaryProjectionV1? MatchedSummary { get; init; }

    /// <summary>Gets the total number of summary entries matching the requested conversation (values &gt; 1 indicate duplicates).</summary>
    internal int MatchCount { get; init; }

    /// <summary>Gets the dispatch reference for the requested conversation, when present.</summary>
    internal ConversationProjectionDispatchReference? DispatchReference { get; init; }
}
