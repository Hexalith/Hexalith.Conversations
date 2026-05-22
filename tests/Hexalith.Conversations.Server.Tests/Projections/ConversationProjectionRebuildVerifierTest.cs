// <copyright file="ConversationProjectionRebuildVerifierTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Proves local projection deletion/rebuild and evidence generation without production infrastructure.
/// </summary>
public sealed class ConversationProjectionRebuildVerifierTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly TenantId OtherTenant = new("tenant-other");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly PartyId Participant = new("party-human");
    private static readonly MessageId Message = new("message-alpha");
    private static readonly DateTimeOffset Started = new(2026, 5, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Generated = new(2026, 5, 22, 8, 0, 10, TimeSpan.Zero);

    [Fact]
    public void DeletedProjectionShouldRebuildEquivalentModelsAndProduceSafeEvidence()
    {
        InMemoryProjectionRepository repository = new();
        ConversationProjectionRebuildVerifier verifier = new(new ConversationProjectionMaterializer());

        ConversationProjectionRebuildResult result = verifier.Rebuild(
            Tenant,
            Conversation,
            OrderedEvents(),
            existing: repository.Query(),
            Generated,
            TimeSpan.FromMinutes(5),
            coveredTestIds: ["story-1.11-rebuild-equivalence"]);

        repository.Upsert(result.Models);

        result.Models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.ExistingArtifactDisposition.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Evidence.StoryKey.ShouldBe("1-11-prove-replay-schema-versioning-and-projection-rebuild-behavior");
        result.Evidence.CoveredTestIds.ShouldBe(["story-1.11-rebuild-equivalence"], ignoreOrder: false);
        result.Evidence.SchemaVersion.ShouldBe(SchemaVersion.Current);
        result.Evidence.ProjectionContractVersion.ShouldBe(SchemaVersion.Current);
        result.Evidence.TenantId.ShouldBe(Tenant);
        result.Evidence.ConversationId.ShouldBe(Conversation);
        result.Evidence.RebuildStatus.ShouldBe(ProjectionTrustState.Current);
        result.Evidence.Passed.ShouldBeTrue();
        result.Evidence.SafeDiagnosticCode.ShouldBe(ProjectionFreshnessReasonCode.Current);
        result.Evidence.ProducedAt.ShouldBe(Generated);
        result.Evidence.Cursor.ShouldBe("pos:0000000003");
        result.Evidence.ToString().ShouldNotContain("stream-");
        result.Evidence.ToString().ShouldNotContain("provider-session");
    }

    [Fact]
    public void DerivedStateDisagreementShouldMarkExistingArtifactStaleAndEventStoreRebuildWins()
    {
        ConversationProjectionRebuildVerifier verifier = new(new ConversationProjectionMaterializer());
        ConversationProjectedReadModels existing = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [Event(1, Created("event-create-001", 1, label: "Wrong"))],
            Generated,
            TimeSpan.FromMinutes(5));

        ConversationProjectionRebuildResult result = verifier.Rebuild(
            Tenant,
            Conversation,
            OrderedEvents(),
            existing,
            Generated,
            TimeSpan.FromMinutes(5),
            coveredTestIds: ["story-1.11-derived-state-disagreement"]);

        result.ExistingArtifactDisposition.ShouldBe(ProjectionTrustState.Stale);
        result.Models.Summary.Label.ShouldBe("Case 123");
        result.Models.Detail.Messages.Single().MessageId.ShouldBe(Message);
        result.Evidence.Passed.ShouldBeTrue();
    }

    [Fact]
    public void UnsupportedVersionAndMixedTenantPoisonShouldNeverProduceCurrentEvidence()
    {
        ConversationProjectionRebuildVerifier verifier = new(new ConversationProjectionMaterializer());

        ConversationProjectionRebuildResult unsupported = verifier.Rebuild(
            Tenant,
            Conversation,
            [Event(1, Created("event-create-001", 1, schemaVersion: new SchemaVersion(2)))],
            existing: null,
            Generated,
            TimeSpan.FromMinutes(5),
            coveredTestIds: ["story-1.11-unsupported-version"]);

        ConversationProjectionRebuildResult poison = verifier.Rebuild(
            Tenant,
            Conversation,
            [
                Event(1, Created("event-create-001", 1)),
                Event(2, ParticipantAdded("event-participant-001", 2, tenantId: OtherTenant)),
            ],
            existing: null,
            Generated,
            TimeSpan.FromMinutes(5),
            coveredTestIds: ["story-1.11-tenant-poison"]);

        unsupported.Models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        unsupported.Evidence.Passed.ShouldBeFalse();
        unsupported.Evidence.SafeDiagnosticCode.ShouldBe(ProjectionFreshnessReasonCode.Unavailable);
        poison.Models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        poison.Evidence.Passed.ShouldBeFalse();
        poison.Evidence.SafeDiagnosticCode.ShouldBe(ProjectionFreshnessReasonCode.PoisonEvent);
    }

    [Theory]
    [InlineData(ProjectionTrustStateNames.Forbidden)]
    [InlineData(ProjectionTrustStateNames.Unavailable)]
    [InlineData(ProjectionTrustStateNames.Rebuilding)]
    public void SideChannelStatusesShouldExposeSamePublicShape(string trustState)
    {
        ProjectionFreshnessV1 first = NonCurrentFreshness(ProjectionTrustState.Parse(trustState));
        ProjectionFreshnessV1 second = NonCurrentFreshness(ProjectionTrustState.Parse(trustState));

        first.FreshnessState.ShouldBe(second.FreshnessState);
        first.LastAppliedEventPosition.ShouldBe(second.LastAppliedEventPosition);
        first.ProjectionCursor.ShouldBe(second.ProjectionCursor);
        first.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    [Fact]
    public void DifferentFailureCausesShouldExposeEquivalentNonCurrentPublicShape()
    {
        // AC8: unauthorized/nonexistent/cross-tenant/unsupported-version/poison cases must not
        // distinguish ownership or existence through public projection shape.
        ConversationProjectionRebuildVerifier verifier = new(new ConversationProjectionMaterializer());

        ConversationProjectionRebuildResult unsupported = verifier.Rebuild(
            Tenant,
            Conversation,
            [Event(1, Created("event-create-001", 1, schemaVersion: new SchemaVersion(2)))],
            existing: null,
            Generated,
            TimeSpan.FromMinutes(5),
            coveredTestIds: ["story-1.11-sidechannel-unsupported"]);

        ConversationProjectionRebuildResult crossTenant = verifier.Rebuild(
            Tenant,
            Conversation,
            [Event(1, Created("event-create-001", 1, tenantId: OtherTenant))],
            existing: null,
            Generated,
            TimeSpan.FromMinutes(5),
            coveredTestIds: ["story-1.11-sidechannel-cross-tenant"]);

        ConversationProjectionRebuildResult nonexistent = verifier.Rebuild(
            Tenant,
            Conversation,
            [],
            existing: null,
            Generated,
            TimeSpan.FromMinutes(5),
            coveredTestIds: ["story-1.11-sidechannel-nonexistent"]);

        ConversationProjectionRebuildResult[] cases = [unsupported, crossTenant, nonexistent];
        foreach (ConversationProjectionRebuildResult result in cases)
        {
            result.Models.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
            result.Models.Summary.MessageCount.ShouldBe(0);
            result.Models.Summary.FileReferenceCount.ShouldBe(0);
            result.Models.Summary.ParticipantPartyIds.ShouldBeEmpty();
            result.Models.Detail.Messages.ShouldBeEmpty();
            result.Models.Detail.Participants.ShouldBeEmpty();
            result.Models.Detail.FileReferences.ShouldBeEmpty();
            result.Evidence.Passed.ShouldBeFalse();

            string serialized = result.Evidence.ToString();
            serialized.ShouldNotContain("tenant-other", Case.Insensitive);
            serialized.ShouldNotContain("stream-", Case.Insensitive);
            serialized.ShouldNotContain("offset:", Case.Insensitive);
            serialized.ShouldNotContain("dapr", Case.Insensitive);
            serialized.ShouldNotContain("signalr", Case.Insensitive);
            serialized.ShouldNotContain("Exception:", Case.Insensitive);
            serialized.ShouldNotContain("@");
        }

        // Public cursor format must be identical across the failure causes so callers cannot use
        // it to distinguish unsupported vs cross-tenant vs nonexistent.
        unsupported.Models.Summary.Freshness.ProjectionCursor.ShouldBe(crossTenant.Models.Summary.Freshness.ProjectionCursor);
        crossTenant.Models.Summary.Freshness.ProjectionCursor.ShouldBe(nonexistent.Models.Summary.Freshness.ProjectionCursor);
    }

    private static ConversationProjectionEventRecord[] OrderedEvents() =>
    [
        Event(1, Created("event-create-001", 1)),
        Event(2, ParticipantAdded("event-participant-001", 2)),
        Event(3, MessageAppended("event-message-001", 3)),
    ];

    private static ConversationProjectionEventRecord Event(long position, object e)
        => new(position, e);

    private static ConversationCreated Created(
        string eventId,
        long position,
        string label = "Case 123",
        SchemaVersion? schemaVersion = null,
        TenantId? tenantId = null)
        => new(
            Metadata(eventId, ConversationEventType.ConversationCreated, position, schemaVersion, tenantId),
            new BusinessReference("crm", "case-123"),
            null,
            null,
            label);

    private static ParticipantAdded ParticipantAdded(string eventId, long position, TenantId? tenantId = null)
        => new(
            Metadata(eventId, ConversationEventType.ParticipantAdded, position, tenantId: tenantId),
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static MessageAppended MessageAppended(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.MessageAppended, position),
            Message,
            Actor,
            "Hello",
            new ProviderCorrelationMetadata("contoso-ai", "assistant", SchemaVersion.Current, "provider-session-001"));

    private static ConversationEventMetadata Metadata(
        string eventId,
        ConversationEventType eventType,
        long position,
        SchemaVersion? schemaVersion = null,
        TenantId? tenantId = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            eventId,
            eventType,
            tenantId ?? Tenant,
            Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");

    private static ProjectionFreshnessV1 NonCurrentFreshness(ProjectionTrustState state)
        => new(
            SchemaVersion.Current,
            "pos:0000000001",
            1,
            Started,
            Generated,
            Generated - Started,
            state == ProjectionTrustState.Stale,
            state,
            state == ProjectionTrustState.Forbidden
                ? ProjectionFreshnessReasonCode.Forbidden
                : state == ProjectionTrustState.Rebuilding
                    ? ProjectionFreshnessReasonCode.Rebuilding
                    : ProjectionFreshnessReasonCode.Unavailable);

    private sealed class InMemoryProjectionRepository
    {
        private ConversationProjectedReadModels? _models;

        public ConversationProjectedReadModels? Query() => _models;

        public void Upsert(ConversationProjectedReadModels models) => _models = models;
    }

    private static class ProjectionTrustStateNames
    {
        public const string Forbidden = "Forbidden";
        public const string Rebuilding = "Rebuilding";
        public const string Unavailable = "Unavailable";
    }
}
