// <copyright file="ConversationCreatedResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Results;

/// <summary>
/// Reports the assigned tenant-scoped conversation identity for create operations.
/// </summary>
/// <param name="schemaVersion">The result schema version.</param>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The assigned tenant-scoped conversation identity.</param>
/// <param name="correlationId">The accepted correlation identifier.</param>
/// <param name="idempotencyKey">The accepted idempotency key, when provided.</param>
/// <param name="visibility">The read-model visibility caveat.</param>
/// <param name="commandType">The accepted public command type.</param>
public sealed record ConversationCreatedResult(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    string CorrelationId,
    string? IdempotencyKey,
    ReadModelVisibility Visibility,
    ConversationCommandType CommandType)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the assigned tenant-scoped conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = RequireNonNull(ConversationId, nameof(ConversationId));

    /// <summary>
    /// Gets the accepted correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId);

    /// <summary>
    /// Gets the accepted public command type.
    /// </summary>
    public ConversationCommandType CommandType { get; } = RequireNonNull(CommandType, nameof(CommandType));

    private static string ValidateRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static T RequireNonNull<T>(T value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);
}
