// <copyright file="ConversationProjectAssignmentContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies the additive conversation project assignment command and event contracts.
/// </summary>
public sealed class ConversationProjectAssignmentContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly ProjectId OldProject = new("project-old");
    private static readonly ProjectId NewProject = new("project-new");

    private static readonly string[] ForbiddenPayloadTerms =
    [
        "transcript",
        "prompt",
        "messageBody",
        "displayName",
        "contact",
        "fileContent",
        "token",
        "claim",
        "eventStore",
        "stream",
        "dapr",
        "exception",
        "providerPayload",
    ];

    [Fact]
    public void ReassignConversationProjectCommandShouldSerializeExplicitAssignTarget()
    {
        ReassignConversationProjectCommand command = new(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            new ConversationProjectAssignment(ConversationProjectAssignmentOperation.Assign, NewProject),
            ExpectedCurrentProjectId: OldProject);

        string json = JsonSerializer.Serialize(command, WebOptions);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"conversationId":"conv:conversation-001","target":{"operation":"Assign","projectId":"project:project-new"},"expectedCurrentProjectId":"project:project-old","callerMetadata":null}
            """,
            command);
        AssertNoForbiddenPayloadTerms(json);
    }

    [Fact]
    public void ReassignConversationProjectCommandShouldSerializeExplicitClearTarget()
    {
        ReassignConversationProjectCommand command = new(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            new ConversationProjectAssignment(ConversationProjectAssignmentOperation.Clear),
            ExpectedCurrentProjectId: OldProject);

        string json = JsonSerializer.Serialize(command, WebOptions);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"conversationId":"conv:conversation-001","target":{"operation":"Clear","projectId":null},"expectedCurrentProjectId":"project:project-old","callerMetadata":null}
            """,
            command);
        AssertNoForbiddenPayloadTerms(json);
    }

    [Fact]
    public void ConversationProjectChangedShouldSerializePreviousAndCurrentProjectIds()
    {
        ConversationProjectChanged changed = new(
            ContractSamples.ProjectChangedEventMetadata,
            OldProject,
            NewProject);

        string json = JsonSerializer.Serialize(changed, WebOptions);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"eventId":"event-project-changed-001","eventType":"ConversationProjectChanged","tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","correlationId":"correlation-001","occurredAt":"2026-05-18T11:00:00+00:00","actorPartyId":"party:party-actor","causationId":"causation-001","deduplicationKey":"tenant:tenant-001|conv:conversation-001|event-project-changed-001|1"},"previousProjectId":"project:project-old","currentProjectId":"project:project-new","changedAt":"2026-05-18T11:00:00+00:00"}
            """,
            changed);
        AssertNoForbiddenPayloadTerms(json);
    }

    [Fact]
    public void ConversationProjectChangedShouldSerializeExplicitClearAsNullCurrentProjectId()
    {
        ConversationProjectChanged changed = new(
            ContractSamples.ProjectChangedEventMetadata,
            OldProject,
            CurrentProjectId: null);

        string json = JsonSerializer.Serialize(changed, WebOptions);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"eventId":"event-project-changed-001","eventType":"ConversationProjectChanged","tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","correlationId":"correlation-001","occurredAt":"2026-05-18T11:00:00+00:00","actorPartyId":"party:party-actor","causationId":"causation-001","deduplicationKey":"tenant:tenant-001|conv:conversation-001|event-project-changed-001|1"},"previousProjectId":"project:project-old","currentProjectId":null,"changedAt":"2026-05-18T11:00:00+00:00"}
            """,
            changed);
        AssertNoForbiddenPayloadTerms(json);
    }

    [Fact]
    public void ProjectAssignmentVocabularyShouldBeClosedAndParseable()
    {
        JsonSerializer.Deserialize<ConversationProjectAssignmentOperation>("\"Assign\"", WebOptions)
            .ShouldBe(ConversationProjectAssignmentOperation.Assign);
        JsonSerializer.Deserialize<ConversationProjectAssignmentOperation>("\"Clear\"", WebOptions)
            .ShouldBe(ConversationProjectAssignmentOperation.Clear);

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ConversationProjectAssignmentOperation>("\"Unassign\"", WebOptions));
        Should.Throw<ArgumentException>(() => ConversationProjectAssignmentOperation.Parse("assign"));
    }

    [Fact]
    public void CommandAndEventTypeVocabulariesShouldIncludeProjectAssignmentTypes()
    {
        ConversationCommandType.Parse("ReassignConversationProjectCommand")
            .ShouldBe(ConversationCommandType.ReassignConversationProjectCommand);
        ConversationEventType.Parse("ConversationProjectChanged")
            .ShouldBe(ConversationEventType.ConversationProjectChanged);
    }

    [Fact]
    public void EveryStaticProjectAssignmentOperationShouldBeParseable()
    {
        IEnumerable<ConversationProjectAssignmentOperation> operations = typeof(ConversationProjectAssignmentOperation)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(ConversationProjectAssignmentOperation))
            .Select(p => (ConversationProjectAssignmentOperation)p.GetValue(null)!);

        foreach (ConversationProjectAssignmentOperation operation in operations)
        {
            ConversationProjectAssignmentOperation.Parse(operation.Value).ShouldBe(operation);
        }
    }

    private static void AssertNoForbiddenPayloadTerms(string json)
    {
        foreach (string forbidden in ForbiddenPayloadTerms)
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
