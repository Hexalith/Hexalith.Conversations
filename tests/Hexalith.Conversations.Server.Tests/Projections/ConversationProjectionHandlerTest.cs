// <copyright file="ConversationProjectionHandlerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Story 2.5 (FR-6) — behavior of the conversation projection served through the platform
/// <c>IDomainProjectionHandler</c> seam. Proves the handler decodes the request's events into the public
/// conversation vocabulary and delegates to the preserved materialization logic, producing the same
/// field/freshness/evidence values through the seam — and surfaces a degraded-state reason code rather than a
/// falsely-current projection (behavior, not a mirror; Epic 1 L1/A1).
/// </summary>
public sealed class ConversationProjectionHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly MessageId Message = new("message-001");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly DateTimeOffset Started = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Generated = new(2026, 5, 20, 9, 0, 10, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The handler serves the singular aggregate domain (not the plural query namespace) and a stable projection
    /// type the gateway stores the read model under.
    /// </summary>
    [Fact]
    public void HandlerShouldServeTheConversationAggregateDomain()
    {
        ConversationProjectionHandler handler = Handler();

        handler.Domain.ShouldBe("conversation");
        ConversationProjectionHandler.ConversationProjectionType.ShouldBe("conversation");
    }

    /// <summary>
    /// The shared registry must expose exactly the legacy public event map: 13 entries keyed by simple type name.
    /// </summary>
    [Fact]
    public void PublicEventRegistryShouldExposeTheLegacyThirteenEventNames()
    {
        ConversationProjectionHandler.PublicEventTypeEntries.Keys.Order(StringComparer.Ordinal).ShouldBe(
            new[]
            {
                "ConversationArchived",
                "ConversationClosed",
                "ConversationContentMarkedSensitive",
                "ConversationCreated",
                "ConversationLifecycleChanged",
                "ConversationMetadataUpdated",
                "ConversationProjectChanged",
                "FileReferenceAttached",
                "MessageAppended",
                "MessageContentRedacted",
                "ParticipantAdded",
                "RetentionPolicyReplaced",
                "RetentionPolicySet",
            },
            ignoreOrder: false);

        ConversationProjectionHandler.PublicEventTypeEntries["ConversationCreated"].ShouldBe(typeof(ConversationCreated));
        ConversationProjectionHandler.PublicEventTypeEntries["MessageContentRedacted"].ShouldBe(typeof(MessageContentRedacted));
    }

    /// <summary>
    /// An ordered, in-order event sequence decoded through the seam produces a current, trust-bearing projection
    /// with the expected field selection and evidence — the same behavior the kept materializer produces directly.
    /// </summary>
    [Fact]
    public void OrderedRequestShouldProjectCurrentTrustBearingReadModelThroughTheSeam()
    {
        ProjectionResponse response = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(2, ParticipantAdded(2)),
            Dto(3, MessageAppended(3))));

        response.ProjectionType.ShouldBe("conversation");
        ConversationProjectedReadModels models = Decode(response);

        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
        models.Summary.TenantId.ShouldBe(Tenant);
        models.Summary.ConversationId.ShouldBe(Conversation);
        models.Summary.MessageCount.ShouldBe(1);
        models.Summary.ParticipantPartyIds.ShouldBe([Participant], ignoreOrder: false);
        models.Detail.Messages.Single().Text.ShouldBe("Hello");
        models.Detail.Participants.Single().ParticipantPartyId.ShouldBe(Participant);
        models.Detail.EvidenceEntries.Select(entry => entry.Kind).ShouldContain("Message");
        models.Detail.EvidenceEntries.Select(entry => entry.Kind).ShouldContain("Participant");
        models.Detail.EvidenceEntries.Select(entry => entry.Kind).ShouldContain("Freshness");
    }

    /// <summary>
    /// Redacted message content stays suppressed when projected through the seam — the redacted text never leaks
    /// into the returned projection state.
    /// </summary>
    [Fact]
    public void RedactedContentShouldStaySuppressedThroughTheSeam()
    {
        ProjectionResponse response = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(2, MessageAppended(2, "secret customer content")),
            Dto(3, Redacted(3))));

        ConversationProjectedReadModels models = Decode(response);

        models.Detail.Messages.Single().Text.ShouldBe("[redacted]");
        models.Detail.Messages.Single().Text.ShouldNotContain("secret", Case.Insensitive);
        models.Detail.Redactions.Single().Target.MessageId.ShouldBe(Message);
        response.State.GetRawText().ShouldNotContain("secret", Case.Insensitive);
    }

    /// <summary>
    /// A source-position gap decoded through the seam surfaces a degraded (Rebuilding / GapDetected) freshness
    /// state that is not trust-bearing — the seam never promotes a gapped replay to current.
    /// </summary>
    [Fact]
    public void GapInRequestShouldSurfaceDegradedFreshnessThroughTheSeam()
    {
        ProjectionResponse response = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(3, MessageAppended(3))));

        ConversationProjectedReadModels models = Decode(response);

        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        models.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.GapDetected);
        models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// A mixed-tenant event decoded through the seam makes the projection unavailable (the materializer re-checks
    /// each event's metadata against the request scope and fails closed) — never a current cross-tenant read.
    /// </summary>
    [Fact]
    public void MixedTenantEventShouldMakeProjectionUnavailableThroughTheSeam()
    {
        ProjectionResponse response = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(2, ParticipantAdded(2, new TenantId("tenant-other"), new ConversationId("conversation-other")))));

        ConversationProjectedReadModels models = Decode(response);

        models.Detail.Participants.ShouldBeEmpty();
        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        models.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.PoisonEvent);
    }

    /// <summary>
    /// Event type names outside the conversation vocabulary are skipped during decode rather than throwing out of
    /// the seam. Here the skipped foreign event is the trailing position, so no gap forms against a later applied
    /// event and the projection stays current — the skip never falsely degrades (the gap-forming case is covered by
    /// <see cref="GapInRequestShouldSurfaceDegradedFreshnessThroughTheSeam"/>).
    /// </summary>
    [Fact]
    public void UnknownEventTypeNameShouldBeSkippedDuringDecodeWithoutThrowing()
    {
        ProjectionEventDto unknown = new(
            "SomeForeignDomainEvent",
            JsonSerializer.SerializeToUtf8Bytes(new { ignored = true }, Options),
            "json",
            2,
            Started.AddSeconds(2),
            "correlation-001");

        ProjectionResponse response = Handler().Project(Request(Dto(1, Created(1)), unknown));

        ConversationProjectedReadModels models = Decode(response);

        // The known ConversationCreated at position 1 still materializes; the foreign event at position 2 is
        // dropped, so the projection stays current (no later positions to gap against) but carries only the
        // decoded event — proving the unknown type was skipped without throwing.
        models.Summary.MessageCount.ShouldBe(0);
        models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
    }

    /// <summary>
    /// Fully-qualified event names continue to resolve by suffix after the shared registry exact-name pass.
    /// </summary>
    [Fact]
    public void FullyQualifiedEventTypeNameShouldResolveBySuffix()
    {
        ProjectionEventDto qualified = new(
            "Hexalith.Conversations.Contracts.Events.ConversationCreated",
            JsonSerializer.SerializeToUtf8Bytes(Created(1), Options),
            "json",
            1,
            Started.AddSeconds(1),
            "correlation-001");

        ProjectionResponse response = Handler().Project(Request(qualified));

        Decode(response).Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
    }

    /// <summary>
    /// The seam returns the same projected read model the kept materializer produces directly for the same
    /// inputs — the handler delegates field/freshness/evidence construction rather than reimplementing it.
    /// </summary>
    [Fact]
    public void SeamOutputShouldEqualDirectMaterializationForTheSameInputs()
    {
        ConversationProjectionEventRecord[] records =
        [
            new(1, Created(1)),
            new(2, ParticipantAdded(2)),
            new(3, MessageAppended(3)),
        ];
        ConversationProjectedReadModels direct = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            records,
            Generated,
            TimeSpan.FromMinutes(5),
            isRebuilding: false,
            metadataWriteFailed: false);

        ProjectionResponse response = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(2, ParticipantAdded(2)),
            Dto(3, MessageAppended(3))));

        response.State.GetRawText().ShouldBe(JsonSerializer.SerializeToElement(direct, Options).GetRawText());
    }

    /// <summary>
    /// Duplicate delivery of the same events through the seam yields a byte-identical projection (NFR5): the
    /// per-event dedup keeps the stateless full-replay projection idempotent under Dapr at-least-once delivery,
    /// so a re-delivered event never double-counts or downgrades the read model.
    /// </summary>
    [Fact]
    public void DuplicateEventDeliveryShouldProjectIdenticalReadModelThroughTheSeam()
    {
        ProjectionResponse once = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(2, ParticipantAdded(2)),
            Dto(3, MessageAppended(3))));

        ProjectionResponse withDuplicates = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(2, ParticipantAdded(2)),
            Dto(2, ParticipantAdded(2)),
            Dto(3, MessageAppended(3)),
            Dto(3, MessageAppended(3))));

        withDuplicates.State.GetRawText().ShouldBe(once.State.GetRawText());
        Decode(withDuplicates).Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
    }

    /// <summary>
    /// An out-of-order position (an event whose source position regresses) decoded through the seam surfaces a
    /// degraded (Rebuilding / OutOfOrderEvent) freshness state that is not trust-bearing — distinct from a forward
    /// gap, and never promoted to current (NFR5).
    /// </summary>
    [Fact]
    public void OutOfOrderEventShouldSurfaceDegradedFreshnessThroughTheSeam()
    {
        ProjectionResponse response = Handler().Project(Request(
            Dto(1, Created(1)),
            Dto(2, ParticipantAdded(2)),
            Dto(2, MessageAppended(2))));

        ConversationProjectedReadModels models = Decode(response);

        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        models.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.OutOfOrderEvent);
        models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// When the generation clock is past the stale threshold relative to the last applied event, the seam reports
    /// Stale / StaleThresholdExceeded rather than pretending the projection is fresh (AC-3 degraded-state surface).
    /// </summary>
    [Fact]
    public void StaleProjectionShouldSurfaceStaleThresholdThroughTheSeam()
    {
        ProjectionResponse response = Handler(Started.AddMinutes(10)).Project(Request(Dto(1, Created(1))));

        ConversationProjectedReadModels models = Decode(response);

        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        models.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
    }

    /// <summary>
    /// An empty event sequence (no <see cref="ConversationCreated"/> seen) projects a non-current Rebuilding read
    /// model through the seam rather than an empty-but-current one — the projection never reports trust on a
    /// stream it has not yet replayed.
    /// </summary>
    [Fact]
    public void EmptyEventSequenceShouldSurfaceRebuildingThroughTheSeam()
    {
        ProjectionResponse response = Handler().Project(Request());

        ConversationProjectedReadModels models = Decode(response);

        models.Summary.MessageCount.ShouldBe(0);
        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        models.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Rebuilding);
        models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// A null request fails closed at the seam boundary (the platform contract is a non-null
    /// <see cref="ProjectionRequest"/>) rather than producing a degenerate projection.
    /// </summary>
    [Fact]
    public void NullRequestShouldThrowArgumentNullException()
        => Should.Throw<ArgumentNullException>(() => Handler().Project(null!));

    /// <summary>
    /// A known event type carrying an empty payload is skipped during decode (the zero-length payload branch),
    /// so the seam never materializes it. With no decodable <see cref="ConversationCreated"/>, freshness stays
    /// non-trust-bearing (Rebuilding) rather than falsely reporting Current off an undecoded position (AC-5).
    /// </summary>
    [Fact]
    public void KnownEventWithEmptyPayloadShouldBeSkippedAndNeverFalselyCurrent()
    {
        ProjectionEventDto emptyPayload = new(
            nameof(ConversationCreated),
            [],
            "json",
            1,
            Started.AddSeconds(1),
            "correlation-001");

        ProjectionResponse response = Handler().Project(Request(emptyPayload));

        ConversationProjectedReadModels models = Decode(response);

        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// A non-positive (malformed, contract is 1-based) sequence number is dropped before type resolution and
    /// decode, so the event is never applied — here the dropped <see cref="ParticipantAdded"/> at sequence 0 adds
    /// no participant, and the lone valid position stays Current without the skipped position degrading it (AC-5).
    /// </summary>
    [Fact]
    public void NonPositiveSequenceNumberShouldBeSkippedBeforeDecode()
    {
        ProjectionEventDto nonPositive = new(
            nameof(ParticipantAdded),
            JsonSerializer.SerializeToUtf8Bytes(ParticipantAdded(2), Options),
            "json",
            0,
            Started,
            "correlation-001");

        ProjectionResponse response = Handler().Project(Request(Dto(1, Created(1)), nonPositive));

        ConversationProjectedReadModels models = Decode(response);

        models.Detail.Participants.ShouldBeEmpty();
        models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
    }

    /// <summary>
    /// A known event type carrying a syntactically malformed payload fails closed: the decode throws a
    /// <see cref="JsonException"/> out of the seam rather than silently dropping the event and serving a
    /// falsely-current projection. This pins the "no more permissive than today" malformed-event rule (AC-5).
    /// </summary>
    [Fact]
    public void MalformedKnownEventPayloadShouldFailClosedThroughTheSeam()
    {
        ProjectionEventDto malformed = new(
            nameof(MessageAppended),
            [(byte)'{'],
            "json",
            2,
            Started.AddSeconds(2),
            "correlation-001");

        Should.Throw<JsonException>(() => Handler().Project(Request(Dto(1, Created(1)), malformed)));
    }

    private static ConversationProjectionHandler Handler()
        => new(new ConversationProjectionMaterializer(), new FixedTimeProvider(Generated));

    private static ConversationProjectionHandler Handler(DateTimeOffset clock)
        => new(new ConversationProjectionMaterializer(), new FixedTimeProvider(clock));

    private static ConversationProjectedReadModels Decode(ProjectionResponse response)
        => response.State.Deserialize<ConversationProjectedReadModels>(Options)
            ?? throw new InvalidOperationException("Projection state did not deserialize.");

    private static ProjectionRequest Request(params ProjectionEventDto[] events)
        => new(Tenant.Value, ConversationProjectionHandler.ConversationDomain, Conversation.Value, events);

    private static ProjectionEventDto Dto(long sequence, object publicEvent)
        => new(
            publicEvent.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(publicEvent, Options),
            "json",
            sequence,
            Started.AddSeconds(sequence),
            "correlation-001");

    private static ConversationCreated Created(long position)
        => new(
            Metadata(ConversationEventType.ConversationCreated, position),
            new BusinessReference("crm", "case-123"),
            Project,
            Folder,
            "Case 123");

    private static MessageAppended MessageAppended(long position, string text = "Hello")
        => new(Metadata(ConversationEventType.MessageAppended, position), Message, Actor, text);

    private static ParticipantAdded ParticipantAdded(
        long position,
        TenantId? tenantId = null,
        ConversationId? conversationId = null)
        => new(
            Metadata(ConversationEventType.ParticipantAdded, position, tenantId, conversationId),
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static MessageContentRedacted Redacted(long position)
        => new(
            Metadata(ConversationEventType.MessageContentRedacted, position),
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-001"),
                "redaction-policy-standard",
                Started.AddSeconds(position)));

    private static ConversationEventMetadata Metadata(
        ConversationEventType eventType,
        long position,
        TenantId? tenantId = null,
        ConversationId? conversationId = null)
        => new(
            SchemaVersion.Current,
            $"event-{eventType}-{position}",
            eventType,
            tenantId ?? Tenant,
            conversationId ?? Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
