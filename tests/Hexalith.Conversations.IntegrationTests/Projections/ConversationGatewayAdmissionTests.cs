// <copyright file="ConversationGatewayAdmissionTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Testing.Builders;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Checks a denied live command records its rejection audit without creating conversation business state.</summary>
[Collection(ConversationGatewayLiveCollection.Name)]
public sealed class ConversationGatewayAdmissionTests(ConversationGatewayLiveFixture fixture)
{
    /// <summary>Rejects another principal through the actual aggregate actor and domain-service admission stage.</summary>
    [Fact]
    public async Task UnauthorisedPrincipalShouldNotPersistConversation()
    {
        fixture.RequireAvailable();
        const string tenant = "tenant-gateway-001";
        string conversation = $"denied-conversation-{Guid.NewGuid():N}";
        CreateConversation command = new(
            new CreateConversationCommand(new ConversationCommandMetadata(SchemaVersion.Current,
                new TenantId(tenant), new PartyId("other-party"), $"correlation-{conversation}",
                $"causation-{conversation}", $"idempotency-{conversation}"), Label: "Denied append"),
            new ConversationId(conversation), DateTimeOffset.UtcNow, $"event-{conversation}");
        CommandEnvelope envelope = new CommandEnvelopeBuilder()
            .WithTenantId(tenant)
            .WithDomain(ConversationProjectionHandler.ConversationDomain)
            .WithAggregateId(conversation)
            .WithCommandType(nameof(CreateConversation))
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(command))
            .WithUserId("other-party")
            .Build();
        IAggregateActor aggregate = fixture.CreateAggregateActor(tenant, ConversationProjectionHandler.ConversationDomain, conversation);
        CommandProcessingResult result = await aggregate.ProcessCommandAsync(envelope);

        result.Accepted.ShouldBeFalse();
        result.EventCount.ShouldBe(1);
        var audit = (await aggregate.GetEventsAsync(0)).ShouldHaveSingleItem();
        audit.EventTypeName.ShouldContain(typeof(ConversationRejectedDomainEvent).FullName!);
        result.RejectionEventType.ShouldBe(audit.EventTypeName);
        ConversationRejectedDomainEvent? rejection = JsonSerializer.Deserialize<ConversationRejectedDomainEvent>(
            audit.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        rejection.ShouldNotBeNull();
        rejection!.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("agents-Denied");
        (await aggregate.GetCurrentSequenceAsync()).ShouldBe(1);
        static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        IReadModelStore store = fixture.Services.GetRequiredService<IReadModelStore>();
        ReadModelEntry<ConversationProjectedReadModels> projected = await store.GetAsync<ConversationProjectedReadModels>(
            ConversationGatewayLiveFixture.StateStoreName, $"projection:conversations:{Encode(tenant)}:{Encode(conversation)}",
            TestContext.Current.CancellationToken);
        projected.Value.ShouldBeNull();
    }
}
