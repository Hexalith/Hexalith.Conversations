using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Client.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>One ordered source-worker step: persist attempt, exact lookup, dispatch, and confirm durable source acknowledgement.</summary>
/// <remarks>Return Available only after authoritative source receipt lookup. An unavailable or poisoned entry is retained, never skipped.
/// A qualified host must bind private transport credentials, namespace coverage, independent receipt admission and the receiver contract.</remarks>
public sealed class ConversationDeletionDeliveryPump(IConversationClient conversations, ConfiguredConversationDeletionWorker worker,
    IConversationDeletionReceiver receiver, TimeProvider? timeProvider = null)
{
    private TimeProvider OperationClock => timeProvider ?? TimeProvider.System;
    /// <summary>Delivers one discovered immutable publication; completion lets an ordered consumer proceed to its next offset.</summary>
    public async Task<ConversationAgentsOutcome> DeliverAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
    {
        TimeProvider clock = OperationClock;
        using var deadline = new AuthoritativeStreamReadDeadline(TimeSpan.FromSeconds(30), clock, cancellationToken, clock.GetTimestamp());
        try
        {
            deadline.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(entry);
            var publication = entry.Publication;
            if (entry.Offset <= 0 || publication is null || publication.Identity is null || publication.Identity.Domain != "conversation"
                || publication.SourceRevision <= 0) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Invalid; }
            var tenant = new TenantId(publication.Identity.TenantId); var conversation = new ConversationId(publication.Identity.AggregateId);
            ConversationAgentAuthorization? admitted = await deadline.ReadAsync(providerToken => worker.AuthorizeAsync(tenant, conversation, providerToken)).ConfigureAwait(false);
            if (admitted is null) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Denied; }
            string? target = await deadline.ReadAsync(providerToken => receiver.CurrentTargetAsync(tenant, providerToken)).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(target) || target.Length > 256) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
            var query = new ConversationDeletionSourceQuery(tenant, conversation, publication.SourceRevision, publication.PublicationId);
            ConversationDeletionSourceResult source = await deadline.ReadAsync(providerToken => conversations.GetConversationDeletionSourceAsync(query, providerToken)).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            if (!await StillAuthorizedAsync(deadline, admitted, conversation, cancellationToken).ConfigureAwait(false)) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Denied; }
            if (source.Outcome == ConversationAgentsOutcome.Quarantined) { deadline.ThrowIfCancellationRequested(); return source.Outcome; }
            ConversationDeletionSignal? signal = source.Signal;
            if (source.Outcome != ConversationAgentsOutcome.Available || signal is null) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
            if (!Matches(publication, signal)) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Conflict; }
            if (source.Acknowledgement is not null) { deadline.ThrowIfCancellationRequested(); return Confirmed(source) ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Quarantined; }
            if (source.LastAttemptId is { } oldAttempt)
            {
                if (string.IsNullOrWhiteSpace(source.TargetVersion)) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
                var old = await deadline.ReadAsync(providerToken => receiver.LookupAsync(signal, oldAttempt, source.TargetVersion, providerToken)).ConfigureAwait(false);
                deadline.ThrowIfCancellationRequested();
                if (old.Outcome == ConversationAgentsOutcome.Available)
                { deadline.ThrowIfCancellationRequested(); return await AcknowledgeAsync(deadline, admitted, query, source, old, cancellationToken).ConfigureAwait(false); }
                if (old.Outcome != ConversationAgentsOutcome.Absent || old.Acknowledgement is not null)
                { deadline.ThrowIfCancellationRequested(); return old.Outcome == ConversationAgentsOutcome.Conflict ? await QuarantineAsync(deadline, admitted, source, oldAttempt, source.TargetVersion, cancellationToken).ConfigureAwait(false) : ConversationAgentsOutcome.Unavailable; }
            }
            if (source.LastAttemptId is null || source.TargetVersion != target)
            {
                string attempt = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
                { "conversation-deletion-attempt-v1", signal.ConversationDeletionSignalId, target,
                    checked(source.DeliveryRevision + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) })));
                if (!await StillAuthorizedAsync(deadline, admitted, conversation, cancellationToken).ConfigureAwait(false)) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Denied; }
                var command = Command(admitted, source, ConversationDeletionDeliveryAction.Attempt, attempt, target);
                await deadline.ReadAsync(async providerToken => { await conversations.RecordConversationDeletionDeliveryAsync(command, providerToken).ConfigureAwait(false); return true; }).ConfigureAwait(false);
                deadline.ThrowIfCancellationRequested();
                source = await deadline.ReadAsync(providerToken => conversations.GetConversationDeletionSourceAsync(query, providerToken)).ConfigureAwait(false);
                deadline.ThrowIfCancellationRequested();
                if (source.Outcome != ConversationAgentsOutcome.Available || source.Signal != signal || source.LastAttemptId != attempt
                    || source.TargetVersion != target || source.DeliveryRevision != command.ExpectedDeliveryRevision + 1)
                { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
                if (!await StillAuthorizedAsync(deadline, admitted, conversation, cancellationToken).ConfigureAwait(false)) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Denied; }
                if (source.Acknowledgement is not null) { deadline.ThrowIfCancellationRequested(); return Confirmed(source) ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Quarantined; }
            }
            string currentAttempt = source.LastAttemptId!;
            var outcome = await deadline.ReadAsync(providerToken => receiver.LookupAsync(signal, currentAttempt, target, providerToken)).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            if (outcome.Outcome == ConversationAgentsOutcome.Absent && outcome.Acknowledgement is null)
            {
                if (!await StillAuthorizedAsync(deadline, admitted, conversation, cancellationToken).ConfigureAwait(false)) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Denied; }
                string? currentTarget = await deadline.ReadAsync(providerToken => receiver.CurrentTargetAsync(tenant, providerToken)).ConfigureAwait(false);
                deadline.ThrowIfCancellationRequested();
                if (currentTarget != target) { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
                if (!await StillAuthorizedAsync(deadline, admitted, conversation, cancellationToken).ConfigureAwait(false)
                    || await deadline.ReadAsync(providerToken => worker.AuthorizeDeliveryAsync(tenant, conversation, providerToken)).ConfigureAwait(false) is null)
                { deadline.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Denied; }
                deadline.ThrowIfCancellationRequested();
                outcome = await deadline.ReadAsync(providerToken => receiver.SubmitAsync(signal, currentAttempt, target, providerToken)).ConfigureAwait(false);
                deadline.ThrowIfCancellationRequested();
            }
            if (outcome.Outcome == ConversationAgentsOutcome.Conflict)
            { deadline.ThrowIfCancellationRequested(); return await QuarantineAsync(deadline, admitted, source, currentAttempt, target, cancellationToken).ConfigureAwait(false); }
            deadline.ThrowIfCancellationRequested(); return outcome.Outcome == ConversationAgentsOutcome.Available
                ? await AcknowledgeAsync(deadline, admitted, query, source, outcome, cancellationToken).ConfigureAwait(false)
                : ConversationAgentsOutcome.Unavailable;
        }
        catch (OperationCanceledException) { cancellationToken.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ArgumentException or JsonException)
        { cancellationToken.ThrowIfCancellationRequested(); return ConversationAgentsOutcome.Unavailable; }
        catch (Exception) { cancellationToken.ThrowIfCancellationRequested(); throw; }
    }

    /// <summary>Proves the exact persisted source acknowledgement or retained pending original under current worker authority, without a receiver effect.</summary>
    public async Task<SourcePublicationDeliveryStatus> LookupAcknowledgementAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
    {
        TimeProvider clock = OperationClock;
        using var deadline = new AuthoritativeStreamReadDeadline(TimeSpan.FromSeconds(30), clock, cancellationToken, clock.GetTimestamp());
        try
        {
            deadline.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(entry);
            var publication = entry.Publication;
            if (entry.Offset <= 0 || publication?.Identity is null || publication.Identity.Domain != "conversation" || publication.SourceRevision <= 0)
            { deadline.ThrowIfCancellationRequested(); return SourcePublicationDeliveryStatus.Unavailable; }
            var tenant = new TenantId(publication.Identity.TenantId); var conversation = new ConversationId(publication.Identity.AggregateId);
            var admitted = await deadline.ReadAsync(providerToken => worker.AuthorizeAsync(tenant, conversation, providerToken)).ConfigureAwait(false);
            if (admitted is null) { deadline.ThrowIfCancellationRequested(); return SourcePublicationDeliveryStatus.Unavailable; }
            var source = await deadline.ReadAsync(providerToken => conversations.GetConversationDeletionSourceAsync(new(tenant, conversation, publication.SourceRevision, publication.PublicationId), providerToken)).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            if (source.Signal is null || !Matches(publication, source.Signal) || !await StillAuthorizedAsync(deadline, admitted, conversation, cancellationToken).ConfigureAwait(false))
            { deadline.ThrowIfCancellationRequested(); return SourcePublicationDeliveryStatus.Unavailable; }
            deadline.ThrowIfCancellationRequested();
            if (source.Outcome == ConversationAgentsOutcome.Quarantined) { deadline.ThrowIfCancellationRequested(); return SourcePublicationDeliveryStatus.Quarantined; }
            if (source.Outcome != ConversationAgentsOutcome.Available) { deadline.ThrowIfCancellationRequested(); return SourcePublicationDeliveryStatus.Unavailable; }
            if (source.Acknowledgement is not null) { deadline.ThrowIfCancellationRequested(); return Confirmed(source) ? SourcePublicationDeliveryStatus.Acknowledged : SourcePublicationDeliveryStatus.Quarantined; }
            deadline.ThrowIfCancellationRequested(); return source.AcknowledgedSourceRevision == 0 && string.IsNullOrWhiteSpace(source.PoisonCode)
                ? SourcePublicationDeliveryStatus.Pending : SourcePublicationDeliveryStatus.Unavailable;
        }
        catch (OperationCanceledException) { cancellationToken.ThrowIfCancellationRequested(); return SourcePublicationDeliveryStatus.Unavailable; }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ArgumentException or JsonException)
        { cancellationToken.ThrowIfCancellationRequested(); return SourcePublicationDeliveryStatus.Unavailable; }
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

    private async Task<bool> StillAuthorizedAsync(AuthoritativeStreamReadDeadline deadline, ConversationAgentAuthorization admitted, ConversationId conversation, CancellationToken token)
        => await deadline.ReadAsync(providerToken => worker.AuthorizeAsync(admitted.TenantId!, conversation, providerToken)).ConfigureAwait(false) == admitted;

    private static RecordConversationDeletionDeliveryCommand Command(ConversationAgentAuthorization actor, ConversationDeletionSourceResult source,
        ConversationDeletionDeliveryAction action, string attempt, string target, ConversationDeletionAcknowledgement? receipt = null)
    {
        string logicalId = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
            { "conversation-deletion-worker-v1", source.Signal!.ConversationDeletionSignalId, attempt, target, action.ToString() })));
        return new(new ConversationCommandMetadata(SchemaVersion.Current, actor.TenantId!, actor.PartyId!, logicalId,
            source.Signal.ApprovalReference, logicalId), source.Signal.ConversationId, source.Signal, action, attempt, target,
            source.DeliveryRevision, receipt);
    }

    private async Task<ConversationAgentsOutcome> AcknowledgeAsync(AuthoritativeStreamReadDeadline deadline, ConversationAgentAuthorization actor, ConversationDeletionSourceQuery query,
        ConversationDeletionSourceResult source, ConversationDeletionReceiverResult outcome, CancellationToken token)
    {
        if (!Exact(source.Signal!, source.TargetVersion!, outcome.Acknowledgement))
        { return await QuarantineAsync(deadline, actor, source, source.LastAttemptId!, source.TargetVersion!, token).ConfigureAwait(false); }
        if (!await StillAuthorizedAsync(deadline, actor, query.ConversationId, token).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
        await deadline.ReadAsync(async providerToken => { await conversations.RecordConversationDeletionDeliveryAsync(Command(actor, source, ConversationDeletionDeliveryAction.Acknowledge,
            source.LastAttemptId!, source.TargetVersion!, outcome.Acknowledgement), providerToken).ConfigureAwait(false); return true; }).ConfigureAwait(false);
        deadline.ThrowIfCancellationRequested();
        ConversationDeletionSourceResult persisted = await deadline.ReadAsync(providerToken => conversations.GetConversationDeletionSourceAsync(query, providerToken)).ConfigureAwait(false);
        deadline.ThrowIfCancellationRequested();
        if (!await StillAuthorizedAsync(deadline, actor, source.Signal!.ConversationId, token).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
        deadline.ThrowIfCancellationRequested();
        return persisted.Outcome == ConversationAgentsOutcome.Available && persisted.Signal == source.Signal
            && persisted.Acknowledgement == outcome.Acknowledgement && Confirmed(persisted)
            ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Unavailable;
    }

    private async Task<ConversationAgentsOutcome> QuarantineAsync(AuthoritativeStreamReadDeadline deadline, ConversationAgentAuthorization actor,
        ConversationDeletionSourceResult source, string attempt, string target, CancellationToken token)
    {
        if (!await StillAuthorizedAsync(deadline, actor, source.Signal!.ConversationId, token).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
        await deadline.ReadAsync(async providerToken => { await conversations.RecordConversationDeletionDeliveryAsync(Command(actor, source, ConversationDeletionDeliveryAction.Quarantine,
            attempt, target), providerToken).ConfigureAwait(false); return true; }).ConfigureAwait(false);
        deadline.ThrowIfCancellationRequested();
        ConversationDeletionSourceResult persisted = await deadline.ReadAsync(providerToken => conversations.GetConversationDeletionSourceAsync(new(source.Signal.TenantId,
            source.Signal.ConversationId, source.Signal.SourceRevision, source.Signal.ConversationDeletionSignalId), providerToken)).ConfigureAwait(false);
        deadline.ThrowIfCancellationRequested();
        if (!await StillAuthorizedAsync(deadline, actor, source.Signal!.ConversationId, token).ConfigureAwait(false)) { return ConversationAgentsOutcome.Denied; }
        deadline.ThrowIfCancellationRequested();
        return persisted.Outcome == ConversationAgentsOutcome.Quarantined && persisted.Signal == source.Signal
            && !string.IsNullOrWhiteSpace(persisted.PoisonCode) && persisted.AcknowledgedSourceRevision == source.AcknowledgedSourceRevision
            && persisted.Acknowledgement == source.Acknowledgement ? ConversationAgentsOutcome.Quarantined : ConversationAgentsOutcome.Unavailable;
    }
}
