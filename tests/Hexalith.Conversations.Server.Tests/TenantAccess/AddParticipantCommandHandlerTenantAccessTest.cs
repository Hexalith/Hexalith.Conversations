// <copyright file="AddParticipantCommandHandlerTenantAccessTest.cs" company="ITANEO">
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
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Verifies the available participant command handler is guarded below HTTP middleware.
/// </summary>
public sealed class AddParticipantCommandHandlerTenantAccessTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly TenantId OtherTenant = new("tenant-b");
    private static readonly ConversationId Conversation = new("conversation-a");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 19, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AddedAt = new(2026, 5, 19, 9, 5, 0, TimeSpan.Zero);

    /// <summary>
    /// A tenant denial returns a typed rejection before aggregate state or Party validation is touched.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldDenyBeforeStateLoadAndParticipantValidation()
    {
        FakeParticipantDirectory directory = new();
        ConversationTenantAccessDecision denial = ConversationTenantAccessDecision.Denied(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            "user-1",
            ConversationTenantAccessDenialReason.MissingMember);
        SpyTenantAccessService access = new(denial);
        AddParticipantCommandHandler handler = new(directory, access);
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

        // D1: durable rejection reason code collapses to the non-disclosing public token.
        rejection.ReasonCode.ShouldBe("tenant_isolation_violation");

        // F4: caller-supplied correlation/causation ids are NOT propagated into the denial path.
        rejection.CorrelationId.ShouldBe("event-add-a");
        rejection.CausationId.ShouldBeNull();

        loadCount.ShouldBe(0);
        directory.CallCount.ShouldBe(0);

        // F25: positively assert the tenant access service was invoked with the right arguments.
        access.Invocations.ShouldBe(1);
        access.LastTrustedTenant.ShouldBe(Tenant);
        access.LastCallerPrincipalId.ShouldBe("user-1");
        access.LastRequirement.ShouldBe(ConversationTenantAccessRequirement.Write);
    }

    /// <summary>
    /// F4: a missing trusted tenant binding fails closed before the access guard or state load runs.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectMissingTrustedTenantBindingBeforeAccessCheck()
    {
        FakeParticipantDirectory directory = new();
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            "user-1"));
        AddParticipantCommandHandler handler = new(directory, access);
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
            trustedTenantId: null,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantBindingMissing);
        rejection.ReasonCode.ShouldBe("tenant_binding_missing");
        rejection.CorrelationId.ShouldBe("event-add-a");
        rejection.CausationId.ShouldBeNull();

        loadCount.ShouldBe(0);
        directory.CallCount.ShouldBe(0);
        access.Invocations.ShouldBe(0);
    }

    /// <summary>
    /// F2: a state-load infrastructure exception is converted to a typed fail-closed rejection
    /// rather than propagating raw infrastructure types to the caller.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldConvertStateLoadFailureToTypedRejection()
    {
        FakeParticipantDirectory directory = new();
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            "user-1"));
        AddParticipantCommandHandler handler = new(directory, access);

        DomainResult result = await handler.HandleAsync(
            Command(),
            callerPrincipalId: "user-1",
            loadStateAsync: _ => throw new InvalidOperationException("EventStore stream snapshot read failed"),
            addedAt: AddedAt,
            eventId: "event-add-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
        rejection.ReasonCode.ShouldBe("tenant_projection_stale");
        rejection.CorrelationId.ShouldBe("event-add-a");
        directory.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// F3: an aggregate whose persisted TenantId disagrees with the granted tenant fails closed
    /// before participant directory validation or aggregate dispatch.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldRejectAggregateTenantMismatchAfterLoad()
    {
        FakeParticipantDirectory directory = new();
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            "user-1"));
        AddParticipantCommandHandler handler = new(directory, access);
        ConversationState stateInOtherTenant = CreatedStateInTenant(OtherTenant);

        DomainResult result = await handler.HandleAsync(
            Command(),
            callerPrincipalId: "user-1",
            loadStateAsync: _ => ValueTask.FromResult<ConversationState?>(stateInOtherTenant),
            addedAt: AddedAt,
            eventId: "event-add-a",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.TenantIsolationViolation);
        rejection.ReasonCode.ShouldBe("tenant_isolation_violation");
        directory.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// F23: handler-level cancellation propagates as <see cref="OperationCanceledException"/>
    /// rather than being converted into a denial or rejection.
    /// </summary>
    [Fact]
    public async Task HandleAsyncShouldPropagateCancellation()
    {
        FakeParticipantDirectory directory = new();
        SpyTenantAccessService access = new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            "user-1"));
        AddParticipantCommandHandler handler = new(directory, access);

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            handler.HandleAsync(
                Command(),
                callerPrincipalId: "user-1",
                loadStateAsync: cancellationToken =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return ValueTask.FromResult<ConversationState?>(CreatedState());
                },
                addedAt: AddedAt,
                eventId: "event-add-a",
                trustedTenantId: Tenant,
                cancellationToken: cts.Token).AsTask());
    }

    private static AddParticipantCommand Command()
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-a",
                "causation-a",
                "idempotency-a"),
            Conversation,
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static ConversationState CreatedState() => CreatedStateInTenant(Tenant);

    private static ConversationState CreatedStateInTenant(TenantId tenantId)
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-a",
                ConversationEventType.ConversationCreated,
                tenantId,
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

    private sealed class SpyTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public int Invocations { get; private set; }

        public TenantId? LastTrustedTenant { get; private set; }

        public string? LastCallerPrincipalId { get; private set; }

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
            Invocations++;
            LastRequirement = requirement;
            LastTrustedTenant = trustedTenantId;
            LastCallerPrincipalId = callerPrincipalId;
            return ValueTask.FromResult(decision);
        }
    }
}
