namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Receiver-issued acknowledgement; authenticity is verified before durable admission.</summary>
/// <param name="SignalId">The immutable source signal.</param>
/// <param name="SourceRevision">The original approval position.</param>
/// <param name="ProtectedDeletionRevision">The receiver's persisted protected deletion position.</param>
/// <param name="TargetVersion">The authenticated receiver target version.</param>
/// <param name="Evidence">Opaque authenticated receipt, not a human role or cancellation capability.</param>
public sealed record ConversationDeletionAcknowledgement(string SignalId, long SourceRevision,
    long ProtectedDeletionRevision, string TargetVersion, string Evidence);
