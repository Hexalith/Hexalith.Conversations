// <copyright file="IConversationGovernanceAuditService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Records or proves audit evidence before governed conversation mutations report success.
/// </summary>
public interface IConversationGovernanceAuditService
{
    /// <summary>
    /// Records audit evidence for a retention policy mutation.
    /// </summary>
    /// <param name="command">The retention policy command.</param>
    /// <param name="operationKind">The governed operation kind.</param>
    /// <param name="operationId">The server-generated operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit precondition result.</returns>
    ValueTask<ConversationGovernanceAuditResult> RecordRetentionPolicyChangeAsync(
        SetConversationRetentionPolicyCommand command,
        GovernanceOperationKind operationKind,
        string operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records audit evidence for a sensitivity mark mutation.
    /// </summary>
    /// <param name="command">The sensitivity mark command.</param>
    /// <param name="operationKind">The governed operation kind.</param>
    /// <param name="operationId">The server-generated operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit precondition result.</returns>
    ValueTask<ConversationGovernanceAuditResult> RecordSensitivityMarkAsync(
        MarkConversationContentSensitiveCommand command,
        GovernanceOperationKind operationKind,
        string operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records audit evidence for a redaction mutation.
    /// </summary>
    /// <param name="command">The redaction command.</param>
    /// <param name="operationKind">The governed operation kind.</param>
    /// <param name="operationId">The server-generated operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit precondition result.</returns>
    ValueTask<ConversationGovernanceAuditResult> RecordRedactionAsync(
        RedactMessageContentCommand command,
        GovernanceOperationKind operationKind,
        string operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records audit evidence for a privileged operational justification boundary.
    /// </summary>
    /// <param name="command">The structured privileged justification command.</param>
    /// <param name="operationKind">The governed operation kind.</param>
    /// <param name="outcome">The bounded public outcome being recorded.</param>
    /// <param name="operationId">The server-generated operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit precondition result.</returns>
    ValueTask<ConversationGovernanceAuditResult> RecordPrivilegedOperationalJustificationAsync(
        RecordPrivilegedOperationalJustificationCommand command,
        GovernanceOperationKind operationKind,
        GovernanceOutcome outcome,
        string operationId,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ConversationGovernanceAuditResult.PolicyBlocked());
}
