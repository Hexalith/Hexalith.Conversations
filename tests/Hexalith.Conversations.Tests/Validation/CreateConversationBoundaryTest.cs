// <copyright file="CreateConversationBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Validation;
using Hexalith.EventStore.Contracts.Results;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Validation;

/// <summary>
/// Verifies the narrow deterministic create-conversation dispatch boundary.
/// </summary>
public sealed class CreateConversationBoundaryTest
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 18, 13, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The boundary maps the public create command into aggregate creation.
    /// </summary>
    [Fact]
    public void DispatchShouldMapPublicCreateCommandIntoConversationCreatedEvent()
    {
        CreateConversationCommand command = PublicCommand();
        ConversationId conversationId = new("conversation-boundary");

        DomainResult result = CreateConversationBoundary.Dispatch(
            command,
            conversationId,
            CreatedAt,
            "event-boundary");

        result.IsSuccess.ShouldBeTrue();
        ConversationCreated created = result.Events.Single().ShouldBeOfType<ConversationCreated>();
        created.Metadata.TenantId.ShouldBe(command.Metadata.TenantId);
        created.Metadata.ActorPartyId.ShouldBe(command.Metadata.ActorPartyId);
        created.Metadata.ConversationId.ShouldBe(conversationId);
        created.Metadata.CommittedAt.ShouldBe(CreatedAt);
    }

    /// <summary>
    /// Null commands fail closed without emitting a success event.
    /// </summary>
    [Fact]
    public void DispatchShouldRejectNullCommand()
    {
        DomainResult result = CreateConversationBoundary.Dispatch(
            command: null,
            new ConversationId("conversation-boundary"),
            CreatedAt,
            "event-boundary");

        ConversationRejected rejection = result.Events.Single().ShouldBeOfType<ConversationRejected>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("command_missing");
    }

    /// <summary>
    /// Malformed boundary metadata fails closed without aggregate side effects.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DispatchShouldRejectMalformedEventMetadata(string eventId)
    {
        DomainResult result = CreateConversationBoundary.Dispatch(
            PublicCommand(),
            new ConversationId("conversation-boundary"),
            CreatedAt,
            eventId);

        ConversationRejected rejection = result.Events.Single().ShouldBeOfType<ConversationRejected>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("event_identity_missing");
    }

    private static CreateConversationCommand PublicCommand()
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                new TenantId("tenant-boundary"),
                new PartyId("party-boundary"),
                "correlation-boundary",
                IdempotencyKey: "idempotency-boundary"),
            BusinessReference: new BusinessReference("support", "ticket-456"),
            ProviderCorrelation: new ProviderCorrelationMetadata(
                "contoso-ai",
                "assistant",
                SchemaVersion.Current,
                ProviderSessionReference: "provider-session-boundary"));
}
