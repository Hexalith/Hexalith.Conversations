// <copyright file="EditConversationMessageCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Current message mutation admitted only under independent human authority.</summary>
/// <param name="Metadata">Authenticated human actor and tenant.</param>
/// <param name="ConversationId">Exact source.</param>
/// <param name="MessageId">Exact existing message.</param>
/// <param name="Text">New visible text.</param>
/// <param name="OperationTimestamp">Persisted occurrence.</param>
public sealed record EditConversationMessageCommand(ConversationCommandMetadata Metadata, ConversationId ConversationId,
    MessageId MessageId, string Text, DateTimeOffset OperationTimestamp);
