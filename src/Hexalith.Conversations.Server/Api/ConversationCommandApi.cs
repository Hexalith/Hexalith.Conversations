// <copyright file="ConversationCommandApi.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Claims;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Api;

/// <summary>
/// Defines guarded conversation command routes for hosts that explicitly opt in.
/// </summary>
public static class ConversationCommandApi
{
    /// <summary>
    /// Maps versioned conversation command endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapConversationCommandApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/api/v1/conversations").RequireAuthorization();
        group.MapPost("/", CreateConversationAsync);
        group.MapPost("/{conversationId}/messages", AppendMessageAsync);
        group.MapPost("/{conversationId}/project", ReassignConversationProjectAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateConversationAsync(
        HttpContext context,
        IConversationCommandApiHandler handler,
        CancellationToken cancellationToken)
    {
        CreateConversationCommand? command = await ReadBodyAsync<CreateConversationCommand>(context, cancellationToken)
            .ConfigureAwait(false);
        if (!TryValidateCommandContext(context, command?.Metadata, out IResult? rejection))
        {
            return rejection;
        }

        ConversationCommandApiOutcome<ConversationCreatedResult> outcome = await handler
            .CreateConversationAsync(command!, cancellationToken)
            .ConfigureAwait(false);
        return ToHttpResult(outcome);
    }

    private static async Task<IResult> ReassignConversationProjectAsync(
        string conversationId,
        HttpContext context,
        IConversationCommandApiHandler handler,
        CancellationToken cancellationToken)
    {
        ReassignConversationProjectCommand? command = await ReadBodyAsync<ReassignConversationProjectCommand>(context, cancellationToken)
            .ConfigureAwait(false);
        if (!TryValidateCommandContext(context, command?.Metadata, out IResult? rejection))
        {
            return rejection;
        }

        if (command?.ConversationId is null || !string.Equals(conversationId, command.ConversationId.Value, StringComparison.Ordinal))
        {
            return ErrorResult(
                ConversationErrorCode.CommandValidationFailed,
                command?.Metadata?.CorrelationId ?? CorrelationIdFrom(context),
                StatusCodes.Status400BadRequest,
                "Route and command conversation identity must match.");
        }

        ConversationCommandApiOutcome<ConversationCommandAcceptedResult> outcome = await handler
            .ReassignConversationProjectAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return ToHttpResult(outcome);
    }

    private static async Task<IResult> AppendMessageAsync(
        string conversationId,
        HttpContext context,
        IConversationCommandApiHandler handler,
        CancellationToken cancellationToken)
    {
        AppendMessageCommand? command = await ReadBodyAsync<AppendMessageCommand>(context, cancellationToken)
            .ConfigureAwait(false);
        if (!TryValidateCommandContext(context, command?.Metadata, out IResult? rejection))
        {
            return rejection;
        }

        if (command is null || !string.Equals(conversationId, command.ConversationId.Value, StringComparison.Ordinal))
        {
            return ErrorResult(
                ConversationErrorCode.CommandValidationFailed,
                command?.Metadata?.CorrelationId ?? CorrelationIdFrom(context),
                StatusCodes.Status400BadRequest,
                "Route and command conversation identity must match.");
        }

        ConversationCommandApiOutcome<ConversationCommandAcceptedResult> outcome = await handler
            .AppendMessageAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return ToHttpResult(outcome);
    }

    private static async Task<T?> ReadBodyAsync<T>(HttpContext context, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    private static IResult ToHttpResult<T>(ConversationCommandApiOutcome<T> outcome)
        where T : class
        => outcome.Value is not null
            ? Results.Json(outcome.Value, statusCode: outcome.StatusCode)
            : Results.Json(outcome.Error, statusCode: outcome.StatusCode);

    private static bool TryValidateCommandContext(
        HttpContext context,
        ConversationCommandMetadata? metadata,
        out IResult rejection)
    {
        rejection = Results.Empty;
        string correlationId = metadata?.CorrelationId ?? CorrelationIdFrom(context);

        if (metadata is null)
        {
            rejection = ErrorResult(
                ConversationErrorCode.CommandValidationFailed,
                correlationId,
                StatusCodes.Status400BadRequest,
                "Command metadata is required.");
            return false;
        }

        ClaimsPrincipal? principal = context.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            rejection = ErrorResult(
                ConversationErrorCode.TenantBindingMissing,
                correlationId,
                StatusCodes.Status403Forbidden,
                "Authenticated tenant and caller context is required.");
            return false;
        }

        string? tenantClaim = principal.FindFirstValue(ConversationReadApi.TenantIdClaimType);
        string? callerClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(tenantClaim) || string.IsNullOrWhiteSpace(callerClaim))
        {
            rejection = ErrorResult(
                ConversationErrorCode.TenantBindingMissing,
                correlationId,
                StatusCodes.Status403Forbidden,
                "Authenticated tenant and caller context is required.");
            return false;
        }

        try
        {
            TenantId trustedTenantId = new(tenantClaim);
            if (trustedTenantId != metadata.TenantId)
            {
                rejection = ErrorResult(
                    ConversationErrorCode.TenantContextMismatch,
                    correlationId,
                    StatusCodes.Status403Forbidden,
                    "Command tenant binding must match authenticated tenant context.");
                return false;
            }
        }
        catch (ArgumentException)
        {
            rejection = ErrorResult(
                ConversationErrorCode.TenantBindingMissing,
                correlationId,
                StatusCodes.Status403Forbidden,
                "Authenticated tenant and caller context is required.");
            return false;
        }

        return true;
    }

