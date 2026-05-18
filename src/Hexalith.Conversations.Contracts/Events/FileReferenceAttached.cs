// <copyright file="FileReferenceAttached.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a stable upstream file reference was attached.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="fileId">The stable upstream file reference.</param>
/// <param name="folderId">An optional stable upstream folder reference.</param>
/// <param name="messageId">An optional stable message reference associated with the file.</param>
public sealed record FileReferenceAttached(
    ConversationEventMetadata Metadata,
    FileId FileId,
    FolderId? FolderId = null,
    MessageId? MessageId = null);
