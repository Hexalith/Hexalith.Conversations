// <copyright file="SchemaVersionCompatibilityTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Versioning;

/// <summary>
/// Verifies v1 schema-version wire shape and compatibility boundaries.
/// </summary>
public sealed class SchemaVersionCompatibilityTest
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CurrentSchemaVersionShouldSerializeAsPositiveInteger()
    {
        string json = JsonSerializer.Serialize(SchemaVersion.Current, Options);

        json.ShouldBe("1");
        JsonSerializer.Deserialize<SchemaVersion>(json, Options).ShouldBe(SchemaVersion.Current);
    }

    [Fact]
    public void AdditiveV1MetadataFieldsShouldBeIgnoredByContractDeserialization()
    {
        string json = """
            {
              "schemaVersion": 1,
              "eventId": "event-create-001",
              "eventType": "ConversationCreated",
              "tenantId": "tenant:tenant-alpha",
              "conversationId": "conv:conversation-alpha",
              "correlationId": "correlation-001",
              "occurredAt": "2026-05-22T08:00:00+00:00",
              "actorPartyId": "party:party-creator",
              "causationId": "causation-001",
              "additiveV1Field": "ignored"
            }
            """;

        ConversationEventMetadata? metadata = JsonSerializer.Deserialize<ConversationEventMetadata>(json, Options);

        metadata.ShouldNotBeNull();
        metadata.SchemaVersion.ShouldBe(SchemaVersion.Current);
        metadata.TenantId.ShouldBe(new TenantId("tenant-alpha"));
        metadata.ConversationId.ShouldBe(new ConversationId("conversation-alpha"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void MissingOrInvalidSchemaVersionsShouldRejectDuringDeserialization(string version)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<SchemaVersion>(version, Options));
    }

    [Fact]
    public void PublicContractsShouldNotExposeInfrastructureTopologyTerms()
    {
        string serialized = JsonSerializer.Serialize(new ConversationEventMetadata(
            SchemaVersion.Current,
            "event-create-001",
            ConversationEventType.ConversationCreated,
            new TenantId("tenant-alpha"),
            new ConversationId("conversation-alpha"),
            "correlation-001",
            new DateTimeOffset(2026, 5, 22, 8, 0, 0, TimeSpan.Zero),
            new PartyId("party-creator"),
            "causation-001"), Options);

        serialized.ShouldNotContain("stream", Case.Insensitive);
        serialized.ShouldNotContain("offset", Case.Insensitive);
        serialized.ShouldNotContain("dapr", Case.Insensitive);
        serialized.ShouldNotContain("topic", Case.Insensitive);
        serialized.ShouldNotContain("subscription", Case.Insensitive);
        serialized.ShouldNotContain("snapshot", Case.Insensitive);
    }
}
