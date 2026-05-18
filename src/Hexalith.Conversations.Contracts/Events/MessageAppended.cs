// <copyright file="MessageAppended.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a message was appended to a conversation.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="messageId">The stable message identity.</param>
/// <param name="authorPartyId">The stable Party reference for the author.</param>
/// <param name="text">The message text supplied by the caller.</param>
/// <param name="providerCorrelation">Optional provider correlation metadata.</param>
public sealed record MessageAppended(
    ConversationEventMetadata Metadata,
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    ProviderCorrelationMetadata? ProviderCorrelation = null);
