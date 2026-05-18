// <copyright file="ContractSerializationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies JSON compatibility for public contract DTOs.
/// </summary>
public sealed class ContractSerializationTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Ensures every public sample contract survives System.Text.Json web-default round-trip serialization.
    /// </summary>
    [Fact]
    public void PublicContractsShouldRoundTripWithSystemTextJsonWebDefaults()
    {
        foreach (object sample in ContractSamples.AllContracts)
        {
            string json = JsonSerializer.Serialize(sample, sample.GetType(), WebOptions);
            object? deserialized = JsonSerializer.Deserialize(json, sample.GetType(), WebOptions);

            deserialized.ShouldNotBeNull(sample.GetType().FullName);
            AssertJsonEquivalent(json, deserialized);
        }
    }

    /// <summary>
    /// Verifies stable representative wire shapes.
    /// </summary>
    [Fact]
    public void RepresentativeFixturesShouldKeepStableCamelCaseJsonShapes()
    {
        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant-001","actorPartyId":"party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"businessReference":{"system":"crm","value":"case-123"},"projectId":"project-001","folderId":"folder-001","label":"Case 123","providerCorrelation":{"providerName":"provider-a","providerType":"assistant","metadataSchemaVersion":1,"providerSessionReference":"session-reference","providerResponseReference":"response-reference","extensionData":{"region":"eu"}}}
            """,
            new CreateConversationCommand(
                ContractSamples.CommandMetadata,
                ContractSamples.Business,
                ContractSamples.Project,
                ContractSamples.Folder,
                "Case 123",
                ContractSamples.ProviderCorrelation));

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"eventId":"event-001","eventType":"ConversationCreated","tenantId":"tenant-001","conversationId":"conversation-001","correlationId":"correlation-001","committedAt":"2026-05-18T11:00:00+00:00","actorPartyId":"party-actor","causationId":"causation-001"},"businessReference":{"system":"crm","value":"case-123"},"projectId":"project-001","folderId":"folder-001","label":"Case 123","providerCorrelation":{"providerName":"provider-a","providerType":"assistant","metadataSchemaVersion":1,"providerSessionReference":"session-reference","providerResponseReference":"response-reference","extensionData":{"region":"eu"}},"createdAt":"2026-05-18T11:00:00+00:00"}
            """,
            new ConversationCreated(
                ContractSamples.EventMetadata,
                ContractSamples.Business,
                ContractSamples.Project,
                ContractSamples.Folder,
                "Case 123",
                ContractSamples.ProviderCorrelation));

        AssertJsonEquivalent(
            """
            {"schemaVersion":1,"code":"tenant_isolation_violation","category":"authorization","isRetryable":false,"correlationId":"correlation-001","auditHandle":"audit-001","documentation":"https://docs.hexalith.local/conversations/errors","safeFieldDiagnostics":{"target":"hidden"},"developerGuidance":"The requested operation was not accepted."}
            """,
            ContractSamples.SafeError(ConversationErrorCode.TenantIsolationViolation));

        AssertJsonEquivalent(
            """
            {"schemaVersion":1,"commandType":"CreateConversationCommand","tenantId":"tenant-001","conversationId":"conversation-001","correlationId":"correlation-001","idempotencyKey":"idempotency-001","visibility":{"state":"Stale","guidance":"Read models may lag immediately after command acceptance."}}
            """,
            new ConversationCreatedResult(
                ContractSamples.Version,
                ConversationCommandType.CreateConversationCommand,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                "correlation-001",
                "idempotency-001",
                ContractSamples.Visibility));

        AssertJsonEquivalent(
            """
            {"tenantId":"tenant-001","conversationId":"conversation-001","freshness":{"state":"Current","observedAt":"2026-05-18T11:00:00+00:00","projectionContractSchemaVersion":1,"guidance":"Visible after accepted writes are projected."},"label":"Case 123","businessReference":{"system":"crm","value":"case-123"},"participantPartyIds":["party-actor","party-participant"]}
            """,
            new ConversationSummaryProjection(
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                ContractSamples.Freshness,
                "Case 123",
                ContractSamples.Business,
                [ContractSamples.Actor, ContractSamples.Participant]));

        AssertJsonEquivalent(
            """
            "Current"
            """,
            ProjectionTrustState.Current);

        AssertJsonEquivalent(
            """
            1
            """,
            SchemaVersion.Current);

        AssertJsonEquivalent(
            """
            {"schemaVersion":1,"tenantId":"tenant-001","actorPartyId":"party-actor","correlationId":"correlation-null","causationId":null,"idempotencyKey":null}
            """,
            new ConversationCommandMetadata(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Actor,
                "correlation-null"));
    }

    /// <summary>
    /// Ensures every released contract shape has at least one serialization fixture in the sample catalog.
    /// </summary>
    [Fact]
    public void ReleasedContractShapesShouldHaveSerializationFixtureCoverage()
    {
        Type[] expectedTypes =
        [
            typeof(CreateConversationCommand),
            typeof(AppendMessageCommand),
            typeof(AddParticipantCommand),
            typeof(AttachFileReferenceCommand),
            typeof(UpdateConversationMetadataCommand),
            typeof(CloseConversationCommand),
            typeof(ArchiveConversationCommand),
            typeof(ConversationCreated),
            typeof(MessageAppended),
            typeof(ParticipantAdded),
            typeof(FileReferenceAttached),
            typeof(ConversationMetadataUpdated),
            typeof(ConversationClosed),
            typeof(ConversationArchived),
            typeof(ConversationCommandAcceptedResult),
            typeof(ConversationCreatedResult),
            typeof(ConversationError),
            typeof(ConversationErrorResult),
            typeof(ContractVersionInfo),
            typeof(UnsupportedSchemaVersion),
            typeof(ConversationSummaryProjection),
            typeof(ConversationMessageProjection),
            typeof(ProjectionFreshness),
            typeof(ReadModelVisibility),
            typeof(ProjectionTrustState),
            typeof(SchemaVersion),
        ];

        Type[] sampleTypes = ContractSamples.AllContracts.Select(sample => sample.GetType()).ToArray();

        foreach (Type expectedType in expectedTypes)
        {
            sampleTypes.ShouldContain(expectedType, $"Missing serialization fixture for {expectedType.Name}.");
        }
    }

    private static void AssertJsonEquivalent(string expected, object value)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(value, value.GetType(), WebOptions));
    }
}
