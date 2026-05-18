// <copyright file="ArchiveConversationCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests that a closed conversation be archived.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="reasonCode">An optional safe reason code.</param>
public sealed record ArchiveConversationCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    string? ReasonCode = null)
{
    /// <summary>
    /// Gets the optional safe reason code.
    /// </summary>
    public string? ReasonCode { get; } = ValidateOptional(ReasonCode);

    private static string? ValidateOptional(string? value)
    {
        if (value is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }

        return value;
    }
}
