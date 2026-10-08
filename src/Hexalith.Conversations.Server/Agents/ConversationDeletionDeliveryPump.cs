using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>One ordered source-worker step: persist attempt, exact lookup, dispatch, and confirm durable source acknowledgement.</summary>
/// <remarks>Return Available only after authoritative source receipt lookup. An unavailable or poisoned entry is retained, never skipped.
/// A qualified host must bind private transport credentials, namespace coverage, independent receipt admission and the receiver contract.</remarks>
public sealed class ConversationDeletionDeliveryPump(IConversationClient conversations, ConfiguredConversationDeletionWorker worker,
    IConversationDeletionReceiver receiver)
{
    /// <summary>Delivers one discovered immutable publication; completion lets an ordered consumer proceed to its next offset.</summary>
    public async Task<ConversationAgentsOutcome> DeliverAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(entry);
            var publication = entry.Publication;
            if (entry.Offset <= 0 || publication is null || publication.Identity is null || publication.Identity.Domain != "conversation"
                || publication.SourceRevision <= 0) { return ConversationAgentsOutcome.Invalid; }
            var tenant = new TenantId(publication.Identity.TenantId); var conversation = new ConversationId(publication.Identity.AggregateId);
            ConversationAgentAuthorization? admitted = await worker.AuthorizeAsync(tenant, conversation, cancellationToken).ConfigureAwait(false);
            if (admitted is null) { return ConversationAgentsOutcome.Denied; }
            string? target = await receiver.CurrentTargetAsync(tenant, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(target) || target.Length > 256) { return ConversationAgentsOutcome.Unavailable; }
            var query = new ConversationDeletionSourceQuery(tenant, conversation, publication.SourceRevision, publication.PublicationId);
            ConversationDeletionSourceResult source = await conversations.GetConversationDeletionSourceAsync(query, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (source.Outcome == ConversationAgentsOutcome.Quarantined) { return source.Outcome; }
            ConversationDeletionSignal? signal = source.Signal;
            if (source.Outcome != ConversationAgentsOutcome.Available || signal is null) { return ConversationAgentsOutcome.Unavailable; }
            if (!Matches(publication, signal)) { return ConversationAgentsOutcome.Conflict; }
            if (source.Acknowledgement is not null) { return Confirmed(source) ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Quarantined; }
            if (source.LastAttemptId is { } oldAttempt)
            {
                if (string.IsNullOrWhiteSpace(source.TargetVersion)) { return ConversationAgentsOutcome.Unavailable; }
                var old = await receiver.LookupAsync(signal, oldAttempt, source.TargetVersion, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (old.Outcome == ConversationAgentsOutcome.Available)
                { return await AcknowledgeAsync(admitted, query, source, old, cancellationToken).ConfigureAwait(false); }
                if (old.Outcome != ConversationAgentsOutcome.Absent || old.Acknowledgement is not null)
                { return old.Outcome == ConversationAgentsOutcome.Conflict ? await QuarantineAsync(admitted, source, oldAttempt, source.TargetVersion, cancellationToken).ConfigureAwait(false) : ConversationAgentsOutcome.Unavailable; }
            }
            if (source.LastAttemptId is null || source.TargetVersion != target)
            {
                string attempt = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
                { "conversation-deletion-attempt-v1", signal.ConversationDeletionSignalId, target,
                    checked(source.DeliveryRevision + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) })));
                if (!await StillAuthorizedAsync(admitted, conversation, cancellationToken).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
                var command = Command(admitted, source, ConversationDeletionDeliveryAction.Attempt, attempt, target);
                await conversations.RecordConversationDeletionDeliveryAsync(command, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                source = await conversations.GetConversationDeletionSourceAsync(query, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (source.Outcome != ConversationAgentsOutcome.Available || source.Signal != signal || source.LastAttemptId != attempt
                    || source.TargetVersion != target || source.DeliveryRevision != command.ExpectedDeliveryRevision + 1)
                { return ConversationAgentsOutcome.Unavailable; }
                if (source.Acknowledgement is not null) { return Confirmed(source) ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Quarantined; }
            }
            string currentAttempt = source.LastAttemptId!;
            var outcome = await receiver.LookupAsync(signal, currentAttempt, target, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (outcome.Outcome == ConversationAgentsOutcome.Absent && outcome.Acknowledgement is null)
            {
                if (!await StillAuthorizedAsync(admitted, conversation, cancellationToken).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
                string? currentTarget = await receiver.CurrentTargetAsync(tenant, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (currentTarget != target) { return ConversationAgentsOutcome.Unavailable; }
                outcome = await receiver.SubmitAsync(signal, currentAttempt, target, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (outcome.Outcome == ConversationAgentsOutcome.Conflict)
            { return await QuarantineAsync(admitted, source, currentAttempt, target, cancellationToken).ConfigureAwait(false); }
            return outcome.Outcome == ConversationAgentsOutcome.Available
                ? await AcknowledgeAsync(admitted, query, source, outcome, cancellationToken).ConfigureAwait(false)
                : ConversationAgentsOutcome.Unavailable;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ArgumentException or JsonException)
        { cancellationToken.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
        catch (Exception) { cancellationToken.ThrowIfCancellationRequested(); throw; }
    }

    private static bool Matches(SourcePublicationDescriptor publication, ConversationDeletionSignal signal)
        => signal.TenantId.Value == publication.Identity.TenantId && signal.ConversationId.Value == publication.Identity.AggregateId
            && signal.SourceStream == publication.Identity.ActorId && signal.SourceRevision == publication.SourceRevision
            && signal.ConversationDeletionSignalId == publication.PublicationId && signal.SourceContractVersion == 1
            && publication.StableFieldsDigest == ConversationDeletionPublicationProjector.SignalDigest(signal);

    private static bool Exact(ConversationDeletionSignal signal, string target, ConversationDeletionAcknowledgement? receipt)
        => receipt is not null && receipt.SignalId == signal.ConversationDeletionSignalId && receipt.SourceRevision == signal.SourceRevision
            && receipt.ProtectedDeletionRevision > 0 && receipt.TargetVersion == target && !string.IsNullOrWhiteSpace(receipt.Evidence);

    private static bool Confirmed(ConversationDeletionSourceResult source)
        => source.Signal is { } signal && source.AcknowledgedSourceRevision == signal.SourceRevision
            && source.TargetVersion is { } target && Exact(signal, target, source.Acknowledgement);

    private async Task<bool> StillAuthorizedAsync(ConversationAgentAuthorization admitted, ConversationId conversation, CancellationToken token)
        => await worker.AuthorizeAsync(admitted.TenantId!, conversation, token).ConfigureAwait(false) == admitted;

    private static RecordConversationDeletionDeliveryCommand Command(ConversationAgentAuthorization actor, ConversationDeletionSourceResult source,
        ConversationDeletionDeliveryAction action, string attempt, string target, ConversationDeletionAcknowledgement? receipt = null)
    {
        string logicalId = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
            { "conversation-deletion-worker-v1", source.Signal!.ConversationDeletionSignalId, attempt, target, action.ToString() })));
        return new(new ConversationCommandMetadata(SchemaVersion.Current, actor.TenantId!, actor.PartyId!, logicalId,
            source.Signal.ApprovalReference, logicalId), source.Signal.ConversationId, source.Signal, action, attempt, target,
            source.DeliveryRevision, receipt);
    }

    private async Task<ConversationAgentsOutcome> AcknowledgeAsync(ConversationAgentAuthorization actor, ConversationDeletionSourceQuery query,
        ConversationDeletionSourceResult source, ConversationDeletionReceiverResult outcome, CancellationToken token)
    {
        if (!Exact(source.Signal!, source.TargetVersion!, outcome.Acknowledgement))
        { return await QuarantineAsync(actor, source, source.LastAttemptId!, source.TargetVersion!, token).ConfigureAwait(false); }
        if (!await StillAuthorizedAsync(actor, query.ConversationId, token).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
        await conversations.RecordConversationDeletionDeliveryAsync(Command(actor, source, ConversationDeletionDeliveryAction.Acknowledge,
            source.LastAttemptId!, source.TargetVersion!, outcome.Acknowledgement), token).WaitAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        ConversationDeletionSourceResult persisted = await conversations.GetConversationDeletionSourceAsync(query, token).WaitAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return persisted.Outcome == ConversationAgentsOutcome.Available && persisted.Signal == source.Signal
            && persisted.Acknowledgement == outcome.Acknowledgement && Confirmed(persisted)
            ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Unavailable;
    }

    private async Task<ConversationAgentsOutcome> QuarantineAsync(ConversationAgentAuthorization actor,
        ConversationDeletionSourceResult source, string attempt, string target, CancellationToken token)
    {
        if (!await StillAuthorizedAsync(actor, source.Signal!.ConversationId, token).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
        await conversations.RecordConversationDeletionDeliveryAsync(Command(actor, source, ConversationDeletionDeliveryAction.Quarantine,
            attempt, target), token).WaitAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        ConversationDeletionSourceResult persisted = await conversations.GetConversationDeletionSourceAsync(new(source.Signal.TenantId,
            source.Signal.ConversationId, source.Signal.SourceRevision, source.Signal.ConversationDeletionSignalId), token).WaitAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return persisted.Outcome == ConversationAgentsOutcome.Quarantined && persisted.Signal == source.Signal
            && !string.IsNullOrWhiteSpace(persisted.PoisonCode) && persisted.AcknowledgedSourceRevision == source.AcknowledgedSourceRevision
            && persisted.Acknowledgement == source.Acknowledgement ? ConversationAgentsOutcome.Quarantined : ConversationAgentsOutcome.Unavailable;
    }
}
