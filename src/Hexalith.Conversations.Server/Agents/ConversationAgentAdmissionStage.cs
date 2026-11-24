// <copyright file="ConversationAgentAdmissionStage.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Current admission before pure processing, with independent approval and receiver ports.</summary>
/// <param name="authority">Current exact scope and immutable Party resolver.</param>
/// <param name="approvals">Independent deletion approval verifier, never membership authority.</param>
/// <param name="receipts">Authenticated exact source worker and receiver verifier.</param>
/// <param name="pilot">Independent write authority installed by the pilot command owner.</param>
/// <param name="commandSources">Authenticated SDK complete-prefix verifier; never re-enters the source actor.</param>
public sealed class ConversationAgentAdmissionStage(IConversationAgentAuthority authority,
    IConversationDeletionApprovalVerifier approvals, IConversationDeletionReceiptVerifier receipts,
    IConversationCommandSourceVerifier commandSources, ConversationPilotWriteAdmission? pilot = null) : IDomainServiceAdmissionStage
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public string Name => "conversations-current-agents-admission";

    /// <inheritdoc />
    public async Task<DomainServiceAdmissionResult> EvaluateAsync(DomainServiceAdmissionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        CommandEnvelope envelope = context.Command;
        if (envelope.Domain != "conversation")
        {
            return DomainServiceAdmissionResult.Accepted();
        }
        try
        {
            string type = NormalizeCommandType(envelope.CommandType);
            object? command = type switch
            {
                nameof(AddAgentParticipant) => JsonSerializer.Deserialize<AddAgentParticipant>(envelope.Payload, Options),
                nameof(AppendAgentMessage) => JsonSerializer.Deserialize<AppendAgentMessage>(envelope.Payload, Options),
                nameof(RemoveAgentParticipant) => JsonSerializer.Deserialize<RemoveAgentParticipant>(envelope.Payload, Options),
                nameof(ApproveConversationDeletion) => JsonSerializer.Deserialize<ApproveConversationDeletion>(envelope.Payload, Options),
                nameof(RecordConversationDeletionDelivery) => JsonSerializer.Deserialize<RecordConversationDeletionDelivery>(envelope.Payload, Options),
                nameof(EditConversationMessageCommand) => JsonSerializer.Deserialize<EditConversationMessageCommand>(envelope.Payload, Options),
                nameof(DeleteConversationMessageCommand) => JsonSerializer.Deserialize<DeleteConversationMessageCommand>(envelope.Payload, Options),
                nameof(AppendMessage) => JsonSerializer.Deserialize<AppendMessage>(envelope.Payload, Options),
                nameof(CreateHumanConversation) => JsonSerializer.Deserialize<CreateHumanConversation>(envelope.Payload, Options),
                nameof(CreateConversation) => JsonSerializer.Deserialize<CreateConversation>(envelope.Payload, Options),
                nameof(AddParticipant) => JsonSerializer.Deserialize<AddParticipant>(envelope.Payload, Options),
                nameof(ReassignConversationProject) => JsonSerializer.Deserialize<ReassignConversationProject>(envelope.Payload, Options),
                nameof(SetConversationRetentionPolicy) => JsonSerializer.Deserialize<SetConversationRetentionPolicy>(envelope.Payload, Options),
                nameof(MarkConversationContentSensitive) => JsonSerializer.Deserialize<MarkConversationContentSensitive>(envelope.Payload, Options),
                nameof(RedactMessageContent) => JsonSerializer.Deserialize<RedactMessageContent>(envelope.Payload, Options),
                _ => null
            };
            if (command is null)
            {
                return Reject(ConversationAgentsOutcome.Invalid);
            }
            var (metadata, conversation) = command switch
            {
                AddAgentParticipant c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                AppendAgentMessage c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                RemoveAgentParticipant c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                ApproveConversationDeletion c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                RecordConversationDeletionDelivery c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                EditConversationMessageCommand c => (c.Metadata, c.ConversationId),
                DeleteConversationMessageCommand c => (c.Metadata, c.ConversationId),
                AppendMessage c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                CreateHumanConversation c => (c.PublicCommand.Metadata, c.ConversationId),
                CreateConversation c => (c.PublicCommand.Metadata, c.ConversationId),
                AddParticipant c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                ReassignConversationProject c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                SetConversationRetentionPolicy c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                MarkConversationContentSensitive c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                RedactMessageContent c => (c.PublicCommand.Metadata, c.PublicCommand.ConversationId),
                _ => throw new InvalidOperationException()
            };
            if (metadata.TenantId.Value != envelope.TenantId || conversation is null || conversation.Value != envelope.AggregateId)
            {
                return Reject(ConversationAgentsOutcome.Denied);
            }
            if (command is AppendMessage or CreateHumanConversation)
            {
                if (pilot is null || command is AppendMessage append && append.PublicCommand.AuthorPartyId != metadata.ActorPartyId)
                    return DomainServiceAdmissionResult.Rejected([new ConversationWriteDenied()]);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (command is AppendMessage posted && (posted.PostedAt > now || posted.EventId !=
                        PilotCommandIdentity.Allocate(metadata, envelope.UserId, "message-event", conversation.Value))
                    || command is CreateHumanConversation created && (created.CreatedAt > now
                        || created.EventId != PilotCommandIdentity.Allocate(metadata, envelope.UserId, "created-event")
                        || conversation.Value != PilotCommandIdentity.Allocate(metadata, envelope.UserId, "conversation")))
                    return DomainServiceAdmissionResult.Rejected([new ConversationRejectedDomainEvent(
                        ConversationErrorCode.CommandValidationFailed, "invalid_owner_allocation")]);
                return await pilot.AuthorizeAsync(envelope.UserId, metadata, cancellationToken).ConfigureAwait(false);
            }
            // A general-operation grant authorizes only the authenticated human Party and exact payload scope.
            if (command is CreateConversation or AddParticipant or ReassignConversationProject
                or SetConversationRetentionPolicy or MarkConversationContentSensitive or RedactMessageContent)
            {
                var general = await authority.AuthorizeAsync(envelope.UserId, metadata.TenantId,
                    conversation, "GeneralCommand", cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return ConversationAgentQueryService.CheckAuthority(general, envelope.UserId, metadata.TenantId, false)
                    == ConversationAgentsOutcome.Available && !general.OrganizationIdentityConfirmed && general.PartyId is not null
                    && general.PartyId == metadata.ActorPartyId
                    ? DomainServiceAdmissionResult.Accepted() : Reject(ConversationAgentsOutcome.Denied);
            }
            bool independent = command is ApproveConversationDeletion or RecordConversationDeletionDelivery;
            bool service = command is AddAgentParticipant or AppendAgentMessage or RemoveAgentParticipant;
            string operation = service ? type : "HumanMessageMutation";
            ConversationAgentAuthorization? admitted = null;
            ConversationAgentsOutcome outcome;
            if (command is ApproveConversationDeletion approval)
            {
                outcome = await approvals.VerifyAsync(envelope, approval.PublicCommand, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            else if (command is RecordConversationDeletionDelivery delivery)
            {
                outcome = await receipts.VerifyAsync(envelope, delivery.PublicCommand, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            else
            {
                admitted = await authority.AuthorizeAsync(envelope.UserId, metadata.TenantId, conversation, operation, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                outcome = ConversationAgentQueryService.CheckAuthority(admitted, envelope.UserId, metadata.TenantId, service);
                if (outcome == ConversationAgentsOutcome.Available && (admitted.PartyId != metadata.ActorPartyId
                    || (!service && (admitted.OrganizationIdentityConfirmed || admitted.PartyId is null))))
                {
                    outcome = ConversationAgentsOutcome.Denied;
                }
                if (outcome == ConversationAgentsOutcome.Available && command is AddAgentParticipant add
                    && add.PublicCommand.ParticipantPartyId != admitted.PartyId)
                {
                    outcome = ConversationAgentsOutcome.Denied;
                }
                if (outcome == ConversationAgentsOutcome.Available && command is AddAgentParticipant shape
                    && (shape.PublicCommand.ParticipantType != ParticipantType.AiAgent || shape.PublicCommand.ParticipantRole != ParticipantRole.Member))
                {
                    outcome = ConversationAgentsOutcome.Conflict;
                }
                if (outcome == ConversationAgentsOutcome.Available && command is RemoveAgentParticipant remove
                    && remove.PublicCommand.ParticipantPartyId != admitted.PartyId)
                {
                    outcome = ConversationAgentsOutcome.Denied;
                }
                if (outcome == ConversationAgentsOutcome.Available && command is AppendAgentMessage post
                    && post.PublicCommand.AuthorPartyId != admitted.PartyId)
                {
                    outcome = ConversationAgentsOutcome.Denied;
                }
            }
            // Available verifies authentic source-worker/receiver evidence; pure processing compares the immutable source fields.
            if (outcome != ConversationAgentsOutcome.Available)
            {
                return Reject(outcome);
            }
            var sourceProof = await commandSources.VerifyAsync(context.Request, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!sourceProof.IsAuthoritative)
            {
                return Reject(ConversationAgentsOutcome.Unavailable);
            }

            var source = ConversationAgentSourceReader.ReplaySource(sourceProof.Stream, envelope.AggregateIdentity,
                cancellationToken, out var eventIdentities);
            if (source is null || !MatchesCompleteState(context.CurrentState, source.Value.Source))
            {
                return Reject(ConversationAgentsOutcome.Unavailable);
            }
            if (!independent)
            {
                var current = await authority.AuthorizeAsync(envelope.UserId, metadata.TenantId, conversation, operation, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                outcome = ConversationAgentQueryService.CheckAuthority(current, envelope.UserId, metadata.TenantId, service);
                if (outcome != ConversationAgentsOutcome.Available)
                {
                    return Reject(outcome);
                }
                if (admitted != current)
                {
                    return Reject(ConversationAgentsOutcome.Unavailable);
                }
            }
            else if (command is ApproveConversationDeletion currentApproval)
            {
                outcome = await approvals.VerifyAsync(envelope, currentApproval.PublicCommand, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (outcome != ConversationAgentsOutcome.Available)
                {
                    return Reject(outcome);
                }
            }
            else if (command is RecordConversationDeletionDelivery delivery)
            {
                outcome = await receipts.VerifyAsync(envelope, delivery.PublicCommand, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (outcome != ConversationAgentsOutcome.Available)
                {
                    return Reject(outcome);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (command is AppendAgentMessage posting && !source.Value.State.Messages.Any(message =>
                message.MessageId == posting.PublicCommand.MessageId || message.IdempotencyKey == metadata.IdempotencyKey))
            {
                if (posting.PublicCommand.AgentProvenance is { } provenance
                    && (provenance.HumanEdited || provenance.EditedByPartyId is not null))
                {
                    return Reject(ConversationAgentsOutcome.Denied);
                }

                if (posting.EventId != AgentCommandIntentId.Create(metadata, conversation, "posting")
                    || eventIdentities.Contains(posting.EventId))
                {
                    return Reject(ConversationAgentsOutcome.Conflict);
                }
            }

            if (command is RemoveAgentParticipant removal
                && !source.Value.State.WasAgentRemoved(removal.PublicCommand.ParticipantPartyId)
                && (removal.EventId != AgentCommandIntentId.Create(metadata, conversation, "removal")
                    || eventIdentities.Contains(removal.EventId)))
            {
                return Reject(ConversationAgentsOutcome.Conflict);
            }

            var (proposedIdentity, purpose) = GetProposedEventIdentity(command, source.Value.State);
            if (proposedIdentity is not null && (eventIdentities.Contains(proposedIdentity)
                || (purpose is not null && proposedIdentity != AgentCommandIntentId.Create(metadata, conversation, purpose))))
            {
                return Reject(ConversationAgentsOutcome.Conflict);
            }

            return DomainServiceAdmissionResult.Accepted();
        }
        catch (Exception ex) when (ex is not ConversationWriteAuthorityUnavailableException && !cancellationToken.IsCancellationRequested)
        {
            return Reject(ConversationAgentsOutcome.Unavailable);
        }
    }

    private static (string? Identity, string? Purpose) GetProposedEventIdentity(object command, ConversationState state)
    {
        // Evaluate the same pure handler so a no-op or refusal never consumes an event identity.
        // Delivery may derive a quarantine identity instead of emitting the supplied identity.
        var result = command switch
        {
            AddAgentParticipant membership => ConversationAggregate.Handle(membership, state),
            ApproveConversationDeletion approval => ConversationAggregate.Handle(approval, state),
            RecordConversationDeletionDelivery delivery => ConversationAggregate.Handle(delivery, state),
            EditConversationMessageCommand edit => ConversationAggregate.Handle(edit, state),
            DeleteConversationMessageCommand delete => ConversationAggregate.Handle(delete, state),
            _ => null,
        };
        return result?.Events.SingleOrDefault() switch
        {
            ParticipantAddedDomainEvent membership => (membership.Metadata.EventId, "membership"),
            ConversationDeletionApprovedDomainEvent approval => (approval.Metadata.EventId, "approval"),
            ConversationDeletionDeliveryRecordedDomainEvent delivery => (delivery.Metadata.EventId,
                delivery.Action == ConversationDeletionDeliveryAction.Quarantine ? null : "delivery"),
            MessageEditedDomainEvent edit => (edit.Metadata.EventId, null),
            MessageDeletedDomainEvent delete => (delete.Metadata.EventId, null),
            _ => (null, null),
        };
    }

    private static readonly Type[] RestrictedTypes = [typeof(AddAgentParticipant), typeof(AppendAgentMessage),
        typeof(RemoveAgentParticipant), typeof(ApproveConversationDeletion), typeof(RecordConversationDeletionDelivery),
        typeof(EditConversationMessageCommand), typeof(DeleteConversationMessageCommand)];

    private static readonly Type[] KnownTypes = [.. RestrictedTypes, typeof(CreateConversation), typeof(CreateHumanConversation), typeof(AppendMessage), typeof(AddParticipant),
        typeof(ReassignConversationProject), typeof(SetConversationRetentionPolicy), typeof(MarkConversationContentSensitive),
        typeof(RedactMessageContent)];

    private static string NormalizeCommandType(string discriminator)
    {
        string[] pieces = discriminator.Split(',', 2);
        string name = pieces[0].Trim();
        Type? contract = KnownTypes.SingleOrDefault(t => name == t.Name || name == t.FullName);
        if (contract is null)
        {
            if (KnownTypes.Any(t => name.Split('.').Last() == t.Name))
            {
                throw new JsonException("Unsupported restricted command alias.");
            }
            return name;
        }
        if (pieces.Length == 2)
        {
            var assembly = new System.Reflection.AssemblyName(pieces[1].Trim());
            if (name != contract.FullName || assembly.Name != contract.Assembly.GetName().Name)
            {
                throw new JsonException("Unsupported restricted assembly alias.");
            }
        }
        return contract.Name;
    }

    private static bool MatchesCompleteState(object? input, AuthoritativeEventStream source)
    {
        DomainServiceCurrentState? current = input as DomainServiceCurrentState;
        if (input is JsonElement { ValueKind: JsonValueKind.Object } json)
        {
            current = json.Deserialize<DomainServiceCurrentState>(Options);
        }
        if (current is null || current.SnapshotState is not null || current.LastSnapshotSequence != 0
            || current.CurrentSequence != source.Head || current.Events.Count != source.Events.Count)
        {
            return false;
        }
        for (int index = 0; index < current.Events.Count; index++)
        {
            var e = current.Events[index];
            var s = source.Events[index];
            if (e.Metadata.TenantId != source.Identity.TenantId || e.Metadata.Domain != source.Identity.Domain
                || e.Metadata.AggregateId != source.Identity.AggregateId || e.Metadata.SequenceNumber != s.SequenceNumber
                || e.Metadata.MessageId != s.MessageId || e.Metadata.EventTypeName != s.EventTypeName
                || e.Metadata.MetadataVersion != 1 || e.Metadata.SerializationFormat != "json"
                || e.Metadata.EventContractType is not null || e.Metadata.PayloadVersion is not null
                || !e.Payload.AsSpan().SequenceEqual(s.Payload))
            {
                return false;
            }
        }
        return true;
    }

    private static DomainServiceAdmissionResult Reject(ConversationAgentsOutcome outcome)
        => DomainServiceAdmissionResult.Rejected([new ConversationRejectedDomainEvent(
            ConversationErrorCode.CommandValidationFailed, $"agents-{outcome}")]);
}
