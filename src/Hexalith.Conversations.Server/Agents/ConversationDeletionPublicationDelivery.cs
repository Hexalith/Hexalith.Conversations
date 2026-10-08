using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Domain adapter to the shared ordered dispatcher; only the pump's authoritatively replayed exact source acknowledgement advances the prefix.</summary>
public sealed class ConversationDeletionPublicationDelivery(ConversationDeletionDeliveryPump pump) : ISourcePublicationDelivery
{
    /// <inheritdoc/>
    public Task<SourcePublicationDeliveryStatus> LookupAcknowledgementAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
        => pump.LookupAcknowledgementAsync(entry, cancellationToken);
    /// <summary>Reuses the original signal/attempt/receipt across restart and receiver target rollover; absent enrollment/receiver authority never advances.</summary>
    public async Task<SourcePublicationDeliveryStatus> DeliverAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
        => await pump.DeliverAsync(entry, cancellationToken).ConfigureAwait(false) switch
        {
            ConversationAgentsOutcome.Available => SourcePublicationDeliveryStatus.Acknowledged,
            ConversationAgentsOutcome.Quarantined => SourcePublicationDeliveryStatus.Quarantined,
            _ => SourcePublicationDeliveryStatus.Unavailable,
        };
}
