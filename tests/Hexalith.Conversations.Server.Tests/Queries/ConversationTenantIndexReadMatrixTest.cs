// <copyright file="ConversationTenantIndexReadMatrixTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Claims;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Story 16.2 / AD-7: the list query maps every tenant-index lifecycle state truthfully, and no query path (list,
/// detail, advisory) writes in any state.
/// </summary>
public sealed class ConversationTenantIndexReadMatrixTest
{
    private static readonly TenantId Tenant = ResolutionMetadataFixture.Tenant;
    private static readonly ConversationId Conversation = ResolutionMetadataFixture.Conversation;
    private static readonly PartyId Actor = new("party-matrix");
    private static readonly DateTimeOffset Started = new(2026, 10, 7, 9, 0, 0, TimeSpan.Zero);
    private static readonly string StoreName = ConversationProjectionReadModelKeys.StateStoreName;
    private static readonly string IndexKey = ConversationProjectionReadModelKeys.TenantIndexKey(Tenant);
    private static readonly IDataProtectionProvider DataProtection = new EphemeralDataProtectionProvider();

    /// <summary>The states every query path is exercised in.</summary>
    public static TheoryData<string> States => new()
    {
        "ready-empty",
        "ready-one",
        "v1",
        "rebuilding",
        "gap",
        "missing",
        "duplicate",
        "contradictory",
        "provider-failure",
    };

