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
/// <param name="IsRetryable">A value indicating whether retry is meaningful.</param>
/// <param name="CorrelationId">The safe correlation handle.</param>
/// <param name="AuditHandle">The optional safe audit handle.</param>
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
    bool IsRetryable,
    string CorrelationId,
    string? AuditHandle = null)
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
    /// Gets the safe correlation handle.
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId, nameof(CorrelationId));

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
    /// <param name="auditHandle">The optional safe audit handle.</param>
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
        string? auditHandle = null)
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
    /// <param name="isRetryable">A value indicating whether retry is meaningful.</param>
    /// <param name="correlationId">The safe correlation handle.</param>
    /// <param name="auditHandle">The optional safe audit handle.</param>
    /// <returns>The stable rejection outcome.</returns>
    public static ConversationIdempotencyOutcome Rejection(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationCommandType commandType,
        ConversationId? conversationId,
        ConversationErrorCode rejectionCode,
        bool isRetryable,
        string correlationId,
        string? auditHandle = null)
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
    /// <returns>The stable no-op outcome.</returns>
    public static ConversationIdempotencyOutcome NoOp(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationCommandType commandType,
        ConversationId? conversationId,
        string correlationId)
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
            IsRetryable: false,
            correlationId);

    /// <summary>
    /// Creates a retryable uncertainty logical outcome.
    /// </summary>
    /// <param name="schemaVersion">The outcome schema version.</param>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="commandType">The public command type.</param>
    /// <param name="conversationId">The durable conversation identity, when relevant.</param>
    /// <param name="correlationId">The safe correlation handle.</param>
    /// <returns>The stable uncertainty outcome.</returns>
    public static ConversationIdempotencyOutcome Uncertain(
        SchemaVersion schemaVersion,
        TenantId tenantId,
        ConversationCommandType commandType,
        ConversationId? conversationId,
        string correlationId)
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
            IsRetryable: true,
            correlationId);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static T RequireNonNull<T>(T value, string parameterName) where T : class
        => value ?? throw new ArgumentNullException(parameterName);
}
