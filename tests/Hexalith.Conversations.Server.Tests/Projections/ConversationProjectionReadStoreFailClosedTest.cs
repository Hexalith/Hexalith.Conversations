// <copyright file="ConversationProjectionReadStoreFailClosedTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Fakes;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Story 2.4 (AC-4) — re-expresses the fail-closed read boundary against the production
/// <see cref="ConversationProjectionReadStore"/> over the SDK <see cref="IReadModelStore"/> (rather than the
/// in-memory <c>FakeProjectionReadStore</c>): a missing key, a backend throw, a tenant/conversation mismatch,
/// and a mixed-generation pair all surface the same non-disclosing shapes as before. These assertions are not
/// weakened — they prove the existing safe shapes hold through the real persistence path.
/// </summary>
public sealed class ConversationProjectionReadStoreFailClosedTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId ConversationA = new("conversation-a");
    private static readonly ConversationId ConversationB = new("conversation-b");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// An absent key reads as <see langword="null"/> and surfaces the same hidden (Forbidden) shape.
    /// </summary>
    [Fact]
    public async Task MissingKeyShouldReturnForbiddenShape()
    {
        ConversationProjectionReadService service = new(AllowAll(), new ConversationProjectionReadStore(new InMemoryReadModelStore()));

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, ConversationA, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Projection.ShouldBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
    }

    /// <summary>
    /// A backend store throw degrades to Unavailable without leaking the raw error.
    /// </summary>
    [Fact]
    public async Task StoreFailureShouldReturnUnavailable()
    {
        ConversationProjectionReadService service = new(AllowAll(), new ConversationProjectionReadStore(new ThrowingReadModelStore()));

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, ConversationA, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Projection.ShouldBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
    }

    /// <summary>
    /// A persisted model whose identity disagrees with the requested key surfaces the PoisonEvent shape.
    /// </summary>
    [Fact]
    public async Task TenantConversationMismatchShouldReturnPoisonEvent()
    {
        InMemoryReadModelStore store = new();

        // Seed a model for conversation B under conversation A's key — an identity mismatch at read time.
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, ConversationA),
            Models(ConversationB, Now.AddSeconds(1)));

        ConversationProjectionReadService service = new(AllowAll(), new ConversationProjectionReadStore(store));

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, ConversationA, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.PoisonEvent);
        result.Projection.ShouldBeNull();
    }

    /// <summary>
    /// A summary/detail pair from different materialization generations surfaces the Rebuilding shape.
    /// </summary>
    [Fact]
    public async Task MixedGenerationShouldReturnRebuilding()
    {
        InMemoryReadModelStore store = new();

        ConversationProjectedReadModels first = Models(ConversationA, Now.AddSeconds(1));
        ConversationProjectedReadModels later = Models(ConversationA, Now.AddSeconds(99));
        ConversationProjectedReadModels mixed = new(first.Summary, later.Detail);
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, ConversationA),
            mixed);

        ConversationProjectionReadService service = new(AllowAll(), new ConversationProjectionReadStore(store));

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, ConversationA, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MixedGeneration);
        result.Projection.ShouldBeNull();
    }

    /// <summary>
    /// A current persisted pair read through the real store path enables trust-bearing detail.
    /// </summary>
    [Fact]
    public async Task CurrentPersistedModelShouldEnableTrustBearingDetail()
    {
        InMemoryReadModelStore store = new();
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, ConversationA),
            Models(ConversationA, Now.AddSeconds(1)));

        ConversationProjectionReadService service = new(AllowAll(), new ConversationProjectionReadStore(store));

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, ConversationA, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Projection.ShouldNotBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeTrue();
    }

    private static FakeTenantAccessService AllowAll()
        => new(ConversationTenantAccessDecision.Allowed(ConversationTenantAccessRequirement.Read, Tenant, "user-001"));

    private static ConversationProjectedReadModels Models(ConversationId conversationId, DateTimeOffset generatedAt)
        => new ConversationProjectionMaterializer().Project(
            Tenant,
            conversationId,
            [
                new ConversationProjectionEventRecord(1, new ConversationCreated(
                    new ConversationEventMetadata(
                        SchemaVersion.Current,
                        $"event-create-{conversationId.Value}",
                        ConversationEventType.ConversationCreated,
                        Tenant,
                        conversationId,
                        $"correlation-{conversationId.Value}",
                        Now,
                        Actor,
                        $"causation-{conversationId.Value}"),
                    Label: $"Case {conversationId.Value}")),
            ],
            generatedAt,
            TimeSpan.FromMinutes(5));

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(decision);
    }

    private sealed class ThrowingReadModelStore : IReadModelStore
    {
        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
            => throw new InvalidOperationException("raw projection backend detail");

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
            => throw new InvalidOperationException("raw projection backend detail");

        public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
            where TValue : class
            => throw new InvalidOperationException("raw projection backend detail");
    }
}