    /// <summary>Each matrix row maps to its public list state, reason and safe next action.</summary>
    /// <param name="state">The persisted index state.</param>
    /// <param name="freshness">The expected public freshness state.</param>
    /// <param name="reason">The expected public reason code.</param>
    /// <param name="rows">The expected page size.</param>
    /// <param name="nextAction">The exact safe next action; a non-ready page never claims to be complete.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("ready-empty", "Current", "current", 0, "No accessible matches.")]
    [InlineData("ready-one", "Current", "current", 1, "Accessible results are complete for the supplied filters.")]
    [InlineData("v1", "Rebuilding", "rebuilding", 1, "Retry after the read model finishes rebuilding.")]
    [InlineData("rebuilding", "Rebuilding", "rebuilding", 1, "Retry after the read model finishes rebuilding.")]
    [InlineData("gap", "Rebuilding", "gap_detected", 1, "Retry after the read model finishes rebuilding.")]
    [InlineData("missing", "Unavailable", "unavailable", 0, "Retry after the read model is available.")]
    [InlineData("provider-failure", "Unavailable", "unavailable", 0, "Retry after the read model is available.")]
    [InlineData("duplicate", "Unavailable", "metadata_contradictory", 0, "Retry after the read model is available.")]
    [InlineData("contradictory", "Unavailable", "metadata_contradictory", 0, "Retry after the read model is available.")]
    public async Task ListShouldMapEveryLifecycleState(string state, string freshness, string reason, int rows, string nextAction)
    {
        WriteLedgerStore store = await SeedAsync(state);

        ConversationListResult result = await Handler(store, new ResolutionMetadataAccess()).ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "actor", "correlation-matrix"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Parse(freshness));
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Parse(reason));
        result.Conversations.Count.ShouldBe(rows);
        result.SafeNextAction.ShouldBe(nextAction);
        store.Writes.ShouldBeEmpty();
    }

    /// <summary>An unauthorized or nonexistent tenant is forbidden before any index read (unchanged).</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task UnauthorizedTenantShouldBeForbiddenWithoutReadingTheIndex()
    {
        WriteLedgerStore store = await SeedAsync("ready-one");

        ConversationListResult result = await Handler(store, new ResolutionMetadataAccess { Denied = true }).ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "actor", "correlation-matrix"),
            TestContext.Current.CancellationToken);

        result.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        result.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Forbidden);
        store.ReadKeys.ShouldNotContain(IndexKey);
        store.Writes.ShouldBeEmpty();
    }

    /// <summary>AC 2: a write ledger around the list, detail and advisory queries records zero writes in every state.</summary>
    /// <param name="state">The persisted index state.</param>
    /// <returns>A task.</returns>
    [Theory]
    [MemberData(nameof(States))]
    public async Task QueryPathsShouldNeverWriteInAnyState(string state)
    {
        WriteLedgerStore store = await SeedAsync(state);
        ResolutionMetadataAccess access = new();
        ConversationQueryHandler handler = Handler(store, access);

        _ = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "actor", "correlation-matrix"),
            TestContext.Current.CancellationToken);
        _ = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "actor", "correlation-matrix", Conversation),
            TestContext.Current.CancellationToken);
        _ = await DispatchAdvisoryAsync(store, access);

        store.Reads.ShouldBeGreaterThan(0);
        store.Writes.ShouldBeEmpty();
    }

    /// <summary>AC 2, forbidden row: a denied caller is refused on list, detail and advisory with no index read and no write.</summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ForbiddenQueryPathsShouldNeverReadTheIndexOrWrite()
    {
        WriteLedgerStore store = await SeedAsync("ready-one");
        ResolutionMetadataAccess access = new() { Denied = true };
        ConversationQueryHandler handler = Handler(store, access);

        ConversationListResult list = await handler.ListAsync(
            new ListConversationsQuery(SchemaVersion.Current, Tenant, "actor", "correlation-matrix"),
            TestContext.Current.CancellationToken);
        ConversationDetailResult detail = await handler.GetAsync(
            new GetConversationQuery(SchemaVersion.Current, Tenant, "actor", "correlation-matrix", Conversation),
            TestContext.Current.CancellationToken);
        QueryResult advisory = await DispatchAdvisoryAsync(store, access);

        list.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        detail.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        advisory.Success.ShouldBeFalse();
        store.ReadKeys.ShouldNotContain(IndexKey);
        store.Writes.ShouldBeEmpty();
    }

    private static async Task<WriteLedgerStore> SeedAsync(string state)
    {
        InMemoryReadModelStore inner = new();
        ConversationProjectionReadModelWriter writer = new(inner);
        ConversationProjectedReadModels models = Models("dispatch-matrix");
        Dictionary<string, ConversationProjectionDispatchReference> dispatch = new(StringComparer.Ordinal)
        {
            [Conversation.Value] = new(models.DispatchId, 1),
        };
        switch (state)
        {
            case "ready-empty":
                _ = await writer.InitializeTenantIndexAsync(Tenant, TestContext.Current.CancellationToken);
                break;
            case "ready-one":
            case "provider-failure":
                _ = await writer.InitializeTenantIndexAsync(Tenant, TestContext.Current.CancellationToken);
                await writer.PersistAsync(models, TestContext.Current.CancellationToken);
                break;
            case "v1":
                inner.SeedRaw(StoreName, ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation), models);
                inner.SeedRaw(StoreName, IndexKey, new ConversationProjectionIndexReadModel { Summaries = [models.Summary], Dispatches = dispatch });
                break;
            case "rebuilding":
                await writer.PersistAsync(models, TestContext.Current.CancellationToken);
                break;
            case "gap":
                inner.SeedRaw(StoreName, ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation), models);
                inner.SeedRaw(StoreName, IndexKey, V2(ConversationProjectionIndexLifecycle.Ready, [models.Summary], dispatch, watermark: 3));
                break;
            case "missing":
                break;
            case "duplicate":
                inner.SeedRaw(StoreName, IndexKey, V2(ConversationProjectionIndexLifecycle.Ready, [models.Summary, models.Summary], dispatch, watermark: 1));
                break;
            case "contradictory":
                inner.SeedRaw(StoreName, IndexKey, V2((ConversationProjectionIndexLifecycle)9, [models.Summary], dispatch, watermark: 1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }

        return new WriteLedgerStore(inner) { FailReads = state == "provider-failure" };
    }

    private static ConversationProjectionIndexReadModel V2(
        ConversationProjectionIndexLifecycle lifecycle,
        IReadOnlyList<ConversationSummaryProjectionV1> summaries,
        IReadOnlyDictionary<string, ConversationProjectionDispatchReference> dispatches,
        long watermark)
        => new()
        {
            SchemaVersion = 2,
            Lifecycle = lifecycle,
            Watermarks = new Dictionary<string, long>(StringComparer.Ordinal) { [Conversation.Value] = watermark },
            Summaries = summaries,
            Dispatches = dispatches,
        };

    private static ConversationProjectedReadModels Models(string dispatchId)
        => new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [new ConversationProjectionEventRecord(1, new ConversationCreated(
                new ConversationEventMetadata(
                    SchemaVersion.Current,
                    "event-created-matrix",
                    ConversationEventType.ConversationCreated,
                    Tenant,
                    Conversation,
                    "correlation-matrix",
                    Started,
                    Actor),
                Label: "Matrix conversation"))],
            Started.AddSeconds(5),
            TimeSpan.FromDays(3650)) with
        {
            DispatchId = dispatchId,
        };

    private static ConversationQueryHandler Handler(WriteLedgerStore store, IConversationTenantAccessService access)
    {
        ConversationProjectionReadStore readStore = new(store);
        return new ConversationQueryHandler(
            access,
            readStore,
            new ConversationProjectionReadService(access, readStore),
            new QueryCursorCodec(DataProtection, ConversationQueryServiceCollectionExtensions.CursorCodecPurpose),
            Options.Create(new ConversationQueryCursorOptions()));
    }

    private static async Task<QueryResult> DispatchAdvisoryAsync(WriteLedgerStore store, IConversationTenantAccessService access)
    {
        QueryEnvelope query = ResolutionMetadataFixture.Query() with { QueryType = GetConversationAdvisoryMetadataQuery.QueryType };
        IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        List<Claim> claims =
        [
            new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
            .. QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)),
        ];
        accessor.HttpContext!.User = new(new ClaimsIdentity(claims, "Workload"));
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(accessor)
            .AddSingleton<IDomainQueryHandler>(new GetConversationAdvisoryMetadataDomainQueryHandler(access, accessor, store))
            .BuildServiceProvider();
        try
        {
            return await DomainQueryDispatcher.ExecuteAsync(provider, query, TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException) when (store.FailReads)
        {
            // A provider outage may surface from the advisory path; it still must not write.
            return QueryResult.Failure("provider-failure");
        }
    }

    /// <summary>Records every read and every write attempted through any SDK store seam.</summary>
    private sealed class WriteLedgerStore(InMemoryReadModelStore inner)
        : IReadModelStore, IReadModelBulkStore, IReadModelExpiringStore, IReadModelBatchStore, IReadModelConditionalEraser
    {
        public bool FailReads { get; init; }

        public int Reads { get; private set; }

        public List<string> ReadKeys { get; } = [];

        public List<string> Writes { get; } = [];

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
        {
            Read(key);
            return inner.GetAsync<TValue>(storeName, key, cancellationToken);
        }

        public Task<ReadModelEntry<TValue>> GetCompactedAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
            where TValue : class
        {
            Read(key);
            return inner.GetCompactedAsync<TValue>(storeName, key, cancellationToken);
        }

        public Task<IReadOnlyList<ReadModelBulkEntry<TValue>>> GetManyAsync<TValue>(
            string storeName, IReadOnlyList<string> keys, int parallelism, CancellationToken cancellationToken = default)
            where TValue : class
        {
            foreach (string key in keys)
            {
                Read(key);
            }

            return inner.GetManyAsync<TValue>(storeName, keys, parallelism, cancellationToken);
        }

        public Task<(bool Present, string Etag)> TryReadEtagAsync(string storeName, string key, CancellationToken cancellationToken = default)
        {
            Read(key);
            return inner.TryReadEtagAsync(storeName, key, cancellationToken);
        }

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
        {
            Writes.Add($"save:{key}");
            return inner.SaveAsync(storeName, key, value, cancellationToken);
        }

        public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
            where TValue : class
        {
            Writes.Add($"try-save:{key}");
            return inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
        }

        public Task<bool> TrySaveWithTimeToLiveAsync<TValue>(
            string storeName, string key, TValue value, string etag, TimeSpan timeToLive, CancellationToken cancellationToken = default)
            where TValue : class
        {
            Writes.Add($"try-save-ttl:{key}");
            return inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
        }

        public Task<ReadModelBatchResult> ExecuteAsync(ReadModelBatch batch, CancellationToken cancellationToken = default)
        {
            Writes.Add("batch");
            return inner.ExecuteAsync(batch, cancellationToken);
        }

        public Task<bool> TryEraseAsync(string storeName, string key, string etag, CancellationToken cancellationToken = default)
        {
            Writes.Add($"erase:{key}");
            return inner.TryEraseAsync(storeName, key, etag, cancellationToken);
        }

        private void Read(string key)
        {
            Reads++;
            ReadKeys.Add(key);
            if (FailReads)
            {
                throw new InvalidOperationException("Synthetic read-model provider outage.");
            }
        }
    }
}
