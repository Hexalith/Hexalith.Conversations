// <copyright file="ConversationPublicationContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Events;

/// <summary>
/// Verifies the public publication event contract shape required by Story 1.10.
/// </summary>
public sealed class ConversationPublicationContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    // Forbidden across every publication event: Party personal data, raw provider payloads,
    // upstream record bodies, transport substrate references. Provider *correlation* references
    // are stable identifiers and are allowed on events that legitimately carry them, so the
    // bare token "providerCorrelation" is intentionally NOT in this list.
    private static readonly string[] ForbiddenPublicationTerms =
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
        "rawUpstream",
        "fileBinary",
        "documentBody",
        "embedding",
        "authorizationHeader",
        "bearerToken",
        "claims",
        "envelope",
        "streamName",
        "streamPosition",
        "snapshot",
        "Dapr",
        "SignalR",
        "redactedContent",
    ];

    /// <summary>
    /// Ensures published metadata has explicit schema, identity, tenant, correlation, and dedupe fields.
    /// </summary>
    [Fact]
    public void EventMetadataShouldPublishV1IdentityAndDeduplicationFields()
    {
        string json = JsonSerializer.Serialize(ContractSamples.EventMetadata, WebOptions);
        JsonObject metadata = JsonNode.Parse(json)!.AsObject();

        metadata["schemaVersion"]!.GetValue<int>().ShouldBe(1);
        metadata["eventType"]!.GetValue<string>().ShouldBe("ConversationCreated");
        metadata["tenantId"]!.GetValue<string>().ShouldBe("tenant:tenant-001");
        metadata["conversationId"]!.GetValue<string>().ShouldBe("conv:conversation-001");
        metadata["eventId"]!.GetValue<string>().ShouldBe("event-001");
        metadata["occurredAt"]!.GetValue<DateTimeOffset>().ShouldBe(new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));
        metadata["correlationId"]!.GetValue<string>().ShouldBe("correlation-001");
        metadata["causationId"]!.GetValue<string>().ShouldBe("causation-001");
        metadata["deduplicationKey"]!.GetValue<string>().ShouldBe("tenant:tenant-001|conv:conversation-001|event-001|1");
        metadata.ContainsKey("committedAt").ShouldBeFalse("publication contracts use occurredAt rather than persistence vocabulary");
    }

    /// <summary>
    /// Ensures lifecycle changes use bounded state values instead of free-form strings.
    /// </summary>
    [Fact]
    public void LifecycleChangedShouldSerializeBoundedPreviousAndCurrentStates()
    {
        ConversationLifecycleChanged e = new(
            ContractSamples.LifecycleChangedEventMetadata,
            ConversationLifecycleStatus.Open,
            ConversationLifecycleStatus.Closed,
            "resolved");

        string json = JsonSerializer.Serialize(e, WebOptions);

        json.ShouldContain(@"""previousState"":""Open""");
        json.ShouldContain(@"""currentState"":""Closed""");
        json.ShouldContain(@"""reasonCode"":""resolved""");
        json.ShouldNotContain("free-form", Case.Insensitive);
    }

    /// <summary>
    /// Ensures unsupported major versions preserve bounded identity fields for the publication diagnostic boundary.
    /// </summary>
    [Fact]
    public void EventMetadataShouldKeepUnsupportedVersionDiagnosticInputsBounded()
    {
        ConversationEventMetadata metadata = ContractSamples.EventMetadata with
        {
            SchemaVersion = new SchemaVersion(2),
        };

        metadata.SchemaVersion.ShouldBe(new SchemaVersion(2));
        metadata.DeduplicationKey.ShouldBe("tenant:tenant-001|conv:conversation-001|event-001|2");
        metadata.CorrelationId.ShouldBe("correlation-001");
    }

    /// <summary>
    /// Scans every public publication event payload for forbidden Party personal data, raw provider
    /// payloads, upstream record bodies, file binaries, and transport substrate vocabulary. AC 2 of
    /// Story 1.10 requires the published wire shape to carry only stable references and bounded
    /// metadata, so every public event contract must survive this deny-term scan.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllPublicationEvents))]
    public void EveryPublicationEventShouldExcludeForbiddenPayloadTerms(string eventLabel, object publicEvent)
    {
        string json = JsonSerializer.Serialize(publicEvent, publicEvent.GetType(), WebOptions);
        foreach (string forbidden in ForbiddenPublicationTerms)
        {
            json.ShouldNotContain(forbidden, Case.Insensitive, $"{eventLabel} payload contains forbidden term '{forbidden}'.");
        }
    }

    public static TheoryData<string, object> AllPublicationEvents()
    {
        ConversationEventMetadata metadata = ContractSamples.EventMetadata;
        return new TheoryData<string, object>
        {
            { nameof(ConversationCreated), new ConversationCreated(metadata, ContractSamples.Business, ContractSamples.Project, ContractSamples.Folder, "Case 123", ContractSamples.ProviderCorrelation) },
            { nameof(MessageAppended), new MessageAppended(metadata, ContractSamples.Message, ContractSamples.Actor, "Hello", ContractSamples.ProviderCorrelation) },
            { nameof(ParticipantAdded), new ParticipantAdded(ContractSamples.ParticipantEventMetadata, ContractSamples.Participant, ParticipantType.Human, ParticipantRole.Member) },
            { nameof(FileReferenceAttached), new FileReferenceAttached(metadata, ContractSamples.File, ContractSamples.Folder, ContractSamples.Message) },
            { nameof(ConversationMetadataUpdated), new ConversationMetadataUpdated(metadata, "Case 123", ContractSamples.Business, new Dictionary<string, string> { ["priority"] = "normal" }) },
            { nameof(ConversationClosed), new ConversationClosed(metadata, "resolved") },
            { nameof(ConversationArchived), new ConversationArchived(metadata, "retained") },
            { nameof(ConversationLifecycleChanged), new ConversationLifecycleChanged(ContractSamples.LifecycleChangedEventMetadata, ConversationLifecycleStatus.Open, ConversationLifecycleStatus.Closed, "resolved") },
        };
    }
}
