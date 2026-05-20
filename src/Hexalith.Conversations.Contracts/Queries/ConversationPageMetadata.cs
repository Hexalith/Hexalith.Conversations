// <copyright file="ConversationPageMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries page metadata that is relative only to rows visible to the authorized caller.
/// </summary>
/// <param name="returnedCount">The number of accessible rows returned in this page.</param>
/// <param name="continuationCursor">An opaque cursor for the next accessible page, when one is issued.</param>
public sealed record ConversationPageMetadata(
    int ReturnedCount,
    string? ContinuationCursor = null)
{
    /// <summary>
    /// Gets the number of accessible rows returned in this page.
    /// </summary>
    public int ReturnedCount { get; } = ValidateReturnedCount(ReturnedCount);

    /// <summary>
    /// Gets an opaque cursor for the next accessible page, when one is issued.
    /// </summary>
    public string? ContinuationCursor { get; } = string.IsNullOrWhiteSpace(ContinuationCursor) ? null : ContinuationCursor;

    private static int ValidateReturnedCount(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        return value;
    }
}
