// <copyright file="ConversationState.Agents.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Events;

namespace Hexalith.Conversations.State;

/// <summary>Event-only restricted membership, original intent and deletion delivery state.</summary>
public sealed partial class ConversationState
{
    private long _sourceRevision;
    private readonly HashSet<PartyId> _removedAgentParties = [];
    private readonly Dictionary<string, string> _deliveryAttemptTargets = new(StringComparer.Ordinal);
    private ConversationDeletionSourceResult _deletionSource = new(ConversationAgentsOutcome.Absent);

    /// <summary>Gets the replayed source position, counting every supported event once.</summary>
    public long SourceRevision => _sourceRevision;

    /// <summary>Gets whether a creation event certified the replayed source prefix.</summary>
    public bool HasCompleteEventPrefix => IsCreated && _sourceRevision > 0 && _prefixCreated;

    private bool _prefixCreated;
    private DateTimeOffset? _deletionApprovedAt;
    private PartyId? _deletionApprover;
    private GovernanceAuditEvidenceReference? _deletionAuditEvidence;

    /// <summary>Gets the immutable independently approved logical-deletion audit evidence.</summary>
    public GovernanceAuditEvidenceReference? DeletionAuditEvidence => _deletionAuditEvidence;

    /// <summary>Gets the original immutable approval occurrence.</summary>
    public DateTimeOffset? DeletionApprovedAt => _deletionApprovedAt;
    /// <summary>Gets the independently admitted original approver Party.</summary>
    public PartyId? DeletionApprover => _deletionApprover;

    /// <summary>Gets the durable deletion publication and delivery checkpoint.</summary>
    public ConversationDeletionSourceResult DeletionSource => _deletionSource;

    /// <summary>Looks up a durable independent attempt target.</summary>
    /// <param name="attempt">Exact attempt identity.</param>
    /// <returns>The original immutable target when recorded.</returns>
    public string? DeliveryAttemptTarget(string attempt) => _deliveryAttemptTargets.GetValueOrDefault(attempt);

    /// <summary>Gets whether approved source deletion was persisted.</summary>
    public bool IsDeleted => _deletionSource.Signal is not null;

    /// <summary>Tests the permanent Agent removal tombstone.</summary>
    /// <param name="partyId">Exact immutable Party.</param>
    /// <returns>Whether that Party was explicitly removed.</returns>
    public bool WasAgentRemoved(PartyId partyId) => _removedAgentParties.Contains(partyId);

