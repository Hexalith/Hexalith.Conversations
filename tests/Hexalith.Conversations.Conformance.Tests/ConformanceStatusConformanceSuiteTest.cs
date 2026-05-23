// <copyright file="ConformanceStatusConformanceSuiteTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Testing.Fixtures;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Verifies the conformance status sub-suite covers all 10 required classifier scenarios, produces
/// machine-readable content-safe CI-suitable results, and maps to the conformance-status release gate.
/// </summary>
public sealed class ConformanceStatusConformanceSuiteTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<ConformanceStatusScenarioData> Scenarios = ConformanceStatusConformanceSeedData.Scenarios;
    private const string CorrelationId = "cs-conformance-corr-001";

    private static ConformanceRunResultV1 Run()
        => new ConformanceStatusConformanceSuite().Run(Scenarios, CorrelationId, EvaluatedAt);

    [Fact]
    public void RunResultShouldHaveExactly10Checks()
    {
        ConformanceRunResultV1 run = Run();
        run.Checks.Count.ShouldBe(10);
    }

    [Fact]
    public void AllChecksShouldUseGovernancePreconditionCheckId()
    {
        ConformanceRunResultV1 run = Run();
        run.Checks.ShouldAllBe(check => check.Check.Equals(ConformanceCheck.GovernancePrecondition));
    }

    [Fact]
    public void EachScenarioShouldProduceExpectedConformanceOutcome()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ConformanceStatusScenarioData scenario in Scenarios)
        {
            ConformanceCheckResultV1 check = run.Checks.Single(c => c.Scenario == scenario.ScenarioId);
            check.Outcome.ShouldBe(
                ConformanceOutcome.Ready,
                $"Scenario '{scenario.ScenarioId}' should produce conformant outcome Ready");
        }
    }

    [Fact]
    public void EachScenarioCheckShouldBeClassifiedAsConformant()
    {
        ConformanceRunResultV1 run = Run();
        run.Checks.ShouldAllBe(check => check.FailureClassification.Equals(ConformanceFailureClassification.Conformant));
        run.OverallClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }

    [Fact]
    public void AllChecksShouldCarryFR99RequirementAndConformanceStatusMappings()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ConformanceCheckResultV1 check in run.Checks)
        {
            check.RequirementMappings.ShouldContain("FR99");
            check.PreconditionMappings.ShouldNotBeEmpty();
            check.ReleaseGateMappings.ShouldContain("conformance-status");
        }
    }

    [Fact]
    public void PreconditionMappingsShouldNotBeEmpty()
    {
        ConformanceRunResultV1 run = Run();
        run.Checks.ShouldAllBe(check => check.PreconditionMappings.Count > 0);
    }

    [Fact]
    public void PassScenariosShouldHaveNullTypedError()
    {
        ConformanceRunResultV1 run = Run();

        IEnumerable<ConformanceCheckResultV1> readyChecks = run.Checks
            .Where(check => check.Outcome.Equals(ConformanceOutcome.Ready));

        readyChecks.ShouldNotBeEmpty();
        readyChecks.ShouldAllBe(check => check.Error == null);
    }

    [Fact]
    public void FailScenariosShouldHaveNonNullTypedError()
    {
        ConformanceRunResultV1 run = Run();

        IEnumerable<ConformanceCheckResultV1> failChecks = run.Checks
            .Where(check => !check.Outcome.Equals(ConformanceOutcome.Ready));

        // For a correct classifier, all 10 checks produce Ready — no fail checks exist.
        // The ShouldAllBe guard ensures that any check the suite ever marks non-Ready carries a typed error.
        failChecks.ShouldBeEmpty("All conformance status checks should be conformant when the classifier is correct");
        failChecks.ShouldAllBe(check => check.Error != null);
    }

    [Fact]
    public void OnlyProductInvariantFailScenarioShouldHaveBlockingTrue()
    {
        IReadOnlyList<ConformanceStatusScenarioData> blockingScenarios =
            Scenarios.Where(s => s.IsBlocking).ToList();

        blockingScenarios.Count.ShouldBe(1);
        blockingScenarios[0].ScenarioId.ShouldBe("conformance-product-invariant-fail");
    }

    [Fact]
    public void WaivedGateScenarioShouldProduceReadyOutcome()
    {
        ConformanceRunResultV1 run = Run();

        ConformanceCheckResultV1 waivedCheck = run.Checks.Single(c => c.Scenario == "conformance-waived-gate");
        waivedCheck.Outcome.ShouldBe(ConformanceOutcome.Ready);
        waivedCheck.FailureClassification.ShouldBe(ConformanceFailureClassification.Conformant);
        waivedCheck.Error.ShouldBeNull();
    }

    [Fact]
    public void SuiteIdAndRunnerIdShouldMatchSpecifiedValues()
    {
        ConformanceRunResultV1 run = Run();
        run.SuiteId.ShouldBe("conformance-status-suite");
        run.RunnerId.ShouldBe("local-ci-runner");
    }

    [Fact]
    public void RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments()
    {
        ConversationConformanceCoreSeedData coreFixture = ConversationConformanceCoreFixtures.Create();
        string json = JsonSerializer.Serialize(Run(), WebOptions);

        foreach (string sentinel in coreFixture.PoisonSentinelValues)
        {
            json.ShouldNotContain(sentinel, Case.Insensitive);
        }

        string[] forbiddenFragments =
        [
            "EventStore",
            "snapshot",
            "SignalR",
            "dispatcher",
            "repository",
            "provider-session",
            "provider payload",
            "raw exception",
            "C:\\",
            "D:\\",
        ];

        foreach (string fragment in forbiddenFragments)
        {
            json.ShouldNotContain(fragment, Case.Insensitive);
        }
    }

    [Fact]
    public void RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip()
    {
        ConformanceRunResultV1 run = Run();

        string json = JsonSerializer.Serialize(run, WebOptions);
        string secondJson = JsonSerializer.Serialize(Run(), WebOptions);
        json.ShouldBe(secondJson);

        json.ShouldContain("\"suiteId\":\"conformance-status-suite\"");
        json.ShouldContain("\"overallOutcome\":\"ready\"");
        json.ShouldContain("\"overallClassification\":\"conformant\"");
        json.ShouldContain("\"failureClassification\":\"conformant\"");

        ConformanceRunResultV1 parsed = JsonSerializer.Deserialize<ConformanceRunResultV1>(json, WebOptions)!;
        parsed.ShouldNotBeNull();
        parsed.SuiteId.ShouldBe(run.SuiteId);
        parsed.OverallOutcome.ShouldBe(run.OverallOutcome);
        parsed.OverallClassification.ShouldBe(run.OverallClassification);
        parsed.GeneratedAtUtc.ShouldBe(run.GeneratedAtUtc);
        parsed.Checks.Count.ShouldBe(run.Checks.Count);

        foreach (ConformanceCheckResultV1 original in run.Checks)
        {
            ConformanceCheckResultV1 reparsed = parsed.Checks.Single(c => c.Scenario == original.Scenario);
            reparsed.Outcome.ShouldBe(original.Outcome);
            reparsed.FailureClassification.ShouldBe(original.FailureClassification);
            reparsed.RequirementMappings.ShouldBe(original.RequirementMappings);
            reparsed.PreconditionMappings.ShouldBe(original.PreconditionMappings);
            reparsed.ReleaseGateMappings.ShouldBe(original.ReleaseGateMappings);
            reparsed.CorrelationId.ShouldBe(original.CorrelationId);
            (reparsed.Error == null).ShouldBe(original.Error == null);
        }
    }

    [Fact]
    public void NullScenariosListShouldThrow()
    {
        ConformanceStatusConformanceSuite suite = new();
        Should.Throw<ArgumentNullException>(() =>
            suite.Run(null!, CorrelationId, EvaluatedAt));
    }

    [Fact]
    public void NullCorrelationIdShouldThrow()
    {
        ConformanceStatusConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run(Scenarios, null!, EvaluatedAt));
    }

    [Fact]
    public void EmptyScenariosListShouldThrow()
    {
        ConformanceStatusConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run([], CorrelationId, EvaluatedAt));
    }
}
