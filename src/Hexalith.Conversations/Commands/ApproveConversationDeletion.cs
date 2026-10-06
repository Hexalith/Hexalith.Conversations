// <copyright file="ApproveConversationDeletion.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Commands;

/// <summary>Pure restricted ApproveConversationDeletion intent; admission supplies current authority.</summary>
/// <param name="PublicCommand">The portable owner contract.</param>

/// <param name="EventId">Deterministic source event identity.</param>
public sealed record ApproveConversationDeletion(ApproveConversationDeletionCommand PublicCommand, string EventId)
{
    /// <summary>Gets the independently verified governance audit evidence for logical deletion.</summary>
    public GovernanceAuditEvidenceReference? AuditEvidence => PublicCommand.AuditEvidence;
}
