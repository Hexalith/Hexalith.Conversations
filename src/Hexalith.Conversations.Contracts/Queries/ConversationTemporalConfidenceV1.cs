// <copyright file="ConversationTemporalConfidenceV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Public confidence metadata for point-in-time reconstruction.
/// </summary>
/// <param name="schemaVersion">The confidence contract schema version.</param>
/// <param name="confidenceState">The public confidence state.</param>
/// <param name="reasonCode">The safe confidence reason code.</param>
/// <param name="isComplete">A value indicating whether bounded evidence was complete.</param>
/// <param name="freshnessSummary">A content-safe freshness summary.</param>
public sealed record ConversationTemporalConfidenceV1(
    SchemaVersion SchemaVersion,
    ProjectionTrustState ConfidenceState,
    ProjectionFreshnessReasonCode ReasonCode,
    bool IsComplete,
    string FreshnessSummary)
{
    /// <summary>
    /// Gets the confidence contract schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the public confidence state.
    /// </summary>
    public ProjectionTrustState ConfidenceState { get; } = ConfidenceState ?? throw new ArgumentNullException(nameof(ConfidenceState));

    /// <summary>
    /// Gets the safe confidence reason code.
    /// </summary>
    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    /// <summary>
    /// Gets a value indicating whether bounded evidence was complete.
    /// </summary>
    public bool IsComplete { get; } = IsComplete;

    /// <summary>
    /// Gets a content-safe freshness summary.
    /// </summary>
    public string FreshnessSummary { get; } = ValidateRequired(FreshnessSummary, nameof(FreshnessSummary));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
