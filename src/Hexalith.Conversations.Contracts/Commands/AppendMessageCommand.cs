// <copyright file="AppendMessageCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;

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
/// <param name="callerMetadata">Optional bounded, content-safe caller provenance metadata.</param>
/// <param name="AgentProvenance">Optional durable Agents provenance, never authority.</param>
/// <param name="OperationTimestamp">Deterministic timestamp required for restricted posting.</param>
public sealed record AppendMessageCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    ProviderCorrelationMetadata? ProviderCorrelation = null,
    CallerMetadata? CallerMetadata = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    AgentMessageProvenance? AgentProvenance = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DateTimeOffset? OperationTimestamp = null);
