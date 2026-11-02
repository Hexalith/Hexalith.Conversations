using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Routing;
using Hexalith.Conversations.Idempotency;
using Hexalith.EventStore.DomainService;

[assembly: RouteCatalogAdapterSource(typeof(Hexalith.Conversations.EventStoreExtensions.ConversationsCatalogAdapters))]

namespace Hexalith.Conversations.EventStoreExtensions;

/// <summary>Exact internal command adapters; owner admission and current business checks remain authoritative.</summary>
public static class ConversationsCatalogAdapters
{
    /// <summary>Creates all thirteen typed semantic adapters without enabling runtime admission.</summary>
    /// <returns>The exact aggregate receiver adapter inventory.</returns>
    public static IReadOnlyList<IDeclaredIdempotencyIntentAdapter> Create() => [
        Adapter<CreateConversation>(command => Ordinary(command.PublicCommand,
            // Creation allocation, boundary timestamps and event identities are generated fields.
            new ConversationId("catalog-create-allocation"))),
        Adapter<AddParticipant>(command => Ordinary(command.PublicCommand, command.PublicCommand.ConversationId)),
        Adapter<ReassignConversationProject>(command => Ordinary(command.PublicCommand, command.PublicCommand.ConversationId)),
        Adapter<SetConversationRetentionPolicy>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.AuditEvidence }),
        Adapter<MarkConversationContentSensitive>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.PublicCommand.Target, command.AuditEvidence }),
        Adapter<RedactMessageContent>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.PublicCommand.Target, command.AuditEvidence }),
        Adapter<AddAgentParticipant>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.AddedAt, command.PublicCommand.OperationTimestamp }),
        Adapter<RemoveAgentParticipant>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            command.PublicCommand.ParticipantPartyId, command.PublicCommand.OperationTimestamp }),
        Adapter<AppendAgentMessage>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            Fingerprint = ConversationAgentPostFingerprint.Create(command.PublicCommand, command.PostedAt), command.PublicCommand.OperationTimestamp }),
        Adapter<ApproveConversationDeletion>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            command.PublicCommand.ApprovalReference, command.PublicCommand.SourceRevision, command.PublicCommand.OperationTimestamp, command.PublicCommand.AuditEvidence }),
        Adapter<RecordConversationDeletionDelivery>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            command.PublicCommand.Signal, command.PublicCommand.Action, command.PublicCommand.DeliveryAttemptId, command.PublicCommand.TargetVersion,
            command.PublicCommand.ExpectedDeliveryRevision, command.PublicCommand.Acknowledgement }),
        Adapter<EditConversationMessageCommand>(command => new { Metadata = SemanticMetadata(command.Metadata), command.ConversationId,
            command.MessageId, command.Text, command.OperationTimestamp }),
        Adapter<DeleteConversationMessageCommand>(command => new { Metadata = SemanticMetadata(command.Metadata), command.ConversationId,
            command.MessageId, command.OperationTimestamp })];

    private static DeclaredIdempotencyIntentAdapter<T> Adapter<T>(Func<T, object> semantic) => new(
        ConversationsCatalogDeclarations.Commands.Single(command => command.Route.Execution!.HandlerType == typeof(T).FullName),
        payload => new Dictionary<string, object?> { ["intent-sha256"] = CanonicalIntentPayload.Sha256(semantic(payload)) });

    private static object SemanticMetadata(ConversationCommandMetadata metadata) => new { metadata.SchemaVersion, metadata.TenantId,
        metadata.ActorPartyId, metadata.IdempotencyKey };

    private static object Ordinary(object command, ConversationId allocation)
    {
        ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(command, allocation);
        // The existing canonical payload owns text normalization and scoped target semantics. Portable
        // provenance remains meaningful even when it does not participate in the earlier public API retry key.
        return command switch
        {
            CreateConversationCommand request => new { fingerprint.Scope, fingerprint.PayloadFingerprint, request.ProviderCorrelation, request.CallerMetadata },
            AddParticipantCommand request => new { fingerprint.Scope, fingerprint.PayloadFingerprint, request.ProviderCorrelation },
            ReassignConversationProjectCommand request => new { fingerprint.Scope, fingerprint.PayloadFingerprint, request.CallerMetadata },
            _ => (object)new { fingerprint.Scope, fingerprint.PayloadFingerprint },
        };
    }
}
