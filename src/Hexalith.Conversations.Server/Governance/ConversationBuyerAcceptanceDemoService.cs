// <copyright file="ConversationBuyerAcceptanceDemoService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Runs the self-serve buyer acceptance scenario over existing read and verification evidence.
/// </summary>
public sealed class ConversationBuyerAcceptanceDemoService(
    ConversationQueryHandler queryHandler,
    ConversationProjectionReadService projectionReadService,
    TimeProvider timeProvider)
{
    private readonly ConversationProjectionReadService _projectionReadService =
        projectionReadService ?? throw new ArgumentNullException(nameof(projectionReadService));

    private readonly ConversationQueryHandler _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>
    /// Executes the deterministic demo scenario without appending events or persisting evidence artifacts.
    /// </summary>
    public async ValueTask<BuyerAcceptanceEvidenceSummaryV1> RunAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        string runnerId,
        IReadOnlyList<ConversationGovernanceVerificationRunResultV1>? verificationOutputs = null,
        TenantId? crossTenantProbeTenantId = null,
        ConversationId? crossTenantProbeConversationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(runnerId);

        string? caller = string.IsNullOrWhiteSpace(callerPrincipalId) ? null : callerPrincipalId;
        bool hasTrustedAuthority = HasTrustedAuthority(scenario, trustedTenantId, caller);
        IReadOnlyList<ConversationGovernanceVerificationRunResultV1> scopedVerificationOutputs = hasTrustedAuthority
            ? ScopedVerificationOutputs(scenario, verificationOutputs ?? [])
            : [];
        List<BuyerAcceptanceEvidenceStepResultV1> results = [];
        foreach (BuyerAcceptanceDemoStepV1 step in scenario.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await RunStepAsync(
                scenario,
                step,
                trustedTenantId,
                caller,
                scopedVerificationOutputs,
                crossTenantProbeTenantId,
                crossTenantProbeConversationId,
                cancellationToken).ConfigureAwait(false));
        }

        BuyerAcceptanceDemoExecutionStatus status = results.All(result => result.Status == BuyerAcceptanceDemoExecutionStatus.Passed)
            ? BuyerAcceptanceDemoExecutionStatus.Passed
            : results.Any(result => result.Status == BuyerAcceptanceDemoExecutionStatus.Passed)
                ? BuyerAcceptanceDemoExecutionStatus.Partial
                : BuyerAcceptanceDemoExecutionStatus.Failed;

        return new BuyerAcceptanceEvidenceSummaryV1(
            scenario.SchemaVersion,
            scenario.TenantId,
            scenario.ScenarioId,
            scenario.SyntheticDataMarker,
            _timeProvider.GetUtcNow(),
            runnerId,
            scenario.CorrelationId,
            status,
            results,
            VerificationSummaries(scopedVerificationOutputs),
            scenario.RequirementMappings,
            status == BuyerAcceptanceDemoExecutionStatus.Passed
                ? "Buyer acceptance demo passed."
                : "Buyer acceptance demo did not pass.",
            [BuyerAcceptanceEvidenceOwnership.Module, BuyerAcceptanceEvidenceOwnership.InheritedPlatformControl]);
    }

    private async ValueTask<BuyerAcceptanceEvidenceStepResultV1> RunStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        IReadOnlyList<ConversationGovernanceVerificationRunResultV1> verificationOutputs,
        TenantId? crossTenantProbeTenantId,
        ConversationId? crossTenantProbeConversationId,
        CancellationToken cancellationToken)
    {
        bool passed = step.StepKind == BuyerAcceptanceDemoStepKind.Find
            ? await RunFindStepAsync(scenario, step, trustedTenantId, callerPrincipalId, cancellationToken).ConfigureAwait(false)
            : step.StepKind == BuyerAcceptanceDemoStepKind.ReadDetail
                ? await RunDetailStepAsync(scenario, step, trustedTenantId, callerPrincipalId, cancellationToken).ConfigureAwait(false)
                : step.StepKind == BuyerAcceptanceDemoStepKind.RedactionAudit
                    ? await RunRedactionAuditStepAsync(scenario, step, trustedTenantId, callerPrincipalId, cancellationToken).ConfigureAwait(false)
                    : step.StepKind == BuyerAcceptanceDemoStepKind.CitationCopy
                        ? await RunCitationStepAsync(scenario, step, trustedTenantId, callerPrincipalId, cancellationToken).ConfigureAwait(false)
                        : step.StepKind == BuyerAcceptanceDemoStepKind.TemporalReconstruction
                            ? await RunTemporalStepAsync(scenario, step, trustedTenantId, callerPrincipalId, cancellationToken).ConfigureAwait(false)
                            : step.StepKind == BuyerAcceptanceDemoStepKind.CommandMetadata
                                ? await RunCommandMetadataStepAsync(scenario, step, trustedTenantId, callerPrincipalId, cancellationToken).ConfigureAwait(false)
                                : step.StepKind == BuyerAcceptanceDemoStepKind.Verification
                                    ? HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId)
                                        && RunVerificationStep(step, verificationOutputs)
                                    : step.StepKind == BuyerAcceptanceDemoStepKind.CrossTenantDenial
                                        ? await RunCrossTenantDenialStepAsync(
                                            scenario,
                                            trustedTenantId,
                                            callerPrincipalId,
                                            crossTenantProbeTenantId,
                                            crossTenantProbeConversationId,
                                            cancellationToken).ConfigureAwait(false)
                                        : step.StepKind == BuyerAcceptanceDemoStepKind.EvidenceSummary
                                            && HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId);

        return new BuyerAcceptanceEvidenceStepResultV1(
            scenario.SchemaVersion,
            step.StepId,
            step.StepKind,
            passed ? BuyerAcceptanceDemoExecutionStatus.Passed : BuyerAcceptanceDemoExecutionStatus.Failed,
            step.ExpectedTrustState,
            BuyerAcceptanceEvidenceOwnership.Module,
            passed ? "Step passed." : "Step did not pass.",
            step.SafeNextAction,
            step.RequirementMappings,
            step.EvidenceHandles);
    }

    private async ValueTask<bool> RunFindStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId) || step.BusinessReference is null)
        {
            return false;
        }

        ConversationListResult result = await _queryHandler.ListAsync(
            new ListConversationsQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                new ConversationListFilterV1(step.BusinessReference),
                new ConversationPageRequest(25)),
            cancellationToken).ConfigureAwait(false);

        return result.FreshnessState == ProjectionTrustState.Current
            && (step.ConversationId is null || result.Conversations.Any(conversation => conversation.ConversationId == step.ConversationId));
    }

    private async ValueTask<bool> RunDetailStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId) || step.ConversationId is null)
        {
            return false;
        }

        ConversationDetailResult result = await _queryHandler.GetAsync(
            new GetConversationQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                step.ConversationId),
            cancellationToken).ConfigureAwait(false);

        if (step.ExpectedTrustState == BuyerAcceptanceDemoTrustState.Stale)
        {
            return result.Details is null;
        }

        if (step.ExpectedTrustState == BuyerAcceptanceDemoTrustState.Unavailable)
        {
            return result.Details?.TrustPosture.ParticipantResolutionState == ProjectionTrustState.Unavailable;
        }

        return result.Details is not null && result.FreshnessState == ProjectionTrustState.Current;
    }

    private async ValueTask<bool> RunRedactionAuditStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId) || step.ConversationId is null)
        {
            return false;
        }

        ConversationDetailResult result = await _queryHandler.GetAsync(
            new GetConversationQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                step.ConversationId),
            cancellationToken).ConfigureAwait(false);

        bool detailHasRedactionEvidence = result.Details?.EvidenceEntries.Any(entry =>
            entry.RedactionAttribution is not null
            && entry.VisibleText == "[redacted]"
            && entry.AuditEvidence is not null) == true;

        if (!detailHasRedactionEvidence || step.AuditEvidenceHandle is null)
        {
            return false;
        }

        ConversationAuditRecordResult auditRecord = await _queryHandler.GetAuditRecordAsync(
            new GetConversationAuditRecordQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                step.ConversationId,
                step.AuditEvidenceHandle.Value,
                AuditRecordActionClassification.Allowed),
            cancellationToken).ConfigureAwait(false);

        return auditRecord.Details is not null
            && auditRecord.FreshnessState == ProjectionTrustState.Current;
    }

    private async ValueTask<bool> RunCitationStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId)
            || step.ConversationId is null
            || step.EvidenceEntryId is null)
        {
            return false;
        }

        ConversationCitationResult result = await _queryHandler.GetCitationAsync(
            new GetConversationCitationQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                step.ConversationId,
                step.EvidenceEntryId),
            cancellationToken).ConfigureAwait(false);

        if (step.ExpectedTrustState == BuyerAcceptanceDemoTrustState.Incomplete)
        {
            return result.Citation?.CitationAvailability == ConversationCitationAvailability.Incomplete
                || result.Citation?.CitationAvailability == ConversationCitationAvailability.Unavailable
                || (result.Citation is null && result.FreshnessState != ProjectionTrustState.Current);
        }

        return result.Citation is not null
            && result.Citation.SafeCopiedText.Contains(step.EvidenceEntryId, StringComparison.Ordinal);
    }

    private async ValueTask<bool> RunTemporalStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId) || step.ConversationId is null)
        {
            return false;
        }

        ConversationDetailResult detail = await _queryHandler.GetAsync(
            new GetConversationQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                step.ConversationId),
            cancellationToken).ConfigureAwait(false);

        ConversationTemporalAnchorV1? anchor = BuildTemporalAnchor(scenario, step, detail.Details);
        if (anchor is null)
        {
            return false;
        }

        ConversationTemporalDetailResult temporal = await _queryHandler.GetAtPointInTimeAsync(
            new GetConversationAtPointInTimeQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                step.ConversationId,
                anchor),
            cancellationToken).ConfigureAwait(false);

        return temporal.Details is not null
            && temporal.AuthoritativeTemporalAnchor is not null
            && temporal.AuthoritativeTemporalAnchor.AnchorKind == ConversationTemporalAnchorV1.CompositeCursorKind
            && temporal.Confidence.IsComplete;
    }

    private async ValueTask<bool> RunCommandMetadataStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId) || step.ConversationId is null)
        {
            return false;
        }

        ConversationDetailResult detail = await _queryHandler.GetAsync(
            new GetConversationQuery(
                scenario.SchemaVersion,
                scenario.TenantId,
                callerPrincipalId!,
                scenario.CorrelationId,
                step.ConversationId),
            cancellationToken).ConfigureAwait(false);

        return detail.Details?.TrustPosture.CommandEligibility.Any(command =>
            command.AvailabilityState == ProjectionTrustState.Unavailable
            && command.RequiresFreshServerRecheck
            && command.ActionClassification == ConversationCommandAvailabilityV1.GovernanceChangingActionClassification) == true;
    }

    private static bool RunVerificationStep(
        BuyerAcceptanceDemoStepV1 step,
        IReadOnlyList<ConversationGovernanceVerificationRunResultV1> verificationOutputs)
    {
        if (step.FixtureKind == BuyerAcceptanceDemoFixtureKind.VerificationFailure)
        {
            return verificationOutputs.Any(output =>
                output.Classification == ConversationGovernanceVerificationFailureClassification.GovernanceFailed
                || output.Classification == ConversationGovernanceVerificationFailureClassification.InfrastructureFailed);
        }

        return verificationOutputs.Any(output => output.Classification == ConversationGovernanceVerificationFailureClassification.Passed);
    }

    private async ValueTask<bool> RunCrossTenantDenialStepAsync(
        BuyerAcceptanceDemoScenarioV1 scenario,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        TenantId? crossTenantProbeTenantId,
        ConversationId? crossTenantProbeConversationId,
        CancellationToken cancellationToken)
    {
        if (!HasTrustedAuthority(scenario, trustedTenantId, callerPrincipalId)
            || crossTenantProbeTenantId is null
            || crossTenantProbeConversationId is null
            || crossTenantProbeTenantId == trustedTenantId)
        {
            return false;
        }

        ConversationProjectionReadResult result = await _projectionReadService
            .ReadDetailAsync(
                trustedTenantId,
                callerPrincipalId!,
                crossTenantProbeTenantId,
                crossTenantProbeConversationId,
                cancellationToken)
            .ConfigureAwait(false);

        return result.Projection is null && result.FreshnessState == ProjectionTrustState.Forbidden;
    }

    private static IReadOnlyList<ConversationGovernanceVerificationRunResultV1> ScopedVerificationOutputs(
        BuyerAcceptanceDemoScenarioV1 scenario,
        IReadOnlyList<ConversationGovernanceVerificationRunResultV1> outputs)
    {
        HashSet<ConversationId> scenarioConversationIds = scenario.Fixtures
            .Select(fixture => fixture.ConversationId)
            .Concat(scenario.Steps.Select(step => step.ConversationId))
            .OfType<ConversationId>()
            .ToHashSet();

        return outputs
            .Where(output => output.Scope.TenantId == scenario.TenantId)
            .Where(output => output.Scope.ConversationId is null || scenarioConversationIds.Contains(output.Scope.ConversationId))
            .ToArray();
    }

    private static IReadOnlyList<BuyerAcceptanceVerificationSummaryV1> VerificationSummaries(
        IReadOnlyList<ConversationGovernanceVerificationRunResultV1> outputs)
        => outputs
            .SelectMany(output => output.Checks.Select(check => new BuyerAcceptanceVerificationSummaryV1(
                output.SchemaVersion,
                check.Suite,
                check.Status,
                check.Classification,
                check.SafeDetail,
                check.Remediation,
                check.RequirementMappings)))
            .ToArray();

    private static bool HasTrustedAuthority(
        BuyerAcceptanceDemoScenarioV1 scenario,
        TenantId? trustedTenantId,
        string? callerPrincipalId)
        => trustedTenantId == scenario.TenantId && !string.IsNullOrWhiteSpace(callerPrincipalId);

    private static ConversationTemporalAnchorV1? BuildTemporalAnchor(
        BuyerAcceptanceDemoScenarioV1 scenario,
        BuyerAcceptanceDemoStepV1 step,
        ConversationDetailsV1? details)
    {
        if (step.ConversationId is null)
        {
            return null;
        }

        if (step.TemporalCursor is not null)
        {
            return new ConversationTemporalAnchorV1(
                scenario.SchemaVersion,
                scenario.TenantId,
                step.ConversationId,
                ConversationTemporalAnchorV1.ContractCursorKind,
                ContractCursor: step.TemporalCursor);
        }

        ConversationEvidenceEntryV1? anchorEntry = details?.EvidenceEntries.FirstOrDefault(entry => entry.SafeSourcePosition is > 0);
        return anchorEntry?.SafeSourcePosition is long safeSourcePosition
            ? new ConversationTemporalAnchorV1(
                scenario.SchemaVersion,
                scenario.TenantId,
                step.ConversationId,
                ConversationTemporalAnchorV1.SafeSourcePositionKind,
                SafeSourcePosition: safeSourcePosition)
            : null;
    }
}
