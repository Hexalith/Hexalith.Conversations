// <copyright file="ConversationState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Immutable;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;

namespace Hexalith.Conversations.State;

/// <summary>
/// Replay-only state for a tenant-scoped conversation.
/// </summary>
/// <remarks>
/// Declared as a sealed class (not a record) because the state carries identity through a
/// rebuilt-on-each-Apply <see cref="ImmutableArray{T}"/> of participants. Record value-equality
/// would be misleading over private collection fields, and the synthesized <c>with</c> copy
/// constructor would share a mutable backing list across clones.
/// </remarks>
public sealed class ConversationState
{
    private readonly Dictionary<string, string> _attributes = new(StringComparer.Ordinal);
    private ImmutableArray<ConversationFileReference> _fileReferences = ImmutableArray<ConversationFileReference>.Empty;
    private ImmutableArray<ConversationMessage> _messages = ImmutableArray<ConversationMessage>.Empty;
    private ImmutableArray<ConversationParticipant> _participants = ImmutableArray<ConversationParticipant>.Empty;
    private readonly Dictionary<string, ConversationRedactionState> _redactions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ConversationSensitivityMarkState> _sensitivityMarks = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the conversation was created.
    /// </summary>
    public bool IsCreated { get; private set; }

    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    public ConversationLifecycleState Lifecycle { get; private set; } = ConversationLifecycleState.NotCreated;

    /// <summary>
    /// Gets the tenant binding copied from persisted event data.
    /// </summary>
    public TenantId? TenantId { get; private set; }

    /// <summary>
    /// Gets the internal Conversations-owned identity copied from persisted event data.
    /// </summary>
    public ConversationId? ConversationId { get; private set; }

    /// <summary>
    /// Gets the creator Party attribution copied from persisted event data.
    /// </summary>
    public PartyId? CreatorPartyId { get; private set; }

    /// <summary>
    /// Gets the deterministic creation timestamp copied from persisted event data.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <summary>
    /// Gets the schema version copied from persisted event data.
    /// </summary>
    public SchemaVersion? SchemaVersion { get; private set; }

    /// <summary>
    /// Gets the correlation identifier copied from persisted event data.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Gets the causation identifier copied from persisted event data.
    /// </summary>
    public string? CausationId { get; private set; }

    /// <summary>
    /// Gets the idempotency key copied from command metadata when supplied.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// Gets the optional adopter-owned business reference.
    /// </summary>
    public BusinessReference? BusinessReference { get; private set; }

    /// <summary>
    /// Gets the optional stable project reference.
    /// </summary>
    public ProjectId? ProjectId { get; private set; }

    /// <summary>
    /// Gets the optional stable folder reference.
    /// </summary>
    public FolderId? FolderId { get; private set; }

    /// <summary>
    /// Gets the optional UI label that is not identity.
    /// </summary>
    public string? Label { get; private set; }

    /// <summary>
    /// Gets the deterministic timestamp of the most recently applied event, when any.
    /// </summary>
    public DateTimeOffset? LastEventAt { get; private set; }

    /// <summary>
    /// Gets optional provider correlation metadata that is never authority.
    /// </summary>
    public ProviderCorrelationMetadata? ProviderCorrelation { get; private set; }

    /// <summary>
    /// Gets the replayed active retention policy state, when a governed policy has been accepted.
    /// </summary>
    public ConversationRetentionPolicyState? ActiveRetentionPolicy { get; private set; }