    private static IResult ErrorResult(
        ConversationErrorCode code,
        string correlationId,
        int statusCode,
        string developerGuidance)
        => Results.Json(
            new ConversationErrorResult(
                [
                    ConversationErrorCatalog.CreateError(
                        code,
                        correlationId,
                        developerGuidance: developerGuidance),
                ]),
            statusCode: statusCode);

    private static string CorrelationIdFrom(HttpContext context)
        => context.Request.Headers.TryGetValue(ConversationReadApi.CorrelationIdHeaderName, out Microsoft.Extensions.Primitives.StringValues values)
            ? values.FirstOrDefault() ?? Guid.NewGuid().ToString("N")
            : Guid.NewGuid().ToString("N");
}

/// <summary>
/// Handles command execution for opt-in Conversations command API hosts.
/// </summary>
public interface IConversationCommandApiHandler
{
    /// <summary>
    /// Executes a create-conversation command.
    /// </summary>
    /// <param name="command">The public command contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed API outcome.</returns>
    ValueTask<ConversationCommandApiOutcome<ConversationCreatedResult>> CreateConversationAsync(
        CreateConversationCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an append-message command.
    /// </summary>
    /// <param name="command">The public command contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed API outcome.</returns>
    ValueTask<ConversationCommandApiOutcome<ConversationCommandAcceptedResult>> AppendMessageAsync(
        AppendMessageCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a project reassignment command.
    /// </summary>
    /// <param name="command">The public command contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed API outcome.</returns>
    ValueTask<ConversationCommandApiOutcome<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(
        ReassignConversationProjectCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Carries a typed command API success or typed Conversations error result.
/// </summary>
/// <typeparam name="T">The success result contract type.</typeparam>
public sealed record ConversationCommandApiOutcome<T>
    where T : class
{
    private ConversationCommandApiOutcome(T? value, ConversationErrorResult? error, int statusCode)
    {
        if ((value is null) == (error is null))
        {
            throw new ArgumentException("Exactly one typed success value or typed error result is required.");
        }

        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the success result when present.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the typed error result when present.
    /// </summary>
    public ConversationErrorResult? Error { get; }

    /// <summary>
    /// Gets the HTTP status code to emit.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Creates a success outcome.
    /// </summary>
    /// <param name="value">The success result.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The API outcome.</returns>
    public static ConversationCommandApiOutcome<T> Success(T value, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, null, statusCode);
    }

    /// <summary>
    /// Creates an error outcome.
    /// </summary>
    /// <param name="error">The typed error result.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The API outcome.</returns>
    public static ConversationCommandApiOutcome<T> Failure(ConversationErrorResult error, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(null, error, statusCode);
    }
}
