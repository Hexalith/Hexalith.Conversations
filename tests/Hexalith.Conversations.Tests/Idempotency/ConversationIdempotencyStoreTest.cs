// <copyright file="ConversationIdempotencyStoreTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Idempotency;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Idempotency;

/// <summary>
/// Verifies atomic reserve/complete behavior for local idempotency evidence.
/// </summary>
public sealed class ConversationIdempotencyStoreTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Now = new(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    /// <summary>
    /// Concurrent equivalent reservations permit one writer and force the rest into retryable uncertainty.
    /// </summary>
    [Fact]
    public async Task ConcurrentEquivalentReservationsShouldHaveSingleWinner()
    {
        // P7 review fix (2026-05-19): ReserveAsync returns a synchronously-completed ValueTask, so a LINQ projection
        // of `.AsTask()` calls runs sequentially. Use Task.Run with a barrier so all 32 callers race the lock at the same
        // wall-clock instant; only then does the test actually prove atomic reservation.
        InMemoryConversationIdempotencyStore store = new();
        ConversationCommandFingerprint fingerprint = Fingerprint();
        const int callers = 32;
        using Barrier barrier = new(callers);
        CancellationToken token = TestContext.Current.CancellationToken;

        Task<ConversationIdempotencyDecision>[] attempts = Enumerable
            .Range(0, callers)
            .Select(_ => Task.Run(
                async () =>
                {
                    barrier.SignalAndWait(token);
                    return await store.ReserveAsync(fingerprint, Now, Retention, token);
                },
                token))
            .ToArray();

        ConversationIdempotencyDecision[] decisions = await Task.WhenAll(attempts);

        decisions.Count(d => d.Kind == ConversationIdempotencyDecisionKind.Reserved).ShouldBe(1);
        decisions.Count(d => d.Kind == ConversationIdempotencyDecisionKind.RetryableUncertainty).ShouldBe(callers - 1);

        ConversationIdempotencyOutcome outcome = SuccessOutcome();
        await store.CompleteAsync(fingerprint, outcome, Now.AddSeconds(1), TestContext.Current.CancellationToken);

        ConversationIdempotencyDecision duplicate = await store.ReserveAsync(
            fingerprint,
            Now.AddSeconds(2),
            Retention,
            TestContext.Current.CancellationToken);

        duplicate.Kind.ShouldBe(ConversationIdempotencyDecisionKind.Duplicate);
        duplicate.StoredOutcome.ShouldBe(outcome);
    }

    /// <summary>
    /// Reusing a scoped key with a different fingerprint is a conflict and does not replace the stored outcome.
    /// </summary>
    [Fact]
    public async Task DifferentPayloadShouldReturnConflictWithoutReplacingOutcome()
    {
        InMemoryConversationIdempotencyStore store = new();
        ConversationCommandFingerprint first = Fingerprint(label: "Case 123");
        ConversationCommandFingerprint second = Fingerprint(label: "Case 456");

        (await store.ReserveAsync(first, Now, Retention, TestContext.Current.CancellationToken))
            .Kind.ShouldBe(ConversationIdempotencyDecisionKind.Reserved);
        await store.CompleteAsync(first, SuccessOutcome(), Now.AddSeconds(1), TestContext.Current.CancellationToken);

        ConversationIdempotencyDecision conflict = await store.ReserveAsync(
            second,
            Now.AddSeconds(2),
            Retention,
            TestContext.Current.CancellationToken);
        ConversationIdempotencyDecision duplicate = await store.ReserveAsync(
            first,
            Now.AddSeconds(3),
            Retention,
            TestContext.Current.CancellationToken);

        conflict.Kind.ShouldBe(ConversationIdempotencyDecisionKind.Conflict);
        conflict.StoredOutcome.ShouldBeNull();
        duplicate.Kind.ShouldBe(ConversationIdempotencyDecisionKind.Duplicate);
        duplicate.StoredOutcome.ShouldBe(SuccessOutcome());
    }

    /// <summary>
    /// Expired records are evicted and a new caller can re-acquire the scoped key (P5 review fix 2026-05-19);
    /// otherwise an expired record would permanently lock the key for callers retrying after the retention window.
    /// </summary>
    [Fact]
    public async Task ExpiredRecordShouldBeEvictedAndReplaceableByFreshReservation()
    {
        InMemoryConversationIdempotencyStore store = new();
        ConversationCommandFingerprint fingerprint = Fingerprint();

        (await store.ReserveAsync(fingerprint, Now, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken))
            .Kind.ShouldBe(ConversationIdempotencyDecisionKind.Reserved);
        await store.CompleteAsync(fingerprint, SuccessOutcome(), Now.AddMinutes(1), TestContext.Current.CancellationToken);

        ConversationIdempotencyDecision afterExpiry = await store.ReserveAsync(
            fingerprint,
            Now.AddMinutes(6),
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        afterExpiry.Kind.ShouldBe(ConversationIdempotencyDecisionKind.Reserved);
    }

    /// <summary>
    /// Stored records and debug output contain bounded metadata and fingerprints, not command or provider payload values.
    /// </summary>
    [Fact]
    public async Task StoredRecordsShouldNotExposeRawPayloadOrProviderData()
    {
        InMemoryConversationIdempotencyStore store = new();
        ConversationCommandFingerprint fingerprint = Fingerprint(label: "Sensitive case label");

        await store.ReserveAsync(fingerprint, Now, Retention, TestContext.Current.CancellationToken);
        await store.CompleteAsync(fingerprint, SuccessOutcome(), Now.AddSeconds(1), TestContext.Current.CancellationToken);

        string recordText = store.SnapshotRecords().Single().ToString();

        recordText.ShouldNotContain("Sensitive case label", Case.Insensitive);
        recordText.ShouldNotContain("provider-session", Case.Insensitive);
        recordText.ShouldNotContain("provider-response", Case.Insensitive);
        recordText.ShouldNotContain("EventStore", Case.Insensitive);
        recordText.ShouldNotContain("stream", Case.Insensitive);
        recordText.ShouldNotContain("payload", Case.Insensitive);
    }

    /// <summary>
    /// Poisoned and version-incompatible artifacts return retryable uncertainty rather than allowing mutation.
    /// </summary>
    [Fact]
    public async Task UnsafeStoredArtifactsShouldReturnRetryableUncertainty()
    {
        ConversationCommandFingerprint poisonedFingerprint = Fingerprint(label: "Poisoned", idempotencyKey: "idempotency-poisoned");
        ConversationCommandFingerprint incompatibleFingerprint = Fingerprint(label: "Incompatible", idempotencyKey: "idempotency-incompatible");
        ConversationIdempotencyRecord poisoned = ConversationIdempotencyRecord.Pending(
            poisonedFingerprint,
            Now,
            Retention) with
        {
            Status = ConversationIdempotencyRecordStatus.Poisoned,
        };
        ConversationIdempotencyRecord incompatible = ConversationIdempotencyRecord.Pending(
            incompatibleFingerprint,
            Now,
            Retention) with
        {
            RecordVersion = ConversationIdempotencyRecord.CurrentRecordVersion + 1,
        };
        InMemoryConversationIdempotencyStore store = new([poisoned, incompatible]);

        ConversationIdempotencyDecision poisonedDecision = await store.ReserveAsync(
            poisonedFingerprint,
            Now.AddSeconds(1),
            Retention,
            TestContext.Current.CancellationToken);
        ConversationIdempotencyDecision incompatibleDecision = await store.ReserveAsync(
            incompatibleFingerprint,
            Now.AddSeconds(1),
            Retention,
            TestContext.Current.CancellationToken);

        poisonedDecision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.RetryableUncertainty);
        poisonedDecision.ReasonCode.ShouldBe("idempotency_record_poisoned");
        incompatibleDecision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.RetryableUncertainty);
        incompatibleDecision.ReasonCode.ShouldBe("idempotency_record_version_incompatible");
    }

    /// <summary>
    /// P19 review fix (2026-05-19): tenant A's stored outcome must be invisible to tenant B with the same idempotency key
    /// and scope value. Scope-record equality includes TenantId; this test makes the cross-tenant isolation explicit.
    /// </summary>
    [Fact]
    public async Task SameIdempotencyKeyUnderDifferentTenantShouldNotReplayStoredOutcome()
    {
        InMemoryConversationIdempotencyStore store = new();
        TenantId tenantA = new("tenant-A");
        TenantId tenantB = new("tenant-B");
        ConversationCommandFingerprint fingerprintA = FingerprintForTenant(tenantA, idempotencyKey: "shared-key");
        ConversationCommandFingerprint fingerprintB = FingerprintForTenant(tenantB, idempotencyKey: "shared-key");

        await store.ReserveAsync(fingerprintA, Now, Retention, TestContext.Current.CancellationToken);
        await store.CompleteAsync(
            fingerprintA,
            ConversationIdempotencyOutcome.Success(
                SchemaVersion.Current,
                tenantA,
                ConversationCommandType.CreateConversationCommand,
                Conversation,
                messageId: null,
                participantPartyId: null,
                fileId: null,
                correlationId: "correlation-A",
                auditHandle: "audit-A"),
            Now.AddSeconds(1),
            TestContext.Current.CancellationToken);

        ConversationIdempotencyDecision tenantBDecision = await store.ReserveAsync(
            fingerprintB,
            Now.AddSeconds(2),
            Retention,
            TestContext.Current.CancellationToken);

        tenantBDecision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.Reserved);
        tenantBDecision.StoredOutcome.ShouldBeNull();
    }

    /// <summary>
    /// P20 review fix (2026-05-19): a key reserved under one command type must not collide with the same key under a
    /// different command type, even at the same tenant + scope. Scope-record equality includes CommandType.
    /// </summary>
    [Fact]
    public async Task SameKeyUnderDifferentCommandTypeShouldNotCollide()
    {
        InMemoryConversationIdempotencyStore store = new();
        ConversationCommandFingerprint createFingerprint = Fingerprint(idempotencyKey: "shared-key");
        ConversationCommandFingerprint addParticipantFingerprint = AddParticipantFingerprint(idempotencyKey: "shared-key");

        ConversationIdempotencyDecision createDecision = await store.ReserveAsync(
            createFingerprint,
            Now,
            Retention,
            TestContext.Current.CancellationToken);
        ConversationIdempotencyDecision addParticipantDecision = await store.ReserveAsync(
            addParticipantFingerprint,
            Now.AddSeconds(1),
            Retention,
            TestContext.Current.CancellationToken);

        createDecision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.Reserved);
        addParticipantDecision.Kind.ShouldBe(ConversationIdempotencyDecisionKind.Reserved);
    }

    private static ConversationCommandFingerprint FingerprintForTenant(
        TenantId tenantId,
        string idempotencyKey = "idempotency-001")
        => ConversationCommandFingerprint.Create(
            new CreateConversationCommand(
                new ConversationCommandMetadata(
                    SchemaVersion.Current,
                    tenantId,
                    Actor,
                    "correlation-001",
                    "causation-001",
                    idempotencyKey),
                new BusinessReference("crm", "case-123"),
                new ProjectId("project-001"),
                new FolderId("folder-001"),
                "Case 123",
                new ProviderCorrelationMetadata(
                    "provider-a",
                    "assistant",
                    SchemaVersion.Current,
                    "provider-session",
                    "provider-response")),
            Conversation);

    private static ConversationCommandFingerprint AddParticipantFingerprint(string idempotencyKey)
        => ConversationCommandFingerprint.Create(
            new AddParticipantCommand(
                Metadata(idempotencyKey),
                Conversation,
                ParticipantPartyId: new PartyId("party-new"),
                ParticipantType: ParticipantType.Human,
                ParticipantRole: ParticipantRole.Member),
            Conversation);

    private static ConversationCommandFingerprint Fingerprint(
        string label = "Case 123",
        string idempotencyKey = "idempotency-001")
        => ConversationCommandFingerprint.Create(
            new CreateConversationCommand(
                Metadata(idempotencyKey),
                new BusinessReference("crm", "case-123"),
                new ProjectId("project-001"),
                new FolderId("folder-001"),
                label,
                new ProviderCorrelationMetadata(
                    "provider-a",
                    "assistant",
                    SchemaVersion.Current,
                    "provider-session",
                    "provider-response")),
            Conversation);

    private static ConversationCommandMetadata Metadata(string idempotencyKey = "idempotency-001")
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-001",
            "causation-001",
            idempotencyKey);

    private static ConversationIdempotencyOutcome SuccessOutcome()
        => ConversationIdempotencyOutcome.Success(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            messageId: null,
            participantPartyId: null,
            fileId: null,
            correlationId: "correlation-001",
            auditHandle: "audit-001");
}
