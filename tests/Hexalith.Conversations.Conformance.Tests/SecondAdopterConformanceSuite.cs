// <copyright file="SecondAdopterConformanceSuite.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Runs the second-adopter lifecycle governance conformance sub-suite against a deterministic synthetic scenario
/// list and produces a machine-readable, content-safe <see cref="ConformanceRunResultV1"/> suitable for CI
/// consumption.
/// </summary>
/// <remarks>
/// The suite verifies that <see cref="SecondAdopterStatusValidator"/> correctly flags or accepts each
/// scenario's second-adopter status entry. It is read-only: no aggregate command dispatch, no event appends, no
/// projection store writes, no governance state mutations, and no external service calls. All 10 scenario
/// checks use <see cref="ConformanceCheck.GovernancePrecondition"/> and map to <c>FR103</c>.
/// A scenario is conformant when the validator's actual error tokens match the scenario's expected error tokens.
/// </remarks>
public sealed class SecondAdopterConformanceSuite
{
    private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/compliance/v1/second-adopter");

    /// <summary>
    /// Runs every second-adopter scenario and aggregates the results into a machine-readable run result.
    /// </summary>
    /// <param name="scenarios">The deterministic synthetic scenario data list.</param>
    /// <param name="correlationId">The safe correlation identifier for the run.</param>
    /// <param name="evaluatedAt">The deterministic evaluation timestamp (do not use DateTimeOffset.UtcNow).</param>
    /// <returns>The content-safe machine-readable conformance run result.</returns>
    public ConformanceRunResultV1 Run(
        IReadOnlyList<SecondAdopterScenarioData> scenarios,
        string correlationId,
        DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (scenarios.Count == 0)
        {
            throw new ArgumentException("At least one scenario is required.", nameof(scenarios));
        }

        ConformanceCheckResultV1[] results = scenarios
            .Select(s => BuildCheck(s, evaluatedAt))
            .ToArray();

        bool anyFailure = results.Any(result => result.FailureClassification.IsFailure);
        bool anyDegraded = results.Any(result => result.Outcome.Equals(ConformanceOutcome.Degraded));

        ConformanceOutcome overallOutcome = anyFailure
            ? ConformanceOutcome.Blocked
            : anyDegraded
                ? ConformanceOutcome.Degraded
                : ConformanceOutcome.Ready;
        ConformanceFailureClassification overallClassification = anyFailure
            ? results.First(result => result.FailureClassification.IsFailure).FailureClassification
            : ConformanceFailureClassification.Conformant;

        return new ConformanceRunResultV1(
            SchemaVersion.Current,
            overallOutcome,
            overallClassification,
            anyFailure
                ? "One or more second-adopter lifecycle scenarios did not pass validation."
                : "All second-adopter lifecycle scenarios conform to expected validator behaviour.",
            "second-adopter-suite",
            "local-ci-runner",
            correlationId,
            evaluatedAt,
            results);
    }

    private static ConformanceCheckResultV1 BuildCheck(SecondAdopterScenarioData scenario, DateTimeOffset evaluatedAt)
    {
        string checkCorrelationId = $"corr-sa-{scenario.ScenarioId}";

        IReadOnlyList<string> actualErrors = SecondAdopterStatusValidator.ValidateEntry(scenario.Entry, evaluatedAt);

        bool isConformant = scenario.ExpectedValidationErrors.Count == 0
            ? actualErrors.Count == 0
            : scenario.ExpectedValidationErrors.All(e => actualErrors.Contains(e, StringComparer.Ordinal));

        ConformanceOutcome checkOutcome = isConformant
            ? ConformanceOutcome.Ready
            : ConformanceOutcome.Blocked;

        ConformanceFailureClassification checkClassification = isConformant
            ? ConformanceFailureClassification.Conformant
            : ConformanceFailureClassification.ProductInvariant;

        ConversationError? error = isConformant
            ? null
            : ConversationErrorCatalog.CreateError(ConversationErrorCode.CommandValidationFailed, checkCorrelationId);

        string remediationCode = isConformant ? "none" : "fail-closed";

        return new ConformanceCheckResultV1(
            SchemaVersion.Current,
            ConformanceCheck.GovernancePrecondition,
            scenario.ScenarioId,
            checkOutcome,
            checkClassification,
            ["FR103"],
            ["second-adopter-precondition"],
            ["second-adopter"],
            scenario.SafeMessage,
            remediationCode,
            Documentation,
            checkCorrelationId,
            error);
    }
}
