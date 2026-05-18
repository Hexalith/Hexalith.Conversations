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

        AssertNonNullableProperty<ConversationCommandMetadata, SchemaVersion>(nameof(ConversationCommandMetadata.SchemaVersion));
        AssertNonNullableProperty<ConversationCommandMetadata, TenantId>(nameof(ConversationCommandMetadata.TenantId));
        AssertNonNullableProperty<ConversationCommandMetadata, PartyId>(nameof(ConversationCommandMetadata.ActorPartyId));
        AssertNonNullableProperty<ConversationCommandMetadata, string>(nameof(ConversationCommandMetadata.CorrelationId));
        AssertNullableReferenceProperty<ConversationCommandMetadata, string>(nameof(ConversationCommandMetadata.CausationId));
        AssertNullableReferenceProperty<ConversationCommandMetadata, string>(nameof(ConversationCommandMetadata.IdempotencyKey));
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

        AssertNonNullableProperty<ConversationEventMetadata, SchemaVersion>(nameof(ConversationEventMetadata.SchemaVersion));
        AssertNonNullableProperty<ConversationEventMetadata, string>(nameof(ConversationEventMetadata.EventId));
        AssertNonNullableProperty<ConversationEventMetadata, ConversationEventType>(nameof(ConversationEventMetadata.EventType));
        AssertNonNullableProperty<ConversationEventMetadata, TenantId>(nameof(ConversationEventMetadata.TenantId));
        AssertNonNullableProperty<ConversationEventMetadata, ConversationId>(nameof(ConversationEventMetadata.ConversationId));
        AssertNonNullableProperty<ConversationEventMetadata, string>(nameof(ConversationEventMetadata.CorrelationId));
        AssertNonNullableProperty<ConversationEventMetadata, DateTimeOffset>(nameof(ConversationEventMetadata.CommittedAt));
        AssertNonNullableProperty<ConversationEventMetadata, PartyId>(nameof(ConversationEventMetadata.ActorPartyId));
        AssertNullableReferenceProperty<ConversationEventMetadata, string>(nameof(ConversationEventMetadata.CausationId));

        AssertNonNullableProperty<ConversationCommandAcceptedResult, ConversationCommandType>(
            nameof(ConversationCommandAcceptedResult.CommandType));
        AssertNonNullableProperty<ConversationCreatedResult, ConversationCommandType>(
            nameof(ConversationCreatedResult.CommandType));
    }

    private static void AssertNonNullableProperty<TContract, TProperty>(string propertyName)
    {
        PropertyInfo property = typeof(TContract).GetProperty(propertyName)
            ?? throw new Xunit.Sdk.XunitException($"{typeof(TContract).Name}.{propertyName} not found.");

        property.PropertyType.ShouldBe(typeof(TProperty), $"{typeof(TContract).Name}.{propertyName} property type.");

        if (!typeof(TProperty).IsValueType)
        {
            NullabilityInfo nullability = new NullabilityInfoContext().Create(property);
            nullability.ReadState.ShouldBe(NullabilityState.NotNull, $"{typeof(TContract).Name}.{propertyName} must be declared non-nullable.");
        }
    }

    private static void AssertNullableReferenceProperty<TContract, TProperty>(string propertyName)
        where TProperty : class
    {
        PropertyInfo property = typeof(TContract).GetProperty(propertyName)
            ?? throw new Xunit.Sdk.XunitException($"{typeof(TContract).Name}.{propertyName} not found.");

        property.PropertyType.ShouldBe(typeof(TProperty), $"{typeof(TContract).Name}.{propertyName} property type.");

        NullabilityInfo nullability = new NullabilityInfoContext().Create(property);
        nullability.ReadState.ShouldBe(NullabilityState.Nullable, $"{typeof(TContract).Name}.{propertyName} must be declared nullable.");
    }
}
