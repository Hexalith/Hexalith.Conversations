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
        gateway.SubmitQueryAsync<ConversationListResult>(Arg.Any<SubmitQueryRequest>(), null, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<EventStoreQueryResult<ConversationListResult>>(unauthorized));
        Status(await PilotQueries.DetailAsync(F.Conversation.Value, Caller(), gateway, TestContext.Current.CancellationToken)).ShouldBe(401);
        Status(await PilotQueries.ListAsync(Caller(), gateway, TestContext.Current.CancellationToken)).ShouldBe(401);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(409)]
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
