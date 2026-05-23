// <copyright file="UpdateConversationMetadataCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests a safe metadata update for a conversation.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="attributes">Optional safe adopter metadata.</param>
/// <param name="callerMetadata">Optional bounded, content-safe caller provenance metadata.</param>
public sealed record UpdateConversationMetadataCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    string? Label = null,
    BusinessReference? BusinessReference = null,
    IReadOnlyDictionary<string, string>? Attributes = null,
    CallerMetadata? CallerMetadata = null);
