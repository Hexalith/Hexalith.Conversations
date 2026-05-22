// <copyright file="SetConversationRetentionPolicy.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Commands;

/// <summary>
/// Domain command for applying a validated governed retention policy mutation.
/// </summary>
/// <param name="PublicCommand">The public retention policy command supplied by an adopter boundary.</param>
/// <param name="AuditEvidence">The safe audit evidence paired before reporting success.</param>
/// <param name="EventId">The deterministic public event identity supplied by the boundary.</param>
public sealed record SetConversationRetentionPolicy(
    SetConversationRetentionPolicyCommand PublicCommand,
    GovernanceAuditEvidenceReference AuditEvidence,
    string EventId);
