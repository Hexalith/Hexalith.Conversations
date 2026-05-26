// <copyright file="IntegrationGuideWorkflowExampleTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Diagnostics;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Documentation;

/// <summary>
/// Compiles and exercises the adopter workflow documented by the integration guide.
/// </summary>
public sealed class IntegrationGuideWorkflowExampleTest
{
    [Fact]
    public async Task DocumentedClientWorkflowShouldCompileAgainstTheSupportedSurface()
    {
        IServiceCollection services = new ServiceCollection();
        IHttpClientBuilder builder = services.AddHexalithConversationsClient(options =>
        {
            options.Endpoint = new Uri("https://docs.hexalith.local/conversations/api/");
        });

        builder.ShouldNotBeNull();

        RecordingConversationClient client = new();
        ConversationClientContext context = new(
            new TenantId("adopter-tenancy"),
            new PartyId("actor-party"),
            "caller-principal",
            "correlation-workflow",
            IdempotencyKey: "idempotency-workflow");

        CreateConversationCommand create = new(
            context.ToCommandMetadata(),
            new BusinessReference("support", "record-key"),
            Label: "Support conversation");

        ConversationClientResult<ConversationCreatedResult> created =
            await client.CreateConversationAsync(create, TestContext.Current.CancellationToken);

        created.IsSuccess.ShouldBeTrue();
        created.Value!.TenantId.ShouldBe(context.TenantId);
        created.Value.IdempotencyKey.ShouldBe(context.IdempotencyKey);
        client.LastCreate.ShouldBe(create);

        AppendMessageCommand append = new(
            context.ToCommandMetadata(),
            created.Value.ConversationId,
            new MessageId("message-workflow"),
            context.ActorPartyId,
            "Message text approved for Conversations content handling.");

        ConversationClientResult<ConversationCommandAcceptedResult> appended =
            await client.AppendMessageAsync(append, TestContext.Current.CancellationToken);

        appended.IsSuccess.ShouldBeTrue();
        appended.Value!.CommandType.ShouldBe(ConversationCommandType.AppendMessageCommand);
        client.LastAppend.ShouldBe(append);

        GetConversationQuery query = context.ToGetConversationQuery(created.Value.ConversationId);
        ConversationClientResult<ConversationDetailResult> detail =
            await client.GetConversationAsync(query, TestContext.Current.CancellationToken);

        detail.IsSuccess.ShouldBeTrue();
        detail.Value!.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        detail.Value.Details.ShouldNotBeNull();
        detail.Value.Details.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
        detail.Value.Details.Messages.ShouldContain(message => message.MessageId == append.MessageId);
        client.LastQuery.ShouldBe(query);

        ContractCompatibilityMetadata active = ConversationContractCompatibility.Current;
        ContractCompatibilityResult compatibility = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(
                CommandSchemaVersion: active.CommandContracts.ActiveSchemaVersion.Value.ToString(),
                ProjectionSchemaVersion: active.ProjectionContracts.ActiveSchemaVersion.Value.ToString(),
                EventSchemaVersion: active.EventContracts.ActiveSchemaVersion.Value.ToString(),
                ContractsPackageVersion: active.ContractsPackage.Version,
                ClientPackageVersion: active.ClientPackage.Version));

