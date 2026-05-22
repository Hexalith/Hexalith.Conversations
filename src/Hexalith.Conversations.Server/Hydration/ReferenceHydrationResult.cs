// <copyright file="ReferenceHydrationResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Carries a Conversations-owned upstream adapter result before public response shaping.
/// </summary>
/// <typeparam name="TReference">The stable reference type.</typeparam>
public sealed record ReferenceHydrationResult<TReference>(
    TReference ReferenceId,
    ReferenceHydrationStatus Status,
    string? SafeLabel = null,
    string? SafeToken = null,
    string? SafeStatus = null)
    where TReference : class
{
    /// <summary>
    /// Gets the stable reference.
    /// </summary>
    public TReference ReferenceId { get; } = ReferenceId ?? throw new ArgumentNullException(nameof(ReferenceId));
}
