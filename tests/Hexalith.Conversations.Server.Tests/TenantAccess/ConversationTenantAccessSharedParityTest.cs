// <copyright file="ConversationTenantAccessSharedParityTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.TenantAccess;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Proves the Conversations facade remains at parity with the promoted shared tenant-access evaluator.
/// </summary>
public sealed class ConversationTenantAccessSharedParityTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private const string Caller = "user-a";

    /// <summary>
    /// Hostile tenant states must produce identical or stricter results through the Conversations facade.
    /// </summary>
    /// <param name="trigger">The hostile trigger state.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("unknown")]
    [InlineData("disabled")]
    [InlineData("insufficient")]
    [InlineData("stale")]
    [InlineData("gap")]
    [InlineData("rollback")]
    [InlineData("poisoned")]
    [InlineData("malformed-projection")]
    [InlineData("unmapped-role")]
    public async Task ConversationFacadeShouldMatchSharedEvaluatorForHostileStates(string trigger)
    {
        TenantLocalState? state = StateFor(trigger);
        ConversationTenantProjectionHealth health = HealthFor(trigger);
        ConversationTenantAccessRequirement requirement = trigger == "insufficient"
            ? ConversationTenantAccessRequirement.Admin
            : ConversationTenantAccessRequirement.Read;

        ConversationTenantAccessService facade = new(
            new StubTenantProjectionStore(state),
            new StubProjectionSignal(health),
            NullLogger<ConversationTenantAccessService>.Instance);

        ConversationTenantAccessDecision facadeDecision = await facade.CheckAccessAsync(
            requirement,
            Tenant,
            Caller,
            commandTenantId: Tenant,
            cancellationToken: CancellationToken.None);

        TenantAccessEvaluation<ConversationTenantAccessRequirement> sharedDecision =
            await TenantAccessEvaluator.EvaluateAsync(
                requirement,
                [Tenant.Value, Tenant.Value],
                Caller,
                new StubTenantAccessStateStore(state),
                new StubSharedProjectionHealth(health),
                static requirement => Enum.IsDefined(requirement),
                static status => Enum.IsDefined((TenantStatus)status),
                static status => (TenantStatus)status == TenantStatus.Active,
                static status => (TenantStatus)status == TenantStatus.Disabled,
                static role => Enum.IsDefined((TenantRole)role),
                static (role, requirement) => ((TenantRole)role) switch
                {
                    TenantRole.TenantReader => requirement == ConversationTenantAccessRequirement.Read,
                    TenantRole.TenantContributor => requirement is ConversationTenantAccessRequirement.Read
                        or ConversationTenantAccessRequirement.Write,
                    TenantRole.TenantOwner => requirement is ConversationTenantAccessRequirement.Read
                        or ConversationTenantAccessRequirement.Write
                        or ConversationTenantAccessRequirement.Admin
                        or ConversationTenantAccessRequirement.Governance,
                    _ => false,
                },
                NullLogger.Instance,
                CancellationToken.None);

        facadeDecision.IsAllowed.ShouldBe(sharedDecision.IsAllowed);
        facadeDecision.IsRetryable.ShouldBe(sharedDecision.IsRetryable);
        facadeDecision.DenialReason.ShouldBe(Map(sharedDecision.DenialKind));
    }

    /// <summary>
    /// A signal that violates its non-null contract must still fail closed (retryable
    /// TenantAccessUnavailable) through the facade, not throw a NullReferenceException.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ConversationFacadeShouldDenyUnavailableWhenSignalReturnsNullHealth()
    {
        ConversationTenantAccessService facade = new(
            new StubTenantProjectionStore(ActiveState()),
            new NullHealthSignal(),
            NullLogger<ConversationTenantAccessService>.Instance);

        ConversationTenantAccessDecision decision = await facade.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            Caller,
            commandTenantId: Tenant,
            cancellationToken: CancellationToken.None);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantAccessUnavailable);
        decision.IsRetryable.ShouldBeTrue();
    }

    private static TenantLocalState? StateFor(string trigger)
        => trigger switch
        {
            "unknown" => null,
            "disabled" => ActiveState(status: TenantStatus.Disabled),
            "insufficient" => ActiveState(role: TenantRole.TenantReader),
            "malformed-projection" => ActiveState(tenantId: "tenant-a:bad"),
            "unmapped-role" => ActiveState(role: (TenantRole)999),
            _ => ActiveState(),
        };

    private static ConversationTenantProjectionHealth HealthFor(string trigger)
        => trigger switch
        {
            "stale" => new ConversationTenantProjectionHealth(IsStale: true),
            "gap" => new ConversationTenantProjectionHealth(HasGap: true),
            "rollback" => new ConversationTenantProjectionHealth(HasRollback: true),
            "poisoned" => new ConversationTenantProjectionHealth(IsPoisoned: true),
            _ => ConversationTenantProjectionHealth.Healthy,
        };

    private static TenantLocalState ActiveState(
        TenantRole role = TenantRole.TenantOwner,
        TenantStatus status = TenantStatus.Active,
        string tenantId = "tenant-a")
        => new()
        {
            TenantId = tenantId,
            Status = status,
            Members = { [Caller] = role },
        };

    private static ConversationTenantAccessDenialReason Map(TenantAccessDenialKind denial)
        => denial switch
        {
            TenantAccessDenialKind.None => ConversationTenantAccessDenialReason.None,
            TenantAccessDenialKind.MissingTenant => ConversationTenantAccessDenialReason.MissingTenant,
            TenantAccessDenialKind.MalformedTenant => ConversationTenantAccessDenialReason.MalformedTenant,
            TenantAccessDenialKind.TenantMismatch => ConversationTenantAccessDenialReason.TenantMismatch,
            TenantAccessDenialKind.MissingCaller => ConversationTenantAccessDenialReason.MissingCaller,
            TenantAccessDenialKind.TenantAccessUnavailable => ConversationTenantAccessDenialReason.TenantAccessUnavailable,
            TenantAccessDenialKind.TenantAccessStale => ConversationTenantAccessDenialReason.TenantAccessStale,
            TenantAccessDenialKind.TenantAccessGapDetected => ConversationTenantAccessDenialReason.TenantAccessGapDetected,
            TenantAccessDenialKind.TenantAccessRolledBack => ConversationTenantAccessDenialReason.TenantAccessRolledBack,
            TenantAccessDenialKind.TenantProjectionPoisoned => ConversationTenantAccessDenialReason.TenantProjectionPoisoned,
            TenantAccessDenialKind.UnknownTenant => ConversationTenantAccessDenialReason.UnknownTenant,
            TenantAccessDenialKind.MalformedProjection => ConversationTenantAccessDenialReason.MalformedProjection,
            TenantAccessDenialKind.UnmappedStatus => ConversationTenantAccessDenialReason.UnmappedStatus,
            TenantAccessDenialKind.TenantDisabled => ConversationTenantAccessDenialReason.TenantDisabled,
            TenantAccessDenialKind.MissingMember => ConversationTenantAccessDenialReason.MissingMember,
            TenantAccessDenialKind.UnmappedRole => ConversationTenantAccessDenialReason.UnmappedRole,
            TenantAccessDenialKind.InsufficientRole => ConversationTenantAccessDenialReason.InsufficientRole,
            _ => ConversationTenantAccessDenialReason.TenantProjectionPoisoned,
        };

    private sealed class NullHealthSignal : IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<ConversationTenantProjectionHealth>(null!);
    }

    private sealed class StubProjectionSignal(ConversationTenantProjectionHealth health) : IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(health);
    }

    private sealed class StubSharedProjectionHealth(ConversationTenantProjectionHealth health) : ITenantAccessProjectionHealthProvider
    {
        public ValueTask<TenantAccessProjectionHealth?> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<TenantAccessProjectionHealth?>(new TenantAccessProjectionHealth(
                health.Version,
                health.Watermark,
                health.IsStale,
                health.HasGap,
                health.HasRollback,
                health.IsPoisoned));
    }

    private sealed class StubTenantProjectionStore(TenantLocalState? state) : ITenantProjectionStore
    {
        public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(state);

        public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubTenantAccessStateStore(TenantLocalState? state) : ITenantAccessStateStore
    {
        public Task<TenantAccessState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(state is null
                ? null
                : new TenantAccessState(
                    state.TenantId,
                    (int)state.Status,
                    state.Members.ToDictionary(
                        static pair => pair.Key,
                        static pair => (int)pair.Value,
                        StringComparer.Ordinal)));
    }
}
