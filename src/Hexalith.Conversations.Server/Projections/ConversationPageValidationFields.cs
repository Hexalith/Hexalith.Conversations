// <copyright file="ConversationPageValidationFields.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Mirrors persisted conversation fields with a streaming detail header for stock Web options.</summary>
/// <param name="Summary">The persisted conversation summary.</param>
/// <param name="Detail">The persisted detail header.</param>
internal sealed record ConversationPageValidationFields(
    ConversationSummaryProjectionV1 Summary,
    ConversationDetailValidationHeader Detail)
{
    /// <summary>Gets the original frozen admission decision; omitted for legacy generations.</summary>
    public ConversationProjectionNativeAdmission? NativeAdmission { get; init; }

    /// <summary>Gets the original SDK-verified native incarnation and complete prefix; absent for legacy input.</summary>
    public NativeSourceProof? NativeSourceProof { get; init; }

    /// <summary>Gets the persisted conversation summary.</summary>
    public ConversationSummaryProjectionV1 Summary { get; } = Summary ?? throw new ArgumentNullException(nameof(Summary));

    /// <summary>Gets the persisted detail header.</summary>
    public ConversationDetailValidationHeader Detail { get; } = Detail ?? throw new ArgumentNullException(nameof(Detail));

    /// <summary>Gets the stable dispatch identity that produced this generation.</summary>
    public string DispatchId { get; init; } = string.Empty;

    /// <summary>Gets the native classification folded with this generation.</summary>
    public ConversationResolutionScoringLabel? ResolutionScoringLabel { get; init; }

    /// <summary>Gets the exact source-local sequence supplied by the named producer.</summary>
    public long? SourceHeadSequence { get; init; }

    /// <summary>Gets the actual native persisted head message.</summary>
    public string? SourceHeadMessageId { get; init; }

    /// <summary>Gets the maximum validated selected event time contributing to deterministic replay state.</summary>
    [JsonInclude]
    internal DateTimeOffset? ReplayAnchorAt { get; init; }

    /// <summary>Gets the lowercase-hex SHA-256 of the deterministic replay-state bytes.</summary>
    [JsonInclude]
    internal string? ReplayStateHash { get; init; }
}
