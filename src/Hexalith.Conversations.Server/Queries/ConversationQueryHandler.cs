// <copyright file="ConversationQueryHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Handles tenant-safe conversation retrieve and list queries.
/// </summary>
public sealed class ConversationQueryHandler(
    IConversationTenantAccessService tenantAccessService,
    IConversationProjectionReadStore projectionReadStore,
    TimeProvider? timeProvider = null)
{
    private readonly IConversationProjectionReadStore _projectionReadStore =
        projectionReadStore ?? throw new ArgumentNullException(nameof(projectionReadStore));

    private readonly IConversationTenantAccessService _tenantAccessService =
        tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

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

        ConversationProjectionReadService readService = new(_tenantAccessService, _projectionReadStore);
        ConversationProjectionReadResult result = await readService
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

        return ConversationDetailResult.Visible(
            query.SchemaVersion,
            ConversationDetailsV1.FromProjection(result.Projection),
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
            if (!ConversationQueryCursor.TryDecode(query.Page.ContinuationCursor, out ConversationQueryCursor.DecodedCursor decoded))
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

        if (cursor.HasValue && !cursor.Value.Matches(query.TenantId, query.CallerPrincipalId, query.Filter, _timeProvider.GetUtcNow()))
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

        int offset = cursor?.Offset ?? 0;
        List<ConversationSummaryProjectionV1> accessible = candidates
            .Where(summary => summary.TenantId == query.TenantId)
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

        string? nextCursor = issueContinuation
            ? ConversationQueryCursor.Encode(
                query.TenantId,
                query.CallerPrincipalId,
                query.Filter,
                offset + page.Count,
                _timeProvider.GetUtcNow())
            : null;

        ProjectionTrustState state = page.Select(summary => summary.Freshness.FreshnessState).FirstOrDefault()
            ?? ProjectionTrustState.Current;
        ProjectionFreshnessReasonCode reason = page.Select(summary => summary.Freshness.ReasonCode).FirstOrDefault()
            ?? ProjectionFreshnessReasonCode.Current;

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
            && MatchesDate(summary.Freshness.LastAppliedEventTimestamp, filter)
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

    private static bool MatchesDate(DateTimeOffset projectedActivity, ConversationListFilterV1 filter)
    {
        if (filter.DateFrom.HasValue && projectedActivity < filter.DateFrom.Value)
        {
            return false;
        }

        if (filter.DateTo.HasValue && projectedActivity > filter.DateTo.Value)
        {
            return false;
        }

        return !filter.RecentActivityAfter.HasValue || projectedActivity > filter.RecentActivityAfter.Value;
    }

    private static bool MatchesParticipant(IReadOnlyList<PartyId> actual, PartyId? expected)
        => expected is null || actual.Contains(expected);
}
