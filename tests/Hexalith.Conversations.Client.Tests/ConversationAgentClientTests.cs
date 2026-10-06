// <copyright file="ConversationAgentClientTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Client.Tests;

/// <summary>Exact returned scope, posting identity and non-disclosing gateway evidence.</summary>
public sealed class ConversationAgentClientTests
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Agent = new("immutable-organization-agent");
    private static readonly MessageId Message = new("deterministic-message");
    private static readonly DateTimeOffset At = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Verifies gateway posting preserves intent and rejects different returned message id.</summary>
    [Fact]
    public async Task GatewayPostingPreservesIntentAndRejectsDifferentReturnedMessageId()
    {
        using var fixture = new ConversationAgentHttpFixture();
        using var http = new HttpClient(fixture) { BaseAddress = new("https://local.invalid/") };
        var client = new ConversationClient(http);
        var command = new AppendMessageCommand(new(SchemaVersion.Current, Tenant, Agent, "correlation", IdempotencyKey: "post-key"),
            Conversation, Message, Agent, "text", AgentProvenance: new("agent-call", true, false), OperationTimestamp: At);
        fixture.Enqueue(new SubmitCommandResponse("correlation", MessageId: "gateway-command-id"));
        fixture.Enqueue(new SubmitCommandResponse("correlation", MessageId: "gateway-command-id"));
        var first = await client.PostAgentMessageAsync(command, TestContext.Current.CancellationToken);
        var retry = await client.PostAgentMessageAsync(command, TestContext.Current.CancellationToken);
        first.MessageId.ShouldBe(Message);
        first.Persisted.ShouldBeFalse();
        retry.ShouldBe(first);
        fixture.Requests[0].GetProperty("messageId").GetString().ShouldBe(fixture.Requests[1].GetProperty("messageId").GetString());
        var payload = fixture.Requests[0].GetProperty("payload").GetProperty("publicCommand");
        payload.Deserialize<AppendMessageCommand>(Options)!.MessageId.ShouldBe(Message);
        payload.GetProperty("operationTimestamp").GetDateTimeOffset().ShouldBe(At);
        fixture.Enqueue(new SubmitCommandResponse("correlation", JsonSerializer.SerializeToElement(
            new ConversationAgentCommandResult(ConversationAgentsOutcome.Available, MessageId: new MessageId("changed")), Options), "gateway-command-id"));
        (await client.PostAgentMessageAsync(command, TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.Conflict);
    }

    /// <summary>Verifies current read rejects foreign scope unknown outcome missing checkpoint and wrong message.</summary>
    [Fact]
    public async Task CurrentReadRejectsForeignScopeUnknownOutcomeMissingCheckpointAndWrongMessage()
    {
        using var fixture = new ConversationAgentHttpFixture();
        using var http = new HttpClient(fixture) { BaseAddress = new("https://local.invalid/") };
        var client = new ConversationClient(http);
        var query = new ConversationAgentReadQuery(Tenant, Conversation, Message);
        var valid = Current();
        foreach (var wrong in new[] { valid with { TenantId = new("foreign") }, valid with { ConversationId = new("foreign") },
            valid with { Outcome = (ConversationAgentsOutcome)999 }, valid with { ObservationId = null },
            valid with { Messages = [valid.Messages![0] with { MessageId = new("changed") }] }, valid with { SourceRevision = null } })
        {
            fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(wrong, Options)));
            var read = await client.GetAgentConversationAsync(query, TestContext.Current.CancellationToken);
            read.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable);
            read.Messages.ShouldBeNull();
            read.Participants.ShouldBeNull();
        }
        var malformed = JsonSerializer.SerializeToElement(valid, Options).EnumerateObject()
            .Where(p => p.Name != "outcome").ToDictionary(p => p.Name, p => p.Value);
        fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(malformed, Options)));
        (await client.GetAgentConversationAsync(query, TestContext.Current.CancellationToken)).Messages.ShouldBeNull();
        fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(valid, Options)));
        (await client.GetAgentConversationAsync(query, TestContext.Current.CancellationToken)).Messages!.Single().MessageId.ShouldBe(Message);
    }

    /// <summary>Verifies stale degraded paged and failed gateway responses never disclose content.</summary>
    [Fact]
    public async Task StaleDegradedPagedAndFailedGatewayResponsesNeverDiscloseContent()
    {
        using var fixture = new ConversationAgentHttpFixture();
        using var http = new HttpClient(fixture) { BaseAddress = new("https://local.invalid/") };
        var client = new ConversationClient(http);
        var query = new ConversationAgentReadQuery(Tenant, Conversation, Message);
        foreach (var metadata in new[] { new QueryResponseMetadata(IsStale: true), new QueryResponseMetadata(IsDegraded: true),
            new QueryResponseMetadata(Paging: new(1, NextCursor: "more", HasMore: true)) })
        {
            fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(Current(), Options), Metadata: metadata));
            var read = await client.GetAgentConversationAsync(query, TestContext.Current.CancellationToken);
            read.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable);
            read.Messages.ShouldBeNull();
        }
        fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(Current(), Options), Success: false));
        (await client.GetAgentConversationAsync(query, TestContext.Current.CancellationToken)).Messages.ShouldBeNull();
    }

    /// <summary>Verifies active count requires exact tenant window and complete evidence.</summary>
    [Fact]
    public async Task ActiveCountRequiresExactTenantWindowAndCompleteEvidence()
    {
        using var fixture = new ConversationAgentHttpFixture();
        using var http = new HttpClient(fixture) { BaseAddress = new("https://local.invalid/") };
        var client = new ConversationClient(http);
        var query = new ConversationActiveCountQuery(Tenant, At.AddDays(-1), At.AddDays(1));
        var valid = new ConversationActiveCountResult(ConversationAgentsOutcome.Available, 2, "catalogue", At, Tenant, query.CreatedFromInclusive, query.CreatedToExclusive);
        foreach (var wrong in new[] { valid with { TenantId = new("foreign") }, valid with { CreatedToExclusive = At.AddDays(2) },
            valid with { CatalogCheckpoint = null }, valid with { Count = -1 }, valid with { SourceContractVersion = 2 } })
        {
            fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(wrong, Options)));
            (await client.GetActiveConversationCountAsync(query, TestContext.Current.CancellationToken)).Count.ShouldBeNull();
        }
        fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(valid, Options)));
        (await client.GetActiveConversationCountAsync(query, TestContext.Current.CancellationToken)).Count.ShouldBe(2);
    }

    /// <summary>Verifies deletion lookup rejects foreign source revision version and acknowledgement.</summary>
    [Fact]
    public async Task DeletionLookupRejectsForeignSourceRevisionVersionAndAcknowledgement()
    {
        using var fixture = new ConversationAgentHttpFixture();
        using var http = new HttpClient(fixture) { BaseAddress = new("https://local.invalid/") };
        var client = new ConversationClient(http);
        var query = new ConversationDeletionSourceQuery(Tenant, Conversation, 4, "signal");
        var signal = new ConversationDeletionSignal("signal", Tenant, Conversation, "tenant-alpha:conversation:conversation-alpha", 4, "independent-approval");
        var valid = new ConversationDeletionSourceResult(ConversationAgentsOutcome.Available, signal);
        foreach (var wrong in new[] { valid with { Signal = signal with { TenantId = new("foreign") } },
            valid with { Signal = signal with { SourceRevision = 5 } }, valid with { Signal = signal with { SourceContractVersion = 2 } },
            valid with { AcknowledgedSourceRevision = 4, Acknowledgement = new("wrong", 4, 8, "target", "authenticated-receipt") } })
        {
            fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(wrong, Options)));
            (await client.GetConversationDeletionSourceAsync(query, TestContext.Current.CancellationToken)).Signal.ShouldBeNull();
        }
        fixture.Enqueue(new SubmitQueryResponse("correlation", JsonSerializer.SerializeToElement(valid, Options)));
        (await client.GetConversationDeletionSourceAsync(query, TestContext.Current.CancellationToken)).Signal.ShouldBe(signal);
    }

    /// <summary>Verifies cancellation ends a blocked send and disposes its eventual response without releasing a result.</summary>
    /// <param name="command">Whether to exercise command submission instead of a protected query.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NeverCompletingHttpSendsRespectCallerCancellationAndDisposeLateResponse(bool command)
    {
        using var fixture = new NonCooperativeAgentHttpFixture();
        using var http = new HttpClient(fixture) { BaseAddress = new("https://local.invalid/") };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var client = new ConversationClient(http);
        Task pending = StartAgentOperation(client, command, cancellation.Token);
        await fixture.SendStarted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        pending.IsCompleted.ShouldBeFalse();
        cancellation.Cancel();
        var exception = await Should.ThrowAsync<OperationCanceledException>(
            () => pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        exception.CancellationToken.ShouldBe(cancellation.Token);
        pending.IsCanceled.ShouldBeTrue();

        using var body = new NonCooperativeAgentResponseStream();
        fixture.Complete(new(System.Net.HttpStatusCode.OK) { Content = new StreamContent(body) });
        await body.Disposed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        pending.IsCanceled.ShouldBeTrue();
    }

    /// <summary>Verifies cancellation ends a JSON read that ignores its token and disposes the owned response body.</summary>
    /// <param name="command">Whether to exercise command submission instead of a protected query.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NeverCompletingJsonBodiesRespectCallerCancellationAndDisposeOwnedResponse(bool command)
    {
        using var fixture = new ConversationAgentHttpFixture();
        using var body = new NonCooperativeAgentResponseStream();
        fixture.Responses.Enqueue(new(System.Net.HttpStatusCode.OK) { Content = new StreamContent(body) });
        using var http = new HttpClient(fixture) { BaseAddress = new("https://local.invalid/") };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var client = new ConversationClient(http);
        Task pending = StartAgentOperation(client, command, cancellation.Token);
        try
        {
            await body.ReadStarted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            pending.IsCompleted.ShouldBeFalse();
            cancellation.Cancel();
            var exception = await Should.ThrowAsync<OperationCanceledException>(
                () => pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            exception.CancellationToken.ShouldBe(cancellation.Token);
            pending.IsCanceled.ShouldBeTrue();
            await body.Disposed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            body.CompleteRead();
        }
        pending.IsCanceled.ShouldBeTrue();
    }

    private static Task StartAgentOperation(ConversationClient client, bool command, CancellationToken cancellationToken)
        => command
            ? client.PostAgentMessageAsync(new AppendMessageCommand(
                new(SchemaVersion.Current, Tenant, Agent, "correlation", IdempotencyKey: "post-key"),
                Conversation, Message, Agent, "text", AgentProvenance: new("agent-call", true, false), OperationTimestamp: At), cancellationToken)
            : client.GetAgentConversationAsync(new(Tenant, Conversation, Message), cancellationToken);

    private static ConversationAgentReadResult Current()
        => new(ConversationAgentsOutcome.Available, Tenant, Conversation, 3, At, "checkpoint",
            [new(Agent, ParticipantType.AiAgent, ParticipantRole.Member)],
            [new(Message, Agent, "safe visible text", At, null, false, false, new("trace", true, false))], true);
}
