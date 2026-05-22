// <copyright file="ConversationQueryHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Handles tenant-safe conversation retrieve and list queries.
/// </summary>
public sealed class ConversationQueryHandler
{
    private readonly IConversationTenantAccessService _tenantAccessService;
    private readonly IConversationProjectionReadStore _projectionReadStore;
    private readonly ConversationProjectionReadService _projectionReadService;
    private readonly ConversationQueryCursor _cursor;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationReadHydrationService _hydrationService;

    public ConversationQueryHandler(
        IConversationTenantAccessService tenantAccessService,
        IConversationProjectionReadStore projectionReadStore,
        ConversationProjectionReadService projectionReadService,
        ConversationQueryCursor cursor,
        TimeProvider? timeProvider = null,
        ConversationReadHydrationService? hydrationService = null)
    {
        _tenantAccessService = tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));
        _projectionReadStore = projectionReadStore ?? throw new ArgumentNullException(nameof(projectionReadStore));
        _projectionReadService = projectionReadService ?? throw new ArgumentNullException(nameof(projectionReadService));
        _cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _hydrationService = hydrationService ?? new ConversationReadHydrationService();
    }

    /// <summary>
    /// Retrieves one authorized conversation detail.
    /// </summary>
    /// <param name="query">The retrieve query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe detail result.</returns>
    public async ValueTask<ConversationDetailResult> GetAsync(
        GetConversationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ConversationProjectionReadResult result = await _projectionReadService
            .ReadDetailAsync(
                query.TenantId,
                query.CallerPrincipalId,
                query.TenantId,
                query.ConversationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Projection is null)
        {
            return result.FreshnessState == ProjectionTrustState.Unavailable
                ? ConversationDetailResult.Unavailable(query.SchemaVersion)
                : ConversationDetailResult.Hidden(query.SchemaVersion);
        }

        ConversationDetailsV1 details = await _hydrationService
            .HydrateDetailAsync(
                ConversationDetailsV1.FromProjection(result.Projection),
                new ConversationHydrationContext(query.TenantId, query.CallerPrincipalId, query.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        return ConversationDetailResult.Visible(
            query.SchemaVersion,
            details,
            "Current projection is available.");
    }

    /// <summary>
    /// Lists authorized tenant-scoped conversation summaries.
    /// </summary>
    /// <param name="query">The list query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe list result.</returns>
    public async ValueTask<ConversationListResult> ListAsync(
        ListConversationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ConversationQueryCursor.DecodedCursor? cursor = null;
        if (query.Page.ContinuationCursor is not null)
        {
            if (!_cursor.TryDecode(query.Page.ContinuationCursor, out ConversationQueryCursor.DecodedCursor decoded))
            {
                return ConversationListResult.Hidden(query.SchemaVersion);
            }

            cursor = decoded;
        }

        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                ConversationTenantAccessRequirement.Read,
                query.TenantId,
                query.CallerPrincipalId,
                routeTenantId: query.TenantId,
                projectionTenantId: query.TenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return ConversationListResult.Hidden(query.SchemaVersion);
        }

        IReadOnlyList<ConversationSummaryProjectionV1> candidates;
        try
        {
            candidates = await _projectionReadStore.ListAsync(query.TenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return ConversationListResult.Unavailable(query.SchemaVersion);
        }
        catch (IOException)
        {
            return ConversationListResult.Unavailable(query.SchemaVersion);
        }
        catch (TimeoutException)
        {
            return ConversationListResult.Unavailable(query.SchemaVersion);
        }

        // Tenant-scoped poison guard: reject any row whose stored TenantId disagrees with the request scope
        // before any filter, count, ordering, or freshness evaluation runs.
        List<ConversationSummaryProjectionV1> tenantScoped = candidates
            .Where(summary => summary.TenantId == query.TenantId)
            .ToList();

        // Mixed-generation poison guard: the detail boundary refuses to trust mixed-generation rows; the
        // list boundary mirrors that posture. When candidate rows disagree on projection generation we
        // surface Rebuilding/MixedGeneration rather than returning a page from inconsistent generations.
        if (HasMixedGenerations(tenantScoped))
        {
            return new ConversationListResult(
                query.SchemaVersion,
                ProjectionTrustState.Rebuilding,
                ProjectionFreshnessReasonCode.MixedGeneration,
                [],
                new ConversationPageMetadata(0),
                "Retry after the read model finishes rebuilding.");
        }

        string projectionGenerationToken = ComputeGenerationToken(tenantScoped);

        if (cursor.HasValue
            && !cursor.Value.Matches(
                query.TenantId,
                query.CallerPrincipalId,
                query.Filter,
                projectionGenerationToken,
                _timeProvider.GetUtcNow()))
        {
            return ConversationListResult.Hidden(query.SchemaVersion);
        }

        int offset = cursor?.Offset ?? 0;
        List<ConversationSummaryProjectionV1> accessible = tenantScoped
            .Where(summary => MatchesFilter(summary, query.Filter))
            .OrderByDescending(summary => summary.Freshness.LastAppliedEventTimestamp)
            .ThenBy(summary => summary.ConversationId.Value, StringComparer.Ordinal)
            .Skip(offset)
            .Take(query.Page.PageSize + 1)
            .ToList();

        bool issueContinuation = accessible.Count > query.Page.PageSize;
        IReadOnlyList<ConversationSummaryV1> page = accessible
            .Take(query.Page.PageSize)
            .Select(ConversationSummaryV1.FromProjection)
            .ToList();

        page = await _hydrationService
            .HydrateSummariesAsync(
                page,
                new ConversationHydrationContext(query.TenantId, query.CallerPrincipalId, query.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        string? nextCursor = issueContinuation
            ? _cursor.Encode(
                query.TenantId,
                query.CallerPrincipalId,
                query.Filter,
                offset + page.Count,
                projectionGenerationToken,
                _timeProvider.GetUtcNow())
            : null;

        (ProjectionTrustState state, ProjectionFreshnessReasonCode reason) = AggregateFreshness(tenantScoped);

        return new ConversationListResult(
            query.SchemaVersion,
            state,
            reason,
            page,
            new ConversationPageMetadata(page.Count, nextCursor),
            nextCursor is null
                ? "Accessible results are complete for the supplied filters."
                : "Use the cursor only with the same tenant, caller, filters, and ordering.");
    }

    private static bool MatchesFilter(ConversationSummaryProjectionV1 summary, ConversationListFilterV1 filter)
        => MatchesBusinessReference(summary.BusinessReference, filter.BusinessReference)
            && Matches(summary.ProjectId, filter.ProjectId)
            && Matches(summary.FolderId, filter.FolderId)
            && MatchesLifecycle(summary.LifecycleState, filter.LifecycleState)
            && MatchesProjectedAt(summary.Freshness.LastAppliedEventTimestamp, filter)
            && MatchesParticipant(summary.ParticipantPartyIds, filter.ParticipantPartyId);

    private static bool MatchesBusinessReference(BusinessReference? actual, BusinessReference? expected)
        => expected is null
            || (actual is not null
                && string.Equals(actual.System, expected.System, StringComparison.Ordinal)
                && string.Equals(actual.Value, expected.Value, StringComparison.Ordinal));

    private static bool Matches<T>(T? actual, T? expected)
        where T : class
        => expected is null || EqualityComparer<T>.Default.Equals(actual, expected);

    private static bool MatchesLifecycle(string actual, string? expected)
        => expected is null || string.Equals(actual, expected, StringComparison.Ordinal);

    private static bool MatchesProjectedAt(DateTimeOffset projectedAt, ConversationListFilterV1 filter)
    {
        if (filter.ProjectedAtFrom.HasValue && projectedAt < filter.ProjectedAtFrom.Value)
        {
            return false;
        }

        if (filter.ProjectedAtTo.HasValue && projectedAt > filter.ProjectedAtTo.Value)
        {
            return false;
        }

        return !filter.RecentActivityAfter.HasValue || projectedAt > filter.RecentActivityAfter.Value;
    }

    private static bool MatchesParticipant(IReadOnlyList<PartyId> actual, PartyId? expected)
        => expected is null || actual.Contains(expected);

    private static bool HasMixedGenerations(IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        if (summaries.Count <= 1)
        {
            return false;
        }

        string firstCursor = summaries[0].Freshness.ProjectionCursor;
        for (int i = 1; i < summaries.Count; i++)
        {
            ProjectionFreshnessV1 freshness = summaries[i].Freshness;
            if (!string.Equals(freshness.ProjectionCursor, firstCursor, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string ComputeGenerationToken(IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        if (summaries.Count == 0)
        {
            return "empty";
        }

        // Same-generation guard ensures every row shares ProjectionCursor; use the first row's cursor
        // and the maximum applied event position as the binding token. Cursor encoding will reject
        // continuation when the token changes between page requests.
        ProjectionFreshnessV1 freshness = summaries[0].Freshness;
        long maxPosition = summaries.Max(s => s.Freshness.LastAppliedEventPosition);
        return $"{freshness.ProjectionCursor}:{maxPosition}";
    }

    private static (ProjectionTrustState State, ProjectionFreshnessReasonCode Reason) AggregateFreshness(
        IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        if (summaries.Count == 0)
        {
            return (ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current);
        }

        // Worst-case aggregation: priority is Unavailable > Rebuilding > Stale > Redacted > Current.
        // Forbidden cannot reach here because tenant-scoped poison guard already filtered cross-tenant
        // rows, and tenant denial returns Hidden() earlier.
        ProjectionTrustState worst = ProjectionTrustState.Current;
        ProjectionFreshnessReasonCode worstReason = ProjectionFreshnessReasonCode.Current;
        foreach (ConversationSummaryProjectionV1 summary in summaries)
        {
            ProjectionTrustState candidate = summary.Freshness.FreshnessState;
            if (Priority(candidate) > Priority(worst))
            {
                worst = candidate;
                worstReason = summary.Freshness.ReasonCode;
            }
        }

        return (worst, worstReason);

        static int Priority(ProjectionTrustState state)
        {
            if (state == ProjectionTrustState.Unavailable) { return 5; }
            if (state == ProjectionTrustState.Rebuilding) { return 4; }
            if (state == ProjectionTrustState.Stale) { return 3; }
            if (state == ProjectionTrustState.Redacted) { return 2; }
            if (state == ProjectionTrustState.Forbidden) { return 1; }
            return 0;
        }
    }
}
