// <copyright file="ProviderPortabilityConformanceSuiteTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Testing.Fixtures;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Verifies the provider portability conformance sub-suite covers all 10 required scenarios, produces
/// machine-readable content-safe CI-suitable results, and feeds the provider-portability release gate mapping.
/// </summary>
public sealed class ProviderPortabilityConformanceSuiteTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<ProviderPortabilityScenarioData> Scenarios = ProviderPortabilityConformanceSeedData.Scenarios;
    private const string CorrelationId = "prt-conformance-corr-001";

    private static ConformanceRunResultV1 Run()
        => new ProviderPortabilityConformanceSuite().Run(Scenarios, CorrelationId, EvaluatedAt);

    [Fact]
    public void RunResultShouldHaveExactly10Checks()
    {
        ConformanceRunResultV1 run = Run();
        run.Checks.Count.ShouldBe(10);
    }

    [Fact]
    public void AllChecksShouldUseEventPublicationCheckId()
    {
        ConformanceRunResultV1 run = Run();
        run.Checks.ShouldAllBe(check => check.Check.Equals(ConformanceCheck.EventPublication));
    }

    [Fact]
    public void EachScenarioShouldProduceExpectedConformanceOutcome()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ProviderPortabilityScenarioData scenario in Scenarios)
        {
            ConformanceCheckResultV1 check = run.Checks.Single(c => c.Scenario == scenario.ScenarioToken);
            check.Outcome.ShouldBe(scenario.ExpectedOutcome,
                $"Scenario '{scenario.ScenarioToken}' should produce outcome '{scenario.ExpectedOutcome}'");
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
    public void AllChecksShouldCarryFR90RequirementAndPortabilityGateMappings()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ConformanceCheckResultV1 check in run.Checks)
        {
            check.RequirementMappings.ShouldContain("FR90");
            check.PreconditionMappings.ShouldNotBeEmpty();
            check.ReleaseGateMappings.ShouldContain("provider-portability");
        }
    }

    [Fact]
    public void ReadyScenariosShouldHaveNullTypedError()
    {
        ConformanceRunResultV1 run = Run();

        IEnumerable<ConformanceCheckResultV1> readyChecks = run.Checks
            .Where(check => check.Outcome.Equals(ConformanceOutcome.Ready));

        readyChecks.ShouldNotBeEmpty();
        readyChecks.ShouldAllBe(check => check.Error == null);
    }

    [Fact]
    public void BlockedScenariosShouldHaveNonNullTypedError()
    {
        ConformanceRunResultV1 run = Run();

        IEnumerable<ConformanceCheckResultV1> blockedChecks = run.Checks
            .Where(check => check.Outcome.Equals(ConformanceOutcome.Blocked));

        blockedChecks.ShouldNotBeEmpty();
        blockedChecks.Count().ShouldBe(2);
        blockedChecks.ShouldAllBe(check => check.Error != null);
    }

    [Fact]
    public void UnknownScenariosShouldCarryAggregateNotFoundTypedError()
    {
        ConformanceRunResultV1 run = Run();

        IEnumerable<ConformanceCheckResultV1> unknownChecks = run.Checks
            .Where(check => check.Outcome.Equals(ConformanceOutcome.Unknown));

        unknownChecks.ShouldNotBeEmpty();
        unknownChecks.Count().ShouldBe(1);
        unknownChecks.ShouldAllBe(check => check.Error != null);
        unknownChecks.ShouldAllBe(check => check.Error!.Code.Equals(ConversationErrorCode.AggregateNotFound));
        unknownChecks.ShouldAllBe(check => check.Error!.ClientAction == ConversationErrorClientAction.HideOrRefresh);
        unknownChecks.ShouldAllBe(check => !check.Error!.IsRetryable);
    }

    [Fact]
    public void AllConformantScenariosProduceOverallReadyOutcome()
    {
        ConformanceRunResultV1 run = Run();

        run.Checks.ShouldAllBe(check => check.IsConformant);
        run.OverallOutcome.ShouldBe(ConformanceOutcome.Ready);
        run.OverallClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }

    [Fact]
    public void SuiteIdAndRunnerIdShouldMatchSpecifiedValues()
    {
        ConformanceRunResultV1 run = Run();
        run.SuiteId.ShouldBe("portability-conformance-suite");
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

        json.ShouldContain("\"suiteId\":\"portability-conformance-suite\"");
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
            (reparsed.Error is null).ShouldBe(original.Error is null);
            if (original.Error is not null)
            {
                reparsed.Error!.Code.ShouldBe(original.Error.Code);
            }
        }
    }

    [Fact]
    public void NullScenariosListShouldThrow()
    {
        ProviderPortabilityConformanceSuite suite = new();
        Should.Throw<ArgumentNullException>(() =>
            suite.Run(null!, CorrelationId, EvaluatedAt));
    }

    [Fact]
    public void EmptyScenariosListShouldThrow()
    {
        ProviderPortabilityConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run([], CorrelationId, EvaluatedAt));
    }

    [Fact]
    public void NullCorrelationIdShouldThrow()
    {
        ProviderPortabilityConformanceSuite suite = new();
        Should.Throw<ArgumentException>(() =>
            suite.Run(Scenarios, null!, EvaluatedAt));
    }
}
