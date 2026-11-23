using Hexalith.Conversations.Server.Agents;
using System.Text.Json;
using ByteAether.Ulid;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Api;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Api;

/// <summary>Translates the ordinary v1 command contracts into admitted EventStore commands.</summary>
internal static class PilotCommands
{
    internal static async Task<IResult> CreateAsync(HttpContext context, IEventStoreGatewayClient gateway, CancellationToken token)
    {
        var command = await ConversationCommandApi.ReadBodyAsync<CreateConversationCommand>(context, token);
        string correlation = ConversationCommandApi.SafeErrorCorrelationId(context, command?.Metadata?.CorrelationId);
        if (Validate(context, command?.Metadata, correlation, out string subject) is { } invalid) return invalid;
        var metadata = command!.Metadata;
        var id = new ConversationId(PilotCommandIdentity.Allocate(metadata, subject, "conversation"));
        var create = new CreateHumanConversation(command, id, DateTimeOffset.UtcNow, PilotCommandIdentity.Allocate(metadata, subject, "created-event"));
        var failure = await SubmitAsync(gateway, metadata, id, create, correlation, token);
        return failure ?? Results.Json(new ConversationCreatedResult(SchemaVersion.Current, metadata.TenantId, id,
            metadata.CorrelationId, metadata.IdempotencyKey, new(ProjectionTrustState.Rebuilding), ConversationCommandType.CreateConversationCommand),
            statusCode: StatusCodes.Status201Created);
    }

    internal static async Task<IResult> AppendAsync(string conversationId, HttpContext context, IEventStoreGatewayClient gateway, CancellationToken token)
    {
        var command = await ConversationCommandApi.ReadBodyAsync<AppendMessageCommand>(context, token);
        string correlation = ConversationCommandApi.SafeErrorCorrelationId(context, command?.Metadata?.CorrelationId);
        if (Validate(context, command?.Metadata, correlation, out string subject) is { } invalid) return invalid;
        var metadata = command!.Metadata;
        if (command.ConversationId?.Value != conversationId || command.MessageId is null
            || string.IsNullOrWhiteSpace(command.Text) || command.AgentProvenance is not null)
            return Failure(correlation, 400, ConversationErrorCode.CommandValidationFailed);
        if (command.AuthorPartyId != metadata.ActorPartyId)
            return Failure(correlation, 403, ConversationErrorCode.TenantIsolationViolation);
        var append = new AppendMessage(command, DateTimeOffset.UtcNow,
            PilotCommandIdentity.Allocate(metadata, subject, "message-event", conversationId));
        var failure = await SubmitAsync(gateway, metadata, command.ConversationId, append, correlation, token);
        return failure ?? Results.Json(new ConversationCommandAcceptedResult(SchemaVersion.Current, metadata.TenantId,
            command.ConversationId, ConversationCommandType.AppendMessageCommand, metadata.CorrelationId,
            metadata.IdempotencyKey, new(ProjectionTrustState.Rebuilding)), statusCode: StatusCodes.Status202Accepted);
    }

    private static IResult? Validate(HttpContext context, ConversationCommandMetadata? metadata, string correlation, out string subject)
    {
        subject = string.Empty;
        if (metadata is null) return Failure(correlation, 400, ConversationErrorCode.CommandValidationFailed);
        if (!ConversationReadApi.TryGetTenantCaller(context, out var tenant, out var principal, out _)
            || tenant != metadata.TenantId || !ConversationReadApi.ScopeHeadersMatch(context, tenant!, principal!)
            || !ConversationReadApi.HeaderMatches(context, "X-Actor-Party-Id", metadata.ActorPartyId.Value))
            return Failure(correlation, 403, ConversationErrorCode.TenantContextMismatch);
        subject = principal!;
        if (metadata.SchemaVersion != SchemaVersion.Current)
            return Failure(correlation, 400, ConversationErrorCode.SchemaVersionUnsupported);
        if (string.IsNullOrWhiteSpace(metadata.IdempotencyKey))
            return Failure(correlation, 400, ConversationErrorCode.IdempotencyKeyMissing);
        if (metadata.IdempotencyKey.Length > 200 || metadata.IdempotencyKey.Any(char.IsControl)
            || !ConversationReadApi.HeaderMatches(context, "Idempotency-Key", metadata.IdempotencyKey))
            return Failure(correlation, 400, ConversationErrorCode.CommandValidationFailed);
        return null;
    }

