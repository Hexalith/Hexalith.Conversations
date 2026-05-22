// <copyright file="ConversationPublicationConsumerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Server.Publication;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Publication;

/// <summary>
/// Verifies documented consumer idempotency semantics with duplicate/replayed/reordered events.
/// </summary>
public sealed class ConversationPublicationConsumerTest
{
    /// <summary>
    /// Ensures duplicate and replayed deliveries with the same stable identity create one effect.
    /// </summary>
    [Fact]
    public void FakeConsumerShouldDeduplicateByTenantConversationEventAndVersion()
    {
        LocalConversationPublicationConsumer consumer = new(PublicationSamples.Tenant);
        ParticipantAdded first = new(PublicationSamples.ParticipantMetadata, PublicationSamples.Participant, ParticipantType.Human, ParticipantRole.Member);
        ParticipantAdded duplicate = new(PublicationSamples.ParticipantMetadata, PublicationSamples.Participant, ParticipantType.Human, ParticipantRole.Member);
        ParticipantAdded reordered = new(
            PublicationSamples.ParticipantMetadata with
            {
                EventId = "event-000",
            },
            new PartyId("party-late"),
            ParticipantType.Human,
            ParticipantRole.Member);

        consumer.TryApply(first).ShouldBeTrue();
        consumer.TryApply(duplicate).ShouldBeFalse();
        consumer.TryApply(reordered).ShouldBeTrue();

        consumer.AppliedEffectCount.ShouldBe(2);
        consumer.ParticipantIds.ShouldBe(["party-late", "party-participant"], ignoreOrder: true);
    }

    /// <summary>
    /// Ensures tenant mismatches are rejected before local state mutation.
    /// </summary>
    [Fact]
    public void FakeConsumerShouldRejectTenantMismatchBeforeMutation()
    {
        LocalConversationPublicationConsumer consumer = new(PublicationSamples.Tenant);
        ParticipantAdded wrongTenant = new(
            PublicationSamples.ParticipantMetadata with
            {
                TenantId = new TenantId("other-tenant"),
            },
            PublicationSamples.Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

        consumer.TryApply(wrongTenant).ShouldBeFalse();

        consumer.AppliedEffectCount.ShouldBe(0);
    }
}
