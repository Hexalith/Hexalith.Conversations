// <copyright file="ConversationAggregateCreateTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Aggregates;

/// <summary>
/// Verifies tenant-safe creation behavior for the conversation aggregate.
/// </summary>
public sealed class ConversationAggregateCreateTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 18, 12, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// A valid create command emits one versioned conversation-created event.
    /// </summary>
    [Fact]
    public void ValidCreateShouldEmitOneConversationCreatedEvent()
    {
        CreateConversation command = CreateDomainCommand();

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);

        ConversationCreatedDomainEvent created = result.Events.Single().ShouldBeOfType<ConversationCreatedDomainEvent>();
        created.Metadata.SchemaVersion.ShouldBe(SchemaVersion.Current);
        created.Metadata.EventId.ShouldBe("event-create-alpha");
        created.Metadata.EventType.ShouldBe(ConversationEventType.ConversationCreated);
        created.Metadata.TenantId.ShouldBe(Tenant);
        created.Metadata.ConversationId.ShouldBe(Conversation);
        created.Metadata.ActorPartyId.ShouldBe(Actor);
        created.Metadata.CorrelationId.ShouldBe("correlation-alpha");
        created.Metadata.CausationId.ShouldBe("causation-alpha");
        created.Metadata.CommittedAt.ShouldBe(CreatedAt);
        created.CreatedAt.ShouldBe(CreatedAt);
        created.IdempotencyKey.ShouldBe("idempotency-alpha");
        created.BusinessReference.ShouldBe(new BusinessReference("crm", "case-123"));
        created.ProjectId.ShouldBe(new ProjectId("project-alpha"));
        created.FolderId.ShouldBe(new FolderId("folder-alpha"));
        created.Label.ShouldBe("Support case");
        created.ProviderCorrelation.ShouldNotBeNull();
        created.ProviderCorrelation.ProviderSessionReference.ShouldBe("provider-session-77");
        created.Metadata.ConversationId.Value.ShouldNotBe(created.ProviderCorrelation.ProviderSessionReference);
    }

    /// <summary>
    /// Replaying the same ordered event history produces the same conversation state.
    /// </summary>
    [Fact]
    public void ConversationCreatedShouldReplayDeterministically()
    {
        ConversationCreatedDomainEvent created = CreatedEvent();

        ConversationState first = new();
        ConversationState second = new();

        first.Apply(created);
        second.Apply(created);

        first.ShouldBe(second);
        first.IsCreated.ShouldBeTrue();
        first.Lifecycle.ShouldBe(ConversationLifecycleState.Open);
        first.TenantId.ShouldBe(Tenant);
        first.ConversationId.ShouldBe(Conversation);
        first.CreatorPartyId.ShouldBe(Actor);
        first.CreatedAt.ShouldBe(CreatedAt);
        first.SchemaVersion.ShouldBe(SchemaVersion.Current);
        first.IdempotencyKey.ShouldBe("idempotency-alpha");
        first.BusinessReference.ShouldBe(new BusinessReference("crm", "case-123"));
        first.ProviderCorrelation.ShouldNotBeNull();
        first.ProviderCorrelation.ProviderResponseReference.ShouldBe("provider-response-88");
    }

    /// <summary>
    /// Replaying a second ConversationCreatedDomainEvent against already-created state surfaces a deterministic invariant violation.
    /// </summary>
    [Fact]
    public void ReplayingDuplicateConversationCreatedShouldThrowReplayInvariantViolation()
    {
        ConversationCreatedDomainEvent created = CreatedEvent();
        ConversationState state = new();
        state.Apply(created);

        Should.Throw<InvalidOperationException>(() => state.Apply(created));
    }

    /// <summary>
    /// Creating an already-created conversation is rejected without a success event.
    /// </summary>
    [Fact]
    public void DuplicateCreateShouldReturnTypedRejection()
    {
        CreateConversation command = CreateDomainCommand();
        ConversationState state = new();
        state.Apply(CreatedEvent());

        DomainResult result = ConversationAggregate.Handle(command, state);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        rejection.ReasonCode.ShouldBe("conversation_already_created");
    }

    /// <summary>
    /// A null domain command fails closed with a typed rejection.
    /// </summary>
    [Fact]
    public void NullDomainCommandShouldReturnTypedRejection()
    {
        DomainResult result = ConversationAggregate.Handle(command: null!, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("command_missing");
    }

    /// <summary>
    /// A null public command fails closed with a typed rejection.
    /// </summary>
    [Fact]
    public void NullPublicCommandShouldReturnTypedRejection()
    {
        CreateConversation command = new(
            PublicCommand: null!,
            ConversationId: Conversation,
            CreatedAt: CreatedAt,
            EventId: "event-create-alpha");

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("public_command_missing");
    }

    /// <summary>
    /// Missing command metadata is reported as metadata_missing (distinct from tenant_binding_missing).
    /// </summary>
    [Fact]
    public void MissingCommandMetadataShouldReturnMetadataMissingRejection()
    {
        CreateConversation command = new(
            PublicCommand: new CreateConversationCommand(Metadata: null!),
            ConversationId: Conversation,
            CreatedAt: CreatedAt,
            EventId: "event-create-alpha");

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("metadata_missing");
    }

    /// <summary>
    /// Unsupported schema versions are rejected with a stable machine-readable code.
    /// </summary>
    [Fact]
    public void UnsupportedSchemaVersionShouldReturnTypedRejection()
    {
        CreateConversation command = CreateDomainCommand(schemaVersion: new SchemaVersion(SchemaVersion.Current.Value + 1));

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        rejection.ReasonCode.ShouldBe("unsupported_schema_version");
    }

    /// <summary>
    /// Missing conversation identity fails closed with a typed rejection.
    /// </summary>
    [Fact]
    public void MissingConversationIdentityShouldReturnTypedRejection()
    {
        CreateConversation valid = CreateDomainCommand();
        CreateConversation command = valid with { ConversationId = null };

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("conversation_identity_missing");
    }

    /// <summary>
    /// Missing or whitespace event identity fails closed with a typed rejection.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingEventIdentityShouldReturnTypedRejection(string eventId)
    {
        CreateConversation valid = CreateDomainCommand();
        CreateConversation command = valid with { EventId = eventId };

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("event_identity_missing");
    }

    /// <summary>
    /// Creation timestamps outside the business range fail closed with a typed rejection.
    /// </summary>
    [Fact]
    public void CreatedAtBeforeBusinessRangeShouldReturnTypedRejection()
    {
        CreateConversation valid = CreateDomainCommand();
        CreateConversation command = valid with { CreatedAt = new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.Zero) };

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("created_timestamp_invalid");
    }

    /// <summary>
    /// Provider and external references cannot replace the internal conversation identity.
    /// </summary>
    [Theory]
    [InlineData("provider-session-77")]
    [InlineData("provider-response-88")]
    [InlineData("case-123")]
    [InlineData("crm")]
    [InlineData("contoso-ai")]
    [InlineData("assistant")]
    [InlineData("thread-42")]
    [InlineData("thread")]
    [InlineData("Support case")]
    [InlineData("event-create-alpha")]
    [InlineData("idempotency-alpha")]
    [InlineData("correlation-alpha")]
    [InlineData("causation-alpha")]
    [InlineData("project-alpha")]
    [InlineData("folder-alpha")]
    public void ProviderAndExternalReferencesShouldNotReplaceConversationIdentity(string substitutedIdentity)
    {
        CreateConversation command = CreateDomainCommand(conversationId: new ConversationId(substitutedIdentity));

        DomainResult result = ConversationAggregate.Handle(command, state: null);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("identity_substitution_forbidden");
    }

    private static CreateConversation CreateDomainCommand(
        SchemaVersion? schemaVersion = null,
        ConversationId? conversationId = null)
    {
        ConversationCommandMetadata metadata = new(
            schemaVersion ?? SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-alpha",
            CausationId: "causation-alpha",
            IdempotencyKey: "idempotency-alpha");

        CreateConversationCommand publicCommand = new(
            metadata,
            new BusinessReference("crm", "case-123"),
            new ProjectId("project-alpha"),
            new FolderId("folder-alpha"),
            "Support case",
            new ProviderCorrelationMetadata(
                "contoso-ai",
                "assistant",
                SchemaVersion.Current,
                ProviderSessionReference: "provider-session-77",
                ProviderResponseReference: "provider-response-88",
                ExtensionData: new Dictionary<string, string> { ["thread"] = "thread-42" }));

        return new CreateConversation(
            publicCommand,
            conversationId ?? Conversation,
            CreatedAt,
            "event-create-alpha");
    }

    private static ConversationCreatedDomainEvent CreatedEvent()
        => SingleCreated(ConversationAggregate.Handle(CreateDomainCommand(), state: null));

    private static ConversationCreatedDomainEvent SingleCreated(DomainResult result)
        => result.Events.Single().ShouldBeOfType<ConversationCreatedDomainEvent>();

    private static ConversationRejectedDomainEvent SingleRejection(DomainResult result)
    {
        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        return result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
    }
}
