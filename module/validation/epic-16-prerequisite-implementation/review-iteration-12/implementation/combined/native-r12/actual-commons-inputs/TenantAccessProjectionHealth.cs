// <copyright file="TenantAccessProjectionHealth.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Commons.TenantAccess;

/// <summary>
/// Bounded projection health evidence used before tenant access trusts stored state.
/// </summary>
/// <param name="Version">The projection version.</param>
/// <param name="Watermark">The projection watermark.</param>
/// <param name="IsStale">A value indicating whether the projection is stale.</param>
/// <param name="HasGap">A value indicating whether the projection has a sequence gap.</param>
/// <param name="HasRollback">A value indicating whether the projection rolled back.</param>
/// <param name="IsPoisoned">A value indicating whether the projection is poisoned.</param>
public sealed record TenantAccessProjectionHealth(
    long? Version,
    string? Watermark,
    bool IsStale,
    bool HasGap,
    bool HasRollback,
    bool IsPoisoned);
