// <copyright file="ConversationRetentionPolicyState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.State;

/// <summary>
/// Replay-only active retention policy state.
/// </summary>
/// <param name="PolicyReference">The active public retention policy reference.</param>
/// <param name="Rationale">The content-safe governance rationale.</param>
/// <param name="ActorPartyId">The stable Party actor attribution.</param>
/// <param name="AppliedAt">The event timestamp recorded by the aggregate.</param>
/// <param name="AuditEvidence">The safe audit evidence paired with the mutation.</param>
/// <param name="PreviousPolicyReference">The previous public retention policy reference when replaced.</param>
public sealed record ConversationRetentionPolicyState(
    string PolicyReference,
    string Rationale,
    PartyId ActorPartyId,
    DateTimeOffset AppliedAt,
    GovernanceAuditEvidenceReference AuditEvidence,
    string? PreviousPolicyReference = null);
