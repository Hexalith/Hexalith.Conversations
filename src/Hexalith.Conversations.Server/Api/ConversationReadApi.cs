// <copyright file="ConversationReadApi.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Queries;

namespace Hexalith.Conversations.Server.Api;

/// <summary>
/// Defines guarded conversation read routes for hosts that explicitly opt in.
/// </summary>
public static class ConversationReadApi
{
    /// <summary>
    /// Maps versioned conversation read endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapConversationReadApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/api/v1/conversations");
        group.MapGet("/{conversationId}", GetConversationAsync);
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
            return Results.Json(ConversationDetailResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status404NotFound);
        }

        ConversationDetailResult result = await handler.GetAsync(
            new GetConversationQuery(
                SchemaVersion.Current,
                tenantId!,
                callerPrincipalId!,
                correlationId,
                new ConversationId(conversationId)),
            cancellationToken)
            .ConfigureAwait(false);

        return Results.Json(result, statusCode: result.Details is null ? StatusCodes.Status404NotFound : StatusCodes.Status200OK);
    }

    private static async Task<IResult> ListConversationsAsync(
        HttpContext context,
        ConversationQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantCaller(context, out TenantId? tenantId, out string? callerPrincipalId, out string correlationId))
        {
            return Results.Json(ConversationListResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status404NotFound);
        }

        try
        {
            ConversationListResult result = await handler.ListAsync(
                new ListConversationsQuery(
                    SchemaVersion.Current,
                    tenantId!,
                    callerPrincipalId!,
                    correlationId,
                    BuildFilter(context.Request.Query),
                    BuildPage(context.Request.Query)),
                cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(result, statusCode: StatusCodes.Status200OK);
        }
        catch (ArgumentException)
        {
            return Results.Json(ConversationListResult.Hidden(SchemaVersion.Current), statusCode: StatusCodes.Status404NotFound);
        }
    }

    private static ConversationListFilterV1 BuildFilter(IQueryCollection query)
        => new(
            BusinessReference: TryGet(query, "businessSystem", out string? businessSystem)
                && TryGet(query, "businessValue", out string? businessValue)
                    ? new BusinessReference(businessSystem, businessValue)
                    : null,
            ProjectId: TryGet(query, "projectId", out string? projectId) ? new ProjectId(projectId) : null,
            FolderId: TryGet(query, "folderId", out string? folderId) ? new FolderId(folderId) : null,
            LifecycleState: TryGet(query, "lifecycleState", out string? lifecycleState) ? lifecycleState : null,
            DateFrom: TryParseDate(query, "dateFrom"),
            DateTo: TryParseDate(query, "dateTo"),
            RecentActivityAfter: TryParseDate(query, "recentActivityAfter"),
            ParticipantPartyId: TryGet(query, "participantPartyId", out string? participantPartyId)
                ? new PartyId(participantPartyId)
                : null);

    private static ConversationPageRequest BuildPage(IQueryCollection query)
    {
        int pageSize = TryGet(query, "pageSize", out string? pageSizeText)
            && int.TryParse(pageSizeText, out int parsedPageSize)
                ? parsedPageSize
                : 25;
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
        correlationId = TryGet(context.Request.Headers, "X-Correlation-Id", out string? suppliedCorrelation)
            ? suppliedCorrelation
            : Guid.NewGuid().ToString("N");

        if (!TryGet(context.Request.Headers, "X-Tenant-Id", out string? tenant)
            || !TryGet(context.Request.Headers, "X-Caller-Principal-Id", out string? caller))
        {
            return false;
        }

        tenantId = new TenantId(tenant);
        callerPrincipalId = caller;
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
        => TryGet(query, key, out string? value) && DateTimeOffset.TryParse(value, out DateTimeOffset parsed)
            ? parsed
            : null;
}
