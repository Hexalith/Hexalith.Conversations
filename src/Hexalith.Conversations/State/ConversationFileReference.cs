// <copyright file="ConversationFileReference.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.State;

/// <summary>
/// Represents a replayed file reference without file payload or upstream record data.
/// </summary>
/// <param name="FileId">The stable file identity.</param>
/// <param name="FolderId">The optional stable folder reference.</param>
/// <param name="MessageId">The optional message the file is attached to.</param>
public sealed record ConversationFileReference(
    FileId FileId,
    FolderId? FolderId = null,
    MessageId? MessageId = null);
