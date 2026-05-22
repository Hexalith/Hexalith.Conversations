// <copyright file="ConversationReadApi.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Security.Claims;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Queries;

namespace Hexalith.Conversations.Server.Api;

/// <summary>
/// Defines guarded conversation read routes for hosts that explicitly opt in.
/// </summary>
/// <remarks>
/// Routes require an authenticated principal via <c>RequireAuthorization()</c>. Tenant scope is taken from
/// the <see cref="TenantIdClaimType"/> claim and caller identity from <see cref="ClaimTypes.NameIdentifier"/>.
/// The host MUST register an authentication scheme (JWT, cookie, etc.) and the corresponding authorization
/// services before mapping these endpoints; otherwise the routes return 401 by construction.
/// </remarks>
public static class ConversationReadApi
{
    /// <summary>
    /// The claim type that carries the tenant binding (matches the common OIDC <c>tid</c> claim name).
    /// </summary>
    public const string TenantIdClaimType = "tid";

    /// <summary>
    /// The header that carries the safe request correlation id. Optional; a new id is generated when absent.
    /// </summary>
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    /// <summary>
    /// Maps versioned conversation read endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapConversationReadApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/api/v1/conversations").RequireAuthorization();
        group.MapGet("/{conversationId}/citations/{evidenceEntryId}", GetCitationAsync);
        group.MapGet("/{conversationId}/temporal", GetTemporalAsync);
        group.MapGet("/{conversationId}", GetConversationAsync);
        group.MapGet("/{conversationId}/audit-records/{auditEvidenceHandle}", GetAuditRecordAsync);
        group.MapGet("/", ListConversationsAsync);
        return endpoints;
    }

    private static async Task<IResult> GetConversationAsync(
        string conversationId,
        HttpContext context,
        ConversationQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantCaller(context, out TenantId? tenantId, out string? callerPrincipalId, out string correlationId))
        {
            return HiddenDetail();
        }

        ConversationDetailResult result;
        try
        {
            result = await handler.GetAsync(
                new GetConversationQuery(
                    SchemaVersion.Current,
                    tenantId!,
                    callerPrincipalId!,
                    correlationId,
                    new ConversationId(conversationId)),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return HiddenDetail();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return UnavailableDetail();
        }

        return DetailToHttpResult(result);
    }

    private static async Task<IResult> GetCitationAsync(
        string conversationId,
        string evidenceEntryId,
        HttpContext context,
        ConversationQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantCaller(context, out TenantId? tenantId, out string? callerPrincipalId, out string correlationId))
        {
            return HiddenCitation();
        }

        ConversationCitationResult result;
        try
        {
            result = await handler.GetCitationAsync(
                new GetConversationCitationQuery(
                    SchemaVersion.Current,
                    tenantId!,
                    callerPrincipalId!,
                    correlationId,
                    new ConversationId(conversationId),
                    evidenceEntryId),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return HiddenCitation();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return UnavailableCitation();
        }

        return CitationToHttpResult(result);
    }

    private static async Task<IResult> GetTemporalAsync(
        string conversationId,
        HttpContext context,
        ConversationQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantCaller(context, out TenantId? tenantId, out string? callerPrincipalId, out string correlationId))
        {
            return HiddenTemporal();
        }

        if (!TryGet(context.Request.Query, "cursor", out string? cursor)
            || !TryParseTemporalCursor(cursor, out TemporalCursorParts cursorParts))
        {
            return HiddenTemporal();
        }

        ConversationTemporalDetailResult result;
        try
        {
            ConversationId parsedConversationId = new(conversationId);
            result = await handler.GetAtPointInTimeAsync(
                new GetConversationAtPointInTimeQuery(
                    SchemaVersion.Current,
                    tenantId!,
                    callerPrincipalId!,
                    correlationId,
                    parsedConversationId,
                    new ConversationTemporalAnchorV1(
                        SchemaVersion.Current,
                        tenantId!,
                        parsedConversationId,
                        ConversationTemporalAnchorV1.CompositeCursorKind,
                        SafeSourcePosition: cursorParts.SourcePosition,
                        ProjectionCursor: FormatProjectionCursor(cursorParts.ProjectionVersion),
                        ContractCursor: cursor,
                        ProjectionVersion: cursorParts.ProjectionVersion)),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return HiddenTemporal();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return UnavailableTemporal();
        }

        return TemporalToHttpResult(result);
    }

    private static async Task<IResult> GetAuditRecordAsync(
        string conversationId,
        string auditEvidenceHandle,
        HttpContext context,
        ConversationQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantCaller(context, out TenantId? tenantId, out string? callerPrincipalId, out string correlationId))
        {
            return HiddenAuditRecord();
        }

        ConversationAuditRecordResult result;
        try
        {
            result = await handler.GetAuditRecordAsync(
                new GetConversationAuditRecordQuery(
                    SchemaVersion.Current,
                    tenantId!,
                    callerPrincipalId!,
                    correlationId,
                    new ConversationId(conversationId),
                    auditEvidenceHandle,
                    AuditRecordActionClassification.Allowed),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return HiddenAuditRecord();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return UnavailableAuditRecord();
        }

        return AuditRecordToHttpResult(result);
    }

    private static async Task<IResult> ListConversationsAsync(
        HttpContext context,
        ConversationQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantCaller(context, out TenantId? tenantId, out string? callerPrincipalId, out string correlationId))
        {
            return HiddenList();
        }

        ConversationListFilterV1 filter;
        ConversationPageRequest page;
        try
        {
            filter = BuildFilter(context.Request.Query);
            page = BuildPage(context.Request.Query);
        }
        catch (ArgumentException)
        {
            // Side-channel equivalence: malformed filter input fails closed with the same shape as a denial.
            return HiddenList();
        }

        ConversationListResult result;
        try
        {
            result = await handler.ListAsync(
                new ListConversationsQuery(
                    SchemaVersion.Current,
                    tenantId!,
                    callerPrincipalId!,
                    correlationId,
                    filter,
                    page),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return UnavailableList();
        }

        return ListToHttpResult(result);
    }

    private static IResult DetailToHttpResult(ConversationDetailResult result)
    {
        // AC 3 / response matrix:
        //   Visible          → 200 OK with body
        //   Unavailable      → 503 Service Unavailable (authorized caller, infrastructure outage)
        //   Forbidden/Hidden → 404 Not Found (same-shape denial)
        if (result.FreshnessState == ProjectionTrustState.Unavailable)
        {
            return Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return result.Details is null
            ? Results.Json(result, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result, statusCode: StatusCodes.Status200OK);
    }

    private static IResult ListToHttpResult(ConversationListResult result)
    {
        if (result.FreshnessState == ProjectionTrustState.Unavailable)
        {
            return Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        // Hidden, accessible-and-current, and stale/rebuilding all return 200 with content-safe bodies.
        // Side-channel equivalence: malformed filters route through HiddenList() and produce the same shape.
        return Results.Json(result, statusCode: StatusCodes.Status200OK);
    }

    private static IResult AuditRecordToHttpResult(ConversationAuditRecordResult result)
    {
        if (result.FreshnessState == ProjectionTrustState.Unavailable
            || result.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return result.Details is null
            ? Results.Json(result, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result, statusCode: StatusCodes.Status200OK);
    }

    private static IResult CitationToHttpResult(ConversationCitationResult result)
    {
        if (result.FreshnessState == ProjectionTrustState.Unavailable
            || result.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return result.Citation is null
            ? Results.Json(result, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result, statusCode: StatusCodes.Status200OK);
    }

    private static IResult TemporalToHttpResult(ConversationTemporalDetailResult result)
    {
        if (result.FreshnessState == ProjectionTrustState.Unavailable
            || result.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return result.Details is null
            ? Results.Json(result, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result, statusCode: StatusCodes.Status200OK);
    }

    private static IResult HiddenDetail()
        => Results.Json(ConversationDetailResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status404NotFound);

    private static IResult UnavailableDetail()
        => Results.Json(ConversationDetailResult.Unavailable(SchemaVersion.Current), statusCode: StatusCodes.Status503ServiceUnavailable);

    private static IResult HiddenCitation()
        => Results.Json(ConversationCitationResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status404NotFound);

    private static IResult UnavailableCitation()
        => Results.Json(
            ConversationCitationResult.Unavailable(
                SchemaVersion.Current,
                ProjectionFreshnessReasonCode.Unavailable),
            statusCode: StatusCodes.Status503ServiceUnavailable);

    private static IResult HiddenTemporal()
        => Results.Json(ConversationTemporalDetailResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status404NotFound);

    private static IResult UnavailableTemporal()
        => Results.Json(
            ConversationTemporalDetailResult.Unavailable(
                SchemaVersion.Current,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after temporal evidence is available."),
            statusCode: StatusCodes.Status503ServiceUnavailable);

    private static IResult HiddenAuditRecord()
        => Results.Json(ConversationAuditRecordResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status404NotFound);

    private static IResult UnavailableAuditRecord()
        => Results.Json(
            ConversationAuditRecordResult.Unavailable(
                SchemaVersion.Current,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after the audit detail is available."),
            statusCode: StatusCodes.Status503ServiceUnavailable);

    private static IResult HiddenList()
        => Results.Json(ConversationListResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status200OK);

    private static IResult UnavailableList()
        => Results.Json(ConversationListResult.Unavailable(SchemaVersion.Current), statusCode: StatusCodes.Status503ServiceUnavailable);

    private static ConversationListFilterV1 BuildFilter(IQueryCollection query)
    {
        bool hasSystem = TryGet(query, "businessSystem", out string? businessSystem);
        bool hasValue = TryGet(query, "businessValue", out string? businessValue);
        if (hasSystem ^ hasValue)
        {
            throw new ArgumentException("businessSystem and businessValue must be supplied together.");
        }

        return new ConversationListFilterV1(
            BusinessReference: hasSystem && hasValue ? new BusinessReference(businessSystem, businessValue) : null,
            ProjectId: TryGet(query, "projectId", out string? projectId) ? new ProjectId(projectId) : null,
            FolderId: TryGet(query, "folderId", out string? folderId) ? new FolderId(folderId) : null,
            LifecycleState: TryGet(query, "lifecycleState", out string? lifecycleState) ? lifecycleState : null,
            ProjectedAtFrom: TryParseDate(query, "projectedAtFrom"),
            ProjectedAtTo: TryParseDate(query, "projectedAtTo"),
            RecentActivityAfter: TryParseDate(query, "recentActivityAfter"),
            ParticipantPartyId: TryGet(query, "participantPartyId", out string? participantPartyId)
                ? new PartyId(participantPartyId)
                : null,
            RedactionState: TryGet(query, "redactionState", out string? redactionState)
                ? ProjectionTrustState.Parse(redactionState)
                : null,
            FreshnessState: TryGet(query, "freshnessState", out string? freshnessState)
                ? ProjectionTrustState.Parse(freshnessState)
                : null,
            AuditReadiness: TryGet(query, "auditReadiness", out string? auditReadiness)
                ? ConversationAuditReadinessState.Parse(auditReadiness)
                : null,
            VerificationState: TryGet(query, "verificationState", out string? verificationState)
                ? ConversationVerificationState.Parse(verificationState)
                : null);
    }

    private static ConversationPageRequest BuildPage(IQueryCollection query)
    {
        int pageSize = 25;
        if (TryGet(query, "pageSize", out string? pageSizeText)
            && !int.TryParse(pageSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out pageSize))
        {
            throw new ArgumentException("Invalid page size.", nameof(query));
        }

        return new ConversationPageRequest(pageSize, TryGet(query, "cursor", out string? cursor) ? cursor : null);
    }

    private static bool TryGetTenantCaller(
        HttpContext context,
        out TenantId? tenantId,
        out string? callerPrincipalId,
        out string correlationId)
    {
        tenantId = null;
        callerPrincipalId = null;
        correlationId = TryGet(context.Request.Headers, CorrelationIdHeaderName, out string? suppliedCorrelation)
            ? suppliedCorrelation
            : Guid.NewGuid().ToString("N");

        ClaimsPrincipal? principal = context.User;
        if (principal is null || principal.Identity is null || !principal.Identity.IsAuthenticated)
        {
            return false;
        }

        string? tenantClaim = principal.FindFirstValue(TenantIdClaimType);
        string? callerClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(tenantClaim) || string.IsNullOrWhiteSpace(callerClaim))
        {
            return false;
        }

        try
        {
            tenantId = new TenantId(tenantClaim);
        }
        catch (ArgumentException)
        {
            return false;
        }

        callerPrincipalId = callerClaim;
        return true;
    }

    private static bool TryGet(IHeaderDictionary headers, string key, out string value)
    {
        value = headers.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues values)
            ? values.FirstOrDefault() ?? string.Empty
            : string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGet(IQueryCollection query, string key, out string value)
    {
        value = query.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues values)
            ? values.FirstOrDefault() ?? string.Empty
            : string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static DateTimeOffset? TryParseDate(IQueryCollection query, string key)
    {
        if (!TryGet(query, key, out string? value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset parsed)
            ? parsed
            : throw new ArgumentException("Invalid date filter.", key);
    }

    private static bool TryParseTemporalCursor(string value, out TemporalCursorParts cursorParts)
    {
        cursorParts = default;
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith("temporal:v1:", StringComparison.Ordinal)
            || value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is ':' or '-' or '_' or '.')))
        {
            return false;
        }

        string[] parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6
            || !string.Equals(parts[0], "temporal", StringComparison.Ordinal)
            || !string.Equals(parts[1], "v1", StringComparison.Ordinal)
            || !string.Equals(parts[2], "pos", StringComparison.Ordinal)
            || !long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
            || parsed < 1
            || !string.Equals(parts[4], "projection", StringComparison.Ordinal)
            || !long.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out long projectionVersion)
            || projectionVersion < 1)
        {
            return false;
        }

        cursorParts = new TemporalCursorParts(parsed, projectionVersion);
        return true;
    }

    private static string FormatProjectionCursor(long position)
        => $"pos:{position.ToString("D10", CultureInfo.InvariantCulture)}";

    private readonly record struct TemporalCursorParts(long SourcePosition, long ProjectionVersion);
}