    /// <summary>Applies immutable Agent removal without allowing later service rejoin.</summary>
    /// <param name="e">Persisted removal.</param>
    public void Apply(AgentParticipantRemovedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _sourceRevision++;
        _removedAgentParties.Add(e.ParticipantPartyId);
        _participants = _participants.RemoveAll(p => p.PartyId == e.ParticipantPartyId);
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>Applies original deterministic posting intent.</summary>
    /// <param name="e">Persisted posting.</param>
    public void Apply(MessageAppendedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Apply(new MessageAppended(e.Metadata, e.MessageId, e.AuthorPartyId, e.Text,
            e.ProviderCorrelation, AgentProvenance: e.AgentProvenance, IdempotencyKey: e.IdempotencyKey));
        int index = FindMessageIndex(e.MessageId);
        _messages = _messages.SetItem(index, _messages[index] with
        {
            OriginalIntentFingerprint = e.IntentFingerprint
        });
    }

    /// <summary>Applies current visible message edit and human provenance.</summary>
    /// <param name="e">Persisted edit.</param>
    public void Apply(MessageEditedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _sourceRevision++;
        int index = FindMessageIndex(e.MessageId);
        ConversationMessage original = _messages[index];
        if (original.Deleted)
        {
            throw new InvalidOperationException("Deleted message cannot be edited.");
        }
        _messages = _messages.SetItem(index, original with
        {
            Text = e.Text,
            EditedAt = e.Metadata.CommittedAt,
            Provenance = original.Provenance is null ? null : original.Provenance with
            {
                HumanEdited = true,
                EditedByPartyId = e.EditedByPartyId
            }
        });
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>Applies current message deletion, retaining immutable original intent.</summary>
    /// <param name="e">Persisted message deletion.</param>
    public void Apply(MessageDeletedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _sourceRevision++;
        int index = FindMessageIndex(e.MessageId);
        _messages = _messages.SetItem(index, _messages[index] with
        {
            Text = string.Empty,
            Deleted = true
        });
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>Applies approved deletion and its outbox entry atomically as one source event.</summary>
    /// <param name="e">Persisted independently approved deletion.</param>
    public void Apply(ConversationDeletionApprovedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _sourceRevision++;
        if (e.AuditEvidence is null || e.AuditEvidence.CapturedAt > e.Metadata.CommittedAt
            || IsDeleted || e.Signal.SourceRevision != SourceRevision
            || e.Signal.TenantId != TenantId || e.Signal.ConversationId != ConversationId
            || e.Signal.SourceStream != AgentSourceStream(TenantId!, ConversationId!))
        {
            throw new InvalidOperationException("Deletion approval source position mismatch.");
        }
        _deletionApprovedAt = e.Metadata.CommittedAt;
        _deletionApprover = e.Metadata.ActorPartyId;
        _deletionAuditEvidence = e.AuditEvidence;
        _deletionSource = new(ConversationAgentsOutcome.Available, e.Signal);
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>Applies retry, exact receipt or durable poison quarantine.</summary>
    /// <param name="e">Persisted delivery fact.</param>
    public void Apply(ConversationDeletionDeliveryRecordedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _sourceRevision++;
        if (_deletionSource.Signal != e.Signal || e.PreviousDeliveryRevision != _deletionSource.DeliveryRevision
            || _deletionSource.Outcome == ConversationAgentsOutcome.Quarantined)
        {
            throw new InvalidOperationException("Delivery replay compare mismatch.");
        }
        if (e.Action == ConversationDeletionDeliveryAction.Acknowledge
            && !IsExactAcknowledgement(e.Signal, e.TargetVersion, e.Acknowledgement))
        {
            throw new InvalidOperationException("Delivery receipt mismatch.");
        }
        _deliveryAttemptTargets.TryAdd(e.DeliveryAttemptId, e.TargetVersion);
        _deletionSource = _deletionSource with
        {
            Outcome = e.Action == ConversationDeletionDeliveryAction.Quarantine ? ConversationAgentsOutcome.Quarantined : ConversationAgentsOutcome.Available,
            DeliveryRevision = _deletionSource.DeliveryRevision + 1,
            AcknowledgedSourceRevision = e.Action == ConversationDeletionDeliveryAction.Acknowledge ? e.Signal.SourceRevision : _deletionSource.AcknowledgedSourceRevision,
            Acknowledgement = e.Acknowledgement ?? _deletionSource.Acknowledgement,
            LastAttemptId = e.DeliveryAttemptId,
            TargetVersion = e.TargetVersion,
            PoisonCode = e.PoisonCode
        };
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>Builds the stable source address.</summary>
    /// <param name="tenant">Exact tenant.</param>
    /// <param name="conversation">Exact Conversation.</param>
    /// <returns>The immutable stream address.</returns>
    public static string AgentSourceStream(TenantId tenant, ConversationId conversation)
        => $"{tenant.Value}:conversation:{conversation.Value}";

    /// <summary>Checks exact acknowledgement identity; authentication is a separate admission requirement.</summary>
    /// <param name="signal">Original publication.</param>
    /// <param name="target">Exact admitted receiver version.</param>
    /// <param name="acknowledgement">Receiver receipt.</param>
    /// <returns>Whether every immutable acknowledgement field matches.</returns>
    public static bool IsExactAcknowledgement(ConversationDeletionSignal signal, string target,
        ConversationDeletionAcknowledgement? acknowledgement)
        => acknowledgement is not null && acknowledgement.SignalId == signal.ConversationDeletionSignalId
            && acknowledgement.SourceRevision == signal.SourceRevision && acknowledgement.ProtectedDeletionRevision > 0
            && acknowledgement.TargetVersion == target && !string.IsNullOrWhiteSpace(acknowledgement.Evidence);

    private int FindMessageIndex(MessageId id)
    {
        for (int index = 0; index < _messages.Length; index++)
        {
            if (_messages[index].MessageId == id)
            {
                return index;
            }
        }
        throw new InvalidOperationException("Mutation target missing in complete replay.");
    }
}
