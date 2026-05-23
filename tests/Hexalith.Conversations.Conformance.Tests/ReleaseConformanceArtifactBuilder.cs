// <copyright file="ReleaseConformanceArtifactBuilder.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Builds a deterministic <see cref="ReleaseConformanceArtifactV1"/> from adopter-suite evidence and environment metadata.
/// </summary>
/// <remarks>
/// The builder maps per-check outcomes from <see cref="ConformanceRunResultV1"/> to release gate IDs using
/// the conservative gate-to-check mapping table from Story 5.2 Dev Notes:
/// <list type="table">
/// <item><term>tenant-isolation</term><description>tenant-binding check</description></item>
/// <item><term>audit-integrity</term><description>governance-precondition check</description></item>
/// <item><term>redaction-non-leakage</term><description>governance-precondition (partial)</description></item>
/// <item><term>unsupported-schema-rejection</term><description>compatibility-discovery check</description></item>
/// <item><term>projection-rebuild-determinism</term><description>projection-freshness check</description></item>
/// <item><term>contract-compatibility</term><description>compatibility-discovery + error-envelope checks</description></item>
/// <item><term>provider-portability</term><description>no direct mapping → unknown-accepted</description></item>
/// </list>
/// A gate status is <c>pass</c> when the mapped check outcome is <c>ready</c>; <c>fail</c> when the mapped
/// check outcome is <c>blocked</c> with a non-conformant failure classification (a true product defect);
/// <c>unknown-accepted</c> in all other cases (partial coverage, deferred suites, or conformant non-ready outcomes).
/// </remarks>
public sealed class ReleaseConformanceArtifactBuilder
{
    private readonly ConformanceRunResultV1 _runResult;
    private readonly string _buildHash;
    private readonly string _signerOrRunnerId;
    private readonly string _testEnvironmentId;
    private readonly string _datasetScale;
    private readonly string _toolVersions;
    private readonly string _releaseManifestReference;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseConformanceArtifactBuilder"/> class.
    /// </summary>
    /// <param name="runResult">The adopter conformance suite run result (Story 4.5 evidence).</param>
    /// <param name="buildHash">The bounded machine-readable build hash for this release candidate.</param>
    /// <param name="signerOrRunnerId">The bounded machine-readable signer or runner identity.</param>
    /// <param name="testEnvironmentId">The bounded machine-readable test environment identity.</param>
    /// <param name="datasetScale">The bounded machine-readable dataset scale descriptor.</param>
    /// <param name="toolVersions">The bounded content-safe tool versions string.</param>
    /// <param name="releaseManifestReference">The bounded machine-readable release manifest reference.</param>
    /// <param name="timeProvider">The time provider for deterministic timestamp generation.</param>
    public ReleaseConformanceArtifactBuilder(
        ConformanceRunResultV1 runResult,
        string buildHash,
        string signerOrRunnerId,
        string testEnvironmentId,
        string datasetScale,
        string toolVersions,
        string releaseManifestReference,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(buildHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(signerOrRunnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(testEnvironmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetScale);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolVersions);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseManifestReference);

        _runResult = runResult;
        _buildHash = buildHash;
        _signerOrRunnerId = signerOrRunnerId;
        _testEnvironmentId = testEnvironmentId;
        _datasetScale = datasetScale;
        _toolVersions = toolVersions;
        _releaseManifestReference = releaseManifestReference;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Builds the release conformance artifact deterministically from the provided inputs.
    /// </summary>
    /// <returns>The validated <see cref="ReleaseConformanceArtifactV1"/>.</returns>
    public ReleaseConformanceArtifactV1 Build()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        ConformanceCheckResultV1? tenantBinding = GetCheck(ConformanceCheck.TenantBinding);
        ConformanceCheckResultV1? governancePrecondition = GetCheck(ConformanceCheck.GovernancePrecondition);
        ConformanceCheckResultV1? compatibilityDiscovery = GetCheck(ConformanceCheck.CompatibilityDiscovery);
        ConformanceCheckResultV1? projectionFreshness = GetCheck(ConformanceCheck.ProjectionFreshness);
        ConformanceCheckResultV1? errorEnvelope = GetCheck(ConformanceCheck.ErrorEnvelope);

        ReleaseGateResultV1[] gateResults =
        [
            MappedGate(ReleaseGateId.TenantIsolation, "FR86", now, "isolation-gate-evidence", tenantBinding),
            MappedGate(ReleaseGateId.AuditIntegrity, "FR86", now, "audit-integrity-evidence", governancePrecondition),
            PartialGate(ReleaseGateId.RedactionNonLeakage, "FR86", now, "redaction-non-leakage-stub", governancePrecondition),
            MappedGate(ReleaseGateId.UnsupportedSchemaRejection, "FR86", now, "unsupported-schema-rejection-evidence", compatibilityDiscovery),
            MappedGate(ReleaseGateId.ProjectionRebuildDeterminism, "FR86", now, "projection-rebuild-determinism-evidence", projectionFreshness),
            MultiGate(ReleaseGateId.ContractCompatibility, "FR86", now, "contract-compatibility-evidence", compatibilityDiscovery, errorEnvelope),
            UnknownAcceptedGate(ReleaseGateId.ProviderPortability, "FR86", now, "provider-portability-stub"),
        ];

        return new ReleaseConformanceArtifactV1(
            SchemaVersion.Current,
            _buildHash,
            _signerOrRunnerId,
            _testEnvironmentId,
            _datasetScale,
            _toolVersions,
            _releaseManifestReference,
            now,
            [SchemaVersion.Current],
            ["hexalith-conversations-contracts-1.0.0"],
            [_runResult.CorrelationId],
            gateResults);
    }

