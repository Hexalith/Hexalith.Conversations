// <copyright file="ConversationAggregateRetentionPolicyTest.cs" company="ITANEO">
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
/// Verifies governed retention policy aggregate behavior.
/// </summary>
public sealed class ConversationAggregateRetentionPolicyTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 18, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AppliedAt = new(2026, 5, 18, 12, 45, 0, TimeSpan.Zero);

    /// <summary>
    /// A first valid retention policy emits exactly one set event with paired audit evidence.
    /// </summary>
    [Fact]
    public void FirstRetentionPolicyShouldEmitSetEvent()
    {
        ConversationState state = CreatedState();

        DomainResult result = ConversationAggregate.Handle(Command("retention-policy-standard"), state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        RetentionPolicySetDomainEvent set = result.Events.Single().ShouldBeOfType<RetentionPolicySetDomainEvent>();
        set.Metadata.EventType.ShouldBe(ConversationEventType.RetentionPolicySet);
        set.Metadata.TenantId.ShouldBe(Tenant);
        set.Metadata.ConversationId.ShouldBe(Conversation);
        set.PolicyReference.ShouldBe("retention-policy-standard");
        set.Rationale.ShouldBe("customer-request");
        set.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");
    }

    /// <summary>
    /// Replacing an active policy emits exactly one replacement event with the prior public reference.
    /// </summary>
    [Fact]
    public void ReplacingRetentionPolicyShouldEmitReplacementEvent()
    {
        ConversationState state = CreatedState();
        state.Apply(SingleSet(ConversationAggregate.Handle(Command("retention-policy-standard"), state)));

        DomainResult result = ConversationAggregate.Handle(Command("retention-policy-extended", AppliedAt.AddMinutes(1)), state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        RetentionPolicyReplacedDomainEvent replaced = result.Events.Single().ShouldBeOfType<RetentionPolicyReplacedDomainEvent>();
        replaced.Metadata.EventType.ShouldBe(ConversationEventType.RetentionPolicyReplaced);
        replaced.PolicyReference.ShouldBe("retention-policy-extended");
        replaced.PreviousPolicyReference.ShouldBe("retention-policy-standard");
    }

    /// <summary>
    /// Retention events replay deterministically into one active policy state in persisted order.
    /// </summary>
    [Fact]
    public void RetentionEventsShouldReplayActivePolicyState()
    {
        ConversationState state = CreatedState();
        RetentionPolicySetDomainEvent set = SingleSet(ConversationAggregate.Handle(Command("retention-policy-standard"), state));
        state.Apply(set);
        RetentionPolicyReplacedDomainEvent replaced = SingleReplaced(
            ConversationAggregate.Handle(Command("retention-policy-extended", AppliedAt.AddMinutes(1)), state));

        ConversationState replayed = CreatedState();
        replayed.Apply(set);
        replayed.Apply(replaced);
        replayed.Apply(replaced);

        replayed.ActiveRetentionPolicy.ShouldNotBeNull();
        replayed.ActiveRetentionPolicy.PolicyReference.ShouldBe("retention-policy-extended");
        replayed.ActiveRetentionPolicy.PreviousPolicyReference.ShouldBe("retention-policy-standard");
        replayed.ActiveRetentionPolicy.ActorPartyId.ShouldBe(Actor);
    }

    /// <summary>
    /// Missing and unsafe state fails closed without retention mutation events.
    /// </summary>
    /// <param name="stateShape">The state shape.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("not-created")]
    [InlineData("tenant-mismatch")]
    [InlineData("conversation-mismatch")]
    [InlineData("closed")]
    public void UnsafeStateShouldRejectRetentionPolicyWithoutMutationEvent(string stateShape)
    {
        ConversationState? state = stateShape switch
        {
            "missing" => null,
            "not-created" => new ConversationState(),
            "tenant-mismatch" => CreatedState(tenant: new TenantId("tenant-other")),
            "conversation-mismatch" => CreatedState(conversation: new ConversationId("conversation-other")),
            "closed" => ClosedState(),
            _ => throw new ArgumentOutOfRangeException(nameof(stateShape), stateShape, "Unsupported state fixture."),
        };

        DomainResult result = ConversationAggregate.Handle(Command("retention-policy-standard"), state);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
    }

    /// <summary>
    /// Missing audit evidence fails closed before a domain mutation event is emitted.
    /// </summary>
    [Fact]
    public void MissingAuditEvidenceShouldReturnAuditPairingRejection()
    {
        SetConversationRetentionPolicy command = Command("retention-policy-standard") with { AuditEvidence = null! };

        DomainResult result = ConversationAggregate.Handle(command, CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_required");
    }

    /// <summary>
    /// Mismatched audit evidence fails closed before a retention mutation event is emitted.
    /// </summary>
    [Fact]
    public void MismatchedAuditEvidenceShouldReturnAuditPairingRejection()
    {
        SetConversationRetentionPolicy command = Command("retention-policy-standard") with
        {
            AuditEvidence = new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-wrong"),
                "retention-policy-other",
                AppliedAt.AddMinutes(1)),
        };

        DomainResult result = ConversationAggregate.Handle(command, CreatedState());

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_mismatch");
        result.Events.Any(e => e is RetentionPolicySetDomainEvent || e is RetentionPolicyReplacedDomainEvent).ShouldBeFalse();
    }

    private static SetConversationRetentionPolicy Command(string policyReference, DateTimeOffset? appliedAt = null)
    {
        ConversationCommandMetadata metadata = new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-alpha",
            CausationId: "causation-alpha",
            IdempotencyKey: "idempotency-alpha");

        SetConversationRetentionPolicyCommand publicCommand = new(
            metadata,
            Conversation,
            policyReference,
            "customer-request",
            appliedAt ?? AppliedAt);

        return new SetConversationRetentionPolicy(
            publicCommand,
            AuditEvidence(policyReference, appliedAt ?? AppliedAt),
            $"event-{policyReference}");
    }

    private static GovernanceAuditEvidenceReference AuditEvidence(string policyReference, DateTimeOffset capturedAt)
        => new(new AuditEvidenceHandle("audit-evidence-001"), policyReference, capturedAt);

    private static ConversationState CreatedState(TenantId? tenant = null, ConversationId? conversation = null)
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-alpha",
                ConversationEventType.ConversationCreated,
                tenant ?? Tenant,
                conversation ?? Conversation,
                "correlation-alpha",
                CreatedAt,
                Actor,
                "causation-alpha")));
        return state;
    }

    private static ConversationState ClosedState()
    {
        ConversationState state = CreatedState();
        state.ForceLifecycleForTests(ConversationLifecycleState.Closed);
        return state;
    }

    private static RetentionPolicySetDomainEvent SingleSet(DomainResult result)
        => result.Events.Single().ShouldBeOfType<RetentionPolicySetDomainEvent>();

    private static RetentionPolicyReplacedDomainEvent SingleReplaced(DomainResult result)
        => result.Events.Single().ShouldBeOfType<RetentionPolicyReplacedDomainEvent>();
}
