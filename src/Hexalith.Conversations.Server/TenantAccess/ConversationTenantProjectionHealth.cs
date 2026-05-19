// <copyright file="ConversationTenantProjectionHealth.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Optional local projection health metadata exposed by a Conversations-owned wrapper.
/// </summary>
/// <param name="IsStale">A value indicating whether the projection is stale or lagging.</param>
/// <param name="HasGap">A value indicating whether a sequence gap was detected.</param>
/// <param name="HasRollback">A value indicating whether a rollback or watermark regression was detected.</param>
/// <param name="IsPoisoned">A value indicating whether the projection is known poisoned or ambiguous.</param>
/// <param name="Version">The safe projection version when available.</param>
/// <param name="Watermark">The safe projection watermark when available.</param>
public sealed record ConversationTenantProjectionHealth(
    bool IsStale = false,
    bool HasGap = false,
    bool HasRollback = false,
    bool IsPoisoned = false,
    long? Version = null,
    string? Watermark = null)
{
    /// <summary>
    /// Gets a healthy projection signal.
    /// </summary>
    public static ConversationTenantProjectionHealth Healthy { get; } = new();
}

/// <summary>
/// Optional signal interface for tenant projection wrappers that expose freshness or poisoning state.
/// </summary>
public interface IConversationTenantProjectionSignal
{
    /// <summary>
    /// Gets projection health for a tenant before the tenant state is trusted.
    /// </summary>
    /// <param name="tenantId">The already canonical tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Projection health metadata.</returns>
    ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}
