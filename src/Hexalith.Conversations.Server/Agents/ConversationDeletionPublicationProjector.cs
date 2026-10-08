using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Events;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Adapts existing source-atomic Conversation approval events to safe generic discovery descriptors.</summary>
/// <remarks>No content, human authority, delivery receipt or production namespace completeness is minted here.</remarks>
public sealed class ConversationDeletionPublicationProjector : ISourcePublicationProjector
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public IReadOnlyList<SourcePublicationDescriptor> Project(AuthoritativeEventStream source, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(source);
        var replay = ConversationAgentSourceReader.ReplaySource(source, source.Identity, cancellationToken);
        if (source.Identity.Domain != "conversation" || replay is null
            || replay.Value.State.DeletionSource.Outcome == ConversationAgentsOutcome.Quarantined)
        { throw new InvalidOperationException("Conversation publication source is incompatible."); }
        var publications = new List<SourcePublicationDescriptor>();
        foreach (StreamReadEvent item in source.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.EventTypeName != typeof(ConversationDeletionApprovedDomainEvent).FullName) { continue; }
            var approved = JsonSerializer.Deserialize<ConversationDeletionApprovedDomainEvent>(item.Payload, Json)
                ?? throw new InvalidOperationException("Missing Conversation publication.");
            ConversationDeletionSignal signal = approved.Signal;
            string expectedId = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
            {
                signal.TenantId.Value, signal.ConversationId.Value, signal.ApprovalReference, "1"
            })));
            if (signal.ConversationDeletionSignalId != expectedId || signal != replay.Value.State.DeletionSource.Signal || signal.SourceRevision != item.SequenceNumber
                || signal.SourceContractVersion != 1 || string.IsNullOrWhiteSpace(signal.ConversationDeletionSignalId)
                || string.IsNullOrWhiteSpace(signal.ApprovalReference))
            { throw new InvalidOperationException("Changed Conversation publication."); }
            // Only the closed safe source Signal tuple is fingerprinted, never message/content bytes.
            string digest = SignalDigest(signal);
            publications.Add(new(signal.ConversationDeletionSignalId, source.Identity, signal.SourceRevision, item.MessageId, digest));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return publications.AsReadOnly();
    }
    internal static string SignalDigest(ConversationDeletionSignal signal)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            signal.ConversationDeletionSignalId, signal.TenantId.Value, signal.ConversationId.Value, signal.SourceStream,
            signal.SourceRevision.ToString(System.Globalization.CultureInfo.InvariantCulture), signal.ApprovalReference,
            signal.SourceContractVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
        })));
}
