// <copyright file="ConversationAggregateParticipantTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Aggregates;

/// <summary>
/// Verifies deterministic participant membership behavior for the conversation aggregate.
/// </summary>
public sealed class ConversationAggregateParticipantTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly PartyId HumanParty = new("party-human");
    private static readonly PartyId AgentParty = new("party-agent");
    private static readonly PartyId LlmParty = new("party-llm");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 18, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AddedAt = new(2026, 5, 18, 12, 45, 0, TimeSpan.Zero);

    /// <summary>
    /// Human, AI agent, and LLM participants can be represented by stable Party IDs and type/role dimensions.
    /// </summary>
    [Fact]
    public void ValidParticipantTypesShouldEmitParticipantAddedEvents()
    {
        (PartyId Party, ParticipantType Type)[] participants =
        [
            (HumanParty, ParticipantType.Human),
            (AgentParty, ParticipantType.AiAgent),
            (LlmParty, ParticipantType.Llm),
        ];

        foreach ((PartyId party, ParticipantType type) in participants)
        {
            ConversationState state = CreatedState();
            AddParticipant command = AddDomainCommand(party, type);

            DomainResult result = ConversationAggregate.Handle(command, state);

            result.IsSuccess.ShouldBeTrue();
            ParticipantAddedDomainEvent added = result.Events.Single().ShouldBeOfType<ParticipantAddedDomainEvent>();
            added.Metadata.SchemaVersion.ShouldBe(SchemaVersion.Current);
            added.Metadata.EventType.ShouldBe(ConversationEventType.ParticipantAdded);
            added.Metadata.TenantId.ShouldBe(Tenant);
            added.Metadata.ConversationId.ShouldBe(Conversation);
            added.Metadata.ActorPartyId.ShouldBe(Actor);
            added.ParticipantPartyId.ShouldBe(party);
            added.ParticipantType.ShouldBe(type);
            added.ParticipantRole.ShouldBe(ParticipantRole.Member);
        }
    }

    /// <summary>
    /// Replaying participant events reconstructs membership without validators or external services.
    /// </summary>
    [Fact]
    public void ParticipantAddedEventsShouldReplayDeterministically()
    {
        ParticipantAddedDomainEvent human = AddedEvent(HumanParty, ParticipantType.Human);
        ParticipantAddedDomainEvent agent = AddedEvent(AgentParty, ParticipantType.AiAgent);
        ParticipantAddedDomainEvent llm = AddedEvent(LlmParty, ParticipantType.Llm);

        ConversationState first = CreatedState();
        ConversationState second = CreatedState();

        first.Apply(human);
        first.Apply(agent);
        first.Apply(llm);
        second.Apply(human);
        second.Apply(agent);
        second.Apply(llm);

        first.Participants.Count.ShouldBe(3);
        second.Participants.Count.ShouldBe(first.Participants.Count);
        second.Participants.ShouldBe(first.Participants, ignoreOrder: false);
        first.HasParticipant(HumanParty, ParticipantType.Human, ParticipantRole.Member).ShouldBeTrue();
        first.HasParticipant(AgentParty, ParticipantType.AiAgent, ParticipantRole.Member).ShouldBeTrue();
        first.HasParticipant(LlmParty, ParticipantType.Llm, ParticipantRole.Member).ShouldBeTrue();
    }

    /// <summary>
    /// Duplicate membership is a domain-state rejection, not a retry-safe idempotency decision.
    /// </summary>
    [Fact]
    public void DuplicateParticipantMembershipShouldReturnTypedRejectionWithoutSuccessEvent()
    {
        ConversationState state = CreatedState();
        state.Apply(AddedEvent(HumanParty, ParticipantType.Human));

        DomainResult result = ConversationAggregate.Handle(AddDomainCommand(HumanParty, ParticipantType.Human), state);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.DuplicateParticipant);
        rejection.ReasonCode.ShouldBe("participant_membership_duplicate");
    }

    /// <summary>
    /// Missing, tenant-mismatched, and incompatible state fail closed without participant-added events.
    /// </summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("not-created")]
    [InlineData("tenant-mismatch")]
    [InlineData("conversation-mismatch")]
    [InlineData("closed")]
    [InlineData("archived")]
    public void UnsafeConversationStatesShouldRejectParticipantAddition(string stateShape)
    {
        ConversationState? state = stateShape switch
        {
            "missing" => null,
            "not-created" => new ConversationState(),
            "tenant-mismatch" => CreatedState(tenant: new TenantId("tenant-other")),
            "conversation-mismatch" => CreatedState(conversation: new ConversationId("conversation-other")),
            "closed" => ClosedState(),
            "archived" => ArchivedState(),
            _ => throw new ArgumentOutOfRangeException(nameof(stateShape), stateShape, "Unsupported state fixture."),
        };

        DomainResult result = ConversationAggregate.Handle(AddDomainCommand(HumanParty, ParticipantType.Human), state);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldAllBe(e => e is ConversationRejectedDomainEvent);
    }

    /// <summary>
    /// Provider correlation values cannot be substituted for stable Party identity.
    /// </summary>
    [Theory]
    [InlineData("provider-session-77")]
    [InlineData("provider-response-88")]
    [InlineData("thread-42")]
    public void ProviderOnlyIdentityShouldReturnTypedRejection(string substitutedIdentity)
    {
        ConversationState state = CreatedState();
        AddParticipant command = AddDomainCommand(new PartyId(substitutedIdentity), ParticipantType.Human);

        DomainResult result = ConversationAggregate.Handle(command, state);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.ProviderOnlyIdentityForbidden);
        rejection.ReasonCode.ShouldBe("provider_identity_not_authority");
    }

    /// <summary>
    /// A null <see cref="ParticipantType"/> or <see cref="ParticipantRole"/> on the public command surface
    /// is rejected with the typed <see cref="ConversationErrorCode.UnsupportedParticipant"/> code at the
    /// aggregate boundary. The closed-vocabulary JSON converters reject unknown wire values at a different
    /// layer; this test pins the aggregate-side rejection that the spec subtask names directly.
    /// </summary>
    [Theory]
    [InlineData("type")]
    [InlineData("role")]
    public void UnsupportedParticipantShapeShouldReturnTypedRejection(string missingField)
    {
        ConversationState state = CreatedState();
        AddParticipantCommand publicCommand = missingField == "type"
            ? BuildCommand(HumanParty).PublicCommand with { ParticipantType = null! }
            : BuildCommand(HumanParty).PublicCommand with { ParticipantRole = null! };

        AddParticipant domainCommand = new(publicCommand, AddedAt, $"event-add-{HumanParty.Value}");
        DomainResult result = ConversationAggregate.Handle(domainCommand, state);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.UnsupportedParticipant);
        rejection.ReasonCode.ShouldBe(missingField == "type"
            ? "participant_type_unsupported"
            : "participant_role_unsupported");
    }

    /// <summary>
    /// Structural proof that the domain assembly has no reference to <c>IParticipantDirectory</c>: the
    /// adapter lives in the Server hydration layer and replay MUST NOT reach for it. This protects against
    /// a future change that wires a validation adapter into the aggregate via a service locator.
    /// </summary>
    [Fact]
    public void DomainAssemblyShouldNotReferenceParticipantDirectoryAdapter()
    {
        Assembly domain = typeof(ConversationAggregate).Assembly;
        IEnumerable<string> referencedAssemblyNames = domain
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        referencedAssemblyNames.ShouldNotContain("Hexalith.Conversations.Server");

        Type[] domainTypes = domain.GetTypes();
        domainTypes.ShouldNotContain(t => t.Name == "IParticipantDirectory");
        domainTypes.ShouldNotContain(t => t.Name == "ParticipantDirectoryValidation");
        domainTypes.ShouldNotContain(t => t.Name == "ParticipantDirectoryValidationStatus");
    }

    private static AddParticipant BuildCommand(PartyId party)
        => AddDomainCommand(party, ParticipantType.Human);

    private static AddParticipant AddDomainCommand(PartyId party, ParticipantType type)
    {
        ConversationCommandMetadata metadata = new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-alpha",
            CausationId: "causation-alpha",
            IdempotencyKey: "idempotency-alpha");

        AddParticipantCommand publicCommand = new(
            metadata,
            Conversation,
            party,
            type,
            ParticipantRole.Member,
            new ProviderCorrelationMetadata(
                "contoso-ai",
                "assistant",
                SchemaVersion.Current,
                ProviderSessionReference: "provider-session-77",
                ProviderResponseReference: "provider-response-88",
                ExtensionData: new Dictionary<string, string> { ["thread"] = "thread-42" }));

        return new AddParticipant(publicCommand, AddedAt, $"event-add-{party.Value}");
    }

    private static ConversationState CreatedState(TenantId? tenant = null, ConversationId? conversation = null)
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-alpha",
                ConversationEventType.ConversationCreated,
                tenant ?? Tenant,
                conversation ?? Conversation,
                "correlation-alpha",
                CreatedAt,
                Actor,
                "causation-alpha")));
        return state;
    }

    private static ConversationState ClosedState()
    {
        ConversationState state = CreatedState();
        state.ForceLifecycleForTests(ConversationLifecycleState.Closed);
        return state;
    }

    private static ConversationState ArchivedState()
    {
        ConversationState state = CreatedState();
        state.ForceLifecycleForTests(ConversationLifecycleState.Archived);
        return state;
    }

    private static ParticipantAddedDomainEvent AddedEvent(PartyId party, ParticipantType type)
        => SingleParticipantAdded(ConversationAggregate.Handle(AddDomainCommand(party, type), CreatedState()));

    private static ParticipantAddedDomainEvent SingleParticipantAdded(DomainResult result)
        => result.Events.Single().ShouldBeOfType<ParticipantAddedDomainEvent>();

    private static ConversationRejectedDomainEvent SingleRejection(DomainResult result)
    {
        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        return result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
    }
}
