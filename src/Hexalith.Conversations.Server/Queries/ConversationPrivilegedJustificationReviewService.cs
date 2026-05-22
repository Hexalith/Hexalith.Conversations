// <copyright file="ConversationPrivilegedJustificationReviewService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Evaluates privileged-action review access before resolving privileged audit evidence.
/// </summary>
public sealed class ConversationPrivilegedJustificationReviewService(
    IConversationTenantAccessService tenantAccessService,
    IPrivilegedOperationalJustificationReviewSource reviewSource)
{
    private readonly IConversationTenantAccessService _tenantAccessService =
        tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));

    private readonly IPrivilegedOperationalJustificationReviewSource _reviewSource =
        reviewSource ?? throw new ArgumentNullException(nameof(reviewSource));

    /// <summary>
    /// Reads one authorized privileged-action justification record.
    /// </summary>
    public async ValueTask<PrivilegedOperationalJustificationResult> GetAsync(
        GetPrivilegedOperationalJustificationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                ConversationTenantAccessRequirement.Governance,
                query.TenantId,
                query.CallerPrincipalId,
                routeTenantId: query.TenantId,
                projectionTenantId: query.TenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return PrivilegedOperationalJustificationResult.Hidden(query.SchemaVersion);
        }

        AuditEvidenceHandle handle;
        try
        {
            handle = new AuditEvidenceHandle(query.AuditEvidenceHandle);
        }
        catch (ArgumentException)
        {
            return PrivilegedOperationalJustificationResult.Hidden(query.SchemaVersion);
        }

        PrivilegedOperationalJustificationDetailsV1? details;
        try
        {
            details = await _reviewSource
                .ReadAsync(query.TenantId, query.ConversationId, handle, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return Unavailable(query);
        }
        catch (IOException)
        {
            return Unavailable(query);
        }
        catch (TimeoutException)
        {
            return Unavailable(query);
        }

        if (details is null
            || details.TenantId != query.TenantId
            || details.ConversationId != query.ConversationId
            || details.AuditEvidence.Handle != handle)
        {
            return PrivilegedOperationalJustificationResult.Hidden(query.SchemaVersion);
        }

        if (!details.Freshness.AllowsTrustBearingDecision())
        {
            return NonCurrent(query, details.Freshness);
        }

        return PrivilegedOperationalJustificationResult.Visible(
            query.SchemaVersion,
            details,
            details.SafeNextAction);
    }

    private static PrivilegedOperationalJustificationResult NonCurrent(
        GetPrivilegedOperationalJustificationQuery query,
        ProjectionFreshnessV1 freshness)
    {
        if (freshness.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return PrivilegedOperationalJustificationResult.Rebuilding(query.SchemaVersion, freshness.ReasonCode);
        }

        if (freshness.FreshnessState == ProjectionTrustState.Unavailable)
        {
            return Unavailable(query);
        }

        return new PrivilegedOperationalJustificationResult(
            query.SchemaVersion,
            freshness.FreshnessState,
            freshness.ReasonCode,
            GovernanceOutcome.Denied,
            null,
            "Retry after current privileged-action evidence is available.");
    }

    private static PrivilegedOperationalJustificationResult Unavailable(GetPrivilegedOperationalJustificationQuery query)
        => PrivilegedOperationalJustificationResult.Unavailable(
            query.SchemaVersion,
            ProjectionFreshnessReasonCode.Unavailable,
            "Retry after privileged-action evidence is available.");
}
