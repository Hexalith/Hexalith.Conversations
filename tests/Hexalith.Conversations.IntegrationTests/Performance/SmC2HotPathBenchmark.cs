// <copyright file="SmC2HotPathBenchmark.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Conversations.IntegrationTests.Performance;

/// <summary>
/// Captures the frozen SM-C2 warm-path workload through the production domain, idempotency, and query paths.
/// </summary>
/// <remarks>
/// The fixture deliberately reports raw per-operation samples. Release evidence binds those samples to the
/// source revision and runtime envelope; the fixture itself does not rewrite committed evidence artifacts.
/// </remarks>
public sealed class SmC2HotPathBenchmark
{
    private const int OperationsPerSample = 2_000;
    private const int Repetitions = 30;
    private const int WarmupRepetitions = 5;
    private const string Caller = "caller-sm-c2";

    private static readonly TenantId Tenant = new("tenant-sm-c2");
    private static readonly ConversationId Conversation = new("conversation-000");
    private static readonly PartyId Actor = new("party-sm-c2");
    private static readonly PartyId Participant = new("party-sm-c2-participant");
    private static readonly MessageId Message = new("message-sm-c2");
    private static readonly BusinessReference Business = new("crm", "case-sm-c2");
    private static readonly ProjectId Project = new("project-sm-c2");
    private static readonly FolderId Folder = new("folder-sm-c2");
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Emits one raw sample set for every frozen warm-path inventory row.</summary>
    [Fact]
    public async Task FrozenWarmPathInventoryEmitsComparableRawSamples()
    {
        CreateConversation create = CreateCommand();
        AppendMessageCommand append = AppendCommand("SM-C2 payload");
        AppendMessageCommand duplicate = AppendCommand("SM-C2 payload");
        AppendMessageCommand mismatch = AppendCommand("SM-C2 changed payload");
        ConversationQueryHandler queryHandler = CreateQueryHandler();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Prove the measured delegates exercise the required semantics before timing them.
        (ConversationIdempotencyDecisionKind accepted, ConversationIdempotencyDecisionKind replay, ConversationIdempotencyDecisionKind rejected) =
            await ExecuteAppendMixAsync(append, duplicate, mismatch, cancellationToken);
        (accepted, replay, rejected).ShouldBe((
            ConversationIdempotencyDecisionKind.Reserved,
            ConversationIdempotencyDecisionKind.Duplicate,
            ConversationIdempotencyDecisionKind.Conflict));
        (int firstPage, int secondPage) = await ListTwoPagesAsync(queryHandler, cancellationToken);
        (firstPage, secondPage).ShouldBe((25, 25));
        ConversationDetailResult opened = await queryHandler.GetAsync(OpenQuery(), cancellationToken);
        opened.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        opened.Details.ShouldNotBeNull();

        Dictionary<string, double[]> samples = new(StringComparer.Ordinal)
        {
            ["HP-CREATE"] = Measure(() => ConversationAggregate.Handle(create, state: null)),
            ["HP-APPEND"] = await MeasureAsync(() => ExecuteAppendMixAsync(append, duplicate, mismatch, cancellationToken)),
            ["HP-LIST"] = await MeasureAsync(() => ListTwoPagesAsync(queryHandler, cancellationToken)),
            ["HP-OPEN"] = await MeasureAsync(() => queryHandler.GetAsync(OpenQuery(), cancellationToken)),
        };

        samples.Keys.OrderBy(static value => value, StringComparer.Ordinal)
            .ShouldBe(["HP-APPEND", "HP-CREATE", "HP-LIST", "HP-OPEN"]);
        samples.ShouldAllBe(static row => row.Value.Length == Repetitions && row.Value.All(value => value > 0));

        foreach ((string hotPathId, double[] raw) in samples.OrderBy(static row => row.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"SM-C2|{hotPathId}|raw-microseconds={string.Join(',', raw.Select(static value => value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)))}|p95-microseconds={Percentile95(raw):F6}");
        }
    }

