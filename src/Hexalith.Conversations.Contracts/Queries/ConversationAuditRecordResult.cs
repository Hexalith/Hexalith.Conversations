// <copyright file="ConversationAuditRecordResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries a content-safe audit-record read or export outcome.
/// </summary>
public sealed record ConversationAuditRecordResult(
    SchemaVersion SchemaVersion,
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode ReasonCode,
    AuditRecordActionClassification ActionClass,
    ConversationAuditRecordDetailsV1? Details,
    string SafeNextAction)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ProjectionTrustState FreshnessState { get; } = FreshnessState ?? throw new ArgumentNullException(nameof(FreshnessState));

    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    public AuditRecordActionClassification ActionClass { get; } =
        ActionClass ?? throw new ArgumentNullException(nameof(ActionClass));

    public string SafeNextAction { get; } = ValidateRequired(SafeNextAction, nameof(SafeNextAction));

    public static ConversationAuditRecordResult Visible(
        SchemaVersion schemaVersion,
        ConversationAuditRecordDetailsV1 details,
        string safeNextAction)
    {
        ArgumentNullException.ThrowIfNull(details);
        return new(
            schemaVersion,
            details.VisibilityState,
            details.ReasonCode,
            details.ActionClass,
            details,
            safeNextAction);
    }

    public static ConversationAuditRecordResult Hidden(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            AuditRecordActionClassification.Denied,
            null,
            "The requested audit record is not available.");

    public static ConversationAuditRecordResult Unavailable(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode,
        string safeNextAction)
        => new(
            schemaVersion,
            ProjectionTrustState.Unavailable,
            reasonCode,
            AuditRecordActionClassification.Denied,
            null,
            safeNextAction);

    public static ConversationAuditRecordResult Rebuilding(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode)
        => new(
            schemaVersion,
            ProjectionTrustState.Rebuilding,
            reasonCode,
            AuditRecordActionClassification.Denied,
            null,
            "Retry after the audit-record view is rebuilt.");

    public static ConversationAuditRecordResult PolicyBlocked(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            AuditRecordActionClassification.PolicyBlocked,
            null,
            "The audit-record action is blocked by policy.");

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
