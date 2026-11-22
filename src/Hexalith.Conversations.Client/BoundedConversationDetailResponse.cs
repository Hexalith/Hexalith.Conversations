// <copyright file="BoundedConversationDetailResponse.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Client;

/// <summary>Checks the owner response against the requested bounded detail contract.</summary>
internal static class BoundedConversationDetailResponse
{
    /// <summary>Returns the validated bounded response with local safe guidance, or null when rejected.</summary>
    internal static ConversationDetailResult? Accept(GetConversationQuery query, ConversationDetailResult result, HttpStatusCode status)
    {
        if (result.SchemaVersion != SchemaVersion.Current)
        {
            return null;
        }

        if (result.Details is not { } detail)
        {
            return result.ReadPurpose is null ? AcceptContentFree(result, status) : null;
        }

        string purpose = query.NativeMembershipOnly ? GetConversationQuery.NativeMembershipPurpose : ConversationResolutionScoringLabel.SupportedPurpose;
        if (status != HttpStatusCode.OK || (query.NativeMembershipOnly && query.AdvisoryMetadataOnly) || result.ReadPurpose != purpose
            || detail.TenantId != query.TenantId || detail.ConversationId != query.ConversationId
            || detail.SchemaVersion != result.SchemaVersion || detail.Freshness.ProjectionContractSchemaVersion != detail.SchemaVersion
            || detail.Freshness.FreshnessState != result.FreshnessState || detail.Freshness.ReasonCode != result.ReasonCode
            || detail.LifecycleState is not ("Initializing" or "Open" or "Closed" or "Archived")
            || !AcceptFreshness(query.NativeMembershipOnly, detail.Freshness) || !AcceptLabel(query.NativeMembershipOnly, detail.Label)
            || !ContainsOnlyBoundedFields(detail) || !HasDefaultTrustPosture(detail))
        {
            return null;
        }

        return ConversationDetailResult.Visible(result.SchemaVersion, detail, query.NativeMembershipOnly
            ? "Native membership evidence is available." : "Classified advisory evidence is available.") with { ReadPurpose = purpose };
    }

    private static ConversationDetailResult? AcceptContentFree(ConversationDetailResult result, HttpStatusCode status)
    {
        if (status == HttpStatusCode.ServiceUnavailable && result.FreshnessState == ProjectionTrustState.Unavailable
            && result.ReasonCode == ProjectionFreshnessReasonCode.Unavailable)
        {
            return ConversationDetailResult.Unavailable(result.SchemaVersion);
        }

        if (status != HttpStatusCode.NotFound)
        {
            return null;
        }

        if (result.FreshnessState == ProjectionTrustState.Forbidden && result.ReasonCode == ProjectionFreshnessReasonCode.Forbidden)
        {
            return ConversationDetailResult.Hidden(result.SchemaVersion);
        }

        return result.FreshnessState == ProjectionTrustState.Rebuilding
            && (result.ReasonCode == ProjectionFreshnessReasonCode.Rebuilding || result.ReasonCode == ProjectionFreshnessReasonCode.MixedGeneration
                || result.ReasonCode == ProjectionFreshnessReasonCode.GapDetected || result.ReasonCode == ProjectionFreshnessReasonCode.OutOfOrderEvent)
            ? new ConversationDetailResult(result.SchemaVersion, result.FreshnessState, result.ReasonCode, null,
                "Retry after the read model finishes rebuilding.")
            : null;
    }

    private static bool AcceptFreshness(bool nativeMembership, ProjectionFreshnessV1 freshness)
        => nativeMembership
            ? freshness.AllowsTrustBearingDecision()
            : freshness.FreshnessState == ProjectionTrustState.Current || freshness.FreshnessState == ProjectionTrustState.Stale;

    private static bool AcceptLabel(bool nativeMembership, string? label)
    {
        if (label is null) return true;
        if (nativeMembership) return false;

        try
        {
            _ = new ConversationResolutionScoringLabel(label, ConversationResolutionScoringLabel.SupportedPurpose,
                ConversationResolutionScoringLabel.OwnerClassification);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool ContainsOnlyBoundedFields(ConversationDetailsV1 detail)
        => detail is
        {
            BusinessReference: null, FolderId: null, ProviderCorrelation: null, GovernanceState: null,
            ProjectHydration: null, FolderHydration: null, Participants.Count: 0, Messages.Count: 0,
            FileReferences.Count: 0, Attributes.Count: 0, PartyHydration.Count: 0, FileHydration.Count: 0,
            SensitivityMarks.Count: 0, Redactions.Count: 0, EvidenceEntries.Count: 0,
        };

    private static bool HasDefaultTrustPosture(ConversationDetailsV1 detail)
    {
        var expected = ConversationEvidenceTrustPostureV1.FromFreshness(detail.SchemaVersion, detail.TenantId, detail.ConversationId, detail.Freshness);
        var actual = detail.TrustPosture;
        return actual.SchemaVersion == expected.SchemaVersion && actual.TenantId == expected.TenantId
            && actual.ConversationId == expected.ConversationId && actual.TemporalCursor == expected.TemporalCursor
            && actual.Freshness == expected.Freshness && actual.EvidenceCompletenessState == expected.EvidenceCompletenessState
            && actual.ParticipantResolutionState == expected.ParticipantResolutionState && actual.CitationAvailability == expected.CitationAvailability
            && actual.AuditReadiness == expected.AuditReadiness && actual.VerificationState == expected.VerificationState
            && actual.CommandEligibility.SequenceEqual(expected.CommandEligibility);
    }
}
