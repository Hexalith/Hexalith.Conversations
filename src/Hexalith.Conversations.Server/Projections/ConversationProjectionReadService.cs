// <copyright file="ConversationProjectionReadService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Applies tenant access and freshness checks before returning projection details.
/// </summary>
public sealed class ConversationProjectionReadService(
    IConversationTenantAccessService tenantAccessService,
    IConversationProjectionReadStore projectionReadStore,
    IConversationProjectionTelemetry? telemetry = null)
{
    private readonly IConversationProjectionReadStore _projectionReadStore =
        projectionReadStore ?? throw new ArgumentNullException(nameof(projectionReadStore));

    private readonly IConversationTenantAccessService _tenantAccessService =
        tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));

    private readonly IConversationProjectionTelemetry? _telemetry = telemetry;

    /// <summary>
    /// Reads a conversation detail projection through the fail-closed tenant and freshness boundary.
    /// </summary>
    /// <param name="trustedTenantId">The trusted request tenant binding.</param>
    /// <param name="callerPrincipalId">The caller principal identity.</param>
    /// <param name="routeTenantId">The route tenant binding.</param>
    /// <param name="conversationId">The conversation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A non-disclosing projection read result.</returns>
    public async ValueTask<ConversationProjectionReadResult> ReadDetailAsync(
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        TenantId? routeTenantId,
        ConversationId? conversationId,
        CancellationToken cancellationToken = default)
    {
        if (trustedTenantId is null || routeTenantId is null || conversationId is null || string.IsNullOrWhiteSpace(callerPrincipalId))
        {
            return Forbidden();
        }

        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                ConversationTenantAccessRequirement.Read,
                trustedTenantId,
                callerPrincipalId,
                routeTenantId: routeTenantId,
                projectionTenantId: routeTenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return Forbidden();
        }

        cancellationToken.ThrowIfCancellationRequested();

        ConversationProjectedReadModels? models;
        try
        {
            models = await _projectionReadStore
                .ReadAsync(routeTenantId, conversationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ConversationProjectionConsistencyException)
        {
            return EmitFreshnessTelemetryAndReturn(new ConversationProjectionReadResult(
                ProjectionTrustState.Rebuilding,
                ProjectionFreshnessReasonCode.MixedGeneration,
                null,
                false));
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return EmitFreshnessTelemetryAndReturn(Unavailable());
        }

        if (models is null)
        {
            return Forbidden();
        }

        if (!ProjectionMatchesRequest(models, routeTenantId, conversationId))
        {
            return new ConversationProjectionReadResult(
                ProjectionTrustState.Forbidden,
                ProjectionFreshnessReasonCode.PoisonEvent,
                null,
                false);
        }

        if (!SameGeneration(models.Summary.Freshness, models.Detail.Freshness))
        {
            return EmitFreshnessTelemetryAndReturn(new ConversationProjectionReadResult(
                ProjectionTrustState.Rebuilding,
                ProjectionFreshnessReasonCode.MixedGeneration,
                null,
                false));
        }

        ProjectionFreshnessV1 freshness = models.Detail.Freshness;
        bool enabled = freshness.AllowsTrustBearingDecision();
        return EmitFreshnessTelemetryAndReturn(new ConversationProjectionReadResult(
            freshness.FreshnessState,
            freshness.ReasonCode,
            enabled ? models.Detail : null,
            enabled));
    }

    private static ConversationProjectionReadResult Forbidden()
        => new(
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            null,
            false);

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

    private static ConversationProjectionReadResult Unavailable()
        => new(
            ProjectionTrustState.Unavailable,
            ProjectionFreshnessReasonCode.Unavailable,
            null,
            false);

    private ConversationProjectionReadResult EmitFreshnessTelemetryAndReturn(ConversationProjectionReadResult result)
    {
        if (_telemetry is not null && result.FreshnessState != ProjectionTrustState.Forbidden)
        {
            string safeCorrelationId = Guid.NewGuid().ToString("N")[..8];
            ConversationProjectionFreshnessClass freshnessClass =
                ConversationProjectionFreshnessClassifier.Classify(result.FreshnessState, result.ReasonCode);
            ConversationProjectionLagClass lagClass =
                ConversationProjectionFreshnessClassifier.ClassifyLag(result.ReasonCode);
            _telemetry.RecordProjectionFreshnessState(freshnessClass, lagClass, safeCorrelationId);
            if (result.FreshnessState == ProjectionTrustState.Rebuilding)
            {
                _telemetry.RecordProjectionRebuildProgress(ConversationProjectionFreshnessClass.Rebuilding, safeCorrelationId);
            }
        }

        return result;
    }
}
