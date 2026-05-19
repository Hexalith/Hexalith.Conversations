// <copyright file="ConversationProjectionAccumulator.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Applies conversation events through idempotent set/update operations for local projection evidence.
/// </summary>
public sealed class ConversationProjectionAccumulator
{
    private readonly Dictionary<string, string> _attributes = new(StringComparer.Ordinal);
    private readonly Dictionary<FileId, FileId> _files = [];
    private readonly Dictionary<MessageId, MessageId> _messages = [];
    private readonly Dictionary<PartyId, PartyId> _participants = [];
    private readonly HashSet<string> _processedEventIds = new(StringComparer.Ordinal);

    private BusinessReference? _businessReference;
    private ConversationId? _conversationId;
    private string? _label;
    private ConversationProjectionLifecycleState _lifecycle = ConversationProjectionLifecycleState.NotCreated;
    private TenantId? _tenantId;

    /// <summary>
    /// Gets the current immutable projection snapshot.
    /// </summary>
    public ConversationProjectionSnapshot Snapshot
        => new(
            _tenantId,
            _conversationId,
            _lifecycle,
            _label,
            _businessReference,
            _participants.Values.OrderBy(p => p.Value, StringComparer.Ordinal).ToArray(),
            _messages.Values.OrderBy(m => m.Value, StringComparer.Ordinal).ToArray(),
            _files.Values.OrderBy(f => f.Value, StringComparer.Ordinal).ToArray(),
            new Dictionary<string, string>(_attributes, StringComparer.Ordinal),
            _processedEventIds.OrderBy(id => id, StringComparer.Ordinal).ToArray());

    /// <summary>
    /// Applies a conversation-created event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ConversationCreated e)
    {
        if (!TryMarkProcessed(e?.Metadata))
        {
            return;
        }

        CaptureIdentity(e!.Metadata);
        if (_lifecycle == ConversationProjectionLifecycleState.NotCreated)
        {
            _lifecycle = ConversationProjectionLifecycleState.Open;
        }

        _label ??= e.Label;
        _businessReference ??= e.BusinessReference;
    }

    /// <summary>
    /// Applies a participant-added event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ParticipantAdded e)
    {
        if (!TryMarkProcessed(e?.Metadata))
        {
            return;
        }

        CaptureIdentity(e!.Metadata);
        _participants[e.ParticipantPartyId] = e.ParticipantPartyId;
    }

    /// <summary>
    /// Applies a message-appended event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(MessageAppended e)
    {
        if (!TryMarkProcessed(e?.Metadata))
        {
            return;
        }

        CaptureIdentity(e!.Metadata);
        _messages[e.MessageId] = e.MessageId;
    }

    /// <summary>
    /// Applies a file-reference-attached event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(FileReferenceAttached e)
    {
        if (!TryMarkProcessed(e?.Metadata))
        {
            return;
        }

        CaptureIdentity(e!.Metadata);
        _files[e.FileId] = e.FileId;
    }

    /// <summary>
    /// Applies a conversation-metadata-updated event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ConversationMetadataUpdated e)
    {
        if (!TryMarkProcessed(e?.Metadata))
        {
            return;
        }

        CaptureIdentity(e!.Metadata);
        _label = e.Label;
        _businessReference = e.BusinessReference;
        _attributes.Clear();
        if (e.Attributes is null)
        {
            return;
        }

        foreach (KeyValuePair<string, string> attribute in e.Attributes.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            _attributes[attribute.Key] = attribute.Value;
        }
    }

    /// <summary>
    /// Applies a conversation-closed event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ConversationClosed e)
    {
        if (!TryMarkProcessed(e?.Metadata))
        {
            return;
        }

        CaptureIdentity(e!.Metadata);
        if (_lifecycle != ConversationProjectionLifecycleState.Archived)
        {
            _lifecycle = ConversationProjectionLifecycleState.Closed;
        }
    }

    /// <summary>
    /// Applies a conversation-archived event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ConversationArchived e)
    {
        if (!TryMarkProcessed(e?.Metadata))
        {
            return;
        }

        CaptureIdentity(e!.Metadata);
        _lifecycle = ConversationProjectionLifecycleState.Archived;
    }

    private bool TryMarkProcessed(ConversationEventMetadata? metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return _processedEventIds.Add(metadata.EventId);
    }

    private void CaptureIdentity(ConversationEventMetadata metadata)
    {
        _tenantId ??= metadata.TenantId;
        _conversationId ??= metadata.ConversationId;
    }
}