        compatibility.Status.ShouldBe(ContractCompatibilityStatus.Supported);
        ConversationCorePreconditionCatalog.All.ShouldContain(
            precondition => precondition.RequiredTrustState == ProjectionTrustState.Current);
    }

    [Fact]
    public void DocumentedTypedErrorBranchesShouldCoverCriticalRetryGuidance()
    {
        ConversationClientResult<ConversationCreatedResult> conflict = Failure(ConversationErrorCode.IdempotencyConflict);
        ConversationClientResult<ConversationCreatedResult> unknown = Failure(ConversationErrorCode.IdempotencyOutcomeUnknown);
        ConversationClientResult<ConversationCreatedResult> stale = Failure(ConversationErrorCode.TenantProjectionStale);

        NextClientAction(conflict).ShouldBe(ConversationErrorClientAction.UseNewIdempotencyKey);
        NextClientAction(unknown).ShouldBe(ConversationErrorClientAction.RetrySameRequest);
        NextClientAction(stale).ShouldBe(ConversationErrorClientAction.RetryLater);

        conflict.Error!.Errors.Single().IsRetryable.ShouldBeFalse();
        unknown.Error!.Errors.Single().IsRetryable.ShouldBeTrue();
        stale.Error!.Errors.Single().Category.ShouldBe(ConversationErrorCategory.Freshness);
    }

    private static ConversationClientResult<ConversationCreatedResult> Failure(ConversationErrorCode code)
        => ConversationClientResult<ConversationCreatedResult>.Failure(
            new ConversationErrorResult([ConversationErrorCatalog.CreateError(code, "correlation-workflow")]),
            HttpStatusCode.BadRequest);

    private static ConversationErrorClientAction NextClientAction(ConversationClientResult<ConversationCreatedResult> result)
    {
        result.IsSuccess.ShouldBeFalse();
        ConversationError error = result.Error!.Errors.Single();

        if (error.Code == ConversationErrorCode.IdempotencyConflict)
        {
            return ConversationErrorClientAction.UseNewIdempotencyKey;
        }

        if (error.Code == ConversationErrorCode.IdempotencyOutcomeUnknown)
        {
            return ConversationErrorClientAction.RetrySameRequest;
        }

        if (error.Category == ConversationErrorCategory.Freshness)
        {
            return ConversationErrorClientAction.RetryLater;
        }

        return error.ClientAction ?? throw new InvalidOperationException("Typed errors must carry a client action.");
    }

    private sealed class RecordingConversationClient : IConversationClient
    {
        public CreateConversationCommand? LastCreate { get; private set; }

        public AppendMessageCommand? LastAppend { get; private set; }

        public GetConversationQuery? LastQuery { get; private set; }

        public Task<ConversationClientResult<ConversationCreatedResult>> CreateConversationAsync(
            CreateConversationCommand command,
            CancellationToken cancellationToken = default)
        {
            LastCreate = command;
            ConversationCreatedResult result = new(
                SchemaVersion.Current,
                command.Metadata.TenantId,
                new ConversationId("conversation-workflow"),
                command.Metadata.CorrelationId,
                command.Metadata.IdempotencyKey,
                new ReadModelVisibility(ProjectionTrustState.Current),
                ConversationCommandType.CreateConversationCommand);

            return Task.FromResult(ConversationClientResult<ConversationCreatedResult>.Success(result, HttpStatusCode.Accepted));
        }

        public Task<ConversationClientResult<ConversationCommandAcceptedResult>> AppendMessageAsync(
            AppendMessageCommand command,
            CancellationToken cancellationToken = default)
        {
            LastAppend = command;
            ConversationCommandAcceptedResult result = new(
                SchemaVersion.Current,
                command.Metadata.TenantId,
                command.ConversationId,
                ConversationCommandType.AppendMessageCommand,
                command.Metadata.CorrelationId,
                command.Metadata.IdempotencyKey,
                new ReadModelVisibility(ProjectionTrustState.Current));

            return Task.FromResult(ConversationClientResult<ConversationCommandAcceptedResult>.Success(result, HttpStatusCode.Accepted));
        }

        public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(
            GetConversationQuery query,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            DateTimeOffset timestamp = new(2026, 05, 23, 0, 0, 0, TimeSpan.Zero);
            ProjectionFreshnessV1 freshness = new(
                SchemaVersion.Current,
                "cursor-workflow",
                1,
                timestamp,
                timestamp.AddSeconds(1),
                TimeSpan.FromSeconds(1),
                false,
                ProjectionTrustState.Current,
                ProjectionFreshnessReasonCode.Current);

            ConversationDetailsV1 details = new(
                SchemaVersion.Current,
                query.TenantId,
                query.ConversationId,
                freshness,
                "Open",
                Messages:
                [
                    new(
                        new MessageId("message-workflow"),
                        new PartyId("actor-party"),
                        "Message text approved for Conversations content handling.",
                        timestamp),
                ]);

            return Task.FromResult(ConversationClientResult<ConversationDetailResult>.Success(
                ConversationDetailResult.Visible(SchemaVersion.Current, details, "Use the current timeline."),
                HttpStatusCode.OK));
        }

        public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(
            ListConversationsQuery query,
            CancellationToken cancellationToken = default)
        {
            ConversationListResult result = new(
                SchemaVersion.Current,
                ProjectionTrustState.Current,
                ProjectionFreshnessReasonCode.Current,
                [],
                new ConversationPageMetadata(0),
                "No accessible matches.");

            return Task.FromResult(ConversationClientResult<ConversationListResult>.Success(result, HttpStatusCode.OK));
        }
    }
}
