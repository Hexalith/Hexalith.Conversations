// <copyright file="ConversationTemporalDetailResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries a content-safe point-in-time reconstruction outcome.
/// </summary>
public sealed record ConversationTemporalDetailResult(
    SchemaVersion SchemaVersion,
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode ReasonCode,
    ConversationTemporalAnchorV1? AuthoritativeTemporalAnchor,
    ConversationTemporalDetailsV1? Details,
    ConversationTemporalConfidenceV1 Confidence,
    string SafeNextAction)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ProjectionTrustState FreshnessState { get; } = FreshnessState ?? throw new ArgumentNullException(nameof(FreshnessState));

    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    public ConversationTemporalConfidenceV1 Confidence { get; } =
        Confidence ?? throw new ArgumentNullException(nameof(Confidence));

    public string SafeNextAction { get; } = ValidateRequired(SafeNextAction, nameof(SafeNextAction));

    public static ConversationTemporalDetailResult Visible(
        SchemaVersion schemaVersion,
        ConversationTemporalDetailsV1 details,
        string safeNextAction)
    {
        ArgumentNullException.ThrowIfNull(details);
        return new(
            schemaVersion,
            details.Confidence.ConfidenceState,
            details.Confidence.ReasonCode,
            details.TemporalAnchor,
            details,
            details.Confidence,
            safeNextAction);
    }

    public static ConversationTemporalDetailResult Hidden(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            null,
            null,
            new ConversationTemporalConfidenceV1(
                schemaVersion,
                ProjectionTrustState.Forbidden,
                ProjectionFreshnessReasonCode.Forbidden,
                false,
                "Temporal evidence is not available."),
            "The requested historical view is not available.");

    public static ConversationTemporalDetailResult Unavailable(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode,
        string safeNextAction)
        => new(
            schemaVersion,
            ProjectionTrustState.Unavailable,
            reasonCode,
            null,
            null,
            new ConversationTemporalConfidenceV1(
                schemaVersion,
                ProjectionTrustState.Unavailable,
                reasonCode,
                false,
                "Temporal evidence cannot be completed now."),
            safeNextAction);

    public static ConversationTemporalDetailResult Rebuilding(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode)
        => new(
            schemaVersion,
            ProjectionTrustState.Rebuilding,
            reasonCode,
            null,
            null,
            new ConversationTemporalConfidenceV1(
                schemaVersion,
                ProjectionTrustState.Rebuilding,
                reasonCode,
                false,
                "Temporal evidence is being rebuilt."),
            "Retry after the temporal evidence is complete.");

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
