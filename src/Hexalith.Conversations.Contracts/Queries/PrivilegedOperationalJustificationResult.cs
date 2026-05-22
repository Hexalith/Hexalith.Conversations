// <copyright file="PrivilegedOperationalJustificationResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries a content-safe privileged-action justification enforcement or review outcome.
/// </summary>
public sealed record PrivilegedOperationalJustificationResult(
    SchemaVersion SchemaVersion,
    ProjectionTrustState VisibilityState,
    ProjectionFreshnessReasonCode ReasonCode,
    GovernanceOutcome Outcome,
    PrivilegedOperationalJustificationDetailsV1? Details,
    string SafeNextAction)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public ProjectionTrustState VisibilityState { get; } = VisibilityState ?? throw new ArgumentNullException(nameof(VisibilityState));

    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    public GovernanceOutcome Outcome { get; } = Outcome ?? throw new ArgumentNullException(nameof(Outcome));

    public string SafeNextAction { get; } = GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));

    public static PrivilegedOperationalJustificationResult Visible(
        SchemaVersion schemaVersion,
        PrivilegedOperationalJustificationDetailsV1 details,
        string safeNextAction)
    {
        ArgumentNullException.ThrowIfNull(details);
        return new(
            schemaVersion,
            details.VisibilityState,
            details.Freshness.ReasonCode,
            details.Outcome,
            details,
            safeNextAction);
    }

    public static PrivilegedOperationalJustificationResult Hidden(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            GovernanceOutcome.Denied,
            null,
            "The requested privileged-action evidence is not available.");

    public static PrivilegedOperationalJustificationResult Denied(SchemaVersion schemaVersion, string safeNextAction)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            GovernanceOutcome.Denied,
            null,
            safeNextAction);

    public static PrivilegedOperationalJustificationResult Unavailable(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode,
        string safeNextAction)
        => new(
            schemaVersion,
            ProjectionTrustState.Unavailable,
            reasonCode,
            GovernanceOutcome.AuditUnavailableFailed,
            null,
            safeNextAction);

    public static PrivilegedOperationalJustificationResult Rebuilding(
        SchemaVersion schemaVersion,
        ProjectionFreshnessReasonCode reasonCode)
        => new(
            schemaVersion,
            ProjectionTrustState.Rebuilding,
            reasonCode,
            GovernanceOutcome.Denied,
            null,
            "Retry after privileged-action evidence is rebuilt.");

    public static PrivilegedOperationalJustificationResult PolicyBlocked(
        SchemaVersion schemaVersion,
        string safeNextAction)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            GovernanceOutcome.PolicyBlocked,
            null,
            safeNextAction);
}
