// <copyright file="ConversationProjectionEventDecoder.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Hexalith.Commons.Serialization;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Decodes platform projection envelopes into the public Conversations event vocabulary.
/// </summary>
internal static class ConversationProjectionEventDecoder
{
    private static readonly JsonSerializerOptions EventJsonOptions =
        JsonSerializationOptions.CreateWeb([ConversationsJsonContext.Default], includeReflectionFallback: true);

    private static readonly PolymorphicTypeRegistry PublicEventTypes = BuildPublicEventTypeRegistry();

    internal static IReadOnlyDictionary<string, Type> PublicEventTypeEntries => PublicEventTypes.Entries;

    internal static IReadOnlyList<ConversationProjectionEventRecord> Decode(ProjectionEventDto[]? events)
    {
        if (events is null || events.Length == 0)
        {
            return [];
        }

        List<ConversationProjectionEventRecord> records = new(events.Length);
        foreach (ProjectionEventDto? evt in events)
        {
            if (evt is null || evt.SequenceNumber < 1)
            {
                continue;
            }

            if (!TryResolvePublicEventType(evt.EventTypeName, out Type? eventType))
            {
                continue;
            }

            object? decoded = evt.Payload is { Length: > 0 }
                ? JsonSerializer.Deserialize(evt.Payload, eventType, EventJsonOptions)
                : null;
            if (decoded is not null)
            {
                records.Add(new ConversationProjectionEventRecord(evt.SequenceNumber, decoded));
            }
        }

        return records;
    }

    /// <summary>
    /// Resolves one persisted or public event discriminator to the public contract type the projection consumes.
    /// </summary>
    /// <param name="eventTypeName">The discriminator carried by the projection envelope.</param>
    /// <param name="eventType">The resolved public contract event type.</param>
    /// <returns><see langword="true"/> when the discriminator resolves.</returns>
    /// <remarks>
    /// Exposed so the durable-stream coverage guard exercises this resolution rather than re-deriving the rule,
    /// which would let the guard agree with a broken decoder.
    /// </remarks>
    internal static bool TryResolvePublicEventType(string? eventTypeName, [NotNullWhen(true)] out Type? eventType)
        => PublicEventTypes.TryResolveExactThenSuffix(eventTypeName, out eventType);

    /// <summary>
    /// The suffix the durable domain event types carry over their public contract counterparts.
    /// </summary>
    /// <remarks>
    /// A persisted envelope names the event by the CLR type the aggregate emitted, which is the domain event
    /// (for example <c>Hexalith.Conversations.Events.ConversationCreatedDomainEvent</c>), not the public
    /// contract type the projection consumes. The suffix resolution matches on the END of the discriminator, so
    /// the public name alone can never match a domain-event name. Registering the suffixed alias is what lets a
    /// real production stream decode; without it every replayed event is silently dropped, the builder never
    /// observes a creation, and the read model persists as Rebuilding forever.
    /// </remarks>
    private const string DomainEventSuffix = "DomainEvent";

    private static PolymorphicTypeRegistry BuildPublicEventTypeRegistry()
    {
        Type[] types =
        [
            typeof(ConversationCreated),
            typeof(MessageAppended),
            typeof(ParticipantAdded),
            typeof(FileReferenceAttached),
            typeof(ConversationMetadataUpdated),
            typeof(ConversationProjectChanged),
            typeof(ConversationClosed),
            typeof(ConversationArchived),
            typeof(ConversationLifecycleChanged),
            typeof(RetentionPolicySet),
            typeof(RetentionPolicyReplaced),
            typeof(ConversationContentMarkedSensitive),
            typeof(MessageContentRedacted),
        ];

        // Every public contract event is registered twice: under its own name, which is what an already-decoded
        // public envelope carries, and under the durable domain-event name a persisted stream carries. The two
        // shapes are wire-compatible by construction because each domain event repeats its public counterpart's
        // members verbatim and only adds the domain-side idempotency key, which the public type ignores.
        return PolymorphicTypeRegistry.Create(
            types.SelectMany(static type => new PolymorphicTypeRegistration[]
            {
                new(type.Name, type),
                new($"{type.Name}{DomainEventSuffix}", type),
            }));
    }
}
