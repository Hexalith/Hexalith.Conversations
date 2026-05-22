// <copyright file="ConversationAggregateSensitivityTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Tests.Aggregates;

/// <summary>
/// Verifies governed sensitivity-mark aggregate behavior.
/// </summary>
public sealed class ConversationAggregateSensitivityTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly MessageId Message = new("message-alpha");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 20, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AppliedAt = new(2026, 5, 20, 8, 5, 0, TimeSpan.Zero);

    /// <summary>
    /// A valid mark emits one content-safe sensitivity event with paired audit evidence.
    /// </summary>
    [Fact]
    public void MarkSensitiveShouldEmitSensitivityEvent()
    {
        ConversationState state = CreatedState();

        DomainResult result = ConversationAggregate.Handle(Command(), state);

        result.IsSuccess.ShouldBeTrue();
        ConversationContentMarkedSensitiveDomainEvent marked =
            result.Events.Single().ShouldBeOfType<ConversationContentMarkedSensitiveDomainEvent>();
        marked.Metadata.EventType.ShouldBe(ConversationEventType.ConversationContentMarkedSensitive);
        marked.Target.Kind.ShouldBe(GovernedTargetKind.Message);
        marked.Target.MessageId.ShouldBe(Message);
        marked.Category.ShouldBe(SensitivityCategory.Restricted);
        marked.PolicyReference.ShouldBe("sensitivity-policy-standard");
        marked.Rationale.ShouldBe("customer-request");
        marked.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
    }

    /// <summary>
    /// Replaying duplicate accepted events keeps one target-keyed sensitivity state.
    /// </summary>
    [Fact]
    public void SensitivityEventsShouldReplayTargetKeyedState()
    {
        ConversationState state = CreatedState();
        ConversationContentMarkedSensitiveDomainEvent marked =
            SingleMarked(ConversationAggregate.Handle(Command(), state));

        ConversationState replayed = CreatedState();
        replayed.Apply(marked);
        replayed.Apply(marked);

        replayed.SensitivityMarks.Count.ShouldBe(1);
        replayed.SensitivityMarks.Single().Category.ShouldBe(SensitivityCategory.Restricted);
        replayed.SensitivityMarks.Single().Target.MessageId.ShouldBe(Message);
    }

    /// <summary>
    /// Compatible repeated marks are idempotent and materially different repeated marks are rejected.
    /// </summary>
    [Fact]
    public void RepeatedMarksShouldBeIdempotentOnlyWhenCompatible()
    {
        ConversationState state = CreatedState();
        state.Apply(SingleMarked(ConversationAggregate.Handle(Command(), state)));

        DomainResult compatible = ConversationAggregate.Handle(Command("event-sensitive-b"), state);
        DomainResult conflict = ConversationAggregate.Handle(
            Command("event-sensitive-c", SensitivityCategory.Regulated),
            state);

        compatible.Events.ShouldBeEmpty();
        ConversationRejectedDomainEvent rejection = conflict.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        rejection.ReasonCode.ShouldBe("sensitivity_mark_conflict");
    }

    /// <summary>
    /// Missing audit evidence fails closed before a sensitivity event is emitted.
    /// </summary>
    [Fact]
    public void MissingAuditEvidenceShouldReturnAuditPairingRejection()
    {
        MarkConversationContentSensitive command = Command() with { AuditEvidence = null! };

        DomainResult result = ConversationAggregate.Handle(command, CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_required");
    }

    /// <summary>
    /// Mismatched audit evidence fails closed before a sensitivity mutation event is emitted.
    /// </summary>
    [Fact]
    public void MismatchedAuditEvidenceShouldReturnAuditPairingRejection()
    {
        MarkConversationContentSensitive command = Command() with
        {
            AuditEvidence = new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-wrong"),
                "sensitivity-policy-other",
                AppliedAt.AddMinutes(1)),
        };

        DomainResult result = ConversationAggregate.Handle(command, CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_mismatch");
        result.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Every supported target type can be marked using only stable target identity.
    /// </summary>
    /// <param name="targetKind">The target kind.</param>
    [Theory]
    [InlineData("conversation")]
    [InlineData("message")]
    [InlineData("file")]
    [InlineData("participant")]
    [InlineData("segment")]
    public void SupportedTargetTypesShouldEmitSensitivityEvent(string targetKind)
    {
        ConversationState state = CreatedState();

        DomainResult result = ConversationAggregate.Handle(Command(target: Target(targetKind)), state);

        result.IsSuccess.ShouldBeTrue();
        ConversationContentMarkedSensitiveDomainEvent marked =
            result.Events.Single().ShouldBeOfType<ConversationContentMarkedSensitiveDomainEvent>();
        marked.Target.ShouldBe(Target(targetKind));
    }

    /// <summary>
    /// Target validation fails closed without emitting sensitivity mutation events.
    /// </summary>
    [Fact]
    public void InvalidTargetShouldRejectWithoutSensitivityEvent()
    {
        GovernanceTarget missingMessage = new(GovernedTargetKind.Message, MessageId: new MessageId("message-missing"));

        DomainResult result = ConversationAggregate.Handle(Command(target: missingMessage), CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("sensitivity_target_invalid");
        result.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
    }

    private static MarkConversationContentSensitive Command(
        string eventId = "event-sensitive-a",
        SensitivityCategory? category = null,
        GovernanceTarget? target = null)
    {
        ConversationCommandMetadata metadata = new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-alpha",
            CausationId: "causation-alpha",
            IdempotencyKey: "idempotency-alpha");

        MarkConversationContentSensitiveCommand publicCommand = new(
            metadata,
            Conversation,
            target ?? Target("message"),
            category ?? SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AppliedAt);

        return new MarkConversationContentSensitive(publicCommand, AuditEvidence(), eventId);
    }

    private static GovernanceAuditEvidenceReference AuditEvidence()
        => new(new AuditEvidenceHandle("audit-evidence-001"), "sensitivity-policy-standard", AppliedAt);

    private static GovernanceTarget Target(string targetKind)
        => targetKind switch
        {
            "conversation" => new GovernanceTarget(GovernedTargetKind.Conversation),
            "message" => new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            "file" => new GovernanceTarget(GovernedTargetKind.File, FileId: new FileId("file-alpha")),
            "participant" => new GovernanceTarget(GovernedTargetKind.Participant, PartyId: Actor),
            "segment" => new GovernanceTarget(GovernedTargetKind.ContentSegment, SegmentReference: "segment-alpha"),
            _ => throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Unsupported target fixture."),
        };

    private static ConversationState CreatedState()
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-alpha",
                ConversationEventType.ConversationCreated,
                Tenant,
                Conversation,
                "correlation-alpha",
                CreatedAt,
                Actor,
                "causation-alpha")));
        state.Apply(new MessageAppended(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-message-alpha",
                ConversationEventType.MessageAppended,
                Tenant,
                Conversation,
                "correlation-alpha",
                CreatedAt.AddMinutes(1),
                Actor,
                "causation-alpha"),
            Message,
            Actor,
            "safe-placeholder"));
        state.Apply(new ParticipantAddedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-participant-alpha",
                ConversationEventType.ParticipantAdded,
                Tenant,
                Conversation,
                "correlation-alpha",
                CreatedAt.AddMinutes(2),
                Actor,
                "causation-alpha"),
            Actor,
            Hexalith.Conversations.Contracts.Participants.ParticipantType.Human,
            Hexalith.Conversations.Contracts.Participants.ParticipantRole.Member));
        state.Apply(new FileReferenceAttached(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-file-alpha",
                ConversationEventType.FileReferenceAttached,
                Tenant,
                Conversation,
                "correlation-alpha",
                CreatedAt.AddMinutes(3),
                Actor,
                "causation-alpha"),
            new FileId("file-alpha"),
            new Hexalith.Conversations.Contracts.Identifiers.FolderId("folder-alpha"),
            Message));
        return state;
    }

    private static ConversationContentMarkedSensitiveDomainEvent SingleMarked(DomainResult result)
        => result.Events.Single().ShouldBeOfType<ConversationContentMarkedSensitiveDomainEvent>();
}