    private static async ValueTask<(ConversationIdempotencyDecisionKind, ConversationIdempotencyDecisionKind, ConversationIdempotencyDecisionKind)>
        ExecuteAppendMixAsync(
            AppendMessageCommand append,
            AppendMessageCommand duplicate,
            AppendMessageCommand mismatch,
            CancellationToken cancellationToken)
    {
        var store = new InMemoryConversationIdempotencyStore();
        ConversationCommandFingerprint acceptedFingerprint = ConversationCommandFingerprint.Create(append, Conversation);
        ConversationCommandFingerprint replayFingerprint = ConversationCommandFingerprint.Create(duplicate, Conversation);
        ConversationCommandFingerprint mismatchFingerprint = ConversationCommandFingerprint.Create(mismatch, Conversation);
        ConversationIdempotencyDecision accepted = await store.ReserveAsync(
            acceptedFingerprint,
            Timestamp,
            TimeSpan.FromHours(24),
            cancellationToken);
        await store.CompleteAsync(
            acceptedFingerprint,
            ConversationIdempotencyOutcome.Success(
                SchemaVersion.Current,
                Tenant,
                ConversationCommandType.AppendMessageCommand,
                Conversation,
                Message,
                participantPartyId: null,
                fileId: null,
                "correlation-sm-c2",
                "audit-sm-c2"),
            Timestamp.AddMilliseconds(1),
            cancellationToken);
        ConversationIdempotencyDecision replay = await store.ReserveAsync(
            replayFingerprint,
            Timestamp.AddMilliseconds(2),
            TimeSpan.FromHours(24),
            cancellationToken);
        ConversationIdempotencyDecision rejected = await store.ReserveAsync(
            mismatchFingerprint,
            Timestamp.AddMilliseconds(3),
            TimeSpan.FromHours(24),
            cancellationToken);
        return (accepted.Kind, replay.Kind, rejected.Kind);
    }

    private static async ValueTask<(int FirstPage, int SecondPage)> ListTwoPagesAsync(
        ConversationQueryHandler handler,
        CancellationToken cancellationToken)
    {
        ConversationListFilterV1 filter = new(BusinessReference: Business);
        ConversationListResult first = await handler.ListAsync(new ListConversationsQuery(
            SchemaVersion.Current,
            Tenant,
            Caller,
            "correlation-sm-c2-list",
            filter,
            new ConversationPageRequest(25)),
            cancellationToken);
        first.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        first.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace();
        ConversationListResult second = await handler.ListAsync(new ListConversationsQuery(
            SchemaVersion.Current,
            Tenant,
            Caller,
            "correlation-sm-c2-list",
            filter,
            new ConversationPageRequest(25, first.Page.ContinuationCursor)),
            cancellationToken);
        second.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        return (first.Conversations.Count, second.Conversations.Count);
    }

    private static GetConversationQuery OpenQuery()
        => new(SchemaVersion.Current, Tenant, Caller, "correlation-sm-c2-open", Conversation);

    private static ConversationQueryHandler CreateQueryHandler()
    {
        var store = new InMemoryReadModelStore();
        SeedReadModels(store);
        var access = new AllowTenantAccessService();
        var readStore = new ConversationProjectionReadStore(store);
        var readService = new ConversationProjectionReadService(access, readStore);
        var cursorCodec = new QueryCursorCodec(
            new EphemeralDataProtectionProvider(),
            "Hexalith.Conversations.SM-C2.Cursor.v1");
        return new ConversationQueryHandler(
            access,
            readStore,
            readService,
            cursorCodec,
            timeProvider: new FixedTimeProvider(Timestamp.AddMinutes(1)),
            hydrationService: new ConversationReadHydrationService(new CurrentHydrationDirectory()));
    }

