// <copyright file="ConversationReplayDeterminismTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Story 16.2 / AD-8: replay identity and time come from validated event inputs, duplicates are bound to their full
/// binding, and identical histories produce byte-identical canonical replay state under any clock.
/// </summary>
public sealed class ConversationReplayDeterminismTest
{
    private static readonly TenantId Tenant = new("tenant-replay");
    private static readonly ConversationId Conversation = new("conversation-replay");
    private static readonly PartyId Actor = new("party-replay-actor");
    private static readonly DateTimeOffset OccurredAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>A contract event's replay time is its OccurredAt; the envelope timestamp is diagnostic only.</summary>
    [Fact]
    public void ContractEventShouldTakeIdentityFromEventIdAndTimeFromOccurredAt()
    {
        ProjectionEventDto envelope = CreatedDto(sequence: 1) with { Timestamp = OccurredAt.AddHours(3) };

        ConversationProjectedReadModels models = Project([envelope], OccurredAt.AddSeconds(30));

        models.ReplayAnchorAt.ShouldBe(OccurredAt);
        models.Summary.Freshness.LastAppliedEventTimestamp.ShouldBe(OccurredAt);
        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        models.Summary.Freshness.LagDuration.ShouldBe(TimeSpan.FromSeconds(30));
    }

    /// <summary>A position-only event's identity is its persisted message identity and its time the envelope timestamp.</summary>
    [Fact]
    public void PositionOnlyEventShouldTakeIdentityFromMessageIdAndTimeFromTheEnvelope()
    {
        ProjectionEventDto rejection = RejectionDto(sequence: 2, "message-rejection-1", OccurredAt.AddSeconds(5));

        ConversationProjectedReadModels models = Project([CreatedDto(1), rejection, rejection], OccurredAt.AddSeconds(10));

        models.ReplayAnchorAt.ShouldBe(OccurredAt.AddSeconds(5));
        models.Summary.Freshness.LastAppliedEventPosition.ShouldBe(2);
        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current, "an exact re-delivery is a no-op");
        models.Summary.Freshness.LagDuration.ShouldBe(TimeSpan.FromSeconds(5));
    }

    /// <summary>A missing identity or a default/invalid time fails before duplicate suppression.</summary>
    /// <param name="defect">The input defect.</param>
    [Theory]
    [InlineData("blank-message-id")]
    [InlineData("whitespace-message-id")]
    [InlineData("default-envelope-time")]
    [InlineData("implausible-envelope-time")]
    [InlineData("blank-event-id")]
    [InlineData("default-occurred-at")]
    [InlineData("valid-identity-then-default-time")]
    public void MissingIdentityOrInvalidTimeShouldBeContradictoryBeforeDeduplication(string defect)
    {
        List<ConversationProjectionEventRecord> records = [.. Decode(CreatedDto(1))];
        DateTimeOffset at = OccurredAt.AddSeconds(5);
        switch (defect)
        {
            case "blank-message-id":
                records.AddRange(Decode(RejectionDto(2, string.Empty, at)));
                break;
            case "whitespace-message-id":
                records.AddRange(Decode(RejectionDto(2, "  ", at)));
                break;
            case "default-envelope-time":
                records.AddRange(Decode(RejectionDto(2, "message-rejection-1", default)));
                break;
            case "implausible-envelope-time":
                records.AddRange(Decode(RejectionDto(2, "message-rejection-1", new DateTimeOffset(1999, 12, 31, 0, 0, 0, TimeSpan.Zero))));
                break;
            case "blank-event-id":
                records[0] = new ConversationProjectionEventRecord(1, Created() with { Metadata = Metadata() with { EventId = " " } });
                break;
            case "default-occurred-at":
                records[0] = new ConversationProjectionEventRecord(1, Created() with { Metadata = Metadata() with { OccurredAt = default } });
                break;
            case "valid-identity-then-default-time":
                // The second delivery reuses a valid identity: it must be rejected, not swallowed as a duplicate.
                records.AddRange(Decode(RejectionDto(2, "message-rejection-1", at)));
                records.AddRange(Decode(RejectionDto(2, "message-rejection-1", default)));
                break;
        }

        ConversationProjectedReadModels models = new ConversationProjectionMaterializer().Project(
            Tenant, Conversation, records, OccurredAt.AddMinutes(1), StaleAfter);

        AssertContradictory(models.Summary.Freshness);
        AssertContradictory(models.Detail.Freshness);
    }

    /// <summary>
    /// The synchronous seam reports a contract payload without identity, or with a default/implausible occurrence
    /// time, as contradictory, never as rebuilding.
    /// </summary>
    /// <param name="field">The metadata field the payload corrupts.</param>
    /// <param name="value">The corrupted value.</param>
    [Theory]
    [InlineData("eventId", " ")]
    [InlineData("occurredAt", "0001-01-01T00:00:00+00:00")]
    [InlineData("occurredAt", "1999-12-31T23:59:59+00:00")]
    public void SynchronousSeamShouldMapInvalidContractIdentityOrTimeToMetadataContradictory(string field, string value)
    {
        JsonNode payload = JsonSerializer.SerializeToNode(Created(), JsonOptions)!;
        payload["metadata"]![field] = value;
        ProjectionRequest request = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [CreatedDto(1) with { Payload = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions) }]);

        ProjectionResponse response = new ConversationProjectionHandler(
            new ConversationProjectionMaterializer(),
            new FixedTimeProvider(OccurredAt.AddSeconds(1))).Project(request);

        ConversationProjectedReadModels models = response.State.Deserialize<ConversationProjectedReadModels>(JsonOptions)!;
        AssertContradictory(models.Summary.Freshness);
    }

    /// <summary>Re-delivering one identity with its exact binding is a no-op.</summary>
    [Fact]
    public void SameIdentityAndBindingShouldBeANoOp()
    {
        ConversationProjectedReadModels once = Project([CreatedDto(1)], OccurredAt.AddSeconds(1));
        ConversationProjectedReadModels twice = Project([CreatedDto(1), CreatedDto(1)], OccurredAt.AddSeconds(1));

        twice.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        twice.ReplayStateHash.ShouldBe(once.ReplayStateHash);
        twice.ReplayAnchorAt.ShouldBe(once.ReplayAnchorAt);
    }

    /// <summary>Reusing one identity with any different binding member is contradictory metadata.</summary>
    /// <param name="member">The binding member that differs.</param>
    [Theory]
    [InlineData("type")]
    [InlineData("sequence")]
    [InlineData("format")]
    [InlineData("payload")]
    [InlineData("utc-ticks")]
    public void SameIdentityWithDifferentBindingShouldBeContradictory(string member)
    {
        ProjectionEventDto[] envelopes = member switch
        {
            "type" => [CreatedDto(1), CreatedDto(1) with { EventTypeName = typeof(ConversationCreated).FullName! }],
            "sequence" => [CreatedDto(1), CreatedDto(2)],
            "format" => [CreatedDto(1), CreatedDto(1) with { SerializationFormat = "JSON" }],
            "payload" => [CreatedDto(1), CreatedDto(1, Created() with { Label = "Changed reuse" })],
            "utc-ticks" =>
            [
                CreatedDto(1),
                RejectionDto(2, "message-rejection-1", OccurredAt.AddSeconds(5)),
                RejectionDto(2, "message-rejection-1", OccurredAt.AddSeconds(6)),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(member)),
        };

        ConversationProjectedReadModels models = Project(envelopes, OccurredAt.AddMinutes(1));

        AssertContradictory(models.Summary.Freshness);
    }

    /// <summary>
    /// A generation instant earlier than the anchor is contradictory: the generation time is not clamped and the
    /// lag is null.
    /// </summary>
    [Fact]
    public void GenerationEarlierThanTheAnchorShouldBeContradictoryWithoutClamping()
    {
        DateTimeOffset generatedAt = OccurredAt.AddSeconds(-30);

        ConversationProjectedReadModels models = Project([CreatedDto(1)], generatedAt);

        ProjectionFreshnessV1 freshness = models.Summary.Freshness;
        AssertContradictory(freshness);
        freshness.ProjectionGeneratedAt.ShouldBe(generatedAt, "the generation instant is never clamped to the anchor");
        freshness.LastAppliedEventTimestamp.ShouldBe(ConversationProjectionMaterializer.NoReplayAnchor);
        models.ReplayAnchorAt.ShouldBe(OccurredAt, "the internal replay anchor still records the event time");
        models.ReplayStateHash.ShouldBe(Project([CreatedDto(1)], OccurredAt.AddDays(1)).ReplayStateHash);
    }

    /// <summary>
    /// Public freshness binds the replay anchor, the actual generation instant and their difference; eligibility and
    /// evidence times come from the anchor, never from the clock.
    /// </summary>
    [Fact]
    public void FreshnessShouldBindTheAnchorTheGenerationInstantAndTheirDifference()
    {
        DateTimeOffset generatedAt = OccurredAt.AddSeconds(90);

        ConversationProjectedReadModels models = Project([CreatedDto(1)], generatedAt);

        ProjectionFreshnessV1 freshness = models.Detail.Freshness;
        freshness.LastAppliedEventTimestamp.ShouldBe(OccurredAt);
        models.ReplayAnchorAt.ShouldBe(OccurredAt);
        freshness.ProjectionGeneratedAt.ShouldBe(generatedAt);
        freshness.LagDuration.ShouldBe(generatedAt - OccurredAt);
        models.Detail.TrustPosture.CommandEligibility.ShouldAllBe(command => command.LastEvaluatedAt == OccurredAt);
        models.Detail.EvidenceEntries.Single(entry => entry.Kind == "Freshness").OccurredAt.ShouldBe(OccurredAt);
    }

    /// <summary>An empty replay has no anchor: the public timestamp is the fixed sentinel, never the clock.</summary>
    [Fact]
    public void ReplayWithoutAValidTimeShouldNotFallBackToTheClock()
    {
        DateTimeOffset generatedAt = OccurredAt.AddDays(2);

        ConversationProjectedReadModels models = new ConversationProjectionMaterializer().Project(
            Tenant, Conversation, [], generatedAt, StaleAfter);

        models.ReplayAnchorAt.ShouldBeNull();
        models.Summary.Freshness.LastAppliedEventTimestamp.ShouldBe(ConversationProjectionMaterializer.NoReplayAnchor);
        models.Summary.Freshness.ProjectionGeneratedAt.ShouldBe(generatedAt);
        models.Summary.Freshness.LagDuration.ShouldBeNull();
        models.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
    }

    /// <summary>The replay state excludes observation freshness but keeps position and anchor.</summary>
    [Fact]
    public void ReplayStateShouldExcludeObservationFreshnessAndKeepPositionAndAnchor()
    {
        ConversationReplayState state = ConversationProjectionMaterializer.BuildReplayState(Tenant, Conversation, Decode(CreatedDto(1)));

        JsonNode node = JsonNode.Parse(state.CanonicalBytes.Span)!;
        node["lastAppliedEventPosition"]!.GetValue<long>().ShouldBe(1);
        node["replayAnchorAt"]!.GetValue<DateTimeOffset>().ShouldBe(OccurredAt);
        string json = node.ToJsonString();
        json.ShouldNotContain("projectionGeneratedAt");
        json.ShouldNotContain("lagDuration");
        json.ShouldNotContain("isStale");
        json.ShouldContain("lastAppliedEventTimestamp");
        state.Hash.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(state.CanonicalBytes.Span)));
    }

    /// <summary>The canonical bytes survive deserialize-and-canonical-reserialize unchanged.</summary>
    [Fact]
    public void ReplayStateCanonicalBytesShouldSurviveCanonicalReserialization()
    {
        ConversationReplayState state = ConversationProjectionMaterializer.BuildReplayState(Tenant, Conversation, Decode(CreatedDto(1)));

        byte[] reserialized = ConversationReplayStateCanonicalizer.Canonicalize(JsonNode.Parse(state.CanonicalBytes.Span)!);

        reserialized.ShouldBe(state.CanonicalBytes.ToArray());
    }

    /// <summary>An unordered collection without a stable composite key fails before any byte is produced.</summary>
    [Fact]
    public void ReplayStateShouldRejectUnorderedCollectionsBeforePersistence()
    {
        Should.Throw<InvalidOperationException>(() => ConversationReplayStateCanonicalizer.ToOrderedNode(
            new UnorderedReplayValue(new HashSet<string>(StringComparer.Ordinal) { "b", "a" })));
        Should.Throw<InvalidOperationException>(() => ConversationReplayStateCanonicalizer.ToOrderedNode(
            new HashSet<string>(StringComparer.Ordinal) { "a" }));
        ConversationReplayStateCanonicalizer.ToOrderedNode(new OrderedReplayValue(["a", "b"])).ShouldNotBeNull();
    }

    /// <summary>
    /// AC 1: two clean rebuilds of an identical history under different clocks persist byte-identical replay state,
    /// <c>ReplayAnchorAt</c> and <c>ReplayStateHash</c>; observation freshness differs and binds each clock.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task TwoCleanRebuildsUnderDifferentClocksShouldProduceByteIdenticalReplayState()
    {
        ProjectionEventDto[] history =
        [
            CreatedDto(1),
            RejectionDto(2, "message-rejection-1", OccurredAt.AddSeconds(4)),
        ];
        DateTimeOffset clockA = OccurredAt.AddSeconds(20);
        DateTimeOffset clockB = OccurredAt.AddHours(6);

        ConversationProjectedReadModels first = await RebuildAsync(history, clockA);
        ConversationProjectedReadModels second = await RebuildAsync(history, clockB);
        ConversationReplayState expected = ConversationProjectionMaterializer.BuildReplayState(
            Tenant, Conversation, ConversationProjectionEventDecoder.Decode(history));

        first.ReplayAnchorAt.ShouldBe(OccurredAt.AddSeconds(4));
        second.ReplayAnchorAt.ShouldBe(first.ReplayAnchorAt);
        first.ReplayStateHash.ShouldBe(expected.Hash, "the persisted hash is the SHA-256 of the canonical replay-state bytes");
        second.ReplayStateHash.ShouldBe(first.ReplayStateHash);
        ConversationProjectionMaterializer.BuildReplayState(Tenant, Conversation, ConversationProjectionEventDecoder.Decode(history))
            .CanonicalBytes.ToArray().ShouldBe(expected.CanonicalBytes.ToArray());
        foreach ((ConversationProjectedReadModels models, DateTimeOffset clock) in new[] { (first, clockA), (second, clockB) })
        {
            ProjectionFreshnessV1 freshness = models.Summary.Freshness;
            freshness.LastAppliedEventTimestamp.ShouldBe(models.ReplayAnchorAt!.Value);
            freshness.ProjectionGeneratedAt.ShouldBe(clock);
            freshness.LagDuration.ShouldBe(clock - models.ReplayAnchorAt.Value);
        }

        first.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        second.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Stale, "observation freshness is generated anew");
    }

    /// <summary>Immediate dispatch under two clocks persists the same replay identity.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task DispatchUnderDifferentClocksShouldPersistTheSameReplayIdentity()
    {
        ProjectionRequest request = Request([CreatedDto(1)]);
        ConversationProjectedReadModels[] persisted = new ConversationProjectedReadModels[2];
        DateTimeOffset[] clocks = [OccurredAt.AddSeconds(3), OccurredAt.AddMinutes(30)];
        for (int index = 0; index < clocks.Length; index++)
        {
            InMemoryReadModelStore store = new();
            DomainProjectionHandlerResult result = await Handler(store, clocks[index])
                .ProjectAsync(request, "dispatch-replay", TestContext.Current.CancellationToken);
            result.Status.ShouldBe(ProjectionDispatchStatus.Completed);
            persisted[index] = store.Snapshot<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation))!;
        }

        persisted[0].ReplayStateHash.ShouldNotBeNullOrWhiteSpace();
        persisted[1].ReplayStateHash.ShouldBe(persisted[0].ReplayStateHash);
        persisted[1].ReplayAnchorAt.ShouldBe(persisted[0].ReplayAnchorAt);
        persisted[0].Summary.Freshness.ProjectionGeneratedAt.ShouldBe(clocks[0]);
        persisted[1].Summary.Freshness.ProjectionGeneratedAt.ShouldBe(clocks[1]);
    }

    /// <summary>A missing position-only identity is rejected even when the slice has nothing to publish.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task PositionOnlySliceWithoutIdentityShouldFailWithoutWritingProjectionState()
    {
        InMemoryReadModelStore store = new();

        DomainProjectionHandlerResult result = await Handler(store, OccurredAt.AddSeconds(3)).ProjectAsync(
            Request([RejectionDto(1, string.Empty, OccurredAt)]),
            "dispatch-rejection-without-identity",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldBeNull();
        store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant)).ShouldBeNull();
    }

    /// <summary>
    /// The rebuild's duplicate-row tie-break uses replay facts only: which equal-position sibling survives does not
    /// depend on which row carries the later generation clock.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RebuildDuplicateTieBreakShouldNotDependOnTheGenerationClock()
    {
        ConversationId sibling = new("conversation-sibling");
        string[] survivors = new string[2];
        for (int order = 0; order < survivors.Length; order++)
        {
            ConversationSummaryProjectionV1 first = SiblingSummary(sibling, "Sibling A", OccurredAt.AddMinutes(order == 0 ? 1 : 2));
            ConversationSummaryProjectionV1 second = SiblingSummary(sibling, "Sibling B", OccurredAt.AddMinutes(order == 0 ? 2 : 1));
            InMemoryReadModelStore store = new();
            store.SeedRaw(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
                new ConversationProjectionIndexReadModel
                {
                    Summaries = [first, second],
                    Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
                    {
                        [sibling.Value] = new("dispatch-sibling", 1),
                    },
                });

            DomainProjectionRebuildPlan plan = await Handler(store, OccurredAt.AddMinutes(3)).PrepareRebuildAsync(
                Request([CreatedDto(1)]), "rebuild-tie-break", TestContext.Current.CancellationToken);

            ConversationProjectionIndexReadModel index = JsonSerializer.Deserialize<ConversationProjectionIndexReadModel>(
                plan.Operations.Single(operation => operation.Key == ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))
                    .CanonicalValue.Span,
                JsonOptions)!;
            survivors[order] = index.Summaries.Single(summary => summary.ConversationId == sibling).Label!;
            index.Lifecycle.ShouldBe(ConversationProjectionIndexLifecycle.Rebuilding, "a rebuild never makes a version 1 index Ready");
        }

        survivors[1].ShouldBe(survivors[0]);
    }

    private static ConversationSummaryProjectionV1 SiblingSummary(ConversationId conversation, string label, DateTimeOffset generatedAt)
        => new ConversationProjectionMaterializer().Project(
            Tenant,
            conversation,
            [new ConversationProjectionEventRecord(1, new ConversationCreated(
                Metadata() with { ConversationId = conversation, EventId = $"event-{label}" },
                Label: label))],
            generatedAt,
            TimeSpan.FromDays(1)).Summary;

    private static async Task<ConversationProjectedReadModels> RebuildAsync(ProjectionEventDto[] history, DateTimeOffset clock)
    {
        InMemoryReadModelStore store = new();
        DomainProjectionRebuildPlan plan = await Handler(store, clock).PrepareRebuildAsync(
            Request(history), "rebuild-replay", TestContext.Current.CancellationToken);
        ReadModelBatchOperation detail = plan.Operations.Single(operation =>
            operation.Key == ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation));
        return JsonSerializer.Deserialize<ConversationProjectedReadModels>(detail.CanonicalValue.Span, JsonOptions)!;
    }

    private static void AssertContradictory(ProjectionFreshnessV1 freshness)
    {
        freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataContradictory);
        freshness.LagDuration.ShouldBeNull();
        freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    private static ConversationProjectedReadModels Project(IEnumerable<ProjectionEventDto> envelopes, DateTimeOffset generatedAt)
        => new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [.. envelopes.SelectMany(Decode)],
            generatedAt,
            StaleAfter);

    private static IReadOnlyList<ConversationProjectionEventRecord> Decode(ProjectionEventDto envelope)
        => ConversationProjectionEventDecoder.Decode([envelope]);

    private static ConversationAsyncProjectionHandler Handler(IReadModelStore store, DateTimeOffset clock)
        => new(
            new ConversationProjectionMaterializer(),
            new ConversationProjectionReadModelWriter(store),
            store,
            new FixedTimeProvider(clock));

    private static ProjectionRequest Request(ProjectionEventDto[] events)
        => new(Tenant.Value, ConversationProjectionHandler.ConversationDomain, Conversation.Value, events);

    private static ProjectionEventDto CreatedDto(long sequence, ConversationCreated? created = null)
        => new(
            nameof(ConversationCreated),
            JsonSerializer.SerializeToUtf8Bytes(created ?? Created(), JsonOptions),
            "json",
            sequence,
            OccurredAt,
            "correlation-replay",
            MessageId: $"message-created-{sequence}");

    private static ProjectionEventDto RejectionDto(long sequence, string messageId, DateTimeOffset timestamp)
        => new(
            nameof(ConversationRejectedDomainEvent),
            JsonSerializer.SerializeToUtf8Bytes(
                new ConversationRejectedDomainEvent(ConversationErrorCode.CommandValidationFailed, "invalid-command"),
                JsonOptions),
            "json",
            sequence,
            timestamp,
            "correlation-replay",
            MessageId: messageId);

    private static ConversationCreated Created()
        => new(Metadata(), Label: "Replay determinism");

    private static ConversationEventMetadata Metadata()
        => new(
            SchemaVersion.Current,
            "event-created-replay",
            ConversationEventType.ConversationCreated,
            Tenant,
            Conversation,
            "correlation-replay",
            OccurredAt,
            Actor,
            "causation-replay");

    private sealed record UnorderedReplayValue(IReadOnlySet<string> Members);

    private sealed record OrderedReplayValue(IReadOnlyList<string> Members);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
