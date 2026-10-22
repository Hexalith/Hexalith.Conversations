using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Events;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Uses SDK complete-source reads to prove source publication and receipt persistence independently.</summary>
public sealed class DeletionConformanceStreams(IAuthoritativeEventStreamReader streams)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Proves the exact approved source and its latest durable attempt before accepting a command.</summary>
    public async Task<ConversationAgentsOutcome> VerifySourceAsync(AcceptConformanceDeletion command, CancellationToken cancellationToken)
    {
        var signal = command.Signal;
        var identity = new AggregateIdentity(signal.TenantId.Value, "conversation", signal.ConversationId.Value);
        var source = await ReadAsync(identity, cancellationToken).ConfigureAwait(false);
        if (source is null || source.Head < signal.SourceRevision) return ConversationAgentsOutcome.Unavailable;
        try
        {
            StreamReadEvent item = source.Events[checked((int)signal.SourceRevision - 1)];
            if (item.EventTypeName != typeof(ConversationDeletionApprovedDomainEvent).FullName) return ConversationAgentsOutcome.Conflict;
            var approved = JsonSerializer.Deserialize<ConversationDeletionApprovedDomainEvent>(item.Payload, Json);
            if (approved?.Signal != signal || approved.AuditEvidence is null
                || approved.Metadata.TenantId != signal.TenantId || approved.Metadata.ConversationId != signal.ConversationId
                || approved.Metadata.SchemaVersion.Value != 1 || approved.Metadata.EventType != ConversationEventType.ConversationDeletionApproved)
                return ConversationAgentsOutcome.Conflict;
            ConversationDeletionDeliveryRecordedDomainEvent? latest = null;
            foreach (var entry in source.Events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.EventTypeName != typeof(ConversationDeletionDeliveryRecordedDomainEvent).FullName) continue;
                var delivery = JsonSerializer.Deserialize<ConversationDeletionDeliveryRecordedDomainEvent>(entry.Payload, Json);
                if (delivery?.Signal != signal || delivery.Metadata.TenantId != signal.TenantId
                    || delivery.Metadata.ConversationId != signal.ConversationId || entry.SequenceNumber <= signal.SourceRevision)
                    return ConversationAgentsOutcome.Conflict;
                latest = delivery;
            }
            return latest is not null && latest.Action is ConversationDeletionDeliveryAction.Attempt
                && latest.DeliveryAttemptId == command.DeliveryAttemptId && latest.TargetVersion == command.TargetVersion
                ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Unavailable;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or OverflowException)
        { cancellationToken.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
    }

    /// <summary>Reconstructs the entire persisted receipt stream; gateway admission alone cannot appear here.</summary>
    public async Task<DeletionConformanceState?> ReadReceiptAsync(AggregateIdentity identity, CancellationToken cancellationToken)
    {
        var source = await ReadAsync(identity, cancellationToken).ConfigureAwait(false);
        if (source is null) return null;
        var state = new DeletionConformanceState();
        try
        {
            foreach (var item in source.Events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.EventTypeName == typeof(ConformanceDeletionAccepted).FullName)
                {
                    var accepted = JsonSerializer.Deserialize<ConformanceDeletionAccepted>(item.Payload, Json);
                    if (accepted is null || accepted.Signal.TenantId.Value != identity.TenantId
                        || accepted.Signal.ConversationDeletionSignalId != identity.AggregateId) return null;
                    state.Apply(accepted);
                }
                else if (item.EventTypeName == typeof(ConformanceDeletionRejected).FullName)
                    state.Apply(JsonSerializer.Deserialize<ConformanceDeletionRejected>(item.Payload, Json)!);
                else return null;
            }
            cancellationToken.ThrowIfCancellationRequested();
            return state;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or OverflowException)
        { cancellationToken.ThrowIfCancellationRequested(); return null; }
    }

    private async Task<AuthoritativeEventStream?> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken)
    {
        var result = await streams.ReadAsync(identity, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.IsAuthoritative || result.Stream is not { } source || source.Identity != identity
            || source.Head < 0 || source.Events is null || source.Head != source.Events.Count
            || source.ObservedAt == default || string.IsNullOrWhiteSpace(source.ObservationId)) return null;
        for (int index = 0; index < source.Events.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = source.Events[index];
            if (item is null || item.SequenceNumber != index + 1L || item.Payload is null || item.MetadataVersion != 1
                || item.SerializationFormat != "json" || string.IsNullOrWhiteSpace(item.MessageId)
                || item.ProtectionMetadata is { } protection && (!EventStorePayloadProtectionMetadataCarrier.TryValidate(protection, out _)
                    || protection.State is not (PayloadProtectionState.Unprotected or PayloadProtectionState.Protected))) return null;
        }
        return source;
    }
}
