// <copyright file="AdopterConformanceSuiteTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Testing.Fixtures;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Verifies the adopter-facing conformance suite covers the CORE integration surface, exercises the AC4
/// scenario matrix, and emits machine-readable, content-safe, CI-suitable results.
/// </summary>
public sealed class AdopterConformanceSuiteTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static ConformanceRunResultV1 Run()
        => new AdopterConformanceSuite(ConversationConformanceCoreFixtures.Create()).Run();

    [Fact]
    public void SuiteShouldCoverEveryCoreConformanceCheck()
    {
        ConformanceRunResultV1 run = Run();

        run.Checks.Select(check => check.Check.Value)
            .ShouldBe(ConformanceCheck.All.Select(check => check.Value), ignoreOrder: true);
    }

    [Fact]
    public void SuiteShouldPassEveryCheckAgainstTheSyntheticFixture()
    {
        ConformanceRunResultV1 run = Run();

        run.Checks.ShouldAllBe(check => check.IsConformant);
        run.OverallClassification.ShouldBe(ConformanceFailureClassification.Conformant);
    }

    [Fact]
    public void SuiteShouldExerciseTheAc4ScenarioMatrix()
    {
        ConformanceRunResultV1 run = Run();

        string[] scenarios = run.Checks.Select(check => check.Scenario).Distinct().ToArray();

        // AC4 scenario matrix: supported, unsupported, stale (projection-lag), cross-tenant, duplicate
        // command, projection lag, and sanitized error must each be exercised by the run result.
        scenarios.ShouldContain("supported");
        scenarios.ShouldContain("unsupported");
        scenarios.ShouldContain("cross-tenant");
        scenarios.ShouldContain("duplicate-command");
        scenarios.ShouldContain("projection-lag");
        scenarios.ShouldContain("sanitized-error");
    }

    [Fact]
    public void TenantBindingCheckShouldExerciseCrossTenantHiddenSideChannelShape()
    {
        ConformanceCheckResultV1 check = Run().Checks.Single(c => c.Check.Equals(ConformanceCheck.TenantBinding));

        // The cross-tenant scenario must be exercised through the machine-readable run result and must
        // collapse to the hidden side-channel-equivalent 'unknown' outcome carrying the typed denial,
        // never distinguishing unauthorized from nonexistent.
        check.IsConformant.ShouldBeTrue();
        check.Scenario.ShouldBe("cross-tenant");
        check.Outcome.ShouldBe(ConformanceOutcome.Unknown);
        check.Error.ShouldNotBeNull();
        check.Error!.Code.ShouldBe(ConversationErrorCode.AggregateNotFound);
        check.Error.ClientAction.ShouldBe(ConversationErrorClientAction.HideOrRefresh);
        check.Error.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    public void EveryCheckShouldCarryTraceableRequirementPreconditionAndReleaseGateMappings()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ConformanceCheckResultV1 check in run.Checks)
        {
            check.RequirementMappings.ShouldNotBeEmpty();
            check.PreconditionMappings.ShouldNotBeEmpty();
            check.ReleaseGateMappings.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public void IdempotencyCheckShouldSurfaceNonRetryableConflictAsBlocked()
    {
        ConformanceCheckResultV1 check = Run().Checks.Single(c => c.Check.Equals(ConformanceCheck.Idempotency));

        check.IsConformant.ShouldBeTrue();
        check.Outcome.ShouldBe(ConformanceOutcome.Blocked);
        check.Error.ShouldNotBeNull();
        check.Error!.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        check.Error.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    public void ProjectionFreshnessCheckShouldSurfaceStaleAsDegradedNonTrustBearing()
    {
        ConformanceCheckResultV1 check = Run().Checks.Single(c => c.Check.Equals(ConformanceCheck.ProjectionFreshness));

        check.IsConformant.ShouldBeTrue();
        check.Outcome.ShouldBe(ConformanceOutcome.Degraded);
        check.Error.ShouldNotBeNull();
        check.Error!.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
    }

    [Fact]
    public void CompatibilityDiscoveryCheckShouldSurfaceUnsupportedAsBlockedTypedError()
    {
        ConformanceCheckResultV1 check = Run().Checks.Single(c => c.Check.Equals(ConformanceCheck.CompatibilityDiscovery));

        check.IsConformant.ShouldBeTrue();
        check.Outcome.ShouldBe(ConformanceOutcome.Blocked);
        check.Scenario.ShouldBe("unsupported");
        check.Error.ShouldNotBeNull();
        check.Error!.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
    }

    [Fact]
    public void ErrorEnvelopeCheckShouldReuseSharedTypedErrorCatalog()
    {
        ConformanceCheckResultV1 check = Run().Checks.Single(c => c.Check.Equals(ConformanceCheck.ErrorEnvelope));

        check.IsConformant.ShouldBeTrue();
        check.Error.ShouldNotBeNull();
        ConversationErrorDescriptor descriptor = ConversationErrorCatalog.Get(check.Error!.Code);
        check.Error.Category.ShouldBe(descriptor.Category);
        check.Error.IsRetryable.ShouldBe(descriptor.IsRetryable);
    }

    [Fact]
    public void RunResultShouldSerializeToDeterministicWebJsonForCi()
    {
        ConformanceRunResultV1 run = Run();

        string first = JsonSerializer.Serialize(run, WebOptions);
        string second = JsonSerializer.Serialize(Run(), WebOptions);

        first.ShouldBe(second);
        // The synthetic fixture intentionally exercises a stale projection-lag scenario, so the aggregate
        // outcome is degraded while every check remains conformant (no product-invariant failure).
        first.ShouldContain("\"overallOutcome\":\"degraded\"");
        first.ShouldContain("\"overallClassification\":\"conformant\"");
        first.ShouldContain("\"failureClassification\":\"conformant\"");
    }

    [Fact]
    public void RunResultShouldRoundTripAndRemainAdditiveTolerantForCi()
    {
        string json = JsonSerializer.Serialize(Run(), WebOptions);

        JsonNode node = JsonNode.Parse(json)!;
        node["futureField"] = "ignored";

        ConformanceRunResultV1? parsed = JsonSerializer.Deserialize<ConformanceRunResultV1>(node.ToJsonString(), WebOptions);
        parsed.ShouldNotBeNull();
        parsed!.Checks.Count.ShouldBe(ConformanceCheck.All.Count);
    }

    [Fact]
    public void RunResultShouldRoundTripLosslesslyPreservingEveryCheckFieldForCi()
    {
        ConformanceRunResultV1 run = Run();

        string json = JsonSerializer.Serialize(run, WebOptions);
        ConformanceRunResultV1 parsed = JsonSerializer.Deserialize<ConformanceRunResultV1>(json, WebOptions)!;

        parsed.OverallOutcome.ShouldBe(run.OverallOutcome);
        parsed.OverallClassification.ShouldBe(run.OverallClassification);
        parsed.SuiteId.ShouldBe(run.SuiteId);
        parsed.GeneratedAtUtc.ShouldBe(run.GeneratedAtUtc);
        parsed.Checks.Count.ShouldBe(run.Checks.Count);

        // CI consumes per-check pass/fail/classification: each must survive a serialization round trip
        // intact, including the embedded typed error on non-ready checks.
        foreach (ConformanceCheckResultV1 original in run.Checks)
        {
            ConformanceCheckResultV1 reparsed = parsed.Checks.Single(c => c.Check.Equals(original.Check));
            reparsed.Scenario.ShouldBe(original.Scenario);
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
    public void RunShouldExerciseTheReadyDegradedAndBlockedOutcomesAcrossChecks()
    {
        ConformanceRunResultV1 run = Run();

        ConformanceOutcome[] observed = run.Checks.Select(check => check.Outcome).Distinct().ToArray();

        // The synthetic fixture deliberately drives every closed outcome value: trust-bearing happy paths
        // (ready), a stale projection (degraded), fail-closed/unsupported/conflict paths (blocked), and the
        // cross-tenant denial that conformantly collapses to the hidden side-channel-equivalent shape
        // (unknown). A check observing the hidden 'unknown' shape is still conformant.
        observed.ShouldContain(ConformanceOutcome.Ready);
        observed.ShouldContain(ConformanceOutcome.Degraded);
        observed.ShouldContain(ConformanceOutcome.Blocked);
        observed.ShouldContain(ConformanceOutcome.Unknown);

        // Every emitted outcome must be a member of the closed vocabulary (no synonyms leaked into the run).
        observed.ShouldAllBe(outcome => ConformanceOutcome.All.Contains(outcome));
    }

    [Fact]
    public void NonReadyChecksMustCarryTypedErrorsAndReadyChecksMustNot()
    {
        ConformanceRunResultV1 run = Run();

        foreach (ConformanceCheckResultV1 check in run.Checks)
        {
            if (check.Outcome.Equals(ConformanceOutcome.Ready))
            {
                check.Error.ShouldBeNull();
            }
            else
            {
                check.Error.ShouldNotBeNull();
            }
        }
    }

    [Fact]
    public void RunAggregationShouldBeDeterministicAndDegradedWhenAllChecksConformWithAStaleScenario()
    {
        ConformanceRunResultV1 first = Run();
        ConformanceRunResultV1 second = Run();

        // No product-invariant or any other failure across a conformant run.
        first.Checks.ShouldAllBe(check => check.IsConformant);
        first.OverallClassification.ShouldBe(ConformanceFailureClassification.Conformant);

        // A conformant run that legitimately observed a stale (degraded) projection aggregates to degraded,
        // not ready and not blocked, and the aggregation is deterministic across runs.
        first.OverallOutcome.ShouldBe(ConformanceOutcome.Degraded);
        second.OverallOutcome.ShouldBe(first.OverallOutcome);
        second.OverallClassification.ShouldBe(first.OverallClassification);
    }

    [Fact]
    public void EveryEmittedFailureClassificationMustBelongToTheClosedVocabulary()
    {
        ConformanceRunResultV1 run = Run();

        run.Checks.Select(check => check.FailureClassification)
            .ShouldAllBe(classification => ConformanceFailureClassification.All.Contains(classification));
        ConformanceFailureClassification.All.ShouldContain(run.OverallClassification);
    }
}
