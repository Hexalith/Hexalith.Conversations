// <copyright file="ConversationPrivilegedOperationalJustificationService.cs" company="ITANEO">
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

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Enforces privileged operational justification, tenant access, freshness, and audit preconditions.
/// </summary>
public sealed class ConversationPrivilegedOperationalJustificationService(
    IConversationTenantAccessService tenantAccessService,
    IConversationProjectionReadStore projectionReadStore,
    IConversationGovernanceAuditService auditService,
    TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan MaximumJustificationAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromMinutes(5);

    private readonly IConversationTenantAccessService _tenantAccessService =
        tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));

    private readonly IConversationProjectionReadStore _projectionReadStore =
        projectionReadStore ?? throw new ArgumentNullException(nameof(projectionReadStore));

    private readonly IConversationGovernanceAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Executes a privileged action only after authorization, current freshness, valid justification, and safe audit evidence.
    /// </summary>
    public async ValueTask<PrivilegedOperationalJustificationResult> ExecuteAsync(
        RecordPrivilegedOperationalJustificationCommand? command,
        Func<PrivilegedOperationalJustificationV1, CancellationToken, ValueTask<PrivilegedOperationalActionOutcome>> actionAsync,
        string callerPrincipalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actionAsync);
        ValidateCallerPrincipalId(callerPrincipalId);

        if (command?.Justification is null)
        {
            return PrivilegedOperationalJustificationResult.Denied(
                Contracts.Versioning.SchemaVersion.Current,
                "Resubmit with structured privileged operational justification.");
        }

        PrivilegedOperationalJustificationV1 justification = command.Justification;
        if (!TimestampIsFresh(justification.OperationTimestamp))
        {
            return PrivilegedOperationalJustificationResult.Denied(
                justification.SchemaVersion,
                "Resubmit with current privileged operational justification.");
        }

        ConversationTenantAccessRequirement requirement = RequirementFor(justification.OperationClass);
        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                requirement,
                justification.TenantId,
                callerPrincipalId,
                routeTenantId: justification.TenantId,
                projectionTenantId: justification.TenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return PrivilegedOperationalJustificationResult.Hidden(justification.SchemaVersion);
        }

        ProjectionFreshnessV1? freshness = null;
        if (justification.ConversationId is not null)
        {
            ConversationProjectedReadModels? models;
            try
            {
                models = await _projectionReadStore
                    .ReadAsync(justification.TenantId, justification.ConversationId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ConversationProjectionConsistencyException)
            {
                // A partial generation is not a store outage: the privileged surface must fail closed on it
                // rather than let the exception escape this service unmapped.
                return PrivilegedOperationalJustificationResult.Unavailable(
                    justification.SchemaVersion,
                    ProjectionFreshnessReasonCode.MixedGeneration,
                    "Retry after the read model finishes rebuilding.");
            }
            catch (InvalidOperationException)
            {
                return PrivilegedOperationalJustificationResult.Unavailable(
                    justification.SchemaVersion,
                    ProjectionFreshnessReasonCode.Unavailable,
                    "Retry after privileged evidence is available.");
            }
            catch (IOException)
            {
                return PrivilegedOperationalJustificationResult.Unavailable(
                    justification.SchemaVersion,
                    ProjectionFreshnessReasonCode.Unavailable,
                    "Retry after privileged evidence is available.");
            }
            catch (TimeoutException)
            {
                return PrivilegedOperationalJustificationResult.Unavailable(
                    justification.SchemaVersion,
                    ProjectionFreshnessReasonCode.Unavailable,
                    "Retry after privileged evidence is available.");
            }

            if (models is null || !ProjectionMatchesRequest(models, justification.TenantId, justification.ConversationId))
            {
                return PrivilegedOperationalJustificationResult.Hidden(justification.SchemaVersion);
            }

            freshness = models.Detail.Freshness;
            if (!SameGeneration(models.Summary.Freshness, freshness))
            {
                return PrivilegedOperationalJustificationResult.Rebuilding(
                    justification.SchemaVersion,
                    ProjectionFreshnessReasonCode.MixedGeneration);
            }

            if (!freshness.AllowsTrustBearingDecision())
            {
                return NonCurrent(justification, freshness);
            }
        }

        ConversationGovernanceAuditResult audit = await ConversationGovernanceAuditGate
            .RecordRequiredAsync(
                ct => _auditService.RecordPrivilegedOperationalJustificationAsync(
                    command,
                    GovernanceOperationKind.RecordPrivilegedJustification,
                    GovernanceOutcome.Succeeded,
                    OperationId(justification),
                    ct),
                cancellationToken)
            .ConfigureAwait(false);

        if (audit.Status != ConversationGovernanceAuditStatus.Succeeded || audit.Evidence is null)
        {
            return AuditFailure(justification, audit.Status);
        }

        PrivilegedOperationalActionOutcome outcome;
        try
        {
            outcome = await actionAsync(justification, cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(outcome);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            outcome = PrivilegedOperationalActionOutcome.PolicyBlocked("Retry after privileged operation is reconciled.");
        }

        ConversationGovernanceAuditResult outcomeAudit = audit;
        if (outcome.Outcome != GovernanceOutcome.Succeeded)
        {
            outcomeAudit = await ConversationGovernanceAuditGate
                .RecordRequiredAsync(
                    ct => _auditService.RecordPrivilegedOperationalJustificationAsync(
                        command,
                        GovernanceOperationKind.RecordPrivilegedJustification,
                        outcome.Outcome,
                        OperationId(justification),
                        ct),
                    cancellationToken)
                .ConfigureAwait(false);

            if (outcomeAudit.Status != ConversationGovernanceAuditStatus.Succeeded || outcomeAudit.Evidence is null)
            {
                return AuditFailure(justification, outcomeAudit.Status);
            }
        }

        ProjectionFreshnessV1 visibleFreshness = freshness ?? TenantScopeFreshness(justification);
        PrivilegedOperationalJustificationDetailsV1 details = new(
            justification.SchemaVersion,
            justification.TenantId,
            justification.ConversationId,
            justification.GovernedScope,
            justification.ActorPartyId,
            justification.OperationClass,
            justification.PrivilegedActionClass,
            justification.PolicyReference,
            justification.Rationale,
            justification.OperationTimestamp,
            outcome.Outcome,
            outcomeAudit.Evidence,
            ProjectionTrustState.Current,
            visibleFreshness,
            outcome.SafeNextAction,
            justification.CorrelationId,
            justification.CausationId);

        return PrivilegedOperationalJustificationResult.Visible(
            justification.SchemaVersion,
            details,
            outcome.SafeNextAction);
    }

    private static ConversationTenantAccessRequirement RequirementFor(PrivilegedOperationalActionClass actionClass)
        => actionClass == PrivilegedOperationalActionClass.VisibilityChange
            || actionClass == PrivilegedOperationalActionClass.MetadataChange
                ? ConversationTenantAccessRequirement.Governance
                : ConversationTenantAccessRequirement.Admin;

    private bool TimestampIsFresh(DateTimeOffset timestamp)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        return timestamp <= now.Add(MaximumFutureSkew) && now - timestamp <= MaximumJustificationAge;
    }

    private static string ValidateCallerPrincipalId(string callerPrincipalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callerPrincipalId);
        if (!string.Equals(callerPrincipalId, callerPrincipalId.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("Caller principal id must be canonical.", nameof(callerPrincipalId));
        }

        foreach (char c in callerPrincipalId)
        {
            if (char.IsControl(c))
            {
                throw new ArgumentException("Caller principal id must be canonical.", nameof(callerPrincipalId));
            }
        }

        return callerPrincipalId;
    }

    private static PrivilegedOperationalJustificationResult NonCurrent(
        PrivilegedOperationalJustificationV1 justification,
        ProjectionFreshnessV1 freshness)
    {
        if (freshness.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return PrivilegedOperationalJustificationResult.Rebuilding(justification.SchemaVersion, freshness.ReasonCode);
        }

        if (freshness.FreshnessState == ProjectionTrustState.Unavailable)
        {
            return PrivilegedOperationalJustificationResult.Unavailable(
                justification.SchemaVersion,
                freshness.ReasonCode,
                "Retry after privileged evidence is available.");
        }

        return new PrivilegedOperationalJustificationResult(
            justification.SchemaVersion,
            freshness.FreshnessState,
            freshness.ReasonCode,
            GovernanceOutcome.Denied,
            null,
            "Retry after current privileged evidence is available.");
    }

    private static PrivilegedOperationalJustificationResult AuditFailure(
        PrivilegedOperationalJustificationV1 justification,
        ConversationGovernanceAuditStatus status)
        => status == ConversationGovernanceAuditStatus.PolicyBlocked
            ? PrivilegedOperationalJustificationResult.PolicyBlocked(
                justification.SchemaVersion,
                "The privileged action is blocked by policy.")
            : PrivilegedOperationalJustificationResult.Unavailable(
                justification.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after privileged audit evidence is available.");

    private static ProjectionFreshnessV1 TenantScopeFreshness(PrivilegedOperationalJustificationV1 justification)
        => new(
            justification.SchemaVersion,
            "tenant-scope",
            1,
            justification.OperationTimestamp,
            justification.OperationTimestamp,
            TimeSpan.Zero,
            false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

    private static string OperationId(PrivilegedOperationalJustificationV1 justification)
        => $"{justification.CorrelationId}:{justification.OperationClass.Value}";

    private static bool ProjectionMatchesRequest(
        ConversationProjectedReadModels models,
        TenantId tenantId,
        ConversationId conversationId)
        => models.Summary.TenantId == tenantId
            && models.Detail.TenantId == tenantId
            && models.Summary.ConversationId == conversationId
            && models.Detail.ConversationId == conversationId;

    private static bool SameGeneration(ProjectionFreshnessV1 summary, ProjectionFreshnessV1 detail)
        => summary.ProjectionCursor == detail.ProjectionCursor
            && summary.LastAppliedEventPosition == detail.LastAppliedEventPosition
            && summary.LastAppliedEventTimestamp.UtcTicks == detail.LastAppliedEventTimestamp.UtcTicks
            && summary.ProjectionGeneratedAt.UtcTicks == detail.ProjectionGeneratedAt.UtcTicks;
}
