// <copyright file="TenantAccessState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Commons.TenantAccess;

/// <summary>
/// Neutral tenant access state consumed by the fail-closed evaluator.
/// </summary>
/// <param name="TenantId">The tenant identifier carried by the projection.</param>
/// <param name="Status">The module-mapped tenant status value.</param>
/// <param name="Members">The member role map keyed by caller principal id.</param>
public sealed record TenantAccessState(
    string TenantId,
    int Status,
    IReadOnlyDictionary<string, int>? Members);
