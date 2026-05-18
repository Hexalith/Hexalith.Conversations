// <copyright file="ConversationErrorResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Wraps one or more content-safe Conversations errors.
/// </summary>
/// <param name="errors">The content-safe machine-readable errors.</param>
public sealed record ConversationErrorResult(IReadOnlyList<ConversationError> Errors)
{
    /// <summary>
    /// Gets the content-safe machine-readable errors.
    /// </summary>
    public IReadOnlyList<ConversationError> Errors { get; } = Validate(Errors);

    private static IReadOnlyList<ConversationError> Validate(IReadOnlyList<ConversationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException("At least one non-null error is required.", nameof(errors));
        }

        ConversationError[] snapshot = errors.ToArray();
        if (snapshot.Any(error => error is null))
        {
            throw new ArgumentException("At least one non-null error is required.", nameof(errors));
        }

        return snapshot;
    }
}
