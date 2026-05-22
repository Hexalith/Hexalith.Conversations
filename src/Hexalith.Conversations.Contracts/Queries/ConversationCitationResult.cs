// <copyright file="ConversationCitationResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries a content-safe citation-copy outcome.
/// </summary>
public sealed record ConversationCitationResult(
    SchemaVersion SchemaVersion,
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode ReasonCode,
    ConversationCitationV1? Citation,
    string SafeNextAction)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ProjectionTrustState FreshnessState { get; } = FreshnessState ?? throw new ArgumentNullException(nameof(FreshnessState));

    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    public string SafeNextAction { get; } = ValidateRequired(SafeNextAction, nameof(SafeNextAction));

    public static ConversationCitationResult Visible(
        SchemaVersion schemaVersion,
        ConversationCitationV1 citation,
        string safeNextAction)
    {
        ArgumentNullException.ThrowIfNull(citation);
        return new(schemaVersion, citation.TrustState, ProjectionFreshnessReasonCode.Current, citation, safeNextAction);
    }

    public static ConversationCitationResult Hidden(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            null,
            "The requested citation is not available.");

    public static ConversationCitationResult Unavailable(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode)
        => new(
            schemaVersion,
            ProjectionTrustState.Unavailable,
            reasonCode,
            null,
            "Retry after citation evidence is available.");

    public static ConversationCitationResult Rebuilding(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode)
        => new(
            schemaVersion,
            ProjectionTrustState.Rebuilding,
            reasonCode,
            null,
            "Retry after citation evidence is rebuilt.");

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
