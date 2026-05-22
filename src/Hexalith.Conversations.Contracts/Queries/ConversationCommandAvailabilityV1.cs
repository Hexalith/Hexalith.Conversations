// <copyright file="ConversationCommandAvailabilityV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Server-owned command availability metadata for a governed read surface.
/// </summary>
public sealed record ConversationCommandAvailabilityV1(
    string ActionName,
    ProjectionTrustState AvailabilityState,
    string RequiredPermission,
    ProjectionTrustState PreconditionState,
    string RiskLevel,
    ProjectionTrustState FreshnessRequirementState,
    ConversationAuditReadinessState AuditRequirement,
    string BlockedReason,
    DateTimeOffset LastEvaluatedAt)
{
    public string ActionName { get; } = RequireSafeText(ActionName, nameof(ActionName));

    public ProjectionTrustState AvailabilityState { get; } =
        AvailabilityState ?? throw new ArgumentNullException(nameof(AvailabilityState));

    public string RequiredPermission { get; } = RequireSafeText(RequiredPermission, nameof(RequiredPermission));

    public ProjectionTrustState PreconditionState { get; } =
        PreconditionState ?? throw new ArgumentNullException(nameof(PreconditionState));

    public string RiskLevel { get; } = RequireSafeText(RiskLevel, nameof(RiskLevel));

    public ProjectionTrustState FreshnessRequirementState { get; } =
        FreshnessRequirementState ?? throw new ArgumentNullException(nameof(FreshnessRequirementState));

    public ConversationAuditReadinessState AuditRequirement { get; } =
        AuditRequirement ?? throw new ArgumentNullException(nameof(AuditRequirement));

    public string BlockedReason { get; } = RequireSafeText(BlockedReason, nameof(BlockedReason));

    public DateTimeOffset LastEvaluatedAt { get; } = ValidateTimestamp(LastEvaluatedAt);

    private static string RequireSafeText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset value)
    {
        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.");
        }

        return value;
    }
}
