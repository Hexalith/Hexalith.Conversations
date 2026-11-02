// <copyright file="TenantStreamHistoryGateway.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// The authoritative Tenants stream served through the EventStore gateway range-read contract.
/// </summary>
internal sealed class TenantStreamHistoryGateway : IEventStoreGatewayClient
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, SortedDictionary<long, StreamReadEvent>> _streams = new(StringComparer.Ordinal);

    /// <summary>Gets the number of authoritative range reads served.</summary>
    public int Reads { get; private set; }

    /// <summary>Gets or sets a value indicating whether every read fails like an unreachable gateway.</summary>
    public bool Unavailable { get; set; }

    /// <summary>Records a persisted event exactly as its delivery envelope carries it.</summary>
    /// <param name="envelope">The delivery envelope.</param>
    public void Append(EventStoreDomainEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        lock (_gate)
        {
            if (!_streams.TryGetValue(envelope.AggregateId, out SortedDictionary<long, StreamReadEvent>? events))
            {
                events = [];
                _streams[envelope.AggregateId] = events;
            }

            events[envelope.SequenceNumber] = new StreamReadEvent(
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
    public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (Unavailable)
        {
            throw new HttpRequestException("Synthetic gateway outage.");
        }

        // Like the real gateway, the retained system/tenants history is requested through the explicit platform
        // scope and answered with the storage identity; any other stream identity is a defect.
        if (request.Tenant != "platform:tenants" || request.Domain != "tenants" || string.IsNullOrWhiteSpace(request.AggregateId))
        {
            throw new InvalidOperationException($"Unexpected history stream {request.Tenant}/{request.Domain}/{request.AggregateId}.");
        }

        lock (_gate)
        {
            Reads++;
            SortedDictionary<long, StreamReadEvent> stream = _streams.TryGetValue(request.AggregateId!, out SortedDictionary<long, StreamReadEvent>? found)
                ? found
                : [];
            StreamReadEvent[] events = [.. stream.Values.Where(e => e.SequenceNumber > request.FromSequence
                && e.SequenceNumber <= (request.ToSequence ?? long.MaxValue))];
            return Task.FromResult(new StreamReadPage(
                "system",
                request.Domain,
                request.AggregateId,
                events,
                new StreamReadMetadata(
                    request.FromSequence,
                    request.ToSequence,
                    events.Length == 0 ? null : events[^1].SequenceNumber,
                    stream.Count == 0 ? 0 : stream.Keys.Max(),
                    events.Length,
                    false,
                    null)));
        }
    }

    /// <inheritdoc />
    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
