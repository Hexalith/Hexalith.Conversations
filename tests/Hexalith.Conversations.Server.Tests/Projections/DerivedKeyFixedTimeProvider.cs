// <copyright file="DerivedKeyFixedTimeProvider.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Deterministic <see cref="TimeProvider"/> returning a fixed UTC instant for Story 12.3 tests.
/// </summary>
/// <param name="now">The fixed UTC instant.</param>
internal sealed class DerivedKeyFixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    private readonly DateTimeOffset _now = now;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;
}
