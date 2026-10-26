// <copyright file="TenantAccessEvaluation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Commons.TenantAccess;

/// <summary>
/// Domain-neutral tenant-access evaluation result.
/// </summary>
/// <typeparam name="TRequirement">The module requirement enum/type.</typeparam>
/// <param name="IsAllowed">A value indicating whether protected state may be touched.</param>
/// <param name="Requirement">The requested operation class.</param>
/// <param name="TenantId">The canonical tenant identifier when available.</param>
/// <param name="CallerPrincipalId">The caller principal identifier when available.</param>
/// <param name="DenialKind">The neutral denial category.</param>
/// <param name="IsRetryable">A value indicating whether retry may make sense internally.</param>
/// <param name="ProjectionVersion">The optional safe projection version.</param>
/// <param name="ProjectionWatermark">The optional safe projection watermark.</param>
public sealed record TenantAccessEvaluation<TRequirement>(
    bool IsAllowed,
    TRequirement Requirement,
    string? TenantId,
    string? CallerPrincipalId,
    TenantAccessDenialKind DenialKind,
    bool IsRetryable,
    long? ProjectionVersion = null,
    string? ProjectionWatermark = null);
