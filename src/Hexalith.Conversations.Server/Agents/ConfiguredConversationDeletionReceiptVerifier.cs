using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Concrete private delivery admission: exact enrolled worker/current Party, source scope, target and independent receipt lookup.</summary>
/// <remarks>Configuration never authenticates a principal. Gateway identity, independent authority, source-prefix verification and receiver credentials remain qualified host bindings.</remarks>
public sealed class ConfiguredConversationDeletionReceiptVerifier(ConfiguredConversationDeletionWorker worker,
    IConversationDeletionReceiver receiver) : IConversationDeletionReceiptVerifier
{
    /// <inheritdoc/>
    public async Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope, RecordConversationDeletionDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(envelope); ArgumentNullException.ThrowIfNull(command);
            if (envelope.Domain != "conversation" || envelope.TenantId != command.Metadata.TenantId.Value
                || envelope.AggregateId != command.ConversationId.Value || command.Metadata.SchemaVersion.Value != 1
                || command.Signal.TenantId != command.Metadata.TenantId || command.Signal.ConversationId != command.ConversationId
                || command.Signal.SourceStream != envelope.AggregateIdentity.ActorId || command.Signal.SourceRevision <= 0
                || command.Signal.SourceContractVersion != 1 || string.IsNullOrWhiteSpace(command.DeliveryAttemptId)
                || string.IsNullOrWhiteSpace(command.TargetVersion) || !Enum.IsDefined(command.Action))
            { return ConversationAgentsOutcome.Denied; }
            var admitted = await worker.AuthorizeDeliveryAsync(command.Metadata.TenantId, command.ConversationId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (admitted is null || admitted.PrincipalId != envelope.UserId || admitted.PartyId != command.Metadata.ActorPartyId)
            { return ConversationAgentsOutcome.Denied; }
            if (command.Action == ConversationDeletionDeliveryAction.Acknowledge)
            {
                var current = await receiver.LookupAsync(command.Signal, command.DeliveryAttemptId, command.TargetVersion, cancellationToken)
                    .WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return current.Outcome == ConversationAgentsOutcome.Available && current.Acknowledgement is { } receipt
                    && receipt == command.Acknowledgement && receipt.SignalId == command.Signal.ConversationDeletionSignalId
                    && receipt.SourceRevision == command.Signal.SourceRevision && receipt.ProtectedDeletionRevision > 0
                    && receipt.TargetVersion == command.TargetVersion && !string.IsNullOrWhiteSpace(receipt.Evidence)
                    ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Denied;
            }
            if (command.Acknowledgement is not null) { return ConversationAgentsOutcome.Denied; }
            if (command.Action == ConversationDeletionDeliveryAction.Attempt)
            {
                string? target = await receiver.CurrentTargetAsync(command.Metadata.TenantId, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (target != command.TargetVersion) { return ConversationAgentsOutcome.Denied; }
            }
            // Only an authenticated dedicated worker can request safe quarantine; pure source replay binds immutable fields.
            return ConversationAgentsOutcome.Available;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ArgumentException)
        { cancellationToken.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
        catch (Exception) { cancellationToken.ThrowIfCancellationRequested(); throw; }
    }
}
