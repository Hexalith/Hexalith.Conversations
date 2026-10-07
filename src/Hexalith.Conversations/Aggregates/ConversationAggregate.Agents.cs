// <copyright file="ConversationAggregate.Agents.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Idempotency;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Aggregates;

/// <summary>Pure restricted service intents; current authentication belongs to admission.</summary>
public sealed partial class ConversationAggregate
{
    /// <summary>Accepts one immutable Agent Member membership and exact replay retries.</summary>
    /// <param name="command">Admitted immutable membership intent.</param>
    /// <param name="state">Complete replayed source.</param>
    /// <returns>One membership event, no-op or safe rejection.</returns>
    public static DomainResult Handle(AddAgentParticipant command, ConversationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        AddParticipantCommand request = command.PublicCommand;
        DomainResult? rejection = CheckAgentSource(request.Metadata, request.ConversationId, state);
        if (rejection is not null)
        {
            return rejection;
        }
        if (request.Metadata.ActorPartyId != request.ParticipantPartyId)
        {
            return AgentReject(ConversationAgentsOutcome.Denied);
        }
        if (state!.WasAgentRemoved(request.ParticipantPartyId))
        {
            return AgentReject(ConversationAgentsOutcome.PrincipalRemovedFromConversation);
        }
        if (request.ParticipantType != ParticipantType.AiAgent || request.ParticipantRole != ParticipantRole.Member)
        {
            return AgentReject(ConversationAgentsOutcome.Conflict);
        }
        var existing = state.Participants.FirstOrDefault(p => p.PartyId == request.ParticipantPartyId);
        if (existing is not null)
        {
            return existing.ParticipantType == request.ParticipantType && existing.ParticipantRole == request.ParticipantRole
            ? DomainResult.NoOp() : AgentReject(ConversationAgentsOutcome.Conflict);
        }
        if (state.Lifecycle != ConversationLifecycleState.Open || !IsValidOccurrence(command.AddedAt, state))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        return AgentSuccess(new ParticipantAddedDomainEvent(AgentMetadata(request.Metadata, request.ConversationId,
            command.EventId, ConversationEventType.ParticipantAdded, command.AddedAt),
            request.ParticipantPartyId, request.ParticipantType, request.ParticipantRole));
    }

    /// <summary>Removes only the immutable Agent Member and persists a permanent tombstone.</summary>
    /// <param name="command">Admitted removal intent.</param>
    /// <param name="state">Complete source.</param>
    /// <returns>One removal fact or exact no-op.</returns>
    public static DomainResult Handle(RemoveAgentParticipant command, ConversationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = command.PublicCommand;
        DomainResult? rejection = CheckAgentSource(request.Metadata, request.ConversationId, state);
        if (rejection is not null)
        {
            return rejection;
        }
        if (request.Metadata.ActorPartyId != request.ParticipantPartyId)
        {
            return AgentReject(ConversationAgentsOutcome.Denied);
        }
        if (state!.WasAgentRemoved(request.ParticipantPartyId))
        {
            return DomainResult.NoOp();
        }
        var existing = state.Participants.FirstOrDefault(p => p.PartyId == request.ParticipantPartyId);
        if (existing is not null && (existing.ParticipantType != ParticipantType.AiAgent || existing.ParticipantRole != ParticipantRole.Member))
        {
            return AgentReject(ConversationAgentsOutcome.Conflict);
        }
        if (!IsValidOccurrence(request.OperationTimestamp, state))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        return AgentSuccess(new AgentParticipantRemovedDomainEvent(AgentMetadata(request.Metadata, request.ConversationId,
            command.EventId, ConversationEventType.AgentParticipantRemoved, request.OperationTimestamp), request.ParticipantPartyId));
    }

