// <copyright file="ConversationEvidenceEntryV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

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
    string? PolicyReference = null)
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

    public string? VisibleText { get; } = string.IsNullOrWhiteSpace(VisibleText) ? null : VisibleText;

    public string? PolicyReference { get; } = string.IsNullOrWhiteSpace(PolicyReference) ? null : PolicyReference;

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
