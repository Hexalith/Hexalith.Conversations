// <copyright file="LiveIdempotencyOracleCharacterizationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
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
/// Story 1.2 oracle-strengthening backfill — behavior #3 (idempotency).
///
/// The IdempotencyConformanceSuite asserts synthetic scenario-engine outcomes; the live
/// <see cref="IdempotentConversationCommandExecutor"/> reserve/complete/replay decision is exercised only
/// in Server.Tests, which is NOT part of the oracle. A broken dedup that re-executes a replay as new work
/// would ride green through the oracle.
///
/// This characterization test runs the LIVE executor from inside the conformance project, pinning the
/// current observable behavior: a completed duplicate replays the stored outcome WITHOUT re-invoking the
/// mutation. If the dedup branch were flipped (Duplicate -> Reserved), the mutation would run again and the
/// mutation-count assertion turns RED.
/// </summary>
public sealed class LiveIdempotencyOracleCharacterizationTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 19, 13, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A completed duplicate command replays the stored outcome and does NOT re-invoke the mutation.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveExecutorShouldReplayDuplicateWithoutReinvokingMutation()
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

        ConversationIdempotencyReplayResult replay = result.ShouldBeOfType<ConversationIdempotencyReplayResult>();
        replay.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Success);
        replay.Outcome.ConversationId.ShouldBe(outcome.ConversationId);
        mutationCount.ShouldBe(0);
    }

    /// <summary>
    /// Positive control: a first, unseen submission DOES invoke the mutation exactly once. Without this, a
    /// "never execute" mutation would pass the dedup assertion above.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task LiveExecutorShouldInvokeMutationOnceForFirstSubmission()
    {
        InMemoryConversationIdempotencyStore store = new();
        IdempotentConversationCommandExecutor executor = new(store, timeProvider: new FixedTimeProvider(Now));
        ConversationCommandFingerprint fingerprint = Fingerprint();
        ConversationIdempotencyOutcome outcome = SuccessOutcome();
        int mutationCount = 0;

        await executor.ExecuteAsync(
            fingerprint,
            Now,
            "correlation-001",
            "causation-001",
            _ =>
            {
                mutationCount++;
                return ValueTask.FromResult(DomainResult.NoOp());
            },
            _ => outcome,
            TestContext.Current.CancellationToken);

        mutationCount.ShouldBe(1);
    }

    private static ConversationCommandFingerprint Fingerprint()
        => ConversationCommandFingerprint.Create(
            new CreateConversationCommand(
                Metadata(),
                new BusinessReference("crm", "case-123"),
                new ProjectId("project-001"),
                new FolderId("folder-001"),
                "Case 123"),
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
            correlationId: "audit-001",
            auditHandle: "audit-001");

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
