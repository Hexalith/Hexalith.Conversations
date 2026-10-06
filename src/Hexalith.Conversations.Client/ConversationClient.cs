// <copyright file="ConversationClient.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Client;

/// <summary>
/// Thin supported HTTP client over the Conversations v1 contract package.
/// </summary>
public sealed class ConversationClient : IConversationClient
{
    /// <summary>Correlation identity header.</summary>
    internal const string CorrelationIdHeaderName = "X-Correlation-Id";
    /// <summary>Causation identity header.</summary>
    internal const string CausationIdHeaderName = "X-Causation-Id";
    /// <summary>Opaque idempotency identity header.</summary>
    internal const string IdempotencyKeyHeaderName = "Idempotency-Key";
    /// <summary>Exact tenant identity header.</summary>
    internal const string TenantIdHeaderName = "X-Tenant-Id";
    /// <summary>Actor Party attribution header, never authority.</summary>
    internal const string ActorPartyIdHeaderName = "X-Actor-Party-Id";
    /// <summary>Caller principal attribution header, never authority.</summary>
    internal const string CallerPrincipalIdHeaderName = "X-Caller-Principal-Id";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationClient"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    public ConversationClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationCreatedResult>> CreateConversationAsync(
        CreateConversationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        ConversationErrorResult? compatibilityError = ValidateCommandSchema(metadata);
        if (compatibilityError is not null)
        {
            return Task.FromResult(ConversationClientResult<ConversationCreatedResult>.Failure(compatibilityError));
        }

