// <copyright file="ConversationProjectionEventDecoder.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Hexalith.Commons.Serialization;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Events;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Decodes platform projection envelopes into the public Conversations event vocabulary.
/// </summary>
internal static class ConversationProjectionEventDecoder
{
    private static readonly JsonSerializerOptions EventJsonOptions =
        JsonSerializationOptions.CreateWeb([ConversationsJsonContext.Default], includeReflectionFallback: true);

    private static readonly Type[] PublicContractEventTypes =
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

    private static readonly (Type DurableType, Type PublicType)[] DurableProjectedEventTypes =
    [
        (typeof(ConversationCreatedDomainEvent), typeof(ConversationCreated)),
        (typeof(ParticipantAddedDomainEvent), typeof(ParticipantAdded)),
        (typeof(ConversationProjectChangedDomainEvent), typeof(ConversationProjectChanged)),
        (typeof(RetentionPolicySetDomainEvent), typeof(RetentionPolicySet)),
        (typeof(RetentionPolicyReplacedDomainEvent), typeof(RetentionPolicyReplaced)),
        (typeof(ConversationContentMarkedSensitiveDomainEvent), typeof(ConversationContentMarkedSensitive)),
        (typeof(MessageContentRedactedDomainEvent), typeof(MessageContentRedacted)),
    ];

    private static readonly PolymorphicTypeRegistry PublicEventTypes = BuildPublicEventTypeRegistry();

    private static readonly PolymorphicTypeRegistry DurableEventTypes = BuildDurableEventTypeRegistry();

    /// <summary>
    /// The namespaces a qualified discriminator is allowed to carry, derived from the registered types.
    /// </summary>
    /// <remarks>
    /// Suffix resolution alone would accept any prefix, so an envelope from an unrelated bounded context named
    /// <c>Other.Context.ConversationCreatedDomainEvent</c> would decode as a Conversations event and be
    /// materialized into a tenant read model. Requiring the prefix to be a namespace this module actually
    /// declares closes that without pinning literal strings the type list could drift away from.
    /// </remarks>
    private static readonly FrozenSet<string> QualifyingNamespaces = BuildQualifyingNamespaces();

    internal static IReadOnlyDictionary<string, Type> PublicEventTypeEntries => PublicEventTypes.Entries;

    /// <summary>
    /// Gets the durable domain-event aliases resolved for persisted streams, keyed by durable name.
    /// </summary>
    internal static IReadOnlyDictionary<string, Type> DurableEventTypeEntries => DurableEventTypes.Entries;

    internal static IReadOnlyList<ConversationProjectionEventRecord> Decode(ProjectionEventDto[]? events)
    {
        if (events is null || events.Length == 0)
        {
            return [];
        }

        List<ConversationProjectionEventRecord> records = new(events.Length);
        foreach (ProjectionEventDto? evt in events)
        {
            records.Add(Decode(evt));
        }

        return records;
    }

