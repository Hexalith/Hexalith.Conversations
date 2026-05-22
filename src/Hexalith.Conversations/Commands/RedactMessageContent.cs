// <copyright file="RedactMessageContent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Commands;

/// <summary>
/// Domain command for applying a validated governed redaction mutation.
/// </summary>
/// <param name="PublicCommand">The public redaction command supplied by an adopter boundary.</param>
/// <param name="AuditEvidence">The safe audit evidence paired before reporting success.</param>
/// <param name="EventId">The deterministic public event identity supplied by the boundary.</param>
public sealed record RedactMessageContent(
    RedactMessageContentCommand PublicCommand,
    GovernanceAuditEvidenceReference AuditEvidence,
    string EventId);