    private static void SeedReadModels(InMemoryReadModelStore store)
    {
        var summaries = new List<ConversationSummaryProjectionV1>(100);
        var dispatches = new Dictionary<string, object>(StringComparer.Ordinal);
        for (int index = 0; index < 100; index++)
        {
            ConversationId conversationId = new($"conversation-{index:000}");
            string dispatchId = $"dispatch-sm-c2-{index:000}";
            ProjectionFreshnessV1 freshness = Freshness(1);
            ConversationSummaryProjectionV1 summary = new(
                SchemaVersion.Current,
                Tenant,
                conversationId,
                freshness,
                "Open",
                $"SM-C2 conversation {index:000}",
                Business,
                Project,
                Folder,
                [Actor, Participant],
                MessageCount: index == 0 ? 10 : 1,
                FileReferenceCount: index == 0 ? 1 : 0);
            ConversationDetailProjectionV1 detail = index == 0
                ? RichDetail(summary)
                : new ConversationDetailProjectionV1(
                    SchemaVersion.Current,
                    Tenant,
                    conversationId,
                    freshness,
                    "Open",
                    summary.Label,
                    Business,
                    Project,
                    Folder,
                    Participants: [new ConversationParticipantProjectionV1(Actor, ParticipantType.Human, ParticipantRole.Member)],
                    Messages: [new ConversationTimelineMessageProjectionV1(new MessageId($"message-{index:000}"), Actor, "Visible message", Timestamp)]);

            summaries.Add(summary);
            dispatches[conversationId.Value] = new
            {
                DispatchId = dispatchId,
                LastAppliedEventPosition = freshness.LastAppliedEventPosition,
            };
            var models = new { Summary = summary, Detail = detail, DispatchId = dispatchId };
            foreach (string key in ConversationKeys(conversationId))
            {
                store.SeedRaw("statestore", key, models);
            }

            store.SeedRaw("statestore", DispatchLedgerKey(dispatchId), new
            {
                DispatchId = dispatchId,
                RequestFingerprint = $"fingerprint-{index:000}",
                TenantId = Tenant,
                ConversationId = conversationId,
                ProjectionGeneratedAt = freshness.ProjectionGeneratedAt,
                Status = 1,
            });
        }

        var indexModel = new { Summaries = summaries, Dispatches = dispatches };
        foreach (string key in TenantIndexKeys())
        {
            store.SeedRaw("statestore", key, indexModel);
        }
    }

