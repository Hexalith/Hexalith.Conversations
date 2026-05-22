// <copyright file="ConversationAggregateRedactionTest.cs" company="ITANEO">
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
/// Verifies governed redaction aggregate behavior.
/// </summary>
public sealed class ConversationAggregateRedactionTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly MessageId Message = new("message-alpha");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 20, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AppliedAt = new(2026, 5, 20, 8, 5, 0, TimeSpan.Zero);

    /// <summary>
    /// A valid redaction emits one content-safe redaction event with paired audit evidence.
    /// </summary>
    [Fact]
    public void RedactMessageShouldEmitRedactionEvent()
    {
        ConversationState state = CreatedState();

        DomainResult result = ConversationAggregate.Handle(Command(), state);

        result.IsSuccess.ShouldBeTrue();
        MessageContentRedactedDomainEvent redacted =
            result.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
        redacted.Metadata.EventType.ShouldBe(ConversationEventType.MessageContentRedacted);
        redacted.Target.Kind.ShouldBe(GovernedTargetKind.Message);
        redacted.Target.MessageId.ShouldBe(Message);
        redacted.Category.ShouldBe(RedactionCategory.ContentSuppression);
        redacted.PolicyReference.ShouldBe("redaction-policy-standard");
        redacted.Rationale.ShouldBe("customer-request");
        redacted.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
        redacted.ToString().ShouldNotContain("safe-placeholder", Case.Insensitive);
    }

    /// <summary>
    /// Replaying duplicate accepted events keeps one target-keyed redaction state.
    /// </summary>
    [Fact]
    public void RedactionEventsShouldReplayTargetKeyedState()
    {
        ConversationState state = CreatedState();
        MessageContentRedactedDomainEvent redacted = SingleRedacted(ConversationAggregate.Handle(Command(), state));

        ConversationState replayed = CreatedState();
        replayed.Apply(redacted);
        replayed.Apply(redacted);

        replayed.Redactions.Count.ShouldBe(1);
        replayed.Redactions.Single().Category.ShouldBe(RedactionCategory.ContentSuppression);
        replayed.Redactions.Single().Target.MessageId.ShouldBe(Message);
    }

    /// <summary>
    /// Existing sensitivity marks do not block a separately audited redaction intent for the same target.
    /// </summary>
    [Fact]
    public void ExistingSensitivityMarkShouldNotBlockRedaction()
    {
        ConversationState state = CreatedState();
        state.Apply(SensitiveEvent());

        DomainResult result = ConversationAggregate.Handle(Command(), state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
        state.SensitivityMarks.Single().Target.MessageId.ShouldBe(Message);
        state.Redactions.ShouldBeEmpty();
    }

    /// <summary>
    /// Compatible repeated redactions are idempotent and materially different repeated redactions are rejected.
    /// </summary>
    [Fact]
    public void RepeatedRedactionsShouldBeIdempotentOnlyWhenCompatible()
    {
        ConversationState state = CreatedState();
        state.Apply(SingleRedacted(ConversationAggregate.Handle(Command(), state)));

        DomainResult compatible = ConversationAggregate.Handle(Command("event-redacted-b"), state);
        DomainResult conflict = ConversationAggregate.Handle(
            Command("event-redacted-c", RedactionCategory.ReferenceWithheld),
            state);

        compatible.Events.ShouldBeEmpty();
        ConversationRejectedDomainEvent rejection = conflict.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyConflict);
        rejection.ReasonCode.ShouldBe("redaction_conflict");
    }

    /// <summary>
    /// Missing audit evidence fails closed before a redaction event is emitted.
    /// </summary>
    [Fact]
    public void MissingAuditEvidenceShouldReturnAuditPairingRejection()
    {
        RedactMessageContent command = Command() with { AuditEvidence = null! };

        DomainResult result = ConversationAggregate.Handle(command, CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_required");
    }

    /// <summary>
    /// Mismatched audit evidence fails closed before a redaction event is emitted.
    /// </summary>
    [Fact]
    public void MismatchedAuditEvidenceShouldReturnAuditPairingRejection()
    {
        RedactMessageContent command = Command() with
        {
            AuditEvidence = new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-wrong"),
                "redaction-policy-other",
                AppliedAt.AddMinutes(1)),
        };

        DomainResult result = ConversationAggregate.Handle(command, CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_mismatch");
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Story 2.4 redaction target types are limited to message and opaque content segment references.
    /// </summary>
    /// <param name="targetKind">The target kind.</param>
    [Theory]
    [InlineData("message")]
    [InlineData("segment")]
    public void SupportedTargetTypesShouldEmitRedactionEvent(string targetKind)
    {
        ConversationState state = CreatedState();

        DomainResult result = ConversationAggregate.Handle(Command(target: Target(targetKind)), state);

        result.IsSuccess.ShouldBeTrue();
        MessageContentRedactedDomainEvent redacted =
            result.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
        redacted.Target.ShouldBe(Target(targetKind));
    }

    /// <summary>
    /// Unsupported target kinds fail closed without emitting redaction mutation events.
    /// </summary>
    [Fact]
    public void UnsupportedTargetShouldRejectWithoutRedactionEvent()
    {
        DomainResult result = ConversationAggregate.Handle(
            Command(target: new GovernanceTarget(GovernedTargetKind.Conversation)),
            CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("redaction_target_invalid");
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Missing message targets fail closed without exposing original content.
    /// </summary>
    [Fact]
    public void MissingMessageTargetShouldRejectWithoutRedactionEvent()
    {
        GovernanceTarget missingMessage = new(GovernedTargetKind.Message, MessageId: new MessageId("message-missing"));

        DomainResult result = ConversationAggregate.Handle(Command(target: missingMessage), CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("redaction_target_invalid");
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
    }

    private static RedactMessageContent Command(
        string eventId = "event-redacted-a",
        RedactionCategory? category = null,
        GovernanceTarget? target = null)
    {
        ConversationCommandMetadata metadata = new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-alpha",
            CausationId: "causation-alpha",
            IdempotencyKey: "idempotency-alpha");

        RedactMessageContentCommand publicCommand = new(
            metadata,
            Conversation,
            target ?? Target("message"),
            category ?? RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AppliedAt);

        return new RedactMessageContent(publicCommand, AuditEvidence(), eventId);
    }

    private static GovernanceAuditEvidenceReference AuditEvidence()
        => new(new AuditEvidenceHandle("audit-evidence-001"), "redaction-policy-standard", AppliedAt);

    private static ConversationContentMarkedSensitiveDomainEvent SensitiveEvent()
        => new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-sensitive-alpha",
                ConversationEventType.ConversationContentMarkedSensitive,
                Tenant,
                Conversation,
                "correlation-alpha",
                AppliedAt,
                Actor,
                "causation-alpha"),
            Target("message"),
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-sensitive-001"),
                "sensitivity-policy-standard",
                AppliedAt));

    private static GovernanceTarget Target(string targetKind)
        => targetKind switch
        {
            "message" => new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
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
        return state;
    }

    private static MessageContentRedactedDomainEvent SingleRedacted(DomainResult result)
        => result.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
}
