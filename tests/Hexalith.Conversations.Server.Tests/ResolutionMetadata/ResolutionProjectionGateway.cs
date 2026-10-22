using System.Security.Cryptography;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.EventStore.Testing.Http;
using Microsoft.Extensions.Options;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

// Source fixture only: payload-derived digest stand-ins exercise native paging and admission.
// Actual raw-envelope codec interoperability is covered in SDK Server.Tests. This fixture
// establishes neither production native provenance nor installation authority.
internal sealed class ResolutionProjectionGateway : IDisposable
{
    private readonly StreamReadEvent[] _events;
    private readonly HttpClient _httpClient;
    public IEventStoreGatewayClient Client { get; }
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
        _httpClient = new(new MockHttpMessageHandler(async (message, token) =>
        {
            var read = await message.Content!.ReadFromJsonAsync<StreamReadRequest>(JsonSerializerOptions.Web, token).ConfigureAwait(false);
            return Unavailable ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Page(read!), options: JsonSerializerOptions.Web) };
        })) { BaseAddress = new Uri("https://resolution-fixture.invalid/") };
        Client = new EventStoreGatewayClient(_httpClient, Options.Create(new EventStoreGatewayClientOptions()));
    }
    private StreamReadPage Page(StreamReadRequest request)
    {
        StreamReadEvent[] events = request.ToSequence == 0 ? [] : _events.Where(item => item.SequenceNumber > request.FromSequence
            && item.SequenceNumber <= request.ToSequence).Take(request.PageSize).ToArray();
        return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, events,
            new(request.FromSequence, request.ToSequence, events.LastOrDefault()?.SequenceNumber, Proof.HeadSequence,
                events.Length, events.Length > 0 && events[^1].SequenceNumber < Math.Min(request.ToSequence ?? Proof.HeadSequence, Proof.HeadSequence), null) { NativeProof = Proof });
    }

    public void Dispose() => _httpClient.Dispose();
}
