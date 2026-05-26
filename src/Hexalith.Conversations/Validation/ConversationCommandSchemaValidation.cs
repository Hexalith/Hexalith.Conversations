// <copyright file="ConversationCommandSchemaValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
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
            ReassignConversationProjectCommand reassignProject => reassignProject.Metadata,
            CloseConversationCommand close => close.Metadata,
            ArchiveConversationCommand archive => archive.Metadata,
            _ => throw new ArgumentException($"Unsupported conversation command type '{command.GetType().FullName}'.", nameof(command)),
        };

        ConversationRejectedDomainEvent? envelopeRejection = ValidateMetadata(metadata);
        if (envelopeRejection is not null)
        {
            return envelopeRejection;
        }

        return ValidateCallerMetadata(command, metadata);
    }

    /// <summary>
    /// Bounds caller-supplied provenance metadata at the command boundary.
    /// </summary>
    /// <remarks>
    /// Caller metadata is provenance only and is validated AFTER the shared envelope (tenant binding, schema version,
    /// idempotency key); it never participates in tenant scope, authorization, command eligibility, or trust state.
    /// This mirrors the idempotency-key bounding precedent: deterministic size/count caps and control-character
    /// rejection, returning a typed <see cref="ConversationRejectedDomainEvent"/> (<c>command_validation_failed</c>)
    /// with a bounded reason code rather than echoing any caller-supplied value. It also bounds the existing safe
    /// adopter metadata bag on <see cref="UpdateConversationMetadataCommand"/>, which was previously unbounded.
    /// </remarks>
    /// <param name="command">The public command contract.</param>
    /// <param name="metadata">The already-validated shared command metadata.</param>
    /// <returns>A typed rejection when caller metadata is out of bounds; otherwise <see langword="null" />.</returns>
    private static ConversationRejectedDomainEvent? ValidateCallerMetadata(object command, ConversationCommandMetadata metadata)
    {
        CallerMetadata? callerMetadata = command switch
        {
            CreateConversationCommand create => create.CallerMetadata,
            AppendMessageCommand append => append.CallerMetadata,
            UpdateConversationMetadataCommand update => update.CallerMetadata,
            ReassignConversationProjectCommand reassignProject => reassignProject.CallerMetadata,
            _ => null,
        };

        if (!CallerMetadata.TryValidateBounds(callerMetadata, out string? callerReason))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                callerReason ?? "caller_metadata_invalid",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        if (command is UpdateConversationMetadataCommand updateCommand
            && !CallerMetadata.TryValidateMetadataBag(updateCommand.Attributes, out string? attributesReason))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                attributesReason ?? "caller_metadata_invalid",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
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

        // P51 review fix (2026-05-20): the idempotency key is used as a dictionary key, baked into the audit-handle hash material,
        // and surfaced into diagnostics. Cap length and forbid control characters at the envelope boundary so storage, hashing,
        // and log-redaction paths cannot be abused by callers.
        if (metadata.IdempotencyKey.Length > IdempotencyKeyMaxLength
            || ContainsControlCharacter(metadata.IdempotencyKey))
        {
            return Reject(
                ConversationErrorCode.CommandValidationFailed,
                "idempotency_key_invalid",
                metadata.SchemaVersion,
                metadata.CorrelationId,
                metadata.CausationId);
        }

        return null;
    }

    private const int IdempotencyKeyMaxLength = 200;

    private static bool ContainsControlCharacter(string value)
    {
        foreach (char c in value)
        {
            if (char.IsControl(c))
            {
                return true;
            }
        }

        return false;
    }

    private static ConversationRejectedDomainEvent Reject(
        ConversationErrorCode code,
        string reasonCode,
        SchemaVersion? schemaVersion = null,
        string? correlationId = null,
        string? causationId = null)
        => new(code, reasonCode, schemaVersion, correlationId, causationId);
}
