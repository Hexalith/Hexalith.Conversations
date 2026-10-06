// <copyright file="ConversationDeletionApproved.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>Public ConversationDeletionApproved fact for compatible source projections.</summary>
/// <param name="Metadata">Exact source occurrence and identity.</param>
/// <param name="Signal">Immutable source-atomic publication.</param>
/// <param name="AuditEvidence">Independent logical-deletion audit evidence.</param>
public sealed record ConversationDeletionApproved(ConversationEventMetadata Metadata, ConversationDeletionSignal Signal, GovernanceAuditEvidenceReference AuditEvidence);
