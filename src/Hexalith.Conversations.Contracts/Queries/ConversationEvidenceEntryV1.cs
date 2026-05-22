// <copyright file="ConversationEvidenceEntryV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Conversations-owned evidence record entry for governed detail reads.
/// </summary>
public sealed record ConversationEvidenceEntryV1(
    string EntryId,
    string Kind,
    PartyId? ActorPartyId,
    DateTimeOffset OccurredAt,
    ProjectionTrustState TrustState,
    ConversationCitationAvailability CitationAvailability,
    ConversationAuditReadinessState AuditReadiness,
    ProjectionTrustState DegradedState,
    MessageId? MessageId = null,
    FileId? FileId = null,
    string? VisibleText = null,
    ConversationProviderCorrelationV1? ProviderCorrelation = null,
    string? PolicyReference = null,
    GovernanceTarget? GovernedTarget = null,
    string? RationaleClass = null,
    GovernanceAuditEvidenceReference? AuditEvidence = null,
    string? SafeSummaryLabel = null,
    string? SafeDetailLabel = null,
    string? SafeAccessibilityLabel = null,
    string? SafeNextAction = null,
    ConversationRedactionAttributionV1? RedactionAttribution = null,
    long? SafeSourcePosition = null)
{
    public string EntryId { get; } = RequireSafeText(EntryId, nameof(EntryId));

    public string Kind { get; } = RequireSafeText(Kind, nameof(Kind));

    public DateTimeOffset OccurredAt { get; } = ValidateTimestamp(OccurredAt);

    public ProjectionTrustState TrustState { get; } = TrustState ?? throw new ArgumentNullException(nameof(TrustState));

    public ConversationCitationAvailability CitationAvailability { get; } =
        CitationAvailability ?? throw new ArgumentNullException(nameof(CitationAvailability));

    public ConversationAuditReadinessState AuditReadiness { get; } =
        AuditReadiness ?? throw new ArgumentNullException(nameof(AuditReadiness));

    public ProjectionTrustState DegradedState { get; } = DegradedState ?? throw new ArgumentNullException(nameof(DegradedState));

    public string? VisibleText { get; } = ValidateVisibleText(VisibleText, TrustState, RedactionAttribution);

    public string? PolicyReference { get; } =
        string.IsNullOrWhiteSpace(PolicyReference)
            ? null
            : GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    public string? RationaleClass { get; } =
        string.IsNullOrWhiteSpace(RationaleClass)
            ? null
            : GovernanceContractValidation.RequiredSafeText(RationaleClass, nameof(RationaleClass));

    public string? SafeSummaryLabel { get; } =
        string.IsNullOrWhiteSpace(SafeSummaryLabel)
            ? null
            : GovernanceContractValidation.RequiredSafeText(SafeSummaryLabel, nameof(SafeSummaryLabel));

    public string? SafeDetailLabel { get; } =
        string.IsNullOrWhiteSpace(SafeDetailLabel)
            ? null
            : GovernanceContractValidation.RequiredSafeText(SafeDetailLabel, nameof(SafeDetailLabel));

    public string? SafeAccessibilityLabel { get; } =
        string.IsNullOrWhiteSpace(SafeAccessibilityLabel)
            ? null
            : GovernanceContractValidation.RequiredSafeText(SafeAccessibilityLabel, nameof(SafeAccessibilityLabel));

    public string? SafeNextAction { get; } =
        string.IsNullOrWhiteSpace(SafeNextAction)
            ? null
            : GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));

    public ConversationRedactionAttributionV1? RedactionAttribution { get; } =
        ValidateRedactionAttribution(RedactionAttribution, AuditReadiness);

    public long? SafeSourcePosition { get; } = ValidateSourcePosition(SafeSourcePosition);

    private static ConversationRedactionAttributionV1? ValidateRedactionAttribution(
        ConversationRedactionAttributionV1? attribution,
        ConversationAuditReadinessState auditReadiness)
    {
        if (attribution is null)
        {
            return null;
        }

        if (auditReadiness != attribution.AuditReadiness)
        {
            throw new ArgumentException(
                "Evidence audit readiness must match redaction attribution audit readiness.",
                nameof(auditReadiness));
        }

        return attribution;
    }

    private static string? ValidateVisibleText(
        string? value,
        ProjectionTrustState trustState,
        ConversationRedactionAttributionV1? attribution)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (attribution is not null && !string.Equals(value, attribution.Placeholder, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Redacted evidence visible text must match the safe redaction placeholder.",
                nameof(value));
        }

        if (trustState == ProjectionTrustState.Redacted
            && !string.Equals(value, GovernanceContractValidation.CanonicalRedactionPlaceholder, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Redacted evidence visible text must use the canonical redaction placeholder.",
                nameof(value));
        }

        return value;
    }

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

    private static long? ValidateSourcePosition(long? value)
    {
        if (value is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 1);
        }

        return value;
    }
}
