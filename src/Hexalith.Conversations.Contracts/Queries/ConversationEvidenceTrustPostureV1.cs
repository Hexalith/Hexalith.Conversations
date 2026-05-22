// <copyright file="ConversationEvidenceTrustPostureV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Source-owned trust posture for an opened governed conversation record.
/// </summary>
public sealed record ConversationEvidenceTrustPostureV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    string TemporalCursor,
    ProjectionFreshnessV1 Freshness,
    ProjectionTrustState EvidenceCompletenessState,
    ProjectionTrustState ParticipantResolutionState,
    ConversationCitationAvailability CitationAvailability,
    ConversationAuditReadinessState AuditReadiness,
    ConversationVerificationState VerificationState,
    IReadOnlyList<ConversationCommandAvailabilityV1>? CommandEligibility = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public string TemporalCursor { get; } = RequireSafeText(TemporalCursor, nameof(TemporalCursor));

    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));

    public ProjectionTrustState EvidenceCompletenessState { get; } =
        EvidenceCompletenessState ?? throw new ArgumentNullException(nameof(EvidenceCompletenessState));

    public ProjectionTrustState ParticipantResolutionState { get; } =
        ParticipantResolutionState ?? throw new ArgumentNullException(nameof(ParticipantResolutionState));

    public ConversationCitationAvailability CitationAvailability { get; } =
        CitationAvailability ?? throw new ArgumentNullException(nameof(CitationAvailability));

    public ConversationAuditReadinessState AuditReadiness { get; } =
        AuditReadiness ?? throw new ArgumentNullException(nameof(AuditReadiness));

    public ConversationVerificationState VerificationState { get; } =
        VerificationState ?? throw new ArgumentNullException(nameof(VerificationState));

    public IReadOnlyList<ConversationCommandAvailabilityV1> CommandEligibility { get; } =
        ValidateCommandEligibility(CommandEligibility, Freshness);

    public static ConversationEvidenceTrustPostureV1 FromFreshness(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationId conversationId,
        ProjectionFreshnessV1 freshness)
    {
        ArgumentNullException.ThrowIfNull(schemaVersion);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(freshness);

        return new(
            schemaVersion,
            tenantId,
            conversationId,
            freshness.ProjectionCursor,
            freshness,
            ProjectionTrustState.Unavailable,
            ProjectionTrustState.Unavailable,
            ConversationCitationAvailability.Unavailable,
            ConversationAuditReadinessState.Unknown,
            ConversationVerificationState.Unknown);
    }

    public ConversationEvidenceTrustPostureV1 WithParticipantResolution(ProjectionTrustState participantResolutionState)
        => new(
            SchemaVersion,
            TenantId,
            ConversationId,
            TemporalCursor,
            Freshness,
            EvidenceCompletenessState,
            participantResolutionState,
            CitationAvailability,
            AuditReadiness,
            VerificationState,
            CommandEligibility);

    private static IReadOnlyList<ConversationCommandAvailabilityV1> ValidateCommandEligibility(
        IReadOnlyList<ConversationCommandAvailabilityV1>? values,
        ProjectionFreshnessV1 freshness)
    {
        if (values is null || values.Count == 0)
        {
            DateTimeOffset evaluatedAt = freshness?.ProjectionGeneratedAt
                ?? throw new ArgumentNullException(nameof(freshness));
            return
            [
                new ConversationCommandAvailabilityV1(
                    "read-governed-record",
                    ProjectionTrustState.Unavailable,
                    "conversations.read",
                    ProjectionTrustState.Unavailable,
                    "read",
                    ProjectionTrustState.Unavailable,
                    ConversationAuditReadinessState.Unknown,
                    "Command availability metadata is unavailable for this record.",
                    evaluatedAt),
            ];
        }

        return values.Any(value => value is null)
            ? throw new ArgumentException("Command eligibility must not contain null elements.", nameof(values))
            : values;
    }

    private static string RequireSafeText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
