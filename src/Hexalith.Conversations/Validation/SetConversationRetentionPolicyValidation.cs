// <copyright file="SetConversationRetentionPolicyValidation.cs" company="ITANEO">
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
/// Validates retention policy domain commands without external lookups.
/// </summary>
internal static class SetConversationRetentionPolicyValidation
{
    public static ConversationRejectedDomainEvent? Validate(SetConversationRetentionPolicy? command, ConversationState? state)
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

        SetConversationRetentionPolicyCommand publicCommand = command!.PublicCommand;

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

        if (state.LastEventAt is { } lastEventAt && publicCommand.OperationTimestamp < lastEventAt)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "retention_timestamp_not_monotonic",
                publicCommand.Metadata.SchemaVersion,
                publicCommand.Metadata.CorrelationId,
                publicCommand.Metadata.CausationId);
        }

        return null;
    }

    public static ConversationRejectedDomainEvent? ValidateSchemaShape(SetConversationRetentionPolicyCommand? command)
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
        SetConversationRetentionPolicyCommand command,
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

        return null;
    }

    public static ConversationRejectedDomainEvent? ValidateAuditEvidenceProvided(
        SetConversationRetentionPolicyCommand command,
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

    private static ConversationRejectedDomainEvent? ValidateShape(
        SetConversationRetentionPolicyCommand? command,
        string? eventId)
    {
        ConversationRejectedDomainEvent? schemaRejection = ValidateSchemaShape(command);
        return schemaRejection ?? ValidateSemanticShape(command!, eventId);
    }

    private static ConversationRejectedDomainEvent Reject(
        ConversationErrorCode code,
        string reasonCode,
        SchemaVersion? schemaVersion = null,
        string? correlationId = null,
        string? causationId = null)
        => new(code, reasonCode, schemaVersion, correlationId, causationId);
}
