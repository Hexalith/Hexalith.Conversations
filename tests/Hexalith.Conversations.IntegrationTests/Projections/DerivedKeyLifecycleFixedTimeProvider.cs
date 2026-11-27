// <copyright file="DerivedKeyLifecycleFixedTimeProvider.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Controllable UTC clock for deterministic replay and cursor-expiration tests in Story 12.3.
/// </summary>
/// <param name="initial">The initial UTC instant.</param>
internal sealed class DerivedKeyLifecycleFixedTimeProvider(DateTimeOffset initial) : TimeProvider
{
    private DateTimeOffset _now = initial;

    /// <summary>Advances or replaces the current UTC instant.</summary>
    /// <param name="now">The new UTC instant.</param>
    public void SetUtcNow(DateTimeOffset now) => _now = now;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;
}
