// <copyright file="ConversationIdempotencyScope.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Defines the tenant-scoped command context used to evaluate an idempotency key.
/// </summary>
/// <param name="TenantId">The trusted tenant binding.</param>
/// <param name="CommandType">The public command type.</param>
/// <param name="ScopeKind">The kind of command scope.</param>
/// <param name="ScopeValue">The durable scope value within the tenant.</param>
/// <param name="IdempotencyKey">The caller-supplied idempotency key.</param>
/// <param name="SchemaVersion">The command schema version.</param>
public sealed record ConversationIdempotencyScope(
    TenantId TenantId,
    ConversationCommandType CommandType,
    string ScopeKind,
    string ScopeValue,
    string IdempotencyKey,
    SchemaVersion SchemaVersion)
{
    /// <summary>
    /// Gets the scope kind used for commands that operate on an existing conversation.
    /// </summary>
    public const string ConversationScopeKind = "conversation";

    /// <summary>
    /// Gets the scope kind used for create-conversation allocation.
    /// </summary>
    public const string CreateAllocationScopeKind = "create-allocation";

    /// <summary>
    /// Gets the trusted tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the public command type.
    /// </summary>
    public ConversationCommandType CommandType { get; } = RequireNonNull(CommandType, nameof(CommandType));

    /// <summary>
    /// Gets the kind of command scope.
    /// </summary>
    public string ScopeKind { get; } = ValidateRequired(ScopeKind, nameof(ScopeKind));

    /// <summary>
    /// Gets the durable scope value within the tenant.
    /// </summary>
    public string ScopeValue { get; } = ValidateRequired(ScopeValue, nameof(ScopeValue));

    /// <summary>
    /// Gets the caller-supplied idempotency key.
    /// </summary>
    public string IdempotencyKey { get; } = ValidateRequired(IdempotencyKey, nameof(IdempotencyKey));

    /// <summary>
    /// Gets the command schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static T RequireNonNull<T>(T value, string parameterName) where T : class
        => value ?? throw new ArgumentNullException(parameterName);
}
