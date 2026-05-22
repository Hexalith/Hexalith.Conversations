// <copyright file="MarkConversationContentSensitive.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Commands;

/// <summary>
/// Domain command for applying a validated governed sensitivity mark mutation.
/// </summary>
/// <param name="PublicCommand">The public sensitivity command supplied by an adopter boundary.</param>
/// <param name="AuditEvidence">The safe audit evidence paired before reporting success.</param>
/// <param name="EventId">The deterministic public event identity supplied by the boundary.</param>
public sealed record MarkConversationContentSensitive(
    MarkConversationContentSensitiveCommand PublicCommand,
    GovernanceAuditEvidenceReference AuditEvidence,
    string EventId);
