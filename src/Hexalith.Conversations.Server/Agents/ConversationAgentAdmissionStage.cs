// <copyright file="ConversationAgentAdmissionStage.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Events;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Current admission before pure processing, with independent approval and receiver ports.</summary>
/// <param name="authority">Current exact scope and immutable Party resolver.</param>
/// <param name="approvals">Independent deletion approval verifier, never membership authority.</param>
/// <param name="receipts">Authenticated exact source worker and receiver verifier.</param>
/// <param name="commandSources">Authenticated SDK complete-prefix verifier; never re-enters the source actor.</param>
public sealed class ConversationAgentAdmissionStage(IConversationAgentAuthority authority,
    IConversationDeletionApprovalVerifier approvals, IConversationDeletionReceiptVerifier receipts,
    IConversationCommandSourceVerifier commandSources) : IDomainServiceAdmissionStage
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public string Name => "conversations-current-agents-admission";

    /// <inheritdoc />
    public async Task<DomainServiceAdmissionResult> EvaluateAsync(DomainServiceAdmissionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommandEnvelope envelope = context.Command;
        if (envelope.Domain != "conversation")
        {
            return DomainServiceAdmissionResult.Accepted();
        }
        try
        {
            string type = NormalizeRestrictedType(envelope.CommandType);
            object? command = type switch
            {
                nameof(AddAgentParticipant) => JsonSerializer.Deserialize<AddAgentParticipant>(envelope.Payload, Options),
                nameof(AppendAgentMessage) => JsonSerializer.Deserialize<AppendAgentMessage>(envelope.Payload, Options),
                nameof(RemoveAgentParticipant) => JsonSerializer.Deserialize<RemoveAgentParticipant>(envelope.Payload, Options),
                nameof(ApproveConversationDeletion) => JsonSerializer.Deserialize<ApproveConversationDeletion>(envelope.Payload, Options),
                nameof(RecordConversationDeletionDelivery) => JsonSerializer.Deserialize<RecordConversationDeletionDelivery>(envelope.Payload, Options),
                nameof(EditConversationMessageCommand) => JsonSerializer.Deserialize<EditConversationMessageCommand>(envelope.Payload, Options),
                nameof(DeleteConversationMessageCommand) => JsonSerializer.Deserialize<DeleteConversationMessageCommand>(envelope.Payload, Options),
                _ => null
            };
            // Existing general commands require an explicitly independent operation grant. The Agent grant
            // cannot authorize arbitrary participant management through a legacy dispatch alias.
            if (command is null && RestrictedTypes.Any(t => t.Name == type))
            {
                return Reject(ConversationAgentsOutcome.Invalid);
            }
            if (command is null)
            {
                var general = await authority.AuthorizeAsync(envelope.UserId, new TenantId(envelope.TenantId),
                    new ConversationId(envelope.AggregateId), "GeneralCommand", cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return ConversationAgentQueryService.CheckAuthority(general, envelope.UserId, new TenantId(envelope.TenantId), false)
                    == ConversationAgentsOutcome.Available && !general.OrganizationIdentityConfirmed && general.PartyId is not null
                    ? DomainServiceAdmissionResult.Accepted() : Reject(ConversationAgentsOutcome.Denied);
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
                _ => throw new InvalidOperationException()
            };
            if (metadata.TenantId.Value != envelope.TenantId || conversation.Value != envelope.AggregateId)
            {
                return Reject(ConversationAgentsOutcome.Denied);
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
            var source = sourceProof.IsAuthoritative
                ? ConversationAgentSourceReader.ReplaySource(sourceProof.Stream, envelope.AggregateIdentity, cancellationToken) : null;
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
            return DomainServiceAdmissionResult.Accepted();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Reject(ConversationAgentsOutcome.Unavailable);
        }
    }

    private static readonly Type[] RestrictedTypes = [typeof(AddAgentParticipant), typeof(AppendAgentMessage),
        typeof(RemoveAgentParticipant), typeof(ApproveConversationDeletion), typeof(RecordConversationDeletionDelivery),
        typeof(EditConversationMessageCommand), typeof(DeleteConversationMessageCommand)];

    private static string NormalizeRestrictedType(string discriminator)
    {
        string[] pieces = discriminator.Split(',', 2);
        string name = pieces[0].Trim();
        Type? contract = RestrictedTypes.SingleOrDefault(t => name == t.Name || name == t.FullName);
        if (contract is null)
        {
            if (RestrictedTypes.Any(t => name.Split('.').Last() == t.Name))
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
