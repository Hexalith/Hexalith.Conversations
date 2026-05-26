// <copyright file="ConversationAggregateProjectAssignmentTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Aggregates;

/// <summary>
/// Verifies pure aggregate behavior for conversation project assignment changes.
/// </summary>
public sealed class ConversationAggregateProjectAssignmentTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly ProjectId OldProject = new("project-old");
    private static readonly ProjectId NewProject = new("project-new");
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 18, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ChangedAt = new(2026, 5, 18, 12, 45, 0, TimeSpan.Zero);

    private static readonly string[] ForbiddenPayloadTerms =
    [
        "transcript",
        "prompt",
        "messageBody",
        "displayName",
        "contact",
        "fileContent",
        "token",
        "claim",
        "eventStore",
        "stream",
        "dapr",
        "exception",
        "providerPayload",
    ];

    [Fact]
    public void AssignProjectShouldEmitExactlyOneProjectChangedEvent()
    {
        ConversationState state = CreatedState();
        ReassignConversationProject command = DomainCommand(ProjectTarget(NewProject));

        DomainResult result = ConversationAggregate.Handle(command, state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        ConversationProjectChangedDomainEvent changed = result.Events.Single().ShouldBeOfType<ConversationProjectChangedDomainEvent>();
        changed.Metadata.EventType.ShouldBe(ConversationEventType.ConversationProjectChanged);
        changed.Metadata.TenantId.ShouldBe(Tenant);
        changed.Metadata.ConversationId.ShouldBe(Conversation);
        changed.Metadata.ActorPartyId.ShouldBe(Actor);
        changed.PreviousProjectId.ShouldBeNull();
        changed.CurrentProjectId.ShouldBe(NewProject);
    }

    [Fact]
    public void ReassignProjectShouldRecordPreviousAndCurrentProjectIds()
    {
        ConversationState state = CreatedState(project: OldProject);
        ReassignConversationProject command = DomainCommand(ProjectTarget(NewProject), expectedCurrentProjectId: OldProject);

        DomainResult result = ConversationAggregate.Handle(command, state);

        ConversationProjectChangedDomainEvent changed = result.Events.Single().ShouldBeOfType<ConversationProjectChangedDomainEvent>();
        changed.PreviousProjectId.ShouldBe(OldProject);
        changed.CurrentProjectId.ShouldBe(NewProject);
    }

    [Fact]
    public void ClearProjectShouldRecordPreviousProjectAndNullCurrentProject()
    {
        ConversationState state = CreatedState(project: OldProject);
        ReassignConversationProject command = DomainCommand(ClearTarget(), expectedCurrentProjectId: OldProject);

        DomainResult result = ConversationAggregate.Handle(command, state);

        ConversationProjectChangedDomainEvent changed = result.Events.Single().ShouldBeOfType<ConversationProjectChangedDomainEvent>();
        changed.PreviousProjectId.ShouldBe(OldProject);
        changed.CurrentProjectId.ShouldBeNull();
    }

    [Fact]
    public void SameTargetProjectShouldReturnNoOpWithoutSuccessEvent()
    {
        ConversationState state = CreatedState(project: NewProject);
        ReassignConversationProject command = DomainCommand(ProjectTarget(NewProject));

        DomainResult result = ConversationAggregate.Handle(command, state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void ProjectChangedEventsShouldReplayDeterministically()
    {
        ConversationState state = CreatedState();
        ConversationProjectChangedDomainEvent assign = ChangedEvent(previous: null, current: OldProject, "event-project-assign");
        ConversationProjectChanged clear = new(
            EventMetadata("event-project-clear", ChangedAt.AddMinutes(1)),
            OldProject,
            CurrentProjectId: null);

        state.Apply(assign);
        state.ProjectId.ShouldBe(OldProject);
        state.Apply(clear);

        state.ProjectId.ShouldBeNull();
        state.LastEventAt.ShouldBe(clear.Metadata.CommittedAt);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("not-created")]
    [InlineData("tenant-mismatch")]
    [InlineData("conversation-mismatch")]
    [InlineData("closed")]
    [InlineData("archived")]
    public void UnsafeConversationStatesShouldRejectProjectAssignment(string stateShape)
    {
        ConversationState? state = stateShape switch
        {
            "missing" => null,
            "not-created" => new ConversationState(),
            "tenant-mismatch" => CreatedState(tenant: new TenantId("tenant-other")),
            "conversation-mismatch" => CreatedState(conversation: new ConversationId("conversation-other")),
            "closed" => ClosedState(),
            "archived" => ArchivedState(),
            _ => throw new ArgumentOutOfRangeException(nameof(stateShape), stateShape, "Unsupported state fixture."),
        };

        DomainResult result = ConversationAggregate.Handle(DomainCommand(ProjectTarget(NewProject)), state);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldAllBe(e => e is ConversationRejectedDomainEvent);
    }

    [Theory]
    [InlineData("missing-target", "project_assignment_target_missing")]
    [InlineData("assign-without-project", "project_assignment_target_missing")]
    [InlineData("clear-with-project", "project_clear_target_must_be_null")]
    [InlineData("expected-current-mismatch", "project_current_mismatch")]
    [InlineData("missing-event-id", "event_identity_missing")]
    [InlineData("invalid-timestamp", "project_changed_timestamp_invalid")]
    public void InvalidTargetShapeShouldReturnTypedRejection(string shape, string reasonCode)
    {
        ConversationState state = CreatedState(project: OldProject);
        ReassignConversationProject command = shape switch
        {
            "missing-target" => DomainCommand(null!),
            "assign-without-project" => DomainCommand(ProjectTarget(null)),
            "clear-with-project" => DomainCommand(new ConversationProjectAssignment(ConversationProjectAssignmentOperation.Clear, OldProject)),
            "expected-current-mismatch" => DomainCommand(ProjectTarget(NewProject), expectedCurrentProjectId: new ProjectId("project-other")),
            "missing-event-id" => DomainCommand(ProjectTarget(NewProject), eventId: " "),
            "invalid-timestamp" => DomainCommand(ProjectTarget(NewProject), changedAt: DateTimeOffset.MinValue),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unsupported target fixture."),
        };

        DomainResult result = ConversationAggregate.Handle(command, state);

        ConversationRejectedDomainEvent rejection = SingleRejection(result);
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe(reasonCode);
    }

    [Fact]
    public void ProjectAssignmentEventsShouldNotSerializeForbiddenPayloadTerms()
    {
        ConversationProjectChangedDomainEvent changed = ChangedEvent(OldProject, NewProject, "event-project-privacy");

        string json = JsonSerializer.Serialize(changed, WebOptions);

        foreach (string forbidden in ForbiddenPayloadTerms)
        {
            json.ShouldNotContain(forbidden, Case.Insensitive);
        }
    }

    private static ReassignConversationProject DomainCommand(
        ConversationProjectAssignment target,
        ProjectId? expectedCurrentProjectId = null,
        DateTimeOffset? changedAt = null,
        string eventId = "event-project-changed")
    {
        ReassignConversationProjectCommand publicCommand = new(
            Metadata(),
            Conversation,
            target,
            expectedCurrentProjectId);

        return new ReassignConversationProject(publicCommand, changedAt ?? ChangedAt, eventId);
    }

    private static ConversationProjectAssignment ProjectTarget(ProjectId? projectId)
        => new(ConversationProjectAssignmentOperation.Assign, projectId);

    private static ConversationProjectAssignment ClearTarget()
        => new(ConversationProjectAssignmentOperation.Clear);

    private static ConversationCommandMetadata Metadata()
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-alpha",
            CausationId: "causation-alpha",
            IdempotencyKey: "idempotency-alpha");

    private static ConversationEventMetadata EventMetadata(string eventId, DateTimeOffset occurredAt)
        => new(
            SchemaVersion.Current,
            eventId,
            ConversationEventType.ConversationProjectChanged,
            Tenant,
            Conversation,
            "correlation-alpha",
            occurredAt,
            Actor,
            "causation-alpha");

    private static ConversationProjectChangedDomainEvent ChangedEvent(ProjectId? previous, ProjectId? current, string eventId)
        => new(EventMetadata(eventId, ChangedAt), previous, current);

    private static ConversationState CreatedState(
        TenantId? tenant = null,
        ConversationId? conversation = null,
        ProjectId? project = null)
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
                "causation-alpha"),
            ProjectId: project));
        return state;
    }

    private static ConversationState ClosedState()
    {
        ConversationState state = CreatedState();
        state.ForceLifecycleForTests(ConversationLifecycleState.Closed);
        return state;
    }

    private static ConversationState ArchivedState()
    {
        ConversationState state = CreatedState();
        state.ForceLifecycleForTests(ConversationLifecycleState.Archived);
        return state;
    }

    private static ConversationRejectedDomainEvent SingleRejection(DomainResult result)
    {
        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        return result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>();
    }
}
