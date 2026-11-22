using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Contracts.Commands;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using Xunit;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

public sealed class ConversationPilotWriteTests
{
    [Fact]
    public async Task HumanAppendReplaysOneMessageAndRejectsChangedOrRestrictedIntent()
    {
        var fixture = new F();
        var original = new AppendMessage(new(fixture.CommandMetadata(F.Human), F.Conversation, new("message-one"),
            F.Human, "Private message"), F.At.AddSeconds(1), "append-one");
        fixture.Persist(ConversationAggregate.Handle(original, await fixture.ReplayAsync()));
        var state = await fixture.ReplayAsync();
        var persisted = state.Messages.ShouldHaveSingleItem();
        persisted.AuthorPartyId.ShouldBe(F.Human);
        persisted.Text.ShouldBe(original.PublicCommand.Text);
        persisted.MessageId.ShouldBe(original.PublicCommand.MessageId);
        persisted.Provenance.ShouldBeNull();
        ConversationAggregate.Handle(original with { PostedAt = original.PostedAt.AddSeconds(1) }, state).Events.ShouldBeEmpty();
        ConversationAggregate.Handle(original with { PublicCommand = original.PublicCommand with { Text = "changed" } },
            state).Events.ShouldHaveSingleItem().ShouldBeOfType<ConversationMessageConflict>();
        ConversationAggregate.Handle(original with { PublicCommand = original.PublicCommand with { AuthorPartyId = F.Agent } },
            state).Events.ShouldHaveSingleItem().ShouldBeOfType<ConversationWriteDenied>();
        ConversationAggregate.Handle(original with { PublicCommand = fixture.Posting.PublicCommand },
            state).Events.ShouldHaveSingleItem().ShouldBeOfType<ConversationRejectedDomainEvent>();
        state.Apply(new ConversationClosed(fixture.Metadata(ConversationEventType.ConversationClosed, F.Human, F.At.AddMinutes(1))));
        var second = original with { PublicCommand = original.PublicCommand with { Metadata = fixture.CommandMetadata(F.Human, "second"),
            MessageId = new("message-two") }, PostedAt = F.At.AddMinutes(2) };
        ConversationAggregate.Handle(second, state).Events.ShouldHaveSingleItem().ShouldBeOfType<ConversationRejectedDomainEvent>();
    }

    [Theory]
    [InlineData("Member", true)]
    [InlineData("Observer", false)]
    [InlineData("None", false)]
    public async Task HumanAuthorRequiresConversationMembershipAndWriteRole(string role, bool allowed)
    {
        var fixture = new F();
        PartyId member = new("member-party");
        var state = await fixture.ReplayAsync();
        if (role != "None")
            state.Apply(new ParticipantAddedDomainEvent(fixture.Metadata(ConversationEventType.ParticipantAdded, F.Human, F.At),
                member, ParticipantType.Human, ParticipantRole.Parse(role)));
        var request = new AppendMessage(new(fixture.CommandMetadata(member), F.Conversation, new("message-one"), member, "text",
            OperationTimestamp: F.At.AddYears(10)), F.At.AddSeconds(1), "event-one");
        var result = ConversationAggregate.Handle(request, state).Events.ShouldHaveSingleItem();
        if (!allowed) { result.ShouldBeOfType<ConversationWriteDenied>(); return; }
        var posted = result.ShouldBeOfType<MessageAppendedDomainEvent>();
        posted.Metadata.CommittedAt.ShouldBe(request.PostedAt);
        state.Apply(posted);
        ConversationAggregate.Handle(request with { PublicCommand = request.PublicCommand with { OperationTimestamp = F.At } },
            state).Events.ShouldHaveSingleItem().ShouldBeOfType<ConversationMessageConflict>();
        state.LastEventAt.ShouldBe(request.PostedAt);
    }

