// <copyright file="MessageDeletedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>Durable MessageDeletedDomainEvent fact replayed from the authoritative stream.</summary>
/// <param name="Metadata">Source occurrence and identity.</param>
/// <param name="MessageId">Exact deleted message.</param>
public sealed record MessageDeletedDomainEvent(ConversationEventMetadata Metadata, MessageId MessageId) : IEventPayload;
