// <copyright file="ReleaseGateStatus.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Defines the closed release-gate status vocabulary for release gating decisions.
/// </summary>
/// <remarks>
/// Each value is a stable machine identifier aligned to FR86 gate classification requirements.
/// Values are intentionally distinct from <see cref="ConformanceOutcome"/> to avoid ambiguity between
/// the adopter-layer check vocabulary and the release-owner gate aggregation layer.
/// </remarks>
[JsonConverter(typeof(ReleaseGateStatusJsonConverter))]
public sealed record ReleaseGateStatus
{
    /// <summary>
    /// Gets the pass status (all evidence for this gate is satisfactory).
    /// </summary>
    public static ReleaseGateStatus Pass { get; } = new("pass");

    /// <summary>
    /// Gets the fail status (a blocking evidence deficit was observed for this gate).
    /// </summary>
    public static ReleaseGateStatus Fail { get; } = new("fail");

    /// <summary>
    /// Gets the waived status (a named waiver explicitly covers this gate; see Story 5.4).
    /// </summary>
    public static ReleaseGateStatus Waived { get; } = new("waived");

    /// <summary>
    /// Gets the unknown-accepted status (evidence is partial or deferred but has been accepted for this release).
    /// </summary>
    public static ReleaseGateStatus UnknownAccepted { get; } = new("unknown-accepted");

    private static readonly IReadOnlyDictionary<string, ReleaseGateStatus> KnownValues = Known(
        Pass,
        Fail,
        Waived,
        UnknownAccepted);

    private ReleaseGateStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported release-gate status in canonical order.
    /// </summary>
    public static IReadOnlyList<ReleaseGateStatus> All { get; } =
    [
        Pass,
        Fail,
        Waived,
        UnknownAccepted,
    ];

    /// <summary>
    /// Gets a value indicating whether this status is a release blocker.
    /// </summary>
    /// <remarks>
    /// Only <see cref="Fail"/> is a blocker; <see cref="Waived"/> and <see cref="UnknownAccepted"/>
    /// are not blockers because they represent accepted deviations through explicit review or deferred evidence.
    /// </remarks>
    public bool IsBlocking => Equals(Fail);

    /// <summary>
    /// Resolves a supported release-gate status.
    /// </summary>
    /// <param name="value">The canonical status value.</param>
    /// <returns>The matching release-gate status.</returns>
    public static ReleaseGateStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ReleaseGateStatus));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Defines the closed release-gate identifier vocabulary covering the seven required gate identifiers.
/// </summary>
/// <remarks>
/// These seven identifiers align with FR87-FR90 and FR86 gate classification requirements. Each identifier
/// maps to a specific conformance domain that must be evidenced before a release is gated.
/// </remarks>
[JsonConverter(typeof(ReleaseGateIdJsonConverter))]
public sealed record ReleaseGateId
{
    /// <summary>
    /// Gets the tenant-isolation gate identifier.
    /// </summary>
    public static ReleaseGateId TenantIsolation { get; } = new("tenant-isolation");

    /// <summary>
    /// Gets the audit-integrity gate identifier.
    /// </summary>
    public static ReleaseGateId AuditIntegrity { get; } = new("audit-integrity");

    /// <summary>
    /// Gets the redaction-non-leakage gate identifier.
    /// </summary>
    public static ReleaseGateId RedactionNonLeakage { get; } = new("redaction-non-leakage");

    /// <summary>
    /// Gets the unsupported-schema-rejection gate identifier.
    /// </summary>
    public static ReleaseGateId UnsupportedSchemaRejection { get; } = new("unsupported-schema-rejection");

    /// <summary>
    /// Gets the projection-rebuild-determinism gate identifier.
    /// </summary>
    public static ReleaseGateId ProjectionRebuildDeterminism { get; } = new("projection-rebuild-determinism");

