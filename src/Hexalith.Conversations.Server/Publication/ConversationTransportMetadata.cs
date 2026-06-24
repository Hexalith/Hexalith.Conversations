// <copyright file="ConversationTransportMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Commons.Publication;

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Represents safe transport-visible metadata for a public conversation event.
/// </summary>
/// <param name="Topic">The safe topic name.</param>
/// <param name="Type">The safe event type attribute.</param>
/// <param name="Source">The safe source attribute.</param>
/// <param name="Subject">The safe subject attribute.</param>
/// <param name="Headers">The safe header/extension values.</param>
public sealed record ConversationTransportMetadata(
    string Topic,
    string Type,
    string Source,
    string Subject,
    IReadOnlyDictionary<string, string> Headers)
{
    /// <summary>
    /// Creates safe transport metadata from a public event.
    /// </summary>
    /// <param name="e">The public event.</param>
    /// <returns>The safe metadata.</returns>
    public static ConversationTransportMetadata FromEvent(object e)
    {
        ConversationEventMetadata metadata = ConversationPublicationMetadata.GetMetadata(e)
            ?? throw new ArgumentException("Unsupported conversation publication event.", nameof(e));

        string tenant = metadata.TenantId.Value;
        string conversation = metadata.ConversationId.Value;
        string eventId = metadata.EventId;
        string schema = metadata.SchemaVersion.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string eventType = metadata.EventType.Value;

        Dictionary<string, string> headers = new(StringComparer.Ordinal)
        {
            ["schemaVersion"] = schema,
            ["eventType"] = eventType,
            ["tenantId"] = tenant,
            ["conversationId"] = conversation,
            ["eventId"] = eventId,
            ["correlationId"] = metadata.CorrelationId,
            ["deduplicationKey"] = metadata.DeduplicationKey,
        };

        if (metadata.CausationId is { Length: > 0 } causation)
        {
            headers["causationId"] = causation;
        }

        PublicationTransportMetadata transport = PublicationTransportMetadataComposer.Compose(
            $"{tenant}.conversations.events",
            $"Hexalith.Conversations.{eventType}.v{schema}",
            $"hexalith-conversations/{tenant}",
            $"conversations/{conversation}/events/{eventId}",
            headers);

        return new ConversationTransportMetadata(
            transport.Topic,
            transport.Type,
            transport.Source,
            transport.Subject,
            transport.Headers);
    }
}
