// <copyright file="ConversationGatewayCommandAuthorityTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Verifies the live fixture authority cannot grant another principal, operation, or scope.</summary>
public sealed class ConversationGatewayCommandAuthorityTests
{
    /// <summary>Checks complete human evidence accepted by the production command admission check.</summary>
    [Fact]
    public async Task EnrolledCommandShouldProvideExactHumanAuthority()
    {
        ConversationGatewayCommandAuthority authority = new();
        authority.Enroll("tenant-gateway-001", "conversation-one");
        TenantId tenant = new("tenant-gateway-001");
        ConversationAgentAuthorization result = await authority.AuthorizeAsync("party-gateway-actor", tenant,
            new ConversationId("conversation-one"), "GeneralCommand", TestContext.Current.CancellationToken);

        ConversationAgentQueryService.CheckAuthority(result, "party-gateway-actor", tenant, false)
            .ShouldBe(ConversationAgentsOutcome.Available);
        result.PartyId.ShouldBe(new PartyId("party-gateway-actor"));
        result.OrganizationIdentityConfirmed.ShouldBeFalse();
        result.AuthorityRevision.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>Checks wrong identities, missing enrollment, cross-tenant access, and Agent operations fail closed.</summary>
    /// <param name="principal">The requested authenticated principal.</param>
    /// <param name="tenant">The requested tenant.</param>
    /// <param name="conversation">The requested optional conversation.</param>
    /// <param name="operation">The exact requested operation.</param>
    [Theory]
    [InlineData("other-party", "tenant-gateway-001", "conversation-one", "GeneralCommand")]
    [InlineData("party-gateway-actor", "tenant-gateway-002", "conversation-one", "GeneralCommand")]
    [InlineData("party-gateway-actor", "unconfigured-tenant", "conversation-one", "GeneralCommand")]
    [InlineData("party-gateway-actor", "tenant-gateway-001", "conversation-two", "GeneralCommand")]
    [InlineData("party-gateway-actor", "tenant-gateway-001", null, "GeneralCommand")]
    [InlineData("party-gateway-actor", "tenant-gateway-001", "conversation-one", "ReadConversation")]
    public async Task OtherRequestsShouldBeDenied(string principal, string tenant, string? conversation, string operation)
    {
        ConversationGatewayCommandAuthority authority = new();
        authority.Enroll("tenant-gateway-001", "conversation-one");
        ConversationAgentAuthorization result = await authority.AuthorizeAsync(principal, new TenantId(tenant),
            conversation is null ? null : new ConversationId(conversation), operation, TestContext.Current.CancellationToken);

        result.Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        result.PartyId.ShouldBeNull();
    }
}
