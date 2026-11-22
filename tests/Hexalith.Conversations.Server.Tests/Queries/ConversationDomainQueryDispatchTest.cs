// <copyright file="ConversationDomainQueryDispatchTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Tests.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Story 2.3 (AC-3) — teeth test proving the conversation queries are served through the SDK
/// <see cref="DomainQueryDispatcher"/> <c>/query</c> seam, not a bypassed in-process call.
/// </summary>
/// <remarks>
/// The load-bearing facts are the contrast pair (Epic 1 L1/A1 — green alone is not evidence): a matched
/// <c>Domain</c>/<c>QueryType</c> reaches the discovered <see cref="IDomainQueryHandler"/> adapter and returns
/// a real <see cref="QueryResult"/>; an unmatched <c>Domain</c> or <c>QueryType</c> surfaces the dispatcher's
/// "No query handler is registered…" failure rather than a silent success. If the adapter's discriminators
/// regressed, the matched facts would turn RED — catching a dispatch that the route-presence smoke test waves
/// through.
/// </remarks>
public sealed class ConversationDomainQueryDispatchTest
{
    private static readonly TenantId Tenant = new("tenant-001");

    [Fact]
    public async Task ListQueryShouldDispatchThroughSdkSeamToAdapter()
    {
        await using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        QueryEnvelope envelope = new(
            tenantId: Tenant.Value,
            domain: ConversationDomainQueryHandlerBase.ConversationsDomain,
            aggregateId: Tenant.Value,
            queryType: ListConversationsDomainQueryHandler.ConversationListQueryType,
            payload: [],
            correlationId: "correlation-001",
            userId: "caller-001");

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        JsonElement payload = result.GetPayload();
        payload.ValueKind.ShouldBe(JsonValueKind.Object);
        // The conversation list response shape proves the adapter delegated to the real ConversationQueryHandler.
        payload.TryGetProperty("conversations", out JsonElement conversations).ShouldBeTrue();
        conversations.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task DetailQueryShouldDispatchThroughSdkSeamToAdapter()
    {
        await using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        byte[] payload = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { conversationId = "conversation-001" }));
        QueryEnvelope envelope = new(
            tenantId: Tenant.Value,
            domain: ConversationDomainQueryHandlerBase.ConversationsDomain,
            aggregateId: "conversation-001",
            queryType: GetConversationDomainQueryHandler.ConversationDetailQueryType,
            payload: payload,
            correlationId: "correlation-001",
            userId: "caller-001");

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.GetPayload().ValueKind.ShouldBe(JsonValueKind.Object);
    }

    /// <summary>Checks bounded selection and owner admission over the authenticated SDK receiver.</summary>
    /// <param name="scenario">The caller and detail mode used in the request.</param>
    [Theory]
    [InlineData("projects")]
    [InlineData("projects-missing-scope")]
    [InlineData("caller-advisory")]
    [InlineData("caller-duplicate-selector")]
    public async Task BoundedDetailShouldEnforceAdmissionThroughSdkHttp(string scenario)
    {
        const string appId = ResolutionRouteCatalog.AppId;
        const string issuer = "conversations-query-test";
        const string channelToken = "conversations-query-test-channel";
        string signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        bool isAdvisory = scenario == "caller-advisory";
        bool isDelegated = scenario is "projects" or "projects-missing-scope";
        var reference = ResolutionMetadataFixture.Query() with { QueryType = GetConversationQuery.QueryType };
        var envelope = isDelegated
            ? reference with
            {
                CallerAuthority = reference.CallerAuthority! with
                {
                    OriginatingContext = reference.CallerAuthority!.OriginatingContext! with
                    { Scopes = scenario == "projects" ? ["projects.read"] : ["projects.list"] },
                },
            }
            : reference with
            {
                IsDelegated = false,
                AuthenticatedWorkloadId = null,
                CallerAuthority = new("authenticated-caller", [], []),
                Payload = isAdvisory ? """{"advisoryMetadataOnly":true}"""u8.ToArray()
                    : """{"nativeMembershipOnly":true,"nativeMembershipOnly":false}"""u8.ToArray(),
            };
        var tenant = new TenantId(envelope.TenantId);
        var conversation = new ConversationId(envelope.AggregateId);
        var now = DateTimeOffset.UtcNow;
        var freshness = new ProjectionFreshnessV1(SchemaVersion.Current, "pos:0000000001", 1, now, now, TimeSpan.Zero,
            false, ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current);
        var stale = new ProjectionFreshnessV1(SchemaVersion.Current, "pos:0000000002", 2, now.AddMinutes(-5), now, TimeSpan.FromMinutes(5),
            true, ProjectionTrustState.Stale, ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        var fixture = new ResolutionMetadataFixture();
        if (isAdvisory)
        {
            await fixture.ProjectAsync();
            fixture.SeedMetadata(fixture.Metadata with { Freshness = stale });
        }

        var project = new ProjectId("project-member");
        var author = new PartyId("author");
        var store = new RecordingProjectionReadStore
        {
            Models = isAdvisory ? null : new(
                new ConversationSummaryProjectionV1(SchemaVersion.Current, tenant, conversation, freshness, "Open", ProjectId: project),
                new ConversationDetailProjectionV1(SchemaVersion.Current, tenant, conversation, freshness, "Open", "Private label", ProjectId: project,
                    Messages: [new ConversationTimelineMessageProjectionV1(new MessageId("message-001"), author, "Timeline text", now)])),
        };
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DaprAppChannelToken.ConfigurationKey] = channelToken,
            ["EventStore:DomainService:AppId"] = appId,
            ["Authentication:JwtBearer:Issuer"] = issuer,
            ["Authentication:JwtBearer:Audience"] = "hexalith-eventstore",
            ["Authentication:JwtBearer:SigningKey"] = signingKey,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = SecurityAlgorithms.HmacSha256,
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
        });
        builder.AddEventStoreDomainService(typeof(ConversationAggregate).Assembly, typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddConversationQueries(_ => { });
        builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowAllTenantAccessService());
        builder.Services.AddSingleton<IConversationProjectionReadStore>(store);
        builder.Services.AddSingleton<IReadModelStore>(fixture.Store);
        string fingerprint = Convert.ToHexStringLower(SHA256.HashData("conversation-detail-http-test"u8));
        builder.Services.Configure<DomainProjectionIdentityOptions>(options => { options.AppId = appId; options.ServiceVersion = "v1"; });
        builder.Services.Configure<RouteCatalogHostOptions>(options => options.Identity = new(appId, appId, fingerprint, fingerprint, fingerprint));
        builder.Services.AddSingleton<ResolutionRouteCatalog>(services => new(services, tenant.Value,
            () => services.GetRequiredService<EventStoreDomainServiceEndpointSource>().Endpoints!.DataSources.SelectMany(source => source.Endpoints),
            includeDetailQuery: true));
        builder.Services.AddSingleton<IRouteCatalogAuthority>(services => services.GetRequiredService<ResolutionRouteCatalog>());

        var token = TestContext.Current.CancellationToken;
        await using var app = builder.Build();
        app.MapEventStoreDomainService();
        await app.StartAsync(token);

        var claims = QueryCallerAuthorityBinding.Bind(envelope).ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal);
        claims[EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = "eventstore";
        claims[EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = new[] { EventStoreWorkloadOperations.DomainServiceQuery };
        claims[RouteCatalogSurfacePolicy.ClaimType] = "service";
        var assertion = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = appId,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(2).UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/query") { Content = JsonContent.Create(envelope) };
        request.Headers.Add(DaprAppChannelToken.HeaderName, channelToken);
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        var catalog = app.Services.GetRequiredService<ResolutionRouteCatalog>().Snapshot;
        request.Headers.Add(RouteCatalogInvocationHandler.RootHeader, catalog.RootDigest);
        request.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, catalog.Epoch.ToString(CultureInfo.InvariantCulture));
        using var client = app.GetTestClient();
        using var response = await client.SendAsync(request, token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<QueryResult>(token)).ShouldNotBeNull();
        result.Success.ShouldBe(scenario is "projects" or "caller-advisory");
        if (result.Success)
        {
            var bounded = result.GetPayload().Deserialize<ConversationDetailResult>(JsonSerializerOptions.Web).ShouldNotBeNull();
            bounded.ReadPurpose.ShouldBe(isAdvisory ? ConversationResolutionScoringLabel.SupportedPurpose : GetConversationQuery.NativeMembershipPurpose);
            var details = bounded.Details.ShouldNotBeNull();
            details.ProjectId.ShouldBe(isAdvisory ? new ProjectId("project-original") : project);
            details.Label.ShouldBe(isAdvisory ? "Scoring label" : null);
            details.Freshness.ShouldBe(isAdvisory ? stale : freshness);
            details.Messages.ShouldBeEmpty();
            details.Participants.ShouldBeEmpty();
            store.DetailReads.ShouldBe(isAdvisory ? 0 : 1);
        }
        else
        {
            result.ErrorMessage.ShouldBe(isDelegated ? "safe-denial" : "The conversation query could not be served.");
            result.PayloadBytes.ShouldBeNull();
            store.DetailReads.ShouldBe(0);
        }
    }

    [Fact]
    public async Task UnmatchedQueryTypeShouldSurfaceDispatcherFailure()
    {
        await using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        QueryEnvelope envelope = new(
            tenantId: Tenant.Value,
            domain: ConversationDomainQueryHandlerBase.ConversationsDomain,
            aggregateId: Tenant.Value,
            queryType: "conversation-does-not-exist",
            payload: [],
            correlationId: "correlation-001",
            userId: "caller-001");

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldContain("No query handler is registered");
    }

    [Fact]
    public async Task UnmatchedDomainShouldSurfaceDispatcherFailure()
    {
        await using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        QueryEnvelope envelope = new(
            tenantId: Tenant.Value,
            domain: "not-conversations",
            aggregateId: Tenant.Value,
            queryType: ListConversationsDomainQueryHandler.ConversationListQueryType,
            payload: [],
            correlationId: "correlation-001",
            userId: "caller-001");

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldContain("No query handler is registered");
    }

    /// <summary>
    /// Story 2.3 (AC-3) — a fault inside the adapter (here: an undeserializable payload) is contained as a
    /// coarse <see cref="QueryResult.Failure"/> and never leaks an exception past the SDK seam. The failure is
    /// the adapter-edge text, not the dispatcher's "No query handler" miss — proving dispatch reached the
    /// adapter and the adapter swallowed the fault rather than the dispatcher absorbing a throw.
    /// </summary>
    [Fact]
    public async Task QueryFaultShouldBeContainedAsCoarseFailureNotExceptionLeak()
    {
        await using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        // Non-empty but malformed JSON bytes force the adapter's payload deserialization to throw.
        QueryEnvelope envelope = new(
            tenantId: Tenant.Value,
            domain: ConversationDomainQueryHandlerBase.ConversationsDomain,
            aggregateId: Tenant.Value,
            queryType: ListConversationsDomainQueryHandler.ConversationListQueryType,
            payload: Encoding.UTF8.GetBytes("{ this is not valid json"),
            correlationId: "correlation-001",
            userId: "caller-001");

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldNotContain("No query handler is registered");
        // The coarse adapter-edge text never carries the raw exception detail.
        result.ErrorMessage.ShouldNotContain("Exception", Case.Insensitive);
    }

    /// <summary>
    /// Story 2.3 (AC-3) — when the detail payload omits the conversation id, the adapter resolves it from the
    /// envelope aggregate identity so an aggregate-routed gateway need not duplicate the id in the body. A
    /// resolved id reaches the real handler and returns a serialized result object (Success), distinguishing it
    /// from the unresolved fail-closed path.
    /// </summary>
    [Fact]
    public async Task DetailQueryShouldResolveConversationIdFromAggregateIdWhenPayloadOmitsIt()
    {
        RecordingProjectionReadStore store = new();
        await using ServiceProvider provider = BuildProvider(store);
        using IServiceScope scope = provider.CreateScope();

        QueryEnvelope envelope = new(
            tenantId: Tenant.Value,
            domain: ConversationDomainQueryHandlerBase.ConversationsDomain,
            aggregateId: "conversation-from-aggregate",
            queryType: GetConversationDomainQueryHandler.ConversationDetailQueryType,
            payload: [],
            correlationId: "correlation-001",
            userId: "caller-001");

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.GetPayload().ValueKind.ShouldBe(JsonValueKind.Object);

        // Reaching the handler with a resolved id means the detail projection read was actually attempted.
        store.DetailReads.ShouldBe(1);
    }

    /// <summary>
    /// Story 2.3 (AC-3) — defense in depth: when no conversation id is resolvable from the payload or the
    /// envelope, the detail adapter fails closed with a coarse failure and never reads any projection row.
    /// </summary>
    [Fact]
    public async Task DetailQueryWithNoResolvableConversationIdShouldFailClosed()
    {
        RecordingProjectionReadStore store = new();
        await using ServiceProvider provider = BuildProvider(store);
        using IServiceScope scope = provider.CreateScope();

        // A well-formed envelope cannot carry a blank aggregate id, so mutate the immutable record to the
        // degenerate state the adapter must still defend against.
        QueryEnvelope envelope = new QueryEnvelope(
            tenantId: Tenant.Value,
            domain: ConversationDomainQueryHandlerBase.ConversationsDomain,
            aggregateId: "placeholder",
            queryType: GetConversationDomainQueryHandler.ConversationDetailQueryType,
            payload: [],
            correlationId: "correlation-001",
            userId: "caller-001")
        {
            AggregateId = " ",
            EntityId = null,
        };

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldNotContain("No query handler is registered");
        store.DetailReads.ShouldBe(0);
    }

    /// <summary>
    /// Story 2.3 (AC-3 / fail-closed) — the adapter rejects an envelope without an authenticated user before
    /// any state access. The envelope contract enforces a non-empty user id at construction, so this proves the
    /// adapter's own defense-in-depth gate: a blank user id fails closed and reads zero projection rows.
    /// </summary>
    [Fact]
    public async Task MissingAuthenticatedUserShouldFailClosedBeforeProjectionRead()
    {
        RecordingProjectionReadStore store = new();
        await using ServiceProvider provider = BuildProvider(store);
        using IServiceScope scope = provider.CreateScope();

        QueryEnvelope envelope = new QueryEnvelope(
            tenantId: Tenant.Value,
            domain: ConversationDomainQueryHandlerBase.ConversationsDomain,
            aggregateId: Tenant.Value,
            queryType: ListConversationsDomainQueryHandler.ConversationListQueryType,
            payload: [],
            correlationId: "correlation-001",
            userId: "placeholder")
        {
            UserId = " ",
        };

        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(
            scope.ServiceProvider, envelope, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldNotContain("No query handler is registered");
        store.ListReads.ShouldBe(0);
    }

    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\"page\":{\"pageSize\":10}}", true)]
    [InlineData("{\"page\":{\"pageSize\":\"10\"}}", false)]
    public async Task OrdinaryPayloadOptionalityAndStrictNumbersMatchCatalog(string json, bool accepted)
    {
        await using ServiceProvider provider = BuildProvider();
        using var scope = provider.CreateScope();
        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider,
            new QueryEnvelope(Tenant.Value, "conversations", "conversation-001", "conversation-list", Encoding.UTF8.GetBytes(json), "correlation", "actor"),
            TestContext.Current.CancellationToken);
        result.Success.ShouldBe(accepted);
        if (json == "{}")
        {
            result = await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider,
                new QueryEnvelope(Tenant.Value, "conversations", "conversation-001", "conversation-detail", Encoding.UTF8.GetBytes(json), "correlation", "actor"),
                TestContext.Current.CancellationToken);
            result.Success.ShouldBeTrue();
        }
    }

    private static ServiceProvider BuildProvider() => BuildProvider(new RecordingProjectionReadStore());

    private static ServiceProvider BuildProvider(RecordingProjectionReadStore store)
    {
        ServiceCollection services = new();
        services.AddSingleton<IConversationTenantAccessService>(new AllowAllTenantAccessService());
        services.AddSingleton<IConversationProjectionReadStore>(store);
        services.AddDataProtection();
        services.AddConversationQueries(options => options.MaxOffset = 100_000);

        // The exact registration the SDK assembly scan performs for a discovered IDomainQueryHandler.
        services.AddScoped<IDomainQueryHandler, ListConversationsDomainQueryHandler>();
        services.AddScoped<IDomainQueryHandler, GetConversationDomainQueryHandler>();

        return services.BuildServiceProvider();
    }

    private sealed class AllowAllTenantAccessService : IConversationTenantAccessService
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
                callerPrincipalId ?? "caller-001"));
    }

    private sealed class RecordingProjectionReadStore : IConversationProjectionReadStore
    {
        public ConversationProjectedReadModels? Models { get; init; }

        public int ListReads { get; private set; }

        public int DetailReads { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            DetailReads++;
            return ValueTask.FromResult(Models);
        }

        public ValueTask<IReadOnlySet<string>> ValidatePageAsync(
            TenantId tenantId,
            ConversationProjectionIndexSnapshot snapshot,
            IReadOnlyList<ConversationSummaryProjectionV1> page,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ProjectionIndexSnapshotTestExtensions.NoInconsistentRows());

        public ValueTask<ConversationProjectionIndexSnapshot> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            ListReads++;
            return ValueTask.FromResult(ConversationProjectionIndexSnapshot.Empty);
        }
    }
}
