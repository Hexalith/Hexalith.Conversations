// <copyright file="ConversationCommandApiTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Api;

public sealed class ConversationCommandApiTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly MessageId Message = new("message-001");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CommandRoutesShouldRequireAuthorizationAndUseNarrowWriteShape()
    {
        using WebApplication app = BuildApp(new FakeCommandHandler());

        RouteEndpoint create = FindEndpoint(app, "/api/v1/conversations/");
        RouteEndpoint append = FindEndpoint(app, "/api/v1/conversations/{conversationId}/messages");

        create.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        append.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
        create.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldBe([HttpMethods.Post], ignoreOrder: false);
        append.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldBe([HttpMethods.Post], ignoreOrder: false);
    }

    [Fact]
    public async Task CreateShouldBindTrustedTenantAndReturnTypedAcceptedResult()
    {
        FakeCommandHandler handler = new()
        {
            CreateOutcome = ConversationCommandApiOutcome<ConversationCreatedResult>.Success(CreatedResult(), StatusCodes.Status201Created),
        };
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/",
            CreateCommand(),
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status201Created);
        response.Body.ShouldContain("\"commandType\":\"CreateConversationCommand\"");
        handler.CreateCalls.ShouldBe(1);
        handler.LastCreateCommand.ShouldNotBeNull();
        handler.LastCreateCommand!.Metadata.TenantId.ShouldBe(Tenant);
        handler.LastCreateCommand.Metadata.IdempotencyKey.ShouldBe("idem-001");
        response.Body.ShouldNotContain("EventStore", Case.Insensitive);
        response.Body.ShouldNotContain("stream", Case.Insensitive);
    }

    [Fact]
    public async Task AppendShouldRejectRouteAndBodyConversationMismatchBeforeHandler()
    {
        FakeCommandHandler handler = new();
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/{conversationId}/messages",
            AppendCommand(),
            routeValues: new Dictionary<string, object?> { ["conversationId"] = "conversation-other" },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        response.Body.ShouldContain("command_validation_failed");
        response.Body.ShouldContain("correct-request");
        response.Body.ShouldNotContain("conversation-other", Case.Insensitive);
        handler.AppendCalls.ShouldBe(0);
    }

    [Fact]
    public async Task CommandTenantMismatchShouldReturnTypedAuthorizationErrorWithoutHandlerCall()
    {
        FakeCommandHandler handler = new();
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/",
            CreateCommand(Tenant),
            user: AuthenticatedUser(OtherTenant));

        response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        response.Body.ShouldContain("tenant_context_mismatch");
        response.Body.ShouldContain("align-context");
        response.Body.ShouldNotContain("tenant-002", Case.Insensitive);
        handler.CreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MissingMetadataShouldReturnTypedValidationErrorWithoutHandlerCall()
    {
        FakeCommandHandler handler = new();
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/",
            new CreateConversationCommand(null!, Label: "Case 123"),
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        response.Body.ShouldContain("command_validation_failed");
        response.Body.ShouldContain("correct-request");
        response.Body.ShouldNotContain("Case 123", Case.Insensitive);
        handler.CreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MalformedBodyShouldReturnTypedValidationErrorWithoutRawBodyLeak()
    {
        FakeCommandHandler handler = new();
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeRawAsync(
            app,
            "/api/v1/conversations/",
            "{ \"metadata\": { \"tenantId\": \"tenant:tenant-999\", ",
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        response.Body.ShouldContain("command_validation_failed");
        response.Body.ShouldContain("correct-request");
        response.Body.ShouldNotContain("tenant-999", Case.Insensitive);
        handler.CreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task UnauthenticatedCallerShouldReturnTypedBindingError()
    {
        FakeCommandHandler handler = new();
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/",
            CreateCommand(),
            user: new ClaimsPrincipal(new ClaimsIdentity()));

        response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        response.Body.ShouldContain("tenant_binding_missing");
        response.Body.ShouldContain("provide-context");
        handler.CreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MissingTenantClaimShouldReturnTypedBindingError()
    {
        FakeCommandHandler handler = new();
        using WebApplication app = BuildApp(handler);
        ClaimsPrincipal user = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "caller-001")],
            authenticationType: "Test"));

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/",
            CreateCommand(),
            user: user);

        response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        response.Body.ShouldContain("tenant_binding_missing");
        response.Body.ShouldContain("provide-context");
        handler.CreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task HandlerTypedErrorsShouldKeepIdempotencyConflictTypedAndSafe()
    {
        FakeCommandHandler handler = new()
        {
            AppendOutcome = ConversationCommandApiOutcome<ConversationCommandAcceptedResult>.Failure(
                new ConversationErrorResult(
                    [
                        ConversationErrorCatalog.CreateError(
                            ConversationErrorCode.IdempotencyConflict,
                            "corr-001",
                            developerGuidance: "Use a new idempotency key for a changed command payload."),
                    ]),
                StatusCodes.Status409Conflict),
        };
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/{conversationId}/messages",
            AppendCommand(),
            routeValues: new Dictionary<string, object?> { ["conversationId"] = Conversation.Value },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        response.Body.ShouldContain("idempotency_conflict");
        response.Body.ShouldContain("use-new-idempotency-key");
        response.Body.ShouldNotContain("EventStore", Case.Insensitive);
        response.Body.ShouldNotContain("stream", Case.Insensitive);
        handler.AppendCalls.ShouldBe(1);
    }

    [Theory]
    [InlineData("audit_sink_unavailable", StatusCodes.Status503ServiceUnavailable, "retry-later")]
    [InlineData("tenant_projection_stale", StatusCodes.Status503ServiceUnavailable, "retry-later")]
    [InlineData("participant_validation_unavailable", StatusCodes.Status503ServiceUnavailable, "retry-later")]
    [InlineData("provider_only_identity_forbidden", StatusCodes.Status400BadRequest, "use-party-identity")]
    public async Task HandlerTypedRemediationErrorsShouldStayCatalogAlignedAndSafe(
        string codeValue,
        int statusCode,
        string expectedClientAction)
    {
        ConversationErrorCode code = ConversationErrorCode.Parse(codeValue);
        FakeCommandHandler handler = new()
        {
            AppendOutcome = ConversationCommandApiOutcome<ConversationCommandAcceptedResult>.Failure(
                new ConversationErrorResult(
                    [
                        ConversationErrorCatalog.CreateError(
                            code,
                            "corr-001",
                            auditHandle: "audit-001",
                            safeFieldDiagnostics: new Dictionary<string, string>
                            {
                                ["remediation"] = "safe-action-required",
                            },
                            developerGuidance: "Use the typed Conversations result to choose remediation."),
                    ]),
                statusCode),
        };
        using WebApplication app = BuildApp(handler);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/{conversationId}/messages",
            AppendCommand(),
            routeValues: new Dictionary<string, object?> { ["conversationId"] = Conversation.Value },
            user: AuthenticatedUser());

        response.StatusCode.ShouldBe(statusCode);
        response.Body.ShouldContain(codeValue);
        response.Body.ShouldContain(expectedClientAction);
        response.Body.ShouldContain(ConversationErrorCatalog.Get(code).SafeMessage);
        response.Body.ShouldContain("https://docs.hexalith.local/conversations/contracts/v1/errors");
        response.Body.ShouldNotContain("tenant-999", Case.Insensitive);
        response.Body.ShouldNotContain("provider-session", Case.Insensitive);
        response.Body.ShouldNotContain("EventStore", Case.Insensitive);
        response.Body.ShouldNotContain("handler", Case.Insensitive);
        response.Body.ShouldNotContain("D:\\", Case.Insensitive);
        handler.AppendCalls.ShouldBe(1);
    }

    private static WebApplication BuildApp(IConversationCommandApiHandler handler)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(handler);

        WebApplication app = builder.Build();
        app.MapConversationCommandApi();
        return app;
    }

    private static async Task<ApiResponse> InvokeAsync<T>(
        WebApplication app,
        string routePattern,
        T body,
        IReadOnlyDictionary<string, object?>? routeValues = null,
        ClaimsPrincipal? user = null)
    {
        RouteEndpoint endpoint = FindEndpoint(app, routePattern);
        DefaultHttpContext context = new()
        {
            RequestServices = app.Services,
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Headers[ConversationReadApi.CorrelationIdHeaderName] = "corr-001";

        string json = JsonSerializer.Serialize(body, JsonOptions);
        byte[] requestBody = Encoding.UTF8.GetBytes(json);
        context.Request.ContentLength = requestBody.Length;
        context.Request.Body = new MemoryStream(requestBody);
        context.Response.Body = new MemoryStream();

        if (routeValues is not null)
        {
            foreach (KeyValuePair<string, object?> routeValue in routeValues)
            {
                context.Request.RouteValues[routeValue.Key] = routeValue.Value;
            }
        }

        await endpoint.RequestDelegate!(context);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8);
        string responseBody = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        return new ApiResponse(context.Response.StatusCode, responseBody);
    }

    private static async Task<ApiResponse> InvokeRawAsync(
        WebApplication app,
        string routePattern,
        string body,
        IReadOnlyDictionary<string, object?>? routeValues = null,
        ClaimsPrincipal? user = null)
    {
        RouteEndpoint endpoint = FindEndpoint(app, routePattern);
        DefaultHttpContext context = new()
        {
            RequestServices = app.Services,
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Headers[ConversationReadApi.CorrelationIdHeaderName] = "corr-001";

        byte[] requestBody = Encoding.UTF8.GetBytes(body);
        context.Request.ContentLength = requestBody.Length;
        context.Request.Body = new MemoryStream(requestBody);
        context.Response.Body = new MemoryStream();

        if (routeValues is not null)
        {
            foreach (KeyValuePair<string, object?> routeValue in routeValues)
            {
                context.Request.RouteValues[routeValue.Key] = routeValue.Value;
            }
        }

        await endpoint.RequestDelegate!(context);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8);
        string responseBody = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        return new ApiResponse(context.Response.StatusCode, responseBody);
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string routePattern)
        => ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText, routePattern, StringComparison.Ordinal));

    private static ClaimsPrincipal AuthenticatedUser()
        => AuthenticatedUser(Tenant);

    private static ClaimsPrincipal AuthenticatedUser(TenantId tenantId)
        => new(new ClaimsIdentity(
            [new Claim(ConversationReadApi.TenantIdClaimType, tenantId.Value), new Claim(ClaimTypes.NameIdentifier, "caller-001")],
            authenticationType: "Test"));

    private static CreateConversationCommand CreateCommand(TenantId? tenant = null)
        => new(Metadata("idem-001", tenant ?? Tenant), Label: "Case 123");

    private static AppendMessageCommand AppendCommand()
        => new(Metadata("idem-001", Tenant), Conversation, Message, Actor, "Hello from the adopter.");

    private static ConversationCommandMetadata Metadata(string idempotencyKey, TenantId tenant)
        => new(SchemaVersion.Current, tenant, Actor, "corr-001", "cause-001", idempotencyKey);

    private static ConversationCreatedResult CreatedResult()
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            "corr-001",
            "idem-001",
            new ReadModelVisibility(ProjectionTrustState.Rebuilding, "Read model is catching up."),
            ConversationCommandType.CreateConversationCommand);

    private sealed record ApiResponse(int StatusCode, string Body);

    private sealed class FakeCommandHandler : IConversationCommandApiHandler
    {
        public int CreateCalls { get; private set; }

        public int AppendCalls { get; private set; }

        public CreateConversationCommand? LastCreateCommand { get; private set; }

        public ConversationCommandApiOutcome<ConversationCreatedResult>? CreateOutcome { get; init; }

        public ConversationCommandApiOutcome<ConversationCommandAcceptedResult>? AppendOutcome { get; init; }

        public ValueTask<ConversationCommandApiOutcome<ConversationCreatedResult>> CreateConversationAsync(
            CreateConversationCommand command,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastCreateCommand = command;
            return ValueTask.FromResult(CreateOutcome ?? ConversationCommandApiOutcome<ConversationCreatedResult>.Success(
                CreatedResult(),
                StatusCodes.Status201Created));
        }

        public ValueTask<ConversationCommandApiOutcome<ConversationCommandAcceptedResult>> AppendMessageAsync(
            AppendMessageCommand command,
            CancellationToken cancellationToken = default)
        {
            AppendCalls++;
            return ValueTask.FromResult(AppendOutcome ?? ConversationCommandApiOutcome<ConversationCommandAcceptedResult>.Success(
                new ConversationCommandAcceptedResult(
                    SchemaVersion.Current,
                    Tenant,
                    Conversation,
                    ConversationCommandType.AppendMessageCommand,
                    "corr-001",
                    "idem-001",
                    new ReadModelVisibility(ProjectionTrustState.Rebuilding, "Read model is catching up.")),
                StatusCodes.Status202Accepted));
        }
    }
}
