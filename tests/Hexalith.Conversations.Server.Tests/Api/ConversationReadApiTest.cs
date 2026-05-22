// <copyright file="ConversationReadApiTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Api;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.Api;

public sealed class ConversationReadApiTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly BusinessReference Business = new("crm", "case-123");
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReadRoutesShouldRequireAuthorization()
    {
        using WebApplication app = BuildApp(AllowedAccess(), new FakeProjectionReadStore());

        RouteEndpoint detail = FindEndpoint(app, "/api/v1/conversations/{conversationId}");
        RouteEndpoint list = FindEndpoint(app, "/api/v1/conversations/");

        detail.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        list.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        detail.RoutePattern.RawText.ShouldBe("/api/v1/conversations/{conversationId}");
        list.RoutePattern.RawText.ShouldBe("/api/v1/conversations/");
    }

    [Fact]
    public async Task DetailRequestMissingTenantClaimShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new();
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = "conversation-001" },
            user: AuthenticatedUserWithoutTenant());

        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
        access.Calls.ShouldBe(0);
        store.DetailReads.ShouldBe(0);
    }

    [Fact]
    public async Task DetailRequestHandlerFailureShouldReturnUnavailableShape()
    {
        using WebApplication app = BuildApp(new ThrowingTenantAccessService(), new FakeProjectionReadStore());

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/{conversationId}",
            routeValues: new Dictionary<string, object?> { ["conversationId"] = "conversation-001" },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        response.Body.ShouldContain("\"freshnessState\":\"Unavailable\"");
        response.Body.ShouldNotContain("tenant-001", Case.Insensitive);
        response.Body.ShouldNotContain("conversation-001", Case.Insensitive);
    }

    [Fact]
    public async Task ListRequestWithIncompleteBusinessFilterShouldReturnHiddenShapeWithoutProjectionRead()
    {
        FakeTenantAccessService access = AllowedAccess();
        FakeProjectionReadStore store = new()
        {
            Summaries = [Summary(Tenant, new ConversationId("conversation-match"), Business, Project, Folder, Participant)],
        };
        using WebApplication app = BuildApp(access, store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/",
            queryString: "?businessSystem=crm",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        response.Body.ShouldContain("\"freshnessState\":\"Forbidden\"");
        response.Body.ShouldContain("\"conversations\":[]");
        access.Calls.ShouldBe(0);
        store.ListReads.ShouldBe(0);
    }

    [Fact]
    public async Task ListRequestShouldBindFilterAndPageParameters()
    {
        FakeProjectionReadStore store = new()
        {
            Summaries =
            [
                Summary(Tenant, new ConversationId("conversation-match"), Business, Project, Folder, Participant),
                Summary(Tenant, new ConversationId("conversation-business-miss"), new BusinessReference("crm", "case-999"), Project, Folder, Participant),
                Summary(OtherTenant, new ConversationId("conversation-cross-tenant"), Business, Project, Folder, Participant),
            ],
        };
        using WebApplication app = BuildApp(AllowedAccess(), store);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/",
            queryString: "?businessSystem=crm&businessValue=case-123&projectId=project-001&folderId=folder-001&lifecycleState=Open&participantPartyId=party-participant&pageSize=1",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        JsonElement conversations = document.RootElement.GetProperty("conversations");
        conversations.GetArrayLength().ShouldBe(1);
        conversations[0].GetProperty("conversationId").GetString().ShouldBe("conv:conversation-match");
        document.RootElement.GetProperty("page").GetProperty("returnedCount").GetInt32().ShouldBe(1);
        response.Body.ShouldNotContain("conversation-business-miss", Case.Insensitive);
        response.Body.ShouldNotContain("conversation-cross-tenant", Case.Insensitive);
    }

    private static WebApplication BuildApp(IConversationTenantAccessService access, IConversationProjectionReadStore store)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(access);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(new ConversationProjectionReadService(access, store));
        builder.Services.AddSingleton(CreateCursor());
        builder.Services.AddSingleton<ConversationQueryHandler>();

        WebApplication app = builder.Build();
        app.MapConversationReadApi();
        return app;
    }

    private static async Task<ApiResponse> InvokeAsync(
        WebApplication app,
        string routePattern,
        IReadOnlyDictionary<string, object?>? routeValues = null,
        string? queryString = null,
        ClaimsPrincipal? user = null)
    {
        RouteEndpoint endpoint = FindEndpoint(app, routePattern);
        DefaultHttpContext context = new()
        {
            RequestServices = app.Services,
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString(queryString ?? string.Empty);
        context.Response.Body = new MemoryStream();

        if (routeValues is not null)
        {
            foreach (KeyValuePair<string, object?> routeValue in routeValues)
            {
                context.Request.RouteValues[routeValue.Key] = routeValue.Value;
            }
        }

        await endpoint.RequestDelegate!(context).ConfigureAwait(false);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8);
        string body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return new ApiResponse(context.Response.StatusCode, body);
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string routePattern)
        => ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText, routePattern, StringComparison.Ordinal));

    private static ClaimsPrincipal AuthenticatedUser() => new(new ClaimsIdentity(
        [new Claim(ConversationReadApi.TenantIdClaimType, Tenant.Value), new Claim(ClaimTypes.NameIdentifier, "caller-001")],
        authenticationType: "Test"));

    private static ClaimsPrincipal AuthenticatedUserWithoutTenant() => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "caller-001")],
        authenticationType: "Test"));

    private static FakeTenantAccessService AllowedAccess()
        => new(ConversationTenantAccessDecision.Allowed(
            ConversationTenantAccessRequirement.Read,
            Tenant,
            "caller-001"));

    private static ConversationQueryCursor CreateCursor()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return new ConversationQueryCursor(Options.Create(new ConversationQueryCursorOptions { SigningKey = key, KeyId = "api-test-key" }));
    }

    private static ConversationSummaryProjectionV1 Summary(TenantId tenantId, ConversationId conversationId, BusinessReference? business, ProjectId? project, FolderId? folder, PartyId participant)
        => new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            new ProjectionFreshnessV1(
                SchemaVersion.Current,
                "pos:0000000001",
                1,
                Now,
                Now.AddSeconds(1),
                TimeSpan.FromSeconds(1),
                IsStale: false,
                ProjectionTrustState.Current,
                ProjectionFreshnessReasonCode.Current),
            "Open",
            "Case 123",
            business,
            project,
            folder,
            [participant],
            MessageCount: 1,
            FileReferenceCount: 0);

    private sealed record ApiResponse(int StatusCode, string Body);

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public int Calls { get; private set; }

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
        {
            Calls++;
            return ValueTask.FromResult(decision);
        }
    }

    private sealed class ThrowingTenantAccessService : IConversationTenantAccessService
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
            => throw new InvalidOperationException("Synthetic tenant projection outage.");
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public IReadOnlyList<ConversationSummaryProjectionV1> Summaries { get; init; } = [];

        public int DetailReads { get; private set; }

        public int ListReads { get; private set; }

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
            return ValueTask.FromResult(Summaries);
        }
    }
}
