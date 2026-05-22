// <copyright file="ConversationRedactionAttributionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries safe inline redaction attribution for governed evidence entries.
/// </summary>
public sealed record ConversationRedactionAttributionV1(
    RedactionCategory Category,
    string PolicyReference,
    string ReasonClass,
    PartyId? ActorPartyId,
    DateTimeOffset RedactedAt,
    GovernanceTarget Target,
    string TargetKey,
    GovernanceAuditEvidenceReference? AuditEvidence,
    ConversationAuditReadinessState AuditReadiness,
    ProjectionTrustState AttributionState,
    string Placeholder,
    string SafeSummaryLabel,
    string SafeAccessibilityLabel,
    string SafeNextAction)
{
    public RedactionCategory Category { get; } = Category ?? throw new ArgumentNullException(nameof(Category));

    public string PolicyReference { get; } =
        GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    public string ReasonClass { get; } =
        GovernanceContractValidation.RequiredSafeText(ReasonClass, nameof(ReasonClass));

    public DateTimeOffset RedactedAt { get; } =
        GovernanceContractValidation.RequiredUtcTimestamp(RedactedAt, nameof(RedactedAt));

    public GovernanceTarget Target { get; } =
        GovernanceContractValidation.RequireNonNull(Target, nameof(Target));

    public string TargetKey { get; } =
        ValidateTargetKey(TargetKey, Target);

    public ConversationAuditReadinessState AuditReadiness { get; } =
        AuditReadiness ?? throw new ArgumentNullException(nameof(AuditReadiness));

    public ProjectionTrustState AttributionState { get; } =
        AttributionState ?? throw new ArgumentNullException(nameof(AttributionState));

    public string Placeholder { get; } =
        GovernanceContractValidation.RequiredSafeRedactionPlaceholder(Placeholder, nameof(Placeholder));

    public string SafeSummaryLabel { get; } =
        GovernanceContractValidation.RequiredSafeText(SafeSummaryLabel, nameof(SafeSummaryLabel));

    public string SafeAccessibilityLabel { get; } =
        GovernanceContractValidation.RequiredSafeText(SafeAccessibilityLabel, nameof(SafeAccessibilityLabel));

    public string SafeNextAction { get; } =
        GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));

    private static string ValidateTargetKey(string targetKey, GovernanceTarget target)
    {
        GovernanceTarget safeTarget = GovernanceContractValidation.RequireNonNull(target, nameof(target));
        string safeTargetKey = GovernanceContractValidation.RequiredSafeToken(targetKey, nameof(targetKey));
        return string.Equals(safeTargetKey, safeTarget.ToTargetKey(), StringComparison.Ordinal)
            ? safeTargetKey
            : throw new ArgumentException("Target key must match the governed target.", nameof(targetKey));
    }
}
