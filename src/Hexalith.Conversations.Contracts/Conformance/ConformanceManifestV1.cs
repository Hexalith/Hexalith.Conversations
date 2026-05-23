// <copyright file="ConformanceManifestV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Contracts.Versioning;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Defines the closed lifecycle-stage vocabulary for conformance manifest entries (NFR1).
/// </summary>
/// <remarks>
/// Exactly six values aligned to NFR1: design-review, automated-test, load-performance-test,
/// operational-drill, release-evidence, accessibility-validation.
/// </remarks>
[JsonConverter(typeof(ConformanceManifestLifecycleStageJsonConverter))]
public sealed record ConformanceManifestLifecycleStage
{
    /// <summary>
    /// Gets the design-review lifecycle stage.
    /// </summary>
    public static ConformanceManifestLifecycleStage DesignReview { get; } = new("design-review");

    /// <summary>
    /// Gets the automated-test lifecycle stage.
    /// </summary>
    public static ConformanceManifestLifecycleStage AutomatedTest { get; } = new("automated-test");

    /// <summary>
    /// Gets the load-performance-test lifecycle stage.
    /// </summary>
    public static ConformanceManifestLifecycleStage LoadPerformanceTest { get; } = new("load-performance-test");

    /// <summary>
    /// Gets the operational-drill lifecycle stage.
    /// </summary>
    public static ConformanceManifestLifecycleStage OperationalDrill { get; } = new("operational-drill");

    /// <summary>
    /// Gets the release-evidence lifecycle stage.
    /// </summary>
    public static ConformanceManifestLifecycleStage ReleaseEvidence { get; } = new("release-evidence");

    /// <summary>
    /// Gets the accessibility-validation lifecycle stage.
    /// </summary>
    public static ConformanceManifestLifecycleStage AccessibilityValidation { get; } = new("accessibility-validation");

    private static readonly IReadOnlyDictionary<string, ConformanceManifestLifecycleStage> KnownValues = Known(
        DesignReview,
        AutomatedTest,
        LoadPerformanceTest,
        OperationalDrill,
        ReleaseEvidence,
        AccessibilityValidation);

    private ConformanceManifestLifecycleStage(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported lifecycle stage in canonical order (NFR1).
    /// </summary>
    public static IReadOnlyList<ConformanceManifestLifecycleStage> All { get; } =
    [
        DesignReview,
        AutomatedTest,
        LoadPerformanceTest,
        OperationalDrill,
        ReleaseEvidence,
        AccessibilityValidation,
    ];

    /// <summary>
    /// Resolves a supported lifecycle stage.
    /// </summary>
    /// <param name="value">The canonical lifecycle stage value.</param>
    /// <returns>The matching lifecycle stage.</returns>
    public static ConformanceManifestLifecycleStage Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConformanceManifestLifecycleStage));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Carries content-safe traceability metadata for one conformance test in a release manifest (FR83, FR84, AC1, AC4).
