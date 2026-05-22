// <copyright file="ConversationClientResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;

using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Client;

/// <summary>
/// Carries either a typed Conversations success result or typed Conversations errors.
/// </summary>
/// <typeparam name="T">The success contract type.</typeparam>
public sealed record ConversationClientResult<T>
    where T : class
{
    private ConversationClientResult(T? value, ConversationErrorResult? error, HttpStatusCode? statusCode)
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
    /// Gets a value indicating whether the operation returned a typed success contract.
    /// </summary>
    public bool IsSuccess => Value is not null;

    /// <summary>
    /// Gets the typed success contract when <see cref="IsSuccess"/> is true.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets typed Conversations errors when <see cref="IsSuccess"/> is false.
    /// </summary>
    public ConversationErrorResult? Error { get; }

    /// <summary>
    /// Gets the HTTP status code observed by the client, when a response was received.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Creates a typed success result.
    /// </summary>
    /// <param name="value">The success contract.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The client result.</returns>
    public static ConversationClientResult<T> Success(T value, HttpStatusCode statusCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, null, statusCode);
    }

    /// <summary>
    /// Creates a typed error result.
    /// </summary>
    /// <param name="error">The typed error contract.</param>
    /// <param name="statusCode">The HTTP status code, when known.</param>
    /// <returns>The client result.</returns>
    public static ConversationClientResult<T> Failure(ConversationErrorResult error, HttpStatusCode? statusCode = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(null, error, statusCode);
    }
}
