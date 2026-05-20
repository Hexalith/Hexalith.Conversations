// <copyright file="ConversationPageRequest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Defines permission-safe page input for conversation list queries.
/// </summary>
/// <param name="pageSize">The bounded number of accessible rows requested.</param>
/// <param name="continuationCursor">An opaque continuation cursor issued by a previous authorized list response.</param>
public sealed record ConversationPageRequest(
    int PageSize = 25,
    string? ContinuationCursor = null)
{
    /// <summary>
    /// Gets the bounded number of accessible rows requested.
    /// </summary>
    public int PageSize { get; } = ValidatePageSize(PageSize);

    /// <summary>
    /// Gets an opaque continuation cursor issued by a previous authorized list response.
    /// </summary>
    public string? ContinuationCursor { get; } = ValidateCursor(ContinuationCursor);

    private static int ValidatePageSize(int value)
    {
        if (value is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Page size must be between 1 and 100.");
        }

        return value;
    }

    private static string? ValidateCursor(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value;
}