        HttpRequestMessage request = CreateJsonRequest(HttpMethod.Post, "api/v1/conversations", command);
        AddCommandHeaders(request, metadata);
        return SendAsync<ConversationCreatedResult>(request, metadata.CorrelationId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationCommandAcceptedResult>> AppendMessageAsync(
        AppendMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        ConversationErrorResult? compatibilityError = ValidateCommandSchema(metadata);
        if (compatibilityError is not null)
        {
            return Task.FromResult(ConversationClientResult<ConversationCommandAcceptedResult>.Failure(compatibilityError));
        }

        string route = $"api/v1/conversations/{Uri.EscapeDataString(command.ConversationId.Value)}/messages";
        HttpRequestMessage request = CreateJsonRequest(HttpMethod.Post, route, command);
        AddCommandHeaders(request, metadata);
        return SendAsync<ConversationCommandAcceptedResult>(request, metadata.CorrelationId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(
        ReassignConversationProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        ConversationErrorResult? compatibilityError = ValidateCommandSchema(metadata);
        if (compatibilityError is not null)
        {
            return Task.FromResult(ConversationClientResult<ConversationCommandAcceptedResult>.Failure(compatibilityError));
        }

        string route = $"api/v1/conversations/{Uri.EscapeDataString(command.ConversationId.Value)}/project";
        HttpRequestMessage request = CreateJsonRequest(HttpMethod.Post, route, command);
        AddCommandHeaders(request, metadata);
        return SendAsync<ConversationCommandAcceptedResult>(request, metadata.CorrelationId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(
        GetConversationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        string route = $"api/v1/conversations/{Uri.EscapeDataString(query.ConversationId.Value)}";
        HttpRequestMessage request = new(HttpMethod.Get, route);
        AddHeader(request, CorrelationIdHeaderName, query.CorrelationId);
        AddHeader(request, TenantIdHeaderName, query.TenantId.Value);
        AddHeader(request, CallerPrincipalIdHeaderName, query.CallerPrincipalId);
        return SendAsync<ConversationDetailResult>(request, query.CorrelationId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(
        ListConversationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        HttpRequestMessage request = new(HttpMethod.Get, BuildListRoute(query));
        AddHeader(request, CorrelationIdHeaderName, query.CorrelationId);
        AddHeader(request, TenantIdHeaderName, query.TenantId.Value);
        AddHeader(request, CallerPrincipalIdHeaderName, query.CallerPrincipalId);
        return SendAsync<ConversationListResult>(request, query.CorrelationId, cancellationToken);
    }

    private static ConversationCommandMetadata RequireMetadata(ConversationCommandMetadata? metadata)
        => metadata ?? throw new ArgumentException("Command metadata is required.", nameof(metadata));

    private static HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string route, T body)
        => new(method, route)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };

    private static void AddCommandHeaders(HttpRequestMessage request, ConversationCommandMetadata metadata)
    {
        AddHeader(request, CorrelationIdHeaderName, metadata.CorrelationId);
        AddHeader(request, CausationIdHeaderName, metadata.CausationId);
        AddHeader(request, IdempotencyKeyHeaderName, metadata.IdempotencyKey);
        AddHeader(request, TenantIdHeaderName, metadata.TenantId.Value);
        AddHeader(request, ActorPartyIdHeaderName, metadata.ActorPartyId.Value);
    }

    private static void AddHeader(HttpRequestMessage request, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static string BuildListRoute(ListConversationsQuery query)
    {
        List<string> parameters = [];
        AddQuery(parameters, "businessSystem", query.Filter.BusinessReference?.System);
        AddQuery(parameters, "businessValue", query.Filter.BusinessReference?.Value);
        AddQuery(parameters, "projectId", query.Filter.ProjectId?.Value);
        AddQuery(parameters, "folderId", query.Filter.FolderId?.Value);
        AddQuery(parameters, "lifecycleState", query.Filter.LifecycleState);
        AddQuery(parameters, "projectedAtFrom", FormatDate(query.Filter.ProjectedAtFrom));
        AddQuery(parameters, "projectedAtTo", FormatDate(query.Filter.ProjectedAtTo));
        AddQuery(parameters, "recentActivityAfter", FormatDate(query.Filter.RecentActivityAfter));
        AddQuery(parameters, "participantPartyId", query.Filter.ParticipantPartyId?.Value);
        AddQuery(parameters, "redactionState", query.Filter.RedactionState?.Value);
        AddQuery(parameters, "freshnessState", query.Filter.FreshnessState?.Value);
        AddQuery(parameters, "auditReadiness", query.Filter.AuditReadiness?.Value);
        AddQuery(parameters, "verificationState", query.Filter.VerificationState?.Value);
        AddQuery(parameters, "pageSize", query.Page.PageSize.ToString(CultureInfo.InvariantCulture));
        AddQuery(parameters, "cursor", query.Page.ContinuationCursor);

        return parameters.Count == 0
            ? "api/v1/conversations"
            : "api/v1/conversations?" + string.Join("&", parameters);
    }

    private static void AddQuery(List<string> parameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parameters.Add(Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value));
    }

    private static string? FormatDate(DateTimeOffset? value)
        => value?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static ConversationErrorResult? ValidateCommandSchema(ConversationCommandMetadata metadata)
    {
        ContractCompatibilityResult compatibility = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(
                CommandSchemaVersion: metadata.SchemaVersion.Value.ToString(CultureInfo.InvariantCulture),
                ContractsPackageVersion: ConversationContractCompatibility.Current.ContractsPackage.Version,
                ClientPackageVersion: ConversationContractCompatibility.Current.ClientPackage.Version));

        return compatibility.Error is null
            ? null
            : new ConversationErrorResult(
                [
                    ConversationErrorCatalog.CreateError(
                        compatibility.Error.Code,
                        metadata.CorrelationId,
                        safeFieldDiagnostics: compatibility.Error.SafeFieldDiagnostics,
                        developerGuidance: compatibility.Error.DeveloperGuidance),
                ]);
    }

    private async Task<ConversationClientResult<T>> SendAsync<T>(
        HttpRequestMessage request,
        string correlationId,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            T? value = await TryReadAsync<T>(response.Content, cancellationToken).ConfigureAwait(false);
            if (value is not null && (response.IsSuccessStatusCode || IsReadResult<T>()))
            {
                return ConversationClientResult<T>.Success(value, response.StatusCode);
            }

            if (response.IsSuccessStatusCode)
            {
                return ConversationClientResult<T>.Failure(
                    FallbackError(
                        ConversationErrorCode.CommandValidationFailed,
                        correlationId,
                        "The response body did not match the supported Conversations contract."),
                    response.StatusCode);
            }

            ConversationErrorResult error = await TryReadErrorAsync(response.Content, cancellationToken).ConfigureAwait(false)
                ?? FallbackForStatus(response.StatusCode, correlationId);
            return ConversationClientResult<T>.Failure(error, response.StatusCode);
        }
        catch (Exception ex) when (IsUnknownOutcomeException(ex, cancellationToken))
        {
            return ConversationClientResult<T>.Failure(UnknownOutcome(correlationId));
        }
    }

    private static async Task<T?> TryReadAsync<T>(HttpContent content, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    private static async Task<ConversationErrorResult?> TryReadErrorAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        try
        {
            using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<ConversationErrorResult>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    private static ConversationErrorResult FallbackForStatus(HttpStatusCode statusCode, string correlationId)
        => statusCode switch
        {
            HttpStatusCode.BadRequest => FallbackError(
                ConversationErrorCode.CommandValidationFailed,
                correlationId,
                "Correct the request and retry."),
            HttpStatusCode.Conflict => FallbackError(
                ConversationErrorCode.IdempotencyConflict,
                correlationId,
                "Use a new idempotency key for a changed command payload."),
            HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => FallbackError(
                ConversationErrorCode.TenantIsolationViolation,
                correlationId,
                "Check tenant access and caller authorization."),
            HttpStatusCode.NotFound => FallbackError(
                ConversationErrorCode.AggregateNotFound,
                correlationId,
                "The requested conversation is not available."),
            _ => UnknownOutcome(correlationId),
        };

    private static ConversationErrorResult UnknownOutcome(string correlationId)
        => FallbackError(
            ConversationErrorCode.IdempotencyOutcomeUnknown,
            correlationId,
            "Retry with the same idempotency metadata when the command outcome is unknown.");

    private static ConversationErrorResult FallbackError(
        ConversationErrorCode code,
        string correlationId,
        string developerGuidance)
        => new(
            [
                ConversationErrorCatalog.CreateError(
                    code,
                    correlationId,
                    developerGuidance: developerGuidance),
            ]);

    private static bool IsUnknownOutcomeException(Exception exception, CancellationToken cancellationToken)
        => !cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or TaskCanceledException or TimeoutException or IOException;

    private static bool IsReadResult<T>()
        => typeof(T) == typeof(ConversationDetailResult)
            || typeof(T) == typeof(ConversationListResult);

    /// <inheritdoc />
    public Task<ConversationAgentCommandResult> AddParticipantAsync(AddParticipantCommand request, CancellationToken cancellationToken = default)
        => request.OperationTimestamp is { } occurrence ? SubmitAgentCommandAsync(request.Metadata, request.ConversationId,
            "AddAgentParticipant", new
            {
                PublicCommand = request,
                AddedAt = occurrence,
                EventId = IntentId(request.Metadata, request.ConversationId, "membership")
            },
            null, cancellationToken) : Task.FromResult(new ConversationAgentCommandResult(ConversationAgentsOutcome.Invalid));

    /// <inheritdoc />
    public Task<ConversationAgentCommandResult> RemoveAgentParticipantAsync(RemoveAgentParticipantCommand request, CancellationToken cancellationToken = default)
        => SubmitAgentCommandAsync(request.Metadata, request.ConversationId, "RemoveAgentParticipant",
            new
            {
                PublicCommand = request,
                EventId = IntentId(request.Metadata, request.ConversationId, "removal")
            }, null, cancellationToken);

    /// <inheritdoc />
    public Task<ConversationAgentCommandResult> PostAgentMessageAsync(AppendMessageCommand request, CancellationToken cancellationToken = default)
        => request.OperationTimestamp is { } occurrence ? SubmitAgentCommandAsync(request.Metadata, request.ConversationId,
            "AppendAgentMessage", new
            {
                PublicCommand = request,
                PostedAt = occurrence,
                EventId = IntentId(request.Metadata, request.ConversationId, "posting")
            },
            request.MessageId, cancellationToken) : Task.FromResult(new ConversationAgentCommandResult(ConversationAgentsOutcome.Invalid));

    /// <inheritdoc />
    public Task<ConversationAgentReadResult> GetAgentConversationAsync(ConversationAgentReadQuery request, CancellationToken cancellationToken = default)
        => SubmitAgentQueryAsync(request.TenantId, request.ConversationId.Value, "conversation-agent-read", request,
            new ConversationAgentReadResult(ConversationAgentsOutcome.Unavailable, request.TenantId, request.ConversationId), cancellationToken);

    /// <inheritdoc />
    public Task<ConversationActiveCountResult> GetActiveConversationCountAsync(ConversationActiveCountQuery request, CancellationToken cancellationToken = default)
        => SubmitAgentQueryAsync(request.TenantId, request.TenantId.Value, "conversation-active-count", request,
            new ConversationActiveCountResult(ConversationAgentsOutcome.Unavailable), cancellationToken);

    /// <inheritdoc />
    public Task<ConversationAgentCommandResult> ApproveConversationDeletionAsync(ApproveConversationDeletionCommand request, CancellationToken cancellationToken = default)
        => SubmitAgentCommandAsync(request.Metadata, request.ConversationId, "ApproveConversationDeletion",
            new
            {
                PublicCommand = request,
                EventId = IntentId(request.Metadata, request.ConversationId, "approval")
            }, null, cancellationToken);

    /// <inheritdoc />
    public Task<ConversationAgentCommandResult> RecordConversationDeletionDeliveryAsync(RecordConversationDeletionDeliveryCommand request, CancellationToken cancellationToken = default)
        => SubmitAgentCommandAsync(request.Metadata, request.ConversationId, "RecordConversationDeletionDelivery",
            new
            {
                PublicCommand = request,
                EventId = IntentId(request.Metadata, request.ConversationId, "delivery")
            }, null, cancellationToken);

    /// <inheritdoc />
    public Task<ConversationDeletionSourceResult> GetConversationDeletionSourceAsync(ConversationDeletionSourceQuery request, CancellationToken cancellationToken = default)
        => SubmitAgentQueryAsync(request.TenantId, request.ConversationId.Value, "conversation-deletion-source", request,
            new ConversationDeletionSourceResult(ConversationAgentsOutcome.Unavailable), cancellationToken);

    private async Task<ConversationAgentCommandResult> SubmitAgentCommandAsync<T>(ConversationCommandMetadata metadata,
        ConversationId conversation, string commandType, T intent, MessageId? requestedMessageId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(metadata.IdempotencyKey) || metadata.SchemaVersion.Value != 1)
        {
            return new(ConversationAgentsOutcome.Invalid);
        }
        var command = new SubmitCommandRequest(IntentId(metadata, conversation, commandType), metadata.TenantId.Value,
            "conversation", conversation.Value, commandType, JsonSerializer.SerializeToElement(intent, JsonOptions),
            metadata.CorrelationId, IdempotencyKey: metadata.IdempotencyKey);
        try
        {
            using var request = CreateJsonRequest(HttpMethod.Post, "api/v1/commands", command);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!response.IsSuccessStatusCode)
            {
                return new(AgentHttpOutcome(response.StatusCode));
            }
            var result = await response.Content.ReadFromJsonAsync<SubmitCommandResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(result?.MessageId))
            {
                return new(ConversationAgentsOutcome.Unavailable);
            }
            if (result.ResultPayload is { } payload)
            {
                if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("outcome", out _))
                {
                    return new(ConversationAgentsOutcome.Unavailable);
                }
                var outcome = payload.Deserialize<ConversationAgentCommandResult>(JsonOptions);
                if (outcome is null || !Enum.IsDefined(outcome.Outcome)
                    || (requestedMessageId is not null && (outcome.MessageId is not null && outcome.MessageId != requestedMessageId
                        || outcome.Outcome == ConversationAgentsOutcome.Available && outcome.MessageId != requestedMessageId)))
                {
                    return new(ConversationAgentsOutcome.Conflict, result.MessageId);
                }
                // A response cannot independently establish persisted completion; current source lookup does.
                return outcome with
                {
                    CommandMessageId = result.MessageId,
                    Persisted = false
                };
            }
            return new(ConversationAgentsOutcome.Available, result.MessageId, requestedMessageId);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException or IOException)
        {
            return new(ConversationAgentsOutcome.Unavailable);
        }
    }

    private async Task<TResult> SubmitAgentQueryAsync<TRequest, TResult>(TenantId tenant, string aggregate,
        string queryType, TRequest payload, TResult unavailable, CancellationToken cancellationToken) where TResult : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = new SubmitQueryRequest(tenant.Value, "conversation", aggregate, queryType,
            Payload: JsonSerializer.SerializeToElement(payload, JsonOptions))
        {
            Freshness = new(RequireFresh: true)
        };
        try
        {
            using var request = CreateJsonRequest(HttpMethod.Post, "api/v1/queries", query);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!response.IsSuccessStatusCode)
            {
                return unavailable;
            }
            var result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (result is not { Success: true } || result.Metadata is { IsStale: true } or { IsDegraded: true }
                || result.Metadata?.Paging is { HasMore: true } || result.Metadata?.Paging?.NextCursor is not null)
            {
                return unavailable;
            }
            if (result.Payload.ValueKind != JsonValueKind.Object || !result.Payload.TryGetProperty("outcome", out _))
            {
                return unavailable;
            }
            if (payload is ConversationAgentReadQuery or ConversationActiveCountQuery
                && !result.Payload.TryGetProperty("sourceContractVersion", out _))
            {
                return unavailable;
            }
            if (payload is ConversationDeletionSourceQuery && result.Payload.TryGetProperty("signal", out var signal)
                && signal.ValueKind == JsonValueKind.Object && !signal.TryGetProperty("sourceContractVersion", out _))
            {
                return unavailable;
            }
            TResult? value = result.Payload.Deserialize<TResult>(JsonOptions);
            return value is not null && ValidAgentQueryResult(payload, value) ? value : unavailable;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException or IOException)
        {
            return unavailable;
        }
    }

    private static bool ValidAgentQueryResult<TRequest, TResult>(TRequest query, TResult value)
    {
        switch (query, value)
        {
            case (ConversationAgentReadQuery request, ConversationAgentReadResult result):
                if (!Enum.IsDefined(result.Outcome) || result.SourceContractVersion != 1 || result.TenantId != request.TenantId
                    || result.ConversationId != request.ConversationId)
                {
                    return false;
                }
                if (result.Outcome is ConversationAgentsOutcome.Denied or ConversationAgentsOutcome.Unavailable or ConversationAgentsOutcome.Invalid
                    or ConversationAgentsOutcome.Conflict or ConversationAgentsOutcome.Quarantined)
                {
                    return result.Participants is null && result.Messages is null && !result.AgentParticipantPresent;
                }
                if (result.SourceRevision is null or < 0 || result.ObservedAt is null || result.ObservedAt == DateTimeOffset.MinValue
                    || string.IsNullOrWhiteSpace(result.ObservationId))
                {
                    return false;
                }
                if (result.Outcome != ConversationAgentsOutcome.Available)
                {
                    return result.Messages is null && result.Participants is null;
                }
                if (!result.AgentParticipantPresent)
                {
                    return false;
                }
                if (request.ParticipantStateOnly || request.AccessibilityOnly)
                {
                    return result.Messages is null && result.Participants is null;
                }
                if (result.Messages is null || result.Participants is null || result.Messages.Any(m => m is null || m.MessageId is null
                    || m.AuthorPartyId is null || (m.Deleted || m.Redacted) && m.Text is not null)
                    || result.Participants.Any(p => p is null || p.ParticipantPartyId is null
                        || p.ParticipantType is null || p.ParticipantRole is null)
                    || result.Messages.Select(m => m.MessageId).Distinct().Count() != result.Messages.Count)
                {
                    return false;
                }
                return request.MessageId is null || result.Messages.Count == 1 && result.Messages[0].MessageId == request.MessageId;
            case (ConversationActiveCountQuery request, ConversationActiveCountResult result):
                if (!Enum.IsDefined(result.Outcome) || result.SourceContractVersion != 1)
                {
                    return false;
                }
                return result.Outcome == ConversationAgentsOutcome.Available
                    ? result.Count is >= 0 && !string.IsNullOrWhiteSpace(result.CatalogCheckpoint) && result.ObservedAt is not null && result.ObservedAt != DateTimeOffset.MinValue
                        && result.TenantId == request.TenantId && result.CreatedFromInclusive == request.CreatedFromInclusive
                        && result.CreatedToExclusive == request.CreatedToExclusive
                    : result.Count is null && result.CatalogCheckpoint is null;
            case (ConversationDeletionSourceQuery request, ConversationDeletionSourceResult result):
                if (!Enum.IsDefined(result.Outcome) || result.DeliveryRevision < 0 || result.AcknowledgedSourceRevision < 0)
                {
                    return false;
                }
                if (result.Signal is not { } signal)
                {
                    return result.Outcome != ConversationAgentsOutcome.Available && result.Outcome != ConversationAgentsOutcome.Quarantined
                    && result.Acknowledgement is null && result.AcknowledgedSourceRevision == 0;
                }
                if (result.Outcome is not (ConversationAgentsOutcome.Available or ConversationAgentsOutcome.Quarantined)
                    || signal.SourceContractVersion != 1 || signal.TenantId != request.TenantId || signal.ConversationId != request.ConversationId
                    || signal.SourceRevision <= 0 || string.IsNullOrWhiteSpace(signal.ConversationDeletionSignalId)
                    || string.IsNullOrWhiteSpace(signal.ApprovalReference)
                    || signal.SourceStream != $"{request.TenantId.Value}:conversation:{request.ConversationId.Value}"
                    || request.SourceRevision is not null && request.SourceRevision != signal.SourceRevision
                    || request.SignalId is not null && request.SignalId != signal.ConversationDeletionSignalId)
                {
                    return false;
                }
                return result.AcknowledgedSourceRevision == 0 ? result.Acknowledgement is null
                    : result.AcknowledgedSourceRevision == signal.SourceRevision && result.Acknowledgement is { } acknowledgement
                        && acknowledgement.SignalId == signal.ConversationDeletionSignalId && acknowledgement.SourceRevision == signal.SourceRevision
                        && acknowledgement.ProtectedDeletionRevision > 0 && !string.IsNullOrWhiteSpace(acknowledgement.TargetVersion)
                        && !string.IsNullOrWhiteSpace(acknowledgement.Evidence);
            default:
                return false;
        }
    }

    private static string IntentId(ConversationCommandMetadata metadata, ConversationId conversation, string purpose)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new[] { metadata.TenantId.Value, conversation.Value, metadata.ActorPartyId.Value, purpose, metadata.IdempotencyKey })));

    private static ConversationAgentsOutcome AgentHttpOutcome(HttpStatusCode status)
        => status is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized ? ConversationAgentsOutcome.Denied
            : status == HttpStatusCode.Conflict ? ConversationAgentsOutcome.Conflict
            : status == HttpStatusCode.BadRequest ? ConversationAgentsOutcome.Invalid : ConversationAgentsOutcome.Unavailable;
}
