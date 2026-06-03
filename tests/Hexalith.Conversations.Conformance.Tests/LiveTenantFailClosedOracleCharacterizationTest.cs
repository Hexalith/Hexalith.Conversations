// <copyright file="LiveTenantFailClosedOracleCharacterizationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.Extensions.Logging;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.2 oracle-strengthening backfill — behavior #1/#3 (tenant fail-closed, NFR3).
///
/// The 14 conformance suites assert the scenario-engine mirror of tenant isolation
/// (<see cref="TenantIsolationConformanceSuiteTest"/> drives synthetic outcomes). They do NOT
/// exercise the live <see cref="ConversationTenantAccessService"/> / <see cref="ConversationTenantAccessGuard"/>
/// decision code, so a fail-open mutation of the live guard rides green through the oracle.
///
/// This adversarial characterization test runs the LIVE decision code from inside the conformance
/// project (the oracle), pinning current fail-closed behavior across every release-gate trigger state —
/// missing / unknown / disabled / stale / ambiguous / insufficient / unavailable — plus cross-tenant
/// denial. It is the AC3 fail-open catch: flipping a deny branch in the live service to fail-open turns
/// at least one assertion here RED. See docs/release-evidence/oracle-blind-spot-analysis-v1.md for the
/// recorded fault-injection experiment.
/// </summary>
public sealed class LiveTenantFailClosedOracleCharacterizationTest
{
    private const string Caller = "party-owner";

    private static readonly TenantId TenantA = new("tenant-a");
    private static readonly TenantId TenantB = new("tenant-b");

    /// <summary>
    /// Gets the seven release-gate fail-closed trigger states (project-context cover list), each
    /// asserted to DENY against the live decision code. A fail-open mutation breaks at least one row.
    /// </summary>
    public static TheoryData<string, ConversationTenantAccessDenialReason> FailClosedTriggerStates() => new()
    {
        // unknown: the local projection has no record for the resolved tenant.
        { "unknown", ConversationTenantAccessDenialReason.UnknownTenant },

        // disabled: the tenant exists but is not active.
        { "disabled", ConversationTenantAccessDenialReason.TenantDisabled },

        // stale: the projection signal reports lagging/stale state.
        { "stale", ConversationTenantAccessDenialReason.TenantAccessStale },

        // ambiguous: the projection signal reports poisoned/contradictory state.
        { "ambiguous", ConversationTenantAccessDenialReason.TenantProjectionPoisoned },

        // insufficient: the caller is a member but lacks the requested permission.
        { "insufficient", ConversationTenantAccessDenialReason.InsufficientRole },

        // unavailable: the projection store is unreachable (throws).
        { "unavailable", ConversationTenantAccessDenialReason.TenantAccessUnavailable },

        // --- Story 1.2 QA gap-fill: additional live deny branches the oracle had not pinned. ---
        // Each is a release-gate fail-closed concern per project-context (out-of-order detection,
        // closed-world role/status mapping, projection-shape integrity). All pin CURRENT behavior.

        // gap: the projection signal reports a sequence gap (Dapr at-least-once / out-of-order events).
        { "gap", ConversationTenantAccessDenialReason.TenantAccessGapDetected },

        // rollback: the projection signal reports a watermark regression / rollback.
        { "rollback", ConversationTenantAccessDenialReason.TenantAccessRolledBack },

        // unmapped-role: the caller's own member role is outside the closed-world TenantRole set.
        { "unmapped-role", ConversationTenantAccessDenialReason.UnmappedRole },

        // unmapped-status: the tenant status is the non-active Unknown sentinel (missing-status fails closed).
        { "unmapped-status", ConversationTenantAccessDenialReason.UnmappedStatus },

        // malformed-projection: the stored projection record's own tenant id is non-canonical.
        { "malformed-projection", ConversationTenantAccessDenialReason.MalformedProjection },

        // member-poisoned: a member key carries trim drift — a broadened/poisoned membership map must not widen access.
        { "member-poisoned", ConversationTenantAccessDenialReason.TenantProjectionPoisoned },
    };

