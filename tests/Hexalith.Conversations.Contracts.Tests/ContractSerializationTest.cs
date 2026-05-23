// <copyright file="ContractSerializationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
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
    /// JSON equivalence between the source payload and re-serialized deserialized object catches converters
    /// that drop fields, since a dropped field would produce asymmetric JSON. Record value-equality is
    /// intentionally not asserted: records with <see cref="IReadOnlyDictionary{TKey,TValue}"/> or
    /// <see cref="IReadOnlyList{T}"/> properties use reference equality on those properties and would
    /// always report inequality after a JSON round-trip even when the round-trip is correct.
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
    /// Verifies stable representative wire shapes for each contract family.
    /// </summary>
    [Fact]
    public void RepresentativeFixturesShouldKeepStableCamelCaseJsonShapes()
    {
        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"businessReference":{"system":"crm","value":"case-123"},"projectId":"project:project-001","folderId":"folder:folder-001","label":"Case 123","providerCorrelation":{"providerName":"provider-a","providerType":"assistant","metadataSchemaVersion":1,"providerSessionReference":"session-reference","providerResponseReference":"response-reference","extensionData":{"region":"eu"}},"callerMetadata":null}
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
            {"metadata":{"schemaVersion":1,"eventId":"event-001","eventType":"ConversationCreated","tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","correlationId":"correlation-001","occurredAt":"2026-05-18T11:00:00+00:00","actorPartyId":"party:party-actor","causationId":"causation-001","deduplicationKey":"tenant:tenant-001|conv:conversation-001|event-001|1"},"businessReference":{"system":"crm","value":"case-123"},"projectId":"project:project-001","folderId":"folder:folder-001","label":"Case 123","providerCorrelation":{"providerName":"provider-a","providerType":"assistant","metadataSchemaVersion":1,"providerSessionReference":"session-reference","providerResponseReference":"response-reference","extensionData":{"region":"eu"}},"createdAt":"2026-05-18T11:00:00+00:00"}
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
            {"schemaVersion":1,"code":"tenant_isolation_violation","category":"authorization","isRetryable":false,"correlationId":"correlation-001","auditHandle":null,"documentation":"https://docs.hexalith.local/conversations/contracts/v1/errors","safeFieldDiagnostics":{"target":"hidden"},"developerGuidance":"The requested operation was not accepted.","clientAction":"check-access","safeMessage":"The request cannot be completed with the supplied access context."}
            """,
            ContractSamples.SafeError(ConversationErrorCode.TenantIsolationViolation));

        AssertJsonEquivalent(
            """
            {"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","correlationId":"correlation-001","idempotencyKey":"idempotency-001","visibility":{"state":"Stale","guidance":"Read models may lag immediately after command acceptance."},"commandType":"CreateConversationCommand"}
            """,
            new ConversationCreatedResult(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                "correlation-001",
                "idempotency-001",
                ContractSamples.Visibility,
                ConversationCommandType.CreateConversationCommand));

        AssertJsonEquivalent(
            """
            {"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","freshness":{"state":"Current","observedAt":"2026-05-18T11:00:00+00:00","projectionContractSchemaVersion":1,"guidance":"Visible after accepted writes are projected."},"label":"Case 123","businessReference":{"system":"crm","value":"case-123"},"participantPartyIds":["party:party-actor","party:party-participant"]}
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
            {"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-null","causationId":null,"idempotencyKey":null}
            """,
            new ConversationCommandMetadata(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Actor,
                "correlation-null"));
    }

    /// <summary>
    /// Verifies the optional caller-metadata provenance member is carried on each command that accepts it
    /// (create, append, update) and round-trips with a stable camelCase wire shape (Story 4.6). This proves
    /// the additive attachment surfaces uniformly across all three commands rather than only on create.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldRoundTripOnEveryCommandThatCarriesIt()
    {
        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"businessReference":null,"projectId":null,"folderId":null,"label":"Case 123","providerCorrelation":null,"callerMetadata":{"metadataSchemaVersion":1,"clientName":"adopter-client","clientVersion":"1.4.0","composerSource":"front-composer","origin":"adopter-portal","integrationContext":"intake","extensionData":{"channel":"web"}}}
            """,
            new CreateConversationCommand(
                ContractSamples.CommandMetadata,
                Label: "Case 123",
                CallerMetadata: ContractSamples.Caller));

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"conversationId":"conv:conversation-001","messageId":"message:message-001","authorPartyId":"party:party-actor","text":"Hello from the adopter.","providerCorrelation":null,"callerMetadata":{"metadataSchemaVersion":1,"clientName":"adopter-client","clientVersion":"1.4.0","composerSource":"front-composer","origin":"adopter-portal","integrationContext":"intake","extensionData":{"channel":"web"}}}
            """,
            new AppendMessageCommand(
                ContractSamples.CommandMetadata,
                ContractSamples.Conversation,
                ContractSamples.Message,
                ContractSamples.Actor,
                "Hello from the adopter.",
                CallerMetadata: ContractSamples.Caller));

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"conversationId":"conv:conversation-001","label":null,"businessReference":null,"attributes":null,"callerMetadata":{"metadataSchemaVersion":1,"clientName":"adopter-client","clientVersion":"1.4.0","composerSource":"front-composer","origin":"adopter-portal","integrationContext":"intake","extensionData":{"channel":"web"}}}
            """,
            new UpdateConversationMetadataCommand(
                ContractSamples.CommandMetadata,
                ContractSamples.Conversation,
                CallerMetadata: ContractSamples.Caller));
    }

    /// <summary>
    /// Ensures every exported public record under Hexalith.Conversations.Contracts has at least one
    /// serialization fixture in <see cref="ContractSamples.AllContracts"/>. Discovers types via assembly
    /// scan so new public records cannot ship without fixture coverage.
    /// </summary>
    [Fact]
    public void EveryPublicContractRecordShouldHaveSerializationFixtureCoverage()
    {
        Assembly assembly = typeof(ConversationId).Assembly;
        Type[] publicRecords = assembly.GetExportedTypes()
            .Where(IsPublicContractRecord)
            .ToArray();

        Type[] sampleTypes = ContractSamples.AllContracts.Select(sample => sample.GetType()).ToArray();

        foreach (Type publicRecord in publicRecords)
        {
            sampleTypes.ShouldContain(publicRecord, $"Missing serialization fixture for {publicRecord.Name}. Add it to ContractSamples.AllContracts.");
        }
    }

    private static bool IsPublicContractRecord(Type type)
    {
        if (!type.IsPublic || !type.IsClass || type.IsAbstract)
        {
            return false;
        }

        // C# records emit a synthesized Clone method named "<Clone>$".
        return type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic) is not null;
    }

    private static void AssertJsonEquivalent(string expected, object value)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(value, value.GetType(), WebOptions));
    }
}
