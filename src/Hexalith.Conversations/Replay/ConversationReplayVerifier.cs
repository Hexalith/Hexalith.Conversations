// <copyright file="ConversationReplayVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;

namespace Hexalith.Conversations.Replay;

/// <summary>
/// Replays ordered persisted conversation events into aggregate state with content-safe failures.
/// </summary>
public static class ConversationReplayVerifier
{
    /// <summary>
    /// Replays the supplied ordered event stream.
    /// </summary>
    /// <param name="tenantId">The expected trusted tenant scope.</param>
    /// <param name="conversationId">The expected conversation scope.</param>
    /// <param name="events">The ordered persisted event records.</param>
    /// <returns>A deterministic replay result.</returns>
    public static ConversationReplayResult Replay(
        TenantId tenantId,
        ConversationId conversationId,
        IEnumerable<ConversationReplayEventRecord> events)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(events);

        ConversationState state = new();
        HashSet<string> eventIds = new(StringComparer.Ordinal);
        long expectedPosition = 1;

        foreach (ConversationReplayEventRecord record in events)
        {
            ConversationEventMetadata? metadata = TryGetMetadata(record.Event);
            if (metadata is null)
            {
                if (record.Event is ConversationRejectedDomainEvent rejected)
                {
                    if (record.Position != expectedPosition)
                    {
                        return Reject(
                            ConversationErrorCode.CommandValidationFailed,
                            record.Position < expectedPosition ? "event_position_reordered" : "event_position_gap");
                    }

                    state.Apply(rejected);
                    expectedPosition++;
                    continue;
                }

                return Reject(ConversationErrorCode.CommandValidationFailed, "unsupported_event_type");
            }

            if (!tenantId.Equals(metadata.TenantId))
            {
                return Reject(ConversationErrorCode.TenantContextMismatch, "tenant_mismatch");
            }

            if (!conversationId.Equals(metadata.ConversationId))
            {
                return Reject(ConversationErrorCode.TenantContextMismatch, "conversation_mismatch");
            }

            if (metadata.SchemaVersion != SchemaVersion.Current)
            {
                return Reject(ConversationErrorCode.SchemaVersionUnsupported, "unsupported_schema_version");
            }

            if (!EventTypeMatchesPayload(record.Event, metadata.EventType))
            {
                return Reject(ConversationErrorCode.CommandValidationFailed, "event_type_mismatch");
            }

            if (record.Position != expectedPosition)
            {
                return Reject(
                    ConversationErrorCode.CommandValidationFailed,
                    record.Position < expectedPosition ? "event_position_reordered" : "event_position_gap");
            }

            if (!eventIds.Add(metadata.EventId))
            {
                if (record.Event is ParticipantAddedDomainEvent or ParticipantAdded
                    or ConversationProjectChangedDomainEvent or ConversationProjectChanged)
                {
                    expectedPosition++;
                    continue;
                }

                return Reject(ConversationErrorCode.IdempotencyConflict, "duplicate_event_identity");
            }

            try
            {
                Apply(state, record.Event);
            }
            catch (ArgumentException)
            {
                return Reject(ConversationErrorCode.CommandValidationFailed, "malformed_payload");
            }
            catch (InvalidOperationException)
            {
                return Reject(ConversationErrorCode.IdempotencyConflict, "replay_invariant_violation");
            }

            expectedPosition++;
        }