    private ConformanceCheckResultV1? GetCheck(ConformanceCheck check)
        => _runResult.Checks.FirstOrDefault(c => c.Check.Equals(check));

    // Maps a single check to a gate: ready → pass; blocked+non-conformant → fail; otherwise → unknown-accepted.
    private static ReleaseGateResultV1 MappedGate(
        ReleaseGateId gateId,
        string requirementId,
        DateTimeOffset evaluatedAt,
        string evidenceHandle,
        ConformanceCheckResultV1? check)
    {
        ReleaseGateStatus status = MapCheckToGateStatus(check);
        string summary = status.Equals(ReleaseGateStatus.Pass)
            ? "Adopter suite check passed; CORE fixture evidence verified."
            : status.Equals(ReleaseGateStatus.Fail)
                ? "Adopter suite check failed; product-invariant defect observed."
                : "Partial adopter suite coverage; full proof suite deferred to a later story.";

        return new ReleaseGateResultV1(gateId, status, summary, evidenceHandle, evaluatedAt, requirementId);
    }

    // For partial-coverage gates: only fail on non-conformant blocked; never promote to pass (coverage is partial).
    private static ReleaseGateResultV1 PartialGate(
        ReleaseGateId gateId,
        string requirementId,
        DateTimeOffset evaluatedAt,
        string evidenceHandle,
        ConformanceCheckResultV1? check)
    {
        ReleaseGateStatus status = IsNonConformantBlocked(check) ? ReleaseGateStatus.Fail : ReleaseGateStatus.UnknownAccepted;
        string summary = status.Equals(ReleaseGateStatus.Fail)
            ? "Adopter suite check failed; product-invariant defect observed."
            : "Partial adopter suite coverage; full proof suite deferred to a later story.";

        return new ReleaseGateResultV1(gateId, status, summary, evidenceHandle, evaluatedAt, requirementId);
    }

    // Maps two checks to one gate: fail if any non-conformant-blocked; pass if both ready; otherwise unknown-accepted.
    private static ReleaseGateResultV1 MultiGate(
        ReleaseGateId gateId,
        string requirementId,
        DateTimeOffset evaluatedAt,
        string evidenceHandle,
        ConformanceCheckResultV1? primary,
        ConformanceCheckResultV1? secondary)
    {
        bool anyFail = IsNonConformantBlocked(primary) || IsNonConformantBlocked(secondary);
        bool allReady = IsReady(primary) && IsReady(secondary);

        ReleaseGateStatus status = anyFail ? ReleaseGateStatus.Fail
            : allReady ? ReleaseGateStatus.Pass
            : ReleaseGateStatus.UnknownAccepted;

        string summary = status.Equals(ReleaseGateStatus.Pass)
            ? "Adopter suite checks passed; CORE fixture evidence verified."
            : status.Equals(ReleaseGateStatus.Fail)
                ? "Adopter suite check failed; product-invariant defect observed."
                : "Partial adopter suite coverage; full manifest coverage deferred to a later story.";

        return new ReleaseGateResultV1(gateId, status, summary, evidenceHandle, evaluatedAt, requirementId);
    }

    private static ReleaseGateResultV1 UnknownAcceptedGate(
        ReleaseGateId gateId,
        string requirementId,
        DateTimeOffset evaluatedAt,
        string evidenceHandle)
        => new(
            gateId,
            ReleaseGateStatus.UnknownAccepted,
            "No direct adopter suite coverage; full proof suite deferred to a later story.",
            evidenceHandle,
            evaluatedAt,
            requirementId);

    // ready → pass; blocked+non-conformant → fail; everything else → unknown-accepted
    private static ReleaseGateStatus MapCheckToGateStatus(ConformanceCheckResultV1? check)
    {
        if (check is null)
        {
            return ReleaseGateStatus.UnknownAccepted;
        }

        if (check.Outcome.Equals(ConformanceOutcome.Ready))
        {
            return ReleaseGateStatus.Pass;
        }

        if (check.Outcome.Equals(ConformanceOutcome.Blocked) && !check.IsConformant)
        {
            return ReleaseGateStatus.Fail;
        }

        return ReleaseGateStatus.UnknownAccepted;
    }

    private static bool IsNonConformantBlocked(ConformanceCheckResultV1? check)
        => check is not null && check.Outcome.Equals(ConformanceOutcome.Blocked) && !check.IsConformant;

    private static bool IsReady(ConformanceCheckResultV1? check)
        => check is not null && check.Outcome.Equals(ConformanceOutcome.Ready);
}
