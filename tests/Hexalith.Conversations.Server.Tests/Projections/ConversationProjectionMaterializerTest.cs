// <copyright file="ConversationProjectionMaterializerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Verifies Story 1.7 projection materialization and freshness downgrade behavior.
/// </summary>
public sealed class ConversationProjectionMaterializerTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-other");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly ConversationId OtherConversation = new("conversation-other");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly MessageId Message = new("message-001");
    private static readonly FileId File = new("file-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly ProjectId Project = new("project-001");
    private static readonly DateTimeOffset Started = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Generated = new(2026, 5, 20, 9, 0, 10, TimeSpan.Zero);

    /// <summary>
    /// Ordered event replay derives summary and detail models from stable IDs and content-safe fields.
    /// </summary>
    [Fact]
    public void OrderedReplayShouldBuildCurrentSummaryAndDetailModels()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            OrderedEvents(),
            Generated,
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
        result.Summary.MessageCount.ShouldBe(1);
        result.Summary.FileReferenceCount.ShouldBe(1);
        result.Summary.ParticipantPartyIds.ShouldBe([Participant], ignoreOrder: false);
        result.Detail.Messages.Single().Text.ShouldBe("Hello");
        result.Detail.Participants.Single().ParticipantPartyId.ShouldBe(Participant);
        result.Detail.FileReferences.Single().FileId.ShouldBe(File);
        result.Detail.ProjectId.ShouldBe(Project);
        result.Detail.FolderId.ShouldBe(Folder);
        result.Detail.Attributes.ShouldBe(new Dictionary<string, string> { ["priority"] = "high" });
    }

    /// <summary>
    /// Bounded lifecycle-change events update projected state without relying on free-form lifecycle strings.
    /// </summary>
    [Fact]
    public void LifecycleChangedEventShouldUpdateProjectedLifecycleState()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, new ConversationLifecycleChanged(
                    Metadata("event-lifecycle-001", ConversationEventType.ConversationLifecycleChanged, 2),
                    ConversationLifecycleStatus.Open,
                    ConversationLifecycleStatus.Closed,
                    "resolved")),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Summary.LifecycleState.ShouldBe("Closed");
        result.Detail.LifecycleState.ShouldBe("Closed");
        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
    }

    /// <summary>
    /// Duplicate and replayed delivery is idempotent and rebuilds to the same read model.
    /// </summary>
    [Fact]
    public void DuplicateAndReplayedEventsShouldRemainDeterministic()
    {
        ConversationProjectionEventRecord[] repeated =
        [
            .. OrderedEvents(),
            Event(2, ParticipantAdded("event-participant-001", 2)),
            Event(3, MessageAppended("event-message-001", 3)),
            Event(4, FileAttached("event-file-001", 4)),
        ];

        ConversationProjectedReadModels first = Materializer().Project(Tenant, Conversation, repeated, Generated, TimeSpan.FromMinutes(5));
        ConversationProjectedReadModels rebuilt = Materializer().Project(Tenant, Conversation, OrderedEvents(), Generated, TimeSpan.FromMinutes(5));

        first.Summary.Freshness.ShouldBe(rebuilt.Summary.Freshness);
        first.Summary.ParticipantPartyIds.ShouldBe(rebuilt.Summary.ParticipantPartyIds, ignoreOrder: false);
        first.Summary.MessageCount.ShouldBe(rebuilt.Summary.MessageCount);
        first.Summary.FileReferenceCount.ShouldBe(rebuilt.Summary.FileReferenceCount);
        first.Detail.Messages.ShouldBe(rebuilt.Detail.Messages, ignoreOrder: false);
        first.Detail.Participants.ShouldBe(rebuilt.Detail.Participants, ignoreOrder: false);
        first.Detail.FileReferences.ShouldBe(rebuilt.Detail.FileReferences, ignoreOrder: false);
        first.Detail.Attributes.ShouldBe(rebuilt.Detail.Attributes);
    }

    /// <summary>
    /// Gaps in source positions degrade trust to rebuilding instead of producing a confident current model.
    /// </summary>
    [Fact]
    public void GapDetectionShouldDowngradeProjectionToRebuilding()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(3, MessageAppended("event-message-001", 3)),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.GapDetected);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// Child events before creation are projected only as rebuilding evidence, never as current truth.
    /// </summary>
    [Fact]
    public void ChildEventBeforeCreatedShouldDowngradeProjectionToRebuilding()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, MessageAppended("event-message-001", 1)),
                Event(2, Created("event-create-001", 2)),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Detail.Messages.Single().MessageId.ShouldBe(Message);
        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.OutOfOrderEvent);
    }

    /// <summary>
    /// Mixed-tenant poison events are rejected before mutation and make the projection unavailable.
    /// </summary>
    [Fact]
    public void MixedTenantPoisonEventShouldNotMutateProjectionAndShouldDowngradeTrust()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, ParticipantAdded("event-poison", 2, OtherTenant, OtherConversation)),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Detail.Participants.ShouldBeEmpty();
        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.PoisonEvent);
    }

    /// <summary>
    /// Stale metadata is explicit and blocks trust-bearing decisions.
    /// </summary>
    [Fact]
    public void StaleProjectionShouldExposeStaleFreshness()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            OrderedEvents(),
            Generated.AddMinutes(30),
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.Summary.Freshness.IsStale.ShouldBeTrue();
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// Metadata write failures after mutation must not leave a public current model.
    /// </summary>
    [Fact]
    public void MetadataWriteFailureAfterMutationShouldDowngradeProjection()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            OrderedEvents(),
            Generated,
            TimeSpan.FromMinutes(5),
            metadataWriteFailed: true);

        result.Detail.Messages.Count.ShouldBe(1);
        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataWriteFailed);
    }

    /// <summary>
    /// Active rebuild windows are public rebuilding states even when the materialized data is otherwise complete.
    /// </summary>
    [Fact]
    public void ActiveRebuildShouldDowngradeProjectionToRebuilding()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            OrderedEvents(),
            Generated,
            TimeSpan.FromMinutes(5),
            isRebuilding: true);

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Rebuilding);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// Rebuilding projection state from accepted events yields the same read models as the original materialization.
    /// </summary>
    [Fact]
    public void DeletingAndRebuildingProjectionShouldProduceEquivalentReadModels()
    {
        ConversationProjectionMaterializer materializer = Materializer();
        ConversationProjectionEventRecord[] history = OrderedEvents();

        ConversationProjectedReadModels original = materializer.Project(
            Tenant,
            Conversation,
            history,
            Generated,
            TimeSpan.FromMinutes(5));

        // Simulate projection-store wipe: discard `original`, replay the same accepted history
        // with a fresh materializer instance, and confirm the rebuilt models are equivalent.
        ConversationProjectedReadModels rebuilt = Materializer().Project(
            Tenant,
            Conversation,
            history,
            Generated,
            TimeSpan.FromMinutes(5));

        rebuilt.Summary.Freshness.ShouldBe(original.Summary.Freshness);
        rebuilt.Summary.LifecycleState.ShouldBe(original.Summary.LifecycleState);
        rebuilt.Summary.MessageCount.ShouldBe(original.Summary.MessageCount);
        rebuilt.Summary.FileReferenceCount.ShouldBe(original.Summary.FileReferenceCount);
        rebuilt.Summary.ParticipantPartyIds.ShouldBe(original.Summary.ParticipantPartyIds, ignoreOrder: false);
        rebuilt.Detail.Freshness.ShouldBe(original.Detail.Freshness);
        rebuilt.Detail.LifecycleState.ShouldBe(original.Detail.LifecycleState);
        rebuilt.Detail.Messages.ShouldBe(original.Detail.Messages, ignoreOrder: false);
        rebuilt.Detail.Participants.ShouldBe(original.Detail.Participants, ignoreOrder: false);
        rebuilt.Detail.FileReferences.ShouldBe(original.Detail.FileReferences, ignoreOrder: false);
        rebuilt.Detail.Attributes.ShouldBe(original.Detail.Attributes);
    }

    /// <summary>
    /// Retention policy events derive descriptive active retention state without becoming command authority.
    /// </summary>
    [Fact]
    public void RetentionPolicyEventsShouldProjectActiveRetentionState()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, RetentionSet("event-retention-set-001", 2)),
                Event(3, RetentionReplaced("event-retention-replaced-001", 3)),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Detail.ActiveRetentionPolicy.ShouldNotBeNull();
        result.Detail.ActiveRetentionPolicy.PolicyReference.ShouldBe("retention-policy-extended");
        result.Detail.ActiveRetentionPolicy.PreviousPolicyReference.ShouldBe("retention-policy-standard");
        result.Detail.ActiveRetentionPolicy.ActorPartyId.ShouldBe(Actor);
        result.Detail.ActiveRetentionPolicy.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
    }

    /// <summary>
    /// Sensitivity events derive target-keyed read state with safe audit and trust metadata.
    /// </summary>
    [Fact]
    public void SensitivityEventsShouldProjectDerivedReadState()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, MessageAppended("event-message-001", 2)),
                Event(3, Sensitive(
                    "event-sensitive-message-001",
                    3,
                    new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message))),
                Event(4, Sensitive(
                    "event-sensitive-segment-001",
                    4,
                    new GovernanceTarget(GovernedTargetKind.ContentSegment, SegmentReference: "segment-001"))),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Detail.SensitivityMarks.Count.ShouldBe(2);
        ConversationSensitivityMarkProjectionV1 messageMark = result.Detail.SensitivityMarks
            .Single(mark => mark.Target.Kind == GovernedTargetKind.Message);
        messageMark.Target.MessageId.ShouldBe(Message);
        messageMark.Category.ShouldBe(SensitivityCategory.Restricted);
        messageMark.PolicyReference.ShouldBe("sensitivity-policy-standard");
        messageMark.Rationale.ShouldBe("customer-request");
        messageMark.ActorPartyId.ShouldBe(Actor);
        messageMark.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
        messageMark.TrustState.ShouldBe(ProjectionTrustState.Current);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
    }

    /// <summary>
    /// Unsupported-version sensitivity events cannot upgrade projected sensitivity state to current truth.
    /// </summary>
    [Fact]
    public void UnsupportedSensitivityEventShouldDowngradeProjectionWithoutSensitivityState()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, Sensitive(
                    "event-sensitive-unsupported",
                    2,
                    new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
                    schemaVersion: new SchemaVersion(2))),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Detail.SensitivityMarks.ShouldBeEmpty();
        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Unavailable);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// MessageAppended with whitespace text is treated as poison rather than crashing the projection pass.
    /// </summary>
    [Fact]
    public void WhitespaceMessageTextShouldDowngradeProjectionToPoisonEvent()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, new MessageAppended(
                    Metadata("event-message-001", ConversationEventType.MessageAppended, 2),
                    Message,
                    Actor,
                    "   ")),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.PoisonEvent);
        result.Detail.Messages.ShouldBeEmpty();
    }

    /// <summary>
    /// MessageId collisions across distinct event IDs degrade trust rather than overwriting silently.
    /// </summary>
    [Fact]
    public void MessageIdCollisionShouldDowngradeProjectionToPoisonEvent()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, MessageAppended("event-message-001", 2)),
                Event(3, MessageAppended("event-message-002", 3)),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.PoisonEvent);
    }

    /// <summary>
    /// A stream starting at a non-1 position has a detectable initial gap.
    /// </summary>
    [Fact]
    public void InitialNonOnePositionShouldDowngradeProjectionToRebuilding()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(5, Created("event-create-001", 5)),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.GapDetected);
    }

    /// <summary>
    /// Unknown event types must not crash the materializer; they are downgraded to a non-current state.
    /// </summary>
    [Fact]
    public void UnknownEventTypeShouldDowngradeProjectionWithoutThrowing()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, new UnknownProjectionEvent()),
            ],
            Generated,
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.OutOfOrderEvent);
    }

    /// <summary>
    /// Contradictory timestamps are downgraded instead of throwing or returning current state.
    /// </summary>
    [Fact]
    public void ContradictoryProjectionMetadataShouldDowngradeToUnavailable()
    {
        ConversationProjectedReadModels result = Materializer().Project(
            Tenant,
            Conversation,
            OrderedEvents(),
            Started,
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataContradictory);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    private static ConversationProjectionMaterializer Materializer() => new();

    private static ConversationProjectionEventRecord[] OrderedEvents() =>
    [
        Event(1, Created("event-create-001", 1)),
        Event(2, ParticipantAdded("event-participant-001", 2)),
        Event(3, MessageAppended("event-message-001", 3)),
        Event(4, FileAttached("event-file-001", 4)),
        Event(5, MetadataUpdated("event-metadata-001", 5)),
    ];

    private static ConversationProjectionEventRecord Event(long position, object e)
        => new(position, e);

    private static ConversationCreated Created(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.ConversationCreated, position),
            new BusinessReference("crm", "case-123"),
            Project,
            Folder,
            "Case 123");

    private static ParticipantAdded ParticipantAdded(
        string eventId,
        long position,
        TenantId? tenantId = null,
        ConversationId? conversationId = null)
        => new(
            Metadata(eventId, ConversationEventType.ParticipantAdded, position, tenantId, conversationId),
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static MessageAppended MessageAppended(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.MessageAppended, position),
            Message,
            Actor,
            "Hello");

    private static FileReferenceAttached FileAttached(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.FileReferenceAttached, position),
            File,
            Folder,
            Message);

    private static ConversationMetadataUpdated MetadataUpdated(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.ConversationMetadataUpdated, position),
            null,
            null,
            new Dictionary<string, string> { ["priority"] = "high" });

    private static RetentionPolicySet RetentionSet(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.RetentionPolicySet, position),
            "retention-policy-standard",
            "customer-request",
            AuditEvidence(position));

    private static RetentionPolicyReplaced RetentionReplaced(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.RetentionPolicyReplaced, position),
            "retention-policy-extended",
            "retention-policy-standard",
            "customer-request",
            AuditEvidence(position));

    private static ConversationContentMarkedSensitive Sensitive(
        string eventId,
        long position,
        GovernanceTarget target,
        SchemaVersion? schemaVersion = null)
        => new(
            Metadata(eventId, ConversationEventType.ConversationContentMarkedSensitive, position, schemaVersion: schemaVersion),
            target,
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AuditEvidence(position, "sensitivity-policy-standard"));

    private static GovernanceAuditEvidenceReference AuditEvidence(long position)
        => AuditEvidence(position, "retention-policy-standard");

    private static GovernanceAuditEvidenceReference AuditEvidence(long position, string policyReference)
        => new(
            new AuditEvidenceHandle("audit-evidence-001"),
            policyReference,
            Started.AddSeconds(position));

    private static ConversationEventMetadata Metadata(
        string eventId,
        ConversationEventType eventType,
        long position,
        TenantId? tenantId = null,
        ConversationId? conversationId = null,
        SchemaVersion? schemaVersion = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            eventId,
            eventType,
            tenantId ?? Tenant,
            conversationId ?? Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");

    private sealed record UnknownProjectionEvent;
}
