// <copyright file="ListConversationsQuery.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Requests tenant-scoped conversation summaries using exact business-context filters.
/// </summary>
/// <param name="schemaVersion">The query contract schema version.</param>
/// <param name="tenantId">The trusted tenant binding selected by the caller context.</param>
/// <param name="callerPrincipalId">The caller principal identity used for tenant access.</param>
/// <param name="correlationId">The safe request correlation id.</param>
/// <param name="filter">The exact-match tenant-scoped filter.</param>
/// <param name="page">The bounded page request.</param>
public sealed record ListConversationsQuery(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    string CallerPrincipalId,
    string CorrelationId,
    ConversationListFilterV1? Filter = null,
    ConversationPageRequest? Page = null)
{
    /// <summary>
    /// Gets the query contract schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the trusted tenant binding selected by the caller context.
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
    /// Gets the exact-match tenant-scoped filter.
    /// </summary>
    public ConversationListFilterV1 Filter { get; } = Filter ?? ConversationListFilterV1.Empty;

    /// <summary>
    /// Gets the bounded page request.
    /// </summary>
    public ConversationPageRequest Page { get; } = Page ?? new ConversationPageRequest();

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
