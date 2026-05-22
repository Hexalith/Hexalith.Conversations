// <copyright file="MarkConversationContentSensitiveValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;

namespace Hexalith.Conversations.Validation;

/// <summary>
/// Validates sensitivity mark domain commands without external lookups.
/// </summary>
internal static class MarkConversationContentSensitiveValidation
{
    public static ConversationRejectedDomainEvent? Validate(MarkConversationContentSensitive? command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? shapeRejection = ValidateShape(command?.PublicCommand, command?.EventId);
        if (shapeRejection is not null)
        {
            return shapeRejection;
        }

        ConversationRejectedDomainEvent? auditRejection = ValidateAuditEvidenceProvided(command!.PublicCommand, command.AuditEvidence);
        if (auditRejection is not null)
        {
            return auditRejection;
        }

        MarkConversationContentSensitiveCommand publicCommand = command.PublicCommand;

        if (state is null || !state.IsCreated)
        {
            return Reject(
                ConversationErrorCode.AggregateNotFound,
                "conversation_not_found",
                publicCommand.Metadata.SchemaVersion,
                publicCommand.Metadata.CorrelationId,
                publicCommand.Metadata.CausationId);
        }

        if (state.TenantId != publicCommand.Metadata.TenantId || state.ConversationId != publicCommand.ConversationId)
        {
            return Reject(
                ConversationErrorCode.TenantIsolationViolation,
                "tenant_isolation_violation",
                publicCommand.Metadata.SchemaVersion,
                publicCommand.Metadata.CorrelationId,
                publicCommand.Metadata.CausationId);
        }

        if (state.Lifecycle != ConversationLifecycleState.Open)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "conversation_not_open",
                publicCommand.Metadata.SchemaVersion,
                publicCommand.Metadata.CorrelationId,
                publicCommand.Metadata.CausationId);
        }

        if (!TargetExists(publicCommand.Target, state))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "sensitivity_target_invalid",
                publicCommand.Metadata.SchemaVersion,
                publicCommand.Metadata.CorrelationId,
                publicCommand.Metadata.CausationId);
        }

        if (state.LastEventAt is { } lastEventAt && publicCommand.OperationTimestamp < lastEventAt)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "sensitivity_timestamp_not_monotonic",
                publicCommand.Metadata.SchemaVersion,
                publicCommand.Metadata.CorrelationId,
                publicCommand.Metadata.CausationId);
        }

        if (state.TryGetSensitivityMark(ConversationState.SensitivityTargetKey(publicCommand.Target), out ConversationSensitivityMarkState? mark)
            && mark is not null
            && !IsCompatible(publicCommand, mark))
        {
            return Reject(
                ConversationErrorCode.IdempotencyConflict,
                "sensitivity_mark_conflict",
                publicCommand.Metadata.SchemaVersion,
                publicCommand.Metadata.CorrelationId,
                publicCommand.Metadata.CausationId);
        }

        return null;
    }

    public static ConversationRejectedDomainEvent? ValidateSchemaShape(MarkConversationContentSensitiveCommand? command)
    {
        if (command is null)
        {
            return Reject(ConversationErrorCode.CommandValidationFailed, "command_missing");
        }

        if (command.Metadata is null)
        {
            return Reject(ConversationErrorCode.CommandValidationFailed, "metadata_missing");
        }

        if (command.Metadata.SchemaVersion is null)
        {
            return Reject(ConversationErrorCode.SchemaVersionUnsupported, "schema_version_missing");
        }

        if (command.Metadata.SchemaVersion != SchemaVersion.Current)
        {
            return Reject(
                ConversationErrorCode.SchemaVersionUnsupported,
                "unsupported_schema_version",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
        }

        if (command.Metadata.TenantId is null)
        {
            return Reject(
                ConversationErrorCode.TenantBindingMissing,
                "tenant_binding_missing",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
        }

        return null;
    }

    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        MarkConversationContentSensitiveCommand command,
        string? eventId)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Metadata.ActorPartyId is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "actor_party_missing",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
        }

        if (command.ConversationId is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "conversation_identity_missing",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
        }

        if (string.IsNullOrWhiteSpace(eventId))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "event_identity_missing",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
        }

        if (command.Target is null || command.Category is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "sensitivity_payload_missing",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
        }

        if (!TargetShapeIsSupported(command.Target))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "sensitivity_target_invalid",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId);
        }

        return null;
    }

    public static ConversationRejectedDomainEvent? ValidateAuditEvidenceProvided(
        MarkConversationContentSensitiveCommand command,
        GovernanceAuditEvidenceReference? auditEvidence)
    {
        ArgumentNullException.ThrowIfNull(command);

        return auditEvidence is null
            ? Reject(
                ConversationErrorCode.AuditPairingRequired,
                "audit_pairing_required",
                command.Metadata.SchemaVersion,
                command.Metadata.CorrelationId,
                command.Metadata.CausationId)
            : null;
    }

    public static bool IsCompatible(MarkConversationContentSensitiveCommand command, ConversationSensitivityMarkState mark)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(mark);

        return mark.Target == command.Target
            && mark.Category == command.Category
            && mark.PolicyReference == command.PolicyReference
            && mark.Rationale == command.Rationale;
    }

    private static ConversationRejectedDomainEvent? ValidateShape(
        MarkConversationContentSensitiveCommand? command,
        string? eventId)
    {
        ConversationRejectedDomainEvent? schemaRejection = ValidateSchemaShape(command);
        return schemaRejection ?? ValidateSemanticShape(command!, eventId);
    }

    private static bool TargetShapeIsSupported(GovernanceTarget target)
    {
        if (target.Kind == GovernedTargetKind.Conversation)
        {
            return target.MessageId is null && target.FileId is null && target.PartyId is null && target.SegmentReference is null;
        }

        if (target.Kind == GovernedTargetKind.Message)
        {
            return target.MessageId is not null && target.FileId is null && target.PartyId is null && target.SegmentReference is null;
        }

        if (target.Kind == GovernedTargetKind.File)
        {
            return target.FileId is not null && target.MessageId is null && target.PartyId is null && target.SegmentReference is null;
        }

        if (target.Kind == GovernedTargetKind.Participant)
        {
            return target.PartyId is not null && target.MessageId is null && target.FileId is null && target.SegmentReference is null;
        }

        if (target.Kind == GovernedTargetKind.ContentSegment)
        {
            return !string.IsNullOrWhiteSpace(target.SegmentReference)
                && target.MessageId is null
                && target.FileId is null
                && target.PartyId is null;
        }

        return false;
    }

    private static bool TargetExists(GovernanceTarget target, ConversationState state)
    {
        if (target.Kind == GovernedTargetKind.Conversation)
        {
            return true;
        }

        if (target.Kind == GovernedTargetKind.Message)
        {
            return state.Messages.Any(message => message.MessageId == target.MessageId);
        }

        if (target.Kind == GovernedTargetKind.File)
        {
            return state.FileReferences.Any(file => file.FileId == target.FileId);
        }

        if (target.Kind == GovernedTargetKind.Participant)
        {
            return state.Participants.Any(participant => participant.PartyId == target.PartyId);
        }

        return target.Kind == GovernedTargetKind.ContentSegment && !string.IsNullOrWhiteSpace(target.SegmentReference);
    }

    private static ConversationRejectedDomainEvent Reject(
        ConversationErrorCode code,
        string reasonCode,
        SchemaVersion? schemaVersion = null,
        string? correlationId = null,
        string? causationId = null)
        => new(code, reasonCode, schemaVersion, correlationId, causationId);
}
