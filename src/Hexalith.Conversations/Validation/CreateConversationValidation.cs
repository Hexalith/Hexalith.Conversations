// <copyright file="CreateConversationValidation.cs" company="ITANEO">
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
/// Validates create-conversation commands without external lookups.
/// </summary>
internal static class CreateConversationValidation
{
    /// <summary>
    /// Validates a create-conversation command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? Validate(CreateConversation? command, ConversationState? state)
    {
        if (command is null)
        {
            return Reject(ConversationErrorCode.CommandValidationFailed, "command_missing");
        }

        CreateConversationCommand? publicCommand = command.PublicCommand;
        if (publicCommand is null)
        {
            return Reject(ConversationErrorCode.CommandValidationFailed, "public_command_missing");
        }

        ConversationCommandMetadata? metadata = publicCommand.Metadata;
        if (metadata is null)
        {
            return Reject(ConversationErrorCode.CommandValidationFailed, "metadata_missing");
        }

        if (metadata.TenantId is null)
        {
            return Reject(ConversationErrorCode.TenantBindingMissing, "tenant_binding_missing");
        }

        if (metadata.SchemaVersion is null)
        {
            return Reject(ConversationErrorCode.SchemaVersionUnsupported, "schema_version_missing");
        }

        if (metadata.ActorPartyId is null)
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "actor_party_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (!metadata.SchemaVersion.Equals(SchemaVersion.Current))
        {
            return Reject(
                ConversationErrorCode.SchemaVersionUnsupported,
                "unsupported_schema_version",
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

        if (string.IsNullOrWhiteSpace(command.EventId))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "event_identity_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (!IsBusinessTimestamp(command.CreatedAt))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "created_timestamp_invalid",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (state?.IsCreated == true)
        {
            return Reject(
                ConversationErrorCode.IdempotencyConflict,
                "conversation_already_created",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (UsesCorrelationOrReferenceAsIdentity(command.ConversationId, command.EventId, publicCommand, metadata))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "identity_substitution_forbidden",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
    }

    private static bool UsesCorrelationOrReferenceAsIdentity(
        ConversationId conversationId,
        string eventId,
        CreateConversationCommand command,
        ConversationCommandMetadata metadata)
    {
        string value = conversationId.Value;

        if (EqualsOrdinal(value, metadata.TenantId.Value)
            || EqualsOrdinal(value, metadata.ActorPartyId.Value)
            || EqualsOrdinal(value, metadata.CorrelationId)
            || EqualsOrdinal(value, metadata.CausationId)
            || EqualsOrdinal(value, metadata.IdempotencyKey)
            || EqualsOrdinal(value, eventId)
            || EqualsOrdinal(value, command.BusinessReference?.System)
            || EqualsOrdinal(value, command.BusinessReference?.Value)
            || EqualsOrdinal(value, command.ProjectId?.Value)
            || EqualsOrdinal(value, command.FolderId?.Value)
            || EqualsOrdinal(value, command.Label))
        {
            return true;
        }

        return ProviderCorrelationCarriesIdentity(value, command.ProviderCorrelation);
    }

    private static bool ProviderCorrelationCarriesIdentity(string value, ProviderCorrelationMetadata? provider)
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

        foreach (KeyValuePair<string, string> entry in provider.ExtensionData)
        {
            if (EqualsOrdinal(value, entry.Key) || EqualsOrdinal(value, entry.Value))
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
