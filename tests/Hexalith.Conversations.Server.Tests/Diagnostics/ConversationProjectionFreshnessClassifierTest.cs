// <copyright file="ConversationProjectionFreshnessClassifierTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies the bounded freshness classifier maps trust states and reason codes correctly.
/// </summary>
public sealed class ConversationProjectionFreshnessClassifierTest
{
    [Fact]
    public void ClassifyTrustState_Current_ReturnsCurrent()
        => ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current)
            .ShouldBe(ConversationProjectionFreshnessClass.Current);

    [Fact]
    public void ClassifyTrustState_Stale_ReturnsStale()
        => ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Stale, ProjectionFreshnessReasonCode.StaleThresholdExceeded)
            .ShouldBe(ConversationProjectionFreshnessClass.Stale);

    [Fact]
    public void ClassifyTrustState_Rebuilding_ReturnsRebuilding()
        => ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Rebuilding, ProjectionFreshnessReasonCode.Rebuilding)
            .ShouldBe(ConversationProjectionFreshnessClass.Rebuilding);

    [Fact]
    public void ClassifyTrustState_Unavailable_ReturnsUnavailable()
        => ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Unavailable, ProjectionFreshnessReasonCode.Unavailable)
            .ShouldBe(ConversationProjectionFreshnessClass.Unavailable);

    [Fact]
    public void ClassifyTrustState_Forbidden_ReturnsUnavailable()
        => ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Forbidden, ProjectionFreshnessReasonCode.Forbidden)
            .ShouldBe(ConversationProjectionFreshnessClass.Unavailable);

    [Fact]
    public void ClassifyLag_StaleThresholdExceeded_ReturnsThresholdBreached()
        => ConversationProjectionFreshnessClassifier
            .ClassifyLag(ProjectionFreshnessReasonCode.StaleThresholdExceeded)
            .ShouldBe(ConversationProjectionLagClass.ThresholdBreached);

    [Fact]
    public void ClassifyLag_GapDetected_ReturnsCriticalLag()
        => ConversationProjectionFreshnessClassifier
            .ClassifyLag(ProjectionFreshnessReasonCode.GapDetected)
            .ShouldBe(ConversationProjectionLagClass.CriticalLag);

    [Fact]
    public void ClassifyLag_OutOfOrderEvent_ReturnsCriticalLag()
        => ConversationProjectionFreshnessClassifier
            .ClassifyLag(ProjectionFreshnessReasonCode.OutOfOrderEvent)
            .ShouldBe(ConversationProjectionLagClass.CriticalLag);

    [Fact]
    public void ClassifyLag_Current_ReturnsWithinThreshold()
        => ConversationProjectionFreshnessClassifier
            .ClassifyLag(ProjectionFreshnessReasonCode.Current)
            .ShouldBe(ConversationProjectionLagClass.WithinThreshold);

    [Fact]
    public void ClassifyTrustState_Redacted_ReturnsCurrent()
        => ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Redacted, ProjectionFreshnessReasonCode.Redacted)
            .ShouldBe(ConversationProjectionFreshnessClass.Current);

    [Fact]
    public void ClassifyLag_Forbidden_ReturnsNone()
        => ConversationProjectionFreshnessClassifier
            .ClassifyLag(ProjectionFreshnessReasonCode.Forbidden)
            .ShouldBe(ConversationProjectionLagClass.None);
}
