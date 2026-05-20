// <copyright file="IdempotentConversationCommandExecutorTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.Tests.Idempotency;

/// <summary>
/// Verifies the server-side reserve/complete command-flow adapter.
/// </summary>
public sealed class IdempotentConversationCommandExecutorTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 19, 13, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Completed duplicate commands replay the stored logical outcome without invoking mutation.
    /// </summary>
    [Fact]
    public async Task DuplicateCompletedOutcomeShouldReplayWithoutMutation()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store);
        ConversationCommandFingerprint fingerprint = Fingerprint();
        ConversationIdempotencyOutcome outcome = SuccessOutcome();
        int mutationCount = 0;

        await store.ReserveAsync(fingerprint, Now, TimeSpan.FromHours(24), TestContext.Current.CancellationToken);
        await store.CompleteAsync(fingerprint, outcome, Now.AddSeconds(1), TestContext.Current.CancellationToken);

        DomainResult result = await executor.ExecuteAsync(
            fingerprint,
            Now.AddSeconds(2),
            "correlation-001",
            "causation-001",
            _ =>
            {
                mutationCount++;
                return ValueTask.FromResult(DomainResult.NoOp());
            },
            _ => outcome,
            TestContext.Current.CancellationToken);

        // P18 review fix (2026-05-19): assert structural fields of the replayed outcome instead of reference equality.
        // Reference equality only proved the store returned the same object; it did not validate that downstream consumers
        // see the expected category, identities, and retryability semantics.
        ConversationIdempotencyReplayResult replay = result.ShouldBeOfType<ConversationIdempotencyReplayResult>();
        replay.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Success);
        replay.Outcome.TenantId.ShouldBe(outcome.TenantId);
        replay.Outcome.CommandType.ShouldBe(outcome.CommandType);
        replay.Outcome.ConversationId.ShouldBe(outcome.ConversationId);
        replay.Outcome.ParticipantPartyId.ShouldBe(outcome.ParticipantPartyId);
        replay.Outcome.RejectionCode.ShouldBeNull();
        replay.Outcome.IsRetryable.ShouldBeFalse();
        mutationCount.ShouldBe(0);
    }

    /// <summary>
    /// P22: duplicate replay payloads expose only the server-generated audit handle and bounded logical outcome fields.
    /// </summary>
    [Fact]
    public async Task DuplicateReplayPayloadShouldExcludeCallerSuppliedCorrelationAndScope()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store);
        ConversationCommandFingerprint fingerprint = Fingerprint(correlationId: "caller-correlation-secret");
        string auditHandle = ConversationAuditHandle.FromServerBoundary(fingerprint, "event-server-001");
        ConversationIdempotencyOutcome outcome = SuccessOutcome(auditHandle);

        await store.ReserveAsync(fingerprint, Now, TimeSpan.FromHours(24), TestContext.Current.CancellationToken);
        await store.CompleteAsync(fingerprint, outcome, Now.AddSeconds(1), TestContext.Current.CancellationToken);

        DomainResult result = await executor.ExecuteAsync(
            fingerprint,
            Now.AddSeconds(2),
            "event-server-002",
            null,
            _ => ValueTask.FromResult(DomainResult.NoOp()),
            _ => outcome,
            TestContext.Current.CancellationToken);

        ConversationIdempotencyReplayResult replay = result.ShouldBeOfType<ConversationIdempotencyReplayResult>();
        replay.Outcome.CorrelationId.ShouldBe(auditHandle);
        replay.Outcome.AuditHandle.ShouldBe(auditHandle);
        replay.ResultPayload.ShouldNotBeNull();
        replay.ResultPayload.ShouldContain("auditHandle");
        replay.ResultPayload.ShouldNotContain("correlationId", Case.Insensitive);
        replay.ResultPayload.ShouldNotContain("caller-correlation-secret", Case.Insensitive);
        replay.ResultPayload.ShouldNotContain("idempotency-001", Case.Insensitive);
        replay.ResultPayload.ShouldNotContain(Tenant.Value, Case.Insensitive);
    }

    /// <summary>
    /// P30: The opaque audit handle includes the command scope value and idempotency key, not only tenant/type/schema.
    /// </summary>
    [Fact]
    public void AuditHandleShouldIncludeScopeValueAndIdempotencyKey()
    {
        ConversationCommandFingerprint first = Fingerprint(idempotencyKey: "idempotency-a");
        ConversationCommandFingerprint differentConversation = Fingerprint(
            conversationId: new ConversationId("conversation-002"),
            idempotencyKey: "idempotency-a");
        ConversationCommandFingerprint differentKey = Fingerprint(idempotencyKey: "idempotency-b");

        string firstHandle = ConversationAuditHandle.FromServerBoundary(first, "event-server-001");

        firstHandle.ShouldNotBe(ConversationAuditHandle.FromServerBoundary(differentConversation, "event-server-001"));
        firstHandle.ShouldNotBe(ConversationAuditHandle.FromServerBoundary(differentKey, "event-server-001"));
    }

    /// <summary>
    /// P48 review fix (2026-05-20): the audit handle's canonical encoding is length-prefixed so that an embedded
    /// delimiter in any scope part cannot collide with a different-but-otherwise-equivalent scope tuple.
    /// </summary>
    [Fact]
    public void AuditHandleShouldUseLengthPrefixedEncodingToPreventDelimiterInjection()
    {
        ConversationCommandFingerprint plainKey = Fingerprint(idempotencyKey: "idempotency-clean");
        ConversationCommandFingerprint embeddedNewlineKey = Fingerprint(idempotencyKey: "idempotency-clean\ninjected");

        string plainHandle = ConversationAuditHandle.FromServerBoundary(plainKey, "event-server-001");
        string injectedHandle = ConversationAuditHandle.FromServerBoundary(embeddedNewlineKey, "event-server-001");

        plainHandle.ShouldNotBe(injectedHandle);
    }

    /// <summary>
    /// Conflicting key reuse returns a typed rejection before mutation.
    /// </summary>
    [Fact]
    public async Task ConflictShouldRejectWithoutMutation()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store);
        ConversationCommandFingerprint first = Fingerprint(label: "Case 123");
        ConversationCommandFingerprint second = Fingerprint(label: "Case 456");
        int mutationCount = 0;

        await store.ReserveAsync(first, Now, TimeSpan.FromHours(24), TestContext.Current.CancellationToken);
        await store.CompleteAsync(first, SuccessOutcome(), Now.AddSeconds(1), TestContext.Current.CancellationToken);

        DomainResult result = await executor.ExecuteAsync(
            second,
            Now.AddSeconds(2),
            "correlation-001",
            "causation-001",
            _ =>
            {
                mutationCount++;
                return ValueTask.FromResult(DomainResult.NoOp());
            },
            _ => SuccessOutcome(),
            TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        rejection.ReasonCode.ShouldBe("idempotency_conflict");
        mutationCount.ShouldBe(0);
    }

    /// <summary>
    /// P44: Duplicate rejection replay preserves the first-attempt reason code, not a generic duplicate label.
    /// </summary>
    [Fact]
    public async Task DuplicateRejectionReplayShouldPreserveOriginalReasonCode()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store);
        ConversationCommandFingerprint fingerprint = Fingerprint();
        ConversationIdempotencyOutcome outcome = ConversationIdempotencyOutcome.Rejection(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            ConversationErrorCode.DuplicateParticipant,
            originalReasonCode: "participant_membership_duplicate",
            isRetryable: false,
            correlationId: "audit-001",
            auditHandle: "audit-001");

        await store.ReserveAsync(fingerprint, Now, TimeSpan.FromHours(24), TestContext.Current.CancellationToken);
        await store.CompleteAsync(fingerprint, outcome, Now.AddSeconds(1), TestContext.Current.CancellationToken);

        DomainResult result = await executor.ExecuteAsync(
            fingerprint,
            Now.AddSeconds(2),
            "correlation-001",
            "causation-001",
            _ => ValueTask.FromResult(DomainResult.NoOp()),
            _ => outcome,
            TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.DuplicateParticipant);
        rejection.ReasonCode.ShouldBe("participant_membership_duplicate");
    }

    /// <summary>
    /// Pending same-key submissions return retryable uncertainty without running a second mutation.
    /// </summary>
    [Fact]
    public async Task PendingEquivalentSubmissionShouldReturnRetryableUncertaintyWithoutMutation()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store);
        ConversationCommandFingerprint fingerprint = Fingerprint();
        int mutationCount = 0;

        await store.ReserveAsync(fingerprint, Now, TimeSpan.FromHours(24), TestContext.Current.CancellationToken);

        DomainResult result = await executor.ExecuteAsync(
            fingerprint,
            Now.AddSeconds(1),
            "correlation-001",
            "causation-001",
            _ =>
            {
                mutationCount++;
                return ValueTask.FromResult(DomainResult.NoOp());
            },
            _ => SuccessOutcome(),
            TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyOutcomeUnknown);

        // P8 review fix (2026-05-19): internal lifecycle reason codes (idempotency_record_pending, _poisoned, _expired, ...)
        // are coarsened to the single public reason "idempotency_outcome_unknown" to avoid disclosing internal store state.
        rejection.ReasonCode.ShouldBe("idempotency_outcome_unknown");
        mutationCount.ShouldBe(0);
    }

    /// <summary>
    /// P36: Completion timestamps come from the injected clock so deterministic tests do not see wall-clock updates.
    /// </summary>
    [Fact]
    public async Task CompletionShouldUseInjectedTimeProvider()
    {
        InMemoryConversationIdempotencyStore store = new();
        DateTimeOffset completedAt = Now.AddMinutes(10);
        IdempotentConversationCommandExecutor executor = new(
            store,
            timeProvider: new FixedTimeProvider(completedAt));
        ConversationCommandFingerprint fingerprint = Fingerprint();
        ConversationIdempotencyOutcome outcome = SuccessOutcome();

        await executor.ExecuteAsync(
            fingerprint,
            Now,
            "correlation-001",
            "causation-001",
            _ => ValueTask.FromResult(DomainResult.NoOp()),
            _ => outcome,
            TestContext.Current.CancellationToken);

        store.SnapshotRecords().Single().UpdatedAt.ShouldBe(completedAt);
    }

    /// <summary>
    /// P43: Retryable outcomes leave a poisoned record with uncertainty instead of releasing the reservation.
    /// </summary>
    [Fact]
    public async Task RetryableOutcomeShouldPoisonReservation()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store);
        ConversationCommandFingerprint fingerprint = Fingerprint();
        DomainResult retryableRejection = DomainResult.Rejection(new[]
        {
            new ConversationRejectedDomainEvent(
                ConversationErrorCode.ParticipantValidationUnavailable,
                "participant_validation_unavailable",
                SchemaVersion.Current,
                "correlation-001",
                "causation-001"),
        });

        await executor.ExecuteAsync(
            fingerprint,
            Now,
            "correlation-001",
            "causation-001",
            _ => ValueTask.FromResult(retryableRejection),
            _ => ConversationIdempotencyOutcome.Rejection(
                SchemaVersion.Current,
                Tenant,
                ConversationCommandType.CreateConversationCommand,
                Conversation,
                ConversationErrorCode.ParticipantValidationUnavailable,
                originalReasonCode: "participant_validation_unavailable",
                isRetryable: true,
                correlationId: "audit-001",
                auditHandle: "audit-001"),
            TestContext.Current.CancellationToken);

        ConversationIdempotencyRecord record = store.SnapshotRecords().Single();
        record.Status.ShouldBe(ConversationIdempotencyRecordStatus.Poisoned);
        record.Outcome.ShouldNotBeNull();
        record.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Uncertain);
    }

    /// <summary>
    /// P43: Outcome-factory exceptions poison the reservation before the original exception is rethrown.
    /// </summary>
    [Fact]
    public async Task OutcomeFactoryExceptionShouldPoisonReservationBeforeRethrow()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store);
        ConversationCommandFingerprint fingerprint = Fingerprint();

        await Should.ThrowAsync<InvalidOperationException>(() => executor.ExecuteAsync(
            fingerprint,
            Now,
            "correlation-001",
            "causation-001",
            _ => ValueTask.FromResult(DomainResult.NoOp()),
            _ => throw new InvalidOperationException("factory failed"),
            TestContext.Current.CancellationToken).AsTask());

        ConversationIdempotencyRecord record = store.SnapshotRecords().Single();
        record.Status.ShouldBe(ConversationIdempotencyRecordStatus.Poisoned);
        record.Outcome.ShouldNotBeNull();
        record.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Uncertain);
    }

    /// <summary>
    /// P37: Replay payload is computed from the current outcome after record copy/update operations.
    /// </summary>
    [Fact]
    public void ReplayResultPayloadShouldTrackUpdatedOutcome()
    {
        ConversationIdempotencyOutcome success = SuccessOutcome("audit-success");
        ConversationIdempotencyOutcome noOp = ConversationIdempotencyOutcome.NoOp(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            correlationId: "audit-noop",
            auditHandle: "audit-noop");

        ConversationIdempotencyReplayResult replay = new(success);
        ConversationIdempotencyReplayResult updated = replay with { Outcome = noOp };
        string payload = updated.ResultPayload.ShouldNotBeNull();

        payload.ShouldContain("NoOp");
        payload.ShouldContain("audit-noop");
        payload.ShouldNotContain("audit-success");
    }

    /// <summary>
    /// Terminal close and archive retries replay stored outcomes without invoking lifecycle mutation twice.
    /// </summary>
    [Fact]
    public async Task TerminalLifecycleCommandsShouldReplayWithoutMutation()
    {
        (object Command, ConversationCommandType CommandType)[] commands =
        [
            (new CloseConversationCommand(Metadata(), Conversation, "resolved"), ConversationCommandType.CloseConversationCommand),
            (new ArchiveConversationCommand(Metadata(), Conversation, "retained"), ConversationCommandType.ArchiveConversationCommand),
        ];

        foreach ((object command, ConversationCommandType commandType) in commands)
        {
            InMemoryConversationIdempotencyStore store = new();
            IdempotentConversationCommandExecutor executor = new(store);
            ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(command, Conversation);
            string auditHandle = ConversationAuditHandle.FromServerBoundary(fingerprint, "event-lifecycle-001");
            ConversationIdempotencyOutcome outcome = ConversationIdempotencyOutcome.Success(
                SchemaVersion.Current,
                Tenant,
                commandType,
                Conversation,
                messageId: null,
                participantPartyId: null,
                fileId: null,
                correlationId: auditHandle,
                auditHandle: auditHandle);
            int mutationCount = 0;

            await store.ReserveAsync(fingerprint, Now, TimeSpan.FromHours(24), TestContext.Current.CancellationToken);
            await store.CompleteAsync(fingerprint, outcome, Now.AddSeconds(1), TestContext.Current.CancellationToken);

            DomainResult result = await executor.ExecuteAsync(
                fingerprint,
                Now.AddSeconds(2),
                "correlation-001",
                "causation-001",
                _ =>
                {
                    mutationCount++;
                    return ValueTask.FromResult(DomainResult.NoOp());
                },
                _ => outcome,
                TestContext.Current.CancellationToken);

            result.ShouldBeOfType<ConversationIdempotencyReplayResult>().Outcome.ShouldBe(outcome);
            mutationCount.ShouldBe(0);
        }
    }

    private static ConversationCommandFingerprint Fingerprint(
        string label = "Case 123",
        string correlationId = "correlation-001",
        ConversationId? conversationId = null,
        string idempotencyKey = "idempotency-001")
        => ConversationCommandFingerprint.Create(
            new CreateConversationCommand(
                Metadata(correlationId, idempotencyKey),
                new BusinessReference("crm", "case-123"),
                new ProjectId("project-001"),
                new FolderId("folder-001"),
                label),
            conversationId ?? Conversation);

    private static ConversationCommandMetadata Metadata(
        string correlationId = "correlation-001",
        string idempotencyKey = "idempotency-001")
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            correlationId,
            "causation-001",
            idempotencyKey);

    private static ConversationIdempotencyOutcome SuccessOutcome(string auditHandle = "audit-001")
        => ConversationIdempotencyOutcome.Success(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            messageId: null,
            participantPartyId: null,
            fileId: null,
            correlationId: auditHandle,
            auditHandle: auditHandle);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
