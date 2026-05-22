// <copyright file="ConversationHydrationContext.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Carries trusted read-boundary context for upstream reference hydration.
/// </summary>
public sealed record ConversationHydrationContext(
    TenantId TenantId,
    string CallerPrincipalId,
    string CorrelationId)
{
    /// <summary>
    /// Gets the trusted tenant scope.
    /// </summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>
    /// Gets the caller identity already checked by the tenant access boundary.
    /// </summary>
    public string CallerPrincipalId { get; } = Required(CallerPrincipalId, nameof(CallerPrincipalId));

    /// <summary>
    /// Gets the safe correlation id.
    /// </summary>
    public string CorrelationId { get; } = Required(CorrelationId, nameof(CorrelationId));

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
