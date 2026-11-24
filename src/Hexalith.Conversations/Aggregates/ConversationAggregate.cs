// <copyright file="ConversationAggregate.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Validation;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using PublicCreateConversationCommand = Hexalith.Conversations.Contracts.Commands.CreateConversationCommand;
using PublicConversationCommandMetadata = Hexalith.Conversations.Contracts.Commands.ConversationCommandMetadata;
using PublicAddParticipantCommand = Hexalith.Conversations.Contracts.Commands.AddParticipantCommand;
using PublicReassignConversationProjectCommand = Hexalith.Conversations.Contracts.Commands.ReassignConversationProjectCommand;
using PublicConversationProjectAssignmentOperation = Hexalith.Conversations.Contracts.Commands.ConversationProjectAssignmentOperation;
using PublicSetConversationRetentionPolicyCommand = Hexalith.Conversations.Contracts.Governance.SetConversationRetentionPolicyCommand;
using PublicMarkConversationContentSensitiveCommand = Hexalith.Conversations.Contracts.Governance.MarkConversationContentSensitiveCommand;
using PublicRedactMessageContentCommand = Hexalith.Conversations.Contracts.Governance.RedactMessageContentCommand;

namespace Hexalith.Conversations.Aggregates;

/// <summary>
/// Handles tenant-scoped conversation domain commands.
/// </summary>
public sealed partial class ConversationAggregate : EventStoreAggregate<ConversationState>
{
    /// <summary>
    /// Creates a conversation when the command is valid and the state has not already been created.
    /// </summary>
    /// <param name="command">The create-conversation domain command.</param>
    /// <param name="state">The current conversation state, when any.</param>
    /// <returns>A domain result containing one creation event or one typed rejection.</returns>
    public static DomainResult Handle(CreateConversation command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = CreateConversationValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        ArgumentNullException.ThrowIfNull(command);
        PublicCreateConversationCommand publicCommand = command.PublicCommand;
        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;

        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            ConversationEventType.ConversationCreated,
            commandMetadata.TenantId,
            command.ConversationId!,
            commandMetadata.CorrelationId,
            command.CreatedAt,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        ConversationCreatedDomainEvent created = new(
            eventMetadata,
            publicCommand.BusinessReference,
            publicCommand.ProjectId,
            publicCommand.FolderId,
            publicCommand.Label,
            commandMetadata.IdempotencyKey,
            publicCommand.ProviderCorrelation)
        {
            ResolutionScoringLabel = publicCommand.ResolutionScoringLabel,
        };

        return DomainResult.Success(new IEventPayload[] { created });
    }

    /// <summary>Creates a conversation after the human pilot authority admits the caller.</summary>
    public static DomainResult Handle(CreateHumanConversation command, ConversationState? state)
        => Handle(new CreateConversation(command.PublicCommand, command.ConversationId, command.CreatedAt, command.EventId), state);

