// <copyright file="ConversationProjectedReadModels.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Carries summary and detail projections from the same materialization pass.
/// </summary>
/// <param name="Summary">The projected conversation summary.</param>
/// <param name="Detail">The projected conversation detail.</param>
public sealed record ConversationProjectedReadModels(
    ConversationSummaryProjectionV1 Summary,
    ConversationDetailProjectionV1 Detail)
{
    /// <summary>Gets the original frozen admission decision; omitted for legacy generations.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ConversationProjectionNativeAdmission? NativeAdmission { get; init; }

    /// <summary>Gets the original SDK-verified native incarnation and complete prefix; absent for legacy input.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Hexalith.EventStore.Contracts.Streams.NativeSourceProof? NativeSourceProof { get; init; }

    /// <summary>
    /// Gets the projected conversation summary.
    /// </summary>
    public ConversationSummaryProjectionV1 Summary { get; } = Summary ?? throw new ArgumentNullException(nameof(Summary));

    /// <summary>
    /// Gets the projected conversation detail.
    /// </summary>
    public ConversationDetailProjectionV1 Detail { get; } = Detail ?? throw new ArgumentNullException(nameof(Detail));

    /// <summary>
    /// Gets the stable dispatch identity that produced this internal persisted generation.
    /// </summary>
    public string DispatchId { get; init; } = string.Empty;

    /// <summary>Gets the native classification folded with this generation; never inferred from descriptive Label.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ConversationResolutionScoringLabel? ResolutionScoringLabel { get; init; }

    /// <summary>Gets the exact source-local sequence supplied by the named producer, separate from projection position.</summary>
    public long? SourceHeadSequence { get; init; }

    /// <summary>Gets the actual native persisted head message, absent for legacy materializations.</summary>
    public string? SourceHeadMessageId { get; init; }

    /// <summary>
    /// Gets the maximum validated selected event time contributing to this generation's deterministic replay
    /// state (AD-8). Internal persisted state; it is never the public <c>ProjectionGeneratedAt</c>. Absent for
    /// legacy generations and for a replay with no validated event time.
    /// </summary>
    [System.Text.Json.Serialization.JsonInclude]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    internal DateTimeOffset? ReplayAnchorAt { get; init; }

    /// <summary>
    /// Gets the lowercase-hex SHA-256 of the EventStore-canonical deterministic replay-state bytes (AD-8), which
    /// exclude observation freshness. Internal persisted state; absent for legacy generations.
    /// </summary>
    [System.Text.Json.Serialization.JsonInclude]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    internal string? ReplayStateHash { get; init; }
}
