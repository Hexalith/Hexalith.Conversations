using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Private source-worker delivery transition; no general dispatcher or human authority.</summary>
/// <param name="Metadata">Authenticated source worker binding.</param>
/// <param name="ConversationId">The exact source stream.</param>
/// <param name="Signal">The immutable source signal.</param>
/// <param name="Action">The bounded delivery transition.</param>
/// <param name="DeliveryAttemptId">Independent attempt identity.</param>
/// <param name="TargetVersion">Exact authenticated receiver version for this attempt.</param>
/// <param name="ExpectedDeliveryRevision">Compare revision; prevents conflicting lost-ack retries.</param>
/// <param name="Acknowledgement">Required only for acknowledgement.</param>
public sealed record RecordConversationDeletionDeliveryCommand(ConversationCommandMetadata Metadata,
    ConversationId ConversationId, ConversationDeletionSignal Signal, ConversationDeletionDeliveryAction Action,
    string DeliveryAttemptId, string TargetVersion, long ExpectedDeliveryRevision,
    ConversationDeletionAcknowledgement? Acknowledgement = null);
