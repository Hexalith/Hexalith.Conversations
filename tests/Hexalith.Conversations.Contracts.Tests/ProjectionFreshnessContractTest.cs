// <copyright file="ProjectionFreshnessContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies Story 1.7 freshness metadata remains explicit and fail-closed.
/// </summary>
public sealed class ProjectionFreshnessContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset LastApplied = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Generated = new(2026, 5, 20, 9, 0, 5, TimeSpan.Zero);

    /// <summary>
    /// The public freshness contract carries the complete v1 trust metadata shape.
    /// </summary>
    [Fact]
    public void ProjectionFreshnessV1ShouldExposeCompleteFreshnessMetadata()
    {
        ProjectionFreshnessV1 freshness = CurrentFreshness();

        string json = JsonSerializer.Serialize(freshness, WebOptions);

        json.ShouldContain("\"projectionContractSchemaVersion\":1");
        json.ShouldContain("\"projectionCursor\":\"pos:0000000042\"");
        json.ShouldContain("\"lastAppliedEventPosition\":42");
        json.ShouldContain("\"lastAppliedEventTimestamp\":\"2026-05-20T09:00:00+00:00\"");
        json.ShouldContain("\"projectionGeneratedAt\":\"2026-05-20T09:00:05+00:00\"");
        json.ShouldContain("\"lagDuration\":\"00:00:05\"");
        json.ShouldContain("\"isStale\":false");
        json.ShouldContain("\"freshnessState\":\"Current\"");
        json.ShouldContain("\"reasonCode\":\"current\"");
        freshness.AllowsTrustBearingDecision().ShouldBeTrue();
    }

    /// <summary>
    /// Unknown public values are rejected by the closed vocabulary and therefore cannot grant trust.
    /// </summary>
    [Fact]
    public void UnknownFreshnessStateOrReasonCodeShouldFailClosed()
    {
        Should.Throw<ArgumentException>(() => ProjectionTrustState.Parse("FreshEnough"));
        Should.Throw<ArgumentException>(() => ProjectionFreshnessReasonCode.Parse("fresh_enough"));
    }

    /// <summary>
    /// Missing required fields must not deserialize into a trust-bearing current state.
    /// </summary>
    [Fact]
    public void MissingRequiredFreshnessFieldsShouldFailClosed()
    {
        string json = """
        {
          "projectionContractSchemaVersion": 1,
          "projectionCursor": "pos:0000000042",
          "lastAppliedEventTimestamp": "2026-05-20T09:00:00+00:00",
          "projectionGeneratedAt": "2026-05-20T09:00:05+00:00",
          "isStale": false,
          "freshnessState": "Current",
          "reasonCode": "current"
        }
        """;

        Should.Throw<Exception>(() =>
        {
            _ = JsonSerializer.Deserialize<ProjectionFreshnessV1>(json, WebOptions);
        });
    }

    /// <summary>
    /// Extra caller-supplied fields do not upgrade non-current server freshness.
    /// </summary>
    [Fact]
    public void UnknownJsonFieldsShouldNotUpgradeFreshnessTrust()
    {
        string json = """
        {
          "projectionContractSchemaVersion": 1,
          "projectionCursor": "pos:0000000042",
          "lastAppliedEventPosition": 42,
          "lastAppliedEventTimestamp": "2026-05-20T09:00:00+00:00",
          "projectionGeneratedAt": "2026-05-20T09:30:00+00:00",
          "lagDuration": "00:30:00",
          "isStale": true,
          "freshnessState": "Stale",
          "reasonCode": "stale_threshold_exceeded",
          "callerSuppliedFreshness": "Current"
        }
        """;

        ProjectionFreshnessV1? freshness = JsonSerializer.Deserialize<ProjectionFreshnessV1>(json, WebOptions);

        freshness.ShouldNotBeNull();
        freshness.AllowsTrustBearingDecision().ShouldBeFalse();
        freshness.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
    }

    /// <summary>
    /// Contradictory current metadata is rejected before it can enable governed decisions.
    /// </summary>
    [Fact]
    public void ContradictoryCurrentFreshnessShouldFailClosed()
    {
        Should.Throw<ArgumentException>(() => new ProjectionFreshnessV1(
            SchemaVersion.Current,
            "pos:0000000042",
            42,
            LastApplied,
            Generated,
            TimeSpan.FromSeconds(5),
            IsStale: true,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current));

        Should.Throw<ArgumentException>(() => new ProjectionFreshnessV1(
            SchemaVersion.Current,
            "pos:0000000042",
            42,
            LastApplied,
            Generated,
            TimeSpan.FromSeconds(5),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.MetadataContradictory));
    }

    private static ProjectionFreshnessV1 CurrentFreshness()
        => new(
            SchemaVersion.Current,
            "pos:0000000042",
            42,
            LastApplied,
            Generated,
            TimeSpan.FromSeconds(5),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);
}
