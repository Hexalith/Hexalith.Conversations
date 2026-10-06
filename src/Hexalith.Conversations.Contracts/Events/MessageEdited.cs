// <copyright file="MessageEdited.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>Public MessageEdited fact for compatible source projections.</summary>
/// <param name="Metadata">Exact source occurrence and identity.</param>
/// <param name="MessageId">Exact current message.</param>
/// <param name="Text">Current edited text.</param>
/// <param name="EditedByPartyId">Human editor attribution.</param>
public sealed record MessageEdited(ConversationEventMetadata Metadata, MessageId MessageId, string Text, PartyId EditedByPartyId);
