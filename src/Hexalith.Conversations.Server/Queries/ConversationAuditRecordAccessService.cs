// <copyright file="ConversationAuditRecordAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Evaluates audit-record access before resolving audit evidence details.
/// </summary>
public sealed class ConversationAuditRecordAccessService(
    IConversationTenantAccessService tenantAccessService,
    IConversationProjectionReadStore projectionReadStore)
{
    private const string SeparateLogRequiredPolicy = "audit-policy-separate-log-required";
    private const string ExpiredAuditPolicy = "audit-policy-expired";

    private readonly IConversationTenantAccessService _tenantAccessService =
        tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));

    private readonly IConversationProjectionReadStore _projectionReadStore =
        projectionReadStore ?? throw new ArgumentNullException(nameof(projectionReadStore));

    /// <summary>
    /// Reads or exports an audit-record view through the tenant and policy boundary.
    /// </summary>
    public async ValueTask<ConversationAuditRecordResult> GetAsync(
        GetConversationAuditRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                ConversationTenantAccessRequirement.Read,
                query.TenantId,
                query.CallerPrincipalId,
                routeTenantId: query.TenantId,
                projectionTenantId: query.TenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return ConversationAuditRecordResult.Hidden(query.SchemaVersion);
        }

        AuditEvidenceHandle handle;
        try
        {
            handle = new AuditEvidenceHandle(query.AuditEvidenceHandle);
        }
        catch (ArgumentException)
        {
            return ConversationAuditRecordResult.Hidden(query.SchemaVersion);
        }

        ConversationProjectedReadModels? models;
        try
        {
            models = await _projectionReadStore
                .ReadAsync(query.TenantId, query.ConversationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return ConversationAuditRecordResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after the audit-record view is available.");
        }
        catch (IOException)
        {
            return ConversationAuditRecordResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after the audit-record view is available.");
        }
        catch (TimeoutException)
        {
            return ConversationAuditRecordResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after the audit-record view is available.");
        }

        if (models is null || !ProjectionMatchesRequest(models, query.TenantId, query.ConversationId))
        {
            return ConversationAuditRecordResult.Hidden(query.SchemaVersion);
        }

        ProjectionFreshnessV1 freshness = models.Detail.Freshness;
        if (!SameGeneration(models.Summary.Freshness, freshness))
        {
            return ConversationAuditRecordResult.Rebuilding(query.SchemaVersion, ProjectionFreshnessReasonCode.MixedGeneration);
        }

        if (!freshness.AllowsTrustBearingDecision())
        {
            return NonCurrent(query, freshness);
        }

        AuditRecordProjectionEntry? entry = FindAuditRecord(models.Detail, handle);
        if (entry is null)
        {
            return ConversationAuditRecordResult.Hidden(query.SchemaVersion);
        }

        if (query.RequestedAction == AuditRecordActionClassification.PolicyBlocked
            || query.RequestedAction == AuditRecordActionClassification.Denied
            || query.RequestedAction == AuditRecordActionClassification.Redacted
            || query.RequestedAction == AuditRecordActionClassification.SeparatelyLogged
            || (query.RequestedAction == AuditRecordActionClassification.Exported
                && string.Equals(entry.PolicyReference, SeparateLogRequiredPolicy, StringComparison.Ordinal)))
        {
            return ConversationAuditRecordResult.PolicyBlocked(query.SchemaVersion);
        }

        bool expired = string.Equals(entry.PolicyReference, ExpiredAuditPolicy, StringComparison.Ordinal);
        AuditRecordActionClassification actionClass = ActionClassFor(query.RequestedAction, expired);
        ProjectionTrustState visibility = expired ? ProjectionTrustState.Redacted : ProjectionTrustState.Current;
        ProjectionFreshnessReasonCode reason = expired
            ? ProjectionFreshnessReasonCode.Redacted
            : ProjectionFreshnessReasonCode.Current;
        string nextAction = expired
            ? "Audit evidence metadata is retained; protected audit detail is withheld."
            : query.RequestedAction == AuditRecordActionClassification.Exported
                ? "Use this in-memory audit export response as governed evidence."
                : "Use the returned audit handle as governed evidence.";

        AuditRecordPolicyTreatmentV1 treatment = new(
            query.SchemaVersion,
            query.TenantId,
            query.ConversationId,
            handle,
            expired ? ProjectionTrustState.Redacted : ProjectionTrustState.Current,
            entry.Redacted ? ProjectionTrustState.Redacted : visibility,
            actionClass,
            ExportEligible: !expired && query.RequestedAction == AuditRecordActionClassification.Exported,
            SeparateLogRequired: string.Equals(entry.PolicyReference, SeparateLogRequiredPolicy, StringComparison.Ordinal),
            entry.PolicyReference,
            nextAction);

        ConversationAuditRecordDetailsV1 details = new(
            query.SchemaVersion,
            query.TenantId,
            query.ConversationId,
            entry.ActorPartyId,
            entry.Timestamp,
            actionClass,
            GovernanceOutcome.Succeeded,
            entry.PolicyReference,
            entry.RationaleClass,
            entry.Target,
            entry.AuditEvidence,
            treatment,
            freshness,
            visibility,
            reason,
            query.CorrelationId);

        return ConversationAuditRecordResult.Visible(query.SchemaVersion, details, nextAction);
    }

    private static ConversationAuditRecordResult NonCurrent(GetConversationAuditRecordQuery query, ProjectionFreshnessV1 freshness)
    {
        if (freshness.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return ConversationAuditRecordResult.Rebuilding(query.SchemaVersion, freshness.ReasonCode);
        }

        if (freshness.FreshnessState == ProjectionTrustState.Unavailable)
        {
            return ConversationAuditRecordResult.Unavailable(
                query.SchemaVersion,
                freshness.ReasonCode,
                "Retry after the audit-record view is available.");
        }

        return new ConversationAuditRecordResult(
            query.SchemaVersion,
            freshness.FreshnessState,
            freshness.ReasonCode,
            AuditRecordActionClassification.Denied,
            null,
            "Retry after current audit evidence is available.");
    }

    private static AuditRecordActionClassification ActionClassFor(
        AuditRecordActionClassification requestedAction,
        bool expired)
    {
        if (expired)
        {
            return AuditRecordActionClassification.Redacted;
        }

        return requestedAction == AuditRecordActionClassification.Exported
            ? AuditRecordActionClassification.Exported
            : AuditRecordActionClassification.Allowed;
    }

    private static AuditRecordProjectionEntry? FindAuditRecord(
        ConversationDetailProjectionV1 detail,
        AuditEvidenceHandle handle)
    {
        if (detail.ActiveRetentionPolicy?.AuditEvidence.Handle == handle)
        {
            ConversationRetentionPolicyProjectionV1 retention = detail.ActiveRetentionPolicy;
            return new(
                retention.ActorPartyId,
                retention.AppliedAt,
                retention.PolicyReference,
                retention.Rationale,
                new GovernanceTarget(GovernedTargetKind.Conversation),
                retention.AuditEvidence,
                Redacted: false);
        }

        foreach (ConversationSensitivityMarkProjectionV1 mark in detail.SensitivityMarks)
        {
            if (mark.AuditEvidence.Handle == handle)
            {
                return new(
                    mark.ActorPartyId,
                    mark.MarkedAt,
                    mark.PolicyReference,
                    mark.Rationale,
                    mark.Target,
                    mark.AuditEvidence,
                    Redacted: mark.TrustState == ProjectionTrustState.Redacted);
            }
        }

        foreach (ConversationRedactionProjectionV1 redaction in detail.Redactions)
        {
            if (redaction.AuditEvidence?.Handle == handle && redaction.ActorPartyId is not null)
            {
                return new(
                    redaction.ActorPartyId,
                    redaction.RedactedAt,
                    redaction.PolicyReference,
                    redaction.ReasonClass,
                    redaction.Target,
                    redaction.AuditEvidence,
                    Redacted: true);
            }
        }

        return null;
    }

    private static bool SameGeneration(ProjectionFreshnessV1 summary, ProjectionFreshnessV1 detail)
        => summary.ProjectionCursor == detail.ProjectionCursor
            && summary.LastAppliedEventPosition == detail.LastAppliedEventPosition
            && summary.LastAppliedEventTimestamp.UtcTicks == detail.LastAppliedEventTimestamp.UtcTicks
            && summary.ProjectionGeneratedAt.UtcTicks == detail.ProjectionGeneratedAt.UtcTicks;

    private static bool ProjectionMatchesRequest(
        ConversationProjectedReadModels models,
        TenantId tenantId,
        ConversationId conversationId)
        => models.Summary.TenantId == tenantId
            && models.Detail.TenantId == tenantId
            && models.Summary.ConversationId == conversationId
            && models.Detail.ConversationId == conversationId;

    private sealed record AuditRecordProjectionEntry(
        PartyId ActorPartyId,
        DateTimeOffset Timestamp,
        string PolicyReference,
        string RationaleClass,
        GovernanceTarget Target,
        GovernanceAuditEvidenceReference AuditEvidence,
        bool Redacted);
}
