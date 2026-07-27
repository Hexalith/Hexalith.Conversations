// <copyright file="ConversationProjectionDurableEventCoverageTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Guards the wire contract between the durable event stream and the projection decoder.
/// </summary>
/// <remarks>
/// <para>
/// A persisted projection envelope names its event by the CLR type the aggregate emitted — the domain event —
/// while the decoder resolves to the public contract type. Story 6.2's live gateway lane found that the decoder
/// knew only the public names, so every real production event was silently dropped and the read model persisted
/// as <c>Rebuilding</c> forever. Every test that existed at the time hand-built the envelope with a public event
/// name, so none of them could observe it.
/// </para>
/// <para>
/// This guard is driven from the durable event types themselves rather than a hand-kept list, so a new domain
/// event that the decoder cannot resolve fails here instead of silently degrading projections in production.
/// </para>
/// </remarks>
public sealed class ConversationProjectionDurableEventCoverageTest
{
    private static readonly JsonSerializerOptions DomainEventWireOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Gets every durable domain event a persisted Conversations stream can carry into a projection.
    /// </summary>
    /// <remarks>
    /// Rejection events are excluded: they are never persisted as projected history.
    /// </remarks>
    private static IReadOnlyList<Type> DurableProjectedEventTypes =>
    [
        .. typeof(ConversationCreatedDomainEvent).Assembly
            .GetTypes()
            .Where(static type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                && typeof(IEventPayload).IsAssignableFrom(type)
                && type.Name.EndsWith("DomainEvent", StringComparison.Ordinal)
                && !type.Name.Contains("Rejected", StringComparison.Ordinal))
            .OrderBy(static type => type.Name, StringComparer.Ordinal),
    ];

    [Fact]
    public void EveryDurableDomainEventNameShouldResolveToItsPublicContractType()
    {
        IReadOnlyList<Type> durableEvents = DurableProjectedEventTypes;

        durableEvents.ShouldNotBeEmpty(
            "the durable event scan found nothing, so this guard would pass without checking any event");

        foreach (Type durableEvent in durableEvents)
        {
            string expectedPublicName = durableEvent.Name[..^"DomainEvent".Length];

            // The persisted envelope carries the assembly-qualified-free full name, which is what the gateway
            // reads off the stored event. Resolving the short name alone would not prove production decodes.
            ConversationProjectionEventDecoder
                .TryResolvePublicEventType(durableEvent.FullName, out Type? resolved)
                .ShouldBeTrue($"the durable event name '{durableEvent.FullName}' must resolve for projections to apply it");
            resolved!.Name.ShouldBe(expectedPublicName);
            resolved.Namespace.ShouldBe(typeof(ConversationCreated).Namespace);
        }
    }

    [Fact]
    public void DurableDomainEventPayloadShouldDecodeIntoItsPublicContractShape()
    {
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        ConversationCreatedDomainEvent durable = new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-durable-decode-001",
                ConversationEventType.ConversationCreated,
                new TenantId("tenant-durable-001"),
                new ConversationId("conversation-durable-001"),
                "correlation-durable-001",
                occurredAt,
                new PartyId("party-durable-001"),
                "causation-durable-001"),
            new BusinessReference("crm", "case-durable-001"),
            new ProjectId("project-durable-001"),
            new FolderId("folder-durable-001"),
            "Durable decode proof",
            "idempotency-durable-001");

        IReadOnlyList<ConversationProjectionEventRecord> decoded = ConversationProjectionEventDecoder.Decode(
        [
            new ProjectionEventDto(
                durable.GetType().FullName!,
                JsonSerializer.SerializeToUtf8Bytes(durable, DomainEventWireOptions),
                "json",
                1,
                occurredAt,
                "correlation-durable-001"),
        ]);

        ConversationCreated created = decoded
            .ShouldHaveSingleItem()
            .Event
            .ShouldBeOfType<ConversationCreated>();
        created.Metadata.ConversationId.ShouldBe(durable.Metadata.ConversationId);
        created.Metadata.TenantId.ShouldBe(durable.Metadata.TenantId);
        created.Label.ShouldBe(durable.Label);
        created.ProjectId.ShouldBe(durable.ProjectId);
        created.FolderId.ShouldBe(durable.FolderId);
        created.BusinessReference.ShouldBe(durable.BusinessReference);
    }

    [Fact]
    public void UnknownEventNamesShouldStillBeRejected()
    {
        // The suffix alias must not degrade into "resolve anything": an unrelated name has to stay unresolved,
        // otherwise a poisoned envelope would be decoded as some arbitrary contract type.
        ConversationProjectionEventDecoder
            .TryResolvePublicEventType("Hexalith.Conversations.Events.SomethingElseDomainEvent", out _)
            .ShouldBeFalse();
        ConversationProjectionEventDecoder
            .TryResolvePublicEventType("DomainEvent", out _)
            .ShouldBeFalse();
        ConversationProjectionEventDecoder.TryResolvePublicEventType(null, out _).ShouldBeFalse();
    }
}
