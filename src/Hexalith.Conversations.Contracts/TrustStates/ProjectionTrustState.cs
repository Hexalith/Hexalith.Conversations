// <copyright file="ProjectionTrustState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.TrustStates;

/// <summary>
/// Represents the public trust and freshness vocabulary for read contracts.
/// </summary>
[JsonConverter(typeof(ProjectionTrustStateJsonConverter))]
public sealed record ProjectionTrustState
{
    /// <summary>
    /// Gets the current trust state.
    /// </summary>
    public static ProjectionTrustState Current { get; } = new(nameof(Current));

    /// <summary>
    /// Gets the stale trust state.
    /// </summary>
    public static ProjectionTrustState Stale { get; } = new(nameof(Stale));

    /// <summary>
    /// Gets the rebuilding trust state.
    /// </summary>
    public static ProjectionTrustState Rebuilding { get; } = new(nameof(Rebuilding));

    /// <summary>
    /// Gets the unavailable trust state.
    /// </summary>
    public static ProjectionTrustState Unavailable { get; } = new(nameof(Unavailable));

    /// <summary>
    /// Gets the forbidden trust state.
    /// </summary>
    public static ProjectionTrustState Forbidden { get; } = new(nameof(Forbidden));

    /// <summary>
    /// Gets the redacted trust state.
    /// </summary>
    public static ProjectionTrustState Redacted { get; } = new(nameof(Redacted));

    private static readonly IReadOnlyDictionary<string, ProjectionTrustState> KnownStates =
        new Dictionary<string, ProjectionTrustState>(StringComparer.Ordinal)
        {
            [nameof(Current)] = Current,
            [nameof(Stale)] = Stale,
            [nameof(Rebuilding)] = Rebuilding,
            [nameof(Unavailable)] = Unavailable,
            [nameof(Forbidden)] = Forbidden,
            [nameof(Redacted)] = Redacted,
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionTrustState"/> class.
    /// </summary>
    /// <param name="value">The trust state value.</param>
    private ProjectionTrustState(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the trust state value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a public trust state value.
    /// </summary>
    /// <param name="value">The trust state value.</param>
    /// <returns>The matching public trust state.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or unsupported.</exception>
    public static ProjectionTrustState Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownStates.TryGetValue(value, out ProjectionTrustState? state)
            ? state
            : throw new ArgumentException($"Unsupported projection trust state '{value}'.", nameof(value));
    }
}
