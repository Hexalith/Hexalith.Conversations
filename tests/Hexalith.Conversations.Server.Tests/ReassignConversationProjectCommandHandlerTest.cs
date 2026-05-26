// <copyright file="ReassignConversationProjectCommandHandlerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests;

/// <summary>
/// Verifies project reassignment command handling at the tenant/idempotency boundary.
/// </summary>
public sealed class ReassignConversationProjectCommandHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly ConversationId Conversation = new("conversation-a");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly ProjectId OldProject = new("project-old");
    private static readonly ProjectId NewProject = new("project-new");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ChangedAt = new(2026, 5, 26, 9, 5, 0, TimeSpan.Zero);

    [Fact]
    public async Task TenantDenialShouldRunBeforeIdempotencyLookupOrAggregateLoad()
    {
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Reserved());
        ReassignConversationProjectCommandHandler handler = new(
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
                return ValueTask.FromResult<ConversationState?>(CreatedState(OldProject));
            },
            changedAt: ChangedAt,
            eventId: "event-project-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        idempotencyStore.ReserveCalls.ShouldBe(0);
        loadCount.ShouldBe(0);
    }

    [Fact]
    public async Task IdempotencyConflictShouldRunBeforeAggregateLoad()
    {
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Conflict());
        ReassignConversationProjectCommandHandler handler = new(
            new StubTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Write,
                Tenant,
                "user-1")),
            new IdempotentConversationCommandExecutor(
                idempotencyStore,
                timeProvider: new FixedTimeProvider(ChangedAt.AddSeconds(1))));
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(),
            callerPrincipalId: "user-1",
            loadStateAsync: _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState(OldProject));
            },
            changedAt: ChangedAt,
            eventId: "event-project-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        idempotencyStore.ReserveCalls.ShouldBe(1);
        loadCount.ShouldBe(0);
    }

    [Fact]
    public async Task DuplicateProjectReassignmentShouldReplayWithoutAggregateLoad()
    {
        ConversationIdempotencyOutcome outcome = ConversationIdempotencyOutcome.Success(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.ReassignConversationProjectCommand,
            Conversation,
            messageId: null,
            participantPartyId: null,
            fileId: null,
            correlationId: "audit-project-a",
            auditHandle: "audit-project-a");
        SpyIdempotencyStore idempotencyStore = new(ConversationIdempotencyDecision.Duplicate(outcome));
        ReassignConversationProjectCommandHandler handler = new(
            new StubTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Write,
                Tenant,
                "user-1")),
            new IdempotentConversationCommandExecutor(idempotencyStore));
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            Command(expectedCurrentProjectId: OldProject),
            callerPrincipalId: "user-1",
            loadStateAsync: _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState(OldProject));
            },
            changedAt: ChangedAt,
            eventId: "event-project-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationIdempotencyReplayResult replay = result.ShouldBeOfType<ConversationIdempotencyReplayResult>();
        replay.Outcome.Category.ShouldBe(IdempotencyOutcomeCategory.Success);
        replay.Outcome.CommandType.ShouldBe(ConversationCommandType.ReassignConversationProjectCommand);
        replay.Outcome.ConversationId.ShouldBe(Conversation);
        idempotencyStore.ReserveCalls.ShouldBe(1);
        loadCount.ShouldBe(0);
    }

    [Fact]
    public async Task AllowedProjectReassignmentShouldEmitProjectChangedEvent()
    {
        ReassignConversationProjectCommandHandler handler = new(new StubTenantAccessService(
            ConversationTenantAccessDecision.Allowed(ConversationTenantAccessRequirement.Write, Tenant, "user-1")));

        DomainResult result = await handler.HandleAsync(
            Command(expectedCurrentProjectId: OldProject),
            callerPrincipalId: "user-1",
            loadStateAsync: _ => ValueTask.FromResult<ConversationState?>(CreatedState(OldProject)),
            changedAt: ChangedAt,
            eventId: "event-project-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        ConversationProjectChangedDomainEvent changed = result.Events.Single().ShouldBeOfType<ConversationProjectChangedDomainEvent>();
        changed.Metadata.EventType.ShouldBe(ConversationEventType.ConversationProjectChanged);
        changed.PreviousProjectId.ShouldBe(OldProject);
        changed.CurrentProjectId.ShouldBe(NewProject);
    }

    [Fact]
    public async Task TenantMismatchedLoadedStateShouldFailClosedWithoutMutationEvent()
    {
        ReassignConversationProjectCommandHandler handler = new(new StubTenantAccessService(
            ConversationTenantAccessDecision.Allowed(ConversationTenantAccessRequirement.Write, Tenant, "user-1")));

        DomainResult result = await handler.HandleAsync(
            Command(expectedCurrentProjectId: OldProject),
            callerPrincipalId: "user-1",
            loadStateAsync: _ => ValueTask.FromResult<ConversationState?>(CreatedState(
                OldProject,
                tenantId: new TenantId("tenant-other"))),
            changedAt: ChangedAt,
            eventId: "event-project-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsRejection.ShouldBeTrue();
        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        result.Events.ShouldNotContain(e => e is ConversationProjectChangedDomainEvent);
    }

    private static ReassignConversationProjectCommand Command(ProjectId? expectedCurrentProjectId = null)
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-a",
                "causation-a",
                "idempotency-a"),
            Conversation,
            new ConversationProjectAssignment(ConversationProjectAssignmentOperation.Assign, NewProject),
            expectedCurrentProjectId);

    private static ConversationState CreatedState(ProjectId? projectId = null, TenantId? tenantId = null)
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-a",
                ConversationEventType.ConversationCreated,
                tenantId ?? Tenant,
                Conversation,
                "correlation-a",
                CreatedAt,
                Actor,
                "causation-a"),
            ProjectId: projectId));
        return state;
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
