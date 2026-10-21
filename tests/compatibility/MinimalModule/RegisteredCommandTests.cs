using System.Text.Json;
using Hexalith.Conversations.MinimalModule.Contracts;
using Hexalith.Conversations.MinimalModule.Server;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.MinimalModule.Tests;

/// <summary>Checks command registration against the platform without changing the measured fixture.</summary>
public sealed class RegisteredCommandTests
{
    /// <summary>Dispatches through the registered domain key and verifies the emitted state and replay no-op.</summary>
    /// <returns>A task representing the command and replay assertions.</returns>
    [Fact]
    public async Task RegisteredCommandProducesCreatedEventAndIsIdempotentAfterReplay()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.AddMinimalConversationModule();
        await using WebApplication app = builder.Build();
        _ = app.UseEventStoreDomainService();
        using IServiceScope scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredKeyedService<IAsyncDomainProcessor>(CreateConversation.Domain)
            .ShouldBeOfType<ConversationAggregate>();

        CreateConversation command = new("registered-conversation", "Registered command");
        CommandEnvelope envelope = new(
            MessageId: "registered-create-1",
            TenantId: "tenant-a",
            Domain: CreateConversation.Domain,
            AggregateId: command.AggregateId,
            CommandType: CreateConversation.CommandType,
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "registered-correlation-1",
            CausationId: null,
            UserId: "user-1",
            Extensions: null);

        DomainServiceWireResult first = await DomainServiceRequestRouter.ProcessAsync(
            scope.ServiceProvider, new DomainServiceRequest(envelope, null), TestContext.Current.CancellationToken);

        first.IsRejection.ShouldBeFalse();
        DomainServiceWireEvent emitted = first.Events.ShouldHaveSingleItem();
        emitted.EventTypeName.ShouldBe(typeof(ConversationCreated).FullName);
        emitted.SerializationFormat.ShouldBe("json");
        ConversationCreated created = JsonSerializer.Deserialize<ConversationCreated>(emitted.Payload).ShouldNotBeNull();
        created.ShouldBe(new ConversationCreated(command.ConversationId, command.Title));
        ConversationState state = new();
        state.Apply(created);
        state.Created.ShouldBeTrue();
        state.Title.ShouldBe(command.Title);

        using IServiceScope nextRequestScope = app.Services.CreateScope();
        DomainServiceWireResult replay = await DomainServiceRequestRouter.ProcessAsync(
            nextRequestScope.ServiceProvider,
            new DomainServiceRequest(envelope with { MessageId = "registered-create-2" }, state),
            TestContext.Current.CancellationToken);

        replay.IsRejection.ShouldBeFalse();
        replay.Events.ShouldBeEmpty();
        state.Created.ShouldBeTrue();
        state.Title.ShouldBe(command.Title);
    }
}
