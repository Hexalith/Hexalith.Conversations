using System.Security.Cryptography;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

// Source fixture only: payload-derived digest stand-ins exercise native paging and admission.
// Actual raw-envelope codec interoperability is covered in SDK Server.Tests. This fixture
// establishes neither production native provenance nor installation authority.
internal sealed class ResolutionProjectionGateway : IEventStoreGatewayClient
{
    private readonly StreamReadEvent[] _events;
    public NativeSourceProof Proof { get; }
    public bool Unavailable { get; set; }
    public ResolutionProjectionGateway(ProjectionRequest request, string incarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAV")
    {
        var identity = new AggregateIdentity(request.TenantId, request.Domain, request.AggregateId);
        NativeSourceProof proof = NativeSourceProofCodec.CreateSeed(identity, incarnation);
        _events = request.Events.Select(item => new StreamReadEvent(item.SequenceNumber, item.EventTypeName,
            item.Payload.ToArray(), item.SerializationFormat, item.MetadataVersion ?? 1, item.MessageId!,
            item.CorrelationId, null, item.Timestamp, item.UserId)
        {
            NativeEventDigest = Convert.ToHexStringLower(SHA256.HashData(item.Payload)),
            GlobalPosition = item.GlobalPosition, PayloadVersion = item.StoredPayloadVersion,
        }).ToArray();
        foreach (StreamReadEvent item in _events)
            proof = NativeSourceProofCodec.ExtendDigest(proof, identity, item.SequenceNumber, item.MessageId, item.NativeEventDigest!);
        Proof = proof;
    }
    public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Unavailable) throw new EventStoreGatewayException(503, "native source unavailable");
        StreamReadEvent[] events = request.ToSequence == 0 ? [] : _events.Where(item => item.SequenceNumber > request.FromSequence
            && item.SequenceNumber <= request.ToSequence).Take(request.PageSize).ToArray();
        return Task.FromResult(new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, events,
            new(request.FromSequence, request.ToSequence, events.LastOrDefault()?.SequenceNumber, Proof.HeadSequence,
                events.Length, events.Length > 0 && events[^1].SequenceNumber < Math.Min(request.ToSequence ?? Proof.HeadSequence, Proof.HeadSequence), null) { NativeProof = Proof }));
    }
    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
