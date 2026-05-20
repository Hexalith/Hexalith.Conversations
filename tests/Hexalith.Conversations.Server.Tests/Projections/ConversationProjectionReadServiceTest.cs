// <copyright file="ConversationProjectionReadServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Verifies projection reads fail closed through tenant access and freshness evaluation.
/// </summary>
public sealed class ConversationProjectionReadServiceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Tenant denial returns hidden semantics and does not touch projection state.
    /// </summary>
    [Fact]
    public async Task DeniedTenantAccessShouldReturnForbiddenWithoutReadingProjection()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "user-001",
            ConversationTenantAccessDenialReason.MissingMember));
        FakeProjectionReadStore store = new();
        ConversationProjectionReadService service = new(access, store);

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, Conversation, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Projection.ShouldBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
        store.Reads.ShouldBe(0);
    }

    /// <summary>
    /// Missing records use the same hidden result shape as denied reads.
    /// </summary>
    [Fact]
    public async Task MissingProjectionShouldNotDiscloseExistence()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "user-001"));
        FakeProjectionReadStore store = new();
        ConversationProjectionReadService service = new(access, store);

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, Conversation, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.Projection.ShouldBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
        store.Reads.ShouldBe(1);
    }

    /// <summary>
    /// Projection store failures degrade to unavailable without returning a partial projection.
    /// </summary>
    [Fact]
    public async Task ProjectionStoreFailureShouldReturnUnavailable()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "user-001"));
        FakeProjectionReadStore store = new() { ThrowOnRead = true };
        ConversationProjectionReadService service = new(access, store);

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, Conversation, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Projection.ShouldBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
    }

    /// <summary>
    /// Mixed summary/detail generations cannot be reported as current.
    /// </summary>
    [Fact]
    public async Task MixedGenerationSummaryAndDetailShouldDowngradeTrust()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "user-001"));
        ConversationProjectedReadModels models = ProjectedModels();
        FakeProjectionReadStore store = new()
        {
            Models = new ConversationProjectedReadModels(
                models.Summary,
                DetailWithFreshness(models.Detail, FreshnessAtPosition(2))),
        };
        ConversationProjectionReadService service = new(access, store);

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, Conversation, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Projection.ShouldBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
    }

    /// <summary>
    /// Only server-current projections enable trust-bearing command availability.
    /// </summary>
    [Fact]
    public async Task CurrentProjectionShouldEnableTrustBearingActions()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "user-001"));
        FakeProjectionReadStore store = new() { Models = ProjectedModels() };
        ConversationProjectionReadService service = new(access, store);

        ConversationProjectionReadResult result = await service.ReadDetailAsync(Tenant, "user-001", Tenant, Conversation, TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Projection.ShouldNotBeNull();
        result.IsAvailableForTrustBearingActions.ShouldBeTrue();
    }

    private static ConversationProjectedReadModels ProjectedModels()
        => new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [
                new ConversationProjectionEventRecord(1, new ConversationCreated(
                    new ConversationEventMetadata(
                        SchemaVersion.Current,
                        "event-create-001",
                        ConversationEventType.ConversationCreated,
                        Tenant,
                        Conversation,
                        "correlation-001",
                        Now,
                        Actor,
                        "causation-001"),
                    Label: "Case 123")),
            ],
            Now.AddSeconds(1),
            TimeSpan.FromMinutes(5));

    private static ProjectionFreshnessV1 FreshnessAtPosition(long position)
        => new(
            SchemaVersion.Current,
            $"pos:{position:D10}",
            position,
            Now,
            Now.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

    private static ConversationDetailProjectionV1 DetailWithFreshness(
        ConversationDetailProjectionV1 detail,
        ProjectionFreshnessV1 freshness)
        => new(
            detail.SchemaVersion,
            detail.TenantId,
            detail.ConversationId,
            freshness,
            detail.LifecycleState,
            detail.Label,
            detail.BusinessReference,
            detail.ProjectId,
            detail.FolderId,
            detail.ProviderCorrelation,
            detail.Participants,
            detail.Messages,
            detail.FileReferences,
            detail.Attributes);

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

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public ConversationProjectedReadModels? Models { get; set; }

        public int Reads { get; private set; }

        public bool ThrowOnRead { get; set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            Reads++;
            if (ThrowOnRead)
            {
                throw new InvalidOperationException("Projection unavailable.");
            }

            return ValueTask.FromResult(Models);
        }

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<ConversationSummaryProjectionV1>>([]);
    }
}
