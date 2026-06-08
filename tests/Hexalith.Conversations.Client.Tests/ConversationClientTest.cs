// <copyright file="ConversationClientTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Client.Tests;

public sealed class ConversationClientTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly MessageId Message = new("message-001");
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateConversationShouldPostV1ContractBodyAndSafeHeaders()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationCreatedResult response = CreatedResult(idempotencyKey: "idem-create-001");
        handler.EnqueueJson(HttpStatusCode.Created, response);
        ConversationClient client = CreateClient(handler);
        CreateConversationCommand command = CreateCommand(idempotencyKey: "idem-create-001");

        ConversationClientResult<ConversationCreatedResult> result = await client
            .CreateConversationAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(response);
        handler.Requests.Count.ShouldBe(1);
        RecordedRequest request = handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.PathAndQuery.ShouldBe("/api/v1/conversations");
        request.Header("X-Correlation-Id").ShouldBe("corr-001");
        request.Header("X-Causation-Id").ShouldBe("cause-001");
        request.Header("Idempotency-Key").ShouldBe("idem-create-001");
        request.Body.ShouldContain("\"schemaVersion\":1");
        request.Body.ShouldContain("\"tenantId\":\"tenant:tenant-001\"");
        request.Body.ShouldContain("\"actorPartyId\":\"party:party-actor\"");
        request.Body.ShouldContain("\"correlationId\":\"corr-001\"");
        request.Body.ShouldContain("\"idempotencyKey\":\"idem-create-001\"");
        request.Body.ShouldNotContain("EventStore", Case.Insensitive);
        request.Body.ShouldNotContain("stream", Case.Insensitive);
        request.PathAndQuery.ShouldNotContain("provider-session-001", Case.Insensitive);
    }

    [Fact]
    public async Task AppendMessageShouldPostV1ContractBodyWithoutProviderIdentityHeaders()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationCommandAcceptedResult response = AcceptedResult(ConversationCommandType.AppendMessageCommand, "idem-append-001");
        handler.EnqueueJson(HttpStatusCode.Accepted, response);
        ConversationClient client = CreateClient(handler);
        AppendMessageCommand command = AppendCommand(idempotencyKey: "idem-append-001");

        ConversationClientResult<ConversationCommandAcceptedResult> result = await client
            .AppendMessageAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(response);
        RecordedRequest request = handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.PathAndQuery.ShouldBe("/api/v1/conversations/conversation-001/messages");
        request.Header("X-Correlation-Id").ShouldBe("corr-001");
        request.Header("Idempotency-Key").ShouldBe("idem-append-001");
        request.Body.ShouldContain("\"conversationId\":\"conv:conversation-001\"");
        request.Body.ShouldContain("\"messageId\":\"message:message-001\"");
        request.Body.ShouldContain("\"providerCorrelation\"");
        request.PathAndQuery.ShouldNotContain("provider-session-001", Case.Insensitive);
        request.Headers.Values.SelectMany(static values => values).ShouldNotContain("provider-session-001");
    }

    [Fact]
    public async Task ReassignProjectShouldPostV1ContractBodyAndSafeHeaders()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationCommandAcceptedResult response = AcceptedResult(
            ConversationCommandType.ReassignConversationProjectCommand,
            "idem-project-001");
        handler.EnqueueJson(HttpStatusCode.Accepted, response);
        ConversationClient client = CreateClient(handler);
        ReassignConversationProjectCommand command = ProjectCommand(idempotencyKey: "idem-project-001");

        ConversationClientResult<ConversationCommandAcceptedResult> result = await client
            .ReassignConversationProjectAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(response);
        RecordedRequest request = handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.PathAndQuery.ShouldBe("/api/v1/conversations/conversation-001/project");
        request.Header("X-Correlation-Id").ShouldBe("corr-001");
        request.Header("Idempotency-Key").ShouldBe("idem-project-001");
        request.Body.ShouldContain("\"conversationId\":\"conv:conversation-001\"");
        request.Body.ShouldContain("\"operation\":\"Assign\"");
        request.Body.ShouldContain("\"projectId\":\"project:project-002\"");
        request.Body.ShouldNotContain("EventStore", Case.Insensitive);
        request.Body.ShouldNotContain("stream", Case.Insensitive);
    }

    [Fact]
    public async Task GetConversationShouldUseReadRouteAndPreserveCurrentFreshnessMetadata()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationDetailResult response = ConversationDetailResult.Visible(
            SchemaVersion.Current,
            Details(ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current, isStale: false),
            "Timeline is current.");
        handler.EnqueueJson(HttpStatusCode.OK, response);
        ConversationClient client = CreateClient(handler);

        ConversationClientResult<ConversationDetailResult> result = await client
            .GetConversationAsync(Query(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Details.ShouldNotBeNull();
        result.Value.Details.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
        result.Value.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Value.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Current);
        RecordedRequest request = handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Get);
        request.PathAndQuery.ShouldBe("/api/v1/conversations/conversation-001");
        request.Header("X-Correlation-Id").ShouldBe("corr-001");
        request.Body.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetConversationShouldDeserializeNonSeekableHttpContent()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationDetailResult response = ConversationDetailResult.Visible(
            SchemaVersion.Current,
            Details(ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current, isStale: false),
            "Timeline is current.");
        handler.EnqueueNonSeekableJson(HttpStatusCode.OK, response);
        ConversationClient client = CreateClient(handler);

        ConversationClientResult<ConversationDetailResult> result = await client
            .GetConversationAsync(Query(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Details.ShouldNotBeNull();
        result.Value.Details.ConversationId.ShouldBe(Conversation);
    }

    [Fact]
    public async Task ListConversationsShouldUseReadListRouteWithProjectFilterAndPaging()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationListResult response = ListResult(continuationCursor: "next-cursor-001");
        handler.EnqueueJson(HttpStatusCode.OK, response);
        ConversationClient client = CreateClient(handler);
        ListConversationsQuery query = new(
            SchemaVersion.Current,
            Tenant,
            "caller-001",
            "corr-001",
            new ConversationListFilterV1(ProjectId: new ProjectId("project-001")),
            new ConversationPageRequest(10, "cursor-001"));

        ConversationClientResult<ConversationListResult> result = await client
            .ListConversationsAsync(query, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.FreshnessState.ShouldBe(response.FreshnessState);
        result.Value.Page.ShouldBe(response.Page);
        result.Value.Conversations.Single().ConversationId.ShouldBe(Conversation);
        RecordedRequest request = handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Get);
        request.PathAndQuery.ShouldBe("/api/v1/conversations?projectId=project-001&pageSize=10&cursor=cursor-001");
        request.Header("X-Correlation-Id").ShouldBe("corr-001");
        request.Header("X-Tenant-Id").ShouldBe("tenant-001");
        request.Header("X-Caller-Principal-Id").ShouldBe("caller-001");
        request.Body.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Stale", "stale_threshold_exceeded", true, HttpStatusCode.OK)]
    [InlineData("Rebuilding", "rebuilding", false, HttpStatusCode.OK)]
    [InlineData("Unavailable", "unavailable", false, HttpStatusCode.ServiceUnavailable)]
    [InlineData("Forbidden", "forbidden", false, HttpStatusCode.NotFound)]
    public async Task GetConversationShouldNotConvertNonCurrentFreshnessIntoFreshTimeline(
        string stateValue,
        string reasonValue,
        bool isStale,
        HttpStatusCode statusCode)
    {
        ProjectionTrustState state = ProjectionTrustState.Parse(stateValue);
        ProjectionFreshnessReasonCode reason = ProjectionFreshnessReasonCode.Parse(reasonValue);
        ConversationDetailResult response = state == ProjectionTrustState.Forbidden
            ? ConversationDetailResult.Hidden(SchemaVersion.Current)
            : state == ProjectionTrustState.Unavailable
                ? ConversationDetailResult.Unavailable(SchemaVersion.Current)
                : ConversationDetailResult.Visible(
                    SchemaVersion.Current,
                    Details(state, reason, isStale),
                    "Timeline is not current.");
        using FakeHttpMessageHandler handler = new();
        handler.EnqueueJson(statusCode, response);
        ConversationClient client = CreateClient(handler);

        ConversationClientResult<ConversationDetailResult> result = await client
            .GetConversationAsync(Query(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.FreshnessState.ShouldBe(state);
        (result.Value.Details?.Freshness.AllowsTrustBearingDecision() ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task CommandsShouldReturnTypedErrorsForUnsupportedSchemaWithoutSendingRequest()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationClient client = CreateClient(handler);
        CreateConversationCommand command = CreateCommand(schemaVersion: new SchemaVersion(99));

        ConversationClientResult<ConversationCreatedResult> result = await client
            .CreateConversationAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Errors.Single().Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        result.Error.Errors.Single().Category.ShouldBe(ConversationErrorCategory.Versioning);
        result.Error.Errors.Single().ClientAction.ShouldBe(ConversationErrorClientAction.UseSupportedVersion);
        result.Error.Errors.Single().SafeMessage.ShouldBe("Use supported Conversations contract and client versions.");
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandsShouldMapIdempotencyConflictAndDuplicateReplayAsTypedOutcomes()
    {
        using FakeHttpMessageHandler handler = new();
        ConversationCommandAcceptedResult replay = AcceptedResult(
            ConversationCommandType.AppendMessageCommand,
            "idem-append-001");
        handler.EnqueueJson(HttpStatusCode.OK, replay);
        handler.EnqueueJson(
            HttpStatusCode.Conflict,
            ErrorResult(
                ConversationErrorCode.IdempotencyConflict,
                ConversationErrorCategory.Conflict,
                retryable: false,
                "corr-001"));
        ConversationClient client = CreateClient(handler);

        ConversationClientResult<ConversationCommandAcceptedResult> duplicate = await client
            .AppendMessageAsync(AppendCommand(idempotencyKey: "idem-append-001"), TestContext.Current.CancellationToken);
        ConversationClientResult<ConversationCommandAcceptedResult> conflict = await client
            .AppendMessageAsync(AppendCommand(idempotencyKey: "idem-append-001", text: "changed payload"), TestContext.Current.CancellationToken);

        duplicate.IsSuccess.ShouldBeTrue();
        duplicate.Value.ShouldBe(replay);
        conflict.IsSuccess.ShouldBeFalse();
        conflict.Error.ShouldNotBeNull();
        conflict.Error!.Errors.Single().Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        conflict.Error.Errors.Single().ClientAction.ShouldBe(ConversationErrorClientAction.UseNewIdempotencyKey);
        handler.Requests.Select(r => r.Header("Idempotency-Key")).ShouldBe(["idem-append-001", "idem-append-001"]);
    }

    [Fact]
    public async Task TimeoutRetryShouldPreserveCallerIdempotencyMetadata()
    {
        using FakeHttpMessageHandler handler = new();
        handler.EnqueueException(new TaskCanceledException("Synthetic timeout."));
        handler.EnqueueJson(HttpStatusCode.Accepted, AcceptedResult(ConversationCommandType.AppendMessageCommand, "idem-timeout-001"));
        ConversationClient client = CreateClient(handler);
        AppendMessageCommand command = AppendCommand(idempotencyKey: "idem-timeout-001");

        ConversationClientResult<ConversationCommandAcceptedResult> timeout = await client
            .AppendMessageAsync(command, TestContext.Current.CancellationToken);
        ConversationClientResult<ConversationCommandAcceptedResult> retry = await client
            .AppendMessageAsync(command, TestContext.Current.CancellationToken);

        timeout.IsSuccess.ShouldBeFalse();
        timeout.Error.ShouldNotBeNull();
        timeout.Error!.Errors.Single().Code.ShouldBe(ConversationErrorCode.IdempotencyOutcomeUnknown);
        timeout.Error.Errors.Single().ClientAction.ShouldBe(ConversationErrorClientAction.RetrySameRequest);
        retry.IsSuccess.ShouldBeTrue();
        handler.Requests.Count.ShouldBe(2);
        handler.Requests.Select(r => r.Header("Idempotency-Key")).ShouldBe(["idem-timeout-001", "idem-timeout-001"]);
        handler.Requests.Select(r => r.Body).Distinct(StringComparer.Ordinal).Count().ShouldBe(1);
    }

    [Fact]
    public async Task SanitizedServerErrorsShouldRemainTypedAndContentSafe()
    {
        using FakeHttpMessageHandler handler = new();
        handler.EnqueueString(
            HttpStatusCode.InternalServerError,
            "EventStore stream exception at D:\\secret\\path");
        ConversationClient client = CreateClient(handler);

        ConversationClientResult<ConversationDetailResult> result = await client
            .GetConversationAsync(Query(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        string serialized = JsonSerializer.Serialize(result.Error, JsonOptions);
        serialized.ShouldContain("idempotency_outcome_unknown");
        serialized.ShouldContain("retry-same-request");
        serialized.ShouldNotContain("EventStore", Case.Insensitive);
        serialized.ShouldNotContain("D:\\", Case.Insensitive);
        serialized.ShouldNotContain("stream", Case.Insensitive);
    }

    [Fact]
    public async Task TenantDenialWithoutTypedBodyShouldMapToSafeAuthorizationError()
    {
        using FakeHttpMessageHandler handler = new();
        handler.EnqueueString(HttpStatusCode.Forbidden, "tenant tenant-999 denied at server route /api/v1/conversations");
        ConversationClient client = CreateClient(handler);

        ConversationClientResult<ConversationCreatedResult> result = await client
            .CreateConversationAsync(CreateCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        string serialized = JsonSerializer.Serialize(result.Error, JsonOptions);
        serialized.ShouldContain("tenant_isolation_violation");
        serialized.ShouldContain("check-access");
        serialized.ShouldNotContain("tenant-999", Case.Insensitive);
        serialized.ShouldNotContain("server route", Case.Insensitive);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "command_validation_failed", "correct-request")]
    [InlineData(HttpStatusCode.Unauthorized, "tenant_isolation_violation", "check-access")]
    [InlineData(HttpStatusCode.Forbidden, "tenant_isolation_violation", "check-access")]
    [InlineData(HttpStatusCode.NotFound, "aggregate_not_found", "hide-or-refresh")]
    [InlineData(HttpStatusCode.Conflict, "idempotency_conflict", "use-new-idempotency-key")]
    [InlineData(HttpStatusCode.InternalServerError, "idempotency_outcome_unknown", "retry-same-request")]
    public async Task NonJsonErrorResponsesShouldMapToTypedSafeFallback(
        HttpStatusCode statusCode,
        string expectedCode,
        string expectedAction)
    {
        using FakeHttpMessageHandler handler = new();
        handler.EnqueueString(statusCode, "EventStore handler stream failure at C:\\private");
        ConversationClient client = CreateClient(handler);

        ConversationClientResult<ConversationCreatedResult> result = await client
            .CreateConversationAsync(CreateCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        string serialized = JsonSerializer.Serialize(result.Error, JsonOptions);
        serialized.ShouldContain(expectedCode);
        serialized.ShouldContain(expectedAction);
        serialized.ShouldNotContain("EventStore", Case.Insensitive);
        serialized.ShouldNotContain("handler", Case.Insensitive);
        serialized.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public void ServiceCollectionExtensionShouldRegisterTypedClientWithConfiguredEndpoint()
    {
        ServiceCollection services = new();

        services.AddHexalithConversationsClient(options =>
        {
            options.Endpoint = new Uri("https://conversations.example.test/");
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        IConversationClient client = provider.GetRequiredService<IConversationClient>();

        client.ShouldBeOfType<ConversationClient>();
    }

    [Fact]
    public void ServiceCollectionExtensionShouldRejectMissingEndpoint()
    {
        ServiceCollection services = new();

        Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithConversationsClient(options => options.Endpoint = null));
    }

    [Fact]
    public void ServiceCollectionExtensionShouldRejectRelativeEndpoint()
    {
        ServiceCollection services = new();

        Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithConversationsClient(options => options.Endpoint = new Uri("/relative", UriKind.Relative)));
    }

    [Fact]
    public void ServiceCollectionExtensionShouldRejectNonHttpScheme()
    {
        ServiceCollection services = new();

        Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithConversationsClient(options => options.Endpoint = new Uri("ftp://conversations.example.test/")));
    }

    [Fact]
    public async Task ServiceCollectionExtensionShouldReturnBuilderForHandlerChainingAndUseConfiguredEndpoint()
    {
        ServiceCollection services = new();
        using FakeHttpMessageHandler primaryHandler = new();
        primaryHandler.EnqueueJson(HttpStatusCode.Created, CreatedResult("idem-create-001"));
        HandlerProbe probe = new();

        IHttpClientBuilder builder = services.AddHexalithConversationsClient(options =>
        {
            options.Endpoint = new Uri("https://conversations.example.test/");
        });

        builder.ShouldNotBeNull();
        builder.AddHttpMessageHandler(() => new ProbeDelegatingHandler(probe));
        builder.ConfigurePrimaryHttpMessageHandler(() => primaryHandler);

        using ServiceProvider provider = services.BuildServiceProvider();
        IConversationClient client = provider.GetRequiredService<IConversationClient>();

        ConversationClientResult<ConversationCreatedResult> result = await client
            .CreateConversationAsync(CreateCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        probe.Paths.ShouldBe(["/api/v1/conversations"]);
        RecordedRequest request = primaryHandler.Requests.Single();
        request.AbsoluteUri.ShouldBe("https://conversations.example.test/api/v1/conversations");
        request.Header(ProbeDelegatingHandler.HeaderName).ShouldBe("observed");
    }

    private static ConversationClient CreateClient(FakeHttpMessageHandler handler)
        => new(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://conversations.example.test/"),
        });

    private static CreateConversationCommand CreateCommand(
        string idempotencyKey = "idem-create-001",
        SchemaVersion? schemaVersion = null)
        => new(
            Metadata(idempotencyKey, schemaVersion),
            new BusinessReference("crm", "case-123"),
            new ProjectId("project-001"),
            new FolderId("folder-001"),
            "Case 123",
            new ProviderCorrelationMetadata(
                "provider-a",
                "llm",
                SchemaVersion.Current,
                ProviderSessionReference: "provider-session-001"));

    private static AppendMessageCommand AppendCommand(string idempotencyKey = "idem-append-001", string text = "Hello from the adopter.")
        => new(
            Metadata(idempotencyKey),
            Conversation,
            Message,
            Actor,
            text,
            new ProviderCorrelationMetadata(
                "provider-a",
                "llm",
                SchemaVersion.Current,
                ProviderSessionReference: "provider-session-001"));

    private static ReassignConversationProjectCommand ProjectCommand(string idempotencyKey = "idem-project-001")
        => new(
            Metadata(idempotencyKey),
            Conversation,
            new ConversationProjectAssignment(
                ConversationProjectAssignmentOperation.Assign,
                new ProjectId("project-002")),
            ExpectedCurrentProjectId: new ProjectId("project-001"));

    private static ConversationCommandMetadata Metadata(string idempotencyKey, SchemaVersion? schemaVersion = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            Tenant,
            Actor,
            "corr-001",
            "cause-001",
            idempotencyKey);

    private static GetConversationQuery Query()
        => new(SchemaVersion.Current, Tenant, "caller-001", "corr-001", Conversation);

    private static ConversationListResult ListResult(string? continuationCursor = null)
        => new(
            SchemaVersion.Current,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [
                new ConversationSummaryV1(
                    SchemaVersion.Current,
                    Tenant,
                    Conversation,
                    Freshness(ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current, isStale: false),
                    "Open",
                    Label: "Case 123",
                    ProjectId: new ProjectId("project-001")),
            ],
            new ConversationPageMetadata(1, continuationCursor),
            "Use the cursor only with the same tenant, caller, filters, and ordering.");

    private static ConversationCreatedResult CreatedResult(string idempotencyKey)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            "corr-001",
            idempotencyKey,
            new ReadModelVisibility(ProjectionTrustState.Rebuilding, "Read model is catching up."),
            ConversationCommandType.CreateConversationCommand);

    private static ConversationCommandAcceptedResult AcceptedResult(ConversationCommandType commandType, string idempotencyKey)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            commandType,
            "corr-001",
            idempotencyKey,
            new ReadModelVisibility(ProjectionTrustState.Rebuilding, "Read model is catching up."));

    private static ConversationDetailsV1 Details(
        ProjectionTrustState state,
        ProjectionFreshnessReasonCode reason,
        bool isStale)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            new ProjectionFreshnessV1(
                SchemaVersion.Current,
                "pos:0000000001",
                1,
                Now,
                Now.AddSeconds(1),
                TimeSpan.FromSeconds(1),
                isStale,
                state,
                reason),
            "Open",
            "Case 123",
            Participants:
            [
                new ConversationParticipantProjectionV1(Actor, ParticipantType.Human, ParticipantRole.Member),
            ],
            Messages:
            [
                new ConversationTimelineMessageProjectionV1(Message, Actor, "Hello from the adopter.", Now),
            ]);

    private static ProjectionFreshnessV1 Freshness(
        ProjectionTrustState state,
        ProjectionFreshnessReasonCode reason,
        bool isStale)
        => new(
            SchemaVersion.Current,
            "pos:0000000001",
            1,
            Now,
            Now.AddSeconds(1),
            TimeSpan.FromSeconds(1),
            isStale,
            state,
            reason);

    private static ConversationErrorResult ErrorResult(
        ConversationErrorCode code,
        ConversationErrorCategory category,
        bool retryable,
        string correlationId)
        => new(
            [
                ConversationErrorCatalog.CreateError(
                    code,
                    correlationId,
                    developerGuidance: "Use the typed Conversations result to decide retry behavior."),
            ]);

    private static JsonSerializerOptions JsonOptions => new(JsonSerializerDefaults.Web);

    private sealed class FakeHttpMessageHandler : HttpMessageHandler, IDisposable
    {
        private readonly Queue<object> _responses = new();

        public List<RecordedRequest> Requests { get; } = [];

        public void EnqueueJson<T>(HttpStatusCode statusCode, T body)
            => _responses.Enqueue(new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(body, options: JsonOptions),
            });

        public void EnqueueString(HttpStatusCode statusCode, string body)
            => _responses.Enqueue(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            });

        public void EnqueueNonSeekableJson<T>(HttpStatusCode statusCode, T body)
        {
            string json = JsonSerializer.Serialize(body, JsonOptions);
            _responses.Enqueue(new HttpResponseMessage(statusCode)
            {
                Content = new StreamContent(new NonSeekableMemoryStream(Encoding.UTF8.GetBytes(json))),
            });
        }

        public void EnqueueException(Exception exception)
            => _responses.Enqueue(exception);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                request.RequestUri?.PathAndQuery ?? string.Empty,
                request.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.Ordinal),
                body));

            object next = _responses.Dequeue();
            if (next is Exception exception)
            {
                throw exception;
            }

            return (HttpResponseMessage)next;
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        string AbsoluteUri,
        string PathAndQuery,
        IReadOnlyDictionary<string, string[]> Headers,
        string Body)
    {
        public string? Header(string name)
            => Headers.TryGetValue(name, out string[]? values) ? values.SingleOrDefault() : null;
    }

    private sealed class HandlerProbe
    {
        public List<string> Paths { get; } = [];
    }

    private sealed class ProbeDelegatingHandler(HandlerProbe probe) : DelegatingHandler
    {
        public const string HeaderName = "X-Builder-Probe";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            probe.Paths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            request.Headers.Add(HeaderName, "observed");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class NonSeekableMemoryStream(byte[] buffer) : MemoryStream(buffer)
    {
        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin loc)
            => throw new NotSupportedException();
    }
}
