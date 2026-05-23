// <copyright file="ReleaseConformanceArtifactV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Carries the content-safe, machine-readable, signed release conformance artifact for release gating.
/// </summary>
/// <remarks>
/// This artifact is the FR82 release-owner evidence type. It captures all required evidence fields and
/// provides a deterministic <see cref="OverallStatus"/> computed from the seven <see cref="ReleaseGateId"/>
/// gate results. "Signed" means runner-attested via <see cref="SignerOrRunnerId"/> — not a PKI/cryptographic
/// signature, which would be an ADR-triggering infrastructure decision.
/// </remarks>
/// <param name="schemaVersion">The artifact schema version.</param>
/// <param name="buildHash">The bounded machine-readable build hash for this release candidate.</param>
/// <param name="signerOrRunnerId">The bounded machine-readable identity of the signer or runner that produced the artifact.</param>
/// <param name="testEnvironmentId">The bounded machine-readable test environment identity.</param>
/// <param name="datasetScale">The bounded machine-readable dataset scale descriptor.</param>
/// <param name="toolVersions">The bounded content-safe tool versions string.</param>
/// <param name="releaseManifestReference">The bounded machine-readable reference to the release manifest.</param>
/// <param name="generatedAtUtc">The UTC timestamp when this artifact was generated.</param>
/// <param name="eventSchemaVersions">The list of event schema versions included in this release.</param>
/// <param name="contractPackageVersions">The list of contract package versions included in this release.</param>
/// <param name="evidenceLinks">The list of bounded evidence artifact handles.</param>
/// <param name="gateResults">The per-gate results covering all seven required <see cref="ReleaseGateId"/> values.</param>
public sealed record ReleaseConformanceArtifactV1(
    SchemaVersion SchemaVersion,
    string BuildHash,
    string SignerOrRunnerId,
    string TestEnvironmentId,
    string DatasetScale,
    string ToolVersions,
    string ReleaseManifestReference,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<SchemaVersion> EventSchemaVersions,
    IReadOnlyList<string> ContractPackageVersions,
    IReadOnlyList<string> EvidenceLinks,
    IReadOnlyList<ReleaseGateResultV1> GateResults)
{
    /// <summary>
    /// Gets the artifact schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the bounded machine-readable build hash.
    /// </summary>
    public string BuildHash { get; } = ConformanceContractValidation.RequiredSafeToken(BuildHash, nameof(BuildHash));

    /// <summary>
    /// Gets the bounded machine-readable identity of the signer or runner.
    /// </summary>
    public string SignerOrRunnerId { get; } = ConformanceContractValidation.RequiredSafeToken(SignerOrRunnerId, nameof(SignerOrRunnerId));

    /// <summary>
    /// Gets the bounded machine-readable test environment identity.
    /// </summary>
    public string TestEnvironmentId { get; } = ConformanceContractValidation.RequiredSafeToken(TestEnvironmentId, nameof(TestEnvironmentId));

    /// <summary>
    /// Gets the bounded machine-readable dataset scale descriptor.
    /// </summary>
    public string DatasetScale { get; } = ConformanceContractValidation.RequiredSafeToken(DatasetScale, nameof(DatasetScale));

    /// <summary>
    /// Gets the bounded content-safe tool versions string.
    /// </summary>
    public string ToolVersions { get; } = ConformanceContractValidation.RequiredSafeText(ToolVersions, nameof(ToolVersions));

    /// <summary>
    /// Gets the bounded machine-readable reference to the release manifest.
    /// </summary>
    public string ReleaseManifestReference { get; } = ConformanceContractValidation.RequiredSafeToken(ReleaseManifestReference, nameof(ReleaseManifestReference));

    /// <summary>
    /// Gets the UTC timestamp when this artifact was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(GeneratedAtUtc, nameof(GeneratedAtUtc));

    /// <summary>
    /// Gets the list of event schema versions included in this release.
    /// </summary>
    public IReadOnlyList<SchemaVersion> EventSchemaVersions { get; } = ValidateSchemaVersionList(EventSchemaVersions, nameof(EventSchemaVersions));

    /// <summary>
    /// Gets the list of contract package version tokens included in this release.
    /// </summary>
    public IReadOnlyList<string> ContractPackageVersions { get; } = ValidateTokenList(ContractPackageVersions, nameof(ContractPackageVersions));

    /// <summary>
    /// Gets the list of bounded evidence artifact handles.
    /// </summary>
    public IReadOnlyList<string> EvidenceLinks { get; } = ValidateTokenList(EvidenceLinks, nameof(EvidenceLinks));

    /// <summary>
    /// Gets the per-gate results covering all seven required <see cref="ReleaseGateId"/> values.
    /// </summary>
    public IReadOnlyList<ReleaseGateResultV1> GateResults { get; } = ValidateGateResults(GateResults);

    /// <summary>
    /// Gets the deterministic computed overall release status from the gate results.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>fail</c> if any gate result is <c>fail</c>.</description></item>
    /// <item><description><c>pass</c> if all gate results are <c>pass</c>.</description></item>
    /// <item><description><c>waived</c> if no gate is <c>fail</c> and at least one is <c>waived</c>.</description></item>
    /// <item><description><c>unknown-accepted</c> otherwise.</description></item>
    /// </list>
    /// This is a deterministic computation, not a settable field, so it cannot be forged.
    /// </remarks>
    public ReleaseGateStatus OverallStatus
    {
        get
        {
            if (GateResults.Any(g => g.Status.Equals(ReleaseGateStatus.Fail)))
            {
                return ReleaseGateStatus.Fail;
            }

            if (GateResults.All(g => g.Status.Equals(ReleaseGateStatus.Pass)))
            {
                return ReleaseGateStatus.Pass;
            }

            if (GateResults.Any(g => g.Status.Equals(ReleaseGateStatus.Waived)))
            {
                return ReleaseGateStatus.Waived;
            }

            return ReleaseGateStatus.UnknownAccepted;
        }
    }

    /// <summary>
    /// Validates the artifact and returns typed diagnostic reasons for any violations.
    /// </summary>
    /// <param name="artifact">The artifact to validate.</param>
    /// <returns>A read-only list of content-safe token error reasons; empty when the artifact is valid.</returns>
    public static IReadOnlyList<string> ValidateArtifact(ReleaseConformanceArtifactV1 artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(artifact.SignerOrRunnerId))
        {
            errors.Add("missing-signer-runner-identity");
        }

        if (string.IsNullOrWhiteSpace(artifact.BuildHash))
        {
            errors.Add("missing-build-hash");
        }

        IReadOnlyList<ReleaseGateId> allGates = ReleaseGateId.All;
        foreach (ReleaseGateId gate in allGates)
        {
            if (!artifact.GateResults.Any(g => g.GateId.Equals(gate)))
            {
                errors.Add($"missing-gate-{gate.Value}");
            }
        }

        foreach (ReleaseGateResultV1 result in artifact.GateResults)
        {
            if (result.Status.Equals(ReleaseGateStatus.Fail)
                && string.IsNullOrWhiteSpace(result.EvidenceHandle))
            {
                errors.Add($"fail-gate-missing-evidence-{result.GateId.Value}");
            }
        }

        // Contradictory: no blocker gate is fail but OverallStatus somehow implies fail
        bool anyBlockerFail = artifact.GateResults.Any(g => g.Status.IsBlocking);
        if (!anyBlockerFail && artifact.OverallStatus.IsBlocking)
        {
            errors.Add("contradictory-overall-status");
        }

        return errors;
    }

    private static IReadOnlyList<SchemaVersion> ValidateSchemaVersionList(IReadOnlyList<SchemaVersion>? values, string parameterName)
    {
        if (values is null || values.Count == 0 || values.Any(v => v is null))
        {
            throw new ArgumentException("At least one schema version is required with no null entries.", parameterName);
        }

        return values.ToArray();
    }

    private static IReadOnlyList<string> ValidateTokenList(IReadOnlyList<string>? values, string parameterName)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-empty token is required.", parameterName);
        }

        return values.Select(v => ConformanceContractValidation.RequiredSafeToken(v, parameterName)).ToArray();
    }

    private static IReadOnlyList<ReleaseGateResultV1> ValidateGateResults(IReadOnlyList<ReleaseGateResultV1>? values)
    {
        if (values is null || values.Count == 0 || values.Any(v => v is null))
        {
            throw new ArgumentException("At least one gate result is required with no null entries.", nameof(values));
        }

        IReadOnlyList<ReleaseGateId> allGates = ReleaseGateId.All;
        foreach (ReleaseGateId gate in allGates)
        {
            if (!values.Any(g => g.GateId.Equals(gate)))
            {
                throw new ArgumentException($"Gate result for '{gate.Value}' is required.", nameof(values));
            }
        }

        return values.ToArray();
    }
}
