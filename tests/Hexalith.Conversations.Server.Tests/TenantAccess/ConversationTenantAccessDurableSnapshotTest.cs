// <copyright file="ConversationTenantAccessDurableSnapshotTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Story 16.1 I/O matrix at the Conversations consumer: every evaluation reads one durable
/// state/health/checkpoint snapshot, and only a persisted healthy snapshot allows.
/// </summary>
public sealed class ConversationTenantAccessDurableSnapshotTest
{
    private const string Caller = "user-durable";
    private static readonly TenantId Tenant = new("tenant-durable");
    private static readonly DateTimeOffset At = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Exact-next deliveries commit transition and checkpoint, and the healthy snapshot allows with its checkpoint.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task HealthyExactNextDeliveriesAllowFromThePersistedSnapshot()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider host = harness.CreateHost();
        await DeliverMemberTenantAsync(harness, host);

        ConversationTenantAccessDecision decision = await CheckAsync(host, ConversationTenantAccessRequirement.Write);

        decision.IsAllowed.ShouldBeTrue();
        decision.ProjectionVersion.ShouldBe(2);
        CheckpointedProjectionRecord<TenantLocalState> record = harness.Record(Tenant.Value).ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        record.LastContiguousSequence.ShouldBe(2);
        record.State.ShouldNotBeNull().Members[Caller].ShouldBe(TenantRole.TenantContributor);
        host.GetRequiredService<ITenantProjectionStore>().ShouldBeOfType<DurableTenantProjectionStore>();
    }

    /// <summary>An identical accepted duplicate is a no-op and authorization is unchanged.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task IdenticalDuplicateIsANoOpAndKeepsAuthorization()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider host = harness.CreateHost();
        (EventStoreDomainEventEnvelope _, EventStoreDomainEventEnvelope member) = await DeliverMemberTenantAsync(harness, host);
        long version = harness.Record(Tenant.Value)!.Version;

        // A second replica (empty marker store) receives the redelivery.
        using ServiceProvider replica = harness.CreateHost();
        (await DurableTenantAccessHarness.DeliverAsync(replica, member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        harness.Record(Tenant.Value)!.Version.ShouldBe(version);
        (await CheckAsync(replica, ConversationTenantAccessRequirement.Read)).IsAllowed.ShouldBeTrue();
    }

    /// <summary>A conflicting identity at the accepted sequence persists terminal Corrupt and denies.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ConflictingIdentityPersistsCorruptAndDenies()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider host = harness.CreateHost();
        await DeliverMemberTenantAsync(harness, host);
        EventStoreDomainEventEnvelope forged = DurableTenantAccessHarness.Envelope(
            new UserAddedToTenant(Tenant.Value, "intruder", TenantRole.TenantOwner),
            Tenant.Value,
            2);

        (await DurableTenantAccessHarness.DeliverAsync(host, forged)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        harness.Record(Tenant.Value)!.Health.ShouldBe(CheckpointedProjectionHealth.Corrupt);
        ConversationTenantAccessDecision decision = await CheckAsync(host, ConversationTenantAccessRequirement.Read);
        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantProjectionPoisoned);
        decision.ProjectionVersion.ShouldBeNull();
    }

    /// <summary>
    /// A future sequence persists Gapped with an immutable expected sequence and a monotonic horizon; it denies,
    /// keeps the delivery retryable, and fenced recovery restores a healthy snapshot only on redelivery.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task FutureSequencePersistsGappedDeniesAndFencedRecoveryRestoresHealthy()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider isolated = harness.CreateHost(withHistory: false);
        FakeEventStoreGatewayClient isolatedGateway = isolated.GetRequiredService<IEventStoreGatewayClient>().ShouldBeOfType<FakeEventStoreGatewayClient>();
        await DurableTenantAccessHarness.DeliverAsync(isolated, harness.Persist(new TenantCreated(Tenant.Value, "Gapped", null, At), Tenant.Value, 1));
        EventStoreDomainEventEnvelope member = harness.Persist(new UserAddedToTenant(Tenant.Value, Caller, TenantRole.TenantOwner), Tenant.Value, 2);
        EventStoreDomainEventEnvelope configured = harness.Persist(new TenantConfigurationSet(Tenant.Value, "k", "v"), Tenant.Value, 3);
        EventStoreDomainEventEnvelope renamed = harness.Persist(new TenantUpdated(Tenant.Value, "Renamed", null, At), Tenant.Value, 4);

        _ = await Should.ThrowAsync<CheckpointedProjectionUnavailableException>(() => DurableTenantAccessHarness.DeliverAsync(isolated, renamed));
        _ = await Should.ThrowAsync<CheckpointedProjectionUnavailableException>(() => DurableTenantAccessHarness.DeliverAsync(isolated, configured));

        CheckpointedProjectionRecord<TenantLocalState> gapped = harness.Record(Tenant.Value).ShouldNotBeNull();
        gapped.Health.ShouldBe(CheckpointedProjectionHealth.Gapped);
        gapped.Expected.ShouldBe(2);
        gapped.RecoveryThrough.ShouldBe(4);
        gapped.Pending.Select(static p => p.Sequence).ShouldBe([3L, 4L]);
        int readsBeforeDenial = isolatedGateway.SubmittedStreamReads.Count;
        ConversationTenantAccessDecision denied = await CheckAsync(isolated, ConversationTenantAccessRequirement.Read);
        denied.IsAllowed.ShouldBeFalse();
        denied.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessGapDetected);
        denied.IsRetryable.ShouldBeTrue();
        denied.ProjectionVersion.ShouldBeNull();
        isolatedGateway.SubmittedStreamReads.Count.ShouldBe(readsBeforeDenial, "authorization never starts recovery");

        using ServiceProvider recovering = harness.CreateHost();
        FakeEventStoreGatewayClient recoveringGateway = recovering.GetRequiredService<IEventStoreGatewayClient>().ShouldBeOfType<FakeEventStoreGatewayClient>();
        harness.ConfigureHistory(Tenant.Value, fromSequence: 1, toSequence: 4);
        (await DurableTenantAccessHarness.DeliverAsync(recovering, configured)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        StreamReadRequest recoveryRead = recoveringGateway.SubmittedStreamReads.ShouldHaveSingleItem();
        recoveryRead.Tenant.ShouldBe("platform:tenants");
        recoveryRead.Domain.ShouldBe("tenants");
        recoveryRead.AggregateId.ShouldBe(Tenant.Value);
        recoveryRead.FromSequence.ShouldBe(1);
        recoveryRead.ToSequence.ShouldBe(4);
        CheckpointedProjectionRecord<TenantLocalState> healthy = harness.Record(Tenant.Value).ShouldNotBeNull();
        healthy.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        healthy.LastContiguousSequence.ShouldBe(4);
        healthy.Pending.ShouldBeEmpty();
        healthy.Lease.ShouldBeNull();
        int readsBeforeAllowance = isolatedGateway.SubmittedStreamReads.Count;
        ConversationTenantAccessDecision allowed = await CheckAsync(isolated, ConversationTenantAccessRequirement.Admin);
        allowed.IsAllowed.ShouldBeTrue();
        allowed.ProjectionVersion.ShouldBe(4);
        isolatedGateway.SubmittedStreamReads.Count.ShouldBe(readsBeforeAllowance, "authorization never starts recovery");
        harness.ConfigureHistory(Tenant.Value, fromSequence: 1, toSequence: 2);
        (await DurableTenantAccessHarness.DeliverAsync(recovering, member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        StreamReadRequest duplicateRead = recoveringGateway.SubmittedStreamReads.Last();
        duplicateRead.Tenant.ShouldBe("platform:tenants");
        duplicateRead.Domain.ShouldBe("tenants");
        duplicateRead.AggregateId.ShouldBe(Tenant.Value);
        duplicateRead.FromSequence.ShouldBe(1);
        duplicateRead.ToSequence.ShouldBe(2);
        harness.Record(Tenant.Value)!.Version.ShouldBe(healthy.Version, "the late gap event is verified against history and never reapplied");
        foreach (FakeEventStoreGatewayClient gateway in new[] { isolatedGateway, recoveringGateway })
        {
            gateway.SubmittedCommands.ShouldBeEmpty("tenant history recovery is read-only");
            gateway.SubmittedQueries.ShouldBeEmpty("tenant history recovery uses bounded stream reads");
        }
    }

    /// <summary>Compare-and-swap exhaustion keeps the delivery unacknowledged; a provider outage makes authorization unavailable.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RetryExhaustionIsRetryableAndProviderOutageDeniesAsUnavailable()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider host = harness.CreateHost();
        await DeliverMemberTenantAsync(harness, host);
        harness.Store.ConcurrentWriteBeforeTrySave = () =>
        {
            CheckpointedProjectionRecord<TenantLocalState> current = harness.Record(Tenant.Value)!;
            harness.Store.SeedRaw(
                DurableTenantAccessHarness.StateStoreName,
                DurableTenantProjectionStore.KeyFor(Tenant.Value),
                current with { Version = current.Version + 1 });
        };

        CheckpointedProjectionUnavailableException exhausted = await Should.ThrowAsync<CheckpointedProjectionUnavailableException>(
            () => DurableTenantAccessHarness.DeliverAsync(host, harness.Persist(new TenantDisabled(Tenant.Value, At), Tenant.Value, 3)));
        exhausted.Reason.ShouldBe(CheckpointedProjectionUnavailableReason.ConcurrencyExhausted);
        harness.Store.ConcurrentWriteBeforeTrySave = null;
        harness.Record(Tenant.Value)!.LastContiguousSequence.ShouldBe(2);

        using ServiceProvider outage = harness.CreateHost(store: new UnavailableReadModelStore());
        ConversationTenantAccessDecision decision = await CheckAsync(outage, ConversationTenantAccessRequirement.Read);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessUnavailable);
        decision.IsRetryable.ShouldBeTrue();
    }

    /// <summary>
    /// A missing record denies exactly like a non-member of an existing tenant: <c>UnknownTenant</c>, non-retryable,
    /// with the same public error and rejection, so the decision never discloses whether the tenant exists.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task MissingRecordDeniesExactlyLikeANonMemberOfAnExistingTenant()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider host = harness.CreateHost();
        await DeliverMemberTenantAsync(harness, host);

        ConversationTenantAccessDecision missing = await DurableTenantAccessHarness.Access(host).CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            new TenantId("tenant-never-projected"),
            Caller,
            commandTenantId: new TenantId("tenant-never-projected"),
            cancellationToken: TestContext.Current.CancellationToken);
        ConversationTenantAccessDecision nonMember = await DurableTenantAccessHarness.Access(host).CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "user-not-a-member",
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        missing.IsAllowed.ShouldBeFalse();
        missing.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.UnknownTenant);
        missing.IsRetryable.ShouldBeFalse();
        missing.ProjectionVersion.ShouldBeNull();
        missing.ProjectionWatermark.ShouldBeNull();
        nonMember.IsAllowed.ShouldBeFalse();
        nonMember.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.MissingMember);
        nonMember.IsRetryable.ShouldBe(missing.IsRetryable);
        nonMember.ProjectionVersion.ShouldBe(missing.ProjectionVersion);
        PublicShape(missing).ShouldBe(PublicShape(nonMember));
        harness.Gateway.SubmittedStreamReads.ShouldBeEmpty("authorization never starts recovery");
    }

    /// <summary>
    /// Malformed and unavailable snapshots deny with one non-retryable, non-disclosing public result; only the
    /// internal decision keeps its retry hint.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task MalformedAndUnavailableSnapshotsDenyWithOneNonRetryableNonDisclosingResult()
    {
        DurableTenantAccessHarness harness = new();
        harness.Store.SeedRaw(
            DurableTenantAccessHarness.StateStoreName,
            DurableTenantProjectionStore.KeyFor("tenant-malformed"),
            new CheckpointedProjectionRecord<TenantLocalState> { SchemaVersion = 1, Health = CheckpointedProjectionHealth.Healthy, LastContiguousSequence = 3 });
        using ServiceProvider host = harness.CreateHost();
        using ServiceProvider outage = harness.CreateHost(store: new UnavailableReadModelStore());

        ConversationTenantAccessDecision malformed = await CheckAsync(host, ConversationTenantAccessRequirement.Read, new TenantId("tenant-malformed"));
        ConversationTenantAccessDecision unavailable = await CheckAsync(outage, ConversationTenantAccessRequirement.Read, new TenantId("tenant-malformed"));

        malformed.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessStale);
        unavailable.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessUnavailable);
        foreach (ConversationTenantAccessDecision decision in new[] { malformed, unavailable })
        {
            decision.IsAllowed.ShouldBeFalse();
            decision.IsRetryable.ShouldBeTrue();
            decision.ProjectionVersion.ShouldBeNull();
            decision.ProjectionWatermark.ShouldBeNull();
        }

        PublicShape(unavailable).ShouldBe(PublicShape(malformed));
        malformed.ToSafeErrorResult(SchemaVersion.Current, "correlation-durable").Errors.ShouldHaveSingleItem().IsRetryable.ShouldBeFalse();
    }

    /// <summary>
    /// Every unsafe tenant state returns the same public denial as a non-member of a healthy tenant: non-retryable,
    /// with the same code and reason. The precise internal reason stays on the decision.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task EveryUnsafeStatePublicDenialMatchesANonMemberOfAHealthyTenant()
    {
        DurableTenantAccessHarness harness = new();
        using ServiceProvider host = harness.CreateHost();
        using ServiceProvider isolated = harness.CreateHost(withHistory: false);
        using ServiceProvider outage = harness.CreateHost(store: new UnavailableReadModelStore());
        await DeliverMemberTenantAsync(harness, host);

        // Gapped: a future delivery that the isolated replica cannot recover.
        await DurableTenantAccessHarness.DeliverAsync(isolated, harness.Persist(new TenantCreated("tenant-gapped", "Gapped", null, At), "tenant-gapped", 1));
        _ = await Should.ThrowAsync<CheckpointedProjectionUnavailableException>(() => DurableTenantAccessHarness.DeliverAsync(
            isolated, harness.Persist(new TenantDisabled("tenant-gapped", At), "tenant-gapped", 3)));

        // Corrupt: a conflicting identity at the accepted sequence.
        await DurableTenantAccessHarness.DeliverAsync(host, harness.Persist(new TenantCreated("tenant-corrupt", "Corrupt", null, At), "tenant-corrupt", 1));
        await DurableTenantAccessHarness.DeliverAsync(host, DurableTenantAccessHarness.Envelope(new TenantCreated("tenant-corrupt", "Forged", null, At), "tenant-corrupt", 1));

        // Malformed: a stored record without its accepted identity.
        harness.Store.SeedRaw(
            DurableTenantAccessHarness.StateStoreName,
            DurableTenantProjectionStore.KeyFor("tenant-malformed"),
            new CheckpointedProjectionRecord<TenantLocalState> { Health = CheckpointedProjectionHealth.Healthy, LastContiguousSequence = 3 });

        // Never created: a rejection-only stream.
        await DurableTenantAccessHarness.DeliverAsync(host, harness.Persist(new TenantNotFoundRejection("tenant-rejected"), "tenant-rejected", 1));

        ConversationTenantAccessDecision nonMember = await CheckAsync(host, ConversationTenantAccessRequirement.Read, Tenant, "user-not-a-member");
        Dictionary<string, ConversationTenantAccessDecision> unsafeStates = new(StringComparer.Ordinal)
        {
            ["missing"] = await CheckAsync(host, ConversationTenantAccessRequirement.Read, new TenantId("tenant-missing")),
            ["never-created"] = await CheckAsync(host, ConversationTenantAccessRequirement.Read, new TenantId("tenant-rejected")),
            ["gapped"] = await CheckAsync(host, ConversationTenantAccessRequirement.Read, new TenantId("tenant-gapped")),
            ["corrupt"] = await CheckAsync(host, ConversationTenantAccessRequirement.Read, new TenantId("tenant-corrupt")),
            ["malformed"] = await CheckAsync(host, ConversationTenantAccessRequirement.Read, new TenantId("tenant-malformed")),
            ["unavailable"] = await CheckAsync(outage, ConversationTenantAccessRequirement.Read, Tenant),
            ["stale"] = ConversationTenantAccessDecision.Denied(ConversationTenantAccessRequirement.Read, Tenant, Caller, ConversationTenantAccessDenialReason.TenantAccessStale, isRetryable: true),
            ["rolled-back"] = ConversationTenantAccessDecision.Denied(ConversationTenantAccessRequirement.Read, Tenant, Caller, ConversationTenantAccessDenialReason.TenantAccessRolledBack, isRetryable: true),
        };

        nonMember.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.MissingMember);
        ConversationError nonMemberError = nonMember.ToSafeErrorResult(SchemaVersion.Current, "correlation-durable").Errors.ShouldHaveSingleItem();
        nonMemberError.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        nonMemberError.IsRetryable.ShouldBeFalse();
        nonMember.ToRejection(null, null, null).ReasonCode.ShouldBe("tenant_isolation_violation");
        string expected = PublicShape(nonMember);
        foreach ((string state, ConversationTenantAccessDecision decision) in unsafeStates)
        {
            decision.IsAllowed.ShouldBeFalse(state);
            PublicShape(decision).ShouldBe(expected, state);
            decision.ToSafeErrorResult(SchemaVersion.Current, "correlation-durable").Errors.ShouldHaveSingleItem().IsRetryable.ShouldBeFalse(state);
        }

        // The precise reason stays internal for logs and diagnostics.
        unsafeStates["missing"].DenialReason.ShouldBe(ConversationTenantAccessDenialReason.UnknownTenant);
        unsafeStates["never-created"].DenialReason.ShouldBe(ConversationTenantAccessDenialReason.UnknownTenant);
        unsafeStates["gapped"].DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessGapDetected);
        unsafeStates["corrupt"].DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantProjectionPoisoned);
        unsafeStates["malformed"].DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessStale);
        unsafeStates["unavailable"].DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessUnavailable);
        unsafeStates["unavailable"].IsRetryable.ShouldBeTrue("the internal retry hint is kept for diagnostics");
    }

    /// <summary>A restarted host and a second replica read the same persisted decision without redelivery.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RestartAndSecondReplicaReloadTheSameDecisionWithoutRedelivery()
    {
        DurableTenantAccessHarness harness = new();
        using (ServiceProvider original = harness.CreateHost())
        {
            await DeliverMemberTenantAsync(harness, original);
            (await DurableTenantAccessHarness.DeliverAsync(original, harness.Persist(new TenantDisabled(Tenant.Value, At), Tenant.Value, 3)))
                .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        }

        using ServiceProvider restarted = harness.CreateHost();
        using ServiceProvider replica = harness.CreateHost();
        ConversationTenantAccessDecision afterRestart = await CheckAsync(restarted, ConversationTenantAccessRequirement.Read);
        ConversationTenantAccessDecision onReplica = await CheckAsync(replica, ConversationTenantAccessRequirement.Read);

        afterRestart.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantDisabled);
        onReplica.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantDisabled);
        TenantProjectionSnapshot a = (await restarted.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant.Value, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        TenantProjectionSnapshot b = (await replica.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant.Value, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        a.Checkpoint.ShouldBe(3);
        b.RecordVersion.ShouldBe(a.RecordVersion);
        b.State.ShouldNotBeNull().Status.ShouldBe(TenantStatus.Disabled);
    }

    /// <summary>
    /// Concurrent change between the shared adapters cannot mix records: one snapshot decides each evaluation.
    /// </summary>
    /// <param name="healthyFirst">Whether the first snapshot read is healthy.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExactlyOneCoherentSnapshotDecidesEachEvaluation(bool healthyFirst)
    {
        TenantProjectionSnapshot healthy = new(Tenant.Value, CheckpointedProjectionHealth.Healthy, Member(TenantRole.TenantOwner), 7, 7);
        TenantProjectionSnapshot gapped = new(Tenant.Value, CheckpointedProjectionHealth.Gapped, null, 7, 8);
        SequencedSnapshotStore store = new(healthyFirst ? [healthy, gapped] : [gapped, healthy]);
        ConversationTenantAccessService service = new(
            store,
            new DefaultConversationTenantProjectionSignal(store, NullLogger<DefaultConversationTenantProjectionSignal>.Instance),
            NullLogger<ConversationTenantAccessService>.Instance);

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller,
            cancellationToken: TestContext.Current.CancellationToken);

        store.Reads.ShouldBe(1);
        decision.IsAllowed.ShouldBe(healthyFirst);
        decision.DenialReason.ShouldBe(healthyFirst ? ConversationTenantAccessDenialReason.None : ConversationTenantAccessDenialReason.TenantAccessGapDetected);
        decision.ProjectionVersion.ShouldBe(healthyFirst ? 7 : null);
    }

    /// <summary>With a durable store, the default signal reports persisted health and never synthesizes Healthy.</summary>
    /// <param name="health">The persisted health.</param>
    /// <param name="stale">The expected stale flag.</param>
    /// <param name="gap">The expected gap flag.</param>
    /// <param name="poisoned">The expected poisoning flag.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData(CheckpointedProjectionHealth.Gapped, false, true, false)]
    [InlineData(CheckpointedProjectionHealth.Corrupt, false, false, true)]
    [InlineData(CheckpointedProjectionHealth.Unavailable, true, false, false)]
    [InlineData((CheckpointedProjectionHealth)0, true, false, false)]
    public async Task DefaultSignalReportsPersistedHealthWithoutSynthesizingHealthy(CheckpointedProjectionHealth health, bool stale, bool gap, bool poisoned)
    {
        SequencedSnapshotStore store = new([new TenantProjectionSnapshot(Tenant.Value, health, null, 4, 4)]) { ThrowOnStateRead = false };
        DefaultConversationTenantProjectionSignal signal = new(store, NullLogger<DefaultConversationTenantProjectionSignal>.Instance);

        ConversationTenantProjectionHealth observed = await signal.GetProjectionHealthAsync(Tenant.Value, TestContext.Current.CancellationToken);
        ConversationTenantProjectionHealth missing = await signal.GetProjectionHealthAsync("tenant-unknown", TestContext.Current.CancellationToken);

        observed.ShouldBe(new ConversationTenantProjectionHealth(stale, gap, false, poisoned));
        observed.ShouldNotBe(ConversationTenantProjectionHealth.Healthy);

        // Diagnostics read this signal without a state lookup, so a missing record reports unproven, never current.
        missing.ShouldBe(new ConversationTenantProjectionHealth(IsStale: true));
        missing.Version.ShouldBeNull();
        (await store.GetAsync("tenant-unknown", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    private static TenantLocalState Member(TenantRole role)
        => new()
        {
            TenantId = Tenant.Value,
            Status = TenantStatus.Active,
            Members = { [Caller] = role },
        };

    private static string PublicShape(ConversationTenantAccessDecision decision)
    {
        ConversationErrorResult result = decision.ToSafeErrorResult(SchemaVersion.Current, "correlation-durable");
        ConversationError error = result.Errors.ShouldHaveSingleItem();
        return $"{error.Code}|{error.Category}|{error.IsRetryable}|{decision.ToRejection(null, null, null).ReasonCode}";
    }

    private static async Task<(EventStoreDomainEventEnvelope Created, EventStoreDomainEventEnvelope Member)> DeliverMemberTenantAsync(
        DurableTenantAccessHarness harness,
        ServiceProvider host)
    {
        EventStoreDomainEventEnvelope created = harness.Persist(new TenantCreated(Tenant.Value, "Durable", null, At), Tenant.Value, 1);
        EventStoreDomainEventEnvelope member = harness.Persist(new UserAddedToTenant(Tenant.Value, Caller, TenantRole.TenantContributor), Tenant.Value, 2);
        (await DurableTenantAccessHarness.DeliverAsync(host, created)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantAccessHarness.DeliverAsync(host, member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        return (created, member);
    }

    private static ValueTask<ConversationTenantAccessDecision> CheckAsync(
        ServiceProvider host,
        ConversationTenantAccessRequirement requirement,
        TenantId? tenant = null,
        string caller = Caller)
        => DurableTenantAccessHarness.Access(host).CheckAccessAsync(
            requirement,
            tenant ?? Tenant,
            caller,
            commandTenantId: tenant ?? Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

    private sealed class SequencedSnapshotStore(IReadOnlyList<TenantProjectionSnapshot> snapshots)
        : ITenantProjectionStore, ITenantProjectionSnapshotStore
    {
        public int Reads { get; private set; }

        public Task<TenantProjectionSnapshot?> GetSnapshotAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            TenantProjectionSnapshot snapshot = snapshots[Math.Min(Reads, snapshots.Count - 1)];
            Reads++;
            return Task.FromResult<TenantProjectionSnapshot?>(string.Equals(snapshot.TenantId, tenantId, StringComparison.Ordinal) ? snapshot : null);
        }

        public Task<CheckpointedProjectionOutcome> ApplyAsync(IEventPayload @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public bool ThrowOnStateRead { get; init; } = true;

        public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => ThrowOnStateRead
                ? throw new NotSupportedException("A durable evaluation must never read state outside its snapshot.")
                : Task.FromResult<TenantLocalState?>(null);

        public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class UnavailableReadModelStore : IReadModelStore
    {
        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
            => Task.FromException<ReadModelEntry<TValue>>(new InvalidOperationException("Synthetic state-store outage."));

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
            => Task.FromException(new InvalidOperationException("Synthetic state-store outage."));

        public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
            where TValue : class
            => Task.FromException<bool>(new InvalidOperationException("Synthetic state-store outage."));
    }
}