    /// <summary>
    /// The live tenant access service denies every release-gate fail-closed trigger state and surfaces the
    /// current internal denial reason. Pins NFR3 fail-closed behavior inside the oracle.
    /// </summary>
    /// <param name="trigger">The fail-closed trigger state name.</param>
    /// <param name="expectedReason">The current internal denial reason for that state.</param>
    /// <returns>A task.</returns>
    [Theory]
    [MemberData(nameof(FailClosedTriggerStates))]
    public async Task LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(
        string trigger,
        ConversationTenantAccessDenialReason expectedReason)
    {
        ConversationTenantAccessService service = ServiceForTrigger(trigger);

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            RequirementForTrigger(trigger),
            TenantA,
            Caller,
            commandTenantId: TenantA,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse($"Trigger '{trigger}' must fail closed against the live decision code.");
        decision.DenialReason.ShouldBe(expectedReason);
    }

    /// <summary>
    /// The "missing" fail-closed state: no tenant binding is present at all, so the live service denies as
    /// MissingTenant before consulting any projection. Exercised as a dedicated case because the shared
    /// trigger theory always supplies a binding.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveServiceShouldFailClosedWhenTenantBindingIsMissing()
    {
        ConversationTenantAccessService service = Service(new StubStore(OwnerState()));

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            trustedTenantId: null,
            Caller,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.MissingTenant);
    }

    /// <summary>
    /// Story 1.2 QA gap-fill: a non-canonical tenant id (embeds a reserved delimiter that hints at upstream
    /// namespace prefixing) is rejected as MalformedTenant before any projection lookup. A relaxed
    /// canonicalization mutation that accepted prefixed ids would turn this RED.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveServiceShouldFailClosedWhenTenantIdIsMalformed()
    {
        ConversationTenantAccessService service = Service(new StubStore(OwnerState()));
        TenantId malformed = new("tenant-a:rogue");

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            malformed,
            Caller,
            commandTenantId: malformed,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.MalformedTenant);
    }

    /// <summary>
    /// Story 1.2 QA gap-fill: a caller principal id with trim drift is rejected as MissingCaller (defense in
    /// depth — the boundary contract requires a canonical principal). A normalization mutation that silently
    /// trimmed the caller would turn this RED.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveServiceShouldFailClosedWhenCallerPrincipalIsMalformed()
    {
        ConversationTenantAccessService service = Service(new StubStore(OwnerState()));

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            TenantA,
            " " + Caller,
            commandTenantId: TenantA,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.MissingCaller);
    }

    /// <summary>
    /// A caller authorized for tenant-a is denied when the request is bound to tenant-b whose projection
    /// does not list the caller. Cross-tenant access is impossible by construction (live decision code).
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveServiceShouldDenyCrossTenantMemberLeakage()
    {
        // Caller is owner of tenant-a, but the resolved/trusted tenant is tenant-b; tenant-b's projection
        // lists only a different member. Membership check denies the caller — no cross-tenant leak.
        TenantLocalState tenantBState = new()
        {
            TenantId = "tenant-b",
            Status = TenantStatus.Active,
            Members = { ["party-other"] = TenantRole.TenantOwner },
        };
        ConversationTenantAccessService service = Service(new StubStore(tenantBState));

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            TenantB,
            Caller,
            commandTenantId: TenantB,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.MissingMember);
    }

    /// <summary>
    /// Contradictory tenant bindings (route tenant-a vs command tenant-b) deny as a mismatch before any
    /// projection lookup. A fail-open mutation that accepts conflicting bindings turns this RED.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveServiceShouldDenyContradictoryTenantBindings()
    {
        ConversationTenantAccessService service = Service(new StubStore(OwnerState()));

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read,
            TenantA,
            Caller,
            routeTenantId: TenantA,
            commandTenantId: TenantB,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeFalse();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.TenantMismatch);
    }

    /// <summary>
    /// The live guard must NOT invoke the protected operation when the live service denies access.
    /// This is the end-to-end fail-open catch: a denied decision must short-circuit downstream work.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveGuardShouldNotRunProtectedOperationWhenLiveServiceDenies()
    {
        // unknown tenant -> live service denies -> guard must not invoke the protected operation.
        ConversationTenantAccessService service = Service(new StubStore(state: null));
        int protectedInvocations = 0;

        bool ran = await ConversationTenantAccessGuard.RunAsync(
            service,
            ConversationTenantAccessRequirement.Write,
            TenantA,
            Caller,
            deniedResult: _ => false,
            protectedOperation: _ =>
            {
                protectedInvocations++;
                return ValueTask.FromResult(true);
            },
            commandTenantId: TenantA,
            cancellationToken: TestContext.Current.CancellationToken);

        ran.ShouldBeFalse();
        protectedInvocations.ShouldBe(0);
    }

    /// <summary>
    /// Positive control: a valid, authorized owner is allowed. Without this, a degenerate "deny everything"
    /// mutation would pass every negative assertion above — this pins that allow still works on main.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveServiceShouldAllowAuthorizedOwner()
    {
        ConversationTenantAccessService service = Service(new StubStore(OwnerState()));

        ConversationTenantAccessDecision decision = await service.CheckAccessAsync(
            ConversationTenantAccessRequirement.Governance,
            TenantA,
            Caller,
            commandTenantId: TenantA,
            cancellationToken: TestContext.Current.CancellationToken);

        decision.IsAllowed.ShouldBeTrue();
        decision.DenialReason.ShouldBe(ConversationTenantAccessDenialReason.None);
    }

    private static ConversationTenantAccessRequirement RequirementForTrigger(string trigger)
        => trigger == "insufficient"
            ? ConversationTenantAccessRequirement.Governance
            : ConversationTenantAccessRequirement.Read;

    private static ConversationTenantAccessService ServiceForTrigger(string trigger) => trigger switch
    {
        "unknown" => Service(new StubStore(state: null)),
        "disabled" => Service(new StubStore(StateWith(TenantRole.TenantOwner, TenantStatus.Disabled))),
        "stale" => Service(
            new StubStore(OwnerState()),
            new StubSignal(new ConversationTenantProjectionHealth(IsStale: true))),
        "ambiguous" => Service(
            new StubStore(OwnerState()),
            new StubSignal(new ConversationTenantProjectionHealth(IsPoisoned: true))),

        // Reader role requesting Governance -> InsufficientRole.
        "insufficient" => Service(new StubStore(StateWith(TenantRole.TenantReader, TenantStatus.Active))),
        "unavailable" => Service(new StubStore(state: null, throwOnGet: true)),

        // Story 1.2 QA gap-fill triggers.
        "gap" => Service(
            new StubStore(OwnerState()),
            new StubSignal(new ConversationTenantProjectionHealth(HasGap: true))),
        "rollback" => Service(
            new StubStore(OwnerState()),
            new StubSignal(new ConversationTenantProjectionHealth(HasRollback: true))),

        // Caller's own member role cast outside the closed-world TenantRole set.
        "unmapped-role" => Service(new StubStore(StateWith((TenantRole)999, TenantStatus.Active))),

        // Unknown (ordinal 0) is a defined-but-non-active status -> UnmappedStatus.
        "unmapped-status" => Service(new StubStore(StateWith(TenantRole.TenantOwner, TenantStatus.Unknown))),

        // Projection record whose own tenant id embeds a reserved delimiter (non-canonical).
        "malformed-projection" => Service(new StubStore(new TenantLocalState
        {
            TenantId = "tenant-a:rogue",
            Status = TenantStatus.Active,
            Members = { [Caller] = TenantRole.TenantOwner },
        })),

        // Member key with leading whitespace: a poisoned/non-Ordinal membership map must fail closed.
        "member-poisoned" => Service(new StubStore(new TenantLocalState
        {
            TenantId = "tenant-a",
            Status = TenantStatus.Active,
            Members = { [" " + Caller] = TenantRole.TenantOwner },
        })),

        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "Unknown trigger state."),
    };

    private static ConversationTenantAccessService Service(
        ITenantProjectionStore store,
        IConversationTenantProjectionSignal? signal = null)
        => new(store, signal ?? new HealthySignal(), NoOpLogger());

    private static ILogger<ConversationTenantAccessService> NoOpLogger()
        => new SilentLogger<ConversationTenantAccessService>();

    private static TenantLocalState OwnerState()
        => StateWith(TenantRole.TenantOwner, TenantStatus.Active);

    private static TenantLocalState StateWith(TenantRole role, TenantStatus status)
        => new()
        {
            TenantId = "tenant-a",
            Status = status,
            Members = { [Caller] = role },
        };

    private sealed class StubStore(TenantLocalState? state, bool throwOnGet = false) : ITenantProjectionStore
    {
        public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => throwOnGet
                ? throw new InvalidOperationException("Tenant projection store is unavailable.")
                : Task.FromResult(state);

        public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class HealthySignal : IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationTenantProjectionHealth.Healthy);
    }

    private sealed class StubSignal(ConversationTenantProjectionHealth health) : IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(health);
    }

    private sealed class SilentLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}