    [Fact]
    public async Task InternalCreateKeepsItsExistingAuthorityWhenPilotBindingsAreEnabled()
    {
        var fixture = new F();
        var metadata = fixture.CommandMetadata(F.Human);
        var authority = Substitute.For<IConversationTenantAccessService>();
        var pilot = new ConversationPilotWriteAdmission(new ConfigurationBuilder().AddInMemoryCollection().Build(), authority);
        var stage = new ConversationAgentAdmissionStage(fixture, fixture, fixture, fixture, pilot);
        var legacy = fixture.Envelope(new CreateConversation(new(metadata), F.Conversation, F.At, "legacy-event"), "human");
        var token = TestContext.Current.CancellationToken;
        (await stage.EvaluateAsync(new(new(legacy, null)), token)).IsRejected.ShouldBeFalse();
        (await stage.EvaluateAsync(new(new(legacy with { UserId = "subject-one" }, null)), token)).IsRejected.ShouldBeTrue();
        fixture.AuthorityCalls.ShouldBe(2);
        authority.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdmissionChecksBindingScopeAndCurrentWriteForEachOrdinaryCommand(bool create)
    {
        var fixture = new F();
        var metadata = fixture.CommandMetadata(F.Human);
        var allocatedId = new ConversationId(PilotCommandIdentity.Allocate(metadata, "subject-one", "conversation"));
        string appendEvent = PilotCommandIdentity.Allocate(metadata, "subject-one", "message-event", F.Conversation.Value);
        var occurrence = DateTimeOffset.UnixEpoch.AddYears(50);
        var envelope = create
            ? fixture.Envelope(new CreateHumanConversation(new(metadata), allocatedId, occurrence,
                PilotCommandIdentity.Allocate(metadata, "subject-one", "created-event")), "subject-one") with { AggregateId = allocatedId.Value }
            : fixture.Envelope(new AppendMessage(new(metadata, F.Conversation, new("msg"), F.Human, "text"), occurrence, appendEvent), "subject-one");
        var settings = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var access = Substitute.For<IConversationTenantAccessService>();
        var stage = new ConversationAgentAdmissionStage(fixture, fixture, fixture, fixture, new(settings, access));
        var token = TestContext.Current.CancellationToken;
        async Task<bool> Accepted(CommandEnvelope command) =>
            !(await stage.EvaluateAsync(new(new(command, null)), token)).IsRejected;

        (await Accepted(envelope)).ShouldBeFalse();
        const string binding = "Conversations:Pilot:HumanPartyBindings:0:";
        settings[binding + "TenantId"] = F.Tenant.Value;
        settings[binding + "PartyId"] = F.Human.Value;
        settings[binding + "PrincipalId"] = "subject-one";
        access.CheckAccessAsync(ConversationTenantAccessRequirement.Write, F.Tenant, "subject-one", commandTenantId: F.Tenant,
            cancellationToken: token).Returns(ConversationTenantAccessDecision.Allowed(ConversationTenantAccessRequirement.Write, F.Tenant, "subject-one"));
        (await Accepted(envelope)).ShouldBeTrue();
        var future = fixture.Envelope(new AppendMessage(new(metadata, F.Conversation, new("msg"), F.Human, "text"),
            DateTimeOffset.UtcNow.AddYears(10), appendEvent), "subject-one");
        (await Accepted(future)).ShouldBeFalse();
        var arbitraryEvent = fixture.Envelope(new AppendMessage(new(metadata, F.Conversation, new("msg"), F.Human, "text"),
            occurrence, "caller-event"), "subject-one");
        (await Accepted(arbitraryEvent)).ShouldBeFalse();
        access.CheckAccessAsync(ConversationTenantAccessRequirement.Write, F.Tenant, "subject-one", commandTenantId: F.Tenant,
            cancellationToken: token).Returns(new ConversationTenantAccessDecision(false, ConversationTenantAccessRequirement.Write,
                F.Tenant, "subject-one", ConversationTenantAccessDenialReason.TenantAccessUnavailable, true),
                ConversationTenantAccessDecision.Allowed(ConversationTenantAccessRequirement.Write, F.Tenant, "subject-one"));
        await Should.ThrowAsync<ConversationWriteAuthorityUnavailableException>(() => Accepted(envelope));
        (await Accepted(envelope)).ShouldBeTrue();
        (await Accepted(envelope with { AggregateId = "foreign-conversation" })).ShouldBeFalse();
        (await Accepted(envelope with { UserId = "foreign-subject" })).ShouldBeFalse();
        (await Accepted(envelope with { TenantId = "foreign-tenant" })).ShouldBeFalse();
        access.CheckAccessAsync(ConversationTenantAccessRequirement.Write, F.Tenant, "subject-one", commandTenantId: F.Tenant,
            cancellationToken: token).Returns(new ConversationTenantAccessDecision(false, ConversationTenantAccessRequirement.Write,
                F.Tenant, "subject-one", ConversationTenantAccessDenialReason.MissingMember, false));
        (await Accepted(envelope)).ShouldBeFalse();
        fixture.AuthorityCalls.ShouldBe(0);
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(token);
        access.CheckAccessAsync(ConversationTenantAccessRequirement.Write, F.Tenant, "subject-one", commandTenantId: F.Tenant,
            cancellationToken: canceled.Token).Returns<ConversationTenantAccessDecision>(_ =>
            {
                canceled.Cancel();
                throw new IOException("Temporary provider fault during cancellation.");
            });
        await Should.ThrowAsync<OperationCanceledException>(() => stage.EvaluateAsync(new(new(envelope, null)), canceled.Token));
    }
}
