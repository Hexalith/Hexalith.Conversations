using System.Security.Claims;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Server.Api;
using Microsoft.AspNetCore.Http;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Exercises the participant handler against serialized local source state.</summary>
public sealed class ConversationAgentParticipantApiHandlerTests
{
    /// <summary>Only the exact current service Party reaches the admitted gateway and one persisted member remains after retry.</summary>
    [Fact]
    public async Task ExactMembershipPersistsOnceAndForeignOrChangedIntentDoesNotDispatch()
    {
        var source = new ConversationAgentLocalFixture();
        var gateway = new ConversationAgentApiClientFixture(source);
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, "agents-service"),
                    new Claim(ConversationReadApi.TenantIdClaimType, ConversationAgentLocalFixture.Tenant.Value),
                ], "test")),
            },
        };
        var handler = new ConversationAgentParticipantApiHandler(source, gateway, accessor);
        var command = source.Membership.PublicCommand with { OperationTimestamp = ConversationAgentLocalFixture.At.AddMinutes(1) };

        var first = await handler.AddParticipantAsync(command, TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(StatusCodes.Status202Accepted);
        first.Value!.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        first.Value.Persisted.ShouldBeFalse();
        (await source.ReplayAsync()).Participants.Count.ShouldBe(1);

        var retry = await handler.AddParticipantAsync(command, TestContext.Current.CancellationToken);
        retry.Value!.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        (await source.ReplayAsync()).Participants.Count.ShouldBe(1);
        gateway.MembershipCalls.ShouldBe(2);

        var changedRole = await handler.AddParticipantAsync(command with { ParticipantRole = ParticipantRole.Facilitator },
            TestContext.Current.CancellationToken);
        changedRole.Value!.Outcome.ShouldBe(ConversationAgentsOutcome.Conflict);
        gateway.MembershipCalls.ShouldBe(2);

        var foreign = await handler.AddParticipantAsync(command with
        {
            Metadata = new ConversationCommandMetadata(command.Metadata.SchemaVersion, new TenantId("foreign"),
                command.Metadata.ActorPartyId, command.Metadata.CorrelationId,
                command.Metadata.CausationId, command.Metadata.IdempotencyKey),
        }, TestContext.Current.CancellationToken);
        foreign.Value!.Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        gateway.MembershipCalls.ShouldBe(2);

        source.Deny = true;
        var revoked = await handler.AddParticipantAsync(command, TestContext.Current.CancellationToken);
        revoked.Value!.Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        gateway.MembershipCalls.ShouldBe(2);
    }
}
