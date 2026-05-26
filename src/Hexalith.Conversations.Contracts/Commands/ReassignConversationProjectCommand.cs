// <copyright file="ReassignConversationProjectCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests assignment, reassignment, or explicit clearing of a conversation project reference.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="target">The explicit assignment operation and target.</param>
/// <param name="expectedCurrentProjectId">Optional optimistic guard for the current project reference.</param>
/// <param name="callerMetadata">Optional bounded, content-safe caller provenance metadata.</param>
public sealed record ReassignConversationProjectCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    ConversationProjectAssignment Target,
    ProjectId? ExpectedCurrentProjectId = null,
    CallerMetadata? CallerMetadata = null);
