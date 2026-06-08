// <copyright file="ConversationTelemetryContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.Diagnostics;
using Hexalith.Conversations.Server.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Pins the Conversations telemetry metric contract before and after shared-helper adoption.
/// </summary>
public sealed class ConversationTelemetryContractTest
{
    [Fact]
    public void MetricContract_ShouldRemainStable()
    {
        ConversationTelemetryDefinitions.MeterName.ShouldBe("Hexalith.Conversations");

        AssertCounter(
            ConversationTelemetryDefinitions.CommandRejections,
            "conversations.command.rejections",
            "rejection_class",
            "operation_class",
            "retryable");
        AssertCounter(
            ConversationTelemetryDefinitions.TenantDenials,
            "conversations.tenant.denials",
            "denial_class",
            "operation_class",
            "retryable");
        AssertCounter(
            ConversationTelemetryDefinitions.PrivilegedAccess,
            "conversations.privileged.access",
            "access_class",
            "operation_class");
        AssertCounter(
            ConversationTelemetryDefinitions.ProjectionFreshness,
            "conversations.projection.freshness",
            "freshness_class",
            "lag_class");
        AssertCounter(
            ConversationTelemetryDefinitions.ProjectionRebuild,
            "conversations.projection.rebuild",
            "rebuild_class");
        AssertCounter(
            ConversationTelemetryDefinitions.PublicationFailures,
            "conversations.publication.failures",
            "failure_class");
        AssertCounter(
            ConversationTelemetryDefinitions.ConformanceOutcomes,
            "conversations.conformance.outcomes",
            "status_class",
            "gate_id",
            "blocking");
    }

    private static void AssertCounter(
        BoundedTelemetryCounterDefinition definition,
        string expectedName,
        params string[] expectedDimensionKeys)
    {
        definition.Name.ShouldBe(expectedName);
        definition.DimensionKeys.ShouldBe(expectedDimensionKeys);
    }
}
