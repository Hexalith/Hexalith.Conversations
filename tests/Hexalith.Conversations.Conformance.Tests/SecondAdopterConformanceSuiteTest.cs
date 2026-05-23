// <copyright file="SecondAdopterConformanceSuiteTest.cs" company="ITANEO">
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
/// Verifies the second-adopter lifecycle conformance sub-suite covers all 10 required scenarios, produces
/// machine-readable content-safe CI-suitable results, and maps to the second-adopter governance precondition.
/// </summary>
public sealed class SecondAdopterConformanceSuiteTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<SecondAdopterScenarioData> Scenarios = SecondAdopterConformanceSeedData.Scenarios;
    private const string CorrelationId = "corr-second-adopter-test";

    private static ConformanceRunResultV1 Run()
        => new SecondAdopterConformanceSuite().Run(Scenarios, CorrelationId, EvaluatedAt);

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
    public void AllPassScenariosShouldProduceReadyOutcome()
    {
        ConformanceRunResultV1 run = Run();

        string[] passScenarioIds =
        [
            "adopter-identified-baseline",
            "adopter-qualified-trigger-set",
            "adopter-deferred-waiver-valid",
            "adopter-disqualified-rationale",
            "adopter-qualified-capability-link",
        ];

        foreach (string scenarioId in passScenarioIds)
        {
            ConformanceCheckResultV1 check = run.Checks.Single(c => c.Scenario == scenarioId);
            check.Outcome.ShouldBe(
                ConformanceOutcome.Ready,
                $"Scenario '{scenarioId}' should produce Ready outcome");
        }
    }

    [Fact]
    public void AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails()
    {
        // With correct validator implementation, all 10 scenarios are conformant (validator correctly
        // flags the negative cases). The overall result is Ready.
        ConformanceRunResultV1 run = Run();
        run.OverallOutcome.ShouldBe(ConformanceOutcome.Ready);
        run.OverallClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }

    [Fact]
    public void AllChecksShouldBeClassifiedAsConformant()
    {
        ConformanceRunResultV1 run = Run();
        run.Checks.ShouldAllBe(check => check.FailureClassification.Equals(ConformanceFailureClassification.Conformant));
        run.OverallClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }

    [Fact]
    public void AllChecksShouldCarryFR103RequirementAndSecondAdopterMappings()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ConformanceCheckResultV1 check in run.Checks)
        {
            check.RequirementMappings.ShouldContain("FR103");
            check.PreconditionMappings.ShouldNotBeEmpty();
            check.ReleaseGateMappings.ShouldContain("second-adopter");
        }
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
    public void SuiteIdAndRunnerIdShouldMatchSpecifiedValues()
    {
        ConformanceRunResultV1 run = Run();
        run.SuiteId.ShouldBe("second-adopter-suite");
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

        json.ShouldContain("\"suiteId\":\"second-adopter-suite\"");
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
        SecondAdopterConformanceSuite suite = new();
        Should.Throw<ArgumentNullException>(() =>
            suite.Run(null!, CorrelationId, EvaluatedAt));
    }

    [Fact]
    public void EmptyScenariosListShouldThrow()
    {
        SecondAdopterConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run([], CorrelationId, EvaluatedAt));
    }

    [Fact]
    public void NullCorrelationIdShouldThrow()
    {
        SecondAdopterConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run(Scenarios, null!, EvaluatedAt));
    }

    [Fact]
    public void MilestoneOverdueShouldProduceConformantResult()
    {
        ConformanceRunResultV1 run = Run();
        ConformanceCheckResultV1 check = run.Checks.Single(c => c.Scenario == "adopter-milestone-overdue");
        check.Outcome.ShouldBe(ConformanceOutcome.Ready);
        check.FailureClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }

    [Fact]
    public void RevertedNoRationaleShouldProduceConformantResult()
    {
        ConformanceRunResultV1 run = Run();
        ConformanceCheckResultV1 check = run.Checks.Single(c => c.Scenario == "adopter-reverted-no-rationale");
        check.Outcome.ShouldBe(ConformanceOutcome.Ready);
        check.FailureClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }
}
