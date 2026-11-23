extern alias pilot;

using System.Security.Claims;
using System.Text.Json;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using Xunit;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;
using PilotCommands = pilot::Hexalith.Conversations.Api.PilotCommands;
using PilotQueries = pilot::Hexalith.Conversations.Api.PilotQueries;

namespace Hexalith.Conversations.Server.Tests.Api;

public sealed class PilotApiTests
{
    [Theory]
    [InlineData("nativeMembershipOnly", GetConversationQuery.NativeMembershipPurpose, false)]
    [InlineData("advisoryMetadataOnly", ConversationResolutionScoringLabel.SupportedPurpose, true)]
    public async Task BoundedDetailChecksRequestedOwnerPurposeAndProjectsOnlyAllowedFields(string mode, string purpose, bool allowLabel)
    {
        var context = Caller();
        context.Request.QueryString = new("?" + mode + "=true");
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var freshness = new ProjectionFreshnessV1(SchemaVersion.Current, "pos:0000000001", 1, F.At, F.At, TimeSpan.Zero,
            false, ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current);
        var row = new ConversationDetailsV1(SchemaVersion.Current, F.Tenant, F.Conversation, freshness, "Active", "advisory label",
            Messages: [new(new("message-one"), F.Human, "private message", F.At)]);
        var rich = ConversationDetailResult.Visible(SchemaVersion.Current, row, "Ready.");
        gateway.SubmitQueryAsync<ConversationDetailResult>(Arg.Is<SubmitQueryRequest>(q => q.Payload!.Value.GetProperty(mode).GetBoolean()),
            null, Arg.Any<CancellationToken>()).Returns(new EventStoreQueryResult<ConversationDetailResult>("correlation", rich, false, null),
                new EventStoreQueryResult<ConversationDetailResult>("correlation", rich with { ReadPurpose = purpose }, false, null));

        var refused = await PilotQueries.DetailAsync(F.Conversation.Value, context, gateway, TestContext.Current.CancellationToken);
        Status(refused).ShouldBe(503);
        Body<ConversationDetailResult>(refused).Details.ShouldBeNull();

        var safe = await PilotQueries.DetailAsync(F.Conversation.Value, context, gateway, TestContext.Current.CancellationToken);
        Status(safe).ShouldBe(200);
        var detail = Body<ConversationDetailResult>(safe).Details.ShouldNotBeNull();
        detail.Messages.ShouldBeEmpty();
        detail.Label.ShouldBe(allowLabel ? row.Label : null);
    }

