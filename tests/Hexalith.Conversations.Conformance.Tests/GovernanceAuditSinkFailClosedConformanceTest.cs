// <copyright file="GovernanceAuditSinkFailClosedConformanceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 2.1 (AC-5 / T3) — surfaces the previously oracle-unreachable internal governance audit gate
/// (fail-closed-on-sink-failure) into the conformance oracle, observed through the <b>public</b> governed
/// command-handler surface (<see cref="RedactMessageContentCommandHandler"/> returning
/// <see cref="DomainResult"/>) — no Server internals, no new <c>InternalsVisibleTo</c>, no public-contract
/// shape change.
/// </summary>
/// <remarks>
/// <para>
/// The publicly-observable governance <i>audit-pairing</i> invariant (every governance mutation event carries
/// its evidence) is already pinned by <see cref="GovernanceAuditPairingSafetyNetConformanceTest"/>. The
/// residual half — handed to Story 2.1 by the Story 1.2 carry-forward and Epic 1 retro action T3 — is the
/// fail-closed-on-sink-failure behavior of <c>ConversationGovernanceAuditGate</c>: when the audit sink
/// <i>throws</i>, the governed command must reject (audit unavailable) and emit no mutation event. That gate
/// stays <c>internal</c> (exposing it would change the public contract shape, which AC-6 forbids); this test
/// surfaces its behavior where the survivable oracle can see it.
/// </para>
/// <para>
/// <b>Fault-injection (teeth, per Epic 1 L1/A1 — green alone is not evidence).</b> The two facts below flip
/// in opposite directions around the gate: a <i>throwing</i> sink must yield a fail-closed rejection with no
/// mutation, and a <i>succeeding</i> sink must yield the mutation event. If the gate's
/// catch-and-fail-closed were removed (the sink failure bypassed), the throwing-sink fact would instead see
/// the exception propagate out of <see cref="RedactMessageContentCommandHandler.HandleAsync"/> — turning this
/// test RED — and the contrast fact rules out a degenerate always-reject implementation.
/// </para>
/// </remarks>
public sealed class GovernanceAuditSinkFailClosedConformanceTest
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly ConversationId Conversation = new("conversation-a");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly MessageId Message = new("message-a");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AppliedAt = new(2026, 5, 20, 9, 5, 0, TimeSpan.Zero);

    /// <summary>
    /// A throwing audit sink fails closed: the governed redaction rejects (audit unavailable) and emits no
    /// redaction mutation event. This is the T3 gate behavior, observed through the public handler surface.
    /// </summary>
    [Fact]
    public async Task GovernedMutationShouldFailClosedWhenAuditSinkThrows()
    {
        ThrowingAuditService audit = new();
        AllowingTenantAccessService access = new();
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        rejection.ReasonCode.ShouldBe("audit_unavailable");
        result.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
        audit.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Contrast fact: a healthy audit sink that returns paired evidence emits the redaction mutation event,
    /// proving the throwing-sink rejection above is real fail-closed signal, not a constant rejection.
    /// </summary>
    [Fact]
    public async Task GovernedMutationShouldEmitEventWhenAuditSinkHealthy()
    {
        SucceedingAuditService audit = new(AuditEvidence());
        AllowingTenantAccessService access = new();
        RedactMessageContentCommandHandler handler = new(access, audit);

        DomainResult result = await handler.HandleAsync(
            Command(),
            "user-1",
            _ => ValueTask.FromResult<ConversationState?>(CreatedState()),
            "event-redacted-a",
            Tenant,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
        audit.CallCount.ShouldBe(1);
    }

    private static RedactMessageContentCommand Command()
        => new(
            new ConversationCommandMetadata(
                SchemaVersion.Current,
                Tenant,
                Actor,
                "correlation-a",
                "causation-a",
                "idempotency-a"),
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AppliedAt);

    private static GovernanceAuditEvidenceReference AuditEvidence()
        => new(new AuditEvidenceHandle("audit-evidence-001"), "redaction-policy-standard", AppliedAt);

    private static ConversationState CreatedState()
    {
        ConversationState state = new();
        state.Apply(new ConversationCreatedDomainEvent(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-create-a",
                ConversationEventType.ConversationCreated,
                Tenant,
                Conversation,
                "correlation-a",
                CreatedAt,
                Actor,
                "causation-a")));
        state.Apply(new MessageAppended(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-message-a",
                ConversationEventType.MessageAppended,
                Tenant,
                Conversation,
                "correlation-a",
                CreatedAt.AddMinutes(1),
                Actor,
                "causation-a"),
            Message,
            Actor,
            "safe-placeholder"));
        return state;
    }

    private sealed class ThrowingAuditService : IConversationGovernanceAuditService
    {
        public int CallCount { get; private set; }

        public ValueTask<ConversationGovernanceAuditResult> RecordRetentionPolicyChangeAsync(
            SetConversationRetentionPolicyCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("audit sink unavailable");

        public ValueTask<ConversationGovernanceAuditResult> RecordSensitivityMarkAsync(
            MarkConversationContentSensitiveCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("audit sink unavailable");

        public ValueTask<ConversationGovernanceAuditResult> RecordRedactionAsync(
            RedactMessageContentCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            throw new InvalidOperationException("audit sink unavailable");
        }
    }

    private sealed class SucceedingAuditService(GovernanceAuditEvidenceReference evidence) : IConversationGovernanceAuditService
    {
        public int CallCount { get; private set; }

        public ValueTask<ConversationGovernanceAuditResult> RecordRetentionPolicyChangeAsync(
            SetConversationRetentionPolicyCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationGovernanceAuditResult.Succeeded(evidence));

        public ValueTask<ConversationGovernanceAuditResult> RecordSensitivityMarkAsync(
            MarkConversationContentSensitiveCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationGovernanceAuditResult.Succeeded(evidence));

        public ValueTask<ConversationGovernanceAuditResult> RecordRedactionAsync(
            RedactMessageContentCommand command,
            GovernanceOperationKind operationKind,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult(ConversationGovernanceAuditResult.Succeeded(evidence));
        }
    }

    private sealed class AllowingTenantAccessService : IConversationTenantAccessService
    {
        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationTenantAccessDecision.Allowed(requirement, Tenant, "user-1"));
    }
}
