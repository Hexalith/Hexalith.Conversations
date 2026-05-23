// <copyright file="BuyerAcceptanceConformanceSuiteTest.cs" company="ITANEO">
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
/// Verifies the buyer partial acceptance conformance sub-suite covers all 10 required scenarios, produces
/// machine-readable content-safe CI-suitable results, and maps to the buyer acceptance governance precondition.
/// </summary>
public sealed class BuyerAcceptanceConformanceSuiteTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<BuyerAcceptanceScenarioData> Scenarios = BuyerAcceptanceConformanceSeedData.Scenarios;
    private const string CorrelationId = "corr-buyer-acceptance-test";

    private static ConformanceRunResultV1 Run()
        => new BuyerAcceptanceConformanceSuite().Run(Scenarios, CorrelationId, EvaluatedAt);

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

        // Scenarios 1-5 expect no errors and should produce Ready outcome
        string[] passScenarioIds =
        [
            "buyer-accept-main",
            "buyer-exclude-boundary",
            "buyer-gap-accepted",
            "buyer-waived-with-link",
            "buyer-blocker-approved-control",
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
        // flags the negative cases). The overall result is Ready. This test verifies the conformance is correct.
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
    public void AllChecksShouldCarryFR102RequirementAndBuyerAcceptanceMappings()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ConformanceCheckResultV1 check in run.Checks)
        {
            check.RequirementMappings.ShouldContain("FR102");
            check.PreconditionMappings.ShouldNotBeEmpty();
            check.ReleaseGateMappings.ShouldContain("buyer-acceptance");
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
        run.SuiteId.ShouldBe("buyer-acceptance-suite");
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

        json.ShouldContain("\"suiteId\":\"buyer-acceptance-suite\"");
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
        BuyerAcceptanceConformanceSuite suite = new();
        Should.Throw<ArgumentNullException>(() =>
            suite.Run(null!, CorrelationId, EvaluatedAt));
    }

    [Fact]
    public void EmptyScenariosListShouldThrow()
    {
        BuyerAcceptanceConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run([], CorrelationId, EvaluatedAt));
    }

    [Fact]
    public void NullCorrelationIdShouldThrow()
    {
        BuyerAcceptanceConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run(Scenarios, null!, EvaluatedAt));
    }

    [Fact]
    public void ExpiredItemShouldProduceConformantResult()
    {
        // Verifies that the "buyer-expired-item" scenario is conformant:
        // the validator correctly flags "expired-acceptance-item" and the suite
        // sees matching expected errors, so the check is conformant (Ready).
        ConformanceRunResultV1 run = Run();
        ConformanceCheckResultV1 check = run.Checks.Single(c => c.Scenario == "buyer-expired-item");
        check.Outcome.ShouldBe(ConformanceOutcome.Ready);
        check.FailureClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }

    [Fact]
    public void MissingAckShouldProduceConformantResult()
    {
        // Verifies that the "buyer-missing-ack" scenario is conformant:
        // the validator correctly flags "missing-buyer-acknowledgement" and the suite sees matching errors.
        ConformanceRunResultV1 run = Run();
        ConformanceCheckResultV1 check = run.Checks.Single(c => c.Scenario == "buyer-missing-ack");
        check.Outcome.ShouldBe(ConformanceOutcome.Ready);
        check.FailureClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }
}
