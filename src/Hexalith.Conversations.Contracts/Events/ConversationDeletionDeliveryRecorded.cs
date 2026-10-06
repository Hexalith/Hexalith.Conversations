// <copyright file="ConversationDeletionDeliveryRecorded.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>Public ConversationDeletionDeliveryRecorded fact for compatible source projections.</summary>
/// <param name="Metadata">Exact source occurrence and identity.</param>
/// <param name="Signal">Original immutable publication.</param>
/// <param name="Action">Recorded delivery transition.</param>
/// <param name="DeliveryAttemptId">Independent delivery attempt identity.</param>
/// <param name="TargetVersion">Exact admitted target.</param>
/// <param name="PreviousDeliveryRevision">Durable delivery compare position.</param>
/// <param name="Acknowledgement">Exact authenticated receipt when accepted.</param>
/// <param name="PoisonCode">Safe poison classification.</param>
public sealed record ConversationDeletionDeliveryRecorded(ConversationEventMetadata Metadata, ConversationDeletionSignal Signal, ConversationDeletionDeliveryAction Action, string DeliveryAttemptId, string TargetVersion, long PreviousDeliveryRevision, ConversationDeletionAcknowledgement? Acknowledgement = null, string? PoisonCode = null);
