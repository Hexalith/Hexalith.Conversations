// <copyright file="MessageEditedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>Durable MessageEditedDomainEvent fact replayed from the authoritative stream.</summary>
/// <param name="Metadata">Source occurrence and identity.</param>
/// <param name="MessageId">Exact message.</param>
/// <param name="Text">Current visible text.</param>
/// <param name="EditedByPartyId">Admitted human actor.</param>
public sealed record MessageEditedDomainEvent(ConversationEventMetadata Metadata, MessageId MessageId, string Text, PartyId EditedByPartyId) : IEventPayload;
