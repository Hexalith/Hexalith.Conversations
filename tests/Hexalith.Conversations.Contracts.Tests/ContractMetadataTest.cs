// <copyright file="ContractMetadataTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies tenant, version, actor, correlation, causation, and idempotency metadata presence.
/// </summary>
public sealed class ContractMetadataTest
{
    /// <summary>
    /// Ensures mutating commands carry the required envelope metadata.
    /// </summary>
    [Fact]
    public void CommandContractsShouldCarryRequiredMetadata()
    {
        Type[] commandTypes =
        [
            typeof(CreateConversationCommand),
            typeof(AppendMessageCommand),
            typeof(AddParticipantCommand),
            typeof(AttachFileReferenceCommand),
            typeof(UpdateConversationMetadataCommand),
            typeof(CloseConversationCommand),
            typeof(ArchiveConversationCommand),
        ];

        foreach (Type commandType in commandTypes)
        {
            commandType.GetProperty(nameof(CreateConversationCommand.Metadata))
                .ShouldNotBeNull($"{commandType.Name}.{nameof(CreateConversationCommand.Metadata)}");
        }

        PropertyInfo[] metadataProperties = typeof(ConversationCommandMetadata).GetProperties();
        metadataProperties.Select(p => p.Name).ShouldBe(
            [
                "SchemaVersion",
                "TenantId",
                "ActorPartyId",
                "CorrelationId",
                "CausationId",
                "IdempotencyKey",
            ],
            ignoreOrder: true);

        AssertPropertyType<ConversationCommandMetadata, SchemaVersion>(nameof(ConversationCommandMetadata.SchemaVersion));
        AssertPropertyType<ConversationCommandMetadata, TenantId>(nameof(ConversationCommandMetadata.TenantId));
        AssertPropertyType<ConversationCommandMetadata, PartyId>(nameof(ConversationCommandMetadata.ActorPartyId));
        AssertPropertyType<ConversationCommandMetadata, string>(nameof(ConversationCommandMetadata.CorrelationId));
        AssertPropertyType<ConversationCommandMetadata, string?>(nameof(ConversationCommandMetadata.CausationId));
        AssertPropertyType<ConversationCommandMetadata, string?>(nameof(ConversationCommandMetadata.IdempotencyKey));
    }

    /// <summary>
    /// Ensures event contracts carry required public event metadata without substrate details.
    /// </summary>
    [Fact]
    public void EventContractsShouldCarryRequiredMetadata()
    {
        Type[] eventTypes =
        [
            typeof(ConversationCreated),
            typeof(MessageAppended),
            typeof(ParticipantAdded),
            typeof(FileReferenceAttached),
            typeof(ConversationMetadataUpdated),
            typeof(ConversationClosed),
            typeof(ConversationArchived),
        ];

        foreach (Type eventType in eventTypes)
        {
            eventType.GetProperty(nameof(ConversationCreated.Metadata))
                .ShouldNotBeNull($"{eventType.Name}.{nameof(ConversationCreated.Metadata)}");
        }

        typeof(ConversationEventMetadata).GetProperties().Select(p => p.Name).ShouldBe(
            [
                "SchemaVersion",
                "EventId",
                "EventType",
                "TenantId",
                "ConversationId",
                "CorrelationId",
                "CommittedAt",
                "ActorPartyId",
                "CausationId",
            ],
            ignoreOrder: true);

        AssertPropertyType<ConversationEventMetadata, SchemaVersion>(nameof(ConversationEventMetadata.SchemaVersion));
        AssertPropertyType<ConversationEventMetadata, string>(nameof(ConversationEventMetadata.EventId));
        AssertPropertyType<ConversationEventMetadata, ConversationEventType>(nameof(ConversationEventMetadata.EventType));
        AssertPropertyType<ConversationEventMetadata, TenantId>(nameof(ConversationEventMetadata.TenantId));
        AssertPropertyType<ConversationEventMetadata, ConversationId>(nameof(ConversationEventMetadata.ConversationId));
        AssertPropertyType<ConversationEventMetadata, string>(nameof(ConversationEventMetadata.CorrelationId));
        AssertPropertyType<ConversationEventMetadata, DateTimeOffset>(nameof(ConversationEventMetadata.CommittedAt));
        AssertPropertyType<ConversationEventMetadata, PartyId?>(nameof(ConversationEventMetadata.ActorPartyId));
        AssertPropertyType<ConversationEventMetadata, string?>(nameof(ConversationEventMetadata.CausationId));

        AssertPropertyType<ConversationCommandAcceptedResult, ConversationCommandType>(
            nameof(ConversationCommandAcceptedResult.CommandType));
        AssertPropertyType<ConversationCreatedResult, ConversationCommandType>(
            nameof(ConversationCreatedResult.CommandType));
    }

    private static void AssertPropertyType<TContract, TProperty>(string propertyName)
    {
        PropertyInfo? property = typeof(TContract).GetProperty(propertyName);

        property.ShouldNotBeNull($"{typeof(TContract).Name}.{propertyName}");
        property.PropertyType.ShouldBe(typeof(TProperty), $"{typeof(TContract).Name}.{propertyName}");
    }
}
