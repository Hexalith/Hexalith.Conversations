// <copyright file="AddParticipantCommandHandlerTest.cs" company="ITANEO">
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
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests;

/// <summary>
/// Verifies command-time Party validation is fail-closed before aggregate invocation.
/// </summary>
public sealed class AddParticipantCommandHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 18, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AddedAt = new(2026, 5, 18, 12, 45, 0, TimeSpan.Zero);

    /// <summary>
    /// A successful Party validation allows aggregate participant addition.
    /// </summary>
    [Fact]
    public async Task ValidPartyProofShouldDispatchAggregate()
    {
        FakeParticipantDirectory directory = new(ParticipantDirectoryValidation.Valid());
        AddParticipantCommandHandler handler = new(directory, AllowedTenantAccess());

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-alpha",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            AddedAt,
            "event-add-alpha",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ParticipantAddedDomainEvent>();
        directory.CallCount.ShouldBe(1);
        directory.LastTenantId.ShouldBe(Tenant);
        directory.LastPartyId.ShouldBe(Participant);
    }

    /// <summary>
    /// Every non-success Party validation outcome fails closed with a typed rejection and no success event.
    /// </summary>
    /// <param name="status">The simulated directory validation status.</param>
    [Theory]
    [InlineData(ParticipantDirectoryValidationStatus.Unavailable)]
    [InlineData(ParticipantDirectoryValidationStatus.Unknown)]
    [InlineData(ParticipantDirectoryValidationStatus.Inaccessible)]
    [InlineData(ParticipantDirectoryValidationStatus.Timeout)]
    [InlineData(ParticipantDirectoryValidationStatus.Error)]
    [InlineData(ParticipantDirectoryValidationStatus.NotFound)]
    [InlineData(ParticipantDirectoryValidationStatus.TenantMismatch)]
    [InlineData(ParticipantDirectoryValidationStatus.Disabled)]
    [InlineData(ParticipantDirectoryValidationStatus.Malformed)]
    [InlineData(ParticipantDirectoryValidationStatus.Indeterminate)]
    public async Task PartyValidationFailuresShouldFailClosedBeforeAggregateInvocation(ParticipantDirectoryValidationStatus status)
    {
        FakeParticipantDirectory directory = new(new ParticipantDirectoryValidation(status));
        AddParticipantCommandHandler handler = new(directory, AllowedTenantAccess());

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-alpha",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            AddedAt,
            "event-add-alpha",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(status == ParticipantDirectoryValidationStatus.TenantMismatch
            ? ConversationErrorCode.TenantContextMismatch
            : ConversationErrorCode.ParticipantValidationUnavailable);
        result.Events.ShouldNotContain(e => e is ParticipantAddedDomainEvent);
    }

    /// <summary>
    /// Invalid command shape is rejected before the Party directory is called.
    /// </summary>
    [Fact]
    public async Task InvalidCommandShapeShouldNotCallParticipantDirectory()
    {
        FakeParticipantDirectory directory = new(ParticipantDirectoryValidation.Valid());
        AddParticipantCommandHandler handler = new(directory, AllowedTenantAccess());
        AddParticipantCommand invalid = Command() with { ParticipantPartyId = null! };
        int loadCount = 0;

        DomainResult result = await handler.HandleAsync(
            invalid,
            "user-alpha",
            _ =>
            {
                loadCount++;
                return ValueTask.FromResult<ConversationState?>(CreatedState());
            },
            AddedAt,
            "event-add-alpha",
            trustedTenantId: Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("participant_party_missing");
        loadCount.ShouldBe(0);
        directory.CallCount.ShouldBe(0);
    }

    private static AddParticipantCommand Command()
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-alpha",
                "causation-alpha",
                "idempotency-alpha"),
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
                "event-create-alpha",
                ConversationEventType.ConversationCreated,
                Tenant,
                Conversation,
                "correlation-alpha",
                CreatedAt,
                Actor,
                "causation-alpha")));
        return state;
    }

    private static ConversationRejectedDomainEvent SingleRejection(DomainResult result)
    {
        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        return result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
    }

    private static IConversationTenantAccessService AllowedTenantAccess()
        => new StubTenantAccessService(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Write,
            Tenant,
            "user-alpha"));

    private sealed class FakeParticipantDirectory(ParticipantDirectoryValidation validation) : IParticipantDirectory
    {
        public int CallCount { get; private set; }

        public TenantId? LastTenantId { get; private set; }

        public PartyId? LastPartyId { get; private set; }

        public ValueTask<ParticipantDirectoryValidation> ValidateParticipantAsync(
            TenantId tenantId,
            PartyId participantPartyId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastTenantId = tenantId;
            LastPartyId = participantPartyId;
            return ValueTask.FromResult(validation);
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
