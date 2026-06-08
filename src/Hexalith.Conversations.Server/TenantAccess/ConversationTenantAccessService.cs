// <copyright file="ConversationTenantAccessService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.TenantAccess;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Checks Conversations tenant access from the local Tenants projection.
/// </summary>
/// <param name="projectionStore">The local tenant projection store.</param>
/// <param name="projectionSignal">The Conversations-owned projection signal for freshness, gap, rollback, and poisoning detection.</param>
/// <param name="logger">The service logger.</param>
public sealed class ConversationTenantAccessService(
    ITenantProjectionStore projectionStore,
    IConversationTenantProjectionSignal projectionSignal,
    ILogger<ConversationTenantAccessService> logger)
    : IConversationTenantAccessService
{
    private readonly ILogger<ConversationTenantAccessService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ITenantAccessProjectionHealthProvider _projectionHealth =
        new ConversationTenantProjectionHealthProvider(
            projectionSignal ?? throw new ArgumentNullException(nameof(projectionSignal)));

    private readonly ITenantAccessStateStore _stateStore =
        new ConversationTenantAccessStateStore(
            projectionStore ?? throw new ArgumentNullException(nameof(projectionStore)));

    /// <inheritdoc />
    public async ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
        ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        TenantId? routeTenantId = null,
        TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null,
        TenantId? projectionTenantId = null,
        TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
    {
        TenantAccessEvaluation<ConversationTenantAccessRequirement> evaluation =
            await TenantAccessEvaluator.EvaluateAsync(
                requirement,
                [
                    trustedTenantId?.Value,
                    routeTenantId?.Value,
                    commandTenantId?.Value,
                    aggregateTenantId?.Value,
                    projectionTenantId?.Value,
                    idempotencyTenantId?.Value,
                ],
                callerPrincipalId,
                _stateStore,
                _projectionHealth,
                static requirement => Enum.IsDefined(requirement),
                static status => Enum.IsDefined((TenantStatus)status),
                static status => (TenantStatus)status == TenantStatus.Active,
                static status => (TenantStatus)status == TenantStatus.Disabled,
                static role => Enum.IsDefined((TenantRole)role),
                static (role, requirement) => HasPermission((TenantRole)role, requirement),
                _logger,
                cancellationToken).ConfigureAwait(false);

        return ToConversationDecision(evaluation, trustedTenantId);
    }

    private static bool HasPermission(TenantRole role, ConversationTenantAccessRequirement requirement)
        => role switch
        {
            TenantRole.TenantReader => requirement == ConversationTenantAccessRequirement.Read,
            TenantRole.TenantContributor => requirement is ConversationTenantAccessRequirement.Read or ConversationTenantAccessRequirement.Write,
            TenantRole.TenantOwner => requirement is ConversationTenantAccessRequirement.Read
                or ConversationTenantAccessRequirement.Write
                or ConversationTenantAccessRequirement.Admin
                or ConversationTenantAccessRequirement.Governance,
            _ => false,
        };

    private static ConversationTenantAccessDecision ToConversationDecision(
        TenantAccessEvaluation<ConversationTenantAccessRequirement> evaluation,
        TenantId? fallbackTenantId)
    {
        TenantId? tenantId = evaluation.TenantId is null
            ? fallbackTenantId
            : new TenantId(evaluation.TenantId);

        return evaluation.IsAllowed
            ? ConversationTenantAccessDecision.Allowed(
                evaluation.Requirement,
                tenantId!,
                evaluation.CallerPrincipalId!,
                evaluation.ProjectionVersion,
                evaluation.ProjectionWatermark)
            : ConversationTenantAccessDecision.Denied(
                evaluation.Requirement,
                tenantId,
                evaluation.CallerPrincipalId,
                MapDenial(evaluation.DenialKind),
                evaluation.IsRetryable,
                evaluation.ProjectionVersion,
                evaluation.ProjectionWatermark);
    }

    private static ConversationTenantAccessDenialReason MapDenial(TenantAccessDenialKind denial)
        => denial switch
        {
            TenantAccessDenialKind.None => ConversationTenantAccessDenialReason.None,
            TenantAccessDenialKind.MissingTenant => ConversationTenantAccessDenialReason.MissingTenant,
            TenantAccessDenialKind.MalformedTenant => ConversationTenantAccessDenialReason.MalformedTenant,
            TenantAccessDenialKind.TenantMismatch => ConversationTenantAccessDenialReason.TenantMismatch,
            TenantAccessDenialKind.MissingCaller => ConversationTenantAccessDenialReason.MissingCaller,
            TenantAccessDenialKind.TenantAccessUnavailable => ConversationTenantAccessDenialReason.TenantAccessUnavailable,
            TenantAccessDenialKind.TenantAccessStale => ConversationTenantAccessDenialReason.TenantAccessStale,
            TenantAccessDenialKind.TenantAccessGapDetected => ConversationTenantAccessDenialReason.TenantAccessGapDetected,
            TenantAccessDenialKind.TenantAccessRolledBack => ConversationTenantAccessDenialReason.TenantAccessRolledBack,
            TenantAccessDenialKind.TenantProjectionPoisoned => ConversationTenantAccessDenialReason.TenantProjectionPoisoned,
            TenantAccessDenialKind.UnknownTenant => ConversationTenantAccessDenialReason.UnknownTenant,
            TenantAccessDenialKind.MalformedProjection => ConversationTenantAccessDenialReason.MalformedProjection,
            TenantAccessDenialKind.UnmappedStatus => ConversationTenantAccessDenialReason.UnmappedStatus,
            TenantAccessDenialKind.TenantDisabled => ConversationTenantAccessDenialReason.TenantDisabled,
            TenantAccessDenialKind.MissingMember => ConversationTenantAccessDenialReason.MissingMember,
            TenantAccessDenialKind.UnmappedRole => ConversationTenantAccessDenialReason.UnmappedRole,
            TenantAccessDenialKind.InsufficientRole => ConversationTenantAccessDenialReason.InsufficientRole,
            _ => ConversationTenantAccessDenialReason.TenantProjectionPoisoned,
        };

    private sealed class ConversationTenantAccessStateStore(ITenantProjectionStore projectionStore) : ITenantAccessStateStore
    {
        public async Task<TenantAccessState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            TenantLocalState? state = await projectionStore.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
            return state is null
                ? null
                : new TenantAccessState(
                    state.TenantId,
                    (int)state.Status,
                    state.Members?.ToDictionary(
                        static pair => pair.Key,
                        static pair => (int)pair.Value,
                        StringComparer.Ordinal));
        }
    }

    private sealed class ConversationTenantProjectionHealthProvider(
        IConversationTenantProjectionSignal projectionSignal)
        : ITenantAccessProjectionHealthProvider
    {
        public async ValueTask<TenantAccessProjectionHealth?> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            ConversationTenantProjectionHealth? health =
                await projectionSignal.GetProjectionHealthAsync(tenantId, cancellationToken).ConfigureAwait(false);

            // Defense-in-depth: a non-conforming signal that returns a null health record must fail
            // closed through the shared evaluator's null-health path (TenantAccessUnavailable, retryable),
            // matching the pre-promotion ConversationTenantAccessService behavior, instead of throwing an NRE.
            return health is null
                ? null
                : new TenantAccessProjectionHealth(
                    health.Version,
                    health.Watermark,
                    health.IsStale,
                    health.HasGap,
                    health.HasRollback,
                    health.IsPoisoned);
        }
    }
}
