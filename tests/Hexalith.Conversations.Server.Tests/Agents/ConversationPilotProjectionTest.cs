// <copyright file="ConversationPilotProjectionTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Exercises pilot command execution, named projection persistence, and read freshness across clock offsets.</summary>
public sealed class ConversationPilotProjectionTest
{
    /// <summary>Both pilot writes are readable as Current at the first projection attempt.</summary>
    /// <param name="apiClockOffsetSeconds">The API clock's offset from the domain clock.</param>
    [Theory]
    [InlineData(-600)]
    [InlineData(0)]
    [InlineData(60)]
    public async Task PilotWritesReachCurrentAtAcceptedClockOffsets(int apiClockOffsetSeconds)
    {
        const string subject = "subject-one";
        const string binding = "Conversations:Pilot:HumanPartyBindings:0:";
        const string correlation = "pilot-clock-correlation";
        var fixture = new F();
        var clock = new PumpClock();
        var createMetadata = fixture.CommandMetadata(F.Human, "create");
        var appendMetadata = fixture.CommandMetadata(F.Human, "append");
        var id = new ConversationId(PilotCommandIdentity.Allocate(createMetadata, subject, "conversation"));
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [binding + "TenantId"] = F.Tenant.Value,
            [binding + "PartyId"] = F.Human.Value,
            [binding + "PrincipalId"] = subject,
        }).Build();
        var access = Substitute.For<IConversationTenantAccessService>();
        access.CheckAccessAsync(Arg.Any<ConversationTenantAccessRequirement>(), F.Tenant, subject,
            Arg.Any<TenantId?>(), Arg.Any<TenantId?>(), Arg.Any<TenantId?>(), Arg.Any<TenantId?>(), Arg.Any<TenantId?>(),
            Arg.Any<CancellationToken>()).Returns(call => ConversationTenantAccessDecision.Allowed(
                call.ArgAt<ConversationTenantAccessRequirement>(0), F.Tenant, subject));
        await using var provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(clock)
            .AddEventStore(typeof(ConversationAggregate).Assembly)
            .AddEventStoreDomainAdmissionStage(_ => new ConversationAgentAdmissionStage(
                fixture, fixture, fixture, fixture, new(settings, access), clock))
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var token = TestContext.Current.CancellationToken;
        var store = new InMemoryReadModelStore();
        var writer = new ConversationProjectionReadModelWriter(store);
        (await writer.InitializeTenantIndexAsync(F.Tenant, token)).ShouldBeTrue();
        var projection = new ConversationAsyncProjectionHandler(new(), writer, store, clock);
        var readStore = new ConversationProjectionReadStore(store);
        var queries = new ConversationQueryHandler(access, readStore, new(access, readStore),
            Substitute.For<IQueryCursorCodec>(), timeProvider: clock);
        var events = new List<ProjectionEventDto>();

        DateTimeOffset creationTime = clock.GetUtcNow();
        var create = new CreateHumanConversation(new(createMetadata, Label: "Pilot"),
            id, creationTime.AddSeconds(apiClockOffsetSeconds), PilotCommandIdentity.Allocate(createMetadata, subject, "created-event"));
        var creationEvent = await DispatchAndProjectAsync(create, null, "pilot-create");
        var created = JsonSerializer.Deserialize<ConversationCreatedDomainEvent>(creationEvent.Payload, F.Options).ShouldNotBeNull();
        created.Metadata.CommittedAt.ShouldBe(creationTime);
        (await ReadCurrentAsync(creationTime)).Detail.Messages.ShouldBeEmpty();

        var state = new ConversationState();
        state.Apply(created);
        clock.Advance(TimeSpan.FromSeconds(1));
        DateTimeOffset appendTime = clock.GetUtcNow();
        var append = new AppendMessage(new(appendMetadata, id, new("pilot-message"), F.Human, "Pilot message"),
            appendTime.AddSeconds(apiClockOffsetSeconds), PilotCommandIdentity.Allocate(appendMetadata, subject, "message-event", id.Value));
        var appendEvent = await DispatchAndProjectAsync(append, state, "pilot-append");
        var appended = JsonSerializer.Deserialize<MessageAppendedDomainEvent>(appendEvent.Payload, F.Options).ShouldNotBeNull();
        appended.Metadata.CommittedAt.ShouldBe(appendTime);
        var message = (await ReadCurrentAsync(appendTime)).Detail.Messages.ShouldHaveSingleItem();
        message.MessageId.ShouldBe(append.PublicCommand.MessageId);
        message.AuthorPartyId.ShouldBe(F.Human);
        message.Text.ShouldBe(append.PublicCommand.Text);

        async Task<DomainServiceWireEvent> DispatchAndProjectAsync<T>(T command, ConversationState? currentState, string dispatch)
        {
            CommandEnvelope envelope = fixture.Envelope(command, subject) with { AggregateId = id.Value, MessageId = dispatch };
            DomainServiceWireResult result = await DomainServiceRequestRouter.ProcessAsync(scope.ServiceProvider,
                new DomainServiceRequest(envelope, currentState), token);
            result.IsRejection.ShouldBeFalse();
            DomainServiceWireEvent persisted = result.Events.ShouldHaveSingleItem();
            events.Add(new(persisted.EventTypeName, persisted.Payload, persisted.SerializationFormat, events.Count + 1,
                clock.GetUtcNow(), correlation, dispatch, subject));
            var projected = await projection.ProjectAsync(new(F.Tenant.Value, envelope.Domain, id.Value, [.. events]), dispatch, token);
            projected.Status.ShouldBe(ProjectionDispatchStatus.Completed, projected.ReasonCode);
            return persisted;
        }

        async Task<ConversationProjectedReadModels> ReadCurrentAsync(DateTimeOffset occurredAt)
        {
            var detail = await queries.GetAsync(new(SchemaVersion.Current, F.Tenant, subject, correlation, id), token);
            var list = await queries.ListAsync(new(SchemaVersion.Current, F.Tenant, subject, correlation), token);
            detail.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            list.FreshnessState.ShouldBe(ProjectionTrustState.Current);
            var summary = list.Conversations.ShouldHaveSingleItem();
            summary.ConversationId.ShouldBe(id);
            summary.Freshness.ShouldBe(detail.Details.ShouldNotBeNull().Freshness);
            summary.Freshness.LastAppliedEventTimestamp.ShouldBe(occurredAt);
            return store.Snapshot<ConversationProjectedReadModels>(ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(F.Tenant, id)).ShouldNotBeNull();
        }
    }
}