    [Theory]
    [InlineData("?nativeMembershipOnly=value")]
    [InlineData("?advisoryMetadataOnly=true&nativeMembershipOnly=true")]
    [InlineData("?nativeMembershipOnly=true&nativeMembershipOnly=false")]
    public async Task AmbiguousBoundedModePreventsDispatch(string query)
    {
        var context = Caller();
        context.Request.QueryString = new(query);
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var denied = await PilotQueries.DetailAsync(F.Conversation.Value, context, gateway, TestContext.Current.CancellationToken);
        Status(denied).ShouldBe(503);
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadsPreserveDownstreamUnauthorized()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var unauthorized = new EventStoreGatewayException(401, "Unauthorized");
        gateway.SubmitQueryAsync<ConversationDetailResult>(Arg.Any<SubmitQueryRequest>(), null, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<EventStoreQueryResult<ConversationDetailResult>>(unauthorized));
        gateway.SubmitQueryAsync<JsonElement>(Arg.Any<SubmitQueryRequest>(), null, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<EventStoreQueryResult<JsonElement>>(unauthorized));
        Status(await PilotQueries.DetailAsync(F.Conversation.Value, Caller(), gateway, TestContext.Current.CancellationToken)).ShouldBe(401);
        Status(await PilotQueries.ListAsync(Caller(), gateway, TestContext.Current.CancellationToken)).ShouldBe(401);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(409)]
    [InlineData(413)]
    public async Task FailedConfirmationKeepsOriginalKeyRecoverable(int status)
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var command = new CreateConversationCommand(new F().CommandMetadata(F.Human));
        var requests = new List<SubmitCommandRequest>();
        gateway.SubmitCommandAsync(Arg.Do<SubmitCommandRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("original-correlation", MessageId: "original-receipt"));
        int polls = 0;
        gateway.GetCommandStatusAsync("original-receipt", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (++polls == 1) throw new EventStoreGatewayException(status, "Confirmation failure", code: "idempotency_conflict");
            return new CommandStatusQueryResponse("original-correlation", "Completed", (int)CommandStatus.Completed, MessageId: "original-receipt")
            { Domain = requests[0].Domain, AggregateId = requests[0].AggregateId, TenantId = requests[0].Tenant };
        });
        var unknown = await PilotCommands.CreateAsync(Caller(command), gateway, TestContext.Current.CancellationToken);
        Status(unknown).ShouldBe(503);
        Body<ConversationErrorResult>(unknown).Errors.Single().Code.ShouldBe(ConversationErrorCode.IdempotencyOutcomeUnknown);
        Status(await PilotCommands.CreateAsync(Caller(command), gateway, TestContext.Current.CancellationToken)).ShouldBe(201);
        requests.Count.ShouldBe(2);
        requests[0].IdempotencyKey.ShouldBe(requests[1].IdempotencyKey);
        requests[0].AggregateId.ShouldBe(requests[1].AggregateId);
    }

    [Theory]
    [InlineData("messageId")]
    [InlineData("tenant")]
    [InlineData("domain")]
    [InlineData("aggregate")]
    public async Task CompletedStatusForDifferentCommandKeepsOutcomeUnknown(string mismatchedField)
    {
        const string receipt = "original-receipt";
        var token = TestContext.Current.CancellationToken;
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var command = new CreateConversationCommand(new F().CommandMetadata(F.Human));
        SubmitCommandRequest request = null!;
        gateway.SubmitCommandAsync(Arg.Do<SubmitCommandRequest>(value => request = value), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("original-correlation", MessageId: receipt));
        gateway.GetCommandStatusAsync(receipt, Arg.Any<CancellationToken>()).Returns(_ =>
            new CommandStatusQueryResponse("original-correlation", nameof(CommandStatus.Completed), (int)CommandStatus.Completed,
                MessageId: mismatchedField == "messageId" ? "another-receipt" : receipt)
            {
                TenantId = mismatchedField == "tenant" ? "another-tenant" : request.Tenant,
                Domain = mismatchedField == "domain" ? "another-domain" : request.Domain,
                AggregateId = mismatchedField == "aggregate" ? "another-aggregate" : request.AggregateId,
            });

        var unknown = await PilotCommands.CreateAsync(Caller(command), gateway, token);

        Status(unknown).ShouldBe(503);
        var error = Body<ConversationErrorResult>(unknown).Errors.Single();
        error.Code.ShouldBe(ConversationErrorCode.IdempotencyOutcomeUnknown);
        error.CorrelationId.ShouldBe(command.Metadata.CorrelationId);
        error.IsRetryable.ShouldBeTrue();
        error.ClientAction.ShouldBe(ConversationErrorClientAction.RetrySameRequest);
        await gateway.Received(1).GetCommandStatusAsync(receipt, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("idempotency_conflict", 409, true)]
    [InlineData("idempotency_key_expired", 409, false)]
    [InlineData("idempotency_in_progress", 503, false)]
    [InlineData(null, 503, false)]
    public async Task SubmissionConflictRequiresActualChangedIntent(string? code, int status, bool conflict)
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SubmitCommandResponse>(new EventStoreGatewayException(409, "Conflict", reasonCode: code)));
        var command = new CreateConversationCommand(new F().CommandMetadata(F.Human));
        var error = await PilotCommands.CreateAsync(Caller(command), gateway, TestContext.Current.CancellationToken);
        Status(error).ShouldBe(status);
        Body<ConversationErrorResult>(error).Errors.Single().Code.ShouldBe(conflict
            ? ConversationErrorCode.IdempotencyConflict : ConversationErrorCode.IdempotencyOutcomeUnknown);
    }

    [Theory]
    [InlineData("correlation-body", "correlation-header", "correlation-body")]
    [InlineData("store-private", "correlation-header", "correlation-header")]
    [InlineData("store-private", "tenant-private", "correlation-trace")]
    [InlineData(null, "correlation-header", "correlation-header")]
    [InlineData(null, null, "correlation-trace")]
    public async Task ValidationErrorsSelectASafeCorrelation(string? bodyCorrelation, string? header, string expected)
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var context = bodyCorrelation is null ? Caller() : Caller(new CreateConversationCommand(
            new ConversationCommandMetadata(SchemaVersion.Current, F.Tenant, F.Human, bodyCorrelation)));
        context.TraceIdentifier = "correlation-trace";
        if (header is not null) context.Request.Headers["X-Correlation-Id"] = header;
        if (bodyCorrelation is null) context.Request.Body = new MemoryStream("{broken"u8.ToArray());

        var response = await PilotCommands.CreateAsync(context, gateway, TestContext.Current.CancellationToken);

        Status(response).ShouldBe(400);
        var error = Body<ConversationErrorResult>(response).Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe(bodyCorrelation is null ? ConversationErrorCode.CommandValidationFailed : ConversationErrorCode.IdempotencyKeyMissing);
        error.CorrelationId.ShouldBe(expected);
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task GatewaySubmissionKeepsOriginalCorrelationWhenPublicErrorUsesAFallback()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        SubmitCommandRequest? submission = null;
        gateway.SubmitCommandAsync(Arg.Do<SubmitCommandRequest>(request => submission = request), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SubmitCommandResponse>(new EventStoreGatewayException(413, "Payload too large")));
        var command = new CreateConversationCommand(new ConversationCommandMetadata(
            SchemaVersion.Current, F.Tenant, F.Human, "store-private", IdempotencyKey: "create-key"));
        var context = Caller(command);
        context.TraceIdentifier = "correlation-trace";

        var response = await PilotCommands.CreateAsync(context, gateway, TestContext.Current.CancellationToken);

        Status(response).ShouldBe(413);
        submission.ShouldNotBeNull().CorrelationId.ShouldBe(command.Metadata.CorrelationId);
        Body<ConversationErrorResult>(response).Errors.ShouldHaveSingleItem().CorrelationId.ShouldBe(context.TraceIdentifier);
    }

    [Fact]
    public async Task ExpiredReceiptDirectsReconciliationWithoutRetrying()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SubmitCommandResponse>(new EventStoreGatewayException(409, "Conflict", code: "idempotency_key_expired")));
        var command = new CreateConversationCommand(new F().CommandMetadata(F.Human));

        var response = await PilotCommands.CreateAsync(Caller(command), gateway, TestContext.Current.CancellationToken);

        Status(response).ShouldBe(409);
        var wire = JsonSerializer.Serialize(Body<ConversationErrorResult>(response), JsonSerializerOptions.Web);
        var error = JsonSerializer.Deserialize<ConversationErrorResult>(wire, JsonSerializerOptions.Web)
            .ShouldNotBeNull().Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe(ConversationErrorCode.IdempotencyOutcomeUnknown);
        error.CorrelationId.ShouldBe(command.Metadata.CorrelationId);
        error.IsRetryable.ShouldBeFalse();
        error.ClientAction.ShouldBeNull();
        error.SafeMessage.ShouldNotBeNull().ShouldContain("Reconcile the original conversation and message");
        error.DeveloperGuidance.ShouldBe(error.SafeMessage);
    }

    [Fact]
    public async Task OversizedSubmissionPreservesTheNonRetryableRefusal()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SubmitCommandResponse>(new EventStoreGatewayException(413, "Payload too large")));
        var command = new CreateConversationCommand(new F().CommandMetadata(F.Human));

        var response = await PilotCommands.CreateAsync(Caller(command), gateway, TestContext.Current.CancellationToken);

        Status(response).ShouldBe(413);
        var error = Body<ConversationErrorResult>(response).Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        error.CorrelationId.ShouldBe(command.Metadata.CorrelationId);
        error.IsRetryable.ShouldBeFalse();
        error.ClientAction.ShouldBe(ConversationErrorClientAction.CorrectRequest);
        error.DeveloperGuidance.ShouldNotBeNull().ShouldContain("Reduce the command payload size");
        await gateway.DidNotReceiveWithAnyArgs().GetCommandStatusAsync(default!, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("\"conversations\": []", 503)]
    [InlineData("\"conversations\": [null], \"page\": {\"returnedCount\": 1}", 503)]
    [InlineData("\"page\": {\"returnedCount\": 0}", 503)]
    [InlineData("\"conversations\": null, \"page\": {\"returnedCount\": 0}", 503)]
    [InlineData("\"conversations\": [], \"page\": {\"returnedCount\": 1}", 503)]
    [InlineData("\"conversations\": [], \"page\": {\"returnedCount\": 0}", 200)]
    public async Task OnlyAWellFormedOwnerListIsAvailable(string listFields, int expectedStatus)
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var data = $$"""
            {"schemaVersion": 1, "freshnessState": "Current", "reasonCode": "current", "safeNextAction": "Ready", {{listFields}}}
            """;
        gateway.SubmitQueryAsync<JsonElement>(Arg.Any<SubmitQueryRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(new EventStoreQueryResult<JsonElement>("correlation", JsonSerializer.Deserialize<JsonElement>(data), false, null));

        var response = await PilotQueries.ListAsync(Caller(), gateway, TestContext.Current.CancellationToken);

        Status(response).ShouldBe(expectedStatus);
        Body<ConversationListResult>(response).FreshnessState.ShouldBe(expectedStatus == 200 ? ProjectionTrustState.Current : ProjectionTrustState.Unavailable);
    }

    [Fact]
    public async Task InvalidListPageIsHiddenBeforeDispatch()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var context = Caller();
        context.Request.QueryString = new("?pageSize=invalid");

        var response = await PilotQueries.ListAsync(context, gateway, TestContext.Current.CancellationToken);

        Status(response).ShouldBe(200);
        Body<ConversationListResult>(response).FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task AppendAllocatesOccurrenceAndRetainsSubmittedTimeAsIntent()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var request = new AppendMessageCommand(new F().CommandMetadata(F.Human), F.Conversation, new("message-one"), F.Human,
            "message", OperationTimestamp: DateTimeOffset.UtcNow.AddYears(10));
        SubmitCommandRequest? sent = null;
        gateway.SubmitCommandAsync(Arg.Do<SubmitCommandRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("correlation"));
        var before = DateTimeOffset.UtcNow;
        await PilotCommands.AppendAsync(F.Conversation.Value, Caller(request), gateway, TestContext.Current.CancellationToken);
        var occurred = sent.ShouldNotBeNull().Payload.Deserialize<AppendMessage>(JsonSerializerOptions.Web).ShouldNotBeNull();
        occurred.PostedAt.ShouldBeInRange(before, DateTimeOffset.UtcNow);
        occurred.PublicCommand.OperationTimestamp.ShouldBe(request.OperationTimestamp);
    }

    [Theory]
    [InlineData(false, 400)]
    [InlineData(true, 403)]
    public async Task DeniedFreshAppendNeverReachesTheGateway(bool foreignCaller, int expectedStatus)
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var metadata = new F().CommandMetadata(F.Human) with { IdempotencyKey = foreignCaller ? "foreign-key" : null };
        var command = new AppendMessageCommand(metadata, F.Conversation, new("fresh-denied-message"), F.Human, "message");
        var context = Caller(command);
        if (foreignCaller)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "another-subject"), new Claim("tid", "another-tenant")], "authenticated"));
        }

        var denied = await PilotCommands.AppendAsync(F.Conversation.Value, context, gateway, TestContext.Current.CancellationToken);

        Status(denied).ShouldBe(expectedStatus);
        Body<ConversationErrorResult>(denied).Errors.Single().Code.ShouldBe(foreignCaller
            ? ConversationErrorCode.TenantContextMismatch : ConversationErrorCode.IdempotencyKeyMissing);
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadCorrelationFollowsTheAuthenticatedRequestThroughTheGateway()
    {
        var context = Caller();
        context.Request.Headers.Authorization = "Bearer caller";
        context.Request.Headers["X-Correlation-ID"] = "original-correlation";
        var transport = new Hexalith.Conversations.Server.Tests.Agents.StubHttpHandler((request, _) =>
        {
            request.Headers.GetValues("X-Correlation-ID").ShouldBe(["original-correlation"]);
            request.Headers.Authorization!.ToString().ShouldBe("Bearer caller");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });
        using var client = new HttpClient(new pilot::Hexalith.Conversations.Api.HumanGatewayBearerHandler(
            new HttpContextAccessor { HttpContext = context }) { InnerHandler = transport });
        using var response = await client.PostAsync(new Uri("http://gateway/query"), null, TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue();
        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe("original-correlation");
    }

    private static DefaultHttpContext Caller(object? command = null)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "subject-one"), new Claim("tid", F.Tenant.Value)], "authenticated")),
        };
        context.Request.Method = command is null ? "GET" : "POST";
        if (command is not null)
            context.Request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(command, JsonSerializerOptions.Web));
        return context;
    }

    private static int? Status(IResult result) => ((IStatusCodeHttpResult)result).StatusCode;

    private static T Body<T>(IResult result) => ((IValueHttpResult)result).Value.ShouldBeOfType<T>();
}
