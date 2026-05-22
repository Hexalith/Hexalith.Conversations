// <copyright file="ConversationProjectionAccumulator.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;

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
    private readonly ConversationId _conversationId;
    private string? _label;
    private ConversationProjectionLifecycleState _lifecycle = ConversationProjectionLifecycleState.NotCreated;
    private ConversationRetentionPolicyProjectionV1? _activeRetentionPolicy;
    private readonly Dictionary<string, ConversationSensitivityMarkProjectionV1> _sensitivityMarks = new(StringComparer.Ordinal);
    private readonly TenantId _tenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionAccumulator"/> class.
    /// </summary>
    /// <param name="expectedTenantId">The tenant identity this accumulator is allowed to project.</param>
    /// <param name="expectedConversationId">The conversation identity this accumulator is allowed to project.</param>
    public ConversationProjectionAccumulator(TenantId expectedTenantId, ConversationId expectedConversationId)
    {
        _tenantId = expectedTenantId ?? throw new ArgumentNullException(nameof(expectedTenantId));
        _conversationId = expectedConversationId ?? throw new ArgumentNullException(nameof(expectedConversationId));
    }

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
            _processedEventIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            _activeRetentionPolicy,
            _sensitivityMarks.Values
                .OrderBy(mark => mark.Target.ToTargetKey(), StringComparer.Ordinal)
                .ToArray());

    /// <summary>
    /// Applies a conversation-created event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ConversationCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

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
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        _participants[e.ParticipantPartyId] = e.ParticipantPartyId;
    }

    /// <summary>
    /// Applies a message-appended event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(MessageAppended e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        _messages[e.MessageId] = e.MessageId;
    }

    /// <summary>
    /// Applies a file-reference-attached event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(FileReferenceAttached e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        _files[e.FileId] = e.FileId;
    }

    /// <summary>
    /// Applies a conversation-metadata-updated event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ConversationMetadataUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        // P12 review fix (2026-05-19): treat null Label / BusinessReference as "no change" rather than "clear".
        // This makes Created+MetadataUpdated reorder-deterministic in combination with Apply(ConversationCreated)'s
        // ??= semantics: whichever event carries a non-null value wins, regardless of arrival order.
        if (e.Label is not null)
        {
            _label = e.Label;
        }

        if (e.BusinessReference is not null)
        {
            _businessReference = e.BusinessReference;
        }

        if (e.Attributes is null || e.Attributes.Count == 0)
        {
            // null/empty Attributes means "do not modify"; this preserves projection state under a metadata update that
            // only touches Label/BusinessReference (or arrives reordered against a previous metadata update).
            return;
        }

        _attributes.Clear();
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
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

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
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        _lifecycle = ConversationProjectionLifecycleState.Archived;
    }

    /// <summary>
    /// Applies a retention-policy-set event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(RetentionPolicySet e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        _activeRetentionPolicy = new ConversationRetentionPolicyProjectionV1(
            e.PolicyReference,
            e.Rationale,
            e.Metadata.ActorPartyId,
            e.Metadata.CommittedAt,
            e.AuditEvidence);
    }

    /// <summary>
    /// Applies a retention-policy-replaced event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(RetentionPolicyReplaced e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        _activeRetentionPolicy = new ConversationRetentionPolicyProjectionV1(
            e.PolicyReference,
            e.Rationale,
            e.Metadata.ActorPartyId,
            e.Metadata.CommittedAt,
            e.AuditEvidence,
            e.PreviousPolicyReference);
    }

    /// <summary>
    /// Applies a sensitivity-marked event.
    /// </summary>
    /// <param name="e">The event to apply.</param>
    public void Apply(ConversationContentMarkedSensitive e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!TryMarkProcessed(e.Metadata))
        {
            return;
        }

        _sensitivityMarks[e.Target.ToTargetKey()] = new ConversationSensitivityMarkProjectionV1(
            e.Target,
            e.Category,
            e.PolicyReference,
            e.Rationale,
            e.Metadata.ActorPartyId,
            e.Metadata.CommittedAt,
            e.AuditEvidence,
            ProjectionTrustState.Current);
    }

    private bool TryMarkProcessed(ConversationEventMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!_tenantId.Equals(metadata.TenantId))
        {
            return false;
        }

        if (!_conversationId.Equals(metadata.ConversationId))
        {
            return false;
        }

        return _processedEventIds.Add(metadata.EventId);
    }
}
