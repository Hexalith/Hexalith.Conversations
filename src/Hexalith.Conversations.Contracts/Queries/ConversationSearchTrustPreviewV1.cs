// <copyright file="ConversationSearchTrustPreviewV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries compact source-owned trust metadata for a tenant-scoped search result row.
/// </summary>
public sealed record ConversationSearchTrustPreviewV1(
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode FreshnessReasonCode,
    ProjectionTrustState RedactionState,
    ProjectionTrustState ParticipantResolutionState,
    ConversationCitationAvailability CitationAvailability,
    ConversationAuditReadinessState AuditReadiness,
    ConversationVerificationState VerificationState,
    ConversationSearchMatchSource MatchSource,
    string WhyVisible)
{
    public ProjectionTrustState FreshnessState { get; } = FreshnessState ?? throw new ArgumentNullException(nameof(FreshnessState));

    public ProjectionFreshnessReasonCode FreshnessReasonCode { get; } =
        FreshnessReasonCode ?? throw new ArgumentNullException(nameof(FreshnessReasonCode));

    public ProjectionTrustState RedactionState { get; } = RedactionState ?? throw new ArgumentNullException(nameof(RedactionState));

    public ProjectionTrustState ParticipantResolutionState { get; } =
        ParticipantResolutionState ?? throw new ArgumentNullException(nameof(ParticipantResolutionState));

    public ConversationCitationAvailability CitationAvailability { get; } =
        CitationAvailability ?? throw new ArgumentNullException(nameof(CitationAvailability));

    public ConversationAuditReadinessState AuditReadiness { get; } =
        AuditReadiness ?? throw new ArgumentNullException(nameof(AuditReadiness));

    public ConversationVerificationState VerificationState { get; } =
        VerificationState ?? throw new ArgumentNullException(nameof(VerificationState));

    public ConversationSearchMatchSource MatchSource { get; } = MatchSource ?? throw new ArgumentNullException(nameof(MatchSource));

    public string WhyVisible { get; } = ValidateWhyVisible(WhyVisible);

    public static ConversationSearchTrustPreviewV1 FromFreshness(ProjectionFreshnessV1 freshness)
    {
        ArgumentNullException.ThrowIfNull(freshness);
        return new(
            freshness.FreshnessState,
            freshness.ReasonCode,
            ProjectionTrustState.Unavailable,
            ProjectionTrustState.Unavailable,
            ConversationCitationAvailability.Unavailable,
            ConversationAuditReadinessState.Unknown,
            ConversationVerificationState.Unknown,
            ConversationSearchMatchSource.Unknown,
            "Visible through authorized tenant scope with incomplete trust metadata.");
    }

    public ConversationSearchTrustPreviewV1 WithMatchSource(ConversationSearchMatchSource matchSource, string whyVisible)
        => new(
            FreshnessState,
            FreshnessReasonCode,
            RedactionState,
            ParticipantResolutionState,
            CitationAvailability,
            AuditReadiness,
            VerificationState,
            matchSource,
            whyVisible);

    public ConversationSearchTrustPreviewV1 WithParticipantResolution(ProjectionTrustState participantResolutionState)
        => new(
            FreshnessState,
            FreshnessReasonCode,
            RedactionState,
            participantResolutionState,
            CitationAvailability,
            AuditReadiness,
            VerificationState,
            MatchSource,
            WhyVisible);

    private static string ValidateWhyVisible(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
