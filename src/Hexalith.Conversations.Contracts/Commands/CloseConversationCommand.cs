// <copyright file="CloseConversationCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests that a conversation be closed.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="reasonCode">An optional safe reason code.</param>
public sealed record CloseConversationCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    string? ReasonCode = null);
