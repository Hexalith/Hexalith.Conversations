// <copyright file="CoreFixtureContentSafetyTest.cs" company="ITANEO">
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
/// Verifies the synthetic CORE fixture and the machine-readable conformance run result are content-safe,
/// preserve side-channel safety, and never leak cross-tenant poison sentinels into authorized-tenant output.
/// </summary>
public sealed class CoreFixtureContentSafetyTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    // Free-text and protected-value disclosure fragments. Closed-vocabulary machine tokens such as
    // 'projection-freshness' or 'error-envelope' are intentionally NOT scanned here (the closed
    // ConformanceCheck vocabulary deliberately includes 'error-envelope'); per the story the scan targets
    // free-text protected-value disclosure and infrastructure terms, not safe machine identifiers. The
    // fragments below are specific enough that they cannot appear inside any legitimate closed token.
    private static readonly string[] ForbiddenFragments =
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

    [Fact]
    public void ConformanceRunResultShouldNotLeakPoisonSentinels()
    {
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();
        string json = JsonSerializer.Serialize(new AdopterConformanceSuite(fixture).Run(), WebOptions);

        foreach (string sentinel in fixture.PoisonSentinelValues)
        {
            json.ShouldNotContain(sentinel, Case.Insensitive);
        }
    }

    [Fact]
    public void ConformanceRunResultShouldNotLeakProtectedIdentifiersOrInfrastructureTerms()
    {
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();
        string json = JsonSerializer.Serialize(new AdopterConformanceSuite(fixture).Run(), WebOptions);

        // Authorized-tenant protected identifiers must not appear in the machine-readable run result.
        json.ShouldNotContain(fixture.AuthorizedTenantId.Value, Case.Insensitive);
        json.ShouldNotContain(fixture.PoisonTenantId.Value, Case.Insensitive);
        json.ShouldNotContain(fixture.HappyPathDetail.ConversationId.Value, Case.Insensitive);

        foreach (string fragment in ForbiddenFragments)
        {
            json.ShouldNotContain(fragment, Case.Insensitive);
        }
    }

    [Fact]
    public void TypedFailureCasesShouldRemainContentSafeOnTheWire()
    {
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();

        foreach (ConversationConformanceTypedFailure failure in new[]
        {
            fixture.UnsupportedSchemaFailure,
            fixture.IdempotencyConflictFailure,
            fixture.CrossTenantDenialFailure,
            fixture.SanitizedErrorFailure,
        })
        {
            string json = JsonSerializer.Serialize(failure.Error, WebOptions);
            foreach (string fragment in ForbiddenFragments)
            {
                json.ShouldNotContain(fragment, Case.Insensitive);
            }

            foreach (string sentinel in fixture.PoisonSentinelValues)
            {
                json.ShouldNotContain(sentinel, Case.Insensitive);
            }
        }
    }

    [Fact]
    public void CrossTenantDenialShouldUseHiddenShapeAndNotRevealExistence()
    {
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();

        // The cross-tenant denial must collapse to the hidden/unavailable typed shape used elsewhere
        // (aggregate_not_found), never distinguishing unauthorized from nonexistent and never disclosing
        // the poison tenant or conversation identity.
        fixture.CrossTenantDenialFailure.Error.Code.ShouldBe(Contracts.Errors.ConversationErrorCode.AggregateNotFound);

        string json = JsonSerializer.Serialize(fixture.CrossTenantDenialFailure.Error, WebOptions);
        json.ShouldNotContain(fixture.PoisonTenantId.Value, Case.Insensitive);
        json.ShouldNotContain(fixture.PoisonProjection.Detail.ConversationId.Value, Case.Insensitive);
    }

    [Fact]
    public void FixtureShouldBeMarkedSyntheticAndDeterministic()
    {
        ConversationConformanceCoreSeedData first = ConversationConformanceCoreFixtures.Create();
        ConversationConformanceCoreSeedData second = ConversationConformanceCoreFixtures.Create();

        first.SyntheticDataMarker.ShouldBe(ConversationConformanceCoreFixtures.SyntheticDataMarker);
        first.SyntheticDataMarker.ShouldContain("synthetic");

        // Deterministic generation: the serialized authorized happy-path projection is stable across loads.
        JsonSerializer.Serialize(first.HappyPathDetail, WebOptions)
            .ShouldBe(JsonSerializer.Serialize(second.HappyPathDetail, WebOptions));
    }

    [Fact]
    public void PoisonProjectionShouldCarrySentinelsThatNeverAppearInAuthorizedSurfaces()
    {
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();

        string poisonJson = JsonSerializer.Serialize(fixture.PoisonProjection, WebOptions);
        fixture.PoisonSentinelValues.ShouldNotBeEmpty();
        fixture.PoisonSentinelValues.Any(sentinel => poisonJson.Contains(sentinel, StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();

        string authorizedJson = JsonSerializer.Serialize(fixture.HappyPathDetail, WebOptions)
            + JsonSerializer.Serialize(fixture.HappyPathSummary, WebOptions)
            + JsonSerializer.Serialize(fixture.StaleDetail, WebOptions);

        foreach (string sentinel in fixture.PoisonSentinelValues)
        {
            authorizedJson.ShouldNotContain(sentinel, Case.Insensitive);
        }
    }

    [Fact]
    public void ConformanceRunResultShouldPreserveClosedTraceabilityTokensContainingTenantAndPartySegments()
    {
        // Story 4.4 lesson regression guard: closed release-gate and precondition machine identifiers
        // legitimately contain 'tenant-' and 'party'/'participant' segments. They are safe machine tokens,
        // not free-text protected values, so the content-safety pipeline must NOT strip or reject them.
        // Their presence on the wire is what keeps Story 5.10 aggregation traceable.
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();
        string json = JsonSerializer.Serialize(new AdopterConformanceSuite(fixture).Run(), WebOptions);

        json.ShouldContain("release-gate-tenant-isolation");
        json.ShouldContain("participant-identity-validation");
    }

    [Fact]
    public void CrossTenantDenialShouldNotCarryClientActionThatDistinguishesExistence()
    {
        // The hidden/unavailable shape must collapse to 'HideOrRefresh' so an unauthorized caller cannot
        // distinguish a forbidden tenant/conversation from a nonexistent one via the typed client action.
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();

        fixture.CrossTenantDenialFailure.Error.ClientAction
            .ShouldBe(ConversationErrorClientAction.HideOrRefresh);
        fixture.CrossTenantDenialFailure.Error.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    public void EveryTypedFailureFixtureScenarioShouldExposeOnlySafeStructuredFields()
    {
        ConversationConformanceCoreSeedData fixture = ConversationConformanceCoreFixtures.Create();

        foreach (ConversationConformanceTypedFailure failure in new[]
        {
            fixture.UnsupportedSchemaFailure,
            fixture.IdempotencyConflictFailure,
            fixture.CrossTenantDenialFailure,
            fixture.SanitizedErrorFailure,
        })
        {
            failure.Error.Documentation.ShouldNotBeNull();
            failure.Error.Documentation!.Scheme.ShouldBe(Uri.UriSchemeHttps);
            failure.Error.SafeMessage.ShouldNotBeNullOrWhiteSpace();
            failure.Error.CorrelationId.ShouldNotBeNullOrWhiteSpace();

            string json = JsonSerializer.Serialize(failure.Error, WebOptions);
            json.ShouldNotContain(fixture.AuthorizedTenantId.Value, Case.Insensitive);
            json.ShouldNotContain(fixture.PoisonTenantId.Value, Case.Insensitive);
        }
    }
}
