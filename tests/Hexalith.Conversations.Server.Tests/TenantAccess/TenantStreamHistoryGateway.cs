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
    private readonly SortedDictionary<long, StreamReadEvent> _events = [];

    /// <summary>Gets the number of authoritative range reads served.</summary>
    public int Reads { get; private set; }

    /// <summary>Records a persisted event exactly as its delivery envelope carries it.</summary>
    /// <param name="envelope">The delivery envelope.</param>
    public void Append(EventStoreDomainEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        lock (_gate)
        {
            _events[envelope.SequenceNumber] = new StreamReadEvent(
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
            Reads++;
            StreamReadEvent[] events = [.. _events.Values.Where(e => e.SequenceNumber > request.FromSequence
                && e.SequenceNumber <= (request.ToSequence ?? long.MaxValue))];
            return Task.FromResult(new StreamReadPage(
                request.Tenant,
                request.Domain,
                request.AggregateId,
                events,
                new StreamReadMetadata(
                    request.FromSequence,
                    request.ToSequence,
                    events.Length == 0 ? null : events[^1].SequenceNumber,
                    _events.Count == 0 ? 0 : _events.Keys.Max(),
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
