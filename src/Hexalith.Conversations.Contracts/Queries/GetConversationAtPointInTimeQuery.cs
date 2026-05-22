// <copyright file="GetConversationAtPointInTimeQuery.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Requests tenant-safe reconstruction of conversation details at a prior temporal anchor.
/// </summary>
/// <param name="schemaVersion">The query contract schema version.</param>
/// <param name="tenantId">The trusted tenant binding selected by caller context.</param>
/// <param name="callerPrincipalId">The caller principal identity used for tenant access.</param>
/// <param name="correlationId">The safe request correlation id.</param>
/// <param name="conversationId">The Conversations-owned conversation identity.</param>
/// <param name="anchor">The requested safe temporal anchor.</param>
public sealed record GetConversationAtPointInTimeQuery(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    string CallerPrincipalId,
    string CorrelationId,
    ConversationId ConversationId,
    ConversationTemporalAnchorV1 Anchor)
{
    /// <summary>
    /// Gets the query contract schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the trusted tenant binding selected by caller context.
    /// </summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>
    /// Gets the caller principal identity used for tenant access.
    /// </summary>
    public string CallerPrincipalId { get; } = ValidateRequired(CallerPrincipalId, nameof(CallerPrincipalId));

    /// <summary>
    /// Gets the safe request correlation id.
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId, nameof(CorrelationId));

    /// <summary>
    /// Gets the Conversations-owned conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    /// <summary>
    /// Gets the requested safe temporal anchor.
    /// </summary>
    public ConversationTemporalAnchorV1 Anchor { get; } = Anchor ?? throw new ArgumentNullException(nameof(Anchor));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
