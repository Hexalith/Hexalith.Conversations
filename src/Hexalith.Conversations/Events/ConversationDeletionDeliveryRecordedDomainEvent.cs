// <copyright file="ConversationDeletionDeliveryRecordedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>Durable ConversationDeletionDeliveryRecordedDomainEvent fact replayed from the authoritative stream.</summary>
/// <param name="Metadata">Source occurrence and identity.</param>
/// <param name="Signal">Original immutable signal.</param>
/// <param name="Action">Bounded durable transition.</param>
/// <param name="DeliveryAttemptId">Independent retry identity.</param>
/// <param name="TargetVersion">Exact authenticated receiver.</param>
/// <param name="PreviousDeliveryRevision">Prior delivery compare revision.</param>
/// <param name="Acknowledgement">Verified exact receipt only.</param>
/// <param name="PoisonCode">Safe quarantine classification.</param>
public sealed record ConversationDeletionDeliveryRecordedDomainEvent(ConversationEventMetadata Metadata, ConversationDeletionSignal Signal, ConversationDeletionDeliveryAction Action, string DeliveryAttemptId, string TargetVersion, long PreviousDeliveryRevision, ConversationDeletionAcknowledgement? Acknowledgement = null, string? PoisonCode = null) : IEventPayload;