    /// <summary>Preserves deterministic MessageId and immutable original intent across edits and retry.</summary>
    /// <param name="command">Admitted posting intent.</param>
    /// <param name="state">Complete source.</param>
    /// <returns>One original message or exact no-op.</returns>
    public static DomainResult Handle(AppendAgentMessage command, ConversationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = command.PublicCommand;
        DomainResult? rejection = CheckAgentSource(request.Metadata, request.ConversationId, state);
        if (rejection is not null)
        {
            return rejection;
        }
        if (request.Metadata.ActorPartyId != request.AuthorPartyId)
        {
            return AgentReject(ConversationAgentsOutcome.Denied);
        }
        if (state!.WasAgentRemoved(request.AuthorPartyId))
        {
            return AgentReject(ConversationAgentsOutcome.PrincipalRemovedFromConversation);
        }
        if (!state.HasParticipant(request.AuthorPartyId, ParticipantType.AiAgent, ParticipantRole.Member))
        {
            return AgentReject(ConversationAgentsOutcome.Denied);
        }
        if (string.IsNullOrWhiteSpace(request.Metadata.IdempotencyKey) || string.IsNullOrWhiteSpace(request.Text)
            || request.AgentProvenance is not { AiGenerated: true } provenance
            || string.IsNullOrWhiteSpace(provenance.AgentCallTraceReference)
            || provenance.HumanEdited != (provenance.EditedByPartyId is not null))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        ConversationMessage? existing = state.Messages.FirstOrDefault(m => m.MessageId == request.MessageId
            || m.IdempotencyKey == request.Metadata.IdempotencyKey);
        if (existing is not null)
        {
            return existing.MessageId == request.MessageId && existing.AuthorPartyId == request.AuthorPartyId
                && existing.IdempotencyKey == request.Metadata.IdempotencyKey && existing.OriginalText == request.Text
                && existing.OriginalProvenance == provenance && existing.CreatedAt == command.PostedAt
                && existing.OriginalIntentFingerprint == PostFingerprint(request, command.PostedAt)
                ? DomainResult.NoOp() : AgentReject(ConversationAgentsOutcome.Conflict);
        }
        if (state.Lifecycle != ConversationLifecycleState.Open || !IsValidOccurrence(command.PostedAt, state))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        return AgentSuccess(new MessageAppendedDomainEvent(AgentMetadata(request.Metadata, request.ConversationId,
            command.EventId, ConversationEventType.MessageAppended, command.PostedAt),
            request.MessageId, request.AuthorPartyId, request.Text, provenance, request.Metadata.IdempotencyKey,
            PostFingerprint(request, command.PostedAt), request.ProviderCorrelation));
    }

