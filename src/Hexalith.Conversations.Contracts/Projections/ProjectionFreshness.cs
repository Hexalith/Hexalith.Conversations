// <copyright file="ProjectionFreshness.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Describes the freshness and trust state of a public read contract.
/// </summary>
/// <param name="state">The public trust state.</param>
/// <param name="observedAt">The time at which the read state was observed.</param>
/// <param name="projectionContractSchemaVersion">The projection contract schema version.</param>
/// <param name="guidance">Optional safe developer guidance.</param>
public sealed record ProjectionFreshness(
    ProjectionTrustState State,
    DateTimeOffset ObservedAt,
    SchemaVersion ProjectionContractSchemaVersion,
    string? Guidance = null)
{
    /// <summary>
    /// Gets the public trust state.
    /// </summary>
    public ProjectionTrustState State { get; } = RequireNonNull(State, nameof(State));

    /// <summary>
    /// Gets the projection contract schema version.
    /// </summary>
    public SchemaVersion ProjectionContractSchemaVersion { get; } = RequireNonNull(ProjectionContractSchemaVersion, nameof(ProjectionContractSchemaVersion));

    /// <summary>
    /// Gets the time at which the read state was observed.
    /// </summary>
    public DateTimeOffset ObservedAt { get; } = ValidateTimestamp(ObservedAt);

    private static T RequireNonNull<T>(T value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset value)
    {
        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must be greater than DateTimeOffset.MinValue.");
        }

        if (value.Year < 2000 || value.Year > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Timestamp must fall within the plausible business range (year 2000-9999).");
        }

        return value;
    }
}
