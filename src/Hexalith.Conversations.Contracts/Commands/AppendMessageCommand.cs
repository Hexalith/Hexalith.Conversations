// <copyright file="AppendMessageCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests that a message be appended to a conversation.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="messageId">The stable message identity.</param>
/// <param name="authorPartyId">The stable Party reference for the message author.</param>
/// <param name="text">The message text supplied by the caller.</param>
/// <param name="providerCorrelation">Optional provider correlation metadata.</param>
public sealed record AppendMessageCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    ProviderCorrelationMetadata? ProviderCorrelation = null);