    private static ConversationDetailProjectionV1 RichDetail(ConversationSummaryProjectionV1 summary)
    {
        ConversationTimelineMessageProjectionV1[] messages =
        [
            .. Enumerable.Range(0, 10).Select(index => new ConversationTimelineMessageProjectionV1(
                new MessageId($"message-sm-c2-{index:00}"),
                index % 2 == 0 ? Actor : Participant,
                index == 9 ? "[redacted]" : $"Visible SM-C2 message {index:00}",
                Timestamp.AddSeconds(index))),
        ];
        return new ConversationDetailProjectionV1(
            SchemaVersion.Current,
            Tenant,
            summary.ConversationId,
            summary.Freshness,
            "Open",
            summary.Label,
            Business,
            Project,
            Folder,
            Participants:
            [
                new ConversationParticipantProjectionV1(Actor, ParticipantType.Human, ParticipantRole.Facilitator),
                new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member),
            ],
            Messages: messages,
            FileReferences:
            [
                new ConversationFileReferenceProjectionV1(
                    new FileId("file-sm-c2"),
                    Folder,
                    messages[0].MessageId,
                    Timestamp),
            ],
            TrustPosture: ConversationEvidenceTrustPostureV1.FromFreshness(
                SchemaVersion.Current,
                Tenant,
                summary.ConversationId,
                summary.Freshness),
            EvidenceEntries:
            [
                new ConversationEvidenceEntryV1(
                    $"message:{messages[0].MessageId.Value}",
                    "Message",
                    Actor,
                    Timestamp,
                    ProjectionTrustState.Current,
                    ConversationCitationAvailability.Available,
                    ConversationAuditReadinessState.Ready,
                    ProjectionTrustState.Current,
                    MessageId: messages[0].MessageId,
                    VisibleText: messages[0].Text),
            ]);
    }

    private static ProjectionFreshnessV1 Freshness(long position)
        => new(
            SchemaVersion.Current,
            $"pos:{position:0000000000}",
            position,
            Timestamp,
            Timestamp.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

    private static IEnumerable<string> ConversationKeys(ConversationId conversationId)
    {
        yield return $"projection:conversations:{Tenant.Value}:{conversationId.Value}";
        yield return $"projection:conversations:{EncodeKeySegment(Tenant.Value)}:{EncodeKeySegment(conversationId.Value)}";
    }

    private static IEnumerable<string> TenantIndexKeys()
    {
        yield return $"projection:conversations-index:{Tenant.Value}";
        yield return $"projection:conversations-index:{EncodeKeySegment(Tenant.Value)}";
    }

    private static string DispatchLedgerKey(string dispatchId)
        => $"projection:conversations-dispatch:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(dispatchId)))}";

    private static string EncodeKeySegment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static AppendMessageCommand AppendCommand(string text)
        => new(Metadata("idempotency-append"), Conversation, Message, Actor, text);

    private static CreateConversation CreateCommand()
        => new(
            new CreateConversationCommand(
                Metadata("idempotency-create"),
                Business,
                Project,
                Folder,
                "SM-C2 conversation"),
            Conversation,
            Timestamp,
            "event-sm-c2-create");

    private static ConversationCommandMetadata Metadata(string idempotencyKey)
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-sm-c2",
            "causation-sm-c2",
            idempotencyKey);

    private static double[] Measure<T>(Func<T> operation)
    {
        for (int repetition = 0; repetition < WarmupRepetitions; repetition++)
        {
            for (int operationIndex = 0; operationIndex < OperationsPerSample; operationIndex++)
            {
                GC.KeepAlive(operation());
            }
        }

        var samples = new double[Repetitions];
        for (int repetition = 0; repetition < Repetitions; repetition++)
        {
            long started = Stopwatch.GetTimestamp();
            for (int operationIndex = 0; operationIndex < OperationsPerSample; operationIndex++)
            {
                GC.KeepAlive(operation());
            }

            samples[repetition] = Stopwatch.GetElapsedTime(started).TotalMicroseconds / OperationsPerSample;
        }

        return samples;
    }

    private static async Task<double[]> MeasureAsync<T>(Func<ValueTask<T>> operation)
    {
        for (int repetition = 0; repetition < WarmupRepetitions; repetition++)
        {
            for (int operationIndex = 0; operationIndex < OperationsPerSample; operationIndex++)
            {
                GC.KeepAlive(await operation());
            }
        }

        var samples = new double[Repetitions];
        for (int repetition = 0; repetition < Repetitions; repetition++)
        {
            long started = Stopwatch.GetTimestamp();
            for (int operationIndex = 0; operationIndex < OperationsPerSample; operationIndex++)
            {
                GC.KeepAlive(await operation());
            }

            samples[repetition] = Stopwatch.GetElapsedTime(started).TotalMicroseconds / OperationsPerSample;
        }

        return samples;
    }

    private static double Percentile95(IEnumerable<double> samples)
    {
        double[] ordered = [.. samples.Order()];
        int index = (int)Math.Ceiling(0.95 * ordered.Length) - 1;
        return ordered[Math.Max(index, 0)];
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class AllowTenantAccessService : IConversationTenantAccessService
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
            => ValueTask.FromResult(ConversationTenantAccessDecision.Allowed(
                requirement,
                trustedTenantId ?? Tenant,
                callerPrincipalId ?? Caller));
    }

    private sealed class CurrentHydrationDirectory : IConversationReferenceHydrationDirectory
    {
        public ValueTask<IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>> HydratePartiesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<PartyId> partyIds,
            CancellationToken cancellationToken = default)
            => Current(partyIds, "party");

        public ValueTask<IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>> HydrateProjectsAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<ProjectId> projectIds,
            CancellationToken cancellationToken = default)
            => Current(projectIds, "project");

        public ValueTask<IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>> HydrateFoldersAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FolderId> folderIds,
            CancellationToken cancellationToken = default)
            => Current(folderIds, "folder");

        public ValueTask<IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>> HydrateFilesAsync(
            ConversationHydrationContext context,
            IReadOnlyCollection<FileId> fileIds,
            CancellationToken cancellationToken = default)
            => Current(fileIds, "file");

        private static ValueTask<IReadOnlyDictionary<T, ReferenceHydrationResult<T>>> Current<T>(
            IReadOnlyCollection<T> references,
            string token)
            where T : class
            => ValueTask.FromResult((IReadOnlyDictionary<T, ReferenceHydrationResult<T>>)references.ToDictionary(
                reference => reference,
                reference => new ReferenceHydrationResult<T>(
                    reference,
                    ReferenceHydrationStatus.Current,
                    $"SM-C2 {token}",
                    token,
                    "Current")));
    }
}
