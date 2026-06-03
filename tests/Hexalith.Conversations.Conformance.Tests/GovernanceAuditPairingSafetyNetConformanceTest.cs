// <copyright file="GovernanceAuditPairingSafetyNetConformanceTest.cs" company="ITANEO">
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
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.3 (AC1) — release-gate governance audit-pairing safety net, re-expressed against the public
/// command / state / event / <see cref="DomainResult"/> surface so it survives the Boilerplate Reduction
/// refactor (Epic 2/3) instead of breaking the moment a Server handler/service type relocates.
///
/// The original <c>Hexalith.Conversations.Server.Tests.Governance.GovernanceAuditPairingSafetyNetTest</c>
/// proved this invariant by reflecting over <c>Server.CommandHandlers</c> / <c>Server.Governance</c> /
/// <c>Server.Projections</c> / <c>Server.Queries</c> / <c>Server.Api</c> concrete plumbing types. That test
/// is classified "re-express, never delete" in the at-risk register: its survival through the refactor IS
/// the point. This re-expression drives the live <see cref="ConversationAggregate"/> command/state/event
/// surface — no Server coupling — so it can live inside the conformance oracle (covered by Story 5.1's
/// full-suite run) and stay green for the right reason (behavior preserved) under the refactor.
///
/// The invariant: every implemented governance mutation pairs its state-change event with audit evidence
/// (and fails closed when the evidence is missing/mismatched), while non-governance commands carry no audit
/// dependency. Pins current observable behavior on <c>main</c>; it does not anticipate the refactor target.
/// </summary>
public sealed class GovernanceAuditPairingSafetyNetConformanceTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly MessageId Message = new("message-alpha");
    private static readonly ProjectId OldProject = new("project-old");
    private static readonly ProjectId NewProject = new("project-new");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 18, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AppliedAt = new(2026, 5, 18, 12, 45, 0, TimeSpan.Zero);

    /// <summary>
    /// Every implemented governance mutation emits a state-change event carrying paired audit evidence,
    /// observed through the public <see cref="ConversationAggregate.Handle(SetConversationRetentionPolicy, ConversationState?)"/>
    /// surface (no Server plumbing). This is the safety net's reason for existing.
    /// </summary>
    [Fact]
    public void EveryGovernanceMutationShouldPairItsEventWithAuditEvidence()
    {
        // SetConversationRetentionPolicy -> RetentionPolicySet (first policy).
        DomainResult set = ConversationAggregate.Handle(RetentionCommand("retention-policy-standard"), CreatedState());
        RetentionPolicySetDomainEvent setEvent = set.Events.Single().ShouldBeOfType<RetentionPolicySetDomainEvent>();
        setEvent.AuditEvidence.ShouldNotBeNull();
        setEvent.AuditEvidence.Handle.Value.ShouldBe("audit-evidence-001");

        // SetConversationRetentionPolicy -> RetentionPolicyReplaced (active policy already present).
        ConversationState replacing = CreatedState();
        replacing.Apply(setEvent);
        DomainResult replaced = ConversationAggregate.Handle(
            RetentionCommand("retention-policy-extended", AppliedAt.AddMinutes(1)),
            replacing);
        RetentionPolicyReplacedDomainEvent replacedEvent = replaced.Events.Single().ShouldBeOfType<RetentionPolicyReplacedDomainEvent>();
        replacedEvent.AuditEvidence.ShouldNotBeNull();

        // MarkConversationContentSensitive -> ConversationContentMarkedSensitive.
        DomainResult sensitive = ConversationAggregate.Handle(SensitiveCommand(), CreatedStateWithMessage());
        ConversationContentMarkedSensitiveDomainEvent sensitiveEvent =
            sensitive.Events.Single().ShouldBeOfType<ConversationContentMarkedSensitiveDomainEvent>();
        sensitiveEvent.AuditEvidence.ShouldNotBeNull();

        // RedactMessageContent -> MessageContentRedacted.
        DomainResult redacted = ConversationAggregate.Handle(RedactCommand(), CreatedStateWithMessage());
        MessageContentRedactedDomainEvent redactedEvent =
            redacted.Events.Single().ShouldBeOfType<MessageContentRedactedDomainEvent>();
        redactedEvent.AuditEvidence.ShouldNotBeNull();
    }

    /// <summary>
    /// The audit pairing is REQUIRED, not incidental: a governance command whose audit evidence is missing
    /// fails closed with the typed audit-pairing rejection and emits no mutation event. This is strictly
    /// stronger than the original inventory test, which only catalogued the mutation paths.
    /// </summary>
    [Fact]
    public void GovernanceMutationWithMissingAuditEvidenceShouldFailClosed()
    {
        DomainResult retention = ConversationAggregate.Handle(
            RetentionCommand("retention-policy-standard") with { AuditEvidence = null! },
            CreatedState());
        DomainResult sensitive = ConversationAggregate.Handle(
            SensitiveCommand() with { AuditEvidence = null! },
            CreatedStateWithMessage());
        DomainResult redacted = ConversationAggregate.Handle(
            RedactCommand() with { AuditEvidence = null! },
            CreatedStateWithMessage());

        foreach (DomainResult result in new[] { retention, sensitive, redacted })
        {
            ConversationRejectedDomainEvent rejection = result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
            rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
            rejection.ReasonCode.ShouldBe("audit_pairing_required");
        }

        retention.Events.Any(e => e is RetentionPolicySetDomainEvent or RetentionPolicyReplacedDomainEvent).ShouldBeFalse();
        sensitive.Events.Any(e => e is ConversationContentMarkedSensitiveDomainEvent).ShouldBeFalse();
        redacted.Events.Any(e => e is MessageContentRedactedDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Mismatched audit evidence (handle/policy that does not pair with the mutation) also fails closed,
    /// before any mutation event is emitted.
    /// </summary>
    [Fact]
    public void GovernanceMutationWithMismatchedAuditEvidenceShouldFailClosed()
    {
        GovernanceAuditEvidenceReference wrong = new(
            new AuditEvidenceHandle("audit-evidence-wrong"),
            "policy-other",
            AppliedAt.AddMinutes(1));

        DomainResult retention = ConversationAggregate.Handle(
            RetentionCommand("retention-policy-standard") with { AuditEvidence = wrong },
            CreatedState());

        ConversationRejectedDomainEvent rejection = retention.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
        rejection.Code.ShouldBe(ConversationErrorCode.AuditPairingRequired);
        rejection.ReasonCode.ShouldBe("audit_pairing_mismatch");
        retention.Events.Any(e => e is RetentionPolicySetDomainEvent or RetentionPolicyReplacedDomainEvent).ShouldBeFalse();
    }

    /// <summary>
    /// Non-governance commands carry no audit dependency: the events they emit through the public surface
    /// expose no <see cref="GovernanceAuditEvidenceReference"/>, while every governance mutation event does.
    /// </summary>
    [Fact]
    public void NonGovernanceCommandsShouldEmitEventsWithoutAuditEvidenceDependency()
    {
        IEventPayload created = ConversationAggregate.Handle(CreateCommand(), state: null)
            .Events.Single();
        IEventPayload participantAdded = ConversationAggregate.Handle(AddParticipantCommand(), CreatedState())
            .Events.Single();
        IEventPayload projectChanged = ConversationAggregate.Handle(ReassignProjectCommand(), CreatedState(project: OldProject))
            .Events.Single();

        created.ShouldBeOfType<ConversationCreatedDomainEvent>();
        participantAdded.ShouldBeOfType<ParticipantAddedDomainEvent>();
        projectChanged.ShouldBeOfType<ConversationProjectChangedDomainEvent>();

        CarriesAuditEvidence(created).ShouldBeFalse();
        CarriesAuditEvidence(participantAdded).ShouldBeFalse();
        CarriesAuditEvidence(projectChanged).ShouldBeFalse();

        // Reciprocal: a governance mutation event DOES carry audit evidence (guards against the predicate
        // trivially returning false for everything).
        IEventPayload retentionSet = ConversationAggregate.Handle(RetentionCommand("retention-policy-standard"), CreatedState())
            .Events.Single();
        CarriesAuditEvidence(retentionSet).ShouldBeTrue();
    }

    /// <summary>
    /// The set of aggregate commands requiring paired audit evidence is exactly the governance mutation set,
    /// and the non-audit commands are exactly the non-governance set. Re-expresses the original completeness
    /// inventory by reflecting over the public <see cref="ConversationAggregate"/> command surface and the
    /// public command contracts — never over Server handler/service plumbing.
    /// </summary>
    [Fact]
    public void AggregateGovernanceCommandSurfaceShouldMatchTheAuditPairedInventory()
    {
        Type[] auditPairedCommands = HandleCommandParameterTypes()
            .Where(RequiresGovernanceAuditEvidence)
            .Distinct()
            .ToArray();
        Type[] nonAuditCommands = HandleCommandParameterTypes()
            .Where(type => !RequiresGovernanceAuditEvidence(type))
            .Distinct()
            .ToArray();

        auditPairedCommands.ShouldBe(
            [
                typeof(SetConversationRetentionPolicy),
                typeof(MarkConversationContentSensitive),
                typeof(RedactMessageContent),
            ],
            ignoreOrder: true);
        nonAuditCommands.ShouldBe(
            [
                typeof(CreateConversation),
                typeof(AddParticipant),
                typeof(ReassignConversationProject),
            ],
            ignoreOrder: true);
    }

    /// <summary>
    /// Future governance vocabulary stays prepared in the public <see cref="GovernanceOperationKind"/> enum
    /// but is not part of the implemented mutation inventory. Pure public-contract assertion.
    /// </summary>
    [Fact]
    public void FutureGovernanceVocabularyShouldNotAppearAsImplementedMutationPaths()
    {
        GovernanceOperationKind[] implemented =
        [
            GovernanceOperationKind.SetRetentionPolicy,
            GovernanceOperationKind.ReplaceRetentionPolicy,
            GovernanceOperationKind.MarkContentSensitive,
            GovernanceOperationKind.RedactMessageContent,
            GovernanceOperationKind.RecordPrivilegedJustification,
        ];
        GovernanceOperationKind[] futureOnly =
        [
            GovernanceOperationKind.ArchiveConversation,
            GovernanceOperationKind.LogicallyDeleteConversation,
            GovernanceOperationKind.DeferForLegalHold,
            GovernanceOperationKind.GovernAuditRecord,
        ];

        foreach (GovernanceOperationKind operationKind in futureOnly)
        {
            implemented.ShouldNotContain(operationKind);
        }
    }

    private static bool CarriesAuditEvidence(IEventPayload payload)
        => payload.GetType()
            .GetProperties()
            .Any(property => property.PropertyType == typeof(GovernanceAuditEvidenceReference)
                && property.GetValue(payload) is not null);

    private static bool RequiresGovernanceAuditEvidence(Type commandType)
        => commandType.GetProperty(nameof(SetConversationRetentionPolicy.AuditEvidence))?.PropertyType
            == typeof(GovernanceAuditEvidenceReference);

    private static Type[] HandleCommandParameterTypes()
        => [.. typeof(ConversationAggregate).GetMethods()
            .Where(method => method.Name == nameof(ConversationAggregate.Handle))
            .SelectMany(method => method.GetParameters().Take(1).Select(parameter => parameter.ParameterType))
            .Distinct()];

    private static SetConversationRetentionPolicy RetentionCommand(string policyReference, DateTimeOffset? appliedAt = null)
    {
        SetConversationRetentionPolicyCommand publicCommand = new(
            Metadata(),
            Conversation,
            policyReference,
            "customer-request",
            appliedAt ?? AppliedAt);
        return new SetConversationRetentionPolicy(
            publicCommand,
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-001"),
                policyReference,
                appliedAt ?? AppliedAt),
            $"event-{policyReference}");
    }

    private static MarkConversationContentSensitive SensitiveCommand()
    {
        MarkConversationContentSensitiveCommand publicCommand = new(
            Metadata(),
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AppliedAt);
        return new MarkConversationContentSensitive(
            publicCommand,
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-001"),
                "sensitivity-policy-standard",
                AppliedAt),
            "event-sensitive-a");
    }

    private static RedactMessageContent RedactCommand()
    {
        RedactMessageContentCommand publicCommand = new(
            Metadata(),
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AppliedAt);
        return new RedactMessageContent(
            publicCommand,
            new GovernanceAuditEvidenceReference(
                new AuditEvidenceHandle("audit-evidence-001"),
                "redaction-policy-standard",
                AppliedAt),
            "event-redacted-a");
    }

    private static CreateConversation CreateCommand()
    {
        CreateConversationCommand publicCommand = new(
            Metadata(),
            new BusinessReference("crm", "case-123"),
            new ProjectId("project-alpha"),
            new FolderId("folder-alpha"),
            "Support case");
        return new CreateConversation(publicCommand, Conversation, CreatedAt, "event-create-alpha");
    }

    private static AddParticipant AddParticipantCommand()
    {
        AddParticipantCommand publicCommand = new(
            Metadata(),
            Conversation,
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);
        return new AddParticipant(publicCommand, AppliedAt, "event-add-participant");
    }

    private static ReassignConversationProject ReassignProjectCommand()
    {
        ReassignConversationProjectCommand publicCommand = new(
            Metadata(),
            Conversation,
            new ConversationProjectAssignment(ConversationProjectAssignmentOperation.Assign, NewProject),
            OldProject);
        return new ReassignConversationProject(publicCommand, AppliedAt, "event-project-changed");
    }

    private static ConversationCommandMetadata Metadata()
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-alpha",
            CausationId: "causation-alpha",
            IdempotencyKey: "idempotency-alpha");

    private static ConversationState CreatedState(ProjectId? project = null)
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
                "causation-alpha"),
            ProjectId: project));
        return state;
    }

    private static ConversationState CreatedStateWithMessage()
    {
        ConversationState state = CreatedState();
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
}
