// <copyright file="ReassignConversationProjectValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;

namespace Hexalith.Conversations.Validation;

/// <summary>
/// Validates project reassignment commands without external lookups.
/// </summary>
internal static class ReassignConversationProjectValidation
{
    /// <summary>
    /// Validates a project reassignment command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? Validate(ReassignConversationProject? command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? shapeRejection = ValidateShape(
            command?.PublicCommand,
            command?.ChangedAt ?? default,
            command?.EventId);

        if (shapeRejection is not null)
        {
            return shapeRejection;
        }

        ReassignConversationProjectCommand publicCommand = command!.PublicCommand;
        ConversationCommandMetadata metadata = publicCommand.Metadata;

        if (state is null || !state.IsCreated)
        {
            return Reject(
                ConversationErrorCode.AggregateNotFound,
                "conversation_not_found",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (state.TenantId != metadata.TenantId)
        {
            return Reject(
                ConversationErrorCode.TenantContextMismatch,
                "aggregate_tenant_invariant_violation",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (state.ConversationId != publicCommand.ConversationId)
        {
            return Reject(
                ConversationErrorCode.AggregateNotFound,
                "conversation_identity_mismatch",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (state.Lifecycle != ConversationLifecycleState.Open)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "conversation_not_open",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (publicCommand.ExpectedCurrentProjectId is not null
            && state.ProjectId != publicCommand.ExpectedCurrentProjectId)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "project_current_mismatch",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (state.LastEventAt is { } lastEventAt && command!.ChangedAt < lastEventAt)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "project_changed_timestamp_not_monotonic",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
    }

    /// <summary>
    /// Validates command shape before application-boundary tenant access.
    /// </summary>
    /// <param name="command">The public reassignment command.</param>
    /// <param name="changedAt">The deterministic project-change timestamp.</param>
    /// <param name="eventId">The deterministic event identity.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateShape(
        ReassignConversationProjectCommand? command,
        DateTimeOffset changedAt,
        string? eventId)
    {
        ConversationRejectedDomainEvent? schemaRejection = ValidateSchemaShape(command);
        if (schemaRejection is not null)
        {
            return schemaRejection;
        }

        return ValidateSemanticShape(command!, changedAt, eventId);
    }

    /// <summary>
    /// Validates fields that must be present before the tenant access guard can run.
    /// </summary>
    /// <param name="command">The public reassignment command.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateSchemaShape(ReassignConversationProjectCommand? command)
        => ConversationCommandSchemaValidation.ValidateEnvelope(command);

    /// <summary>
    /// Validates the semantic shape fields checked after tenant access is allowed.
    /// </summary>
    /// <param name="command">The public reassignment command.</param>
    /// <param name="changedAt">The deterministic project-change timestamp.</param>
    /// <param name="eventId">The deterministic event identity.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        ReassignConversationProjectCommand command,
        DateTimeOffset changedAt,
        string? eventId)
    {
        ArgumentNullException.ThrowIfNull(command);
        ConversationCommandMetadata metadata = command.Metadata!;

        if (metadata.ActorPartyId is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "actor_party_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command.ConversationId is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "conversation_identity_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command.Target is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "project_assignment_target_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command.Target.Operation is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "project_assignment_operation_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command.Target.Operation == ConversationProjectAssignmentOperation.Assign
            && command.Target.ProjectId is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "project_assignment_target_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command.Target.Operation == ConversationProjectAssignmentOperation.Clear
            && command.Target.ProjectId is not null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "project_clear_target_must_be_null",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (string.IsNullOrWhiteSpace(eventId))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "event_identity_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (!IsBusinessTimestamp(changedAt))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "project_changed_timestamp_invalid",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
    }

    private static bool IsBusinessTimestamp(DateTimeOffset value)
        => value > DateTimeOffset.MinValue && value.Year is >= 2000 and <= 9999;

    private static ConversationRejectedDomainEvent Reject(
        ConversationErrorCode code,
        string reasonCode,
        SchemaVersion? schemaVersion = null,
        string? correlationId = null,
        string? causationId = null)
        => new(code, reasonCode, schemaVersion, correlationId, causationId);
}
