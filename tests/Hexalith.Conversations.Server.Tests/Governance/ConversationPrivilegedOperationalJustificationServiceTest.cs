// <copyright file="ConversationPrivilegedOperationalJustificationServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Tests.Projections;

namespace Hexalith.Conversations.Server.Tests.Governance;

/// <summary>
/// Verifies the privileged operational justification precondition boundary.
/// </summary>
public sealed class ConversationPrivilegedOperationalJustificationServiceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private const string CallerPrincipalId = "caller-001";
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApprovedPrivilegedActionShouldAuditBeforeExecutingDelegate()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Admin,
            Tenant,
            "caller-001"));
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            access,
            new FakeProjectionReadStore { Models = Models(Tenant) },
            audit,
            new FakeTimeProvider(Now));
        int delegateCalls = 0;

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Read),
            (_, _) =>
            {
                delegateCalls++;
                return ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded());
            },
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(GovernanceOutcome.Succeeded);
        result.Details.ShouldNotBeNull();
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-privileged-001");
        access.LastRequirement.ShouldBe(ConversationTenantAccessRequirement.Admin);
        audit.RecordCalls.ShouldBe(1);
        audit.LastOperationKind.ShouldBe(GovernanceOperationKind.RecordPrivilegedJustification);
        access.LastCallerPrincipalId.ShouldBe(CallerPrincipalId);
        delegateCalls.ShouldBe(1);
    }

    [Fact]
    public async Task MissingOrMalformedJustificationShouldFailClosedBeforeProjectionAuditOrDelegate()
    {
        FakeProjectionReadStore store = new() { Models = Models(Tenant) };
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Admin,
                Tenant,
                "caller-001")),
            store,
            audit);
        int delegateCalls = 0;

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            null!,
            (_, _) =>
            {
                delegateCalls++;
                return ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded());
            },
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(GovernanceOutcome.Denied);
        result.Details.ShouldBeNull();
        store.DetailReads.ShouldBe(0);
        audit.RecordCalls.ShouldBe(0);
        delegateCalls.ShouldBe(0);
    }

    [Theory]
    [InlineData("stale")]
    [InlineData("future")]
    public async Task StaleOrFutureJustificationShouldFailClosedBeforeAuthorizationProjectionAuditOrDelegate(string clockState)
    {
        DateTimeOffset timestamp = clockState == "stale"
            ? Now.AddHours(-25)
            : Now.AddMinutes(6);
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Admin,
            Tenant,
            "caller-001"));
        FakeProjectionReadStore store = new() { Models = Models(Tenant) };
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            access,
            store,
            audit,
            new FakeTimeProvider(Now));
        int delegateCalls = 0;

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Read, timestamp),
            (_, _) =>
            {
                delegateCalls++;
                return ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded());
            },
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(GovernanceOutcome.Denied);
        result.Details.ShouldBeNull();
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
        audit.RecordCalls.ShouldBe(0);
        delegateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task UnauthorizedOperatorShouldNotReadProjectionOrAuditOrExecute()
    {
        FakeProjectionReadStore store = new() { Models = Models(Tenant) };
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Denied(
                ConversationTenantAccessRequirement.Admin,
                Tenant,
                "caller-001",
                ConversationTenantAccessDenialReason.MissingMember)),
            store,
            audit,
            new FakeTimeProvider(Now));
        int delegateCalls = 0;

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Export),
            (_, _) =>
            {
                delegateCalls++;
                return ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded());
            },
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Forbidden);
        store.DetailReads.ShouldBe(0);
        audit.RecordCalls.ShouldBe(0);
        delegateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task GovernanceChangingOperationShouldRequireGovernanceAccess()
    {
        FakeTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "caller-001"));
        ConversationPrivilegedOperationalJustificationService service = new(
            access,
            new FakeProjectionReadStore { Models = Models(Tenant) },
            new FakeGovernanceAuditService(),
            new FakeTimeProvider(Now));

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.VisibilityChange),
            static (_, _) => ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded()),
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(GovernanceOutcome.Succeeded);
        access.LastRequirement.ShouldBe(ConversationTenantAccessRequirement.Governance);
    }

    [Theory]
    [InlineData("stale")]
    [InlineData("rebuilding")]
    public async Task NonCurrentProjectionShouldBlockPrivilegedDecision(string state)
    {
        ProjectionTrustState trustState = state == "stale" ? ProjectionTrustState.Stale : ProjectionTrustState.Rebuilding;
        ProjectionFreshnessReasonCode reason = state == "stale"
            ? ProjectionFreshnessReasonCode.StaleThresholdExceeded
            : ProjectionFreshnessReasonCode.Rebuilding;
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Admin,
                Tenant,
                "caller-001")),
            new FakeProjectionReadStore { Models = Models(Tenant, trustState, reason) },
            audit,
            new FakeTimeProvider(Now));
        int delegateCalls = 0;

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Verify),
            (_, _) =>
            {
                delegateCalls++;
                return ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded());
            },
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(trustState);
        result.ReasonCode.ShouldBe(reason);
        audit.RecordCalls.ShouldBe(0);
        delegateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task CrossTenantProjectionShouldFailClosedWithoutAuditingOrExecuting()
    {
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Admin,
                Tenant,
                "caller-001")),
            new FakeProjectionReadStore { Models = Models(OtherTenant) },
            audit,
            new FakeTimeProvider(Now));
        int delegateCalls = 0;

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Read),
            (_, _) =>
            {
                delegateCalls++;
                return ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded());
            },
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.VisibilityState.ShouldBe(ProjectionTrustState.Forbidden);
        audit.RecordCalls.ShouldBe(0);
        delegateCalls.ShouldBe(0);
    }

    [Theory]
    [InlineData(ConversationGovernanceAuditStatus.AuditUnavailable)]
    [InlineData(ConversationGovernanceAuditStatus.UnsafeEvidence)]
    [InlineData(ConversationGovernanceAuditStatus.Uncertain)]
    [InlineData(ConversationGovernanceAuditStatus.PolicyBlocked)]
    public async Task UnsafeAuditPreconditionShouldFailClosedBeforeDelegate(ConversationGovernanceAuditStatus auditStatus)
    {
        FakeGovernanceAuditService audit = new() { NextStatus = auditStatus };
        ConversationPrivilegedOperationalJustificationService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Admin,
                Tenant,
                "caller-001")),
            new FakeProjectionReadStore { Models = Models(Tenant) },
            audit,
            new FakeTimeProvider(Now));
        int delegateCalls = 0;

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Repair),
            (_, _) =>
            {
                delegateCalls++;
                return ValueTask.FromResult(PrivilegedOperationalActionOutcome.Succeeded());
            },
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(auditStatus == ConversationGovernanceAuditStatus.PolicyBlocked
            ? GovernanceOutcome.PolicyBlocked
            : GovernanceOutcome.AuditUnavailableFailed);
        delegateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task PartialOperationFailureShouldRemainAuditLinkedAndContentSafe()
    {
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Admin,
                Tenant,
                "caller-001")),
            new FakeProjectionReadStore { Models = Models(Tenant) },
            audit,
            new FakeTimeProvider(Now));

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Rebuild),
            static (_, _) => ValueTask.FromResult(PrivilegedOperationalActionOutcome.Partial("Retry after privileged operation is reconciled.")),
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(GovernanceOutcome.PolicyBlocked);
        result.Details.ShouldNotBeNull();
        result.Details.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-privileged-001");
        audit.RecordCalls.ShouldBe(2);
        audit.RecordedOutcomes.ShouldBe(
        [
            GovernanceOutcome.Succeeded,
            GovernanceOutcome.PolicyBlocked,
        ]);
        result.SafeNextAction.ShouldNotContain("exception", Case.Insensitive);
        result.SafeNextAction.ShouldNotContain("storage", Case.Insensitive);
    }

    [Fact]
    public async Task ThrowingPrivilegedActionShouldReturnSafeAuditedFailure()
    {
        FakeGovernanceAuditService audit = new();
        ConversationPrivilegedOperationalJustificationService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Admin,
                Tenant,
                "caller-001")),
            new FakeProjectionReadStore { Models = Models(Tenant) },
            audit,
            new FakeTimeProvider(Now));

        PrivilegedOperationalJustificationResult result = await service.ExecuteAsync(
            Command(PrivilegedOperationalActionClass.Repair),
            static (_, _) => throw new InvalidOperationException("raw storage exception with token"),
            CallerPrincipalId,
            TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(GovernanceOutcome.PolicyBlocked);
        result.Details.ShouldNotBeNull();
        result.SafeNextAction.ShouldNotContain("exception", Case.Insensitive);
        result.SafeNextAction.ShouldNotContain("storage", Case.Insensitive);
        result.SafeNextAction.ShouldNotContain("token", Case.Insensitive);
        audit.RecordedOutcomes.ShouldBe(
        [
            GovernanceOutcome.Succeeded,
            GovernanceOutcome.PolicyBlocked,
        ]);
    }

    private static RecordPrivilegedOperationalJustificationCommand Command(
        PrivilegedOperationalActionClass actionClass,
        DateTimeOffset? timestamp = null)
        => new(new PrivilegedOperationalJustificationV1(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            Actor,
            actionClass,
            PrivilegedActionClass.OperationalOverride,
            "privileged-review-policy",
            "customer-request",
            timestamp ?? Now,
            "correlation-001",
            "causation-001"));

    private static ConversationProjectedReadModels Models(
        TenantId tenantId,
        ProjectionTrustState state = null!,
        ProjectionFreshnessReasonCode reason = null!)
    {
        ProjectionTrustState trustState = state ?? ProjectionTrustState.Current;
        ProjectionFreshnessV1 freshness = new(
            SchemaVersion.Current,
            "pos:0000000001",
            1,
            Now,
            Now.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            trustState == ProjectionTrustState.Stale,
            trustState,
            reason ?? ProjectionFreshnessReasonCode.Current);
        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            tenantId,
            Conversation,
            freshness,
            "Open",
            "Case 123",
            null,
            null,
            null,
            [Participant],
            0,
            0);
        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            tenantId,
            Conversation,
            freshness,
            "Open",
            "Case 123",
            null,
            null,
            null,
            null,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [],
            []);
        return new(summary, detail);
    }

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public int Calls { get; private set; }

        public ConversationTenantAccessRequirement LastRequirement { get; private set; }

        public string? LastCallerPrincipalId { get; private set; }

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
        {
            Calls++;
            LastRequirement = requirement;
            LastCallerPrincipalId = callerPrincipalId;
            return ValueTask.FromResult(decision with { Requirement = requirement });
        }
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public ConversationProjectedReadModels? Models { get; set; }

        public int DetailReads { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            DetailReads++;
            return ValueTask.FromResult(Models);
        }

        public ValueTask<IReadOnlySet<string>> ValidatePageAsync(
            TenantId tenantId,
            ConversationProjectionIndexSnapshot snapshot,
            IReadOnlyList<ConversationSummaryProjectionV1> page,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ProjectionIndexSnapshotTestExtensions.NoInconsistentRows());

        public ValueTask<ConversationProjectionIndexSnapshot> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationProjectionIndexSnapshot.Empty);
    }

    private sealed class FakeGovernanceAuditService : IConversationGovernanceAuditService
    {
        public ConversationGovernanceAuditStatus NextStatus { get; set; } = ConversationGovernanceAuditStatus.Succeeded;

        public int RecordCalls { get; private set; }

        public GovernanceOperationKind? LastOperationKind { get; private set; }

        public List<GovernanceOutcome> RecordedOutcomes { get; } = [];

        public ValueTask<ConversationGovernanceAuditResult> RecordPrivilegedOperationalJustificationAsync(
            RecordPrivilegedOperationalJustificationCommand command,
            GovernanceOperationKind operationKind,
            GovernanceOutcome outcome,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            RecordCalls++;
            LastOperationKind = operationKind;
            RecordedOutcomes.Add(outcome);
            ConversationGovernanceAuditResult result = NextStatus switch
            {
                ConversationGovernanceAuditStatus.Succeeded => ConversationGovernanceAuditResult.Succeeded(
                    new GovernanceAuditEvidenceReference(
                        new AuditEvidenceHandle("audit-evidence-privileged-001"),
                        command.Justification.PolicyReference,
                        Now)),
                ConversationGovernanceAuditStatus.AuditUnavailable => ConversationGovernanceAuditResult.AuditUnavailable(),
                ConversationGovernanceAuditStatus.Uncertain => ConversationGovernanceAuditResult.Uncertain(),
                ConversationGovernanceAuditStatus.UnsafeEvidence => ConversationGovernanceAuditResult.UnsafeEvidence(),
                ConversationGovernanceAuditStatus.PolicyBlocked => ConversationGovernanceAuditResult.PolicyBlocked(),
                _ => throw new ArgumentOutOfRangeException(),
            };

            return ValueTask.FromResult(result);
        }

        public ValueTask<ConversationGovernanceAuditResult> RecordRetentionPolicyChangeAsync(
            SetConversationRetentionPolicyCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask<ConversationGovernanceAuditResult> RecordSensitivityMarkAsync(
            MarkConversationContentSensitiveCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask<ConversationGovernanceAuditResult> RecordRedactionAsync(
            RedactMessageContentCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
