// <copyright file="BuyerAcceptanceDemoFixtureTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Tests.Testing;

/// <summary>
/// Verifies reusable buyer acceptance demo fixtures are deterministic and synthetic.
/// </summary>
public sealed class BuyerAcceptanceDemoFixtureTest
{
    [Fact]
    public void DemoFixturesShouldBeDeterministicAndCoverCanonicalStates()
    {
        BuyerAcceptanceDemoSeedData first = BuyerAcceptanceDemoFixtures.Create();
        BuyerAcceptanceDemoSeedData second = BuyerAcceptanceDemoFixtures.Create();

        JsonSerializer.Serialize(first.Scenario).ShouldBe(JsonSerializer.Serialize(second.Scenario));
        first.GeneratedAtUtc.ShouldBe(second.GeneratedAtUtc);
        first.AuthorizedTenantId.ShouldBe(second.AuthorizedTenantId);
        first.PoisonTenantId.ShouldNotBe(first.AuthorizedTenantId);
        first.PoisonSentinelValues.ShouldNotBeEmpty();

        first.Scenario.SyntheticDataMarker.ShouldBe(BuyerAcceptanceDemoFixtures.SyntheticDataMarker);
        first.Scenario.Fixtures.Select(fixture => fixture.FixtureKind).ShouldBe(
            BuyerAcceptanceDemoFixtureKind.Canonical,
            ignoreOrder: false);
        first.Scenario.Steps.Select(step => step.StepId).ShouldBeUnique();
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.Find);
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.ReadDetail);
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.RedactionAudit);
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.CitationCopy);
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.TemporalReconstruction);
        first.Scenario.Steps.Single(step => step.StepKind == BuyerAcceptanceDemoStepKind.TemporalReconstruction)
            .TemporalCursor.ShouldBe("temporal:v1:pos:0000000003:projection:0000000100");
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.CommandMetadata);
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.Verification);
        first.Scenario.Steps.ShouldContain(step => step.StepKind == BuyerAcceptanceDemoStepKind.CrossTenantDenial);

        first.AuthorizedProjections.Select(item => item.Summary.ConversationId).ShouldBeUnique();
        first.AuthorizedProjections.ShouldAllBe(item => item.Summary.TenantId == first.AuthorizedTenantId);
        first.AuthorizedProjections.ShouldAllBe(item => item.Detail.TenantId == first.AuthorizedTenantId);
        first.AuthorizedProjections.ShouldContain(item =>
            item.Detail.EvidenceEntries.Any(entry => entry.VisibleText == "[redacted]"));
        first.AuthorizedProjections.ShouldContain(item =>
            item.Detail.Freshness.FreshnessState == ProjectionTrustState.Stale);
        first.AuthorizedProjections.ShouldContain(item =>
            item.Detail.TrustPosture.CitationAvailability == ConversationCitationAvailability.Unavailable);
        first.AuthorizedProjections.ShouldContain(item =>
            item.Detail.TrustPosture.ParticipantResolutionState == ProjectionTrustState.Unavailable);
        first.AuthorizedProjections.ShouldContain(item =>
            item.Detail.TrustPosture.CommandEligibility.Any(command => command.AvailabilityState == ProjectionTrustState.Unavailable));
        first.VerificationPass.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.Passed);
        first.VerificationFailure.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
    }

    [Fact]
    public void DemoFixturePublicScenarioShouldNotExposeCrossTenantPoisonSentinels()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();

        string scenarioJson = JsonSerializer.Serialize(seed.Scenario);
        string authorizedJson = JsonSerializer.Serialize(seed.AuthorizedProjections);

        foreach (string sentinel in seed.PoisonSentinelValues)
        {
            scenarioJson.ShouldNotContain(sentinel, Case.Insensitive);
            authorizedJson.ShouldNotContain(sentinel, Case.Insensitive);
        }

        seed.PoisonProjection.Summary.TenantId.ShouldBe(seed.PoisonTenantId);
        seed.PoisonProjection.Detail.Label.ShouldNotBeNull();
        seed.PoisonProjection.Detail.Label.ShouldContain("POISON-SENTINEL", Case.Insensitive);
    }
}
