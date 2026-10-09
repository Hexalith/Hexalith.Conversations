using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Client.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Concrete private delivery admission: exact enrolled worker/current Party, source scope, target and independent receipt lookup.</summary>
/// <remarks>Configuration never authenticates a principal. Gateway identity, independent authority, source-prefix verification and receiver credentials remain qualified host bindings.</remarks>
public sealed class ConfiguredConversationDeletionReceiptVerifier(ConfiguredConversationDeletionWorker worker,
    IConversationDeletionReceiver receiver, TimeProvider? clock = null) : IConversationDeletionReceiptVerifier
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <inheritdoc/>
    public async Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope, RecordConversationDeletionDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        using var deadline = new AuthoritativeStreamReadDeadline(TimeSpan.FromSeconds(30), _clock, cancellationToken, _clock.GetTimestamp());
        try
        {
            deadline.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(envelope); ArgumentNullException.ThrowIfNull(command);
            if (envelope.Domain != "conversation" || envelope.TenantId != command.Metadata.TenantId.Value
                || envelope.AggregateId != command.ConversationId.Value || command.Metadata.SchemaVersion.Value != 1
                || command.Signal.TenantId != command.Metadata.TenantId || command.Signal.ConversationId != command.ConversationId
                || command.Signal.SourceStream != envelope.AggregateIdentity.ActorId || command.Signal.SourceRevision <= 0
                || command.Signal.SourceContractVersion != 1 || string.IsNullOrWhiteSpace(command.DeliveryAttemptId)
                || string.IsNullOrWhiteSpace(command.TargetVersion) || !Enum.IsDefined(command.Action))
            { return ConversationAgentsOutcome.Denied; }
            var admitted = await deadline.ReadAsync(token => worker.AuthorizeDeliveryAsync(command.Metadata.TenantId, command.ConversationId, token)).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            if (admitted is null || admitted.PrincipalId != envelope.UserId || admitted.PartyId != command.Metadata.ActorPartyId)
            { return ConversationAgentsOutcome.Denied; }
            if (command.Action == ConversationDeletionDeliveryAction.Acknowledge)
            {
                var current = await deadline.ReadAsync(token => receiver.LookupAsync(command.Signal, command.DeliveryAttemptId, command.TargetVersion, token)).ConfigureAwait(false);
                deadline.ThrowIfCancellationRequested();
                bool exact = current.Outcome == ConversationAgentsOutcome.Available && current.Acknowledgement is { } receipt
                    && receipt == command.Acknowledgement && receipt.SignalId == command.Signal.ConversationDeletionSignalId
                    && receipt.SourceRevision == command.Signal.SourceRevision && receipt.ProtectedDeletionRevision > 0
                    && receipt.TargetVersion == command.TargetVersion && !string.IsNullOrWhiteSpace(receipt.Evidence)
                    && await deadline.ReadAsync(token => worker.AuthorizeDeliveryAsync(command.Metadata.TenantId, command.ConversationId, token)).ConfigureAwait(false) == admitted
                    ;
                deadline.ThrowIfCancellationRequested();
                return exact ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Denied;
            }
            if (command.Acknowledgement is not null) { return ConversationAgentsOutcome.Denied; }
            if (command.Action == ConversationDeletionDeliveryAction.Attempt)
            {
                string? target = await deadline.ReadAsync(token => receiver.CurrentTargetAsync(command.Metadata.TenantId, token)).ConfigureAwait(false);
                deadline.ThrowIfCancellationRequested();
                if (target != command.TargetVersion) { return ConversationAgentsOutcome.Denied; }
            }
            // Only an authenticated dedicated worker can request safe quarantine; pure source replay binds immutable fields.
            var final = await deadline.ReadAsync(token => worker.AuthorizeDeliveryAsync(command.Metadata.TenantId, command.ConversationId, token)).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            return final == admitted ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Denied;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsExpired)
        { return ConversationAgentsOutcome.Unavailable; }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ArgumentException)
        { cancellationToken.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
        catch (Exception) { cancellationToken.ThrowIfCancellationRequested(); throw; }
    }
}
