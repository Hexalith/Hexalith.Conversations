// <copyright file="ConversationDetailResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries a content-safe retrieve outcome.
/// </summary>
public sealed record ConversationDetailResult(
    SchemaVersion SchemaVersion,
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode ReasonCode,
    ConversationDetailsV1? Details,
    string SafeNextAction)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the public freshness state.
    /// </summary>
    public ProjectionTrustState FreshnessState { get; } = FreshnessState ?? throw new ArgumentNullException(nameof(FreshnessState));

    /// <summary>
    /// Gets the public freshness reason code.
    /// </summary>
    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    /// <summary>
    /// Gets safe next-action metadata.
    /// </summary>
    public string SafeNextAction { get; } = ValidateRequired(SafeNextAction, nameof(SafeNextAction));

    /// <summary>
    /// Creates a visible result.
    /// </summary>
    public static ConversationDetailResult Visible(SchemaVersion schemaVersion, ConversationDetailsV1 details, string safeNextAction)
    {
        ArgumentNullException.ThrowIfNull(details);
        return new(schemaVersion, details.Freshness.FreshnessState, details.Freshness.ReasonCode, details, safeNextAction);
    }

    /// <summary>
    /// Creates a content-safe hidden result.
    /// </summary>
    public static ConversationDetailResult Hidden(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            null,
            "The requested conversation is not available.");

    /// <summary>
    /// Creates a content-safe unavailable result.
    /// </summary>
    public static ConversationDetailResult Unavailable(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Unavailable,
            ProjectionFreshnessReasonCode.Unavailable,
            null,
            "Retry after the read model is available.");

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