    private static async Task<IResult?> SubmitAsync<T>(IEventStoreGatewayClient gateway, ConversationCommandMetadata metadata,
        ConversationId conversation, T payload, string correlation, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        bool received = false;
        try
        {
            // The gateway selects the original execution on replay using the semantic adapter and key.
            var request = new SubmitCommandRequest(Ulid.New().ToString(), metadata.TenantId.Value, "conversation", conversation.Value,
                typeof(T).Name, JsonSerializer.SerializeToElement(payload, JsonSerializerOptions.Web), metadata.CorrelationId,
                IdempotencyKey: metadata.IdempotencyKey);
            var receipt = await gateway.SubmitCommandAsync(request, deadline.Token);
            received = true;
            int pollDelayMs = 100;
            if (!string.IsNullOrWhiteSpace(receipt.MessageId))
            {
                while (true)
                {
                    var state = await gateway.GetCommandStatusAsync(receipt.MessageId, deadline.Token);
                    if (state is not null)
                    {
                        if (state.MessageId != receipt.MessageId || state.TenantId != request.Tenant
                            || state.Domain != request.Domain || state.AggregateId != request.AggregateId) break;
                        if (state.Status == nameof(CommandStatus.Completed) && state.StatusCode == (int)CommandStatus.Completed) return null;
                        if (state.IsRejected) return Rejection(correlation, state.RejectionEventType);
                        if (state.Status is nameof(CommandStatus.PublishFailed) or nameof(CommandStatus.Rejected) or nameof(CommandStatus.TimedOut)) break;
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(pollDelayMs), deadline.Token);
                    pollDelayMs = Math.Min(2000, pollDelayMs * 2);
                }
            }
        }
        catch (EventStoreGatewayException ex) when (!received)
        {
            if (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
                return ConversationCommandApi.ErrorResult(ConversationErrorCode.CommandValidationFailed, correlation,
                    StatusCodes.Status413PayloadTooLarge, "Reduce the command payload size before sending it again.");
            if (ex.Extensions.TryGetValue("rejectionType", out var kind) && kind.ValueKind == JsonValueKind.String)
                return Rejection(correlation, kind.GetString());
            return ex.StatusCode switch
            {
                400 or 422 => Failure(correlation, 400, ConversationErrorCode.CommandValidationFailed),
                401 or 403 => Failure(correlation, ex.StatusCode, ConversationErrorCode.TenantIsolationViolation),
                409 when ex.Code == "idempotency_conflict" || ex.ReasonCode == "idempotency_conflict"
                    => Failure(correlation, 409, ConversationErrorCode.IdempotencyConflict),
                409 when ex.Code == "idempotency_key_expired" || ex.ReasonCode == "idempotency_key_expired"
                    => ExpiredReceipt(correlation),
                _ => Failure(correlation, 503, ConversationErrorCode.IdempotencyOutcomeUnknown),
            };
        }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            return Failure(correlation, 503, ConversationErrorCode.IdempotencyOutcomeUnknown);
        }
        return Failure(correlation, 503, ConversationErrorCode.IdempotencyOutcomeUnknown);
    }

    private static IResult ExpiredReceipt(string correlation)
    {
        const string guidance = "The original receipt expired. Reconcile the original conversation and message before issuing another mutation.";
        var descriptor = ConversationErrorCatalog.Get(ConversationErrorCode.IdempotencyOutcomeUnknown);
        var error = new ConversationError(SchemaVersion.Current, descriptor.Code, descriptor.Category, IsRetryable: false,
            correlation, Documentation: descriptor.Documentation, DeveloperGuidance: guidance, SafeMessage: guidance);
        return Results.Json(new ConversationErrorResult([error]), statusCode: StatusCodes.Status409Conflict);
    }

    private static IResult Rejection(string correlation, string? kind)
        => kind?.Split(',')[0].Split('.').Last() switch
        {
            nameof(ConversationWriteDenied) => Failure(correlation, 403, ConversationErrorCode.TenantIsolationViolation),
            nameof(ConversationMessageConflict) => Failure(correlation, 409, ConversationErrorCode.IdempotencyConflict),
            nameof(ConversationRejectedDomainEvent) => Failure(correlation, 400, ConversationErrorCode.CommandValidationFailed),
            _ => Failure(correlation, 503, ConversationErrorCode.IdempotencyOutcomeUnknown),
        };

    private static IResult Failure(string correlation, int status, ConversationErrorCode code)
        => ConversationCommandApi.ErrorResult(code, correlation, status);
}
