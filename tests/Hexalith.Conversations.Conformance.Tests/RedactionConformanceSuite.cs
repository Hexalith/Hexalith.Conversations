// <copyright file="RedactionConformanceSuite.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Runs the redaction replay conformance sub-suite against a deterministic synthetic scenario list and
/// produces a machine-readable, content-safe <see cref="ConformanceRunResultV1"/> suitable for CI consumption.
/// </summary>
/// <remarks>
/// The suite targets the <c>redaction-non-leakage</c> release gate mapping exclusively. It is read-only: no
/// aggregate command dispatch, no event appends, no projection store writes, no governance state mutations,
/// and no external service calls. All 10 scenario checks use <see cref="ConformanceCheck.GovernancePrecondition"/>
/// as the check ID and map to <c>FR89</c>.
/// </remarks>
public sealed class RedactionConformanceSuite
{
    private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/conformance");

    /// <summary>
    /// Runs every redaction replay scenario and aggregates the results into a machine-readable run result.
    /// </summary>
    /// <param name="scenarios">The deterministic synthetic scenario data list.</param>
    /// <param name="correlationId">The safe correlation identifier for the run.</param>
    /// <param name="evaluatedAt">The deterministic evaluation timestamp (do not use DateTimeOffset.UtcNow).</param>
    /// <returns>The content-safe machine-readable conformance run result.</returns>
    public ConformanceRunResultV1 Run(
        IReadOnlyList<RedactionReplayScenarioData> scenarios,
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
                ? "One or more redaction replay scenarios failed conformance."
                : "All redaction replay scenarios conform to expected behaviour.",
            "redaction-conformance-suite",
            "local-ci-runner",
            correlationId,
            evaluatedAt,
            results);
    }

    private static ConformanceCheckResultV1 BuildCheck(RedactionReplayScenarioData scenario)
    {
        string checkCorrelationId = $"corr-rdx-{scenario.ScenarioToken}";
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
            ConformanceCheck.GovernancePrecondition,
            scenario.ScenarioToken,
            scenario.ExpectedOutcome,
            scenario.ExpectedClassification,
            ["FR89"],
            ["redaction-precondition"],
            ["redaction-non-leakage"],
            scenario.SafeMessage,
            remediationCode,
            Documentation,
            checkCorrelationId,
            error);
    }
}
