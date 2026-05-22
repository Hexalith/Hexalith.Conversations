// <copyright file="SensitivityContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies public sensitivity-marking contracts stay content-safe and serializable.
/// </summary>
public sealed class SensitivityContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// A mark-sensitive command carries only governed metadata, target identity, category, and policy attribution.
    /// </summary>
    [Fact]
    public void MarkSensitiveCommandShouldRoundTripWithoutRawContent()
    {
        MarkConversationContentSensitiveCommand command = ContractSamples.SensitivityCommand;

        string json = JsonSerializer.Serialize(command, WebOptions);
        MarkConversationContentSensitiveCommand? roundTrip =
            JsonSerializer.Deserialize<MarkConversationContentSensitiveCommand>(json, WebOptions);

        roundTrip.ShouldNotBeNull();
        roundTrip.Target.Kind.ShouldBe(GovernedTargetKind.Message);
        roundTrip.Target.MessageId.ShouldBe(ContractSamples.Message);
        roundTrip.Category.ShouldBe(SensitivityCategory.Restricted);
        json.ShouldContain("\"category\":\"Restricted\"", Case.Sensitive);
        json.ShouldNotContain("Hello", Case.Insensitive);
        json.ShouldNotContain("provider", Case.Insensitive);
        json.ShouldNotContain("storage", Case.Insensitive);
    }

    /// <summary>
    /// Content segment targets may carry only bounded opaque references.
    /// </summary>
    [Fact]
    public void SegmentTargetShouldRejectUnsafeContentReferences()
    {
        Should.Throw<ArgumentException>(() => new MarkConversationContentSensitiveCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.ContentSegment, SegmentReference: "selected text"),
            SensitivityCategory.Sensitive,
            "sensitivity-policy-standard",
            "customer-request",
            ContractSamples.GovernanceTimestamp));

        MarkConversationContentSensitiveCommand command = new(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.ContentSegment, SegmentReference: "segment-001"),
            SensitivityCategory.Sensitive,
            "sensitivity-policy-standard",
            "customer-request",
            ContractSamples.GovernanceTimestamp);

        command.Target.SegmentReference.ShouldBe("segment-001");
    }

    /// <summary>
    /// Public sensitivity events and results omit raw rationale/policy details from ToString output.
    /// </summary>
    [Fact]
    public void SensitivityRecordsShouldKeepToStringContentSafe()
    {
        ConversationContentMarkedSensitive marked = new(
            ContractSamples.SensitivityMarkedEventMetadata,
            ContractSamples.SensitivityMessageTarget,
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            ContractSamples.AuditEvidence);

        marked.ToString().ShouldNotContain("customer-request", Case.Insensitive);
        marked.ToString().ShouldNotContain("sensitivity-policy-standard", Case.Insensitive);
        ContractSamples.SensitivityCommand.ToString().ShouldNotContain("customer-request", Case.Insensitive);
        ContractSamples.SensitivityCommand.ToString().ShouldNotContain("sensitivity-policy-standard", Case.Insensitive);
    }
}
