// <copyright file="ConversationRuntimeCallerTokenHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Http.Headers;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Selects the gateway identity of a Conversations client call by execution path; the two paths never share one.</summary>
/// <remarks>Inside an HTTP request the inbound caller's Bearer token is forwarded, and a request without one fails without
/// sending. Outside a request (the background pump) no caller token is set, so the inner SDK gateway authority transport
/// attaches the enrolled worker token. No token is minted here.</remarks>
/// <param name="contextAccessor">The current request accessor.</param>
public sealed class ConversationRuntimeCallerTokenHandler(IHttpContextAccessor contextAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.Authorization = null;
        HttpContext? context = contextAccessor.HttpContext;
        if (context is not null)
        {
            if (!TryGetBearer(context, out string? token))
            {
                // Never fall back from a caller to the worker identity.
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = request });
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool TryGetBearer(HttpContext context, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? token)
    {
        token = null;
        if (context.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Authorization] is not { Count: 1 } values || values[0] is not { } header
            || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string candidate = header["Bearer ".Length..].Trim();
        if (candidate.Length == 0 || candidate.Any(static character => char.IsWhiteSpace(character) || char.IsControl(character)))
        {
            return false;
        }

        token = candidate;
        return true;
    }
}
