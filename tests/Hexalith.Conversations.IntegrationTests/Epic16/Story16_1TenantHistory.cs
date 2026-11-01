// <copyright file="Story16_1TenantHistory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// The authoritative Tenants stream of the live lane, served through the gateway range-read contract.
/// </summary>
internal sealed class Story16_1TenantHistory : IEventStoreGatewayClient
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, SortedDictionary<long, StreamReadEvent>> _streams = new(StringComparer.Ordinal);

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
    public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            StreamReadEvent[] events = _streams.TryGetValue(request.AggregateId ?? string.Empty, out SortedDictionary<long, StreamReadEvent>? stream)
                ? [.. stream.Values.Where(e => e.SequenceNumber > request.FromSequence && e.SequenceNumber <= (request.ToSequence ?? long.MaxValue))]
                : [];
            long latest = stream is { Count: > 0 } ? stream.Keys.Max() : 0;
            return Task.FromResult(new StreamReadPage(
                request.Tenant,
                request.Domain,
                request.AggregateId,
                events,
                new StreamReadMetadata(request.FromSequence, request.ToSequence, events.Length == 0 ? null : events[^1].SequenceNumber, latest, events.Length, false, null)));
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
