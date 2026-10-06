// <copyright file="ConversationDeletionApprovedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>Durable ConversationDeletionApprovedDomainEvent fact replayed from the authoritative stream.</summary>
/// <param name="Metadata">Source occurrence and identity.</param>
/// <param name="Signal">Immutable source-atomic publication.</param>
/// <param name="AuditEvidence">Independent logical-deletion audit evidence.</param>
public sealed record ConversationDeletionApprovedDomainEvent(ConversationEventMetadata Metadata, ConversationDeletionSignal Signal, GovernanceAuditEvidenceReference AuditEvidence) : IEventPayload;
