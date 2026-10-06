using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Immutable source-atomic deletion publication. Delivery attempts never change these fields.</summary>
/// <param name="ConversationDeletionSignalId">Deterministic identity of one logical approval.</param>
/// <param name="TenantId">The exact owning tenant.</param>
/// <param name="ConversationId">The only source Conversation authorized by this signal.</param>
/// <param name="SourceStream">The stable source stream address.</param>
/// <param name="SourceRevision">The persisted approval event position.</param>
/// <param name="ApprovalReference">The independently verified approval reference.</param>
/// <param name="SourceContractVersion">Closed source contract version.</param>
public sealed record ConversationDeletionSignal(string ConversationDeletionSignalId, TenantId TenantId,
    ConversationId ConversationId, string SourceStream, long SourceRevision, string ApprovalReference,
    int SourceContractVersion = 1);