    /// <summary>Appends a message attributed to an admitted human participant.</summary>
    /// <param name="command">The original request and its API-assigned PostedAt, a lower bound: the event commits at the
    /// later of PostedAt and the conversation's LastEventAt.</param>
    /// <param name="state">The current conversation.</param>
    /// <returns>One message event, an identical retry, or a typed refusal.</returns>
    public static DomainResult Handle(AppendMessage command, ConversationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = command.PublicCommand;
        var invalid = ConversationCommandSchemaValidation.ValidateEnvelope(request);
        if (invalid is not null)
            return DomainResult.Rejection([invalid]);
        if (request.ConversationId is null || request.MessageId is null || string.IsNullOrWhiteSpace(request.Text)
            || request.AgentProvenance is not null || string.IsNullOrWhiteSpace(command.EventId))
            return DomainResult.Rejection([new ConversationRejectedDomainEvent(
                Contracts.Errors.ConversationErrorCode.CommandValidationFailed, "invalid_message")]);

        var metadata = request.Metadata;
        if (state is null || !state.IsCreated || state.TenantId != metadata.TenantId
            || state.ConversationId != request.ConversationId || state.IsDeleted
            || request.AuthorPartyId != metadata.ActorPartyId
            || (state.CreatorPartyId != request.AuthorPartyId && !state.Participants.Any(participant =>
                participant.PartyId == request.AuthorPartyId && participant.ParticipantType == ParticipantType.Human
                && participant.ParticipantRole != ParticipantRole.Observer)))
            return DomainResult.Rejection([new ConversationWriteDenied()]);

        // Generated envelope times differ on resubmission. Only a caller-supplied time is intent.
        string fingerprint = (request.OperationTimestamp.HasValue ? "supplied:" : "unspecified:")
            + ConversationAgentPostFingerprint.Create(request, request.OperationTimestamp ?? DateTimeOffset.UnixEpoch);
        var previous = state.Messages.FirstOrDefault(message => message.MessageId == request.MessageId
            || message.IdempotencyKey == metadata.IdempotencyKey);
        if (previous is not null)
            return previous.MessageId == request.MessageId && previous.OriginalProvenance is null
                && previous.AuthorPartyId == request.AuthorPartyId && previous.IdempotencyKey == metadata.IdempotencyKey
                && previous.OriginalIntentFingerprint == fingerprint
                ? DomainResult.NoOp() : DomainResult.Rejection([new ConversationMessageConflict()]);

        // The aggregate is the serialized write boundary. An append stamped before the latest event commits at that
        // event's time, so a valid append handled after a later-stamped one commits after it. The year floor applies
        // to the API-assigned PostedAt. Agent occurrences bind their portable timestamp and keep IsValidOccurrence.
        if (state.Lifecycle != ConversationLifecycleState.Open || command.PostedAt.Year < 2000
            || state.LastEventAt is not { } lastEventAt)
            return DomainResult.Rejection([new ConversationRejectedDomainEvent(
                Contracts.Errors.ConversationErrorCode.CommandValidationFailed, "message_lifecycle_or_occurrence_invalid")]);
        DateTimeOffset occurrence = command.PostedAt > lastEventAt ? command.PostedAt : lastEventAt;

        return DomainResult.Success([new MessageAppendedDomainEvent(AgentMetadata(metadata, request.ConversationId,
            command.EventId, ConversationEventType.MessageAppended, occurrence),
            request.MessageId, request.AuthorPartyId, request.Text, null, metadata.IdempotencyKey!, fingerprint, request.ProviderCorrelation)]);
    }

    /// <summary>
    /// Adds a validated participant when the conversation state permits new membership.
    /// </summary>
    /// <param name="command">The add-participant domain command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one participant-added event or one typed rejection.</returns>
    public static DomainResult Handle(AddParticipant command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = AddParticipantValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        PublicAddParticipantCommand publicCommand = command.PublicCommand;
        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;

        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            ConversationEventType.ParticipantAdded,
            commandMetadata.TenantId,
            publicCommand.ConversationId,
            commandMetadata.CorrelationId,
            command.AddedAt,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        ParticipantAddedDomainEvent added = new(
            eventMetadata,
            publicCommand.ParticipantPartyId,
            publicCommand.ParticipantType,
            publicCommand.ParticipantRole);

        return DomainResult.Success(new IEventPayload[] { added });
    }

    /// <summary>
    /// Assigns, reassigns, or clears the conversation project reference when state permits it.
    /// </summary>
    /// <param name="command">The project reassignment domain command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one project-changed event, a no-op, or one typed rejection.</returns>
    public static DomainResult Handle(ReassignConversationProject command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = ReassignConversationProjectValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        ArgumentNullException.ThrowIfNull(state);
        PublicReassignConversationProjectCommand publicCommand = command.PublicCommand;
        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;
        ProjectId? currentProjectId = publicCommand.Target.Operation == PublicConversationProjectAssignmentOperation.Clear
            ? null
            : publicCommand.Target.ProjectId;

        if (state.ProjectId == currentProjectId)
        {
            return DomainResult.NoOp();
        }

        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            ConversationEventType.ConversationProjectChanged,
            commandMetadata.TenantId,
            publicCommand.ConversationId,
            commandMetadata.CorrelationId,
            command.ChangedAt,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        ConversationProjectChangedDomainEvent changed = new(
            eventMetadata,
            state.ProjectId,
            currentProjectId);

        return DomainResult.Success(new IEventPayload[] { changed });
    }

