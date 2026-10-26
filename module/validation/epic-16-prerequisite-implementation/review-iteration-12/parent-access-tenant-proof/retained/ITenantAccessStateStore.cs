// <copyright file="ITenantAccessStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Commons.TenantAccess;

/// <summary>
/// Reads neutral tenant access state for the fail-closed evaluator.
/// </summary>
public interface ITenantAccessStateStore
{
    /// <summary>
    /// Gets neutral tenant access state.
    /// </summary>
    /// <param name="tenantId">The canonical tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant state, or <see langword="null"/> when absent.</returns>
    Task<TenantAccessState?> GetAsync(string tenantId, CancellationToken cancellationToken = default);
}
