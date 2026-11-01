// <copyright file="StubHttpHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.ServiceDefaults.Authentication;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>A shared primary handler that records requests and returns scripted responses.</summary>
internal sealed class StubHttpHandler(Func<HttpRequestMessage, string, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(),
            Header(request, "dapr-api-token"), Header(request, EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName), body);
        lock (_requests)
        {
            _requests.Add(recorded);
        }

        HttpResponseMessage response = await respond(request, body);
        response.RequestMessage ??= request;
        return response;
    }

    // Shared by every client pipeline in a composed host; the test owns its lifetime.
    protected override void Dispose(bool disposing)
    {
    }

    private static string? Header(HttpRequestMessage request, string name)
        => request.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(",", values) : null;
}
