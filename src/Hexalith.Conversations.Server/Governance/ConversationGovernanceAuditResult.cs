// <copyright file="ConversationGovernanceAuditResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Carries internal audit precondition evidence without exposing infrastructure details.
/// </summary>
/// <param name="Status">The internal audit precondition status.</param>
/// <param name="Evidence">The safe audit evidence reference when successful.</param>
public sealed record ConversationGovernanceAuditResult(
    ConversationGovernanceAuditStatus Status,
    GovernanceAuditEvidenceReference? Evidence = null)
{
    /// <summary>
    /// Creates a successful audit precondition result.
    /// </summary>
    /// <param name="evidence">The safe audit evidence reference.</param>
    /// <returns>The successful result.</returns>
    public static ConversationGovernanceAuditResult Succeeded(GovernanceAuditEvidenceReference evidence)
        => new(ConversationGovernanceAuditStatus.Succeeded, evidence ?? throw new ArgumentNullException(nameof(evidence)));

    /// <summary>
    /// Creates an audit-unavailable result.
    /// </summary>
    /// <returns>The audit-unavailable result.</returns>
    public static ConversationGovernanceAuditResult AuditUnavailable()
        => new(ConversationGovernanceAuditStatus.AuditUnavailable);

    /// <summary>
    /// Creates an uncertain audit result.
    /// </summary>
    /// <returns>The uncertain result.</returns>
    public static ConversationGovernanceAuditResult Uncertain()
        => new(ConversationGovernanceAuditStatus.Uncertain);

    /// <summary>
    /// Creates an unsafe-evidence result.
    /// </summary>
    /// <returns>The unsafe-evidence result.</returns>
    public static ConversationGovernanceAuditResult UnsafeEvidence()
        => new(ConversationGovernanceAuditStatus.UnsafeEvidence);

    /// <summary>
    /// Creates a policy-blocked result.
    /// </summary>
    /// <returns>The policy-blocked result.</returns>
    public static ConversationGovernanceAuditResult PolicyBlocked()
        => new(ConversationGovernanceAuditStatus.PolicyBlocked);
}
