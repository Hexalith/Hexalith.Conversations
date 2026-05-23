// <copyright file="ConformanceRunResultV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Carries the content-safe machine-readable result of an adopter-facing conformance run.
/// </summary>
/// <remarks>
/// This is the deterministic, CI-suitable artifact an adopter emits to prove their integration respects
/// Conversations contracts before deployment. It carries only structured, content-safe data and serializes
/// to stable camelCase web JSON so CI can parse pass/fail and per-check failure classification. It does not
/// aggregate release-gate evidence, sign artifacts, or build a manifest; those are carried forward into
/// Story 5.10, which consumes this local evidence.
/// </remarks>
/// <param name="schemaVersion">The result schema version.</param>
/// <param name="overallOutcome">The aggregate closed-vocabulary outcome across all checks.</param>
/// <param name="overallClassification">The aggregate closed-vocabulary failure classification across all checks.</param>
/// <param name="safeSummary">The bounded content-safe summary message.</param>
/// <param name="suiteId">The bounded machine-readable conformance suite identifier.</param>
/// <param name="runnerId">The bounded machine-readable runner identifier.</param>
/// <param name="correlationId">The safe correlation identifier.</param>
/// <param name="generatedAtUtc">The UTC timestamp when the run was generated.</param>
/// <param name="checks">The per-check results.</param>
public sealed record ConformanceRunResultV1(
    SchemaVersion SchemaVersion,
    ConformanceOutcome OverallOutcome,
    ConformanceFailureClassification OverallClassification,
    string SafeSummary,
    string SuiteId,
    string RunnerId,
    string CorrelationId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<ConformanceCheckResultV1> Checks)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the aggregate closed-vocabulary outcome.
    /// </summary>
    public ConformanceOutcome OverallOutcome { get; } = OverallOutcome ?? throw new ArgumentNullException(nameof(OverallOutcome));

    /// <summary>
    /// Gets the aggregate closed-vocabulary failure classification.
    /// </summary>
    public ConformanceFailureClassification OverallClassification { get; } =
        OverallClassification ?? throw new ArgumentNullException(nameof(OverallClassification));

    /// <summary>
    /// Gets the bounded content-safe summary message.
    /// </summary>
    public string SafeSummary { get; } = ConformanceContractValidation.RequiredSafeText(SafeSummary, nameof(SafeSummary));

    /// <summary>
    /// Gets the bounded machine-readable conformance suite identifier.
    /// </summary>
    public string SuiteId { get; } = ConformanceContractValidation.RequiredSafeToken(SuiteId, nameof(SuiteId));

    /// <summary>
    /// Gets the bounded machine-readable runner identifier.
    /// </summary>
    public string RunnerId { get; } = ConformanceContractValidation.RequiredSafeToken(RunnerId, nameof(RunnerId));

    /// <summary>
    /// Gets the safe correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = ConformanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    /// <summary>
    /// Gets the UTC timestamp when the run was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(GeneratedAtUtc, nameof(GeneratedAtUtc));

    /// <summary>
    /// Gets the per-check results.
    /// </summary>
    public IReadOnlyList<ConformanceCheckResultV1> Checks { get; } = ValidateChecks(Checks);

    private static IReadOnlyList<ConformanceCheckResultV1> ValidateChecks(IReadOnlyList<ConformanceCheckResultV1>? values)
        => values is null || values.Count == 0 || values.Any(value => value is null)
            ? throw new ArgumentException("At least one conformance check result is required.", nameof(values))
            : values.ToArray();
}
