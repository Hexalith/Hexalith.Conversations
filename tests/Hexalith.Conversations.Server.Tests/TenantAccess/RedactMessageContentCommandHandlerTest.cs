// <copyright file="RedactMessageContentCommandHandlerTest.cs" company="ITANEO">
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
/// Verifies redaction command authorization and audit gates.
/// </summary>
public sealed class RedactMessageContentCommandHandlerTest
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
        RedactMessageContentCommandHandler handler = new(access, audit);
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        loadCount.ShouldBe(0);
        audit.CallCount.ShouldBe(0);
        access.LastRequirement.ShouldBe(ConversationTenantAccessRequirement.Governance);
    }

    /// <summary>
    /// Audit unavailability fails closed and emits no redaction mutation event.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldFailClosedWhenAuditUnavailable()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.AuditUnavailable());
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Audit service exceptions fail closed and emit no redaction mutation event.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldFailClosedWhenAuditServiceThrows()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()), throwOnRecord: true);
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        rejection.ReasonCode.ShouldBe("audit_unavailable");
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Non-success audit precondition outcomes fail closed without redaction mutation events.
    /// </summary>
    /// <param name="status">The audit status.</param>
    /// <param name="expectedCodeValue">The expected public rejection code value.</param>
    /// <param name="expectedReason">The expected public reason.</param>
    [Theory]
    [InlineData(ConversationGovernanceAuditStatus.UnsafeEvidence, "audit_pairing_required", "audit_evidence_unsafe")]
    [InlineData(ConversationGovernanceAuditStatus.Uncertain, "idempotency_outcome_unknown", "audit_pairing_uncertain")]
    [InlineData(ConversationGovernanceAuditStatus.PolicyBlocked, "command_validation_failed", "redaction_policy_blocked")]
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
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.Parse(expectedCodeValue));
        rejection.ReasonCode.ShouldBe(expectedReason);
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Target validation happens before audit evidence is created, avoiding external side effects for invalid targets.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectInvalidTargetBeforeAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);
        GovernanceTarget missingMessage = new(GovernedTargetKind.Message, MessageId: new MessageId("message-missing"));

        DomainResult result = await handler.HandleAsync(
            Command(target: missingMessage),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("redaction_target_invalid");
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Compatible already-redacted targets return no-op before duplicate audit evidence can be created.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldReturnNoOpForCompatibleDuplicateBeforeAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);
        ConversationState state = CreatedState();
        state.Apply(RedactedEvent());

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(state),
            "event-redacted-duplicate",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Audit evidence must be paired to the command policy and timestamp before mutation dispatch.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectMismatchedAuditEvidenceWithoutMutation()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(new GovernanceAuditEvidenceReference(
            new AuditEvidenceHandle("audit-evidence-wrong"),
            "redaction-policy-other",
            AppliedAt.AddMinutes(1))));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_mismatch");
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// A successful audited command emits a redaction event after state tenant binding is checked.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldEmitRedactionEventWhenGovernanceAuditAndStatePass()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// A prior sensitivity mark on the target does not bypass the redaction audit gate or block the mutation.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRedactAlreadySensitiveTargetAfterAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);
        ConversationState state = CreatedState();
        state.Apply(SensitiveEvent());

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(state),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
        state.SensitivityMarks.Single().Target.MessageId.ShouldBe(Message);
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
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState(OtherTenant)),
            "event-redacted-a",
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
        RedactMessageContentCommandHandler handler = new(
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
            "event-redacted-a",
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
    /// Unsupported schema versions are rejected before tenant access, state load, idempotency, or audit disclosure.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectUnsupportedSchemaBeforeTenantAccessAndDisclosure()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Reserved());
        RedactMessageContentCommandHandler handler = new(
            access,
            audit,
            new IdempotentConversationCommandExecutor(idempotencyStore));
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(schemaVersion: new SchemaVersion(SchemaVersion.Current.Value + 1)),
            "user-1",
            _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        rejection.ReasonCode.ShouldBe("unsupported_schema_version");
        access.CallCount.ShouldBe(0);
        idempotencyStore.ReserveCalls.ShouldBe(0);
        loadCount.ShouldBe(0);
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// State loading failures are coarsened to stale tenant state and fail before audit proof.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldCoarsenStateLoadFailureBeforeAudit()
    {
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => throw new InvalidOperationException("projection store outage"),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
        rejection.ReasonCode.ShouldBe("tenant_projection_stale");
        audit.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Completed duplicate redaction requests replay the stored sanitized result without state load or audit evidence creation.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldReplayCompletedDuplicateWithoutStateLoadOrAudit()
    {
        ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(Command(), Conversation);
        string auditHandle = ConversationAuditHandle.FromServerBoundary(fingerprint, "event-redacted-original");
        ConversationIdempotencyOutcome outcome = ConversationIdempotencyOutcome.Success(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.RedactMessageContentCommand,
            Conversation,
            Message,
            participantPartyId: null,
            fileId: null,
            auditHandle,
            auditHandle);
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Duplicate(outcome));
        FakeAuditService audit = new(ConversationGovernanceAuditResult.Succeeded(AuditEvidence()));
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Governance,
            Tenant,
            "user-1"));
        RedactMessageContentCommandHandler handler = new(
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
            "event-redacted-replay",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationIdempotencyReplayResult replay = result.ShouldBeOfType<ConversationIdempotencyReplayResult>();
        replay.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Success);
        replay.Outcome.CommandType.ShouldBe(ConversationCommandType.RedactMessageContentCommand);
        replay.Outcome.MessageId.ShouldBe(Message);
        string replayPayload = replay.ResultPayload.ShouldNotBeNull();
        replayPayload.ShouldNotContain("idempotency-a", Case.Insensitive);
        replayPayload.ShouldNotContain("customer-request", Case.Insensitive);
        idempotencyStore.ReserveCalls.ShouldBe(1);
        loadCount.ShouldBe(0);
        audit.CallCount.ShouldBe(0);
    }

    private static RedactMessageContentCommand Command(
        RedactionCategory? category = null,
        SchemaVersion? schemaVersion = null,
        GovernanceTarget? target = null)
        => new(
            new ConversationCommandMetadata(
                schemaVersion ?? SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-a",
                "causation-a",
                "idempotency-a"),
            Conversation,
            target ?? new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            category ?? RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AppliedAt);

    private static GovernanceAuditEvidenceReference AuditEvidence()
        => new(new AuditEvidenceHandle("audit-evidence-001"), "redaction-policy-standard", AppliedAt);

    private static ConversationContentMarkedSensitiveDomainEvent SensitiveEvent()
        => new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-sensitive-a",
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
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-sensitive-001"),
                "sensitivity-policy-standard",
                AppliedAt));

    private static MessageContentRedactedDomainEvent RedactedEvent()
        => new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-redacted-original",
                ConversationEventType.MessageContentRedacted,
                Tenant,
                Conversation,
                "correlation-a",
                AppliedAt,
                Actor,
                "causation-a"),
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AuditEvidence());

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
                tenant ?? Tenant,
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
            => ValueTask.FromResult(ConversationGovernanceAuditResult.AuditUnavailable());

        public ValueTask<ConversationGovernanceAuditResult> RecordRedactionAsync(
            RedactMessageContentCommand command,
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
    }

    private sealed class SpyTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public int CallCount { get; private set; }

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
            CallCount++;
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
}
