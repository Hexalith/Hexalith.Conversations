// <copyright file="ConversationRedactionState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.State;

/// <summary>
/// Replay-only active redaction intent state for one governed target.
/// </summary>
/// <param name="TargetKey">The deterministic safe target key.</param>
/// <param name="Target">The content-safe governed target reference.</param>
/// <param name="Category">The bounded redaction category.</param>
/// <param name="PolicyReference">The content-safe public policy reference.</param>
/// <param name="Rationale">The content-safe governance rationale.</param>
/// <param name="ActorPartyId">The stable Party actor attribution.</param>
/// <param name="RedactedAt">The event timestamp recorded by the aggregate.</param>
/// <param name="AuditEvidence">The safe audit evidence paired with the mutation.</param>
public sealed record ConversationRedactionState(
    string TargetKey,
    GovernanceTarget Target,
    RedactionCategory Category,
    string PolicyReference,
    string Rationale,
    PartyId ActorPartyId,
    DateTimeOffset RedactedAt,
    GovernanceAuditEvidenceReference AuditEvidence);