    /// <summary>Applies a current message edit only after independent human admission.</summary>
    /// <param name="request">Exact message edit.</param>
    /// <param name="state">Complete source.</param>
    /// <returns>The current content mutation event.</returns>
    public static DomainResult Handle(EditConversationMessageCommand request, ConversationState? state)
    {
        DomainResult? rejection = CheckAgentSource(request.Metadata, request.ConversationId, state);
        if (rejection is not null)
        {
            return rejection;
        }
        var message = state!.Messages.FirstOrDefault(m => m.MessageId == request.MessageId);
        if (message is null)
        {
            return AgentReject(ConversationAgentsOutcome.Absent);
        }
        if (message.Deleted || string.IsNullOrWhiteSpace(request.Text) || !IsValidOccurrence(request.OperationTimestamp, state))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        if (message.Text == request.Text && message.Provenance?.EditedByPartyId == request.Metadata.ActorPartyId)
        {
            return DomainResult.NoOp();
        }
        return AgentSuccess(new MessageEditedDomainEvent(AgentMetadata(request.Metadata, request.ConversationId,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
            {
                Type = "edit",
                Request = request
            }))), ConversationEventType.MessageEdited, request.OperationTimestamp),
            request.MessageId, request.Text, request.Metadata.ActorPartyId));
    }

    /// <summary>Deletes current message content only after independent human admission.</summary>
    /// <param name="request">Exact deletion.</param>
    /// <param name="state">Complete source.</param>
    /// <returns>One message deletion event.</returns>
    public static DomainResult Handle(DeleteConversationMessageCommand request, ConversationState? state)
    {
        DomainResult? rejection = CheckAgentSource(request.Metadata, request.ConversationId, state);
        if (rejection is not null)
        {
            return rejection;
        }
        var message = state!.Messages.FirstOrDefault(m => m.MessageId == request.MessageId);
        if (message is null)
        {
            return AgentReject(ConversationAgentsOutcome.Absent);
        }
        if (message.Deleted)
        {
            return DomainResult.NoOp();
        }
        if (!IsValidOccurrence(request.OperationTimestamp, state))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        return AgentSuccess(new MessageDeletedDomainEvent(AgentMetadata(request.Metadata, request.ConversationId,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
            {
                Type = "delete",
                Request = request
            }))), ConversationEventType.MessageDeleted, request.OperationTimestamp), request.MessageId));
    }

    /// <summary>Atomically approves deletion and publishes one immutable original source signal.</summary>
    /// <param name="command">Independently approved intent.</param>
    /// <param name="state">Complete source, including every rejection position.</param>
    /// <returns>One durable approval/publication fact.</returns>
    public static DomainResult Handle(ApproveConversationDeletion command, ConversationState? state)
    {
        var request = command.PublicCommand;
        DomainResult? rejection = CheckAgentSource(request.Metadata, request.ConversationId, state, allowDeleted: true);
        if (rejection is not null)
        {
            return rejection;
        }
        if (request.AuditEvidence is null || request.AuditEvidence.CapturedAt > request.OperationTimestamp)
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        if (state!.IsDeleted)
        {
            return state.DeletionSource.Signal!.ApprovalReference == request.ApprovalReference
            && state.DeletionSource.Signal.SourceRevision == request.SourceRevision + 1
            && state.DeletionApprovedAt == request.OperationTimestamp && state.DeletionApprover == request.Metadata.ActorPartyId && state.DeletionAuditEvidence == request.AuditEvidence
            ? DomainResult.NoOp() : AgentReject(ConversationAgentsOutcome.Conflict);
        }
        if (request.SourceRevision != state.SourceRevision)
        {
            return AgentReject(ConversationAgentsOutcome.Conflict);
        }
        if (string.IsNullOrWhiteSpace(request.ApprovalReference) || !IsValidOccurrence(request.OperationTimestamp, state))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        string signalId = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new[] { request.Metadata.TenantId.Value, request.ConversationId.Value, request.ApprovalReference, "1" })));
        var signal = new ConversationDeletionSignal(signalId, request.Metadata.TenantId, request.ConversationId,
            ConversationState.AgentSourceStream(request.Metadata.TenantId, request.ConversationId),
            checked(state.SourceRevision + 1), request.ApprovalReference);
        return AgentSuccess(new ConversationDeletionApprovedDomainEvent(AgentMetadata(request.Metadata, request.ConversationId,
            command.EventId, ConversationEventType.ConversationDeletionApproved, request.OperationTimestamp), signal, request.AuditEvidence));
    }

    /// <summary>Records retry or exact authenticated acknowledgement; changed evidence durably quarantines.</summary>
    /// <param name="command">Source-worker admitted transition.</param>
    /// <param name="state">Complete source with durable original signal.</param>
    /// <returns>One delivery transition, exact no-op or safe refusal.</returns>
    public static DomainResult Handle(RecordConversationDeletionDelivery command, ConversationState? state)
    {
        var request = command.PublicCommand;
        DomainResult? rejection = CheckAgentSource(request.Metadata, request.ConversationId, state, allowDeleted: true);
        if (rejection is not null)
        {
            return rejection;
        }
        var current = state!.DeletionSource;
        if (current.Signal is null)
        {
            return AgentReject(ConversationAgentsOutcome.Absent);
        }
        if (current.Outcome == ConversationAgentsOutcome.Quarantined)
        {
            return AgentReject(ConversationAgentsOutcome.Quarantined);
        }
        if (!Enum.IsDefined(request.Action) || string.IsNullOrWhiteSpace(request.DeliveryAttemptId)
            || string.IsNullOrWhiteSpace(request.TargetVersion))
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        bool poison = request.Signal != current.Signal || request.Action == ConversationDeletionDeliveryAction.Quarantine
            || (request.Action != ConversationDeletionDeliveryAction.Acknowledge && request.Acknowledgement is not null)
            || (request.Action == ConversationDeletionDeliveryAction.Acknowledge
                && !ConversationState.IsExactAcknowledgement(current.Signal, request.TargetVersion, request.Acknowledgement));
        if (!poison && current.Acknowledgement is not null)
        {
            return request.Action != ConversationDeletionDeliveryAction.Acknowledge || current.Acknowledgement == request.Acknowledgement
            ? DomainResult.NoOp() : AgentQuarantine(command, state, "changed-acknowledgement");
        }
        if (poison)
        {
            return AgentQuarantine(command, state, "source-or-receipt-conflict");
        }
        string? previousTarget = state.DeliveryAttemptTarget(request.DeliveryAttemptId);
        if (previousTarget is not null && previousTarget != request.TargetVersion)
        {
            return AgentQuarantine(command, state, "attempt-target-conflict");
        }
        if (current.LastAttemptId == request.DeliveryAttemptId)
        {
            if (current.TargetVersion != request.TargetVersion)
            {
                return AgentQuarantine(command, state, "attempt-target-conflict");
            }
            if (request.Action == ConversationDeletionDeliveryAction.Attempt)
            {
                return DomainResult.NoOp();
            }
        }
        if (request.ExpectedDeliveryRevision != current.DeliveryRevision)
        {
            return AgentReject(ConversationAgentsOutcome.Conflict);
        }
        return AgentSuccess(new ConversationDeletionDeliveryRecordedDomainEvent(AgentMetadata(request.Metadata, request.ConversationId,
            command.EventId, ConversationEventType.ConversationDeletionDeliveryRecorded, state.LastEventAt!.Value),
            current.Signal, request.Action, request.DeliveryAttemptId, request.TargetVersion, current.DeliveryRevision, request.Acknowledgement));
    }

    private static DomainResult AgentQuarantine(RecordConversationDeletionDelivery command, ConversationState state, string code)
        => AgentSuccess(new ConversationDeletionDeliveryRecordedDomainEvent(AgentMetadata(command.PublicCommand.Metadata,
            command.PublicCommand.ConversationId, Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    Signal = state.DeletionSource.Signal!.ConversationDeletionSignalId,
                    state.DeletionSource.DeliveryRevision,
                    Code = code
                }))),
            ConversationEventType.ConversationDeletionDeliveryRecorded, state.LastEventAt!.Value), state.DeletionSource.Signal!, ConversationDeletionDeliveryAction.Quarantine,
            command.PublicCommand.DeliveryAttemptId, command.PublicCommand.TargetVersion,
            state.DeletionSource.DeliveryRevision, PoisonCode: code));

    private static DomainResult? CheckAgentSource(ConversationCommandMetadata metadata, ConversationId conversation,
        ConversationState? state, bool allowDeleted = false)
    {
        if (state is null || !state.HasCompleteEventPrefix)
        {
            return AgentReject(ConversationAgentsOutcome.Unavailable);
        }
        if (metadata.TenantId != state.TenantId || conversation != state.ConversationId)
        {
            return AgentReject(ConversationAgentsOutcome.Denied);
        }
        if (metadata.SchemaVersion.Value != 1)
        {
            return AgentReject(ConversationAgentsOutcome.Invalid);
        }
        return state.IsDeleted && !allowDeleted ? AgentReject(ConversationAgentsOutcome.ConversationDeleted) : null;
    }

    private static bool IsValidOccurrence(DateTimeOffset value, ConversationState state)
        => value.Year >= 2000 && value >= state.LastEventAt;

    private static ConversationEventMetadata AgentMetadata(ConversationCommandMetadata metadata, ConversationId conversation,
        string eventId, ConversationEventType type, DateTimeOffset occurrence)
        => new(metadata.SchemaVersion, eventId, type, metadata.TenantId, conversation,
            metadata.CorrelationId, occurrence, metadata.ActorPartyId, metadata.CausationId);

    private static string PostFingerprint(AppendMessageCommand request, DateTimeOffset occurrence)
    {
        var parts = new List<KeyValuePair<string, string?>> {
            new("message", request.MessageId.Value), new("author", request.AuthorPartyId.Value),
            new("text", request.Text), new("occurrence", occurrence.ToString("O")),
            new("key", request.Metadata.IdempotencyKey) };
        AddParts(parts, "provenance", JsonSerializer.SerializeToElement(request.AgentProvenance));
        AddParts(parts, "provider", JsonSerializer.SerializeToElement(request.ProviderCorrelation));
        AddParts(parts, "caller", JsonSerializer.SerializeToElement(request.CallerMetadata));
        return ConversationPayloadFingerprint.FromParts(parts).Value;
    }

    private static void AddParts(List<KeyValuePair<string, string?>> parts, string prefix, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                AddParts(parts, $"{prefix}.{property.Name}", property.Value);
            }
        }
        else
        {
            parts.Add(new(prefix, value.GetRawText()));
        }
    }

    private static DomainResult AgentSuccess(IEventPayload payload) => DomainResult.Success([payload]);

    private static DomainResult AgentReject(ConversationAgentsOutcome outcome)
        => DomainResult.Rejection([new ConversationRejectedDomainEvent(
            outcome == ConversationAgentsOutcome.Conflict ? ConversationErrorCode.IdempotencyConflict : ConversationErrorCode.CommandValidationFailed,
            $"agents-{outcome}")]);
}
