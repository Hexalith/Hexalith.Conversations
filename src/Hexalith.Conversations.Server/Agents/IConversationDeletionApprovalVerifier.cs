// <copyright file="IConversationDeletionApprovalVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Independently verifies approved logical deletion and its governance audit evidence; defaults fail closed.</summary>
/// <remarks>Available requires current authenticated approval, the exact tenant/Conversation/source revision,
/// approval reference, policy, original approver and operation timestamp, and an authentic
/// GovernanceAuditEvidenceReference for LogicallyDeleteConversation. Caller-supplied handles are never proof.
/// Verification must preserve the immutable original binding on retries and reconfirm it before append.</remarks>
public interface IConversationDeletionApprovalVerifier
{
    /// <summary>Resolves current exact-operation evidence without inferring authority from projections or caller claims.</summary>
    /// <param name="envelope">Authenticated exact source transport.</param>
    /// <param name="command">Approval with independently issued logical-deletion audit evidence.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A classified authenticated approval and audit result.</returns>
    Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope,
        ApproveConversationDeletionCommand command, CancellationToken cancellationToken = default);
}
