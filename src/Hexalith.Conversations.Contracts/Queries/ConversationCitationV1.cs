// <copyright file="ConversationCitationV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Permission-safe citation DTO built by Conversations-owned server code after authorization recheck.
/// </summary>
public sealed record ConversationCitationV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    string EvidenceEntryId,
    string EvidenceKind,
    DateTimeOffset OccurredAt,
    ProjectionTrustState TrustState,
    ConversationCitationAvailability CitationAvailability,
    ConversationAuditReadinessState AuditReadiness,
    PartyId? ActorPartyId,
    GovernanceAuditEvidenceReference? AuditEvidence,
    string ProjectionCursor,
    long ProjectionVersion,
    string TemporalCursor,
    string SafeCopiedText,
    string SafeLabel,
    string SafeAccessibilityLabel,
    string SafeNextAction)
{
    private const int MaxSafeCitationTextLength = 2048;

    private static readonly string[] UnsafeCitationTerms =
    [
        "eventstore",
        "provider payload",
        "provider correlation",
        "provider session",
        "snapshot",
        "storage offset",
        "storage location",
        "raw exception",
        "raw message",
        "original message",
        "displayname",
        "display name",
        "selectedtext",
        "selected text",
        "browser-selected",
        "browsertitle",
        "browser title",
        "clipboardselection",
        "rendered-text-only",
        "rendered text",
        "hidden field",
        "localstorage",
        "sessionstorage",
        "personal data",
        "secret",
    ];

    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public string CitationId { get; } = BuildCitationId(ConversationId, EvidenceEntryId);

    public string EvidenceEntryId { get; } = ConversationCitationTargetV1.ValidateSafeToken(
        EvidenceEntryId,
        nameof(EvidenceEntryId));

    public string EvidenceKind { get; } = ValidateSafeText(EvidenceKind, nameof(EvidenceKind));

    public DateTimeOffset OccurredAt { get; } = ValidateTimestamp(OccurredAt);

    public ProjectionTrustState TrustState { get; } = TrustState ?? throw new ArgumentNullException(nameof(TrustState));

    public ConversationCitationAvailability CitationAvailability { get; } =
        CitationAvailability ?? throw new ArgumentNullException(nameof(CitationAvailability));

    public ConversationAuditReadinessState AuditReadiness { get; } =
        AuditReadiness ?? throw new ArgumentNullException(nameof(AuditReadiness));

    public GovernanceAuditEvidenceReference? AuditEvidence { get; } =
        AuditReadiness == ConversationAuditReadinessState.Ready ? AuditEvidence : null;

    public string ProjectionCursor { get; } = ValidateSafeText(ProjectionCursor, nameof(ProjectionCursor));

    public long ProjectionVersion { get; } = ValidatePositive(ProjectionVersion, nameof(ProjectionVersion));

    public string TemporalCursor { get; } = ValidateSafeText(TemporalCursor, nameof(TemporalCursor));

    public string SafeCopiedText { get; } = ValidateSafeText(SafeCopiedText, nameof(SafeCopiedText));

    public string SafeLabel { get; } = ValidateSafeText(SafeLabel, nameof(SafeLabel));

    public string SafeAccessibilityLabel { get; } = ValidateSafeText(
        SafeAccessibilityLabel,
        nameof(SafeAccessibilityLabel));

    public string SafeNextAction { get; } = ValidateSafeText(SafeNextAction, nameof(SafeNextAction));

    private static string BuildCitationId(ConversationId conversationId, string evidenceEntryId)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        string safeEvidenceEntryId = ConversationCitationTargetV1.ValidateSafeToken(
            evidenceEntryId,
            nameof(evidenceEntryId));
        return $"citation:v1:{conversationId.Value}:{safeEvidenceEntryId}";
    }

    private static string ValidateSafeText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > MaxSafeCitationTextLength)
        {
            throw new ArgumentException("Citation text exceeds the safe bounded length.", parameterName);
        }

        if (value.Any(char.IsControl))
        {
            throw new ArgumentException("Citation text cannot contain control characters.", parameterName);
        }

        foreach (string term in UnsafeCitationTerms)
        {
            if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Citation text contains reserved disclosure vocabulary.", parameterName);
            }
        }

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

    private static long ValidatePositive(long value, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, parameterName);
        return value;
    }
}
