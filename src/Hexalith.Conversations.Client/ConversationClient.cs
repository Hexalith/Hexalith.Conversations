// <copyright file="ConversationClient.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

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
    internal const string CorrelationIdHeaderName = "X-Correlation-Id";
    internal const string CausationIdHeaderName = "X-Causation-Id";
    internal const string IdempotencyKeyHeaderName = "Idempotency-Key";
    internal const string TenantIdHeaderName = "X-Tenant-Id";
    internal const string ActorPartyIdHeaderName = "X-Actor-Party-Id";
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
}
