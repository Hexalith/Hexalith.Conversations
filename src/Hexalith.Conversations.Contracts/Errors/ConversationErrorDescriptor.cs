// <copyright file="ConversationErrorDescriptor.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Describes the canonical safe semantics for a Conversations error code.
/// </summary>
/// <param name="Code">The stable machine-readable code.</param>
/// <param name="Category">The broad error category.</param>
/// <param name="IsRetryable">A value indicating whether retry is meaningful.</param>
/// <param name="ClientAction">The bounded adopter action.</param>
/// <param name="SafeMessage">The safe adopter-facing message.</param>
/// <param name="Documentation">The absolute HTTPS documentation pointer.</param>
/// <param name="AllowsAuditHandle">A value indicating whether an audit handle may be included.</param>
public sealed record ConversationErrorDescriptor(
    ConversationErrorCode Code,
    ConversationErrorCategory Category,
    bool IsRetryable,
    ConversationErrorClientAction ClientAction,
    string SafeMessage,
    Uri Documentation,
    bool AllowsAuditHandle)
{
    /// <summary>
    /// Gets the stable machine-readable code.
    /// </summary>
    public ConversationErrorCode Code { get; } = Code ?? throw new ArgumentNullException(nameof(Code));

    /// <summary>
    /// Gets the broad error category.
    /// </summary>
    public ConversationErrorCategory Category { get; } = Category ?? throw new ArgumentNullException(nameof(Category));

    /// <summary>
    /// Gets the bounded adopter action.
    /// </summary>
    public ConversationErrorClientAction ClientAction { get; } = ClientAction ?? throw new ArgumentNullException(nameof(ClientAction));

    /// <summary>
    /// Gets the safe adopter-facing message.
    /// </summary>
    public string SafeMessage { get; } = EnsureSafeMessage(SafeMessage);

    /// <summary>
    /// Gets the absolute HTTPS documentation pointer.
    /// </summary>
    public Uri Documentation { get; } = EnsureDocumentation(Documentation);

    private static string EnsureSafeMessage(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ConversationError.EnsureContentSafe(value, nameof(SafeMessage));
        return value;
    }

    private static Uri EnsureDocumentation(Uri value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.IsAbsoluteUri || value.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Error documentation pointers must use absolute HTTPS URIs.", nameof(Documentation));
        }

        return value;
    }
}
