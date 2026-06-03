// <copyright file="LiveIdempotencyConflictOracleCharacterizationTest.cs" company="ITANEO">
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

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.3 (AC2, Task 4) — idempotency release-gate behavior re-expressed against the PUBLIC
/// <see cref="DomainResult"/> / <see cref="ConversationIdempotencyReplayResult"/> outcome surface and the
/// <see cref="ConversationRejectedDomainEvent"/> envelope.
///
/// Story 1.2's <c>LiveIdempotencyOracleCharacterizationTest</c> already pins the completed-duplicate
/// replay-without-mutation case inside the oracle. This test covers the cases it omits — conflicting-key
/// rejection before mutation, pending-key retryable uncertainty, duplicate-rejection reason-code
/// preservation, and replay-payload secret exclusion — so the full observable idempotency contract is
/// covered by the conformance oracle (Story 5.1), not only by Server.Tests.
///
/// Disposition (at-risk register): coupled-by-design-retarget-in-owning-story @ Story 2.2 (FR-7,
/// <c>EventStoreAggregate&lt;TState&gt;</c> base / idempotency-bridge shims). It deliberately drives the live
/// <see cref="IdempotentConversationCommandExecutor"/> to catch a flipped dedup branch; FR-7 retargets the
/// executor seam while these observable-outcome assertions stay. The replay-result/outcome types live in the
/// <c>Hexalith.Conversations.Idempotency</c> core namespace, which itself may shift under FR-7 — recorded as a
/// residual coupling in the register. Pins current behavior on <c>main</c>.
/// </summary>
public sealed class LiveIdempotencyConflictOracleCharacterizationTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 19, 13, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A conflicting reuse of a completed idempotency key returns the typed conflict rejection through the
    /// public surface and does NOT re-invoke the mutation.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveExecutorShouldRejectConflictingKeyReuseWithoutMutation()
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
    /// A pending same-key submission returns retryable uncertainty (coarsened to the single public reason)
    /// without running a second mutation.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveExecutorShouldReturnRetryableUncertaintyForPendingKeyWithoutMutation()
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
        rejection.ReasonCode.ShouldBe("idempotency_outcome_unknown");
        mutationCount.ShouldBe(0);
    }

    /// <summary>
    /// A duplicate replay of a stored rejection preserves the first-attempt reason code through the public
    /// envelope, not a generic duplicate label.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveExecutorShouldPreserveOriginalReasonCodeOnDuplicateRejectionReplay()
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
    /// A duplicate replay payload exposes only the server-generated audit handle and bounded logical outcome
    /// fields — never caller-supplied correlation or scope secrets.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveExecutorReplayPayloadShouldExcludeCallerSuppliedSecrets()
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
        replay.Outcome.AuditHandle.ShouldBe(auditHandle);
        replay.ResultPayload.ShouldNotBeNull();
        replay.ResultPayload.ShouldContain("auditHandle");
        replay.ResultPayload.ShouldNotContain("caller-correlation-secret", Case.Insensitive);
        replay.ResultPayload.ShouldNotContain("idempotency-001", Case.Insensitive);
        replay.ResultPayload.ShouldNotContain(Tenant.Value, Case.Insensitive);
    }

    private static ConversationCommandFingerprint Fingerprint(
        string label = "Case 123",
        string correlationId = "correlation-001",
        string idempotencyKey = "idempotency-001")
        => ConversationCommandFingerprint.Create(
            new CreateConversationCommand(
                Metadata(correlationId, idempotencyKey),
                new BusinessReference("crm", "case-123"),
                new ProjectId("project-001"),
                new FolderId("folder-001"),
                label),
            Conversation);

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
}
