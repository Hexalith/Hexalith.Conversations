// <copyright file="ConversationQueryHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Queries;

using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Handles tenant-safe conversation retrieve and list queries.
/// </summary>
public sealed class ConversationQueryHandler
{
    private readonly IConversationTenantAccessService _tenantAccessService;
    private readonly IConversationProjectionReadStore _projectionReadStore;
    private readonly ConversationProjectionReadService _projectionReadService;
    private readonly IQueryCursorCodec _cursorCodec;
    private readonly ConversationQueryCursorOptions _cursorOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationReadHydrationService _hydrationService;
    private readonly ConversationCitationAccessService _citationAccessService;
    private readonly ConversationTemporalReconstructionService _temporalReconstructionService;
    private readonly ConversationAuditRecordAccessService _auditRecordAccessService;
    private readonly ConversationPrivilegedJustificationReviewService? _privilegedJustificationReviewService;

    public ConversationQueryHandler(
        IConversationTenantAccessService tenantAccessService,
        IConversationProjectionReadStore projectionReadStore,
        ConversationProjectionReadService projectionReadService,
        IQueryCursorCodec cursorCodec,
        IOptions<ConversationQueryCursorOptions>? cursorOptions = null,
        TimeProvider? timeProvider = null,
        ConversationReadHydrationService? hydrationService = null,
        ConversationCitationAccessService? citationAccessService = null,
        ConversationTemporalReconstructionService? temporalReconstructionService = null,
        ConversationAuditRecordAccessService? auditRecordAccessService = null,
        ConversationPrivilegedJustificationReviewService? privilegedJustificationReviewService = null)
    {
        _tenantAccessService = tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));
        _projectionReadStore = projectionReadStore ?? throw new ArgumentNullException(nameof(projectionReadStore));
        _projectionReadService = projectionReadService ?? throw new ArgumentNullException(nameof(projectionReadService));
        _cursorCodec = cursorCodec ?? throw new ArgumentNullException(nameof(cursorCodec));
        _cursorOptions = cursorOptions?.Value ?? new ConversationQueryCursorOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _hydrationService = hydrationService ?? new ConversationReadHydrationService();
        _citationAccessService = citationAccessService ?? new ConversationCitationAccessService(_projectionReadService);
        _temporalReconstructionService = temporalReconstructionService
            ?? new ConversationTemporalReconstructionService(
                _tenantAccessService,
                _projectionReadService,
                new UnavailableConversationTemporalEventSource());
        _auditRecordAccessService = auditRecordAccessService
            ?? new ConversationAuditRecordAccessService(_tenantAccessService, _projectionReadStore);
        _privilegedJustificationReviewService = privilegedJustificationReviewService;
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
            if (result.FreshnessState == ProjectionTrustState.Unavailable)
            {
                return ConversationDetailResult.Unavailable(query.SchemaVersion);
            }

            if (result.FreshnessState != ProjectionTrustState.Rebuilding)
            {
                return ConversationDetailResult.Hidden(query.SchemaVersion);
            }

            return new ConversationDetailResult(
                query.SchemaVersion,
                result.FreshnessState,
                result.ReasonCode,
                null,
                "Retry after the read model finishes rebuilding.");
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
    /// Retrieves one authorized conversation detail reconstructed at a prior temporal anchor.
    /// </summary>
    /// <param name="query">The temporal retrieve query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe temporal detail result.</returns>
    public ValueTask<ConversationTemporalDetailResult> GetAtPointInTimeAsync(
        GetConversationAtPointInTimeQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _temporalReconstructionService.ReconstructAsync(query, cancellationToken);
    }

    /// <summary>
    /// Retrieves one authorized permission-safe citation for a governed evidence entry.
    /// </summary>
    /// <param name="query">The citation query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe citation result.</returns>
    public ValueTask<ConversationCitationResult> GetCitationAsync(
        GetConversationCitationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _citationAccessService.GetAsync(query, cancellationToken);
    }

    /// <summary>
    /// Retrieves one authorized audit-record view for review or in-memory export.
    /// </summary>
    /// <param name="query">The audit-record query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe audit-record result.</returns>
    public ValueTask<ConversationAuditRecordResult> GetAuditRecordAsync(
        GetConversationAuditRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _auditRecordAccessService.GetAsync(query, cancellationToken);
    }

    /// <summary>
    /// Retrieves one authorized privileged operational justification record for compliance review.
    /// </summary>
    /// <param name="query">The privileged-action review query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe privileged-action review result.</returns>
    public ValueTask<PrivilegedOperationalJustificationResult> GetPrivilegedOperationalJustificationAsync(
        GetPrivilegedOperationalJustificationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (_privilegedJustificationReviewService is null)
        {
            return ValueTask.FromResult(PrivilegedOperationalJustificationResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after privileged-action evidence is available."));
        }

        return _privilegedJustificationReviewService.GetAsync(query, cancellationToken);
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

        ConversationListCursorPosition? cursor = null;
        if (query.Page.ContinuationCursor is not null)
        {
            // Rebuild the pre-read scope identically to encode. A tamper/key-rotation integrity failure or a
            // tenant/caller/filter/sort mismatch makes TryDecode fail closed here, before any projection read.
            string scope = ConversationListCursor.BuildScope(query.TenantId, query.CallerPrincipalId, query.Filter);
            if (!_cursorCodec.TryDecode(
                    query.Page.ContinuationCursor,
                    ConversationListCursor.QueryType,
                    scope,
                    out string? position,
                    out _)
                || !ConversationListCursor.TryParsePosition(position, out ConversationListCursorPosition decoded))
            {
                return ConversationListResult.Hidden(query.SchemaVersion);
            }

            // Re-apply the domain bounds the codec does not own: it has no wall-clock lifetime and no offset
            // ceiling. An oversized offset, an expired cursor, or a future-dated cursor (clock skew or forged)
            // fails closed exactly as the hand-rolled codec did.
            DateTimeOffset now = _timeProvider.GetUtcNow();
            TimeSpan age = now - decoded.IssuedAt;
            if (decoded.Offset > _cursorOptions.MaxOffset
                || age < TimeSpan.Zero
                || age > _cursorOptions.MaxAge)
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

        ConversationProjectionIndexSnapshot snapshot;
        try
        {
            snapshot = await _projectionReadStore.ListAsync(query.TenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (ConversationProjectionConsistencyException)
        {
            return new ConversationListResult(
                query.SchemaVersion,
                ProjectionTrustState.Rebuilding,
                ProjectionFreshnessReasonCode.MixedGeneration,
                [],
                new ConversationPageMetadata(0),
                "Retry after the read model finishes rebuilding.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ConversationListResult.Unavailable(query.SchemaVersion);
        }

        // Tenant-scoped poison guard: reject any row whose stored TenantId disagrees with the request scope
        // before any filter, count, ordering, or freshness evaluation runs.
        List<ConversationSummaryProjectionV1> tenantScoped = snapshot.Summaries
            .Where(summary => summary.TenantId == query.TenantId)
            .ToList();

        string projectionGenerationToken = ComputeGenerationToken(tenantScoped);

        // Projection-generation binding is re-checked here rather than in the codec scope: the token is only
        // knowable after the projection read. A cursor issued against a superseded generation fails closed,
        // exactly as the prior DecodedCursor.Matches generation comparison did.
        if (cursor.HasValue
            && !string.Equals(cursor.Value.ProjectionGenerationToken, projectionGenerationToken, StringComparison.Ordinal))
        {
            return ConversationListResult.Hidden(query.SchemaVersion);
        }

        int offset = cursor?.Offset ?? 0;
        List<ConversationSummaryProjectionV1> accessibleMatches = tenantScoped
            .Where(summary => MatchesFilter(summary, query.Filter))
            .OrderByDescending(summary => summary.Freshness.LastAppliedEventTimestamp)
            .ThenBy(summary => summary.ConversationId.Value, StringComparer.Ordinal)
            .ToList();
        List<ConversationSummaryProjectionV1> accessible = accessibleMatches
            .Skip(offset)
            .Take(query.Page.PageSize + 1)
            .ToList();

        bool issueContinuation = accessible.Count > query.Page.PageSize;
        List<ConversationSummaryProjectionV1> selected = accessible
            .Take(query.Page.PageSize)
            .ToList();

        // Cross-key verification is scoped to the rows actually being returned, so the cost is proportional to
        // the page (NFR2, no per-conversation fan-out over the tenant) and a conversation mid-dispatch cannot
        // make an unrelated conversation unreadable.
        IReadOnlySet<string> inconsistent;
        try
        {
            inconsistent = await _projectionReadStore
                .ValidatePageAsync(query.TenantId, snapshot, selected, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ConversationProjectionConsistencyException)
        {
            return new ConversationListResult(
                query.SchemaVersion,
                ProjectionTrustState.Rebuilding,
                ProjectionFreshnessReasonCode.MixedGeneration,
                [],
                new ConversationPageMetadata(0),
                "Retry after the read model finishes rebuilding.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ConversationListResult.Unavailable(query.SchemaVersion);
        }

        // A row that cannot prove one completed generation is withheld rather than shown; an in-flight dispatch
        // elsewhere in the tenant means an accepted conversation may be missing from every page. Either way the
        // page must not claim to be current, but the rows that are proven are still returned.
        bool partialGeneration = inconsistent.Count > 0 || snapshot.HasIncompleteDispatch;
        IReadOnlyList<ConversationSummaryProjectionV1> pageCandidates = inconsistent.Count == 0
            ? selected
            : [.. selected.Where(summary => !inconsistent.Contains(summary.ConversationId.Value))];
        (ConversationSearchMatchSource matchSource, string whyVisible) = DetermineMatchSource(query.Filter);
        IReadOnlyList<ConversationSummaryV1> page = pageCandidates
            .Select(summary => ConversationSummaryV1
                .FromProjection(summary)
                .WithSearchTrustPreview(summary.SearchTrustPreview.WithMatchSource(matchSource, whyVisible)))
            .ToList();

        page = await _hydrationService
            .HydrateSummariesAsync(
                page,
                new ConversationHydrationContext(query.TenantId, query.CallerPrincipalId, query.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        string? nextCursor = issueContinuation
            ? _cursorCodec.Encode(
                ConversationListCursor.QueryType,
                ConversationListCursor.BuildScope(query.TenantId, query.CallerPrincipalId, query.Filter),
                ConversationListCursor.EncodePosition(offset + selected.Count, _timeProvider.GetUtcNow(), projectionGenerationToken))
            : null;

        (ProjectionTrustState state, ProjectionFreshnessReasonCode reason) = partialGeneration
            ? (ProjectionTrustState.Rebuilding, ProjectionFreshnessReasonCode.MixedGeneration)
            : AggregateFreshness(accessibleMatches);

        return new ConversationListResult(
            query.SchemaVersion,
            state,
            reason,
            page,
            new ConversationPageMetadata(page.Count, nextCursor),
            partialGeneration
                ? "Retry after the read model finishes rebuilding."
                : page.Count == 0
                ? "No accessible matches."
                : nextCursor is null
                ? "Accessible results are complete for the supplied filters."
                : "Use the cursor only with the same tenant, caller, filters, and ordering.");
    }

    private static bool MatchesFilter(ConversationSummaryProjectionV1 summary, ConversationListFilterV1 filter)
        => MatchesBusinessReference(summary.BusinessReference, filter.BusinessReference)
            && Matches(summary.ProjectId, filter.ProjectId)
            && Matches(summary.FolderId, filter.FolderId)
            && MatchesLifecycle(summary.LifecycleState, filter.LifecycleState)
            && MatchesProjectedAt(summary.Freshness.LastAppliedEventTimestamp, filter)
            && MatchesParticipant(summary.ParticipantPartyIds, filter.ParticipantPartyId)
            && Matches(summary.SearchTrustPreview.RedactionState, filter.RedactionState)
            && Matches(summary.Freshness.FreshnessState, filter.FreshnessState)
            && Matches(summary.SearchTrustPreview.AuditReadiness, filter.AuditReadiness)
            && Matches(summary.SearchTrustPreview.VerificationState, filter.VerificationState);

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

    private static (ConversationSearchMatchSource MatchSource, string WhyVisible) DetermineMatchSource(ConversationListFilterV1 filter)
    {
        if (filter.BusinessReference is not null)
        {
            return (ConversationSearchMatchSource.BusinessReference, "Visible through authorized tenant scope and matched business reference.");
        }

        if (filter.ProjectId is not null)
        {
            return (ConversationSearchMatchSource.ProjectReference, "Visible through authorized tenant scope and matched project reference.");
        }

        if (filter.FolderId is not null)
        {
            return (ConversationSearchMatchSource.FolderReference, "Visible through authorized tenant scope and matched folder reference.");
        }

        if (filter.ParticipantPartyId is not null)
        {
            return (ConversationSearchMatchSource.ParticipantReference, "Visible through authorized tenant scope and matched participant reference.");
        }

        if (filter.LifecycleState is not null)
        {
            return (ConversationSearchMatchSource.LifecycleState, "Visible through authorized tenant scope and matched lifecycle state.");
        }

        if (filter.ProjectedAtFrom.HasValue || filter.ProjectedAtTo.HasValue || filter.RecentActivityAfter.HasValue)
        {
            return (ConversationSearchMatchSource.DateRange, "Visible through authorized tenant scope and matched date range.");
        }

        if (filter.RedactionState is not null)
        {
            return (ConversationSearchMatchSource.RedactionState, "Visible through authorized tenant scope and matched redaction state.");
        }

        if (filter.FreshnessState is not null)
        {
            return (ConversationSearchMatchSource.FreshnessState, "Visible through authorized tenant scope and matched freshness state.");
        }

        if (filter.AuditReadiness is not null)
        {
            return (ConversationSearchMatchSource.AuditReadiness, "Visible through authorized tenant scope and matched audit readiness.");
        }

        if (filter.VerificationState is not null)
        {
            return (ConversationSearchMatchSource.VerificationState, "Visible through authorized tenant scope and matched verification state.");
        }

        return (ConversationSearchMatchSource.TenantScope, "Visible through authorized tenant scope.");
    }

    private static string ComputeGenerationToken(IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        if (summaries.Count == 0)
        {
            return "empty";
        }

        // Bind continuation to the complete logical tenant index rather than one row plus the maximum
        // position. Different conversations legitimately carry different cursors, and a mutation below the
        // prior maximum must still invalidate a continuation that could otherwise skip or duplicate rows.
        byte[] canonicalIndex = JsonSerializer.SerializeToUtf8Bytes(
            summaries.OrderBy(summary => summary.ConversationId.Value, StringComparer.Ordinal));
        return Convert.ToHexStringLower(SHA256.HashData(canonicalIndex));
    }

    private static (ProjectionTrustState State, ProjectionFreshnessReasonCode Reason) AggregateFreshness(
        IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        if (summaries.Count == 0)
        {
            return (ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current);
        }

        // Worst-case aggregation: priority is Forbidden > Unavailable > Rebuilding > Stale > Redacted > Current.
        // Forbidden normally cannot reach here because tenant-scoped poison guard already filtered cross-tenant
        // rows, but keep the ordering fail-closed if a future caller supplies one.
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
            if (state == ProjectionTrustState.Forbidden) { return 6; }
            if (state == ProjectionTrustState.Unavailable) { return 5; }
            if (state == ProjectionTrustState.Rebuilding) { return 4; }
            if (state == ProjectionTrustState.Stale) { return 3; }
            if (state == ProjectionTrustState.Redacted) { return 2; }
            return 0;
        }
    }
}
