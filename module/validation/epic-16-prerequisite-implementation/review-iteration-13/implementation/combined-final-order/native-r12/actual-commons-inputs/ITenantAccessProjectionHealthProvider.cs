// <copyright file="ITenantAccessProjectionHealthProvider.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Commons.TenantAccess;

/// <summary>
/// Provides bounded health evidence for a local tenant projection before authorization trusts it.
/// </summary>
public interface ITenantAccessProjectionHealthProvider
{
    /// <summary>
    /// Gets health evidence for the tenant projection.
    /// </summary>
    /// <param name="tenantId">The canonical tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The projection health, or <see langword="null"/> when health is unavailable.</returns>
    ValueTask<TenantAccessProjectionHealth?> GetProjectionHealthAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}