    /// <summary>
    /// Decodes every valid envelope and reports whether any individual envelope was rejected.
    /// </summary>
    internal static IReadOnlyList<ConversationProjectionEventRecord> DecodePermissive(
        ProjectionEventDto[]? events,
        out bool decodeFailed)
    {
        decodeFailed = false;
        if (events is null || events.Length == 0)
        {
            return [];
        }

        List<ConversationProjectionEventRecord> records = new(events.Length);
        foreach (ProjectionEventDto? evt in events)
        {
            try
            {
                records.Add(Decode(evt));
            }
            catch (Exception exception) when (exception is ArgumentException or JsonException)
            {
                decodeFailed = true;
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
        => TryResolveExactOrQualified(PublicEventTypes, eventTypeName, out eventType)
            || TryResolveExactOrQualified(DurableEventTypes, eventTypeName, out eventType);

    private static FrozenSet<string> BuildQualifyingNamespaces()
        => PublicContractEventTypes
            .Select(static type => type.Namespace)
            .Concat(DurableProjectedEventTypes.Select(static pair => pair.DurableType.Namespace))
            .Concat(DurableProjectedEventTypes.Select(static pair => pair.PublicType.Namespace))
            .Append(typeof(ConversationRejectedDomainEvent).Namespace)
            .Where(static candidate => !string.IsNullOrEmpty(candidate))
            .Select(static candidate => candidate!)
            .ToFrozenSet(StringComparer.Ordinal);

    private static PolymorphicTypeRegistry BuildPublicEventTypeRegistry()
        => PolymorphicTypeRegistry.FromTypeNames(PublicContractEventTypes);

    /// <summary>
    /// Builds the durable-name aliases a persisted stream carries, kept apart from the public vocabulary.
    /// </summary>
    /// <returns>A registry mapping each durable domain-event name onto its public contract type.</returns>
    /// <remarks>
    /// A persisted envelope names the event by the CLR type the aggregate emitted, which is the domain event
    /// (for example <c>Hexalith.Conversations.Events.ConversationCreatedDomainEvent</c>), not the public contract
    /// type the projection consumes. Suffix resolution matches the END of the discriminator, so the public name
    /// alone can never match a domain-event name; without these aliases every replayed durable event would be
    /// rejected before materialization. The two
    /// shapes are wire-compatible by construction because each domain event repeats its public counterpart's
    /// members verbatim and only adds the domain-side idempotency key, which the public type ignores.
    /// <para>
    /// These aliases are deliberately a separate registry rather than extra entries in the public one:
    /// <see cref="PublicEventTypeEntries"/> remains the module's 13-name public event vocabulary, while this
    /// registry contains only the seven domain-event types the aggregate actually persists and projects.
    /// </para>
    /// </remarks>
    private static PolymorphicTypeRegistry BuildDurableEventTypeRegistry()
        => PolymorphicTypeRegistry.Create(
            DurableProjectedEventTypes.Select(static pair =>
                new PolymorphicTypeRegistration(pair.DurableType.Name, pair.PublicType)));

    private static ConversationProjectionEventRecord Decode(ProjectionEventDto? evt)
    {
        if (evt is null)
        {
            throw new JsonException("The projection event envelope is missing.");
        }

        if (evt.SequenceNumber < 1)
        {
            throw new JsonException("The projection event sequence is invalid.");
        }

        if (!string.Equals(evt.SerializationFormat, "json", StringComparison.OrdinalIgnoreCase))
        {
            throw new JsonException("The projection event serialization format is unsupported.");
        }

        if (evt.Payload is not { Length: > 0 })
        {
            throw new JsonException("The projection event payload is missing.");
        }

        if (IsRejectedDomainEvent(evt.EventTypeName))
        {
            _ = JsonSerializer.Deserialize<ConversationRejectedDomainEvent>(evt.Payload, EventJsonOptions)
                ?? throw new JsonException("The projection event payload decoded to null.");
            return new(evt.SequenceNumber, new ConversationProjectionPositionOnlyEvent(evt.Timestamp));
        }

        if (!TryResolvePublicEventType(evt.EventTypeName, out Type? eventType))
        {
            throw new JsonException("The projection event discriminator is unsupported.");
        }

        object decoded = JsonSerializer.Deserialize(evt.Payload, eventType, EventJsonOptions)
            ?? throw new JsonException("The projection event payload decoded to null.");
        return new(evt.SequenceNumber, decoded);
    }

    private static bool IsRejectedDomainEvent(string? discriminator)
    {
        if (string.IsNullOrWhiteSpace(discriminator))
        {
            return false;
        }

        string alias = nameof(ConversationRejectedDomainEvent);
        if (string.Equals(discriminator, alias, StringComparison.Ordinal))
        {
            return true;
        }

        int separatorIndex = discriminator.Length - alias.Length - 1;
        return separatorIndex >= 0
            && discriminator.EndsWith(alias, StringComparison.Ordinal)
            && discriminator[separatorIndex] is '.' or '+'
            && QualifyingNamespaces.Contains(discriminator[..separatorIndex]);
    }

    private static bool TryResolveExactOrQualified(
        PolymorphicTypeRegistry registry,
        string? discriminator,
        [NotNullWhen(true)] out Type? eventType)
    {
        eventType = null;
        if (string.IsNullOrWhiteSpace(discriminator))
        {
            return false;
        }

        if (registry.Entries.TryGetValue(discriminator, out eventType))
        {
            return true;
        }

        foreach ((string alias, Type registeredType) in registry.Entries)
        {
            int separatorIndex = discriminator.Length - alias.Length - 1;
            if (separatorIndex >= 0
                && discriminator.EndsWith(alias, StringComparison.Ordinal)
                && discriminator[separatorIndex] is '.' or '+'
                && QualifyingNamespaces.Contains(discriminator[..separatorIndex]))
            {
                eventType = registeredType;
                return true;
            }
        }

        return false;
    }
}
