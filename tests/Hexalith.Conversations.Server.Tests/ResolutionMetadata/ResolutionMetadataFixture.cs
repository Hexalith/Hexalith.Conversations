using System.Security.Claims;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionMetadataFixture
{
    public static readonly TenantId Tenant = new("tenant-resolution");
    public static readonly ConversationId Conversation = new("conversation-resolution");
    public static readonly DateTimeOffset Started = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    public ResolutionTestTimeProvider Time { get; } = new(Started.AddSeconds(10));
    public InMemoryReadModelStore Store { get; } = new();
    public ResolutionMetadataAccess Access { get; } = new();
    public ResolutionNativeEvidence Native { get; }
    public ResolutionGuardedStore Guard { get; }
    public ResolutionMetadataFixture() { Guard = new(Store); Native = new(this); }
    public ConversationAsyncProjectionHandler Handler(IReadModelStore? store = null)
        => new(new(), new(store ?? Store), store ?? Store, Time);
    public ConversationProjectionReadStore Reader(IReadModelStore? store = null) => new(store ?? Store);
    public static ProjectionRequest Request(string? label = "Scoring label", bool changedProject = false, TenantId? tenantId = null, ConversationId? conversationId = null, bool classifyLabel = true)
    {
        tenantId ??= Tenant;
        conversationId ??= Conversation;
        ConversationEventMetadata metadata = new(SchemaVersion.Current, "event-create", ConversationEventType.ConversationCreated,
            tenantId, conversationId, "correlation", Started, new PartyId("actor"), "cause");
        ConversationCreated created = new(metadata, ProjectId: new("project-original"), Label: label)
        {
            ResolutionScoringLabel = classifyLabel && ConversationResolutionScoringPolicy.CanClassify(label, ConversationResolutionScoringPolicy.Purpose)
                ? new(label, ConversationResolutionScoringLabel.SupportedPurpose, ConversationResolutionScoringLabel.OwnerClassification) : null,
        };
        List<ProjectionEventDto> events = [new(nameof(ConversationCreated), JsonSerializer.SerializeToUtf8Bytes(created, JsonSerializerOptions.Web), "json", 1, Started, "correlation", "source-message-1")];
        if (changedProject)
        {
            ConversationProjectChanged changed = new(metadata with { EventType = ConversationEventType.ConversationProjectChanged,
                OccurredAt = Started.AddSeconds(1), EventId = "event-change" }, new("project-original"), new("project-new"));
            events.Add(new(nameof(ConversationProjectChanged), JsonSerializer.SerializeToUtf8Bytes(changed, JsonSerializerOptions.Web), "json", 2, Started.AddSeconds(1), "correlation", "source-message-2"));
        }

        ProjectionRequest request = new(tenantId.Value, ConversationProjectionHandler.ConversationDomain, conversationId.Value, [.. events]);
        try { return AdmitNativeAsync(request).GetAwaiter().GetResult(); }
        catch (ArgumentException) { return request; } // opaque legacy-only fixture identity
    }

    internal static async Task<ProjectionRequest> AdmitNativeAsync(ProjectionRequest request,
        string incarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAV")
    {
        using var gateway = new ResolutionProjectionGateway(request, incarnation);
        var capture = new ResolutionProjectionCapture();
        using ServiceProvider services = new ServiceCollection().AddSingleton<IAsyncDomainProjectionHandler>(capture)
            .AddSingleton<IEventStoreGatewayClient>(gateway.Client).BuildServiceProvider();
        var catalog = new DomainProjectionCatalogRegistry();
        const string fingerprint = "fixture-native-provenance";
        catalog.Register(fingerprint, [new(capture.Domain, capture.ProjectionType)]);
        await DomainProjectionDispatcher.DispatchAsync(services,
            new(request with { NativeSourceProof = gateway.Proof }, [capture.ProjectionType], "fixture-admission", fingerprint),
            new(), catalog, TestContext.Current.CancellationToken);
        return capture.Request!;
    }

    public async Task ProjectAsync(string dispatch = "dispatch-a", string? label = "Scoring label", bool changedProject = false)
    {
        ProjectionRequest request = Request(label, changedProject);
        (await Handler().ProjectAsync(request, dispatch, TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
    }

    public static QueryEnvelope Query() => new(Tenant.Value, "conversations", Conversation.Value,
        GetConversationResolutionMetadataQuery.QueryType, "{}"u8.ToArray(), "correlation", "actor")
    {
        OriginalActorId = "actor", AuthenticatedWorkloadId = "projects", IsDelegated = true,
        EntityId = Conversation.Value,
        CallerAuthority = new("delegated-owner-query", [], [])
        {
            OriginatingContext = new("actor", "original-client", false, null, ["projects.read"], ["hexalith-eventstore"]),
        },
    };

    public async Task<QueryResult> DispatchAsync(QueryEnvelope? query = null, bool verified = true,
        IConversationTenantAccessService? access = null, IConversationTenantProjectionSignal? signal = null, bool nativeAuthorization = true,
        CancellationToken? cancellationToken = null)
    {
        query ??= Query();
        Guard.ForbidLegacyReads = true;
        Guard.ForbidWrites = true;
        IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        List<Claim> claims =
        [
            new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
        ];
        if (verified) claims.AddRange(QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)));
        accessor.HttpContext!.User = new(new ClaimsIdentity(claims, "Workload"));
        var handler = new GetConversationResolutionMetadataDomainQueryHandler(
            new(access ?? Access, accessor, signal ?? Access, Time, nativeAuthorization ? new ResolutionPolicyNativeEvidence(this, access ?? Access, signal ?? Access) : null), Reader(Guard), Native);
        await using ServiceProvider provider = new ServiceCollection().AddSingleton(accessor)
            .AddSingleton<IDomainQueryHandler>(handler).BuildServiceProvider();
        try
        {
            return await DomainQueryDispatcher.ExecuteAsync(provider, query, cancellationToken ?? TestContext.Current.CancellationToken);
        }
        finally
        {
            Guard.ForbiddenAttempts.ShouldBe(0);
            Guard.ReadKeys.ShouldAllBe(key => key.StartsWith("projection:conversations-resolution:", StringComparison.Ordinal)
                || key.StartsWith("projection:conversations-resolution-proof:", StringComparison.Ordinal)
                || key.StartsWith("projection:conversations-dispatch:", StringComparison.Ordinal));
        }
    }

    public async Task<QueryResult> DispatchAdvisoryAsync(QueryEnvelope? query = null, bool verified = true,
        CancellationToken? cancellationToken = null, IConversationTenantAccessService? access = null)
    {
        query ??= Query() with { QueryType = GetConversationAdvisoryMetadataQuery.QueryType };
        Guard.AllowSummaryIndexReads = true;
        Guard.ForbidLegacyReads = true;
        Guard.ForbidWrites = true;
        IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        List<Claim> claims =
        [
            new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
        ];
        if (verified) claims.AddRange(QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)));
        accessor.HttpContext!.User = new(new ClaimsIdentity(claims, "Workload"));
        await using ServiceProvider provider = new ServiceCollection().AddSingleton(accessor)
            .AddSingleton<IDomainQueryHandler>(new GetConversationAdvisoryMetadataDomainQueryHandler(access ?? Access, accessor, Guard)).BuildServiceProvider();
        return await DomainQueryDispatcher.ExecuteAsync(provider, query, cancellationToken ?? TestContext.Current.CancellationToken);
    }

    public async Task EraseAsync(string key)
    {
        (bool exists, string etag) = await Store.TryReadEtagAsync("statestore", key, TestContext.Current.CancellationToken);
        exists.ShouldBeTrue();
        (await Store.TryEraseAsync("statestore", key, etag, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    public ConversationResolutionMetadataReadModel Metadata => Store.Snapshot<ConversationResolutionMetadataReadModel>("statestore",
        ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation))!;
    public ConversationResolutionMetadataProof Proof => Store.Snapshot<ConversationResolutionMetadataProof>("statestore",
        ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation))!;
}
