// <copyright file="ConversationProjectionAccumulatorTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Verifies duplicate and reordered event delivery remains deterministic for local read models.
/// </summary>
public sealed class ConversationProjectionAccumulatorTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly MessageId Message = new("message-001");
    private static readonly FileId File = new("file-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly DateTimeOffset Now = new(2026, 5, 19, 14, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Duplicate and reordered participant/message/file events set by stable IDs instead of appending blindly.
    /// </summary>
    [Fact]
    public void DuplicateAndReorderedDeliveriesShouldNotCreateDuplicateReadModelRows()
    {
        ConversationProjectionAccumulator accumulator = Accumulator();

        accumulator.Apply(ParticipantAdded("event-participant-001"));
        accumulator.Apply(MessageAppended("event-message-001"));
        accumulator.Apply(FileAttached("event-file-001"));
        accumulator.Apply(Created("event-create-001"));
        accumulator.Apply(ParticipantAdded("event-participant-001"));
        accumulator.Apply(MessageAppended("event-message-001"));
        accumulator.Apply(FileAttached("event-file-001"));

        ConversationProjectionSnapshot snapshot = accumulator.Snapshot;

        snapshot.ParticipantPartyIds.ShouldBe([Participant], ignoreOrder: false);
        snapshot.MessageIds.ShouldBe([Message], ignoreOrder: false);
        snapshot.FileIds.ShouldBe([File], ignoreOrder: false);
        snapshot.Label.ShouldBe("Case 123");
        snapshot.ProcessedEventIds.Count.ShouldBe(4);
    }

    /// <summary>
    /// Terminal lifecycle events are idempotent and cannot regress from archived back to closed.
    /// </summary>
    [Fact]
    public void DuplicateTerminalLifecycleEventsShouldNotRegressState()
    {
        ConversationProjectionAccumulator accumulator = Accumulator();

        accumulator.Apply(Created("event-create-001"));
        accumulator.Apply(Archived("event-archive-001"));
        accumulator.Apply(Closed("event-close-late"));
        accumulator.Apply(Archived("event-archive-001"));

        accumulator.Snapshot.Lifecycle.ShouldBe(ConversationProjectionLifecycleState.Archived);
        accumulator.Snapshot.ProcessedEventIds.Count.ShouldBe(3);
    }

    /// <summary>
    /// Metadata updates are deterministic under duplicates and dictionary order differences.
    /// </summary>
    [Fact]
    public void DuplicateMetadataUpdatesShouldRemainDeterministic()
    {
        ConversationProjectionAccumulator first = Accumulator();
        ConversationProjectionAccumulator second = Accumulator();

        ConversationMetadataUpdated metadata = MetadataUpdated("event-metadata-001");

        first.Apply(Created("event-create-001"));
        first.Apply(metadata);
        first.Apply(metadata);

        second.Apply(metadata);
        second.Apply(Created("event-create-001"));
        second.Apply(metadata);

        first.Snapshot.Label.ShouldBe(second.Snapshot.Label);
        first.Snapshot.BusinessReference.ShouldBe(second.Snapshot.BusinessReference);
        first.Snapshot.Attributes.ShouldBe(second.Snapshot.Attributes);
    }

    /// <summary>
    /// P41: A lifecycle close that arrives before creation is deterministic and cannot be reopened by a late create.
    /// </summary>
    [Fact]
    public void ClosedBeforeCreatedShouldRemainClosed()
    {
        ConversationProjectionAccumulator accumulator = Accumulator();

        accumulator.Apply(Closed("event-close-early"));
        accumulator.Apply(Created("event-create-late"));

        accumulator.Snapshot.Lifecycle.ShouldBe(ConversationProjectionLifecycleState.Closed);
        accumulator.Snapshot.ProcessedEventIds.ShouldBe(["event-close-early", "event-create-late"], ignoreOrder: false);
    }

    /// <summary>
    /// P42: A wrong-identity event before creation is ignored and cannot poison the accumulator identity.
    /// </summary>
    [Fact]
    public void WrongIdentityEventBeforeCreatedShouldNotPoisonAccumulator()
    {
        ConversationProjectionAccumulator accumulator = Accumulator();

        accumulator.Apply(ParticipantAdded(
            "event-wrong-participant",
            tenantId: new TenantId("tenant-other"),
            conversationId: new ConversationId("conversation-other")));
        accumulator.Apply(Created("event-create-001"));

        accumulator.Snapshot.TenantId.ShouldBe(Tenant);
        accumulator.Snapshot.ConversationId.ShouldBe(Conversation);
        accumulator.Snapshot.Label.ShouldBe("Case 123");
        accumulator.Snapshot.ParticipantPartyIds.ShouldBeEmpty();
        accumulator.Snapshot.ProcessedEventIds.ShouldBe(["event-create-001"], ignoreOrder: false);
    }

    /// <summary>
    /// P45: Empty attributes mean no change, so metadata update ordering remains deterministic.
    /// </summary>
    [Fact]
    public void EmptyAttributesShouldNotClearExistingProjectionAttributes()
    {
        ConversationProjectionAccumulator first = Accumulator();
        ConversationProjectionAccumulator second = Accumulator();

        ConversationMetadataUpdated withAttributes = MetadataUpdated(
            "event-metadata-with-attributes",
            new Dictionary<string, string>
            {
                ["owner"] = "support",
                ["priority"] = "high",
            });
        ConversationMetadataUpdated emptyAttributes = MetadataUpdated(
            "event-metadata-empty-attributes",
            new Dictionary<string, string>());

        first.Apply(Created("event-create-001"));
        first.Apply(withAttributes);
        first.Apply(emptyAttributes);

        second.Apply(Created("event-create-001"));
        second.Apply(emptyAttributes);
        second.Apply(withAttributes);

        first.Snapshot.Attributes.ShouldBe(second.Snapshot.Attributes);
        first.Snapshot.Attributes.Count.ShouldBe(2);
    }

    private static ConversationCreated Created(string eventId)
        => new(
            Metadata(eventId, ConversationEventType.ConversationCreated),
            new BusinessReference("crm", "case-123"),
            new ProjectId("project-001"),
            Folder,
            "Case 123");

    private static ParticipantAdded ParticipantAdded(
        string eventId,
        TenantId? tenantId = null,
        ConversationId? conversationId = null)
        => new(
            Metadata(eventId, ConversationEventType.ParticipantAdded, tenantId, conversationId),
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static MessageAppended MessageAppended(string eventId)
        => new(
            Metadata(eventId, ConversationEventType.MessageAppended),
            Message,
            Actor,
            "Hello");

    private static FileReferenceAttached FileAttached(string eventId)
        => new(
            Metadata(eventId, ConversationEventType.FileReferenceAttached),
            File,
            Folder,
            Message);

    private static ConversationMetadataUpdated MetadataUpdated(
        string eventId,
        IReadOnlyDictionary<string, string>? attributes = null)
        => new(
            Metadata(eventId, ConversationEventType.ConversationMetadataUpdated),
            "Case 456",
            new BusinessReference("crm", "case-456"),
            attributes ?? new Dictionary<string, string>
            {
                ["owner"] = "support",
                ["priority"] = "high",
            });

    private static ConversationClosed Closed(string eventId)
        => new(Metadata(eventId, ConversationEventType.ConversationClosed), "resolved");

    private static ConversationArchived Archived(string eventId)
        => new(Metadata(eventId, ConversationEventType.ConversationArchived), "retained");

    private static ConversationProjectionAccumulator Accumulator()
        => new(Tenant, Conversation);

    private static ConversationEventMetadata Metadata(
        string eventId,
        ConversationEventType eventType,
        TenantId? tenantId = null,
        ConversationId? conversationId = null)
        => new(
            SchemaVersion.Current,
            eventId,
            eventType,
            tenantId ?? Tenant,
            conversationId ?? Conversation,
            "correlation-001",
            Now,
            Actor,
            "causation-001");
}
