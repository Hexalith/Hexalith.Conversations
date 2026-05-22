// <copyright file="ConversationPublicationMapper.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Maps durable Conversations facts to public publication contracts.
/// </summary>
public static class ConversationPublicationMapper
{
    /// <summary>
    /// Maps a persisted candidate to a publishable public event or a bounded diagnostic.
    /// </summary>
    /// <param name="persisted">The persisted candidate.</param>
    /// <returns>The publication result.</returns>
    public static ConversationPublicationResult TryMap(PersistedConversationEvent persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);

        if (persisted.Outcome != ConversationPersistenceOutcome.Succeeded)
        {
            return ConversationPublicationResult.Rejected(new ConversationPublicationDiagnostic(
                DiagnosticCodeFor(persisted.Outcome)));
        }

        return TryCreatePublicEvent(persisted.Payload) is { } publicEvent
            ? ValidateAndPublish(persisted, publicEvent)
            : ConversationPublicationResult.Rejected(new ConversationPublicationDiagnostic(ConversationErrorCode.CommandValidationFailed));
    }

    private static ConversationPublicationResult ValidateAndPublish(PersistedConversationEvent persisted, object publicEvent)
    {
        ConversationEventMetadata? metadata = ConversationPublicationMetadata.GetMetadata(publicEvent);
        if (metadata is null)
        {
            return ConversationPublicationResult.Rejected(new ConversationPublicationDiagnostic(ConversationErrorCode.CommandValidationFailed));
        }

        if (!persisted.TenantId.Equals(metadata.TenantId))
        {
            return ConversationPublicationResult.Rejected(CreateDiagnostic(ConversationErrorCode.TenantContextMismatch, metadata));
        }

        if (metadata.SchemaVersion.Value != SchemaVersion.Current.Value)
        {
            return ConversationPublicationResult.Rejected(CreateDiagnostic(ConversationErrorCode.SchemaVersionUnsupported, metadata));
        }

        if (!ConversationPublicationMetadata.EventTypeMatches(publicEvent, metadata.EventType))
        {
            return ConversationPublicationResult.Rejected(CreateDiagnostic(ConversationErrorCode.CommandValidationFailed, metadata));
        }

        return ConversationPublicationResult.Published(publicEvent);
    }

    private static object? TryCreatePublicEvent(object payload)
        => payload switch
        {
            ConversationCreated e => e,
            ParticipantAdded e => e,
            MessageAppended e => e,
            FileReferenceAttached e => e,
            ConversationMetadataUpdated e => e,
            ConversationClosed e => ToLifecycleChanged(e, ConversationLifecycleStatus.Open, ConversationLifecycleStatus.Closed),
            ConversationArchived e => ToLifecycleChanged(e, ConversationLifecycleStatus.Closed, ConversationLifecycleStatus.Archived),
            ConversationLifecycleChanged e => e,
            RetentionPolicySet e => e,
            RetentionPolicyReplaced e => e,
            ConversationContentMarkedSensitive e => e,
            MessageContentRedacted e => e,
            ConversationCreatedDomainEvent e => new ConversationCreated(
                e.Metadata,
                e.BusinessReference,
                e.ProjectId,
                e.FolderId,
                e.Label,
                e.ProviderCorrelation),
            ParticipantAddedDomainEvent e => new ParticipantAdded(
                e.Metadata,
                e.ParticipantPartyId,
                e.ParticipantType,
                e.ParticipantRole),
            RetentionPolicySetDomainEvent e => new RetentionPolicySet(
                e.Metadata,
                e.PolicyReference,
                e.Rationale,
                e.AuditEvidence),
            RetentionPolicyReplacedDomainEvent e => new RetentionPolicyReplaced(
                e.Metadata,
                e.PolicyReference,
                e.PreviousPolicyReference,
                e.Rationale,
                e.AuditEvidence),
            ConversationContentMarkedSensitiveDomainEvent e => new ConversationContentMarkedSensitive(
                e.Metadata,
                e.Target,
                e.Category,
                e.PolicyReference,
                e.Rationale,
                e.AuditEvidence),
            MessageContentRedactedDomainEvent e => new MessageContentRedacted(
                e.Metadata,
                e.Target,
                e.Category,
                e.PolicyReference,
                e.Rationale,
                e.AuditEvidence),
            _ => null,
        };

    private static ConversationLifecycleChanged ToLifecycleChanged(
        ConversationClosed closed,
        ConversationLifecycleStatus previous,
        ConversationLifecycleStatus current)
        => new(
            closed.Metadata with
            {
                EventType = ConversationEventType.ConversationLifecycleChanged,
            },
            previous,
            current,
            closed.ReasonCode);

    private static ConversationLifecycleChanged ToLifecycleChanged(
        ConversationArchived archived,
        ConversationLifecycleStatus previous,
        ConversationLifecycleStatus current)
        => new(
            archived.Metadata with
            {
                EventType = ConversationEventType.ConversationLifecycleChanged,
            },
            previous,
            current,
            archived.ReasonCode);

    private static ConversationPublicationDiagnostic CreateDiagnostic(
        ConversationErrorCode code,
        ConversationEventMetadata metadata)
        => new(
            code,
            metadata.SchemaVersion,
            metadata.EventType,
            metadata.TenantId,
            metadata.ConversationId,
            metadata.EventId,
            metadata.CorrelationId,
            metadata.CausationId);

    private static ConversationErrorCode DiagnosticCodeFor(ConversationPersistenceOutcome outcome)
        => outcome switch
        {
            ConversationPersistenceOutcome.IdempotencyConflict => ConversationErrorCode.IdempotencyConflict,
            ConversationPersistenceOutcome.FailedTenantCheck => ConversationErrorCode.TenantIsolationViolation,
            ConversationPersistenceOutcome.FailedParticipantValidation => ConversationErrorCode.ParticipantValidationUnavailable,
            _ => ConversationErrorCode.CommandValidationFailed,
        };
}
