// <copyright file="AttachFileReferenceCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests that a stable upstream file reference be attached to a conversation.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="fileId">The stable upstream file reference.</param>
/// <param name="folderId">An optional stable upstream folder reference.</param>
/// <param name="messageId">An optional stable message reference associated with the file.</param>
public sealed record AttachFileReferenceCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    FileId FileId,
    FolderId? FolderId = null,
    MessageId? MessageId = null);
