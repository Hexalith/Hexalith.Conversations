// <copyright file="ConversationAgentReadQuery.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Exact current read; tenant and principal are independently authenticated.</summary>
/// <param name="TenantId">The requested tenant.</param>
/// <param name="ConversationId">The exact source Conversation.</param>
/// <param name="MessageId">Optional exact message lookup; null requests current content and roster.</param>
/// <param name="ParticipantStateOnly">Permits membership-state lookup without disclosing content before joining.</param>
/// <param name="AccessibilityOnly">Returns accessibility evidence without content.</param>
public sealed record ConversationAgentReadQuery(TenantId TenantId, ConversationId ConversationId,
    MessageId? MessageId = null, bool ParticipantStateOnly = false, bool AccessibilityOnly = false);
