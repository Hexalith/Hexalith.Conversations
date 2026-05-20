// <copyright file="AddParticipantValidation.cs" company="ITANEO">
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
/// Validates add-participant commands without external lookups.
/// </summary>
internal static class AddParticipantValidation
{
    /// <summary>
    /// Validates an add-participant command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? Validate(AddParticipant? command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? shapeRejection = ValidateShape(
            command?.PublicCommand,
            command?.AddedAt ?? default,
            command?.EventId);

        if (shapeRejection is not null)
        {
            return shapeRejection;
        }

        AddParticipantCommand publicCommand = command!.PublicCommand;
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

        // Aggregate-side tenant invariant violation: the persisted state's tenant binding does not match
        // the command metadata's tenant. This is distinct from the application-boundary
        // ParticipantDirectoryValidationStatus.TenantMismatch path (which uses TenantContextMismatch)
        // so that the future Story 1.5 tenant-access gate has a single boundary-side seam to wrap.
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

        // Non-Open lifecycle is collapsed to a single content-safe reason so callers cannot
        // distinguish Closed from Archived (or any future lifecycle value) from a rejection.
        if (state.Lifecycle != ConversationLifecycleState.Open)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "conversation_not_open",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        // Provider-identity substitution runs before duplicate-membership so an attempt to use
        // a provider correlation value as identity surfaces in the audit trail as the
        // identity-substitution rejection, not silently as a duplicate.
        if (UsesProviderCorrelationAsIdentity(publicCommand.ParticipantPartyId.Value, publicCommand.ProviderCorrelation))
        {
            return Reject(
                ConversationErrorCode.ProviderOnlyIdentityForbidden,
                "provider_identity_not_authority",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (state.HasParticipant(publicCommand.ParticipantPartyId, publicCommand.ParticipantType, publicCommand.ParticipantRole))
        {
            return Reject(
                ConversationErrorCode.DuplicateParticipant,
                "participant_membership_duplicate",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        // addedAt monotonicity: the deterministic timestamp must be no earlier than the last event,
        // preventing back-dated participant events from corrupting projection ordering.
        if (state.LastEventAt is { } lastEventAt && command!.AddedAt < lastEventAt)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "participant_timestamp_not_monotonic",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
    }

    /// <summary>
    /// Validates command shape before application-boundary Party lookup.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <param name="addedAt">The deterministic participant-added timestamp.</param>
    /// <param name="eventId">The deterministic event identity.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateShape(
        AddParticipantCommand? command,
        DateTimeOffset addedAt,
        string? eventId)
    {
        ConversationRejectedDomainEvent? schemaRejection = ValidateSchemaShape(command);
        if (schemaRejection is not null)
        {
            return schemaRejection;
        }

        return ValidateSemanticShape(command!, addedAt, eventId);
    }

    /// <summary>
    /// Validates the shape fields that must be present before the tenant access guard can run.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    /// <remarks>
    /// Only checks fields whose vocabulary is already public via the contract package and that are
    /// strictly required to authorize the request: presence of command, metadata, tenant binding,
    /// and schema version. Semantic shape (party id, type, role, conversation id, event id, timestamp)
    /// is validated post-authorization to avoid fingerprinting the validation surface to unauthorized
    /// callers.
    /// </remarks>
    public static ConversationRejectedDomainEvent? ValidateSchemaShape(AddParticipantCommand? command)
    {
        return ConversationCommandSchemaValidation.ValidateEnvelope(command);
    }

    /// <summary>
    /// Validates the semantic shape fields that must be checked after the tenant access guard allows the request.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <param name="addedAt">The deterministic participant-added timestamp.</param>
    /// <param name="eventId">The deterministic event identity.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        AddParticipantCommand command,
        DateTimeOffset addedAt,
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

        if (command.ParticipantPartyId is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "participant_party_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command.ParticipantType is null)
        {
            return Reject(
                ConversationErrorCode.UnsupportedParticipant,
                "participant_type_unsupported",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command.ParticipantRole is null)
        {
            return Reject(
                ConversationErrorCode.UnsupportedParticipant,
                "participant_role_unsupported",
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

        if (!IsBusinessTimestamp(addedAt))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "participant_timestamp_invalid",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
    }

    private static bool UsesProviderCorrelationAsIdentity(string value, ProviderCorrelationMetadata? provider)
    {
        if (provider is null)
        {
            return false;
        }

        if (EqualsOrdinal(value, provider.ProviderName)
            || EqualsOrdinal(value, provider.ProviderType)
            || EqualsOrdinal(value, provider.ProviderSessionReference)
            || EqualsOrdinal(value, provider.ProviderResponseReference))
        {
            return true;
        }

        if (provider.ExtensionData is null)
        {
            return false;
        }

        // Only the dictionary values are candidate identity values. Keys are vocabulary
        // (e.g., "region", "thread") and matching them against a PartyId would produce
        // false-positive rejections for any PartyId that happened to equal a key name.
        foreach (KeyValuePair<string, string> entry in provider.ExtensionData)
        {
            if (EqualsOrdinal(value, entry.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBusinessTimestamp(DateTimeOffset value)
        => value > DateTimeOffset.MinValue && value.Year is >= 2000 and <= 9999;

    private static bool EqualsOrdinal(string value, string? candidate)
        => !string.IsNullOrWhiteSpace(candidate) && string.Equals(value, candidate, StringComparison.Ordinal);

    private static ConversationRejectedDomainEvent Reject(
        ConversationErrorCode code,
        string reasonCode,
        SchemaVersion? schemaVersion = null,
        string? correlationId = null,
        string? causationId = null)
        => new(code, reasonCode, schemaVersion, correlationId, causationId);
}
