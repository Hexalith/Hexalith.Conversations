// <copyright file="MessageAppendedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>Durable MessageAppendedDomainEvent fact replayed from the authoritative stream.</summary>
/// <param name="Metadata">Source occurrence and identity.</param>
/// <param name="MessageId">Original deterministic message identity.</param>
/// <param name="AuthorPartyId">Immutable Agent Party.</param>
/// <param name="Text">Original posting text.</param>
/// <param name="AgentProvenance">Original trace and provenance.</param>
/// <param name="IdempotencyKey">Original intent key.</param>
/// <param name="IntentFingerprint">Canonical immutable full posting intent.</param>
/// <param name="ProviderCorrelation">Original provider provenance.</param>
public sealed record MessageAppendedDomainEvent(ConversationEventMetadata Metadata, MessageId MessageId, PartyId AuthorPartyId, string Text, AgentMessageProvenance AgentProvenance, string IdempotencyKey, string IntentFingerprint, ProviderCorrelationMetadata? ProviderCorrelation = null) : IEventPayload;
