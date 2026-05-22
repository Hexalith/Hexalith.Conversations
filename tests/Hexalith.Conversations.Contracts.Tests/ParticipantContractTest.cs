// <copyright file="ParticipantContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
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

    // Provider-correlation terms are legitimate on the command surface (commands carry provider
    // correlation metadata) but MUST NOT appear in durable events. The command surface scan applies
    // a stricter Party-personal-data + provider-payload subset.
    private static readonly string[] ForbiddenCommandTerms =
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
    ];

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
            {"metadata":{"schemaVersion":1,"eventId":"event-participant-001","eventType":"ParticipantAdded","tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","correlationId":"correlation-001","occurredAt":"2026-05-18T11:00:00+00:00","actorPartyId":"party:party-actor","causationId":"causation-001","deduplicationKey":"tenant:tenant-001|conv:conversation-001|event-participant-001|1"},"participantPartyId":"party:party-participant","participantType":"AIAgent","participantRole":"Facilitator"}
            """,
            added);

        AssertNoForbiddenDurableTerms(json);
    }

    /// <summary>
    /// Forbidden durable terms must also be absent from serialized command JSON, not only from the durable event,
    /// so that command-time leakage cannot regress without contract test coverage. Note: this command intentionally
    /// carries provider correlation, so the test excludes correlation-only forbidden terms that are legitimate
    /// on the command surface but must remain absent from the durable event.
    /// </summary>
    [Fact]
    public void AddParticipantCommandShouldNotContainPartyPersonalDataTerms()
    {
        AddParticipantCommand command = new(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            ContractSamples.Participant,
            ParticipantType.Human,
            ParticipantRole.Member,
            ProviderCorrelation: null);

        string json = JsonSerializer.Serialize(command, WebOptions);
        foreach (string forbidden in ForbiddenCommandTerms)
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

    /// <summary>
    /// The .NET property names for <see cref="ParticipantType.AiAgent"/> and <see cref="ParticipantType.Llm"/>
    /// diverge from their canonical wire values (<c>"AIAgent"</c> and <c>"LLM"</c>). Adopters reflecting over
    /// the property name MUST hit a typed rejection rather than silently producing an invalid value.
    /// </summary>
    [Theory]
    [InlineData("AiAgent")]
    [InlineData("Llm")]
    [InlineData("aiagent")]
    [InlineData("llm")]
    [InlineData("AIAGENT")]
    public void ParticipantTypeParseShouldRejectDivergentDotNetNameAndCaseVariants(string value)
        => Should.Throw<ArgumentException>(() => ParticipantType.Parse(value));

    /// <summary>
    /// Every public static <see cref="ParticipantType"/> property must be reachable via <see cref="ParticipantType.Parse"/>.
    /// Catches a future-static-value-added-but-not-registered-in-KnownTypes regression at contract-test time.
    /// </summary>
    [Fact]
    public void EveryStaticParticipantTypeShouldBeParseable()
    {
        IEnumerable<ParticipantType> staticTypes = typeof(ParticipantType)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(ParticipantType))
            .Select(p => (ParticipantType)p.GetValue(null)!);

        foreach (ParticipantType type in staticTypes)
        {
            ParticipantType.Parse(type.Value).ShouldBe(type);
        }
    }

    /// <summary>
    /// Every public static <see cref="ParticipantRole"/> property must be reachable via <see cref="ParticipantRole.Parse"/>.
    /// </summary>
    [Fact]
    public void EveryStaticParticipantRoleShouldBeParseable()
    {
        IEnumerable<ParticipantRole> staticRoles = typeof(ParticipantRole)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(ParticipantRole))
            .Select(p => (ParticipantRole)p.GetValue(null)!);

        foreach (ParticipantRole role in staticRoles)
        {
            ParticipantRole.Parse(role.Value).ShouldBe(role);
        }
    }

    /// <summary>
    /// <see cref="JsonNode.DeepEquals(JsonNode?, JsonNode?)"/> is value-equivalent across token kinds, so it
    /// would accept <c>"1"</c> where a strict integer is expected. Pin the strict integer wire shape directly
    /// for sensitive fields.
    /// </summary>
    [Fact]
    public void ParticipantAddedJsonShouldContainStrictIntegerSchemaVersion()
    {
        ParticipantAdded added = new(
            ContractSamples.ParticipantEventMetadata,
            ContractSamples.Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

        string json = JsonSerializer.Serialize(added, WebOptions);
        json.ShouldContain("\"schemaVersion\":1");
        json.ShouldNotContain("\"schemaVersion\":\"1\"");
    }

    private static void AssertNoForbiddenDurableTerms(string json)
    {
        foreach (string forbidden in ForbiddenDurableTerms)
        {
            json.ShouldNotContain(forbidden, Case.Insensitive);
        }
    }

    private static void AssertJsonEquivalent(string expected, object value)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(value, value.GetType(), WebOptions));
    }
}
