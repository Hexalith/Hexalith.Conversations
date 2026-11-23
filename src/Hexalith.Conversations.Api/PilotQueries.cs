using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Api;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Conversations.Api;

/// <summary>Dispatches the two adopter reads using only bearer-bound scope and the domain's read contracts.</summary>
internal static class PilotQueries
{
    internal static async Task<IResult> DetailAsync(string conversationId, HttpContext context, IEventStoreGatewayClient gateway, CancellationToken token)
    {
        if (!ConversationReadApi.TryGetTenantCaller(context, out var tenant, out var subject, out _)
            || !ConversationReadApi.ScopeHeadersMatch(context, tenant!, subject!))
            return ConversationReadApi.HiddenDetail();
        try
        {
            if (!ConversationReadApi.TryGetDetailModes(context.Request.Query, out bool nativeMembershipOnly, out bool advisoryMetadataOnly))
                return ConversationReadApi.UnavailableDetail();
            string? purpose = nativeMembershipOnly ? GetConversationQuery.NativeMembershipPurpose
                : advisoryMetadataOnly ? ConversationResolutionScoringLabel.SupportedPurpose : null;
            var request = new SubmitQueryRequest(tenant!.Value, "conversations", conversationId, GetConversationQuery.QueryType,
                Payload: JsonSerializer.SerializeToElement(new { conversationId, nativeMembershipOnly, advisoryMetadataOnly }));
            var result = (await gateway.SubmitQueryAsync<ConversationDetailResult>(request, cancellationToken: token)).Payload;
            if (result is null) return ConversationReadApi.UnavailableDetail();
            if (result.Details is { } details && (details.TenantId != tenant || details.ConversationId.Value != conversationId))
                return ConversationReadApi.HiddenDetail();
            if (result.Details is { } visible)
            {
                if (result.ReadPurpose != purpose) return ConversationReadApi.UnavailableDetail();
                if (purpose is not null)
                {
                    // A bounded caller receives only the fields admitted by the declared purpose.
                    var bounded = new ConversationDetailsV1(visible.SchemaVersion, visible.TenantId, visible.ConversationId,
                        visible.Freshness, visible.LifecycleState, advisoryMetadataOnly ? visible.Label : null, ProjectId: visible.ProjectId);
                    result = ConversationDetailResult.Visible(result.SchemaVersion, bounded,
                        advisoryMetadataOnly ? "Classified advisory evidence is available." : "Native membership evidence is available.")
                        with { ReadPurpose = purpose };
                }
            }
            return ConversationReadApi.DetailToHttpResult(result);
        }
        catch (EventStoreGatewayException ex) when (ex.StatusCode == 401) { return Results.Unauthorized(); }
        catch (EventStoreGatewayException ex) when (ex.StatusCode is 400 or 403 or 404) { return ConversationReadApi.HiddenDetail(); }
        catch (Exception) when (!token.IsCancellationRequested) { return ConversationReadApi.UnavailableDetail(); }
    }

    internal static async Task<IResult> ListAsync(HttpContext context, IEventStoreGatewayClient gateway, CancellationToken token)
    {
        if (!ConversationReadApi.TryGetTenantCaller(context, out var tenant, out var subject, out _)
            || !ConversationReadApi.ScopeHeadersMatch(context, tenant!, subject!))
            return ConversationReadApi.HiddenList();
        ConversationListFilterV1 filter;
        ConversationPageRequest page;
        try
        {
            filter = ConversationReadApi.BuildFilter(context.Request.Query);
            page = ConversationReadApi.BuildPage(context.Request.Query);
        }
        catch (ArgumentException) { return ConversationReadApi.HiddenList(); }

        try
        {
            var request = new SubmitQueryRequest(tenant!.Value, "conversations", tenant.Value, "conversation-list",
                Payload: JsonSerializer.SerializeToElement(new { filter, page }, JsonSerializerOptions.Web));
            var body = (await gateway.SubmitQueryAsync<JsonElement>(request, cancellationToken: token)).Payload;
            if (body.ValueKind != JsonValueKind.Object) return ConversationReadApi.UnavailableList();

            // The public DTO normalizes absent rows to empty. Require the owner to supply an array.
            var rows = body.EnumerateObject().SingleOrDefault(property =>
                string.Equals(property.Name, "conversations", StringComparison.OrdinalIgnoreCase)).Value;
            if (rows.ValueKind != JsonValueKind.Array) return ConversationReadApi.UnavailableList();

            var result = body.Deserialize<ConversationListResult>(JsonSerializerOptions.Web);
            if (result is null || result.Page.ReturnedCount != result.Conversations.Count)
                return ConversationReadApi.UnavailableList();
            if (result.Conversations.Any(row => row.TenantId != tenant)) return ConversationReadApi.HiddenList();
            return ConversationReadApi.ListToHttpResult(result);
        }
        catch (EventStoreGatewayException ex) when (ex.StatusCode == 401) { return Results.Unauthorized(); }
        catch (EventStoreGatewayException ex) when (ex.StatusCode is 400 or 403 or 404) { return ConversationReadApi.HiddenList(); }
        catch (Exception) when (!token.IsCancellationRequested) { return ConversationReadApi.UnavailableList(); }
    }
}
