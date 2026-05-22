// <copyright file="PublicationSamples.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Tests.Publication;

internal static class PublicationSamples
{
    internal static readonly SchemaVersion Version = SchemaVersion.Current;
    internal static readonly TenantId Tenant = new("tenant-001");
    internal static readonly ConversationId Conversation = new("conversation-001");
    internal static readonly PartyId Actor = new("party-actor");
    internal static readonly PartyId Participant = new("party-participant");
    internal static readonly ProjectId Project = new("project-001");
    internal static readonly FolderId Folder = new("folder-001");
    internal static readonly BusinessReference Business = new("crm", "case-123");

    internal static readonly ProviderCorrelationMetadata ProviderCorrelation = new(
        "provider-a",
        "assistant",
        Version,
        "session-reference",
        "response-reference",
        new Dictionary<string, string>
        {
            ["region"] = "eu",
        });

    internal static readonly ConversationEventMetadata CreatedMetadata = new(
        Version,
        "event-001",
        ConversationEventType.ConversationCreated,
        Tenant,
        Conversation,
        "correlation-001",
        new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero),
        Actor,
        "causation-001");

    internal static readonly ConversationEventMetadata ParticipantMetadata = new(
        Version,
        "event-participant-001",
        ConversationEventType.ParticipantAdded,
        Tenant,
        Conversation,
        "correlation-001",
        new DateTimeOffset(2026, 5, 18, 11, 1, 0, TimeSpan.Zero),
        Actor,
        "causation-001");
}