    /// <summary>
    /// Gets the contract-compatibility gate identifier.
    /// </summary>
    public static ReleaseGateId ContractCompatibility { get; } = new("contract-compatibility");

    /// <summary>
    /// Gets the provider-portability gate identifier.
    /// </summary>
    public static ReleaseGateId ProviderPortability { get; } = new("provider-portability");

    private static readonly IReadOnlyDictionary<string, ReleaseGateId> KnownValues = Known(
        TenantIsolation,
        AuditIntegrity,
        RedactionNonLeakage,
        UnsupportedSchemaRejection,
        ProjectionRebuildDeterminism,
        ContractCompatibility,
        ProviderPortability);

    private ReleaseGateId(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported release-gate identifier in canonical order.
    /// </summary>
    public static IReadOnlyList<ReleaseGateId> All { get; } =
    [
        TenantIsolation,
        AuditIntegrity,
        RedactionNonLeakage,
        UnsupportedSchemaRejection,
        ProjectionRebuildDeterminism,
        ContractCompatibility,
        ProviderPortability,
    ];

    /// <summary>
    /// Resolves a supported release-gate identifier.
    /// </summary>
    /// <param name="value">The canonical gate identifier value.</param>
    /// <returns>The matching release-gate identifier.</returns>
    public static ReleaseGateId Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ReleaseGateId));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Carries the content-safe machine-readable result of one release-gate evaluation.
/// </summary>
/// <param name="gateId">The closed-vocabulary release-gate identifier.</param>
/// <param name="status">The closed-vocabulary release-gate status.</param>
/// <param name="safeEvidenceSummary">The bounded content-safe human-readable evidence summary.</param>
/// <param name="evidenceHandle">The bounded machine-readable handle for the evidence artifact.</param>
/// <param name="evaluatedAtUtc">The UTC timestamp when the gate was evaluated.</param>
/// <param name="requirementId">The bounded machine-readable requirement identifier (e.g. FR86).</param>
/// <param name="waiverReference">The optional bounded waiver reference (used only when status is waived or unknown-accepted).</param>
public sealed record ReleaseGateResultV1(
    ReleaseGateId GateId,
    ReleaseGateStatus Status,
    string SafeEvidenceSummary,
    string EvidenceHandle,
    DateTimeOffset EvaluatedAtUtc,
    string RequirementId,
    string? WaiverReference = null)
{
    /// <summary>
    /// Gets the closed-vocabulary release-gate identifier.
    /// </summary>
    public ReleaseGateId GateId { get; } = GateId ?? throw new ArgumentNullException(nameof(GateId));

    /// <summary>
    /// Gets the closed-vocabulary release-gate status.
    /// </summary>
    public ReleaseGateStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    /// <summary>
    /// Gets the bounded content-safe human-readable evidence summary.
    /// </summary>
    public string SafeEvidenceSummary { get; } = ConformanceContractValidation.RequiredSafeText(SafeEvidenceSummary, nameof(SafeEvidenceSummary));

    /// <summary>
    /// Gets the bounded machine-readable handle for the evidence artifact.
    /// </summary>
    public string EvidenceHandle { get; } = ConformanceContractValidation.RequiredSafeToken(EvidenceHandle, nameof(EvidenceHandle));

    /// <summary>
    /// Gets the UTC timestamp when the gate was evaluated.
    /// </summary>
    public DateTimeOffset EvaluatedAtUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(EvaluatedAtUtc, nameof(EvaluatedAtUtc));

    /// <summary>
    /// Gets the bounded machine-readable requirement identifier (e.g. FR86, FR87).
    /// </summary>
    public string RequirementId { get; } = ConformanceContractValidation.RequiredSafeToken(RequirementId, nameof(RequirementId));

    /// <summary>
    /// Gets the optional bounded waiver reference used only when status is waived or unknown-accepted.
    /// </summary>
    public string? WaiverReference { get; } = ConformanceContractValidation.OptionalSafeToken(WaiverReference, nameof(WaiverReference));
}
