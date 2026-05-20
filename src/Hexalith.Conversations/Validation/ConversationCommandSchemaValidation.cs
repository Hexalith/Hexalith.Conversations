// <copyright file="ConversationCommandSchemaValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;

namespace Hexalith.Conversations.Validation;

/// <summary>
/// Validates public command envelope fields shared by every conversation command type.
/// </summary>
internal static class ConversationCommandSchemaValidation
{
    /// <summary>
    /// Validates command metadata common to every public command shape.
    /// </summary>
    /// <param name="command">The public command contract.</param>
    /// <returns>A typed rejection when the common command envelope is invalid; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateEnvelope(object? command)
    {
        if (command is null)
        {
            return Reject(ConversationErrorCode.CommandValidationFailed, "command_missing");
        }

        ConversationCommandMetadata? metadata = command switch
        {
            CreateConversationCommand create => create.Metadata,
            AppendMessageCommand append => append.Metadata,
            AddParticipantCommand add => add.Metadata,
            AttachFileReferenceCommand attach => attach.Metadata,
            UpdateConversationMetadataCommand update => update.Metadata,
            CloseConversationCommand close => close.Metadata,
            ArchiveConversationCommand archive => archive.Metadata,
            _ => throw new ArgumentException($"Unsupported conversation command type '{command.GetType().FullName}'.", nameof(command)),
        };

        return ValidateMetadata(metadata);
    }

    /// <summary>
    /// Validates shared metadata fields.
    /// </summary>
    /// <param name="metadata">The command metadata.</param>
    /// <returns>A typed rejection when metadata is invalid; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateMetadata(ConversationCommandMetadata? metadata)
    {
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

        if (!metadata.SchemaVersion.Equals(SchemaVersion.Current))
        {
            return Reject(
                ConversationErrorCode.SchemaVersionUnsupported,
                "unsupported_schema_version",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (string.IsNullOrWhiteSpace(metadata.IdempotencyKey))
        {
            return Reject(
                ConversationErrorCode.IdempotencyKeyMissing,
                "idempotency_key_missing",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
    }

    private static ConversationRejectedDomainEvent Reject(
        ConversationErrorCode code,
        string reasonCode,
        SchemaVersion? schemaVersion = null,
        string? correlationId = null,
        string? causationId = null)
        => new(code, reasonCode, schemaVersion, correlationId, causationId);
}
