// <copyright file="ParticipantContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies participant command and event contracts preserve stable Party attribution only.
/// </summary>
public sealed class ParticipantContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// AddParticipantCommand serializes stable Party identity, type, role, and command metadata.
    /// </summary>
    [Fact]
    public void AddParticipantCommandShouldSerializeStableAttributionShape()
    {
        AddParticipantCommand command = new(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            ContractSamples.Participant,
            ParticipantType.Human,
            ParticipantRole.Member,
            ContractSamples.ProviderCorrelation);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"conversationId":"conv:conversation-001","participantPartyId":"party:party-participant","participantType":"Human","participantRole":"Member","providerCorrelation":{"providerName":"provider-a","providerType":"assistant","metadataSchemaVersion":1,"providerSessionReference":"session-reference","providerResponseReference":"response-reference","extensionData":{"region":"eu"}}}
            """,
            command);
    }

    /// <summary>
    /// ParticipantAdded serializes durable event data without provider correlation or Party personal data.
    /// </summary>
    [Fact]
    public void ParticipantAddedShouldSerializeDurableStableAttributionOnly()
    {
        ParticipantAdded added = new(
            ContractSamples.ParticipantEventMetadata,
            ContractSamples.Participant,
            ParticipantType.AiAgent,
            ParticipantRole.Facilitator);

        string json = JsonSerializer.Serialize(added, WebOptions);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"eventId":"event-participant-001","eventType":"ParticipantAdded","tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","correlationId":"correlation-001","committedAt":"2026-05-18T11:00:00+00:00","actorPartyId":"party:party-actor","causationId":"causation-001"},"participantPartyId":"party:party-participant","participantType":"AIAgent","participantRole":"Facilitator"}
            """,
            added);

        foreach (string forbidden in ForbiddenDurableTerms)
        {
            json.ShouldNotContain(forbidden, Case.Insensitive);
        }
    }

    /// <summary>
    /// Participant vocabularies round-trip through System.Text.Json and reject unknown values.
    /// </summary>
    [Fact]
    public void ParticipantVocabularyShouldBeClosedAndJsonFriendly()
    {
        JsonSerializer.Deserialize<ParticipantType>("\"Human\"", WebOptions).ShouldBe(ParticipantType.Human);
        JsonSerializer.Deserialize<ParticipantType>("\"AIAgent\"", WebOptions).ShouldBe(ParticipantType.AiAgent);
        JsonSerializer.Deserialize<ParticipantType>("\"LLM\"", WebOptions).ShouldBe(ParticipantType.Llm);
        JsonSerializer.Deserialize<ParticipantRole>("\"Member\"", WebOptions).ShouldBe(ParticipantRole.Member);

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ParticipantType>("\"ProviderSession\"", WebOptions));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ParticipantRole>("\"DisplayName\"", WebOptions));
    }

    private static readonly string[] ForbiddenDurableTerms =
    [
        "displayName",
        "email",
        "phone",
        "personDetails",
        "organizationDetails",
        "contact",
        "providerPayload",
        "prompt",
        "rawProblem",
        "providerSessionReference",
        "providerResponseReference",
        "providerCorrelation",
    ];

    private static void AssertJsonEquivalent(string expected, object value)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(value, value.GetType(), WebOptions));
    }
}
