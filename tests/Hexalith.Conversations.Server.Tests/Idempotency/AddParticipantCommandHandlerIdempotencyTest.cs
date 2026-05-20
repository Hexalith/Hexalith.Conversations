// <copyright file="AddParticipantCommandHandlerIdempotencyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.Tests.Idempotency;

/// <summary>
/// Verifies idempotency is placed after tenant access and before aggregate mutation.
/// </summary>
public sealed class AddParticipantCommandHandlerIdempotencyTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly ConversationId Conversation = new("conversation-a");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 19, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AddedAt = new(2026, 5, 19, 9, 5, 0, TimeSpan.Zero);

    /// <summary>
    /// Tenant denial happens before idempotency lookup, state load, or participant validation.
    /// </summary>
    [Fact]
    public async Task TenantDenialShouldRunBeforeIdempotencyLookup()
    {
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Reserved());
        AddParticipantCommandHandler handler = new(
            new FakeParticipantDirectory(),
            new StubTenantAccessService(ConversationTenantAccessDecision.Denied(
                ConversationTenantAccessRequirement.Write,
                Tenant,
                "user-1",
                ConversationTenantAccessDenialReason.MissingMember)),
            new IdempotentConversationCommandExecutor(idempotencyStore));
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(),
            callerPrincipalId: "user-1",
            loadStateAsync: _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            addedAt: AddedAt,
            eventId: "event-add-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        idempotencyStore.ReserveCalls.ShouldBe(0);
        loadCount.ShouldBe(0);
    }

    /// <summary>
    /// An idempotency conflict is returned before state load, participant validation, or aggregate dispatch.
    /// </summary>
    [Fact]
    public async Task IdempotencyConflictShouldRunBeforeAggregateLoad()
    {
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Conflict());
        FakeParticipantDirectory directory = new();
        AddParticipantCommandHandler handler = new(
            directory,
            new StubTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Write,
                Tenant,
                "user-1")),
            new IdempotentConversationCommandExecutor(idempotencyStore));
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(),
            callerPrincipalId: "user-1",
            loadStateAsync: _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            addedAt: AddedAt,
            eventId: "event-add-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        idempotencyStore.ReserveCalls.ShouldBe(1);
        loadCount.ShouldBe(0);
        directory.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// P24: a missing idempotency key is rejected before tenant access, idempotency lookup, or aggregate load.
    /// </summary>
    [Fact]
    public async Task MissingIdempotencyKeyShouldRejectBeforeTenantAccessAndIdempotencyLookup()
    {
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Reserved());
        SpyTenantAccessService tenantAccess = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            "user-1"));
        AddParticipantCommandHandler handler = new(
            new FakeParticipantDirectory(),
            tenantAccess,
            new IdempotentConversationCommandExecutor(idempotencyStore));
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(idempotencyKey: null),
            callerPrincipalId: "user-1",
            loadStateAsync: _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            addedAt: AddedAt,
            eventId: "event-add-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyKeyMissing);
        rejection.ReasonCode.ShouldBe("idempotency_key_missing");
        tenantAccess.Invocations.ShouldBe(0);
        idempotencyStore.ReserveCalls.ShouldBe(0);
        loadCount.ShouldBe(0);
    }

    private static AddParticipantCommand Command(string? idempotencyKey = "idempotency-a")
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-a",
                "causation-a",
                idempotencyKey),
            Conversation,
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static ConversationState CreatedState()
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-a",
                ConversationEventType.ConversationCreated,
                Tenant,
                Conversation,
                "correlation-a",
                CreatedAt,
                Actor,
                "causation-a")));
        return state;
    }

    private sealed class FakeParticipantDirectory : IParticipantDirectory
    {
        public int CallCount { get; private set; }

        public ValueTask<ParticipantDirectoryValidation> ValidateParticipantAsync(
            TenantId tenantId,
            PartyId participantPartyId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult(ParticipantDirectoryValidation.Valid());
        }
    }

    private sealed class StubTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
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
            => ValueTask.FromResult(decision);
    }

    private sealed class SpyTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public int Invocations { get; private set; }

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
            Invocations++;
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
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
