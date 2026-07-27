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

    private static bool TryResolvePublicEventType(string? eventTypeName, [NotNullWhen(true)] out Type? eventType)
        => PublicEventTypes.TryResolveExactThenSuffix(eventTypeName, out eventType);

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
        return PolymorphicTypeRegistry.FromTypeNames(types);
    }
}
