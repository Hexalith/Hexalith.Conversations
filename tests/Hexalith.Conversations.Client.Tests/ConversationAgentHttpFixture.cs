// <copyright file="ConversationAgentHttpFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Hexalith.Conversations.Client.Tests;

/// <summary>Recorded local HTTP contract fixture; it performs no live requests.</summary>
public sealed class ConversationAgentHttpFixture : HttpMessageHandler
{
    /// <summary>Gets queued local HTTP responses.</summary>
    public Queue<HttpResponseMessage> Responses
    {
        get;
    } = [];
    /// <summary>Gets captured gateway request payloads.</summary>
    public List<JsonElement> Requests
    {
        get;
    } = [];
    /// <summary>Queues one serialized exact response.</summary>
    /// <param name="value">The portable response.</param>
    public void Enqueue<T>(T value) => Responses.Enqueue(new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(value, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
    });
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(JsonSerializer.Deserialize<JsonElement>(await request.Content!.ReadAsStringAsync(cancellationToken)));
        return Responses.Dequeue();
    }
}
