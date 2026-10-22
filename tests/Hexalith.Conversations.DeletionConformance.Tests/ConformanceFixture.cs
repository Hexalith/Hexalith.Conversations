using System.Security.Claims;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.DeletionConformance;
using Hexalith.Conversations.Events;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.DeletionConformance.Tests;

/// <summary>Serialized SDK-port component fixture; it supplies no real enrollment or live persistence evidence.</summary>
internal sealed class ConformanceFixture : IAuthoritativeEventStreamReader
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static readonly DateTimeOffset At = DateTimeOffset.Parse("2026-10-22T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    internal readonly Dictionary<AggregateIdentity, AuthoritativeEventStream> Stored = [];
    internal readonly HttpContextAccessor Http = new() { HttpContext = new DefaultHttpContext() };
    internal readonly DeletionConformanceOptions Options = new()
    {
        Enabled = true, TenantId = "tenant-one", TargetVersion = new string('a', 40), ReceiverPrincipalId = "receiver",
        LookupPrincipalId = "receipt-verifier", WorkerPrincipalId = "source-worker", GatewayWorkloadId = "eventstore",
        StorageIdentity = "independent-receipt-store", GatewayBaseAddress = "https://gateway.example/",
    };
    internal ConversationDeletionSignal Signal => new("signal-one", new("tenant-one"), new("conv-one"), "tenant-one:conversation:conv-one", 1, "approval-one");
    internal AcceptConformanceDeletion Command => new(Signal, "attempt-one", Options.TargetVersion);
    internal DeletionConformanceStreams Streams => new(this);
    internal DeletionConformanceAuthorization Authorization => new(Http);
    internal DeletionConformanceReceiptQuery Lookup => new(Microsoft.Extensions.Options.Options.Create(Options), Authorization, Streams);
    internal DeletionConformanceAdmission Admission => new(Microsoft.Extensions.Options.Options.Create(Options), Authorization, Streams);
    internal int Reads { get; private set; }

    internal void Authenticate(string? principal = null, string operation = EventStoreWorkloadOperations.DomainServiceQuery, bool bindings = true)
    {
        List<Claim> claims = [new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, principal ?? Options.LookupPrincipalId),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, operation)];
        if (bindings)
        {
            claims.Add(new(EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType, Options.TenantId));
            claims.Add(new(EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType, DeletionConformanceContract.Domain));
        }
        Http.HttpContext!.User = new(new ClaimsIdentity(claims, EventStoreWorkloadAuthenticationDefaults.WorkloadScheme));
    }

    internal QueryEnvelope Query(ConversationDeletionSignal? signal = null, string? attempt = null, string? target = null)
    {
        signal ??= Signal;
        return new(signal.TenantId.Value, DeletionConformanceContract.Domain, signal.ConversationDeletionSignalId,
            "deletion-conformance-receipt", JsonSerializer.SerializeToUtf8Bytes(new DeletionConformanceLookup(signal, attempt ?? "attempt-one", target ?? Options.TargetVersion), Json),
            "lookup-correlation", Options.LookupPrincipalId);
    }

    internal CommandEnvelope Envelope(AcceptConformanceDeletion? command = null)
    {
        command ??= Command;
        return new("command-one", command.Signal.TenantId.Value, DeletionConformanceContract.Domain, command.Signal.ConversationDeletionSignalId,
            typeof(AcceptConformanceDeletion).FullName!, JsonSerializer.SerializeToUtf8Bytes(command, Json), "command-correlation", null, Options.WorkerPrincipalId, null);
    }

    internal void Persist(object value)
    {
        var identity = new AggregateIdentity(Options.TenantId, DeletionConformanceContract.Domain, Signal.ConversationDeletionSignalId);
        var events = Stored.GetValueOrDefault(identity)?.Events.ToList() ?? [];
        events.Add(Event(events.Count + 1, value));
        Stored[identity] = new(identity, events.Count, At, events, "independent-complete-receipt-observation");
    }

    internal void PersistSource(ConversationDeletionDeliveryAction action = ConversationDeletionDeliveryAction.Attempt)
    {
        var identity = new AggregateIdentity(Signal.TenantId.Value, "conversation", Signal.ConversationId.Value);
        var metadata = new ConversationEventMetadata(SchemaVersion.Current, "approval-event", ConversationEventType.ConversationDeletionApproved,
            Signal.TenantId, Signal.ConversationId, "correlation", At, new PartyId("approver"));
        var approved = new ConversationDeletionApprovedDomainEvent(metadata, Signal, new(new AuditEvidenceHandle("audit-handle"), "policy-one", At));
        var delivery = new ConversationDeletionDeliveryRecordedDomainEvent(metadata with { EventId = "attempt-event", EventType = ConversationEventType.ConversationDeletionDeliveryRecorded },
            Signal, action, Command.DeliveryAttemptId, Options.TargetVersion, 0);
        Stored[identity] = new(identity, 2, At, [Event(1, approved), Event(2, delivery)], "source-observation");
    }

    internal static StreamReadEvent Event(long revision, object value) => new(revision, value.GetType().FullName!,
        JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), Json), "json", 1, "persisted-" + revision, null, null, At, "fixture");

    internal static DeletionConformanceResult Result(QueryResult result)
        => JsonSerializer.Deserialize<DeletionConformanceResult>(result.PayloadBytes!, Json)!;

    /// <inheritdoc/>
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); Reads++;
        var source = Stored.GetValueOrDefault(identity) ?? new(identity, 0, At, [], "empty-complete-observation");
        // Every read crosses serialized bytes, so replay never reuses command or in-memory state instances.
        return Task.FromResult(new AuthoritativeStreamReadResult(JsonSerializer.Deserialize<AuthoritativeEventStream>(
            JsonSerializer.SerializeToUtf8Bytes(source, Json), Json), null));
    }
}
