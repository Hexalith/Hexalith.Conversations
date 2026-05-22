// <copyright file="SetConversationRetentionPolicyCommandHandlerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Verifies the governed retention handler authorization and audit gates.
/// </summary>
public sealed class SetConversationRetentionPolicyCommandHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly TenantId OtherTenant = new("tenant-b");
    private static readonly ConversationId Conversation = new("conversation-a");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 19, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AppliedAt = new(2026, 5, 19, 9, 5, 0, TimeSpan.Zero);

    /// <summary>
    /// Tenant or governance denial happens before aggregate load and before audit proof.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldDenyBeforeStateLoadAndAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        ConversationTenantAccessDecision denial = ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1",
            ConversationTenantAccessDenialReason.InsufficientRole);
        SpyTenantAccessService access = new(denial);
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        rejection.CorrelationId.ShouldBe("event-retention-a");
        loadCount.ShouldBe(0);
        audit.CallCount.ShouldBe(0);
        access.LastRequirement.ShouldBe(ConversationTenantAccessRequirement.Governance);
    }

    /// <summary>
    /// Audit unavailability fails closed and emits no retention mutation event.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldFailClosedWhenAuditUnavailable()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.AuditUnavailable());
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        rejection.ReasonCode.ShouldBe("audit_unavailable");
        result.Events.Any(e => e is RetentionPolicySetDomainEvent || e is RetentionPolicyReplacedDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Audit service exceptions fail closed and emit no retention mutation event.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldFailClosedWhenAuditServiceThrows()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()), throwOnRecord: true);
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        rejection.ReasonCode.ShouldBe("audit_unavailable");
        result.Events.Any(e => e is RetentionPolicySetDomainEvent || e is RetentionPolicyReplacedDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Non-success audit precondition outcomes fail closed without retention mutation events.
    /// </summary>
    /// <param name="status">The audit status.</param>
    /// <param name="expectedCodeValue">The expected public rejection code value.</param>
    /// <param name="expectedReason">The expected public reason.</param>
    [Theory]
    [InlineData(ConversationGovernanceAuditStatus.UnsafeEvidence, "audit_pairing_required", "audit_evidence_unsafe")]
    [InlineData(ConversationGovernanceAuditStatus.Uncertain, "idempotency_outcome_unknown", "audit_pairing_uncertain")]
    [InlineData(ConversationGovernanceAuditStatus.PolicyBlocked, "command_validation_failed", "retention_policy_blocked")]
    public async Task HandleAsyncShouldFailClosedForNonSuccessAuditStatuses(
        ConversationGovernanceAuditStatus status,
        string expectedCodeValue,
        string expectedReason)
    {
        FakeAuditService audit = new(new ConversationGovernanceAuditResult(status));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.Parse(expectedCodeValue));
        rejection.ReasonCode.ShouldBe(expectedReason);
        result.Events.Any(e => e is RetentionPolicySetDomainEvent || e is RetentionPolicyReplacedDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// A successful audited command emits a retention policy event after state tenant binding is checked.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldEmitRetentionEventWhenGovernanceAuditAndStatePass()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        RetentionPolicySetDomainEvent set = result.Events.Single().ShouldBeOfType<RetentionPolicySetDomainEvent>();
        set.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Closed conversation state is rejected before audit evidence is created.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectClosedConversationBeforeAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(ClosedState()),
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("conversation_not_open");
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Audit evidence must be paired to the retention command policy and timestamp before mutation dispatch.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectMismatchedAuditEvidenceWithoutMutation()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(new GovernanceAuditEvidenceReference(
            new AuditEvidenceHandle("audit-evidence-wrong"),
            "retention-policy-other",
            AppliedAt.AddMinutes(1))));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_mismatch");
        result.Events.Any(e => e is RetentionPolicySetDomainEvent || e is RetentionPolicyReplacedDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// A loaded aggregate with a mismatched tenant fails closed before audit proof.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectTenantMismatchBeforeAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState(OtherTenant)),
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Idempotency conflicts are rejected before state load or audit evidence creation.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectIdempotencyConflictBeforeStateLoadAndAudit()
    {
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Conflict());
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(
            access,
            audit,
            new IdempotentConversationCommandExecutor(idempotencyStore));
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        rejection.ReasonCode.ShouldBe("idempotency_conflict");
        idempotencyStore.ReserveCalls.ShouldBe(1);
        loadCount.ShouldBe(0);
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Compatible duplicate retention commands replay the original sanitized outcome without duplicate audit or mutation work.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldReplayDuplicateRetentionOutcomeWithoutDuplicateAuditOrMutation()
    {
        InMemoryConversationIdempotencyStore idempotencyStore = new();
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(
            access,
            audit,
            new IdempotentConversationCommandExecutor(
                idempotencyStore,
                timeProvider: new FixedTimeProvider(AppliedAt.AddMinutes(1))));
        int loadCount = 0;

        ValueTask<ConversationState?> LoadStateAsync(CancellationToken _)
        {
            loadCount++;
            return ValueTask.FromResult<ConversationState?>(CreatedState());
        }

        DomainResult first = await handler.HandleAsync(
            Command(),
            "user-1",
            LoadStateAsync,
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);
        DomainResult replay = await handler.HandleAsync(
            Command(),
            "user-1",
            LoadStateAsync,
            "event-retention-b",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        ConversationIdempotencyReplayResult replayResult = replay.ShouldBeOfType<ConversationIdempotencyReplayResult>();
        replayResult.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Success);
        replayResult.Outcome.CommandType.ShouldBe(ConversationCommandType.SetConversationRetentionPolicyCommand);
        replayResult.Outcome.AuditHandle.ShouldNotBeNullOrWhiteSpace();
        replayResult.Outcome.AuditHandle.ShouldBe(replayResult.Outcome.CorrelationId);
        string replayPayload = replayResult.ResultPayload.ShouldNotBeNull();
        replayPayload.ShouldNotContain("idempotency-a", Case.Insensitive);
        loadCount.ShouldBe(1);
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Reusing an idempotency identity for a materially different retention policy rejects without state load or audit.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectMateriallyDifferentPolicyWithSameIdempotencyIdentity()
    {
        InMemoryConversationIdempotencyStore idempotencyStore = new();
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SetConversationRetentionPolicyCommandHandler handler = new(
            access,
            audit,
            new IdempotentConversationCommandExecutor(
                idempotencyStore,
                timeProvider: new FixedTimeProvider(AppliedAt.AddMinutes(1))));
        int loadCount = 0;

        ValueTask<ConversationState?> LoadStateAsync(CancellationToken _)
        {
            loadCount++;
            return ValueTask.FromResult<ConversationState?>(CreatedState());
        }

        DomainResult first = await handler.HandleAsync(
            Command(),
            "user-1",
            LoadStateAsync,
            "event-retention-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);
        DomainResult conflict = await handler.HandleAsync(
            Command("retention-policy-extended"),
            "user-1",
            LoadStateAsync,
            "event-retention-b",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        ConversationRejectedDomainEvent rejection = conflict.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        rejection.ReasonCode.ShouldBe("idempotency_conflict");
        loadCount.ShouldBe(1);
        audit.CallCount.ShouldBe(1);
    }

    private static SetConversationRetentionPolicyCommand Command(string policyReference = "retention-policy-standard")
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-a",
                "causation-a",
                "idempotency-a"),
            Conversation,
            policyReference,
            "customer-request",
            AppliedAt);

    private static GovernanceAuditEvidenceReference AuditEvidence()
        => new(new AuditEvidenceHandle("audit-evidence-001"), "retention-policy-standard", AppliedAt);

    private static ConversationState CreatedState(TenantId? tenant = null)
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-a",
                ConversationEventType.ConversationCreated,
                tenant ?? Tenant,
                Conversation,
                "correlation-a",
                CreatedAt,
                Actor,
                "causation-a")));
        return state;
    }

    private static ConversationState ClosedState()
    {
        ConversationState state = CreatedState();
        state.Apply(new ConversationClosed(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-close-a",
                ConversationEventType.ConversationClosed,
                Tenant,
                Conversation,
                "correlation-a",
                AppliedAt,
                Actor,
                "causation-a"),
            "resolved"));
        return state;
    }

    private sealed class FakeAuditService(ConversationGovernanceAuditResult result, bool throwOnRecord = false) : IConversationGovernanceAuditService
    {
        public int CallCount { get; private set; }

        public ValueTask<ConversationGovernanceAuditResult> RecordRetentionPolicyChangeAsync(
            SetConversationRetentionPolicyCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (throwOnRecord)
            {
                throw new InvalidOperationException("audit sink unavailable");
            }

            return ValueTask.FromResult(result);
        }

        public ValueTask<ConversationGovernanceAuditResult> RecordSensitivityMarkAsync(
            MarkConversationContentSensitiveCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationGovernanceAuditResult.AuditUnavailable());

        public ValueTask<ConversationGovernanceAuditResult> RecordRedactionAsync(
            RedactMessageContentCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationGovernanceAuditResult.AuditUnavailable());
    }

    private sealed class SpyTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public ConversationTenantAccessRequirement LastRequirement { get; private set; }

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
            LastRequirement = requirement;
            return ValueTask.FromResult(decision);
        }
    }

    private sealed class SpyIdempotencyStore(ConversationIdempotencyDecision decision) : IConversationIdempotencyStore
    {
        public int ReserveCalls { get; private set; }

        public ValueTask<ConversationIdempotencyDecision> ReserveAsync(
            ConversationCommandFingerprint fingerprint,
            DateTimeOffset now,
            TimeSpan retention,
            CancellationToken cancellationToken = default)
        {
            ReserveCalls++;
            return ValueTask.FromResult(decision);
        }

        public ValueTask CompleteAsync(
            ConversationCommandFingerprint fingerprint,
            ConversationIdempotencyOutcome outcome,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask ReleaseAsync(
            ConversationCommandFingerprint fingerprint,
            DateTimeOffset reservationCreatedAt,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask MarkPoisonedAsync(
            ConversationCommandFingerprint fingerprint,
            ConversationIdempotencyOutcome outcome,
            DateTimeOffset poisonedAt,
            DateTimeOffset reservationCreatedAt,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
