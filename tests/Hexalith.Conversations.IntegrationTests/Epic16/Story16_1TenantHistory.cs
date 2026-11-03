// <copyright file="Story16_1TenantHistory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Testing.Http;

using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// The authoritative Tenants envelopes exposed by a domain-specific in-process HTTP endpoint to the published gateway client.
/// </summary>
internal sealed class Story16_1TenantHistory : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, SortedDictionary<long, StreamReadEvent>> _streams = new(StringComparer.Ordinal);
    private readonly HttpClient _httpClient;

    /// <summary>Initializes the Tenants endpoint using the SDK's shared HTTP test transport.</summary>
    public Story16_1TenantHistory()
    {
        _httpClient = new HttpClient(new MockHttpMessageHandler(ReadTenantsStreamAsync))
        {
            BaseAddress = new Uri("https://tenant-history.test/"),
        };
        Client = new EventStoreGatewayClient(_httpClient, Options.Create(new EventStoreGatewayClientOptions()));
    }

    /// <summary>Gets the published HTTP gateway client consuming the Tenants endpoint.</summary>
    public EventStoreGatewayClient Client { get; }

    /// <summary>Records a persisted event exactly as its delivery envelope carries it.</summary>
    /// <param name="envelope">The delivery envelope.</param>
    public void Append(EventStoreDomainEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        lock (_gate)
        {
            if (!_streams.TryGetValue(envelope.AggregateId, out SortedDictionary<long, StreamReadEvent>? stream))
            {
                stream = [];
                _streams[envelope.AggregateId] = stream;
            }

            stream[envelope.SequenceNumber] = new StreamReadEvent(
                envelope.SequenceNumber,
                envelope.EventTypeName,
                envelope.Payload,
                envelope.SerializationFormat,
                1,
                envelope.MessageId,
                envelope.CorrelationId,
                envelope.CausationId,
                envelope.Timestamp,
                envelope.UserId);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();

    private async Task<HttpResponseMessage> ReadTenantsStreamAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        if (message.Method != HttpMethod.Post || message.RequestUri?.AbsolutePath != "/api/v1/streams/read")
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        StreamReadRequest? request = message.Content is null ? null : await message.Content
            .ReadFromJsonAsync<StreamReadRequest>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (request is null || request.Tenant != "platform:tenants" || request.Domain != "tenants"
            || string.IsNullOrWhiteSpace(request.AggregateId)
            || !message.Headers.TryGetValues("X-Hexalith-Scope", out IEnumerable<string>? scopes)
            || !scopes.SequenceEqual([request.Tenant]))
        {
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        }

        // This endpoint serves only the domain's persisted system/tenants envelopes. The real SDK client owns
        // request serialization, platform-scope headers, cancellation and gateway error translation.
        lock (_gate)
        {
            StreamReadEvent[] events = _streams.TryGetValue(request.AggregateId, out SortedDictionary<long, StreamReadEvent>? stream)
                ? [.. stream.Values.Where(e => e.SequenceNumber > request.FromSequence && e.SequenceNumber <= (request.ToSequence ?? long.MaxValue))]
                : [];
            long latest = stream is { Count: > 0 } ? stream.Keys.Max() : 0;
            StreamReadPage page = new("system", "tenants", request.AggregateId, events,
                new StreamReadMetadata(request.FromSequence, request.ToSequence,
                    events.LastOrDefault()?.SequenceNumber, latest, events.Length, false, null));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(page, options: JsonSerializerOptions.Web),
            };
        }
    }
}
