// <copyright file="ConversationPublicationMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Reads public event metadata without exposing transport or persistence details to contracts.
/// </summary>
public static class ConversationPublicationMetadata
{
    /// <summary>
    /// Gets metadata from a supported public event.
    /// </summary>
    /// <param name="e">The public event.</param>
    /// <returns>The metadata, or <c>null</c> for unsupported payloads.</returns>
    public static ConversationEventMetadata? GetMetadata(object e)
        => e switch
        {
            ConversationCreated created => created.Metadata,
            ParticipantAdded participant => participant.Metadata,
            MessageAppended message => message.Metadata,
            FileReferenceAttached file => file.Metadata,
            ConversationMetadataUpdated update => update.Metadata,
            ConversationClosed closed => closed.Metadata,
            ConversationArchived archived => archived.Metadata,
            ConversationLifecycleChanged lifecycle => lifecycle.Metadata,
            RetentionPolicySet retentionSet => retentionSet.Metadata,
            RetentionPolicyReplaced retentionReplaced => retentionReplaced.Metadata,
            _ => null,
        };

    /// <summary>
    /// Verifies that the metadata event type matches the public payload type.
    /// </summary>
    /// <param name="e">The public event.</param>
    /// <param name="eventType">The metadata event type.</param>
    /// <returns><c>true</c> when the type matches.</returns>
    public static bool EventTypeMatches(object e, ConversationEventType eventType)
        => e switch
        {
            ConversationCreated => eventType == ConversationEventType.ConversationCreated,
            ParticipantAdded => eventType == ConversationEventType.ParticipantAdded,
            MessageAppended => eventType == ConversationEventType.MessageAppended,
            FileReferenceAttached => eventType == ConversationEventType.FileReferenceAttached,
            ConversationMetadataUpdated => eventType == ConversationEventType.ConversationMetadataUpdated,
            ConversationClosed => eventType == ConversationEventType.ConversationClosed,
            ConversationArchived => eventType == ConversationEventType.ConversationArchived,
            ConversationLifecycleChanged => eventType == ConversationEventType.ConversationLifecycleChanged,
            RetentionPolicySet => eventType == ConversationEventType.RetentionPolicySet,
            RetentionPolicyReplaced => eventType == ConversationEventType.RetentionPolicyReplaced,
            _ => false,
        };
}
