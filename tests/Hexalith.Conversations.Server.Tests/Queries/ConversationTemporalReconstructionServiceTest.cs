// <copyright file="ConversationTemporalReconstructionServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Verifies tenant-safe point-in-time reconstruction behavior.
/// </summary>
public sealed class ConversationTemporalReconstructionServiceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly MessageId Message = new("message-001");
    private static readonly DateTimeOffset Started = new(2026, 5, 22, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Authorized timestamp reconstruction replays up to the anchor and applies current redaction policy.
    /// </summary>
    [Fact]
    public async Task TimestampAnchorShouldReplayHistoricalStateAndApplyCurrentRedaction()
    {
        ConversationReplayEventRecord[] history =
        [
            Event(1, Created("event-create-001", 1)),
            Event(2, ParticipantAdded("event-participant-001", 2)),
            Event(3, MessageAppended("event-message-001", 3, "secret customer content")),
            Event(4, Redacted("event-redacted-001", 4)),
        ];
        FakeTemporalEventSource source = new(ConversationTemporalEventSourceResult.Available(history));
        ConversationTemporalReconstructionService service = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithRedaction() },
            source);

        ConversationTemporalDetailResult result = await service.ReconstructAsync(
            Query(TimestampAnchor(Started.AddSeconds(3))),
            TestContext.Current.CancellationToken);

        result.Details.ShouldNotBeNull();
        result.Details.TemporalAnchor.SafeSourcePosition.ShouldBe(3);
        result.Details.Messages.Single().Text.ShouldBe("[redacted]");
        result.Details.Messages.Single().Text.ShouldNotContain("secret", Case.Insensitive);
        result.Details.Redactions.Single().AuditEvidence!.Handle.Value.ShouldBe("audit-evidence-001");
        result.Confidence.IsComplete.ShouldBeTrue();
        source.Reads.ShouldBe(1);
    }

    /// <summary>
    /// Safe-position and contract-defined cursors resolve to the same bounded event set.
    /// </summary>
    [Fact]
    public async Task SafePositionAndContractCursorAnchorsShouldResolveDeterministically()
    {
        ConversationReplayEventRecord[] history =
        [
            Event(1, Created("event-create-001", 1)),
            Event(2, ParticipantAdded("event-participant-001", 2)),
            Event(3, MessageAppended("event-message-001", 3)),
        ];
        ConversationTemporalReconstructionService service = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.Available(history)));

        ConversationTemporalDetailResult byPosition = await service.ReconstructAsync(
            Query(PositionAnchor(2)),
            TestContext.Current.CancellationToken);
        ConversationTemporalDetailResult byCursor = await service.ReconstructAsync(
            Query(ContractCursorAnchor("temporal:v1:pos:0000000002")),
            TestContext.Current.CancellationToken);

        byPosition.Details.ShouldNotBeNull();
        byCursor.Details.ShouldNotBeNull();
        byPosition.Details.TemporalAnchor.ShouldBe(byCursor.Details.TemporalAnchor);
        byPosition.Details.Messages.ShouldBeEmpty();
        byCursor.Details.Messages.ShouldBeEmpty();
    }

    /// <summary>
    /// Projection cursors use the same safe position semantics as contract-defined cursors.
    /// </summary>
    [Fact]
    public async Task ProjectionCursorAnchorShouldResolveToSafeAuthoritativeAnchor()
    {
        ConversationReplayEventRecord[] history =
        [
            Event(1, Created("event-create-001", 1)),
            Event(2, ParticipantAdded("event-participant-001", 2)),
            Event(3, MessageAppended("event-message-001", 3)),
        ];
        ConversationTemporalReconstructionService service = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.Available(history)));

        ConversationTemporalDetailResult result = await service.ReconstructAsync(
            Query(ProjectionCursorAnchor("pos:0000000003")),
            TestContext.Current.CancellationToken);

        result.Details.ShouldNotBeNull();
        result.AuthoritativeTemporalAnchor.ShouldNotBeNull();
        result.AuthoritativeTemporalAnchor.AnchorKind.ShouldBe(ConversationTemporalAnchorV1.CompositeCursorKind);
        result.AuthoritativeTemporalAnchor.SafeSourcePosition.ShouldBe(3);
        result.AuthoritativeTemporalAnchor.ProjectionCursor.ShouldBe("pos:0000000004");
        result.AuthoritativeTemporalAnchor.ProjectionVersion.ShouldBe(4);
        result.Details.Messages.Single().MessageId.ShouldBe(Message);
        result.Details.TemporalAnchor.ProjectionCursor.ShouldBe("pos:0000000004");
    }

    /// <summary>
    /// Invalid and cross-tenant cursors fail closed without reading replay evidence.
    /// </summary>
    [Fact]
    public async Task InvalidTemporalCursorsShouldFailClosedWithoutReadingTemporalEvidence()
    {
        FakeTemporalEventSource source = new(ConversationTemporalEventSourceResult.Available([]));
        ConversationTemporalReconstructionService service = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            source);

        ConversationTemporalDetailResult malformed = await service.ReconstructAsync(
            Query(ContractCursorAnchor("not-a-supported-cursor")),
            TestContext.Current.CancellationToken);
        ConversationTemporalDetailResult mismatched = await service.ReconstructAsync(
            new GetConversationAtPointInTimeQuery(
                SchemaVersion.Current,
                Tenant,
                "caller-001",
                "correlation-001",
                Conversation,
                new ConversationTemporalAnchorV1(
                    SchemaVersion.Current,
                    OtherTenant,
                    Conversation,
                    ConversationTemporalAnchorV1.ContractCursorKind,
                    ContractCursor: "temporal:v1:pos:0000000002")),
            TestContext.Current.CancellationToken);
        ConversationTemporalDetailResult mismatchedProjection = await service.ReconstructAsync(
            Query(ContractCursorAnchor("temporal:v1:pos:0000000002:projection:0000000999")),
            TestContext.Current.CancellationToken);

        malformed.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        mismatched.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        mismatchedProjection.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        malformed.Details.ShouldBeNull();
        mismatched.Details.ShouldBeNull();
        mismatchedProjection.Details.ShouldBeNull();
        mismatchedProjection.AuthoritativeTemporalAnchor.ShouldBeNull();
        source.Reads.ShouldBe(0);
    }

    /// <summary>
    /// Projection rebuild and temporal source gaps never produce authoritative historical details.
    /// </summary>
    [Fact]
    public async Task RebuildingOrIncompleteSourcesShouldReturnConfidenceLimitedResults()
    {
        ConversationTemporalReconstructionService rebuildingProjection = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction(ProjectionTrustState.Rebuilding, ProjectionFreshnessReasonCode.Rebuilding) },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.Available([])));
        ConversationTemporalDetailResult projectionResult = await rebuildingProjection.ReconstructAsync(
            Query(PositionAnchor(1)),
            TestContext.Current.CancellationToken);

        ConversationTemporalReconstructionService gapSource = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.Available(
            [
                Event(1, Created("event-create-001", 1)),
                Event(3, MessageAppended("event-message-001", 3)),
            ])));
        ConversationTemporalDetailResult gapResult = await gapSource.ReconstructAsync(
            Query(PositionAnchor(3)),
            TestContext.Current.CancellationToken);

        projectionResult.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        projectionResult.Details.ShouldBeNull();
        projectionResult.Confidence.IsComplete.ShouldBeFalse();
        gapResult.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        gapResult.Details.ShouldBeNull();
        gapResult.Confidence.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.GapDetected);

        ConversationTemporalReconstructionService incompleteSource = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.Available(
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, ParticipantAdded("event-participant-001", 2)),
            ],
            isComplete: false)));
        ConversationTemporalDetailResult incompleteResult = await incompleteSource.ReconstructAsync(
            Query(PositionAnchor(2)),
            TestContext.Current.CancellationToken);

        incompleteResult.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        incompleteResult.AuthoritativeTemporalAnchor.ShouldBeNull();
        incompleteResult.Details.ShouldBeNull();
        incompleteResult.Confidence.IsComplete.ShouldBeFalse();
    }

    /// <summary>
    /// Out-of-coverage anchors and unsupported schema history return safe non-disclosing failures.
    /// </summary>
    [Fact]
    public async Task OutOfCoverageAndUnsupportedSchemaShouldNotRevealProtectedDetails()
    {
        ConversationTemporalReconstructionService outsideCoverage = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.OutsideCoverage()));
        ConversationTemporalDetailResult outside = await outsideCoverage.ReconstructAsync(
            Query(PositionAnchor(99)),
            TestContext.Current.CancellationToken);

        ConversationTemporalReconstructionService beyondTail = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.Available(
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, ParticipantAdded("event-participant-001", 2)),
                Event(3, MessageAppended("event-message-001", 3)),
            ])));
        ConversationTemporalDetailResult beyondTailResult = await beyondTail.ReconstructAsync(
            Query(PositionAnchor(99)),
            TestContext.Current.CancellationToken);

        ConversationTemporalReconstructionService unsupportedSchema = CreateService(
            AllowedAccess(),
            new FakeProjectionReadStore { Models = CurrentProjectionWithoutRedaction() },
            new FakeTemporalEventSource(ConversationTemporalEventSourceResult.Available(
            [
                Event(1, Created("event-create-001", 1, schemaVersion: new SchemaVersion(2))),
            ])));
        ConversationTemporalDetailResult unsupported = await unsupportedSchema.ReconstructAsync(
            Query(PositionAnchor(1)),
            TestContext.Current.CancellationToken);

        outside.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        outside.Details.ShouldBeNull();
        beyondTailResult.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        beyondTailResult.Details.ShouldBeNull();
        beyondTailResult.AuthoritativeTemporalAnchor.ShouldBeNull();
        unsupported.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        unsupported.Details.ShouldBeNull();
    }

    private static ConversationTemporalReconstructionService CreateService(
        FakeTenantAccessService access,
        FakeProjectionReadStore store,
        FakeTemporalEventSource source)
        => new(access, new ConversationProjectionReadService(access, store), source);

    private static GetConversationAtPointInTimeQuery Query(ConversationTemporalAnchorV1 anchor)
        => new(SchemaVersion.Current, Tenant, "caller-001", "correlation-001", Conversation, anchor);

    private static ConversationTemporalAnchorV1 TimestampAnchor(DateTimeOffset timestamp)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            ConversationTemporalAnchorV1.TimestampKind,
            Timestamp: timestamp);

    private static ConversationTemporalAnchorV1 PositionAnchor(long position)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            ConversationTemporalAnchorV1.SafeSourcePositionKind,
            SafeSourcePosition: position);

    private static ConversationTemporalAnchorV1 ContractCursorAnchor(string cursor)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            ConversationTemporalAnchorV1.ContractCursorKind,
            ContractCursor: cursor);

    private static ConversationTemporalAnchorV1 ProjectionCursorAnchor(string cursor)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            ConversationTemporalAnchorV1.ProjectionCursorKind,
            ProjectionCursor: cursor);

    private static ConversationProjectedReadModels CurrentProjectionWithRedaction()
    {
        ConversationProjectedReadModels current = CurrentProjectionWithoutRedaction();
        ConversationDetailProjectionV1 detail = current.Detail;
        return new(
            current.Summary,
            new ConversationDetailProjectionV1(
                detail.SchemaVersion,
                detail.TenantId,
                detail.ConversationId,
                detail.Freshness,
                detail.LifecycleState,
                detail.Label,
                detail.BusinessReference,
                detail.ProjectId,
                detail.FolderId,
                detail.ProviderCorrelation,
                detail.Participants,
                detail.Messages,
                detail.FileReferences,
                detail.Attributes,
                detail.ActiveRetentionPolicy,
                detail.SensitivityMarks,
                Redactions:
                [
                    new ConversationRedactionProjectionV1(
                        new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
                        RedactionCategory.ContentSuppression,
                        "redaction-policy-standard",
                        "customer-request",
                        Actor,
                        Started.AddSeconds(4),
                        AuditEvidence(4, "redaction-policy-standard"),
                        ProjectionTrustState.Redacted,
                        "[redacted]"),
                ]));
    }

    private static ConversationProjectedReadModels CurrentProjectionWithoutRedaction(
        ProjectionTrustState? state = null,
        ProjectionFreshnessReasonCode? reason = null)
    {
        ProjectionFreshnessV1 freshness = Freshness(state ?? ProjectionTrustState.Current, reason ?? ProjectionFreshnessReasonCode.Current);
        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            freshness,
            "Open",
            "Case 123",
            ParticipantPartyIds: [Participant],
            MessageCount: 1,
            FileReferenceCount: 0);
        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            freshness,
            "Open",
            "Case 123",
            Participants: [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            Messages: [new ConversationTimelineMessageProjectionV1(Message, Actor, "secret customer content", Started.AddSeconds(3))]);
        return new(summary, detail);
    }

    private static ProjectionFreshnessV1 Freshness(ProjectionTrustState state, ProjectionFreshnessReasonCode reason)
        => new(
            SchemaVersion.Current,
            "pos:0000000004",
            4,
            Started.AddSeconds(4),
            Started.AddSeconds(5),
            TimeSpan.FromSeconds(1),
            IsStale: state == ProjectionTrustState.Stale,
            state,
            reason);

    private static ConversationReplayEventRecord Event(long position, object e)
        => new(position, e);

    private static ConversationCreated Created(string eventId, long position, SchemaVersion? schemaVersion = null)
        => new(
            Metadata(eventId, ConversationEventType.ConversationCreated, position, schemaVersion),
            new BusinessReference("crm", "case-123"),
            Label: "Case 123");

    private static ParticipantAdded ParticipantAdded(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.ParticipantAdded, position),
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static MessageAppended MessageAppended(string eventId, long position, string text = "Hello")
        => new(
            Metadata(eventId, ConversationEventType.MessageAppended, position),
            Message,
            Actor,
            text);

    private static MessageContentRedacted Redacted(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.MessageContentRedacted, position),
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AuditEvidence(position, "redaction-policy-standard"));

    private static ConversationEventMetadata Metadata(
        string eventId,
        ConversationEventType eventType,
        long position,
        SchemaVersion? schemaVersion = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            eventId,
            eventType,
            Tenant,
            Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");

    private static GovernanceAuditEvidenceReference AuditEvidence(long position, string policyReference)
        => new(
            new AuditEvidenceHandle("audit-evidence-001"),
            policyReference,
            Started.AddSeconds(position));

    private static FakeTenantAccessService AllowedAccess()
        => new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001"));

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(decision);
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public ConversationProjectedReadModels? Models { get; set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Models);

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult((IReadOnlyList<ConversationSummaryProjectionV1>)[]);
    }

    private sealed class FakeTemporalEventSource(ConversationTemporalEventSourceResult result) : IConversationTemporalEventSource
    {
        public int Reads { get; private set; }

        public ValueTask<ConversationTemporalEventSourceResult> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(result);
        }
    }
}
