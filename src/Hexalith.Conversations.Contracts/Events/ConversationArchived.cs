// <copyright file="ConversationArchived.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a conversation was archived.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="reasonCode">An optional safe reason code.</param>
public sealed record ConversationArchived(
    ConversationEventMetadata Metadata,
    string? ReasonCode = null);
