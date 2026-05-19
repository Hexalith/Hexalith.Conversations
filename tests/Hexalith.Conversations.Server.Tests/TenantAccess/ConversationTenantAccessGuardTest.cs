// <copyright file="ConversationTenantAccessGuardTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Verifies guarded command/read delegates cannot bypass tenant access checks.
/// </summary>
public sealed class ConversationTenantAccessGuardTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private const string Caller = "user-1";

    /// <summary>
    /// Denied writes do not load aggregate state, dispatch commands, emit events, mutate projections, or publish metadata.
    /// </summary>
    [Fact]
    public async Task RunAsyncShouldNotInvokeWriteDelegatesWhenAccessIsDenied()
    {
        ProtectedWriteSpy spy = new();
        ConversationTenantAccessDecision denial = ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            Caller,
            ConversationTenantAccessDenialReason.MissingMember);

        DomainResult result = await ConversationTenantAccessGuard.RunAsync(
            new StubTenantAccessService(denial),
            ConversationTenantAccessRequirement.Write,
            Tenant,
            Caller,
            deniedResult: decision => DomainResult.Rejection(new IRejectionEvent[]
            {
                decision.ToRejection(SchemaVersion.Current, "correlation-safe", "causation-safe"),
            }),
            protectedOperation: _ =>
            {
                spy.LoadAggregate();
                spy.DispatchCommand();
                spy.AppendEvent();
                spy.MutateProjection();
                spy.PublishMetadata();
                return ValueTask.FromResult(DomainResult.Success(Array.Empty<IEventPayload>()));
            },
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        spy.AggregateLoadCount.ShouldBe(0);
        spy.CommandDispatchCount.ShouldBe(0);
        spy.EventAppendCount.ShouldBe(0);
        spy.ProjectionMutationCount.ShouldBe(0);
        spy.PublicationMetadataCount.ShouldBe(0);
    }

    /// <summary>
    /// Denied reads do not call projection lookup, totals, pagination, Party hydration, provider metadata, or existence branches.
    /// </summary>
    [Fact]
    public async Task RunAsyncShouldNotInvokeReadDelegatesWhenAccessIsDenied()
    {
        ProtectedReadSpy spy = new();
        ConversationTenantAccessDecision denial = ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller,
            ConversationTenantAccessDenialReason.TenantAccessStale,
            isRetryable: true);

        ReadBoundaryResult result = await ConversationTenantAccessGuard.RunAsync(
            new StubTenantAccessService(denial),
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller,
            deniedResult: decision =>
            {
                ConversationErrorResult error = decision.ToSafeErrorResult(SchemaVersion.Current, "correlation-safe");
                return new ReadBoundaryResult(IsDenied: true, ErrorCode: error.Errors.Single().Code);
            },
            protectedOperation: _ =>
            {
                spy.LookupProjection();
                spy.CalculateTotals();
                spy.ResolvePagination();
                spy.HydrateParties();
                spy.LookupProviderMetadata();
                spy.BranchOnExistence();
                return ValueTask.FromResult(new ReadBoundaryResult(IsDenied: false));
            },
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsDenied.ShouldBeTrue();
        result.ErrorCode.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        spy.ProjectionLookupCount.ShouldBe(0);
        spy.TotalCalculationCount.ShouldBe(0);
        spy.PaginationResolutionCount.ShouldBe(0);
        spy.PartyHydrationCount.ShouldBe(0);
        spy.ProviderMetadataCount.ShouldBe(0);
        spy.ExistenceBranchCount.ShouldBe(0);
    }

    /// <summary>
    /// Allowed access invokes the protected delegate exactly once.
    /// </summary>
    [Fact]
    public async Task RunAsyncShouldInvokeProtectedDelegateWhenAccessIsAllowed()
    {
        int callCount = 0;
        ConversationTenantAccessDecision allowed = ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller);

        ReadBoundaryResult result = await ConversationTenantAccessGuard.RunAsync(
            new StubTenantAccessService(allowed),
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller,
            deniedResult: _ => new ReadBoundaryResult(IsDenied: true),
            protectedOperation: _ =>
            {
                callCount++;
                return ValueTask.FromResult(new ReadBoundaryResult(IsDenied: false));
            },
            commandTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsDenied.ShouldBeFalse();
        callCount.ShouldBe(1);
    }

    private sealed record ReadBoundaryResult(bool IsDenied, ConversationErrorCode? ErrorCode = null);

    private sealed class StubTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
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

    private sealed class ProtectedWriteSpy
    {
        public int AggregateLoadCount { get; private set; }

        public int CommandDispatchCount { get; private set; }

        public int EventAppendCount { get; private set; }

        public int ProjectionMutationCount { get; private set; }

        public int PublicationMetadataCount { get; private set; }

        public void LoadAggregate() => AggregateLoadCount++;

        public void DispatchCommand() => CommandDispatchCount++;

        public void AppendEvent() => EventAppendCount++;

        public void MutateProjection() => ProjectionMutationCount++;

        public void PublishMetadata() => PublicationMetadataCount++;
    }

    private sealed class ProtectedReadSpy
    {
        public int ProjectionLookupCount { get; private set; }

        public int TotalCalculationCount { get; private set; }

        public int PaginationResolutionCount { get; private set; }

        public int PartyHydrationCount { get; private set; }

        public int ProviderMetadataCount { get; private set; }

        public int ExistenceBranchCount { get; private set; }

        public void LookupProjection() => ProjectionLookupCount++;

        public void CalculateTotals() => TotalCalculationCount++;

        public void ResolvePagination() => PaginationResolutionCount++;

        public void HydrateParties() => PartyHydrationCount++;

        public void LookupProviderMetadata() => ProviderMetadataCount++;

        public void BranchOnExistence() => ExistenceBranchCount++;
    }
}
