// <copyright file="Story16_1EvidenceTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// Story 16.1 live evidence: the durable tenant projection persists one state/health/checkpoint record in a live
/// Dapr Redis state store, a restarted host reloads it without redelivery, and two replicas on separate sidecars
/// converge on one decision under split and concurrent consumer-group delivery.
/// </summary>
/// <remarks>
/// Deliveries enter through the production consumer processor that backs the <c>/tenants/events</c> route, and
/// state is asserted as the Dapr state store holds it. The authoritative Tenants stream is a gateway double fed
/// the same envelope bytes, because the subject is the consumer's persisted decision, not the EventStore server.
/// </remarks>
[Collection(Story16_1LiveCollection.Name)]
public sealed class Story16_1EvidenceTests(Story16_1LiveFixture fixture)
{
    private const string Caller = "user-live";
    private static readonly DateTimeOffset At = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Acknowledged tenant events persist one healthy record whose bytes carry state, health, and checkpoint.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task AcknowledgedEventsPersistOneStateHealthCheckpointRecordInTheLiveStateStore()
    {
        string tenant = NewTenant();
        EventStoreDomainEventEnvelope created = Persist(new TenantCreated(tenant, "Live", null, At), tenant, 1);
        EventStoreDomainEventEnvelope member = Persist(new UserAddedToTenant(tenant, Caller, TenantRole.TenantContributor), tenant, 2);
        using Story16_1Replica replica = new(fixture, fixture.ReplicaA);
        using Story16_1Replica observer = new(fixture, fixture.ReplicaB);

        (await replica.DeliverOnceAsync(created)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await replica.DeliverOnceAsync(member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        // Read through the other sidecar process: the record is in the shared state store, not in replica memory.
        JsonElement persisted = await observer.ReadPersistedAsync(tenant);
        persisted.GetProperty("health").GetInt32().ShouldBe((int)CheckpointedProjectionHealth.Healthy);
        persisted.GetProperty("lastContiguousSequence").GetInt64().ShouldBe(2);
        JsonElement accepted = persisted.GetProperty("lastAccepted");
        accepted.GetProperty("messageId").GetString().ShouldBe(member.MessageId);
        accepted.GetProperty("eventType").GetString().ShouldBe(member.EventTypeName);
        accepted.GetProperty("payloadFingerprint").GetString().ShouldBe(Convert.ToHexStringLower(SHA256.HashData(member.Payload)));
        persisted.GetProperty("pending").GetArrayLength().ShouldBe(0);
        persisted.GetProperty("lease").ValueKind.ShouldBe(JsonValueKind.Null);
        JsonElement state = persisted.GetProperty("state");
        state.GetProperty("tenantId").GetString().ShouldBe(tenant);
        state.GetProperty("status").GetString().ShouldBe(nameof(TenantStatus.Active));
        state.GetProperty("members").GetProperty(Caller).GetString().ShouldBe(nameof(TenantRole.TenantContributor));
        state.GetProperty("lastEvent").GetProperty("lastSequenceNumber").GetInt64().ShouldBe(2);

        ConversationTenantAccessDecision decision = await observer.CheckAsync(ConversationTenantAccessRequirement.Write, tenant, Caller);
        decision.IsAllowed.ShouldBeTrue();
        decision.ProjectionVersion.ShouldBe(2);
    }

    /// <summary>A restarted host on a restarted sidecar reloads the same persisted decision without any redelivery.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RestartReloadsThePersistedDecisionWithoutRedelivery()
    {
        string tenant = NewTenant();
        string before;
        using (Story16_1Replica original = new(fixture, fixture.ReplicaA))
        {
            (await original.DeliverOnceAsync(Persist(new TenantCreated(tenant, "Restart", null, At), tenant, 1))).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            (await original.DeliverOnceAsync(Persist(new UserAddedToTenant(tenant, Caller, TenantRole.TenantOwner), tenant, 2))).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            (await original.DeliverOnceAsync(Persist(new TenantConfigurationSet(tenant, "retention", "P30D"), tenant, 3))).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            (await original.CheckAsync(ConversationTenantAccessRequirement.Admin, tenant, Caller)).IsAllowed.ShouldBeTrue();
            before = (await original.ReadPersistedAsync(tenant)).GetRawText();
        }

        // The process and its sidecar both restart; Dapr does not redeliver acknowledged events.
        int starts = fixture.ReplicaA.Starts;
        await fixture.ReplicaA.StartAsync();
        fixture.ReplicaA.Starts.ShouldBe(starts + 1);
        using Story16_1Replica restarted = new(fixture, fixture.ReplicaA);

        ConversationTenantAccessDecision decision = await restarted.CheckAsync(ConversationTenantAccessRequirement.Admin, tenant, Caller);
        TenantProjectionSnapshot snapshot = (await restarted.SnapshotAsync(tenant)).ShouldNotBeNull();

        decision.IsAllowed.ShouldBeTrue();
        decision.ProjectionVersion.ShouldBe(3);
        snapshot.IsHealthy.ShouldBeTrue();
        snapshot.Checkpoint.ShouldBe(3);
        snapshot.State.ShouldNotBeNull().Configuration["retention"].ShouldBe("P30D");
        (await restarted.ReadPersistedAsync(tenant)).GetRawText().ShouldBe(before);
    }

    /// <summary>
    /// Two replicas on separate sidecars receive split, out-of-order, concurrent, and duplicate deliveries and
    /// converge on one persisted record and one authorization decision.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task TwoReplicasConvergeOnOnePersistedDecision()
    {
        string tenant = NewTenant();
        EventStoreDomainEventEnvelope[] events =
        [
            Persist(new TenantCreated(tenant, "Converge", null, At), tenant, 1),
            Persist(new UserAddedToTenant(tenant, Caller, TenantRole.TenantOwner), tenant, 2),
            Persist(new TenantConfigurationSet(tenant, "locale", "fr"), tenant, 3),
            Persist(new UserAddedToTenant(tenant, "user-other", TenantRole.TenantReader), tenant, 4),
            Persist(new UserRoleChanged(tenant, Caller, TenantRole.TenantOwner, TenantRole.TenantReader), tenant, 5),
            Persist(new TenantConfigurationRemoved(tenant, "locale"), tenant, 6),
        ];
        using Story16_1Replica a = new(fixture, fixture.ReplicaA);
        using Story16_1Replica b = new(fixture, fixture.ReplicaB);

        // Consumer-group delivery: each event reaches one replica, out of order.
        _ = await a.DeliverUntilAcknowledgedAsync(events[0]);
        _ = await b.DeliverUntilAcknowledgedAsync(events[2]);
        _ = await a.DeliverUntilAcknowledgedAsync(events[1]);
        (EventStoreDomainEventProcessingResult Result, int Deliveries)[] concurrent = await Task.WhenAll(
            a.DeliverUntilAcknowledgedAsync(events[3]),
            b.DeliverUntilAcknowledgedAsync(events[4]),
            a.DeliverUntilAcknowledgedAsync(events[5]));
        concurrent.ShouldAllBe(static delivery => delivery.Result == EventStoreDomainEventProcessingResult.Processed);

        TenantProjectionSnapshot fromA = (await a.SnapshotAsync(tenant)).ShouldNotBeNull();
        TenantProjectionSnapshot fromB = (await b.SnapshotAsync(tenant)).ShouldNotBeNull();
        fromA.IsHealthy.ShouldBeTrue();
        fromA.Checkpoint.ShouldBe(6);
        fromB.RecordVersion.ShouldBe(fromA.RecordVersion);
        fromB.State.ShouldNotBeNull().Members[Caller].ShouldBe(TenantRole.TenantReader, "the concurrent role change is never lost or regressed");
        fromB.State.Members["user-other"].ShouldBe(TenantRole.TenantReader);
        fromB.State.Configuration.ShouldNotContainKey("locale");
        string persistedA = (await a.ReadPersistedAsync(tenant)).GetRawText();
        (await b.ReadPersistedAsync(tenant)).GetRawText().ShouldBe(persistedA);

        foreach (Story16_1Replica replica in new[] { a, b })
        {
            ConversationTenantAccessDecision read = await replica.CheckAsync(ConversationTenantAccessRequirement.Read, tenant, Caller);
            ConversationTenantAccessDecision write = await replica.CheckAsync(ConversationTenantAccessRequirement.Write, tenant, Caller);
            read.IsAllowed.ShouldBeTrue();
            read.ProjectionVersion.ShouldBe(6);
            write.IsAllowed.ShouldBeFalse();
            write.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.InsufficientRole);
        }

        // Redelivering every event to both replicas changes nothing.
        foreach (EventStoreDomainEventEnvelope envelope in events)
        {
            _ = await a.DeliverUntilAcknowledgedAsync(envelope);
            using Story16_1Replica fresh = new(fixture, fixture.ReplicaB);
            _ = await fresh.DeliverUntilAcknowledgedAsync(envelope);
        }

        (await b.ReadPersistedAsync(tenant)).GetRawText().ShouldBe(persistedA);
    }

    /// <summary>
    /// A gap persists as Gapped and denies on every replica until a redelivery drives fenced recovery from history.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task PersistedGapDeniesOnEveryReplicaUntilFencedRecovery()
    {
        string tenant = NewTenant();
        EventStoreDomainEventEnvelope created = Persist(new TenantCreated(tenant, "Gap", null, At), tenant, 1);
        _ = Persist(new UserAddedToTenant(tenant, Caller, TenantRole.TenantOwner), tenant, 2);
        EventStoreDomainEventEnvelope future = Persist(new TenantUpdated(tenant, "Gap renamed", null, At), tenant, 3);
        using Story16_1Replica isolated = new(fixture, fixture.ReplicaA, withHistory: false);
        using Story16_1Replica other = new(fixture, fixture.ReplicaB);
        (await isolated.DeliverOnceAsync(created)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        _ = await Should.ThrowAsync<CheckpointedProjectionUnavailableException>(() => isolated.DeliverOnceAsync(future));

        JsonElement gapped = await other.ReadPersistedAsync(tenant);
        gapped.GetProperty("health").GetInt32().ShouldBe((int)CheckpointedProjectionHealth.Gapped);
        gapped.GetProperty("expected").GetInt64().ShouldBe(2);
        gapped.GetProperty("recoveryThrough").GetInt64().ShouldBe(3);
        gapped.GetProperty("pending").EnumerateArray().Single().GetProperty("sequence").GetInt64().ShouldBe(3);
        foreach (Story16_1Replica replica in new[] { isolated, other })
        {
            ConversationTenantAccessDecision denied = await replica.CheckAsync(ConversationTenantAccessRequirement.Read, tenant, Caller);
            denied.IsAllowed.ShouldBeFalse();
            denied.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessGapDetected);
        }

        (EventStoreDomainEventProcessingResult result, int deliveries) = await other.DeliverUntilAcknowledgedAsync(future);

        result.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        deliveries.ShouldBe(1);
        JsonElement recovered = await isolated.ReadPersistedAsync(tenant);
        recovered.GetProperty("health").GetInt32().ShouldBe((int)CheckpointedProjectionHealth.Healthy);
        recovered.GetProperty("lastContiguousSequence").GetInt64().ShouldBe(3);
        recovered.GetProperty("lease").ValueKind.ShouldBe(JsonValueKind.Null);
        recovered.GetProperty("lastFencingToken").GetInt64().ShouldBe(1);
        (await isolated.CheckAsync(ConversationTenantAccessRequirement.Admin, tenant, Caller)).IsAllowed.ShouldBeTrue();
    }

    private string NewTenant()
    {
        string tenant = $"tenant-live-{Guid.NewGuid():N}";
        fixture.Track(DurableTenantProjectionStore.KeyFor(tenant));
        return tenant;
    }

    private EventStoreDomainEventEnvelope Persist<TEvent>(TEvent @event, string tenantId, long sequence)
        where TEvent : IEventPayload
    {
        EventStoreDomainEventEnvelope envelope = new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            tenantId,
            "system",
            @event.GetType().FullName!,
            sequence,
            At.AddMinutes(sequence),
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "json",
            JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType()))
        {
            Domain = "tenants",
            UserId = "tenants-producer",
        };
        fixture.History.Append(envelope);
        return envelope;
    }
}