    /// <summary>
    /// Gets replayed sensitivity marks keyed by deterministic governed target reference.
    /// </summary>
    public IReadOnlyList<ConversationSensitivityMarkState> SensitivityMarks
        => _sensitivityMarks.Values.OrderBy(mark => mark.TargetKey, StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Gets replayed redaction intents keyed by deterministic governed target reference.
    /// </summary>
    public IReadOnlyList<ConversationRedactionState> Redactions
        => _redactions.Values.OrderBy(redaction => redaction.TargetKey, StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Gets the replayed participant membership as an immutable snapshot.
    /// </summary>
    public IReadOnlyList<ConversationParticipant> Participants => _participants;

    /// <summary>
    /// Gets the replayed messages as an immutable snapshot.
    /// </summary>
    public IReadOnlyList<ConversationMessage> Messages => _messages;

    /// <summary>
    /// Gets the replayed file references as an immutable snapshot.
    /// </summary>
    public IReadOnlyList<ConversationFileReference> FileReferences => _fileReferences;

    /// <summary>
    /// Gets deterministic metadata attributes copied from persisted events.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes
        => new Dictionary<string, string>(_attributes.OrderBy(a => a.Key, StringComparer.Ordinal), StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a participant membership already exists.
    /// </summary>
    /// <param name="partyId">The stable Party reference.</param>
    /// <param name="participantType">The participant type.</param>
    /// <param name="participantRole">The participant role.</param>
    /// <returns><see langword="true" /> when matching membership exists.</returns>
    public bool HasParticipant(PartyId partyId, ParticipantType participantType, ParticipantRole participantRole)
        => _participants.Any(p => p.PartyId == partyId
            && p.ParticipantType == participantType
            && p.ParticipantRole == participantRole);

    /// <summary>
    /// Looks up replayed sensitivity state by deterministic governed target key.
    /// </summary>
    /// <param name="targetKey">The target key.</param>
    /// <param name="mark">The matched mark when present.</param>
    /// <returns><see langword="true" /> when the target already has a replayed mark.</returns>
    public bool TryGetSensitivityMark(string targetKey, out ConversationSensitivityMarkState? mark)
        => _sensitivityMarks.TryGetValue(targetKey, out mark);

    /// <summary>
    /// Looks up replayed redaction state by deterministic governed target key.
    /// </summary>
    /// <param name="targetKey">The target key.</param>
    /// <param name="redaction">The matched redaction when present.</param>
    /// <returns><see langword="true" /> when the target already has a replayed redaction.</returns>
    public bool TryGetRedaction(string targetKey, out ConversationRedactionState? redaction)
        => _redactions.TryGetValue(targetKey, out redaction);

    /// <summary>
    /// Builds a deterministic safe key for a governed target reference.
    /// </summary>
    /// <param name="target">The content-safe governed target reference.</param>
    /// <returns>The deterministic target key.</returns>
    public static string SensitivityTargetKey(GovernanceTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return target.ToTargetKey();
    }

    /// <summary>
    /// Builds a deterministic safe key for a governed redaction target reference.
    /// </summary>
    /// <param name="target">The content-safe governed target reference.</param>
    /// <returns>The deterministic target key.</returns>
    public static string RedactionTargetKey(GovernanceTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return target.ToTargetKey();
    }

    /// <summary>
    /// Applies a conversation-created event during deterministic replay.
    /// </summary>
    /// <param name="e">The conversation-created event.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the state has already been created. Duplicate <see cref="ConversationCreatedDomainEvent"/>
    /// entries indicate a corrupted or replayed stream and must surface as a deterministic invariant
    /// violation rather than silently overwriting tenant binding, creator attribution, or timestamps.
    /// </exception>
    public void Apply(ConversationCreatedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (IsCreated)
        {
            throw new InvalidOperationException(
                "ConversationCreatedDomainEvent applied to an already-created ConversationState. "
                + "Duplicate creation in event history violates the replay invariant.");
        }

        IsCreated = true;
        Lifecycle = ConversationLifecycleState.Open;
        TenantId = e.Metadata.TenantId;
        ConversationId = e.Metadata.ConversationId;
        CreatorPartyId = e.Metadata.ActorPartyId;
        CreatedAt = e.Metadata.CommittedAt;
        LastEventAt = e.Metadata.CommittedAt;
        SchemaVersion = e.Metadata.SchemaVersion;
        CorrelationId = e.Metadata.CorrelationId;
        CausationId = e.Metadata.CausationId;
        BusinessReference = e.BusinessReference;
        ProjectId = e.ProjectId;
        FolderId = e.FolderId;
        Label = e.Label;
        IdempotencyKey = e.IdempotencyKey;
        ProviderCorrelation = e.ProviderCorrelation;
    }

    /// <summary>
    /// Applies a participant-added event during deterministic replay.
    /// </summary>
    /// <remarks>
    /// Replay is idempotent: a duplicate event in the stream (snapshot pointer not advanced,
    /// dispatcher retry) is treated as a no-op so the aggregate remains loadable. Command-time
    /// duplicate detection is the authority for rejecting duplicate membership and lives in the
    /// aggregate validation, not here.
    /// </remarks>
    /// <param name="e">The participant-added event.</param>
    public void Apply(ParticipantAddedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (HasParticipant(e.ParticipantPartyId, e.ParticipantType, e.ParticipantRole))
        {
            return;
        }

        _participants = _participants.Add(new ConversationParticipant(
            e.ParticipantPartyId,
            e.ParticipantType,
            e.ParticipantRole,
            e.AddedAt,
            e.Metadata.ActorPartyId));
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a project-changed event during deterministic replay.
    /// </summary>
    /// <remarks>
    /// Replay is idempotent for duplicate delivery of the same accepted target. A mismatched
    /// previous-project value indicates a corrupted event sequence and is surfaced as a replay
    /// invariant violation by the verifier.
    /// </remarks>
    /// <param name="e">The project-changed event.</param>
    public void Apply(ConversationProjectChangedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (ProjectId == e.CurrentProjectId)
        {
            return;
        }

        if (ProjectId != e.PreviousProjectId)
        {
            throw new InvalidOperationException("Conversation project assignment replay invariant violated.");
        }

        ProjectId = e.CurrentProjectId;
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a retention-policy-set event during deterministic replay.
    /// </summary>
    /// <param name="e">The retention policy event.</param>
    public void Apply(RetentionPolicySetDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (ActiveRetentionPolicy is not null
            && ActiveRetentionPolicy.PolicyReference == e.PolicyReference
            && ActiveRetentionPolicy.Rationale == e.Rationale
            && ActiveRetentionPolicy.AuditEvidence == e.AuditEvidence)
        {
            return;
        }

        ActiveRetentionPolicy = new ConversationRetentionPolicyState(
            e.PolicyReference,
            e.Rationale,
            e.Metadata.ActorPartyId,
            e.Metadata.CommittedAt,
            e.AuditEvidence);
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a retention-policy-replaced event during deterministic replay.
    /// </summary>
    /// <param name="e">The retention policy event.</param>
    public void Apply(RetentionPolicyReplacedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (ActiveRetentionPolicy is not null
            && ActiveRetentionPolicy.PolicyReference == e.PolicyReference
            && ActiveRetentionPolicy.Rationale == e.Rationale
            && ActiveRetentionPolicy.AuditEvidence == e.AuditEvidence)
        {
            return;
        }

        ActiveRetentionPolicy = new ConversationRetentionPolicyState(
            e.PolicyReference,
            e.Rationale,
            e.Metadata.ActorPartyId,
            e.Metadata.CommittedAt,
            e.AuditEvidence,
            e.PreviousPolicyReference);
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a sensitivity-marked event during deterministic replay.
    /// </summary>
    /// <param name="e">The sensitivity event.</param>
    public void Apply(ConversationContentMarkedSensitiveDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        string targetKey = SensitivityTargetKey(e.Target);
        if (_sensitivityMarks.TryGetValue(targetKey, out ConversationSensitivityMarkState? existing)
            && existing.Category == e.Category
            && existing.PolicyReference == e.PolicyReference
            && existing.Rationale == e.Rationale)
        {
            return;
        }

        _sensitivityMarks[targetKey] = new ConversationSensitivityMarkState(
            targetKey,
            e.Target,
            e.Category,
            e.PolicyReference,
            e.Rationale,
            e.Metadata.ActorPartyId,
            e.Metadata.CommittedAt,
            e.AuditEvidence);
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a message-content-redacted event during deterministic replay.
    /// </summary>
    /// <param name="e">The redaction event.</param>
    public void Apply(MessageContentRedactedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        string targetKey = RedactionTargetKey(e.Target);
        if (_redactions.TryGetValue(targetKey, out ConversationRedactionState? existing)
            && existing.Category == e.Category
            && existing.PolicyReference == e.PolicyReference
            && existing.Rationale == e.Rationale)
        {
            return;
        }

        _redactions[targetKey] = new ConversationRedactionState(
            targetKey,
            e.Target,
            e.Category,
            e.PolicyReference,
            e.Rationale,
            e.Metadata.ActorPartyId,
            e.Metadata.CommittedAt,
            e.AuditEvidence);
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a public conversation-created event during deterministic replay.
    /// </summary>
    /// <param name="e">The public conversation-created event.</param>
    public void Apply(ConversationCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new ConversationCreatedDomainEvent(
            e.Metadata,
            e.BusinessReference,
            e.ProjectId,
            e.FolderId,
            e.Label,
            ProviderCorrelation: e.ProviderCorrelation));
    }

    /// <summary>
    /// Applies a public participant-added event during deterministic replay.
    /// </summary>
    /// <param name="e">The public participant-added event.</param>
    public void Apply(ParticipantAdded e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new ParticipantAddedDomainEvent(
            e.Metadata,
            e.ParticipantPartyId,
            e.ParticipantType,
            e.ParticipantRole));
    }

    /// <summary>
    /// Applies a public project-changed event during deterministic replay.
    /// </summary>
    /// <param name="e">The public project-changed event.</param>
    public void Apply(ConversationProjectChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new ConversationProjectChangedDomainEvent(
            e.Metadata,
            e.PreviousProjectId,
            e.CurrentProjectId));
    }

    /// <summary>
    /// Applies a public retention-policy-set event during deterministic replay.
    /// </summary>
    /// <param name="e">The public retention policy event.</param>
    public void Apply(RetentionPolicySet e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new RetentionPolicySetDomainEvent(
            e.Metadata,
            e.PolicyReference,
            e.Rationale,
            e.AuditEvidence));
    }

    /// <summary>
    /// Applies a public retention-policy-replaced event during deterministic replay.
    /// </summary>
    /// <param name="e">The public retention policy event.</param>
    public void Apply(RetentionPolicyReplaced e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new RetentionPolicyReplacedDomainEvent(
            e.Metadata,
            e.PolicyReference,
            e.PreviousPolicyReference,
            e.Rationale,
            e.AuditEvidence));
    }

    /// <summary>
    /// Applies a public sensitivity-marked event during deterministic replay.
    /// </summary>
    /// <param name="e">The public sensitivity event.</param>
    public void Apply(ConversationContentMarkedSensitive e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new ConversationContentMarkedSensitiveDomainEvent(
            e.Metadata,
            e.Target,
            e.Category,
            e.PolicyReference,
            e.Rationale,
            e.AuditEvidence));
    }

    /// <summary>
    /// Applies a public message-content-redacted event during deterministic replay.
    /// </summary>
    /// <param name="e">The public redaction event.</param>
    public void Apply(MessageContentRedacted e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new MessageContentRedactedDomainEvent(
            e.Metadata,
            e.Target,
            e.Category,
            e.PolicyReference,
            e.Rationale,
            e.AuditEvidence));
    }

    /// <summary>
    /// Applies a public message-appended event during deterministic replay.
    /// </summary>
    /// <param name="e">The public message-appended event.</param>
    public void Apply(MessageAppended e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentException.ThrowIfNullOrWhiteSpace(e.Text);

        if (_messages.Any(m => m.MessageId == e.MessageId))
        {
            throw new InvalidOperationException("Duplicate message identity in replayed event history.");
        }

        _messages = _messages.Add(new ConversationMessage(
            e.MessageId,
            e.AuthorPartyId,
            e.Text,
            e.Metadata.CommittedAt,
            e.ProviderCorrelation));
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a public file-reference-attached event during deterministic replay.
    /// </summary>
    /// <param name="e">The public file-reference-attached event.</param>
    public void Apply(FileReferenceAttached e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_fileReferences.Any(f => f.FileId == e.FileId))
        {
            throw new InvalidOperationException("Duplicate file identity in replayed event history.");
        }

        _fileReferences = _fileReferences.Add(new ConversationFileReference(e.FileId, e.FolderId, e.MessageId));
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a public metadata-updated event during deterministic replay.
    /// </summary>
    /// <param name="e">The public metadata-updated event.</param>
    public void Apply(ConversationMetadataUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.Label is not null)
        {
            Label = e.Label;
        }

        if (e.BusinessReference is not null)
        {
            BusinessReference = e.BusinessReference;
        }

        if (e.Attributes is not null && e.Attributes.Count > 0)
        {
            _attributes.Clear();
            foreach (KeyValuePair<string, string> attribute in e.Attributes.OrderBy(a => a.Key, StringComparer.Ordinal))
            {
                _attributes[attribute.Key] = attribute.Value;
            }
        }

        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a public conversation-closed event during deterministic replay.
    /// </summary>
    /// <param name="e">The public conversation-closed event.</param>
    public void Apply(ConversationClosed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Lifecycle != ConversationLifecycleState.Archived)
        {
            Lifecycle = ConversationLifecycleState.Closed;
        }

        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a public conversation-archived event during deterministic replay.
    /// </summary>
    /// <param name="e">The public conversation-archived event.</param>
    public void Apply(ConversationArchived e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Lifecycle = ConversationLifecycleState.Archived;
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a bounded public lifecycle-changed event during deterministic replay.
    /// </summary>
    /// <param name="e">The public lifecycle-changed event.</param>
    public void Apply(ConversationLifecycleChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Lifecycle = e.CurrentState.Value switch
        {
            "Open" => ConversationLifecycleState.Open,
            "Closed" => ConversationLifecycleState.Closed,
            "Archived" => ConversationLifecycleState.Archived,
            _ => throw new ArgumentException("Unsupported lifecycle state.", nameof(e)),
        };
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a rejection event as a no-op during replay.
    /// </summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ConversationRejectedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        // Keep this as an instance replay overload so event dispatchers can route it uniformly.
        _ = IsCreated;
    }

    /// <summary>
    /// Test-only seam for setting <see cref="Lifecycle"/> directly. Exposed to
    /// <c>Hexalith.Conversations.Tests</c> via <c>InternalsVisibleTo</c>.
    /// </summary>
    /// <remarks>
    /// Production code must never call this. Lifecycle transitions in production must flow through
    /// the corresponding <c>Apply</c> overload for an emitted domain event. This seam exists so
    /// participant tests can set up Closed/Archived states without depending on close/archive
    /// commands, which are owned by a future story.
    /// </remarks>
    /// <param name="lifecycle">The lifecycle state to force.</param>
    internal void ForceLifecycleForTests(ConversationLifecycleState lifecycle) => Lifecycle = lifecycle;
}