/// </summary>
/// <param name="testId">The stable bounded machine-readable test identifier.</param>
/// <param name="testName">The bounded human-readable test name.</param>
/// <param name="requirementId">The FR or NFR identifier this test verifies (e.g., FR83).</param>
/// <param name="carryForwardCommitmentRef">The optional bounded carry-forward commitment reference.</param>
/// <param name="releaseGateId">The optional release gate this test contributes to; null when not gate-specific.</param>
/// <param name="passCriteria">The bounded pass criteria description.</param>
/// <param name="releaseDecisionStatus">The current decision status (reuses ReleaseGateStatus vocabulary).</param>
/// <param name="waiverReference">The optional bounded waiver reference; required by ValidateManifest when status is waived.</param>
/// <param name="measurementMethod">The bounded measurement method description.</param>
/// <param name="environment">The bounded environment descriptor.</param>
/// <param name="evidenceArtifactHandle">The bounded evidence artifact handle.</param>
/// <param name="owner">The bounded owner identifier.</param>
/// <param name="lifecycleStage">The required lifecycle stage.</param>
/// <param name="registeredAtUtc">The UTC registration timestamp.</param>
public sealed record ConformanceManifestRowV1(
    string TestId,
    string TestName,
    string RequirementId,
    string? CarryForwardCommitmentRef,
    ReleaseGateId? ReleaseGateId,
    string PassCriteria,
    ReleaseGateStatus ReleaseDecisionStatus,
    string? WaiverReference,
    string MeasurementMethod,
    string Environment,
    string EvidenceArtifactHandle,
    string Owner,
    ConformanceManifestLifecycleStage LifecycleStage,
    DateTimeOffset RegisteredAtUtc)
{
    /// <summary>
    /// Gets the stable bounded machine-readable test identifier.
    /// </summary>
    public string TestId { get; } = ConformanceContractValidation.RequiredSafeToken(TestId, nameof(TestId));

    /// <summary>
    /// Gets the bounded human-readable test name.
    /// </summary>
    public string TestName { get; } = ConformanceContractValidation.RequiredSafeText(TestName, nameof(TestName));

    /// <summary>
    /// Gets the FR or NFR identifier this test verifies.
    /// </summary>
    public string RequirementId { get; } = ConformanceContractValidation.RequiredSafeToken(RequirementId, nameof(RequirementId));

    /// <summary>
    /// Gets the optional bounded carry-forward commitment reference.
    /// </summary>
    public string? CarryForwardCommitmentRef { get; } = ConformanceContractValidation.OptionalSafeToken(CarryForwardCommitmentRef, nameof(CarryForwardCommitmentRef));

    /// <summary>
    /// Gets the optional release gate this test contributes to; null when not gate-specific.
    /// </summary>
    public ReleaseGateId? ReleaseGateId { get; } = ReleaseGateId;

    /// <summary>
    /// Gets the bounded pass criteria description.
    /// </summary>
    public string PassCriteria { get; } = ConformanceContractValidation.RequiredSafeText(PassCriteria, nameof(PassCriteria));

    /// <summary>
    /// Gets the current release decision status.
    /// </summary>
    public ReleaseGateStatus ReleaseDecisionStatus { get; } = ReleaseDecisionStatus ?? throw new ArgumentNullException(nameof(ReleaseDecisionStatus));

    /// <summary>
    /// Gets the optional bounded waiver reference; ValidateManifest enforces presence when status is waived.
    /// </summary>
    public string? WaiverReference { get; } = ConformanceContractValidation.OptionalSafeToken(WaiverReference, nameof(WaiverReference));

    /// <summary>
    /// Gets the bounded measurement method description.
    /// </summary>
    public string MeasurementMethod { get; } = ConformanceContractValidation.RequiredSafeText(MeasurementMethod, nameof(MeasurementMethod));

    /// <summary>
    /// Gets the bounded environment descriptor.
    /// </summary>
    public string Environment { get; } = ConformanceContractValidation.RequiredSafeToken(Environment, nameof(Environment));

    /// <summary>
    /// Gets the bounded evidence artifact handle.
    /// </summary>
    public string EvidenceArtifactHandle { get; } = ConformanceContractValidation.RequiredSafeToken(EvidenceArtifactHandle, nameof(EvidenceArtifactHandle));

    /// <summary>
    /// Gets the bounded owner identifier.
    /// </summary>
    public string Owner { get; } = ConformanceContractValidation.RequiredSafeToken(Owner, nameof(Owner));

    /// <summary>
    /// Gets the required lifecycle stage.
    /// </summary>
    public ConformanceManifestLifecycleStage LifecycleStage { get; } = LifecycleStage ?? throw new ArgumentNullException(nameof(LifecycleStage));

    /// <summary>
    /// Gets the UTC registration timestamp.
    /// </summary>
    public DateTimeOffset RegisteredAtUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(RegisteredAtUtc, nameof(RegisteredAtUtc));
}

/// <summary>
/// Records a version history entry for a conformance manifest change (AC2).
/// </summary>
/// <param name="changeId">The bounded machine-readable change identifier.</param>
/// <param name="changeSummary">The bounded human-readable change summary.</param>
/// <param name="affectedRequirementIds">The non-empty list of affected requirement identifiers.</param>
/// <param name="changedAtUtc">The UTC timestamp when the change occurred.</param>
/// <param name="changedBy">The bounded identifier of who made the change.</param>
public sealed record ConformanceManifestChangeV1(
    string ChangeId,
    string ChangeSummary,
    IReadOnlyList<string> AffectedRequirementIds,
    DateTimeOffset ChangedAtUtc,
    string ChangedBy)
{
    /// <summary>
    /// Gets the bounded machine-readable change identifier.
    /// </summary>
    public string ChangeId { get; } = ConformanceContractValidation.RequiredSafeToken(ChangeId, nameof(ChangeId));

    /// <summary>
    /// Gets the bounded human-readable change summary.
    /// </summary>
    public string ChangeSummary { get; } = ConformanceContractValidation.RequiredSafeText(ChangeSummary, nameof(ChangeSummary));

    /// <summary>
    /// Gets the non-empty list of affected requirement identifiers.
    /// </summary>
    public IReadOnlyList<string> AffectedRequirementIds { get; } = ValidateAffectedIds(AffectedRequirementIds, nameof(AffectedRequirementIds));

    /// <summary>
    /// Gets the UTC timestamp when the change occurred.
    /// </summary>
    public DateTimeOffset ChangedAtUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ChangedAtUtc, nameof(ChangedAtUtc));

    /// <summary>
    /// Gets the bounded identifier of who made the change.
    /// </summary>
    public string ChangedBy { get; } = ConformanceContractValidation.RequiredSafeToken(ChangedBy, nameof(ChangedBy));

    private static IReadOnlyList<string> ValidateAffectedIds(IReadOnlyList<string>? values, string parameterName)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one affected requirement identifier is required.", parameterName);
        }

        return values.Select(v => ConformanceContractValidation.RequiredSafeToken(v, parameterName)).ToArray();
    }
}

