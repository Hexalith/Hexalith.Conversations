// <copyright file="ConformanceStatusConformanceSuite.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Diagnostics;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Runs the conformance status classification sub-suite against a deterministic synthetic scenario list and
/// produces a machine-readable, content-safe <see cref="ConformanceRunResultV1"/> suitable for CI consumption.
/// </summary>
/// <remarks>
/// The suite verifies that <see cref="ConversationConformanceStatusClassifier"/> correctly maps all conformance
/// inputs to the expected bounded status classes. It is read-only: no aggregate command dispatch, no event
/// appends, no projection writes, no governance state mutations, and no external service calls. All 10 scenario
/// checks use <see cref="ConformanceCheck.GovernancePrecondition"/> and map to <c>FR99</c>.
/// </remarks>
public sealed class ConformanceStatusConformanceSuite
{
    private readonly Uri _documentation = new("https://docs.hexalith.local/conversations/compliance/v1/conformance-status");

    /// <summary>
    /// Runs every conformance status scenario and aggregates the results into a machine-readable run result.
    /// </summary>
    /// <param name="scenarios">The deterministic synthetic scenario data list.</param>
    /// <param name="correlationId">The safe correlation identifier for the run.</param>
    /// <param name="evaluatedAt">The deterministic evaluation timestamp (do not use DateTimeOffset.UtcNow).</param>
    /// <returns>The content-safe machine-readable conformance run result.</returns>
    public ConformanceRunResultV1 Run(
        IReadOnlyList<ConformanceStatusScenarioData> scenarios,
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
            .Select(s => BuildCheck(s))
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
                ? "One or more conformance status classification scenarios did not produce the expected status class."
                : "All conformance status classification scenarios conform to expected classifier behaviour.",
            "conformance-status-suite",
            "local-ci-runner",
            correlationId,
            evaluatedAt,
            results);
    }

    private ConformanceCheckResultV1 BuildCheck(ConformanceStatusScenarioData scenario)
    {
        string checkCorrelationId = $"corr-cs-{scenario.ScenarioId}";

        ConversationConformanceStatusClass actualStatusClass = scenario.GateStatus is not null
            ? ConversationConformanceStatusClassifier.ClassifyGate(scenario.GateStatus)
            : ConversationConformanceStatusClassifier.Classify(scenario.ExpectedOutcome!, scenario.ExpectedClassification!);

        bool isConformant = actualStatusClass == scenario.ExpectedStatusClass;

        ConformanceOutcome checkOutcome = isConformant
            ? ConformanceOutcome.Ready
            : ConformanceOutcome.Blocked;

        ConformanceFailureClassification checkClassification = isConformant
            ? ConformanceFailureClassification.Conformant
            : ConformanceFailureClassification.ProductInvariant;

        ConversationError? error = isConformant
            ? null
            : ConversationErrorCatalog.CreateError(ConversationErrorCode.SchemaVersionUnsupported, checkCorrelationId);

        string safeMessage = isConformant
            ? $"Conformance classification scenario {scenario.ScenarioId} verified: classifier returned expected class."
            : $"Conformance classification scenario {scenario.ScenarioId} did not return expected class.";

        string remediationCode = isConformant ? "none" : "fail-closed";

        return new ConformanceCheckResultV1(
            SchemaVersion.Current,
            ConformanceCheck.GovernancePrecondition,
            scenario.ScenarioId,
            checkOutcome,
            checkClassification,
            ["FR99"],
            ["conformance-status-precondition"],
            ["conformance-status"],
            safeMessage,
            remediationCode,
            _documentation,
            checkCorrelationId,
            error);
    }
}