    /// <summary>
    /// Sets or replaces a governed retention policy when the current conversation state permits it.
    /// </summary>
    /// <param name="command">The retention policy domain command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one retention event or one typed rejection.</returns>
    public static DomainResult Handle(SetConversationRetentionPolicy command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = SetConversationRetentionPolicyValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        ArgumentNullException.ThrowIfNull(state);
        PublicSetConversationRetentionPolicyCommand publicCommand = command.PublicCommand;
        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;
        bool replacing = state.ActiveRetentionPolicy is not null;

        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            replacing ? ConversationEventType.RetentionPolicyReplaced : ConversationEventType.RetentionPolicySet,
            commandMetadata.TenantId,
            publicCommand.ConversationId,
            commandMetadata.CorrelationId,
            publicCommand.OperationTimestamp,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        if (replacing)
        {
            RetentionPolicyReplacedDomainEvent replaced = new(
                eventMetadata,
                publicCommand.PolicyReference,
                state.ActiveRetentionPolicy!.PolicyReference,
                publicCommand.Rationale,
                command.AuditEvidence,
                commandMetadata.IdempotencyKey);
            return DomainResult.Success(new IEventPayload[] { replaced });
        }

        RetentionPolicySetDomainEvent set = new(
            eventMetadata,
            publicCommand.PolicyReference,
            publicCommand.Rationale,
            command.AuditEvidence,
            commandMetadata.IdempotencyKey);
        return DomainResult.Success(new IEventPayload[] { set });
    }

    /// <summary>
    /// Marks governed conversation content as sensitive when the current conversation state permits it.
    /// </summary>
    /// <param name="command">The sensitivity mark domain command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one sensitivity event, a no-op, or one typed rejection.</returns>
    public static DomainResult Handle(MarkConversationContentSensitive command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = MarkConversationContentSensitiveValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        ArgumentNullException.ThrowIfNull(state);
        PublicMarkConversationContentSensitiveCommand publicCommand = command.PublicCommand;
        string targetKey = ConversationState.SensitivityTargetKey(publicCommand.Target);
        if (state.TryGetSensitivityMark(targetKey, out ConversationSensitivityMarkState? mark)
            && mark is not null
            && MarkConversationContentSensitiveValidation.IsCompatible(publicCommand, mark))
        {
            return DomainResult.NoOp();
        }

        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;
        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            ConversationEventType.ConversationContentMarkedSensitive,
            commandMetadata.TenantId,
            publicCommand.ConversationId,
            commandMetadata.CorrelationId,
            publicCommand.OperationTimestamp,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        ConversationContentMarkedSensitiveDomainEvent marked = new(
            eventMetadata,
            publicCommand.Target,
            publicCommand.Category,
            publicCommand.PolicyReference,
            publicCommand.Rationale,
            command.AuditEvidence,
            commandMetadata.IdempotencyKey);
        return DomainResult.Success(new IEventPayload[] { marked });
    }

    /// <summary>
    /// Records governed message-content redaction intent when the current conversation state permits it.
    /// </summary>
    /// <param name="command">The redaction domain command.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one redaction event, a no-op, or one typed rejection.</returns>
    public static DomainResult Handle(RedactMessageContent command, ConversationState? state)
    {
        ConversationRejectedDomainEvent? rejection = RedactMessageContentValidation.Validate(command, state);
        if (rejection is not null)
        {
            return DomainResult.Rejection(new IRejectionEvent[] { rejection });
        }

        ArgumentNullException.ThrowIfNull(state);
        PublicRedactMessageContentCommand publicCommand = command.PublicCommand;
        string targetKey = ConversationState.RedactionTargetKey(publicCommand.Target);
        if (state.TryGetRedaction(targetKey, out ConversationRedactionState? redaction)
            && redaction is not null
            && RedactMessageContentValidation.IsCompatible(publicCommand, redaction))
        {
            return DomainResult.NoOp();
        }

        PublicConversationCommandMetadata commandMetadata = publicCommand.Metadata;
        ConversationEventMetadata eventMetadata = new(
            commandMetadata.SchemaVersion,
            command.EventId,
            ConversationEventType.MessageContentRedacted,
            commandMetadata.TenantId,
            publicCommand.ConversationId,
            commandMetadata.CorrelationId,
            publicCommand.OperationTimestamp,
            commandMetadata.ActorPartyId,
            commandMetadata.CausationId);

        MessageContentRedactedDomainEvent redacted = new(
            eventMetadata,
            publicCommand.Target,
            publicCommand.Category,
            publicCommand.PolicyReference,
            publicCommand.Rationale,
            command.AuditEvidence,
            commandMetadata.IdempotencyKey);
        return DomainResult.Success(new IEventPayload[] { redacted });
    }
}
