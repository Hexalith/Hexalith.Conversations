// <copyright file="MessageDeleted.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>Public MessageDeleted fact for compatible source projections.</summary>
/// <param name="Metadata">Exact source occurrence and identity.</param>
/// <param name="MessageId">Exact deleted message.</param>
public sealed record MessageDeleted(ConversationEventMetadata Metadata, MessageId MessageId);
