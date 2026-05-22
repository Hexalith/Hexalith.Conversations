// <copyright file="MarkConversationContentSensitiveCommandHandlerTest.cs" company="ITANEO">
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
/// Verifies sensitivity-mark command authorization and audit gates.
/// </summary>
public sealed class MarkConversationContentSensitiveCommandHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly TenantId OtherTenant = new("tenant-b");
    private static readonly ConversationId Conversation = new("conversation-a");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly MessageId Message = new("message-a");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AppliedAt = new(2026, 5, 20, 9, 5, 0, TimeSpan.Zero);

    /// <summary>
    /// Tenant or governance denial happens before aggregate load and before audit proof.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldDenyBeforeStateLoadAndAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1",
            ConversationTenantAccessDenialReason.InsufficientRole));
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        loadCount.ShouldBe(0);
        audit.CallCount.ShouldBe(0);
        access.LastRequirement.ShouldBe(ConversationTenantAccessRequirement.Governance);
    }

    /// <summary>
    /// Audit unavailability fails closed and emits no sensitivity mutation event.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldFailClosedWhenAuditUnavailable()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.AuditUnavailable());
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        result.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Audit service exceptions fail closed and emit no sensitivity mutation event.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldFailClosedWhenAuditServiceThrows()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()), throwOnRecord: true);
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        rejection.ReasonCode.ShouldBe("audit_unavailable");
        result.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Non-success audit precondition outcomes fail closed without sensitivity mutation events.
    /// </summary>
    /// <param name="status">The audit status.</param>
    /// <param name="expectedCodeValue">The expected public rejection code value.</param>
    /// <param name="expectedReason">The expected public reason.</param>
    [Theory]
    [InlineData(ConversationGovernanceAuditStatus.UnsafeEvidence, "audit_pairing_required", "audit_evidence_unsafe")]
    [InlineData(ConversationGovernanceAuditStatus.Uncertain, "idempotency_outcome_unknown", "audit_pairing_uncertain")]
    [InlineData(ConversationGovernanceAuditStatus.PolicyBlocked, "command_validation_failed", "sensitivity_policy_blocked")]
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
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.Parse(expectedCodeValue));
        rejection.ReasonCode.ShouldBe(expectedReason);
        result.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// A successful audited command emits a sensitivity event after state tenant binding is checked.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldEmitSensitivityEventWhenGovernanceAuditAndStatePass()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ConversationContentMarkedSensitiveDomainEvent>();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Target validation happens before audit evidence is created.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectInvalidTargetBeforeAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);
        GovernanceTarget missingMessage = new(GovernedTargetKind.Message, MessageId: new MessageId("message-missing"));

        DomainResult result = await handler.HandleAsync(
            Command(target: missingMessage),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("sensitivity_target_invalid");
        result.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Compatible already-sensitive targets return no-op before duplicate audit evidence can be created.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldReturnNoOpForCompatibleDuplicateBeforeAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);
        ConversationState state = CreatedState();
        state.Apply(SensitiveEvent());

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(state),
            "event-sensitive-duplicate",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Audit evidence must be paired to the sensitivity command policy and timestamp before mutation dispatch.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectMismatchedAuditEvidenceWithoutMutation()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(new GovernanceAuditEvidenceReference(
            new AuditEvidenceHandle("audit-evidence-wrong"),
            "sensitivity-policy-other",
            AppliedAt.AddMinutes(1))));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_mismatch");
        result.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
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
        MarkConversationContentSensitiveCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState(OtherTenant)),
            "event-sensitive-a",
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
        MarkConversationContentSensitiveCommandHandler handler = new(
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
            "event-sensitive-a",
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
    /// Compatible duplicate sensitivity commands replay the original sanitized outcome.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldReplayDuplicateSensitivityOutcomeWithoutDuplicateAuditOrMutation()
    {
        InMemoryConversationIdempotencyStore idempotencyStore = new();
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(
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
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);
        DomainResult replay = await handler.HandleAsync(
            Command(),
            "user-1",
            LoadStateAsync,
            "event-sensitive-b",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        ConversationIdempotencyReplayResult replayResult = replay.ShouldBeOfType<ConversationIdempotencyReplayResult>();
        replayResult.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Success);
        replayResult.Outcome.CommandType.ShouldBe(ConversationCommandType.MarkConversationContentSensitiveCommand);
        replayResult.Outcome.AuditHandle.ShouldNotBeNullOrWhiteSpace();
        replayResult.Outcome.AuditHandle.ShouldBe(replayResult.Outcome.CorrelationId);
        string replayPayload = replayResult.ResultPayload.ShouldNotBeNull();
        replayPayload.ShouldNotContain("idempotency-a", Case.Insensitive);
        loadCount.ShouldBe(1);
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Reusing an idempotency identity for a materially different sensitivity mark rejects without state load or audit.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectMateriallyDifferentMarkWithSameIdempotencyIdentity()
    {
        InMemoryConversationIdempotencyStore idempotencyStore = new();
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        MarkConversationContentSensitiveCommandHandler handler = new(
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
            "event-sensitive-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);
        DomainResult conflict = await handler.HandleAsync(
            Command(SensitivityCategory.Regulated),
            "user-1",
            LoadStateAsync,
            "event-sensitive-b",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        ConversationRejectedDomainEvent rejection = conflict.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        rejection.ReasonCode.ShouldBe("idempotency_conflict");
        loadCount.ShouldBe(1);
        audit.CallCount.ShouldBe(1);
    }

    private static MarkConversationContentSensitiveCommand Command(
        SensitivityCategory? category = null,
        GovernanceTarget? target = null)
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-a",
                "causation-a",
                "idempotency-a"),
            Conversation,
            target ?? new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            category ?? SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AppliedAt);

    private static GovernanceAuditEvidenceReference AuditEvidence()
        => new(new AuditEvidenceHandle("audit-evidence-001"), "sensitivity-policy-standard", AppliedAt);

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
        state.Apply(new MessageAppended(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-message-a",
                ConversationEventType.MessageAppended,
                Tenant,
                Conversation,
                "correlation-a",
                CreatedAt.AddMinutes(1),
                Actor,
                "causation-a"),
            Message,
            Actor,
            "safe-placeholder"));
        return state;
    }

    private static ConversationContentMarkedSensitiveDomainEvent SensitiveEvent()
        => new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-sensitive-original",
                ConversationEventType.ConversationContentMarkedSensitive,
                Tenant,
                Conversation,
                "correlation-a",
                AppliedAt,
                Actor,
                "causation-a"),
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AuditEvidence());

    private sealed class FakeAuditService(ConversationGovernanceAuditResult result, bool throwOnRecord = false) : IConversationGovernanceAuditService
    {
        public int CallCount { get; private set; }

        public ValueTask<ConversationGovernanceAuditResult> RecordRetentionPolicyChangeAsync(
            SetConversationRetentionPolicyCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationGovernanceAuditResult.AuditUnavailable());

        public ValueTask<ConversationGovernanceAuditResult> RecordSensitivityMarkAsync(
            MarkConversationContentSensitiveCommand command,
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
