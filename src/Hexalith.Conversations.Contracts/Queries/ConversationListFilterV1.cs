// <copyright file="ConversationListFilterV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;

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
/// <param name="redactionState">The explicit redaction trust state to match.</param>
/// <param name="freshnessState">The explicit projection freshness state to match.</param>
/// <param name="auditReadiness">The explicit audit-readiness state to match.</param>
/// <param name="verificationState">The explicit verification state to match.</param>
public sealed record ConversationListFilterV1(
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    string? LifecycleState = null,
    DateTimeOffset? ProjectedAtFrom = null,
    DateTimeOffset? ProjectedAtTo = null,
    DateTimeOffset? RecentActivityAfter = null,
    PartyId? ParticipantPartyId = null,
    ProjectionTrustState? RedactionState = null,
    ProjectionTrustState? FreshnessState = null,
    ConversationAuditReadinessState? AuditReadiness = null,
    ConversationVerificationState? VerificationState = null)
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

    /// <summary>
    /// Gets the explicit redaction trust state to match.
    /// </summary>
    public ProjectionTrustState? RedactionState { get; } = RedactionState;

    /// <summary>
    /// Gets the explicit projection freshness state to match.
    /// </summary>
    public ProjectionTrustState? FreshnessState { get; } = FreshnessState;

    /// <summary>
    /// Gets the explicit audit-readiness state to match.
    /// </summary>
    public ConversationAuditReadinessState? AuditReadiness { get; } = AuditReadiness;

    /// <summary>
    /// Gets the explicit verification state to match.
    /// </summary>
    public ConversationVerificationState? VerificationState { get; } = VerificationState;

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
