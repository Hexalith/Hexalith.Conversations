// <copyright file="ConversationGovernanceVerificationService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Orchestrates read-only governance verification from existing derived and replay evidence.
/// </summary>
public sealed class ConversationGovernanceVerificationService
{
    private static readonly ConversationGovernanceVerificationEvidenceHandle LocalEvidence =
        new("verification-proof-local");

    private readonly IConversationTemporalEventSource _eventSource;
    private readonly IConversationProjectionReadStore _projectionReadStore;
    private readonly ConversationProjectionReadService _projectionReadService;
    private readonly ConversationProjectionRebuildVerifier _rebuildVerifier;
    private readonly IConversationTenantAccessService _tenantAccessService;

    public ConversationGovernanceVerificationService(
        IConversationTenantAccessService tenantAccessService,
        ConversationProjectionReadService projectionReadService,
        IConversationProjectionReadStore projectionReadStore,
        IConversationTemporalEventSource eventSource,
        ConversationProjectionRebuildVerifier rebuildVerifier)
    {
        _tenantAccessService = tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));
        _projectionReadService = projectionReadService ?? throw new ArgumentNullException(nameof(projectionReadService));
        _projectionReadStore = projectionReadStore ?? throw new ArgumentNullException(nameof(projectionReadStore));
        _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
        _rebuildVerifier = rebuildVerifier ?? throw new ArgumentNullException(nameof(rebuildVerifier));
    }

    /// <summary>
    /// Runs governance verification after binding tenant and caller authority from the trusted server boundary.
    /// </summary>
    public async ValueTask<ConversationGovernanceVerificationRunResultV1> VerifyAsync(
        ConversationGovernanceVerificationRequestV1 request,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        PrivilegedOperationalJustificationDetailsV1? privilegedJustification,
        DateTimeOffset generatedAtUtc,
        bool localReadOnlyEvidenceOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (trustedTenantId is null
            || trustedTenantId != request.Scope.TenantId
            || string.IsNullOrWhiteSpace(callerPrincipalId))
        {
            return BuildSingleCheckResult(
                request,
                generatedAtUtc,
                ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden,
                ConversationGovernanceVerificationExecutionStatus.Blocked,
                "authorized-scope",
                ConversationGovernanceVerificationRemediation.RequestAuthorization,
                "Requested scope is hidden or unavailable.",
                AuditEvidence(privilegedJustification),
                auditNotRecordedReason: null);
        }

        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                ConversationTenantAccessRequirement.Admin,
                trustedTenantId,
                callerPrincipalId,
                routeTenantId: request.Scope.TenantId,
                projectionTenantId: request.Scope.TenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return BuildSingleCheckResult(
                request,
                generatedAtUtc,
                ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden,
                ConversationGovernanceVerificationExecutionStatus.Blocked,
                "tenant-access",
                ConversationGovernanceVerificationRemediation.RequestAuthorization,
                "Requested scope is hidden or unavailable.",
                AuditEvidence(privilegedJustification),
                auditNotRecordedReason: null);
        }

        if (request.Scope.ConversationId is null)
        {
            IReadOnlyList<ConversationGovernanceVerificationCheckResultV1> checks = request.SelectedSuites
                .Select(suite => Check(
                    request.SchemaVersion,
                    suite,
                    "v1-scope-coverage",
                    ConversationGovernanceVerificationExecutionStatus.NotApplicable,
                    ConversationGovernanceVerificationFailureClassification.NotApplicable,
                    "Tenant wide verification is deferred for v1.",
                    ConversationGovernanceVerificationRemediation.None,
                    ["AC1"],
                    evidence: null))
                .ToArray();

            return BuildResult(
                request,
                generatedAtUtc,
                checks,
                AuditEvidence(privilegedJustification),
                localReadOnlyEvidenceOnly ? "Local read only proof did not touch tenant data." : null);
        }

        if (!localReadOnlyEvidenceOnly && !IsValidVerifyJustification(privilegedJustification, request.Scope))
        {
            return BuildSingleCheckResult(
                request,
                generatedAtUtc,
                ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden,
                ConversationGovernanceVerificationExecutionStatus.Blocked,
                "verify-justification",
                ConversationGovernanceVerificationRemediation.ProvideVerifyJustification,
                "Verify justification is required before tenant evidence is touched.",
                AuditEvidence(privilegedJustification),
                auditNotRecordedReason: null);
        }

        ConversationProjectionReadResult projection = await _projectionReadService
            .ReadDetailAsync(
                trustedTenantId,
                callerPrincipalId,
                request.Scope.TenantId,
                request.Scope.ConversationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (projection.Projection is null || !projection.IsAvailableForTrustBearingActions)
        {
            ConversationGovernanceVerificationFailureClassification classification = projection.FreshnessState == ProjectionTrustState.Forbidden
                ? ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden
                : projection.FreshnessState == ProjectionTrustState.Stale || projection.FreshnessState == ProjectionTrustState.Rebuilding
                    ? ConversationGovernanceVerificationFailureClassification.StaleProjection
                    : ConversationGovernanceVerificationFailureClassification.DependencyUnavailable;

            return BuildSingleCheckResult(
                request,
                generatedAtUtc,
                classification,
                ConversationGovernanceVerificationExecutionStatus.Blocked,
                "freshness-gate",
                classification == ConversationGovernanceVerificationFailureClassification.StaleProjection
                    ? ConversationGovernanceVerificationRemediation.RefreshDerivedEvidence
                    : ConversationGovernanceVerificationRemediation.RetryLater,
                "Current trusted read evidence is unavailable.",
                AuditEvidence(privilegedJustification),
                auditNotRecordedReason: null);
        }

        ConversationTemporalEventSourceResult source = await ReadTemporalSourceAsync(
            request.Scope.TenantId,
            request.Scope.ConversationId,
            cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ConversationGovernanceVerificationCheckResultV1> results = await Task.WhenAll(request.SelectedSuites
            .Select(suite => RunSuiteAsync(request.SchemaVersion, suite, projection.Projection, source, generatedAtUtc, cancellationToken)
                .AsTask()))
            .ConfigureAwait(false);

        return BuildResult(
            request,
            generatedAtUtc,
            results,
            AuditEvidence(privilegedJustification),
            localReadOnlyEvidenceOnly ? "Local read only proof did not touch tenant data." : null);
    }

    private ValueTask<ConversationGovernanceVerificationCheckResultV1> RunSuiteAsync(
        SchemaVersion schemaVersion,
        ConversationGovernanceVerificationSuite suite,
        ConversationDetailProjectionV1 projection,
        ConversationTemporalEventSourceResult source,
        DateTimeOffset generatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (suite == ConversationGovernanceVerificationSuite.AuditPairing)
        {
            return ValueTask.FromResult(VerifyAuditPairing(schemaVersion, projection));
        }

        if (suite == ConversationGovernanceVerificationSuite.TenantIsolation)
        {
            return ValueTask.FromResult(VerifyTenantIsolation(schemaVersion, projection, source));
        }

        if (suite == ConversationGovernanceVerificationSuite.RedactionReplay)
        {
            return ValueTask.FromResult(VerifyRedactionReplay(schemaVersion, projection, source));
        }

        if (suite == ConversationGovernanceVerificationSuite.ProjectionRebuild)
        {
            return VerifyProjectionRebuildAsync(schemaVersion, projection, source, generatedAtUtc, cancellationToken);
        }

        if (suite == ConversationGovernanceVerificationSuite.ProviderPortability)
        {
            return ValueTask.FromResult(VerifyProviderPortability(schemaVersion, projection));
        }

        return ValueTask.FromResult(VerifySchemaCompatibility(schemaVersion, projection, source));
    }

    private static ConversationGovernanceVerificationCheckResultV1 VerifyAuditPairing(
        SchemaVersion schemaVersion,
        ConversationDetailProjectionV1 projection)
    {
        bool hasGovernedState = projection.ActiveRetentionPolicy is not null
            || projection.SensitivityMarks.Count > 0
            || projection.Redactions.Count > 0;

        bool paired = (projection.ActiveRetentionPolicy is null || projection.ActiveRetentionPolicy.AuditEvidence is not null)
            && projection.SensitivityMarks.All(mark => mark.AuditEvidence is not null)
            && projection.Redactions.All(redaction => redaction.AuditEvidence is not null);

        return Check(
            schemaVersion,
            ConversationGovernanceVerificationSuite.AuditPairing,
            "audit-pairing",
            ConversationGovernanceVerificationExecutionStatus.Completed,
            paired ? ConversationGovernanceVerificationFailureClassification.Passed : ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            paired
                ? hasGovernedState ? "Governed state has paired audit references." : "No governed changes require pairing."
                : "Governed state is missing paired audit references.",
            paired
                ? ConversationGovernanceVerificationRemediation.None
                : ConversationGovernanceVerificationRemediation.InspectGovernanceEvidence,
            "AC1",
            "AC2",
            "AC3",
            "AC5");
    }

    private static ConversationGovernanceVerificationCheckResultV1 VerifyTenantIsolation(
        SchemaVersion schemaVersion,
        ConversationDetailProjectionV1 projection,
        ConversationTemporalEventSourceResult source)
    {
        if (source.State != ConversationTemporalEventSourceState.Available)
        {
            return SourceUnavailableCheck(schemaVersion, ConversationGovernanceVerificationSuite.TenantIsolation, "tenant-isolation", source);
        }

        bool matches = source.Events.All(e =>
        {
            ConversationEventMetadata? metadata = Metadata(e.Event);
            return metadata is not null
                && metadata.TenantId == projection.TenantId
                && metadata.ConversationId == projection.ConversationId;
        });

        return Check(
            schemaVersion,
            ConversationGovernanceVerificationSuite.TenantIsolation,
            "tenant-isolation",
            ConversationGovernanceVerificationExecutionStatus.Completed,
            matches ? ConversationGovernanceVerificationFailureClassification.Passed : ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            matches ? "Trusted scope matches derived and replayed records." : "Cross scope evidence was hidden.",
            matches ? ConversationGovernanceVerificationRemediation.None : ConversationGovernanceVerificationRemediation.RequestAuthorization,
            "AC1",
            "AC3",
            "AC5");
    }

    private static ConversationGovernanceVerificationCheckResultV1 VerifyRedactionReplay(
        SchemaVersion schemaVersion,
        ConversationDetailProjectionV1 projection,
        ConversationTemporalEventSourceResult source)
    {
        if (source.State != ConversationTemporalEventSourceState.Available)
        {
            return SourceUnavailableCheck(schemaVersion, ConversationGovernanceVerificationSuite.RedactionReplay, "redaction-replay", source);
        }

        ConversationReplayResult replay = ConversationReplayVerifier.Replay(projection.TenantId, projection.ConversationId, source.Events);
        if (replay.Outcome != ConversationReplayOutcome.Replay)
        {
            return ReplayFailureCheck(schemaVersion, ConversationGovernanceVerificationSuite.RedactionReplay, "redaction-replay", replay);
        }

        bool placeholderSafe = projection.Redactions.All(redaction =>
            redaction.Target.MessageId is null
            || projection.Messages
                .Where(message => message.MessageId == redaction.Target.MessageId)
                .All(message => string.Equals(message.Text, redaction.Placeholder, StringComparison.Ordinal)));

        return Check(
            schemaVersion,
            ConversationGovernanceVerificationSuite.RedactionReplay,
            "redaction-replay",
            ConversationGovernanceVerificationExecutionStatus.Completed,
            placeholderSafe ? ConversationGovernanceVerificationFailureClassification.Passed : ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            placeholderSafe ? "Redacted timeline entries remain placeholder safe." : "Redacted timeline entry was not placeholder safe.",
            placeholderSafe ? ConversationGovernanceVerificationRemediation.None : ConversationGovernanceVerificationRemediation.InspectGovernanceEvidence,
            "AC1",
            "AC3",
            "AC5");
    }

    private async ValueTask<ConversationGovernanceVerificationCheckResultV1> VerifyProjectionRebuildAsync(
        SchemaVersion schemaVersion,
        ConversationDetailProjectionV1 projection,
        ConversationTemporalEventSourceResult source,
        DateTimeOffset generatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (source.State != ConversationTemporalEventSourceState.Available || !source.IsComplete)
        {
            return SourceUnavailableCheck(schemaVersion, ConversationGovernanceVerificationSuite.ProjectionRebuild, "projection-rebuild", source);
        }

        ConversationProjectedReadModels? existing;
        try
        {
            existing = await _projectionReadStore
                .ReadAsync(projection.TenantId, projection.ConversationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Check(
                schemaVersion,
                ConversationGovernanceVerificationSuite.ProjectionRebuild,
                "projection-rebuild",
                ConversationGovernanceVerificationExecutionStatus.Failed,
                ConversationGovernanceVerificationFailureClassification.DependencyUnavailable,
                "Derived read evidence is unavailable.",
                ConversationGovernanceVerificationRemediation.RetryLater,
                "AC2",
                "AC5");
        }

        // The read store returns a misfiled record unvalidated so each caller can apply its own poison shape.
        // Without this guard a record belonging to another tenant would be compared against this conversation's
        // event history and produce a verification verdict derived from foreign data.
        if (existing is not null
            && (existing.Summary.TenantId != projection.TenantId
                || existing.Detail.TenantId != projection.TenantId
                || existing.Summary.ConversationId != projection.ConversationId
                || existing.Detail.ConversationId != projection.ConversationId))
        {
            return Check(
                schemaVersion,
                ConversationGovernanceVerificationSuite.ProjectionRebuild,
                "projection-rebuild",
                ConversationGovernanceVerificationExecutionStatus.Failed,
                ConversationGovernanceVerificationFailureClassification.DependencyUnavailable,
                "Derived read evidence does not belong to the verified conversation.",
                ConversationGovernanceVerificationRemediation.RetryLater,
                "AC2",
                "AC5");
        }

        ConversationProjectionRebuildResult result = _rebuildVerifier.Rebuild(
            projection.TenantId,
            projection.ConversationId,
            source.Events.Select(e => new ConversationProjectionEventRecord(e.Position, e.Event)),
            existing,
            generatedAtUtc,
            TimeSpan.FromMinutes(5),
            ["story-3.6-projection-rebuild"]);

        bool passed = result.Evidence.Passed && result.ExistingArtifactDisposition != ProjectionTrustState.Stale;
        return Check(
            schemaVersion,
            ConversationGovernanceVerificationSuite.ProjectionRebuild,
            "projection-rebuild",
            ConversationGovernanceVerificationExecutionStatus.Completed,
            passed ? ConversationGovernanceVerificationFailureClassification.Passed : ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            passed ? "Rebuilt derived state matches current read evidence." : "Rebuilt derived state disagrees with current read evidence.",
            passed ? ConversationGovernanceVerificationRemediation.None : ConversationGovernanceVerificationRemediation.RefreshDerivedEvidence,
            "AC1",
            "AC2",
            "AC5");
    }

    private static ConversationGovernanceVerificationCheckResultV1 VerifyProviderPortability(
        SchemaVersion schemaVersion,
        ConversationDetailProjectionV1 projection)
    {
        ProviderCorrelationMetadata? metadata = projection.ProviderCorrelation;
        bool portable = metadata is null
            || (!string.Equals(metadata.ProviderSessionReference, projection.ConversationId.Value, StringComparison.Ordinal)
                && !string.Equals(metadata.ProviderResponseReference, projection.ConversationId.Value, StringComparison.Ordinal));

        return Check(
            schemaVersion,
            ConversationGovernanceVerificationSuite.ProviderPortability,
            "provider-portability",
            ConversationGovernanceVerificationExecutionStatus.Completed,
            portable ? ConversationGovernanceVerificationFailureClassification.Passed : ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            portable ? "Provider correlation is metadata only." : "Provider correlation was treated as authority.",
            portable ? ConversationGovernanceVerificationRemediation.None : ConversationGovernanceVerificationRemediation.InspectGovernanceEvidence,
            "AC1",
            "AC2",
            "AC5");
    }

    private static ConversationGovernanceVerificationCheckResultV1 VerifySchemaCompatibility(
        SchemaVersion schemaVersion,
        ConversationDetailProjectionV1 projection,
        ConversationTemporalEventSourceResult source)
    {
        if (projection.SchemaVersion != SchemaVersion.Current)
        {
            return Check(
                schemaVersion,
                ConversationGovernanceVerificationSuite.SchemaCompatibility,
                "schema-compatibility",
                ConversationGovernanceVerificationExecutionStatus.Failed,
                ConversationGovernanceVerificationFailureClassification.UnsupportedVersion,
                "Unsupported contract version.",
                ConversationGovernanceVerificationRemediation.MigrateSchema,
                "AC2",
                "AC3",
                "AC5");
        }

        if (source.State != ConversationTemporalEventSourceState.Available)
        {
            return SourceUnavailableCheck(schemaVersion, ConversationGovernanceVerificationSuite.SchemaCompatibility, "schema-compatibility", source);
        }

        ConversationReplayResult replay = ConversationReplayVerifier.Replay(projection.TenantId, projection.ConversationId, source.Events);
        if (replay.Outcome != ConversationReplayOutcome.Replay)
        {
            return ReplayFailureCheck(schemaVersion, ConversationGovernanceVerificationSuite.SchemaCompatibility, "schema-compatibility", replay);
        }

        return Check(
            schemaVersion,
            ConversationGovernanceVerificationSuite.SchemaCompatibility,
            "schema-compatibility",
            ConversationGovernanceVerificationExecutionStatus.Completed,
            ConversationGovernanceVerificationFailureClassification.Passed,
            "Current contract version is supported.",
            ConversationGovernanceVerificationRemediation.None,
            "AC1",
            "AC2",
            "AC5");
    }

    private static ConversationGovernanceVerificationCheckResultV1 ReplayFailureCheck(
        SchemaVersion schemaVersion,
        ConversationGovernanceVerificationSuite suite,
        string checkName,
        ConversationReplayResult replay)
    {
        ConversationGovernanceVerificationFailureClassification classification = replay.DiagnosticCode == "unsupported_schema_version"
            ? ConversationGovernanceVerificationFailureClassification.UnsupportedVersion
            : replay.DiagnosticCode is "tenant_mismatch" or "conversation_mismatch"
                ? ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden
                : ConversationGovernanceVerificationFailureClassification.GovernanceFailed;

        return Check(
            schemaVersion,
            suite,
            checkName,
            ConversationGovernanceVerificationExecutionStatus.Failed,
            classification,
            classification == ConversationGovernanceVerificationFailureClassification.UnsupportedVersion
                ? "Unsupported contract version."
                : "Replay proof could not be trusted.",
            classification == ConversationGovernanceVerificationFailureClassification.UnsupportedVersion
                ? ConversationGovernanceVerificationRemediation.MigrateSchema
                : ConversationGovernanceVerificationRemediation.InspectGovernanceEvidence,
            "AC2",
            "AC3",
            "AC5");
    }

    private static ConversationGovernanceVerificationCheckResultV1 SourceUnavailableCheck(
        SchemaVersion schemaVersion,
        ConversationGovernanceVerificationSuite suite,
        string checkName,
        ConversationTemporalEventSourceResult source)
    {
        ConversationGovernanceVerificationFailureClassification classification = source.State switch
        {
            ConversationTemporalEventSourceState.Rebuilding => ConversationGovernanceVerificationFailureClassification.StaleProjection,
            ConversationTemporalEventSourceState.OutsideCoverage => ConversationGovernanceVerificationFailureClassification.DataUnavailable,
            _ => ConversationGovernanceVerificationFailureClassification.DependencyUnavailable,
        };

        ConversationGovernanceVerificationRemediation remediation =
            classification == ConversationGovernanceVerificationFailureClassification.StaleProjection
                ? ConversationGovernanceVerificationRemediation.RefreshDerivedEvidence
                : ConversationGovernanceVerificationRemediation.RetryLater;

        return Check(
            schemaVersion,
            suite,
            checkName,
            ConversationGovernanceVerificationExecutionStatus.Failed,
            classification,
            "Replay proof is unavailable.",
            remediation,
            "AC2",
            "AC3",
            "AC5");
    }

    private static bool IsValidVerifyJustification(
        PrivilegedOperationalJustificationDetailsV1? justification,
        ConversationGovernanceVerificationScopeV1 scope)
        => justification is not null
            && justification.OperationClass == PrivilegedOperationalActionClass.Verify
            && justification.Outcome == GovernanceOutcome.Succeeded
            && justification.VisibilityState == ProjectionTrustState.Current
            && justification.Freshness.AllowsTrustBearingDecision()
            && justification.TenantId == scope.TenantId
            && (scope.ConversationId is null || justification.ConversationId == scope.ConversationId);

    private static GovernanceAuditEvidenceReference? AuditEvidence(PrivilegedOperationalJustificationDetailsV1? justification)
        => justification?.AuditEvidence;

    private static ConversationGovernanceVerificationCheckResultV1 Check(
        SchemaVersion schemaVersion,
        ConversationGovernanceVerificationSuite suite,
        string checkName,
        ConversationGovernanceVerificationExecutionStatus status,
        ConversationGovernanceVerificationFailureClassification classification,
        string detail,
        ConversationGovernanceVerificationRemediation remediation,
        params string[] requirementMappings)
        => Check(schemaVersion, suite, checkName, status, classification, detail, remediation, requirementMappings, LocalEvidence);

    private static ConversationGovernanceVerificationCheckResultV1 Check(
        SchemaVersion schemaVersion,
        ConversationGovernanceVerificationSuite suite,
        string checkName,
        ConversationGovernanceVerificationExecutionStatus status,
        ConversationGovernanceVerificationFailureClassification classification,
        string detail,
        ConversationGovernanceVerificationRemediation remediation,
        IReadOnlyList<string> requirementMappings,
        ConversationGovernanceVerificationEvidenceHandle? evidence)
        => new(
            schemaVersion,
            suite,
            checkName,
            requirementMappings,
            status,
            classification,
            detail,
            remediation,
            evidence);

    private static ConversationGovernanceVerificationRunResultV1 BuildSingleCheckResult(
        ConversationGovernanceVerificationRequestV1 request,
        DateTimeOffset generatedAtUtc,
        ConversationGovernanceVerificationFailureClassification classification,
        ConversationGovernanceVerificationExecutionStatus status,
        string checkName,
        ConversationGovernanceVerificationRemediation remediation,
        string detail,
        GovernanceAuditEvidenceReference? auditEvidence,
        string? auditNotRecordedReason)
    {
        ConversationGovernanceVerificationCheckResultV1 check = Check(
            request.SchemaVersion,
            request.SelectedSuites[0],
            checkName,
            status,
            classification,
            detail,
            remediation,
            ["AC1", "AC2", "AC3", "AC4", "AC5"],
            evidence: null);

        return new(
            request.SchemaVersion,
            request.Scope,
            request.SelectedSuites,
            generatedAtUtc,
            request.CorrelationId,
            status,
            classification,
            detail,
            [check],
            auditEvidence,
            auditNotRecordedReason);
    }

    private static ConversationGovernanceVerificationRunResultV1 BuildResult(
        ConversationGovernanceVerificationRequestV1 request,
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<ConversationGovernanceVerificationCheckResultV1> checks,
        GovernanceAuditEvidenceReference? auditEvidence,
        string? auditNotRecordedReason)
    {
        ConversationGovernanceVerificationFailureClassification classification = OverallClassification(checks);
        ConversationGovernanceVerificationExecutionStatus status = classification == ConversationGovernanceVerificationFailureClassification.Passed
            || classification == ConversationGovernanceVerificationFailureClassification.NotApplicable
            ? ConversationGovernanceVerificationExecutionStatus.Completed
            : checks.Any(check => check.Classification == ConversationGovernanceVerificationFailureClassification.Passed)
                ? ConversationGovernanceVerificationExecutionStatus.Partial
                : ConversationGovernanceVerificationExecutionStatus.Failed;

        return new(
            request.SchemaVersion,
            request.Scope,
            request.SelectedSuites,
            generatedAtUtc,
            request.CorrelationId,
            status,
            classification,
            SummaryFor(classification),
            checks,
            auditEvidence,
            auditNotRecordedReason);
    }

    private static ConversationGovernanceVerificationFailureClassification OverallClassification(
        IReadOnlyList<ConversationGovernanceVerificationCheckResultV1> checks)
    {
        ConversationGovernanceVerificationFailureClassification[] priority =
        [
            ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            ConversationGovernanceVerificationFailureClassification.UnsupportedVersion,
            ConversationGovernanceVerificationFailureClassification.StaleProjection,
            ConversationGovernanceVerificationFailureClassification.DependencyUnavailable,
            ConversationGovernanceVerificationFailureClassification.InfrastructureFailed,
            ConversationGovernanceVerificationFailureClassification.DataUnavailable,
            ConversationGovernanceVerificationFailureClassification.ExecutionFailed,
            ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden,
            ConversationGovernanceVerificationFailureClassification.NotApplicable,
            ConversationGovernanceVerificationFailureClassification.Passed,
        ];

        return priority.First(classification => checks.Any(check => check.Classification == classification));
    }

    private static string SummaryFor(ConversationGovernanceVerificationFailureClassification classification)
        => classification == ConversationGovernanceVerificationFailureClassification.Passed
            ? "Governance verification passed."
            : classification == ConversationGovernanceVerificationFailureClassification.NotApplicable
                ? "Governance verification is not applicable for this scope."
                : "Governance verification did not pass.";

    private static async ValueTask<ConversationTemporalEventSourceResult> ReadTemporalSourceAsync(
        IConversationTemporalEventSource eventSource,
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await eventSource.ReadAsync(tenantId, conversationId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ConversationTemporalEventSourceResult.Unavailable();
        }
    }

    private ValueTask<ConversationTemporalEventSourceResult> ReadTemporalSourceAsync(
        TenantId tenantId,
        ConversationId conversationId,
        CancellationToken cancellationToken)
        => ReadTemporalSourceAsync(_eventSource, tenantId, conversationId, cancellationToken);

    private static ConversationEventMetadata? Metadata(object e)
        => e switch
        {
            ConversationCreatedDomainEvent created => created.Metadata,
            ParticipantAddedDomainEvent participant => participant.Metadata,
            RetentionPolicySetDomainEvent retentionSet => retentionSet.Metadata,
            RetentionPolicyReplacedDomainEvent retentionReplaced => retentionReplaced.Metadata,
            ConversationContentMarkedSensitiveDomainEvent sensitive => sensitive.Metadata,
            MessageContentRedactedDomainEvent redacted => redacted.Metadata,
            ConversationCreated created => created.Metadata,
            ParticipantAdded participant => participant.Metadata,
            RetentionPolicySet retentionSet => retentionSet.Metadata,
            RetentionPolicyReplaced retentionReplaced => retentionReplaced.Metadata,
            ConversationContentMarkedSensitive sensitive => sensitive.Metadata,
            MessageContentRedacted redacted => redacted.Metadata,
            MessageAppended message => message.Metadata,
            FileReferenceAttached file => file.Metadata,
            ConversationMetadataUpdated update => update.Metadata,
            ConversationClosed closed => closed.Metadata,
            ConversationArchived archived => archived.Metadata,
            ConversationLifecycleChanged lifecycle => lifecycle.Metadata,
            _ => null,
        };
}
