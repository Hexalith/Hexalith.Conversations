// <copyright file="ConversationGovernanceAuditStatus.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Describes internal audit precondition outcomes for governed mutations.
/// </summary>
public enum ConversationGovernanceAuditStatus
{
    /// <summary>
    /// Audit evidence was accepted and is safe to cite.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// Audit evidence could not be recorded.
    /// </summary>
    AuditUnavailable = 1,

    /// <summary>
    /// Audit evidence was uncertain and cannot prove pairing.
    /// </summary>
    Uncertain = 2,

    /// <summary>
    /// Audit evidence was unsafe to expose or reuse.
    /// </summary>
    UnsafeEvidence = 3,

    /// <summary>
    /// Policy blocked the requested mutation without proving a success.
    /// </summary>
    PolicyBlocked = 4,
}