        return state.IsCreated
            ? ConversationReplayResult.Replayed(state)
            : Reject(ConversationErrorCode.AggregateNotFound, "conversation_not_created");
    }

    private static ConversationReplayResult Reject(ConversationErrorCode errorCode, string diagnosticCode)
        => ConversationReplayResult.Rejected(errorCode, diagnosticCode);

    private static void Apply(ConversationState state, object e)
    {
        switch (e)
        {
            case ConversationCreatedDomainEvent created:
                state.Apply(created);
                break;
            case ParticipantAddedDomainEvent participant:
                state.Apply(participant);
                break;
            case ConversationProjectChangedDomainEvent projectChanged:
                state.Apply(projectChanged);
                break;
            case RetentionPolicySetDomainEvent retentionSet:
                state.Apply(retentionSet);
                break;
            case RetentionPolicyReplacedDomainEvent retentionReplaced:
                state.Apply(retentionReplaced);
                break;
            case ConversationContentMarkedSensitiveDomainEvent sensitive:
                state.Apply(sensitive);
                break;
            case MessageContentRedactedDomainEvent redacted:
                state.Apply(redacted);
                break;
            case ConversationCreated created:
                state.Apply(created);
                break;
            case ParticipantAdded participant:
                state.Apply(participant);
                break;
            case ConversationProjectChanged projectChanged:
                state.Apply(projectChanged);
                break;
            case RetentionPolicySet retentionSet:
                state.Apply(retentionSet);
                break;
            case RetentionPolicyReplaced retentionReplaced:
                state.Apply(retentionReplaced);
                break;
            case ConversationContentMarkedSensitive sensitive:
                state.Apply(sensitive);
                break;
            case MessageContentRedacted redacted:
                state.Apply(redacted);
                break;
            case MessageAppended message:
                state.Apply(message);
                break;
            case FileReferenceAttached file:
                state.Apply(file);
                break;
            case ConversationMetadataUpdated metadataUpdated:
                state.Apply(metadataUpdated);
                break;
            case ConversationClosed closed:
                state.Apply(closed);
                break;
            case ConversationArchived archived:
                state.Apply(archived);
                break;
            case ConversationLifecycleChanged lifecycle:
                state.Apply(lifecycle);
                break;
            default:
                throw new ArgumentException("Unsupported conversation replay event.", nameof(e));
        }
    }

    private static bool EventTypeMatchesPayload(object e, ConversationEventType eventType)
        => e switch
        {
            ConversationCreatedDomainEvent or ConversationCreated => eventType == ConversationEventType.ConversationCreated,
            ParticipantAddedDomainEvent or ParticipantAdded => eventType == ConversationEventType.ParticipantAdded,
            ConversationProjectChangedDomainEvent or ConversationProjectChanged =>
                eventType == ConversationEventType.ConversationProjectChanged,
            RetentionPolicySetDomainEvent or RetentionPolicySet => eventType == ConversationEventType.RetentionPolicySet,
            RetentionPolicyReplacedDomainEvent or RetentionPolicyReplaced => eventType == ConversationEventType.RetentionPolicyReplaced,
            ConversationContentMarkedSensitiveDomainEvent or ConversationContentMarkedSensitive =>
                eventType == ConversationEventType.ConversationContentMarkedSensitive,
            MessageContentRedactedDomainEvent or MessageContentRedacted => eventType == ConversationEventType.MessageContentRedacted,
            MessageAppended => eventType == ConversationEventType.MessageAppended,
            FileReferenceAttached => eventType == ConversationEventType.FileReferenceAttached,
            ConversationMetadataUpdated => eventType == ConversationEventType.ConversationMetadataUpdated,
            ConversationClosed => eventType == ConversationEventType.ConversationClosed,
            ConversationArchived => eventType == ConversationEventType.ConversationArchived,
            ConversationLifecycleChanged => eventType == ConversationEventType.ConversationLifecycleChanged,
            _ => false,
        };

    private static ConversationEventMetadata? TryGetMetadata(object e)
        => e switch
        {
            ConversationCreatedDomainEvent created => created.Metadata,
            ParticipantAddedDomainEvent participant => participant.Metadata,
            ConversationProjectChangedDomainEvent projectChanged => projectChanged.Metadata,
            RetentionPolicySetDomainEvent retentionSet => retentionSet.Metadata,
            RetentionPolicyReplacedDomainEvent retentionReplaced => retentionReplaced.Metadata,
            ConversationContentMarkedSensitiveDomainEvent sensitive => sensitive.Metadata,
            MessageContentRedactedDomainEvent redacted => redacted.Metadata,
            ConversationCreated created => created.Metadata,
            ParticipantAdded participant => participant.Metadata,
            ConversationProjectChanged projectChanged => projectChanged.Metadata,
            RetentionPolicySet retentionSet => retentionSet.Metadata,
            RetentionPolicyReplaced retentionReplaced => retentionReplaced.Metadata,
            ConversationContentMarkedSensitive sensitive => sensitive.Metadata,
            MessageContentRedacted redacted => redacted.Metadata,
            MessageAppended message => message.Metadata,
            FileReferenceAttached file => file.Metadata,
            ConversationMetadataUpdated update => update.Metadata,
            ConversationClosed closed => closed.Metadata,
            ConversationArchived archived => archived.Metadata,
            ConversationLifecycleChanged lifecycle => lifecycle.Metadata,
            _ => null,
        };
}
