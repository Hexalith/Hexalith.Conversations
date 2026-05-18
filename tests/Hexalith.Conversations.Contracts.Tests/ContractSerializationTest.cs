// <copyright file="ContractSerializationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

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
            {"metadata":{"schemaVersion":{"value":1},"tenantId":{"value":"tenant-001"},"actorPartyId":{"value":"party-actor"},"correlationId":"correlation-001","causationId":"causation-001","idempotencyKey":"idempotency-001"},"businessReference":{"system":"crm","value":"case-123"},"projectId":{"value":"project-001"},"folderId":{"value":"folder-001"},"label":"Case 123","providerCorrelation":{"providerName":"provider-a","providerType":"assistant","metadataSchemaVersion":{"value":1},"providerSessionReference":"session-reference","providerResponseReference":"response-reference","extensionData":{"region":"eu"}}}
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
            {"metadata":{"schemaVersion":{"value":1},"eventType":"ConversationCreated","tenantId":{"value":"tenant-001"},"conversationId":{"value":"conversation-001"},"actorPartyId":{"value":"party-actor"},"correlationId":"correlation-001","causationId":"causation-001","committedAt":"2026-05-18T11:00:00+00:00"},"businessReference":{"system":"crm","value":"case-123"},"projectId":{"value":"project-001"},"folderId":{"value":"folder-001"},"label":"Case 123","providerCorrelation":{"providerName":"provider-a","providerType":"assistant","metadataSchemaVersion":{"value":1},"providerSessionReference":"session-reference","providerResponseReference":"response-reference","extensionData":{"region":"eu"}}}
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
            {"schemaVersion":{"value":1},"code":"tenant_isolation_violation","category":"authorization","isRetryable":false,"correlationId":"correlation-001","auditHandle":"audit-001","documentation":"https://docs.hexalith.local/conversations/errors","safeFieldDiagnostics":{"target":"hidden"},"developerGuidance":"The requested operation was not accepted."}
            """,
            ContractSamples.SafeError(ConversationErrorCode.TenantIsolationViolation));

        AssertJsonEquivalent(
            """
            {"schemaVersion":{"value":1},"tenantId":{"value":"tenant-001"},"conversationId":{"value":"conversation-001"},"correlationId":"correlation-001","idempotencyKey":"idempotency-001","visibility":{"state":{"value":"Stale"},"guidance":"Read models may lag immediately after command acceptance."}}
            """,
            new ConversationCreatedResult(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                "correlation-001",
                "idempotency-001",
                ContractSamples.Visibility));

        AssertJsonEquivalent(
            """
            {"tenantId":{"value":"tenant-001"},"conversationId":{"value":"conversation-001"},"freshness":{"state":{"value":"Current"},"observedAt":"2026-05-18T11:00:00+00:00","schemaVersion":{"value":1},"guidance":"Visible after accepted writes are projected."},"label":"Case 123","businessReference":{"system":"crm","value":"case-123"},"participantPartyIds":[{"value":"party-actor"},{"value":"party-participant"}]}
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
            {"value":"Current"}
            """,
            ProjectionTrustState.Current);

        AssertJsonEquivalent(
            """
            {"value":1}
            """,
            SchemaVersion.Current);
    }

    private static void AssertJsonEquivalent(string expected, object value)
    {
        using JsonDocument expectedDocument = JsonDocument.Parse(expected);
        using JsonDocument actualDocument = JsonDocument.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        actualDocument.RootElement.ToString().ShouldBe(expectedDocument.RootElement.ToString());
    }
}
