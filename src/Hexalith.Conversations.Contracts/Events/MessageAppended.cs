// <copyright file="MessageAppended.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a message was appended to a conversation.
/// </summary>
/// <remarks>
/// Story 1.4.1 will define the length cap, encoding contract, and inline-vs-reference policy
/// for <see cref="Text"/>. Until then this contract carries the message body inline as a string.
/// </remarks>
/// <param name="metadata">The public event metadata.</param>
/// <param name="messageId">The stable message identity.</param>
/// <param name="authorPartyId">The stable Party reference for the author.</param>
/// <param name="text">The message text supplied by the caller.</param>
/// <param name="providerCorrelation">Optional provider correlation metadata.</param>
/// <param name="AgentProvenance">Durable original posting provenance.</param>
/// <param name="IdempotencyKey">Durable opaque posting intent identity.</param>
public sealed record MessageAppended(
    ConversationEventMetadata Metadata,
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    ProviderCorrelationMetadata? ProviderCorrelation = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    AgentMessageProvenance? AgentProvenance = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? IdempotencyKey = null);
