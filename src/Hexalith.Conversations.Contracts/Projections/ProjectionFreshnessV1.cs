// <copyright file="ProjectionFreshnessV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Carries public v1 projection freshness metadata computed by the server.
/// </summary>
/// <param name="projectionContractSchemaVersion">The projection contract schema version.</param>
/// <param name="projectionCursor">A safe projection cursor equivalent.</param>
/// <param name="lastAppliedEventPosition">The last accepted public event position.</param>
/// <param name="lastAppliedEventTimestamp">The last accepted public event timestamp.</param>
/// <param name="projectionGeneratedAt">The UTC time when this projection generation was produced.</param>
/// <param name="lagDuration">The observed lag between the last accepted event and projection generation.</param>
/// <param name="isStale">A value indicating whether the projection exceeded the freshness threshold.</param>
/// <param name="freshnessState">The public freshness state.</param>
/// <param name="reasonCode">The safe public freshness reason code.</param>
public sealed record ProjectionFreshnessV1(
    SchemaVersion ProjectionContractSchemaVersion,
    string ProjectionCursor,
    long LastAppliedEventPosition,
    DateTimeOffset LastAppliedEventTimestamp,
    DateTimeOffset ProjectionGeneratedAt,
    TimeSpan? LagDuration,
    bool IsStale,
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode ReasonCode)
{
    /// <summary>
    /// Gets the projection contract schema version.
    /// </summary>
    public SchemaVersion ProjectionContractSchemaVersion { get; } = RequireNonNull(
        ProjectionContractSchemaVersion,
        nameof(ProjectionContractSchemaVersion));

    /// <summary>
    /// Gets the safe projection cursor equivalent.
    /// </summary>
    public string ProjectionCursor { get; } = ValidateCursor(ProjectionCursor);

    /// <summary>
    /// Gets the last accepted public event position.
    /// </summary>
    public long LastAppliedEventPosition { get; } = ValidatePosition(LastAppliedEventPosition);

    /// <summary>
    /// Gets the last accepted public event timestamp.
    /// </summary>
    public DateTimeOffset LastAppliedEventTimestamp { get; } = ValidateTimestamp(LastAppliedEventTimestamp);

    /// <summary>
    /// Gets the UTC time when this projection generation was produced.
    /// </summary>
    public DateTimeOffset ProjectionGeneratedAt { get; } = ValidateGeneratedAt(
        ProjectionGeneratedAt,
        LastAppliedEventTimestamp);

    /// <summary>
    /// Gets the observed lag between the last accepted event and projection generation.
    /// </summary>
    public TimeSpan? LagDuration { get; } = ValidateLag(LagDuration);

    /// <summary>
    /// Gets a value indicating whether the projection exceeded the freshness threshold.
    /// </summary>
    public bool IsStale { get; } = ValidateStaleFlag(IsStale, FreshnessState);

    /// <summary>
    /// Gets the public freshness state.
    /// </summary>
    public ProjectionTrustState FreshnessState { get; } = RequireNonNull(FreshnessState, nameof(FreshnessState));

    /// <summary>
    /// Gets the safe public freshness reason code.
    /// </summary>
    public ProjectionFreshnessReasonCode ReasonCode { get; } = ValidateReasonCode(ReasonCode, FreshnessState, IsStale);

    /// <summary>
    /// Determines whether this metadata can enable trust-bearing decisions.
    /// </summary>
    /// <returns>True when this freshness metadata is current and non-stale.</returns>
    public bool AllowsTrustBearingDecision()
        => FreshnessState == ProjectionTrustState.Current
            && ReasonCode == ProjectionFreshnessReasonCode.Current
            && !IsStale;

    private static string ValidateCursor(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static long ValidatePosition(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }

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

    private static DateTimeOffset ValidateGeneratedAt(DateTimeOffset generatedAt, DateTimeOffset lastApplied)
    {
        DateTimeOffset value = ValidateTimestamp(generatedAt);
        if (value < lastApplied)
        {
            throw new ArgumentException("Projection generation time must not precede the last applied event timestamp.", nameof(generatedAt));
        }

        return value;
    }

    private static TimeSpan? ValidateLag(TimeSpan? value)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Projection lag must not be negative.");
        }

        return value;
    }

    private static bool ValidateStaleFlag(bool isStale, ProjectionTrustState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state == ProjectionTrustState.Current && isStale)
        {
            throw new ArgumentException("Current projections must not be marked stale.", nameof(isStale));
        }

        return isStale;
    }

    private static ProjectionFreshnessReasonCode ValidateReasonCode(
        ProjectionFreshnessReasonCode reasonCode,
        ProjectionTrustState state,
        bool isStale)
    {
        ArgumentNullException.ThrowIfNull(reasonCode);
        ArgumentNullException.ThrowIfNull(state);

        if (state == ProjectionTrustState.Current
            && (reasonCode != ProjectionFreshnessReasonCode.Current || isStale))
        {
            throw new ArgumentException("Current projections require the current reason code and a non-stale flag.", nameof(reasonCode));
        }

        if (reasonCode == ProjectionFreshnessReasonCode.Current && state != ProjectionTrustState.Current)
        {
            throw new ArgumentException("The current reason code cannot be used for non-current projections.", nameof(reasonCode));
        }

        return reasonCode;
    }
}
