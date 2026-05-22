// <copyright file="ConversationCitationContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies citation-copy contracts remain permission-safe and serialization friendly.
/// </summary>
public sealed class ConversationCitationContractTest
{
    [Fact]
    public void CitationDtoShouldSerializeOnlySafeCopyAndAnchorMetadata()
    {
        ConversationCitationV1 citation = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            "message:message-001",
            "Message",
            ContractSamples.EventMetadata.CommittedAt,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ContractSamples.Actor,
            ContractSamples.AuditEvidence,
            ContractSamples.FreshnessV1.ProjectionCursor,
            ContractSamples.FreshnessV1.LastAppliedEventPosition,
            "temporal:v1:pos:42",
            "Conversation evidence message:message-001 at 2026-05-18T11:00:00.0000000+00:00.",
            "Message evidence citation",
            "Copy message evidence citation",
            "Open stable temporal evidence link.");

        string json = JsonSerializer.Serialize(citation, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        JsonNode parsed = JsonNode.Parse(json)!;

        parsed["schemaVersion"]!.GetValue<int>().ShouldBe(1);
        parsed["tenantId"]!.GetValue<string>().ShouldBe("tenant:tenant-001");
        parsed["conversationId"]!.GetValue<string>().ShouldBe("conv:conversation-001");
        parsed["evidenceEntryId"]!.GetValue<string>().ShouldBe("message:message-001");
        parsed["projectionVersion"]!.GetValue<long>().ShouldBe(42);
        parsed["safeCopiedText"]!.GetValue<string>().ShouldContain("message:message-001");
        parsed["safeLabel"]!.GetValue<string>().ShouldBe("Message evidence citation");
        parsed["safeAccessibilityLabel"]!.GetValue<string>().ShouldBe("Copy message evidence citation");
        parsed["safeNextAction"]!.GetValue<string>().ShouldBe("Open stable temporal evidence link.");
        json.ShouldContain("\"auditEvidence\"");
        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("stream", Case.Insensitive);
        json.ShouldNotContain("snapshot", Case.Insensitive);
        json.ShouldNotContain("storage", Case.Insensitive);
        json.ShouldNotContain("provider", Case.Insensitive);
        json.ShouldNotContain("browserTitle", Case.Insensitive);
        json.ShouldNotContain("selectedText", Case.Insensitive);
        json.ShouldNotContain("clipboardSelection", Case.Insensitive);
        json.ShouldNotContain("Hello from the adopter.", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("displayName", Case.Insensitive);
    }

    [Fact]
    public void CitationResultHiddenShapeShouldNotCarryTargetIdentity()
    {
        ConversationCitationResult hidden = ConversationCitationResult.Hidden(ContractSamples.Version);

        hidden.Citation.ShouldBeNull();
        hidden.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        hidden.SafeNextAction.ShouldBe("The requested citation is not available.");

        string json = JsonSerializer.Serialize(hidden, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("message-001", Case.Insensitive);
        json.ShouldNotContain("conversation-001", Case.Insensitive);
        json.ShouldNotContain("tenant-001", Case.Insensitive);
    }
}
