// <copyright file="ConversationTransportMetadataTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Server.Publication;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Publication;

/// <summary>
/// Verifies transport-visible publication metadata stays bounded and Conversations-owned.
/// </summary>
public sealed class ConversationTransportMetadataTest
{
    /// <summary>
    /// Ensures CloudEvent/topic/header style metadata excludes unsafe source values and payload fragments.
    /// </summary>
    [Fact]
    public void TransportMetadataShouldContainOnlyBoundedIdentifiers()
    {
        ConversationCreated e = new(
            PublicationSamples.CreatedMetadata,
            PublicationSamples.Business,
            PublicationSamples.Project,
            PublicationSamples.Folder,
            "Contains forbidden sentinel provider-token raw upstream party@example.com",
            PublicationSamples.ProviderCorrelation);

        ConversationTransportMetadata metadata = ConversationTransportMetadata.FromEvent(e);

        metadata.Topic.ShouldBe("tenant-001.conversations.events");
        metadata.Type.ShouldBe("Hexalith.Conversations.ConversationCreated.v1");
        metadata.Source.ShouldBe("hexalith-conversations/tenant-001");
        metadata.Subject.ShouldBe("conversations/conversation-001/events/event-001");
        metadata.Headers.Values.ShouldContain("tenant-001");
        metadata.Headers.Values.ShouldContain("conversation-001");
        metadata.Headers.Values.ShouldContain("event-001");

        string combined = string.Join('|', [metadata.Topic, metadata.Type, metadata.Source, metadata.Subject, .. metadata.Headers.Keys, .. metadata.Headers.Values]);
        combined.ShouldNotContain("provider-token", Case.Insensitive);
        combined.ShouldNotContain("raw upstream", Case.Insensitive);
        combined.ShouldNotContain("party@example.com", Case.Insensitive);
        combined.ShouldNotContain("EventStore", Case.Insensitive);
        combined.ShouldNotContain("Dapr", Case.Insensitive);
        combined.ShouldNotContain("SignalR", Case.Insensitive);
    }
}
