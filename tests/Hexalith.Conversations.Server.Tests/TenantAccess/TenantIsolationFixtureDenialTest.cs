// <copyright file="TenantIsolationFixtureDenialTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>Checks published tenant-isolation fixture codes against the live access decision.</summary>
public sealed class TenantIsolationFixtureDenialTest
{
    [Fact]
    public void UnsafeProjectionFixturesMatchTheLiveTenantAccessDenial()
    {
        (string Scenario, ConversationTenantAccessDenialReason Reason)[] cases =
        [
            ("stale-projection", ConversationTenantAccessDenialReason.TenantAccessStale),
            ("unavailable-projection", ConversationTenantAccessDenialReason.TenantAccessUnavailable),
        ];

        foreach ((string scenarioToken, ConversationTenantAccessDenialReason reason) in cases)
        {
            TenantIsolationScenarioData scenario = TenantIsolationConformanceSeedData.Scenarios
                .Single(item => item.ScenarioToken == scenarioToken);
            ConversationTenantAccessDecision decision = ConversationTenantAccessDecision.Denied(
                ConversationTenantAccessRequirement.Read,
                new TenantId("tenant-001"),
                "caller-001",
                reason,
                isRetryable: true);

            scenario.ExpectedErrorCode.ShouldBe(decision.ToRejection(SchemaVersion.Current, "correlation-001", null).Code);
            ConversationError error = decision.ToSafeErrorResult(SchemaVersion.Current, "correlation-001")
                .Errors.ShouldHaveSingleItem();
            error.Code.ShouldBe(scenario.ExpectedErrorCode);
            error.IsRetryable.ShouldBeFalse();
        }
    }
}
