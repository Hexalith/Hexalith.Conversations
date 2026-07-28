// <copyright file="ConversationProjectionDurableEventCoverageTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
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
    public void EveryDurableDomainEventPayloadShouldDecodeIntoItsPublicContractShape()
    {
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        TenantId tenantId = new("tenant-durable-001");
        ConversationId conversationId = new("conversation-durable-001");
        PartyId actor = new("party-durable-001");
        GovernanceAuditEvidenceReference evidence = new(
            new AuditEvidenceHandle("audit-durable-001"),
            "policy-durable-001",
            occurredAt);
        object[] durableEvents =
        [
            new ConversationCreatedDomainEvent(
                Metadata(ConversationEventType.ConversationCreated),
                new BusinessReference("crm", "case-durable-001"),
                new ProjectId("project-durable-001"),
                new FolderId("folder-durable-001"),
                "Durable decode proof",
                "idempotency-durable-001"),
            new ParticipantAddedDomainEvent(
                Metadata(ConversationEventType.ParticipantAdded),
                new PartyId("party-participant-001"),
                ParticipantType.Human,
                ParticipantRole.Member),
            new ConversationProjectChangedDomainEvent(
                Metadata(ConversationEventType.ConversationProjectChanged),
                new ProjectId("project-previous-001"),
                new ProjectId("project-current-001")),
            new RetentionPolicySetDomainEvent(
                Metadata(ConversationEventType.RetentionPolicySet),
                "policy-durable-001",
                "retention-set",
                evidence,
                "idempotency-retention-set"),
            new RetentionPolicyReplacedDomainEvent(
                Metadata(ConversationEventType.RetentionPolicyReplaced),
                "policy-durable-002",
                "policy-durable-001",
                "retention-replaced",
                evidence,
                "idempotency-retention-replaced"),
            new ConversationContentMarkedSensitiveDomainEvent(
                Metadata(ConversationEventType.ConversationContentMarkedSensitive),
                new GovernanceTarget(GovernedTargetKind.Conversation),
                SensitivityCategory.Restricted,
                "policy-durable-001",
                "sensitivity-marked",
                evidence,
                "idempotency-sensitive"),
            new MessageContentRedactedDomainEvent(
                Metadata(ConversationEventType.MessageContentRedacted),
                new GovernanceTarget(GovernedTargetKind.Message, MessageId: new MessageId("message-durable-001")),
                RedactionCategory.ContentSuppression,
                "policy-durable-001",
                "message-redacted",
                evidence,
                "idempotency-redacted"),
        ];

        foreach (object durable in durableEvents)
        {
            IReadOnlyList<ConversationProjectionEventRecord> decoded = ConversationProjectionEventDecoder.Decode(
            [
                new ProjectionEventDto(
                    durable.GetType().FullName!,
                    JsonSerializer.SerializeToUtf8Bytes(durable, durable.GetType(), DomainEventWireOptions),
                    "json",
                    1,
                    occurredAt,
                    "correlation-durable-001"),
            ]);

            decoded.ShouldHaveSingleItem().Event.GetType().Name.ShouldBe(
                durable.GetType().Name[..^"DomainEvent".Length]);
        }

        ConversationEventMetadata Metadata(ConversationEventType eventType)
            => new(
                SchemaVersion.Current,
                $"event-durable-{eventType.Value}",
                eventType,
                tenantId,
                conversationId,
                "correlation-durable-001",
                occurredAt,
                actor,
                "causation-durable-001");
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
            .TryResolvePublicEventType("EvilConversationCreatedDomainEvent", out _)
            .ShouldBeFalse();
        ConversationProjectionEventDecoder
            .TryResolvePublicEventType("DomainEvent", out _)
            .ShouldBeFalse();
        ConversationProjectionEventDecoder.TryResolvePublicEventType(null, out _).ShouldBeFalse();
    }

    /// <summary>
    /// The durable aliases must stay out of the module's public event vocabulary.
    /// </summary>
    /// <remarks>
    /// Registering the durable names alongside the public ones in a single registry is the obvious way to make
    /// persisted streams decode, and it silently widened the public vocabulary from 13 names to 26 — a public
    /// contract change. Keeping the two registries disjoint is the invariant; this asserts it directly so the
    /// shortcut cannot be reintroduced without a red test.
    /// </remarks>
    [Fact]
    public void DurableAliasesShouldNotWidenThePublicEventVocabulary()
    {
        IReadOnlyDictionary<string, Type> publicEntries = ConversationProjectionEventDecoder.PublicEventTypeEntries;
        IReadOnlyDictionary<string, Type> durableEntries = ConversationProjectionEventDecoder.DurableEventTypeEntries;

        publicEntries.Count.ShouldBe(13);
        durableEntries.Count.ShouldBe(DurableProjectedEventTypes.Count);
        publicEntries.Keys.ShouldAllBe(name => !name.EndsWith("DomainEvent", StringComparison.Ordinal));
        durableEntries.Keys.ShouldAllBe(name => name.EndsWith("DomainEvent", StringComparison.Ordinal));
        publicEntries.Keys.Intersect(durableEntries.Keys, StringComparer.Ordinal).ShouldBeEmpty();

        durableEntries.Keys.Order(StringComparer.Ordinal).ShouldBe(
            [.. DurableProjectedEventTypes.Select(type => type.Name).Order(StringComparer.Ordinal)]);

        // Each alias must point at the public contract type it is named after, not merely at some public type.
        foreach ((string durableName, Type mappedType) in durableEntries)
        {
            mappedType.Name.ShouldBe(durableName[..^"DomainEvent".Length]);
            publicEntries[mappedType.Name].ShouldBe(mappedType);
        }
    }
}
