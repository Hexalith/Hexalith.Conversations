using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Publishes a previously independently approved deletion with its durable source signal.</summary>
/// <param name="Metadata">Authenticated publisher metadata; never Agents service authority.</param>
/// <param name="ConversationId">The exact source Conversation.</param>
/// <param name="ApprovalReference">The independent approval verified before admission.</param>
/// <param name="SourceRevision">Expected prior complete source revision.</param>
/// <param name="OperationTimestamp">Deterministic approval occurrence time.</param>
/// <param name="AuditEvidence">Independent logically-delete audit evidence bound to approval, policy and exact source.</param>
public sealed record ApproveConversationDeletionCommand(ConversationCommandMetadata Metadata,
    ConversationId ConversationId, string ApprovalReference, long SourceRevision,
    DateTimeOffset OperationTimestamp, GovernanceAuditEvidenceReference? AuditEvidence = null);
