// <copyright file="ConversationCitationAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Projections;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Resolves permission-safe citation DTOs from the governed read projection.
/// </summary>
public sealed class ConversationCitationAccessService(ConversationProjectionReadService projectionReadService)
{
    private readonly ConversationProjectionReadService _projectionReadService =
        projectionReadService ?? throw new ArgumentNullException(nameof(projectionReadService));

    /// <summary>
    /// Resolves one citation-copy request after tenant/caller authorization and freshness recheck.
    /// </summary>
    public async ValueTask<ConversationCitationResult> GetAsync(
        GetConversationCitationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ConversationProjectionReadResult result = await _projectionReadService
            .ReadDetailAsync(
                query.TenantId,
                query.CallerPrincipalId,
                query.TenantId,
                query.ConversationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Projection is null)
        {
            return ProjectionUnavailableResult(query, result);
        }

        ConversationEvidenceEntryV1? entry = result.Projection.EvidenceEntries
            .FirstOrDefault(e => string.Equals(e.EntryId, query.EvidenceEntryId, StringComparison.Ordinal));
        if (entry is null)
        {
            return ConversationCitationResult.Hidden(query.SchemaVersion);
        }

        if (entry.SafeSourcePosition is long safeSourcePosition
            && safeSourcePosition > result.Projection.Freshness.LastAppliedEventPosition)
        {
            return ConversationCitationResult.Rebuilding(query.SchemaVersion, ProjectionFreshnessReasonCode.GapDetected);
        }

        ConversationCitationV1 citation = BuildCitation(query, result.Projection, entry);
        return ConversationCitationResult.Visible(
            query.SchemaVersion,
            citation,
            citation.CitationAvailability == ConversationCitationAvailability.Available
                ? "Use the copied citation text only for governed evidence references."
                : citation.SafeNextAction);
    }

    private static ConversationCitationResult ProjectionUnavailableResult(
        GetConversationCitationQuery query,
        ConversationProjectionReadResult result)
    {
        if (result.FreshnessState == ProjectionTrustState.Forbidden)
        {
            return ConversationCitationResult.Hidden(query.SchemaVersion);
        }

        if (result.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return ConversationCitationResult.Rebuilding(query.SchemaVersion, result.ReasonCode);
        }

        return ConversationCitationResult.Unavailable(query.SchemaVersion, result.ReasonCode);
    }

    private static ConversationCitationV1 BuildCitation(
        GetConversationCitationQuery query,
        ConversationDetailProjectionV1 projection,
        ConversationEvidenceEntryV1 entry)
    {
        ProjectionFreshnessV1 freshness = projection.Freshness;
        long safeSourcePosition = entry.SafeSourcePosition ?? freshness.LastAppliedEventPosition;
        string temporalCursor = BuildTemporalCursor(safeSourcePosition, freshness.LastAppliedEventPosition);
        ConversationAuditReadinessState auditReadiness = entry.AuditReadiness;
        ConversationCitationAvailability availability = entry.CitationAvailability;
        GovernanceAuditEvidenceReference? auditEvidence = entry.AuditEvidence;

        if (auditReadiness == ConversationAuditReadinessState.Ready && auditEvidence is null)
        {
            auditReadiness = ConversationAuditReadinessState.Incomplete;
            availability = ConversationCitationAvailability.Incomplete;
        }

        if (auditReadiness != ConversationAuditReadinessState.Ready)
        {
            if (availability == ConversationCitationAvailability.Available)
            {
                availability = ConversationCitationAvailability.Incomplete;
            }

            auditEvidence = null;
        }

        string safeCopiedText = availability == ConversationCitationAvailability.Available
            ? BuildSafeCopiedText(query, projection, entry, auditReadiness, auditEvidence, temporalCursor)
            : availability == ConversationCitationAvailability.Incomplete
                ? "Citation is incomplete."
                : "Citation is unavailable.";

        return new ConversationCitationV1(
            query.SchemaVersion,
            query.TenantId,
            query.ConversationId,
            entry.EntryId,
            entry.Kind,
            entry.OccurredAt,
            entry.TrustState,
            availability,
            auditReadiness,
            entry.ActorPartyId,
            auditEvidence,
            freshness.ProjectionCursor,
            freshness.LastAppliedEventPosition,
            temporalCursor,
            safeCopiedText,
            entry.SafeSummaryLabel ?? $"{entry.Kind} evidence citation",
            entry.SafeAccessibilityLabel ?? $"Copy {entry.Kind} evidence citation",
            entry.SafeNextAction ?? "Use the stable temporal evidence link when available.");
    }

    private static string BuildSafeCopiedText(
        GetConversationCitationQuery query,
        ConversationDetailProjectionV1 projection,
        ConversationEvidenceEntryV1 entry,
        ConversationAuditReadinessState auditReadiness,
        GovernanceAuditEvidenceReference? auditEvidence,
        string temporalCursor)
    {
        List<string> fields =
        [
            $"citation={query.ConversationId.Value}/{entry.EntryId}",
            $"tenant={query.TenantId.Value}",
            $"conversation={query.ConversationId.Value}",
            $"evidence={entry.EntryId}",
            $"kind={entry.Kind}",
            $"occurredAt={entry.OccurredAt.ToString("O", CultureInfo.InvariantCulture)}",
            $"trust={entry.TrustState.Value}",
            $"citationAvailability={entry.CitationAvailability.Value}",
            $"auditReadiness={auditReadiness.Value}",
            $"safeSourcePosition={entry.SafeSourcePosition?.ToString(CultureInfo.InvariantCulture) ?? projection.Freshness.LastAppliedEventPosition.ToString(CultureInfo.InvariantCulture)}",
            $"temporalCursor={temporalCursor}",
            $"projectionCursor={projection.Freshness.ProjectionCursor}",
            $"projectionVersion={projection.Freshness.LastAppliedEventPosition.ToString(CultureInfo.InvariantCulture)}",
            $"contractVersion={query.SchemaVersion.Value.ToString(CultureInfo.InvariantCulture)}",
            $"freshness={projection.Freshness.FreshnessState.Value}",
            $"completeness={projection.TrustPosture.EvidenceCompletenessState.Value}",
        ];

        if (entry.ActorPartyId is not null)
        {
            fields.Add($"actorPartyId={entry.ActorPartyId.Value}");
        }

        if (auditEvidence is not null)
        {
            fields.Add($"auditHandle={auditEvidence.Handle.Value}");
            fields.Add($"auditPolicy={auditEvidence.PolicyReference}");
            fields.Add($"auditTimestamp={auditEvidence.CapturedAt.ToString("O", CultureInfo.InvariantCulture)}");
        }

        if (entry.RedactionAttribution is not null)
        {
            fields.Add($"redaction={entry.RedactionAttribution.Placeholder}");
            fields.Add($"redactionPolicy={entry.RedactionAttribution.PolicyReference}");
            fields.Add($"redactionReason={entry.RedactionAttribution.ReasonClass}");
        }

        if (!string.IsNullOrWhiteSpace(entry.SafeSummaryLabel))
        {
            fields.Add($"label={entry.SafeSummaryLabel}");
        }

        return string.Join("; ", fields);
    }

    private static string BuildTemporalCursor(long safeSourcePosition, long projectionVersion)
        => $"temporal:v1:pos:{safeSourcePosition.ToString("D10", CultureInfo.InvariantCulture)}:projection:{projectionVersion.ToString("D10", CultureInfo.InvariantCulture)}";
}
