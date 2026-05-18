// <copyright file="ContractMetadataTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;

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
            commandType.GetProperty(nameof(CreateConversationCommand.Metadata)).ShouldNotBeNull(commandType.Name);
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
            eventType.GetProperty(nameof(ConversationCreated.Metadata)).ShouldNotBeNull(eventType.Name);
        }

        typeof(ConversationEventMetadata).GetProperties().Select(p => p.Name).ShouldBe(
            [
                "SchemaVersion",
                "EventType",
                "TenantId",
                "ConversationId",
                "ActorPartyId",
                "CorrelationId",
                "CausationId",
                "CommittedAt",
            ],
            ignoreOrder: true);
    }
}
