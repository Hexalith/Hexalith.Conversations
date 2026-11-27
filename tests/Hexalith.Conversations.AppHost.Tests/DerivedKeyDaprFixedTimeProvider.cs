// <copyright file="DerivedKeyDaprFixedTimeProvider.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.AppHost.Tests;

/// <summary>
/// Deterministic UTC <see cref="TimeProvider"/> for live Dapr+Redis fault tests in Story 12.3.
/// </summary>
/// <param name="now">The fixed UTC instant.</param>
internal sealed class DerivedKeyDaprFixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    private readonly DateTimeOffset _now = now;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;
}
