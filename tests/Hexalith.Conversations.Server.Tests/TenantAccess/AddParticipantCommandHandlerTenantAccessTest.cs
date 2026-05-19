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
        AddParticipantCommandHandler handler = new(directory, new StubTenantAccessService(denial));
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
        rejection.ReasonCode.ShouldBe("tenant_member_missing");
        loadCount.ShouldBe(0);
        directory.CallCount.ShouldBe(0);
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
}