/// <summary>
/// Carries the versioned release-specific conformance manifest mapping tests to requirements (FR83, FR84, AC1-AC3).
/// </summary>
/// <param name="schemaVersion">The manifest schema version.</param>
/// <param name="manifestVersion">The bounded release-specific manifest version string (e.g., v1-2026-05-23).</param>
/// <param name="releaseReference">The bounded release reference.</param>
/// <param name="generatedAtUtc">The UTC generation timestamp.</param>
/// <param name="entries">The non-empty list of conformance manifest rows; null entries forbidden.</param>
/// <param name="changeLog">The version history; may be empty but null list is not allowed.</param>
public sealed record ConformanceManifestV1(
    SchemaVersion SchemaVersion,
    string ManifestVersion,
    string ReleaseReference,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<ConformanceManifestRowV1> Entries,
    IReadOnlyList<ConformanceManifestChangeV1> ChangeLog)
{
    /// <summary>
    /// Gets the manifest schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the bounded release-specific manifest version string.
    /// </summary>
    public string ManifestVersion { get; } = ConformanceContractValidation.RequiredSafeToken(ManifestVersion, nameof(ManifestVersion));

    /// <summary>
    /// Gets the bounded release reference.
    /// </summary>
    public string ReleaseReference { get; } = ConformanceContractValidation.RequiredSafeToken(ReleaseReference, nameof(ReleaseReference));

    /// <summary>
    /// Gets the UTC generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(GeneratedAtUtc, nameof(GeneratedAtUtc));

    /// <summary>
    /// Gets the non-empty list of conformance manifest rows.
    /// </summary>
    public IReadOnlyList<ConformanceManifestRowV1> Entries { get; } = ValidateEntries(Entries, nameof(Entries));

    /// <summary>
    /// Gets the version history (may be empty; null is not allowed).
    /// </summary>
    public IReadOnlyList<ConformanceManifestChangeV1> ChangeLog { get; } = ChangeLog ?? throw new ArgumentNullException(nameof(ChangeLog));

    private static IReadOnlyList<ConformanceManifestRowV1> ValidateEntries(IReadOnlyList<ConformanceManifestRowV1>? values, string parameterName)
    {
        if (values is null || values.Count == 0 || values.Any(v => v is null))
        {
            throw new ArgumentException("At least one manifest entry is required with no null entries.", parameterName);
        }

        return values.ToArray();
    }
}

/// <summary>
/// Validates a <see cref="ConformanceManifestV1"/> and returns content-safe typed diagnostic tokens (AC2, AC3, AC4).
/// </summary>
public static class ConformanceManifestValidator
{
    /// <summary>
    /// Validates the manifest and returns typed diagnostic reasons for any violations.
    /// </summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <returns>A read-only list of content-safe token error reasons; empty when the manifest is valid.</returns>
    public static IReadOnlyList<string> ValidateManifest(ConformanceManifestV1 manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        List<string> errors = [];

        IEnumerable<IGrouping<string, ConformanceManifestRowV1>> duplicates = manifest.Entries
            .GroupBy(e => e.TestId, StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (IGrouping<string, ConformanceManifestRowV1> _ in duplicates)
        {
            errors.Add("duplicate-test-id");
        }

        foreach (ConformanceManifestRowV1 row in manifest.Entries)
        {
            if (row.ReleaseDecisionStatus.Equals(ReleaseGateStatus.Waived) && row.WaiverReference is null)
            {
                errors.Add("missing-waiver-reference");
            }

            if (string.IsNullOrWhiteSpace(row.RequirementId))
            {
                errors.Add("missing-requirement-id");
            }

            if (string.IsNullOrWhiteSpace(row.PassCriteria))
            {
                errors.Add("missing-pass-criteria");
            }
        }

        return errors;
    }
}
