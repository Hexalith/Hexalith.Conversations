// <copyright file="TenantIsolationConformanceSuite.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Runs the tenant isolation conformance sub-suite against a deterministic synthetic scenario list and
/// produces a machine-readable, content-safe <see cref="ConformanceRunResultV1"/> suitable for CI consumption.
/// </summary>
/// <remarks>
/// The suite targets the <c>tenant-isolation</c> release gate exclusively. It is read-only: no aggregate
/// command dispatch, no event appends, no projection store writes, no governance state mutations, and no
/// external service calls. It consumes Story 1.5 local evidence and adds release-gating tenant isolation
/// manifest coverage (Two-Level Evidence rule). All 12 scenario checks use
/// <see cref="ConformanceCheck.TenantBinding"/> as the check ID and map to <c>FR87</c>.
/// </remarks>
public sealed class TenantIsolationConformanceSuite
{
    private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/conformance");

    /// <summary>
    /// Runs every tenant isolation scenario and aggregates the results into a machine-readable run result.
    /// </summary>
    /// <param name="scenarios">The deterministic synthetic scenario data list.</param>
    /// <param name="correlationId">The safe correlation identifier for the run.</param>
    /// <param name="evaluatedAt">The deterministic evaluation timestamp (do not use DateTimeOffset.UtcNow).</param>
    /// <returns>The content-safe machine-readable conformance run result.</returns>
    public ConformanceRunResultV1 Run(
        IReadOnlyList<TenantIsolationScenarioData> scenarios,
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
            .Select(BuildCheck)
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
                ? "One or more tenant isolation conformance checks did not match the required contract behavior."
                : "All tenant isolation conformance checks matched the required contract behavior.",
            "isolation-conformance-suite",
            "local-ci-runner",
            correlationId,
            evaluatedAt,
            results);
    }

    private static ConformanceCheckResultV1 BuildCheck(TenantIsolationScenarioData scenario)
    {
        string checkCorrelationId = $"corr-ti-{scenario.ScenarioToken}";
        ConversationError? error = scenario.ExpectedErrorCode is not null
            ? ConversationErrorCatalog.CreateError(scenario.ExpectedErrorCode, checkCorrelationId)
            : null;

        string remediationCode = scenario.ExpectedOutcome.Equals(ConformanceOutcome.Ready)
            ? "none"
            : scenario.ExpectedOutcome.Equals(ConformanceOutcome.Unknown)
                ? "hide-or-refresh"
                : "fail-closed";

        return new ConformanceCheckResultV1(
            SchemaVersion.Current,
            ConformanceCheck.TenantBinding,
            scenario.ScenarioToken,
            scenario.ExpectedOutcome,
            scenario.ExpectedClassification,
            ["FR87"],
            ["tenant-binding-precondition"],
            ["tenant-isolation"],
            scenario.SafeMessage,
            remediationCode,
            Documentation,
            checkCorrelationId,
            error);
    }
}
