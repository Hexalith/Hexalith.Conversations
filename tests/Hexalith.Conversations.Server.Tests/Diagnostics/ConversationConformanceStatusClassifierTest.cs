// <copyright file="ConversationConformanceStatusClassifierTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Server.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies <see cref="ConversationConformanceStatusClassifier"/> maps all conformance inputs to the correct bounded status class.
/// </summary>
public sealed class ConversationConformanceStatusClassifierTest
{
    [Fact]
    public void Classify_ConformantReady_ReturnsPass()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant);
        result.ShouldBe(ConversationConformanceStatusClass.Pass);
    }

    [Fact]
    public void Classify_ConformantBlocked_ReturnsPass()
    {
        // Conformant + Blocked = fail-closed observed correctly per contract
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant);
        result.ShouldBe(ConversationConformanceStatusClass.Pass);
    }

    [Fact]
    public void Classify_ConformantDegraded_ReturnsStaleEvidence()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Degraded,
            ConformanceFailureClassification.Conformant);
        result.ShouldBe(ConversationConformanceStatusClass.StaleEvidence);
    }

    [Fact]
    public void Classify_ConformantUnknown_ReturnsUnknownAccepted()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant);
        result.ShouldBe(ConversationConformanceStatusClass.UnknownAccepted);
    }

    [Fact]
    public void Classify_ProductInvariant_ReturnsFail()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.ProductInvariant);
        result.ShouldBe(ConversationConformanceStatusClass.Fail);
    }

    [Fact]
    public void Classify_Infrastructure_ReturnsInfrastructureFailure()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Infrastructure);
        result.ShouldBe(ConversationConformanceStatusClass.InfrastructureFailure);
    }

    [Fact]
    public void Classify_UnavailableDependency_ReturnsInfrastructureFailure()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.UnavailableDependency);
        result.ShouldBe(ConversationConformanceStatusClass.InfrastructureFailure);
    }

    [Fact]
    public void Classify_ExecutionClassification_ReturnsExecutionFailure()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Execution);
        result.ShouldBe(ConversationConformanceStatusClass.ExecutionFailure);
    }

    [Fact]
    public void Classify_Configuration_ReturnsExecutionFailure()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.Classify(
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Configuration);
        result.ShouldBe(ConversationConformanceStatusClass.ExecutionFailure);
    }

    [Fact]
    public void ClassifyGate_Pass_ReturnsPass()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.ClassifyGate(
            ReleaseGateStatus.Pass);
        result.ShouldBe(ConversationConformanceStatusClass.Pass);
    }

    [Fact]
    public void ClassifyGate_Fail_ReturnsFail()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.ClassifyGate(
            ReleaseGateStatus.Fail);
        result.ShouldBe(ConversationConformanceStatusClass.Fail);
    }

    [Fact]
    public void ClassifyGate_Waived_ReturnsWaived()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.ClassifyGate(
            ReleaseGateStatus.Waived);
        result.ShouldBe(ConversationConformanceStatusClass.Waived);
    }

    [Fact]
    public void ClassifyGate_UnknownAccepted_ReturnsUnknownAccepted()
    {
        ConversationConformanceStatusClass result = ConversationConformanceStatusClassifier.ClassifyGate(
            ReleaseGateStatus.UnknownAccepted);
        result.ShouldBe(ConversationConformanceStatusClass.UnknownAccepted);
    }
}
