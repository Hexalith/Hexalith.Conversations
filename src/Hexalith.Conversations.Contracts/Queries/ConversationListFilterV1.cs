// <copyright file="ConversationListFilterV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Defines exact-match tenant-scoped filters for conversation listing.
/// </summary>
/// <remarks>
/// The date bounds filter on the projection's last-applied event timestamp
/// (<see cref="Projections.ProjectionFreshnessV1.LastAppliedEventTimestamp"/>), not on business activity.
/// A future projection field for "last visible message activity" is planned in Story 1.9; the current
/// recent-activity bound shares the same projection timestamp until that field exists.
/// </remarks>
/// <param name="businessReference">The adopter-owned external business correlation key.</param>
/// <param name="projectId">The stable project reference.</param>
/// <param name="folderId">The stable folder reference.</param>
/// <param name="lifecycleState">The closed conversation lifecycle value.</param>
/// <param name="projectedAtFrom">The inclusive lower bound for the projection's last-applied event timestamp.</param>
/// <param name="projectedAtTo">The inclusive upper bound for the projection's last-applied event timestamp.</param>
/// <param name="recentActivityAfter">The exclusive lower bound for projected activity (projection-write-time until Story 1.9 ships LastMessageAt).</param>
/// <param name="participantPartyId">The stable participant Party reference.</param>
public sealed record ConversationListFilterV1(
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    string? LifecycleState = null,
    DateTimeOffset? ProjectedAtFrom = null,
    DateTimeOffset? ProjectedAtTo = null,
    DateTimeOffset? RecentActivityAfter = null,
    PartyId? ParticipantPartyId = null)
{
    /// <summary>
    /// Gets an empty filter.
    /// </summary>
    public static ConversationListFilterV1 Empty { get; } = new();

    /// <summary>
    /// Gets the closed conversation lifecycle value.
    /// </summary>
    public string? LifecycleState { get; } = ValidateLifecycle(LifecycleState);

    /// <summary>
    /// Gets the inclusive lower bound for the projection's last-applied event timestamp.
    /// </summary>
    public DateTimeOffset? ProjectedAtFrom { get; } = ProjectedAtFrom;

    /// <summary>
    /// Gets the inclusive upper bound for the projection's last-applied event timestamp.
    /// </summary>
    public DateTimeOffset? ProjectedAtTo { get; } = ValidateProjectedAtRange(ProjectedAtFrom, ProjectedAtTo);

    private static DateTimeOffset? ValidateProjectedAtRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentException(
                "The projected-at lower bound must not be after the upper bound.",
                nameof(ProjectedAtTo));
        }

        return to;
    }

    private static string? ValidateLifecycle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value switch
        {
            "Initializing" or "Open" or "Closed" or "Archived" => value,
            _ => throw new ArgumentException("Unsupported conversation lifecycle state.", nameof(LifecycleState)),
        };
    }
}
