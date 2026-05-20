// <copyright file="ConversationListFilterV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Defines exact-match tenant-scoped filters for conversation listing.
/// </summary>
/// <param name="businessReference">The adopter-owned external business correlation key.</param>
/// <param name="projectId">The stable project reference.</param>
/// <param name="folderId">The stable folder reference.</param>
/// <param name="lifecycleState">The closed conversation lifecycle value.</param>
/// <param name="dateFrom">The inclusive lower bound for the projected activity timestamp.</param>
/// <param name="dateTo">The inclusive upper bound for the projected activity timestamp.</param>
/// <param name="recentActivityAfter">The exclusive recent-activity lower bound.</param>
/// <param name="participantPartyId">The stable participant Party reference.</param>
public sealed record ConversationListFilterV1(
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    string? LifecycleState = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
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
    /// Gets the inclusive lower bound for the projected activity timestamp.
    /// </summary>
    public DateTimeOffset? DateFrom { get; } = DateFrom;

    /// <summary>
    /// Gets the inclusive upper bound for the projected activity timestamp.
    /// </summary>
    public DateTimeOffset? DateTo { get; } = ValidateDateRange(DateFrom, DateTo);

    private static DateTimeOffset? ValidateDateRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentException("The date lower bound must not be after the upper bound.", nameof(to));
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
            _ => throw new ArgumentException("Unsupported conversation lifecycle state.", nameof(value)),
        };
    }
}
