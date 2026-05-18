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

        return errors.Count == 0 || errors.Any(error => error is null)
            ? throw new ArgumentException("At least one non-null error is required.", nameof(errors))
            : errors;
    }
}
