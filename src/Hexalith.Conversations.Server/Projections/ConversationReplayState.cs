// <copyright file="ConversationReplayState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// The deterministic replay state of one conversation materialization (AD-8): the materialized summary and
/// detail with observation freshness removed, plus <c>LastAppliedEventPosition</c> and <c>ReplayAnchorAt</c>.
/// </summary>
/// <remarks>
/// <para>
/// The value is canonicalized by EventStore's coordinated-batch canonicalizer (fixed web defaults, compact UTF-8,
/// recursively ordinal-sorted object properties). Arrays keep the explicit order the materializer gives them;
/// an unordered collection type fails before any byte is produced (see
/// <see cref="ConversationReplayStateCanonicalizer"/>). <see cref="Hash"/> is the lowercase-hex SHA-256 of
/// <see cref="CanonicalBytes"/>.
/// </para>
/// <para>
/// Only the observation fields of every freshness value (<c>projectionGeneratedAt</c>, <c>lagDuration</c>,
/// <c>isStale</c>) are removed. The materializer evaluates the replay projection at its own anchor, so the
/// remaining trust states describe replay facts (gaps, contradictions, poison), never the clock.
/// </para>
/// </remarks>
internal sealed class ConversationReplayState
{
    /// <summary>The version of the replay-state value shape.</summary>
    internal const int ReplayStateVersion = 1;

    private static readonly string[] ObservationFields = ["projectionGeneratedAt", "lagDuration", "isStale"];

    private ConversationReplayState(DateTimeOffset? replayAnchorAt, byte[] canonicalBytes)
    {
        ReplayAnchorAt = replayAnchorAt;
        CanonicalBytes = canonicalBytes;
        Hash = ConversationReplayStateCanonicalizer.Hash(canonicalBytes);
    }

    /// <summary>Gets the maximum validated selected event time contributing to this state, in UTC.</summary>
    public DateTimeOffset? ReplayAnchorAt { get; }

    /// <summary>Gets the exact EventStore-canonical bytes of the replay-state value.</summary>
    public ReadOnlyMemory<byte> CanonicalBytes { get; }

    /// <summary>Gets the lowercase-hex SHA-256 of <see cref="CanonicalBytes"/>.</summary>
    public string Hash { get; }

    /// <summary>Creates the replay state from an observation-free summary/detail pair.</summary>
    /// <param name="summary">The summary materialized without observation.</param>
    /// <param name="detail">The detail materialized without observation.</param>
    /// <param name="replayAnchorAt">The replay anchor, or <see langword="null"/> when none applies.</param>
    /// <returns>The canonical replay state.</returns>
    internal static ConversationReplayState Create(
        ConversationSummaryProjectionV1 summary,
        ConversationDetailProjectionV1 detail,
        DateTimeOffset? replayAnchorAt)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(detail);

        JsonObject state = new()
        {
            ["replayStateVersion"] = ReplayStateVersion,
            ["tenantId"] = summary.TenantId.Value,
            ["conversationId"] = summary.ConversationId.Value,
            ["lastAppliedEventPosition"] = summary.Freshness.LastAppliedEventPosition,
            ["replayAnchorAt"] = replayAnchorAt is { } anchor ? JsonValue.Create(anchor) : null,
            ["summary"] = WithoutObservation(ConversationReplayStateCanonicalizer.ToOrderedNode(summary)),
            ["detail"] = WithoutObservation(ConversationReplayStateCanonicalizer.ToOrderedNode(detail)),
        };

        return new(replayAnchorAt, ConversationReplayStateCanonicalizer.Canonicalize(state));
    }

    /// <summary>Computes a clock-free identity of one summary, used where an ordering needs a stable tie-break.</summary>
    /// <param name="summary">The summary.</param>
    /// <returns>The lowercase-hex SHA-256 of the canonical summary without observation freshness.</returns>
    internal static string SummaryIdentity(ConversationSummaryProjectionV1 summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        JsonNode node = WithoutObservation(ConversationReplayStateCanonicalizer.ToOrderedNode(summary));
        return ConversationReplayStateCanonicalizer.Hash(ConversationReplayStateCanonicalizer.Canonicalize(node));
    }

    private static JsonNode WithoutObservation(JsonNode node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                if (jsonObject.ContainsKey("projectionContractSchemaVersion"))
                {
                    foreach (string field in ObservationFields)
                    {
                        _ = jsonObject.Remove(field);
                    }
                }

                foreach (KeyValuePair<string, JsonNode?> child in jsonObject.ToList())
                {
                    if (child.Value is not null)
                    {
                        _ = WithoutObservation(child.Value);
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (JsonNode? item in jsonArray)
                {
                    if (item is not null)
                    {
                        _ = WithoutObservation(item);
                    }
                }

                break;
        }

        return node;
    }
}
