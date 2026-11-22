using Hexalith.Conversations.Server.Api;

namespace Hexalith.Conversations.Api;

/// <summary>Forwards only the authenticated caller's bearer and derives status scope from validated claims.</summary>
internal sealed class HumanGatewayBearerHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (accessor.HttpContext is not { } context
            || !ConversationReadApi.TryGetTenantCaller(context, out var tenant, out _, out var correlation)
            || context.Request.Headers.Authorization.Count != 1)
            throw new InvalidOperationException("A validated gateway caller is required.");
        request.Headers.TryAddWithoutValidation("Authorization", context.Request.Headers.Authorization[0]);
        if (context.Request.Method == HttpMethods.Get)
        {
            request.Headers.Add(ConversationReadApi.CorrelationIdHeaderName, correlation);
            context.Response.Headers[ConversationReadApi.CorrelationIdHeaderName] = correlation;
        }
        if (request.Method == HttpMethod.Get)
            request.Headers.Add("X-Hexalith-Tenant", tenant!.Value);
        return base.SendAsync(request, cancellationToken);
    }
}
