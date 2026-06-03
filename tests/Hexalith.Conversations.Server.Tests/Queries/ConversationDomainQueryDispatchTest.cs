// <copyright file="ConversationDomainQueryDispatchTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

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
        EmptyProjectionReadStore store = new();
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
        EmptyProjectionReadStore store = new();
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
        EmptyProjectionReadStore store = new();
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

    private static ServiceProvider BuildProvider() => BuildProvider(new EmptyProjectionReadStore());

    private static ServiceProvider BuildProvider(EmptyProjectionReadStore store)
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

    private sealed class EmptyProjectionReadStore : IConversationProjectionReadStore
    {
        public int ListReads { get; private set; }

        public int DetailReads { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            DetailReads++;
            return ValueTask.FromResult<ConversationProjectedReadModels?>(null);
        }

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            ListReads++;
            return ValueTask.FromResult((IReadOnlyList<ConversationSummaryProjectionV1>)[]);
        }
    }
}
