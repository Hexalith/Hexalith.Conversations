// <copyright file="ConversationIdempotencyOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Stores the minimal logical result metadata needed for idempotent replay.
/// </summary>
/// <param name="Category">The stable logical outcome category.</param>
/// <param name="SchemaVersion">The outcome schema version.</param>
/// <param name="TenantId">The tenant binding.</param>
/// <param name="CommandType">The public command type.</param>
/// <param name="ConversationId">The durable conversation identity, when relevant.</param>
/// <param name="MessageId">The durable message identity, when relevant.</param>
/// <param name="ParticipantPartyId">The durable participant Party identity, when relevant.</param>
/// <param name="FileId">The durable file reference identity, when relevant.</param>
/// <param name="RejectionCode">The typed rejection code, when relevant.</param>
/// <param name="OriginalReasonCode">The original typed rejection reason code, when relevant.</param>
/// <param name="IsRetryable">A value indicating whether retry is meaningful.</param>
/// <param name="CorrelationId">The safe correlation handle.</param>
/// <param name="AuditHandle">The server-derived safe audit handle.</param>
public sealed record ConversationIdempotencyOutcome(
    IdempotencyOutcomeCategory Category,
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationCommandType CommandType,
    ConversationId? ConversationId,
    MessageId? MessageId,
    PartyId? ParticipantPartyId,
    FileId? FileId,
    ConversationErrorCode? RejectionCode,
    string? OriginalReasonCode,
    bool IsRetryable,
    string CorrelationId,
    string AuditHandle)
{
    /// <summary>
    /// Gets the outcome schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the public command type.
    /// </summary>
    public ConversationCommandType CommandType { get; } = RequireNonNull(CommandType, nameof(CommandType));

    /// <summary>
    /// Gets the safe correlation handle. Should be a server-derived opaque token, not a raw caller-supplied tracking ID (D1 review decision 2026-05-19).
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId, nameof(CorrelationId));

    /// <summary>
    /// Gets the server-generated audit handle.
    /// </summary>
    public string AuditHandle { get; } = ValidateRequired(AuditHandle, "auditHandle");

    /// <summary>
    /// Gets the original rejection reason code, when the stored outcome is a rejection.
    /// </summary>
    public string? OriginalReasonCode { get; } =
        ValidateOriginalReasonCode(Category, RejectionCode, OriginalReasonCode);

    /// <summary>
    /// Gets the outcome category, validated against the rejection-code/retryability invariant.
    /// </summary>
    public IdempotencyOutcomeCategory Category { get; } = ValidateCategoryInvariant(Category, RejectionCode, IsRetryable);

    /// <summary>
    /// Creates a successful logical outcome.
    /// </summary>
    /// <param name="schemaVersion">The outcome schema version.</param>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="commandType">The public command type.</param>
    /// <param name="conversationId">The durable conversation identity.</param>
    /// <param name="messageId">The durable message identity, when relevant.</param>
    /// <param name="participantPartyId">The durable participant Party identity, when relevant.</param>
    /// <param name="fileId">The durable file reference identity, when relevant.</param>
    /// <param name="correlationId">The safe correlation handle.</param>
    /// <param name="auditHandle">The server-derived safe audit handle.</param>
    /// <returns>The stable success outcome.</returns>
    public static ConversationIdempotencyOutcome Success(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationCommandType commandType,
        ConversationId conversationId,
        MessageId? messageId,
        PartyId? participantPartyId,
        FileId? fileId,
        string correlationId,
        string auditHandle)
        => new(
            IdempotencyOutcomeCategory.Success,
            schemaVersion,
            tenantId,
            commandType,
            conversationId,
            messageId,
            participantPartyId,
            fileId,
            RejectionCode: null,
            OriginalReasonCode: null,
            IsRetryable: false,
            correlationId,
            auditHandle);

    /// <summary>
    /// Creates a typed rejection logical outcome.
    /// </summary>
    /// <param name="schemaVersion">The outcome schema version.</param>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="commandType">The public command type.</param>
    /// <param name="conversationId">The durable conversation identity, when relevant.</param>
    /// <param name="rejectionCode">The typed rejection code.</param>
    /// <param name="originalReasonCode">The original typed rejection reason code.</param>
    /// <param name="isRetryable">A value indicating whether retry is meaningful.</param>
    /// <param name="correlationId">The safe correlation handle.</param>
    /// <param name="auditHandle">The server-derived safe audit handle.</param>
    /// <returns>The stable rejection outcome.</returns>
    public static ConversationIdempotencyOutcome Rejection(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationCommandType commandType,
        ConversationId? conversationId,
        ConversationErrorCode rejectionCode,
        string originalReasonCode,
        bool isRetryable,
        string correlationId,
        string auditHandle)
        => new(
            IdempotencyOutcomeCategory.Rejection,
            schemaVersion,
            tenantId,
            commandType,
            conversationId,
            MessageId: null,
            ParticipantPartyId: null,
            FileId: null,
            rejectionCode,
            originalReasonCode,
            isRetryable,
            correlationId,
            auditHandle);

    /// <summary>
    /// Creates a no-op logical outcome.
    /// </summary>
    /// <param name="schemaVersion">The outcome schema version.</param>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="commandType">The public command type.</param>
    /// <param name="conversationId">The durable conversation identity, when relevant.</param>
    /// <param name="correlationId">The safe correlation handle.</param>
    /// <param name="auditHandle">The server-derived safe audit handle.</param>
    /// <returns>The stable no-op outcome.</returns>
    public static ConversationIdempotencyOutcome NoOp(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationCommandType commandType,
        ConversationId? conversationId,
        string correlationId,
        string auditHandle)
        => new(
            IdempotencyOutcomeCategory.NoOp,
            schemaVersion,
            tenantId,
            commandType,
            conversationId,
            MessageId: null,
            ParticipantPartyId: null,
            FileId: null,
            RejectionCode: null,
            OriginalReasonCode: null,
            IsRetryable: false,
            correlationId,
            auditHandle);

    /// <summary>
    /// Creates a retryable uncertainty logical outcome.
    /// </summary>
    /// <param name="schemaVersion">The outcome schema version.</param>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="commandType">The public command type.</param>
    /// <param name="conversationId">The durable conversation identity, when relevant.</param>
    /// <param name="correlationId">The safe correlation handle.</param>
    /// <param name="auditHandle">The server-derived safe audit handle.</param>
    /// <returns>The stable uncertainty outcome.</returns>
    public static ConversationIdempotencyOutcome Uncertain(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationCommandType commandType,
        ConversationId? conversationId,
        string correlationId,
        string auditHandle)
        => new(
            IdempotencyOutcomeCategory.Uncertain,
            schemaVersion,
            tenantId,
            commandType,
            conversationId,
            MessageId: null,
            ParticipantPartyId: null,
            FileId: null,
            ConversationErrorCode.IdempotencyOutcomeUnknown,
            OriginalReasonCode: "idempotency_outcome_unknown",
            IsRetryable: true,
            correlationId,
            auditHandle);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static T RequireNonNull<T>(T value, string parameterName) where T : class
        => value ?? throw new ArgumentNullException(parameterName);

    private static IdempotencyOutcomeCategory ValidateCategoryInvariant(
        IdempotencyOutcomeCategory category,
        ConversationErrorCode? rejectionCode,
        bool isRetryable)
    {
        switch (category)
        {
            case IdempotencyOutcomeCategory.Success:
            case IdempotencyOutcomeCategory.NoOp:
                if (rejectionCode is not null)
                {
                    throw new ArgumentException(
                        $"Idempotency outcome category '{category}' must not carry a rejection code.",
                        nameof(rejectionCode));
                }

                if (isRetryable)
                {
                    throw new ArgumentException(
                        $"Idempotency outcome category '{category}' must not be marked retryable.",
                        nameof(isRetryable));
                }

                break;
            case IdempotencyOutcomeCategory.Rejection:
                if (rejectionCode is null)
                {
                    throw new ArgumentException(
                        "Idempotency outcome category 'Rejection' requires a rejection code.",
                        nameof(rejectionCode));
                }

                bool expectedRetryable = ConversationErrorCode.IsRetryable(rejectionCode);
                if (isRetryable != expectedRetryable)
                {
                    throw new ArgumentException(
                        $"Idempotency outcome rejection code '{rejectionCode.Value}' requires IsRetryable={expectedRetryable}.",
                        nameof(isRetryable));
                }

                break;
            case IdempotencyOutcomeCategory.Uncertain:
                if (!isRetryable)
                {
                    throw new ArgumentException(
                        "Idempotency outcome category 'Uncertain' must be marked retryable.",
                        nameof(isRetryable));
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown idempotency outcome category.");
        }

        return category;
    }

    private static string? ValidateOriginalReasonCode(
        IdempotencyOutcomeCategory category,
        ConversationErrorCode? rejectionCode,
        string? originalReasonCode)
    {
        if (category is IdempotencyOutcomeCategory.Rejection or IdempotencyOutcomeCategory.Uncertain)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(originalReasonCode, nameof(OriginalReasonCode));
            return originalReasonCode;
        }

        if (rejectionCode is null && originalReasonCode is not null)
        {
            throw new ArgumentException(
                $"Idempotency outcome category '{category}' must not carry an original reason code.",
                nameof(originalReasonCode));
        }

        return null;
    }
}
