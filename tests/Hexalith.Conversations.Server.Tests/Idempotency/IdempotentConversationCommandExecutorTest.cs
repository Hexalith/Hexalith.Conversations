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
            ConversationIdempotencyOutcome outcome = ConversationIdempotencyOutcome.Success(
                SchemaVersion.Current,
                Tenant,
                commandType,
                Conversation,
                messageId: null,
                participantPartyId: null,
                fileId: null,
                correlationId: "correlation-001");
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

    private static ConversationCommandFingerprint Fingerprint(string label = "Case 123")
        => ConversationCommandFingerprint.Create(
            new CreateConversationCommand(
                Metadata(),
                new BusinessReference("crm", "case-123"),
                new ProjectId("project-001"),
                new FolderId("folder-001"),
                label),
            Conversation);

    private static ConversationCommandMetadata Metadata()
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-001",
            "causation-001",
            "idempotency-001");

    private static ConversationIdempotencyOutcome SuccessOutcome()
        => ConversationIdempotencyOutcome.Success(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            messageId: null,
            participantPartyId: null,
            fileId: null,
            correlationId: "correlation-001");
}
