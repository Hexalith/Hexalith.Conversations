// <copyright file="ConversationAggregateBaseClassDispatchTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

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
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Replay;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Aggregates;

/// <summary>
/// Proves that the live route into <see cref="ConversationAggregate"/> is the SDK
/// <see cref="EventStore.Client.Aggregates.EventStoreAggregate{TState}"/> reflection dispatch
/// (<c>ProcessAsync</c>) and replay (<c>Replay</c>) — not a hand-rolled per-command switch table or
/// an idempotency-bridge shim. This is the AC-1 teeth test for Story 2.2: it drives the reflection
/// path for every <c>Handle</c> overload and goes RED if dispatch is bypassed (a command with no
/// matching <c>Handle</c> must surface the SDK's "No Handle method found" failure, not a silent
/// no-op) or if the <c>Apply</c> convention is removed (replay of a known event must reconstruct state).
/// The pure command→state→event tests (<see cref="ConversationAggregateCreateTest"/> et al.) stay as
/// direct <c>Handle</c>/<c>Apply</c> calls — that is the intended pure-function style and is unchanged.
/// </summary>
public sealed class ConversationAggregateBaseClassDispatchTest
{
    private const string Domain = "conversations";

    // Command-time dispatch deserializes the payload with default System.Text.Json options
    // (EventStoreAggregate.DispatchCommandAsync calls JsonSerializer.Deserialize(payload, type)
    // with no options), so the test serializes the payload the same way to round-trip faithfully.
    private static readonly JsonSerializerOptions CommandOptions = JsonSerializerOptions.Default;

    // Replay deserializes event payloads with Web options (DomainProcessorStateRehydrator.SerializerOptions
    // = new(JsonSerializerDefaults.Web)); match that so the replayed envelope round-trips.
    private static readonly JsonSerializerOptions ReplayOptions = new(JsonSerializerDefaults.Web);

    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly MessageId Message = new("message-alpha");
    private static readonly DateTimeOffset At = new(2026, 5, 18, 12, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// The SDK <c>ProcessAsync</c> reflection path reaches every static <c>Handle</c> overload and
    /// produces the same <see cref="DomainResult"/> as a direct static <c>Handle</c> call on the same
    /// command and state. Driving all six overloads (not just one) proves the reflection dispatch — not
    /// a manual dispatch table — is the live route into the aggregate. With a null prior state, the
    /// create command succeeds and the five state-dependent commands fail closed with their typed
    /// rejections; in every case the result is produced by the domain handler the reflection reached.
    /// </summary>
    /// <param name="commandKey">The command overload under test.</param>
    [Theory]
    [InlineData("CreateConversation")]
    [InlineData("AddParticipant")]
    [InlineData("ReassignConversationProject")]
    [InlineData("SetConversationRetentionPolicy")]
    [InlineData("MarkConversationContentSensitive")]
    [InlineData("RedactMessageContent")]
    public async Task ProcessAsyncReflectionDispatchMatchesDirectHandleForEveryOverload(string commandKey)
    {
        object command = BuildDomainCommand(commandKey);
        DomainResult direct = DirectHandle(command);

        CommandEnvelope envelope = Envelope(command.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(command, command.GetType(), CommandOptions));
        DomainResult dispatched = await new ConversationAggregate().ProcessAsync(envelope, currentState: null);

        ShouldMatchOutcome(dispatched, direct);
    }

    /// <summary>
    /// The reflection dispatch resolves a successful create through the SDK path with the same emitted
    /// event identity as the direct handler — confirming the route is the real live path, not a mirror.
    /// </summary>
    [Fact]
    public async Task ProcessAsyncResolvesCreateThroughReflectionToTheConversationCreatedEvent()
    {
        CreateConversation command = CreateDomainCommand();

        CommandEnvelope envelope = Envelope(typeof(CreateConversation).FullName!, JsonSerializer.SerializeToUtf8Bytes(command, CommandOptions));
        DomainResult dispatched = await new ConversationAggregate().ProcessAsync(envelope, currentState: null);

        dispatched.IsSuccess.ShouldBeTrue();
        ConversationCreatedDomainEvent created = dispatched.Events.Single().ShouldBeOfType<ConversationCreatedDomainEvent>();
        created.Metadata.EventId.ShouldBe("event-create-alpha");
        created.Metadata.ConversationId.ShouldBe(Conversation);
        created.Metadata.EventType.ShouldBe(ConversationEventType.ConversationCreated);
    }

    /// <summary>
    /// TEETH: a command type with no matching <c>Handle</c> overload surfaces the SDK's
    /// "No Handle method found" failure rather than a silent no-op. If the aggregate ever regressed to
    /// a stubbed or bypassed dispatch table, this assertion would go RED — proving the test exercises the
    /// real reflection lookup, not coverage theatre.
    /// </summary>
    [Fact]
    public async Task ProcessAsyncWithUnknownCommandTypeSurfacesNoHandleFoundFailure()
    {
        CommandEnvelope envelope = Envelope(
            "Hexalith.Conversations.Commands.DefinitelyNotAConversationCommand",
            JsonSerializer.SerializeToUtf8Bytes(new { ignored = true }, CommandOptions));

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await new ConversationAggregate().ProcessAsync(envelope, currentState: null).ConfigureAwait(true)).ConfigureAwait(true);

        ex.Message.ShouldContain("No Handle method found");
    }

    /// <summary>
    /// The SDK replay path reconstructs <see cref="ConversationState"/> via the
    /// <c>ConversationState.Apply(TEvent)</c> reflection convention. Replaying an ordered
    /// <see cref="ConversationCreatedDomainEvent"/> succeeds and applies sequence 1 — so removing the
    /// <c>Apply</c> convention (or the base-class replay wiring) would turn this RED.
    /// </summary>
    [Fact]
    public void ReplayReconstructsStateThroughTheApplyConvention()
    {
        AggregateReconstructionRequest request = ReplayRequest(
            new ReplayEventEnvelope(
                SequenceNumber: 1,
                EventTypeName: nameof(ConversationCreatedDomainEvent),
                Payload: JsonSerializer.SerializeToUtf8Bytes(CreatedEvent(), ReplayOptions),
                SerializationFormat: "json",
                MetadataVersion: 1,
                MessageId: "message-replay-1",
                CorrelationId: "correlation-alpha",
                CausationId: null));

        AggregateReconstructionResult result = new ConversationAggregate().Replay(request);

        result.Status.ShouldBe(AggregateReconstructionStatus.Succeeded);
        result.LastAppliedSequenceNumber.ShouldBe(1);
        result.ErrorCategory.ShouldBe(AggregateReconstructionErrorCategory.None);
        result.StateJson.ShouldNotBeNull();
    }

    /// <summary>
    /// TEETH: an event whose type name is not in the <c>Apply</c> convention is rejected as an unknown
    /// event type rather than silently skipped — proving replay routes through the discovered
    /// <c>Apply</c> methods, not a no-op fallback.
    /// </summary>
    [Fact]
    public void ReplayWithUnknownEventTypeFailsAsUnknownEventType()
    {
        AggregateReconstructionRequest request = ReplayRequest(
            new ReplayEventEnvelope(
                SequenceNumber: 1,
                EventTypeName: "DefinitelyNotAConversationEvent",
                Payload: JsonSerializer.SerializeToUtf8Bytes(new { ignored = true }, ReplayOptions),
                SerializationFormat: "json",
                MetadataVersion: 1,
                MessageId: "message-replay-1",
                CorrelationId: "correlation-alpha",
                CausationId: null));

        AggregateReconstructionResult result = new ConversationAggregate().Replay(request);

        result.Status.ShouldBe(AggregateReconstructionStatus.Failed);
        result.ErrorCategory.ShouldBe(AggregateReconstructionErrorCategory.UnknownEventType);
    }

    /// <summary>
    /// TEETH: the SDK reflection dispatch must bind the rehydrated <b>non-null</b> state into the
    /// handler's second parameter, not a null placeholder. With a created state passed as
    /// <c>currentState</c>, the AddParticipant handler <b>succeeds</b> and emits a
    /// <see cref="ParticipantAddedDomainEvent"/> — an outcome the null-state dispatch cases can never
    /// produce (they all fail closed). If the base class ever regressed to passing <see langword="null"/>
    /// for <c>parameters[1]</c> regardless of <c>currentState</c>, this would go RED while the existing
    /// null-state cases stayed green — so it closes the "state is actually delivered" gap that
    /// <see cref="ProcessAsyncReflectionDispatchMatchesDirectHandleForEveryOverload"/> cannot detect.
    /// </summary>
    [Fact]
    public async Task ProcessAsyncDeliversRehydratedNonNullStateToTheHandlerSuccessPath()
    {
        ConversationState created = new();
        created.Apply(CreatedEvent());

        AddParticipant command = AddParticipantDomainCommand();
        DomainResult direct = ConversationAggregate.Handle(command, created);

        CommandEnvelope envelope = Envelope(typeof(AddParticipant).FullName!, JsonSerializer.SerializeToUtf8Bytes(command, CommandOptions));
        DomainResult dispatched = await new ConversationAggregate().ProcessAsync(envelope, currentState: created);

        // Guard against a silently-rejecting fixture that would make the success assertion vacuous:
        // the direct handler must genuinely succeed against the created state.
        direct.IsSuccess.ShouldBeTrue();
        dispatched.IsSuccess.ShouldBeTrue();
        ParticipantAddedDomainEvent added = dispatched.Events.Single().ShouldBeOfType<ParticipantAddedDomainEvent>();
        added.ParticipantPartyId.ShouldBe(new PartyId("party-human"));
        added.Metadata.EventType.ShouldBe(ConversationEventType.ParticipantAdded);
        ShouldMatchOutcome(dispatched, direct);
    }

    /// <summary>
    /// The SDK replay path applies an ordered multi-event stream through the
    /// <c>ConversationState.Apply(TEvent)</c> convention, reaching more than one <c>Apply</c> overload
    /// (<see cref="ConversationCreatedDomainEvent"/> at sequence 1, then
    /// <see cref="ParticipantAddedDomainEvent"/> at sequence 2) and <b>accumulating</b> state.
    /// Reconstruction advances to the last sequence and the rebuilt state carries the participant —
    /// so a regression that stopped after the first event, or skipped the second <c>Apply</c> overload,
    /// would turn this RED. This complements <see cref="ReplayReconstructsStateThroughTheApplyConvention"/>,
    /// which only proves the single-event happy path.
    /// </summary>
    [Fact]
    public void ReplayAppliesAnOrderedEventSequenceThroughTheApplyConventionAccumulatingState()
    {
        AggregateReconstructionRequest request = ReplayRequest(
            upToSequence: 2,
            new ReplayEventEnvelope(
                SequenceNumber: 1,
                EventTypeName: nameof(ConversationCreatedDomainEvent),
                Payload: JsonSerializer.SerializeToUtf8Bytes(CreatedEvent(), ReplayOptions),
                SerializationFormat: "json",
                MetadataVersion: 1,
                MessageId: "message-replay-1",
                CorrelationId: "correlation-alpha",
                CausationId: null),
            new ReplayEventEnvelope(
                SequenceNumber: 2,
                EventTypeName: nameof(ParticipantAddedDomainEvent),
                Payload: JsonSerializer.SerializeToUtf8Bytes(ParticipantAddedEvent(), ReplayOptions),
                SerializationFormat: "json",
                MetadataVersion: 1,
                MessageId: "message-replay-2",
                CorrelationId: "correlation-alpha",
                CausationId: null));

        AggregateReconstructionResult result = new ConversationAggregate().Replay(request);

        result.Status.ShouldBe(AggregateReconstructionStatus.Succeeded);
        result.LastAppliedSequenceNumber.ShouldBe(2);
        result.ErrorCategory.ShouldBe(AggregateReconstructionErrorCategory.None);
        result.StateJson.ShouldNotBeNull();
        result.StateJson.ShouldContain("party-human");
    }

    private static void ShouldMatchOutcome(DomainResult dispatched, DomainResult direct)
    {
        dispatched.IsSuccess.ShouldBe(direct.IsSuccess);
        dispatched.IsRejection.ShouldBe(direct.IsRejection);
        dispatched.IsNoOp.ShouldBe(direct.IsNoOp);
        dispatched.Events.Select(e => e.GetType().Name)
            .ShouldBe(direct.Events.Select(e => e.GetType().Name));

        for (int i = 0; i < direct.Events.Count; i++)
        {
            if (direct.Events[i] is ConversationRejectedDomainEvent expected)
            {
                ConversationRejectedDomainEvent actual = dispatched.Events[i].ShouldBeOfType<ConversationRejectedDomainEvent>();
                actual.Code.ShouldBe(expected.Code);
                actual.ReasonCode.ShouldBe(expected.ReasonCode);
            }
        }
    }

    private static DomainResult DirectHandle(object command) => command switch
    {
        CreateConversation c => ConversationAggregate.Handle(c, state: null),
        AddParticipant c => ConversationAggregate.Handle(c, state: null),
        ReassignConversationProject c => ConversationAggregate.Handle(c, state: null),
        SetConversationRetentionPolicy c => ConversationAggregate.Handle(c, state: null),
        MarkConversationContentSensitive c => ConversationAggregate.Handle(c, state: null),
        RedactMessageContent c => ConversationAggregate.Handle(c, state: null),
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unsupported command fixture."),
    };

    private static object BuildDomainCommand(string commandKey) => commandKey switch
    {
        "CreateConversation" => CreateDomainCommand(),
        "AddParticipant" => AddParticipantDomainCommand(),
        "ReassignConversationProject" => ReassignDomainCommand(),
        "SetConversationRetentionPolicy" => RetentionDomainCommand(),
        "MarkConversationContentSensitive" => SensitivityDomainCommand(),
        "RedactMessageContent" => RedactionDomainCommand(),
        _ => throw new ArgumentOutOfRangeException(nameof(commandKey), commandKey, "Unsupported command fixture."),
    };

    private static ConversationCommandMetadata Metadata() => new(
        SchemaVersion.Current,
        Tenant,
        Actor,
        "correlation-alpha",
        CausationId: "causation-alpha",
        IdempotencyKey: "idempotency-alpha");

    private static CreateConversation CreateDomainCommand() => new(
        new CreateConversationCommand(
            Metadata(),
            new BusinessReference("crm", "case-123"),
            new ProjectId("project-alpha"),
            new FolderId("folder-alpha"),
            "Support case",
            new ProviderCorrelationMetadata(
                "contoso-ai",
                "assistant",
                SchemaVersion.Current,
                ProviderSessionReference: "provider-session-77",
                ProviderResponseReference: "provider-response-88",
                ExtensionData: new Dictionary<string, string> { ["thread"] = "thread-42" })),
        Conversation,
        At,
        "event-create-alpha");

    private static AddParticipant AddParticipantDomainCommand() => new(
        new AddParticipantCommand(
            Metadata(),
            Conversation,
            new PartyId("party-human"),
            ParticipantType.Human,
            ParticipantRole.Member),
        At,
        "event-add-party-human");

    private static ReassignConversationProject ReassignDomainCommand() => new(
        new ReassignConversationProjectCommand(
            Metadata(),
            Conversation,
            new ConversationProjectAssignment(ConversationProjectAssignmentOperation.Assign, new ProjectId("project-new"))),
        At,
        "event-project-changed");

    private static SetConversationRetentionPolicy RetentionDomainCommand() => new(
        new SetConversationRetentionPolicyCommand(
            Metadata(),
            Conversation,
            "retention-policy-standard",
            "customer-request",
            At),
        new GovernanceAuditEvidenceReference(new AuditEvidenceHandle("audit-evidence-001"), "retention-policy-standard", At),
        "event-retention-policy-standard");

    private static MarkConversationContentSensitive SensitivityDomainCommand() => new(
        new MarkConversationContentSensitiveCommand(
            Metadata(),
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            At),
        new GovernanceAuditEvidenceReference(new AuditEvidenceHandle("audit-evidence-001"), "sensitivity-policy-standard", At),
        "event-sensitive-a");

    private static RedactMessageContent RedactionDomainCommand() => new(
        new RedactMessageContentCommand(
            Metadata(),
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            At),
        new GovernanceAuditEvidenceReference(new AuditEvidenceHandle("audit-evidence-001"), "redaction-policy-standard", At),
        "event-redacted-a");

    private static ParticipantAddedDomainEvent ParticipantAddedEvent() => new(
        new ConversationEventMetadata(
            SchemaVersion.Current,
            "event-add-party-human",
            ConversationEventType.ParticipantAdded,
            Tenant,
            Conversation,
            "correlation-alpha",
            At,
            Actor,
            "causation-alpha"),
        new PartyId("party-human"),
        ParticipantType.Human,
        ParticipantRole.Member);

    private static ConversationCreatedDomainEvent CreatedEvent() => new(
        new ConversationEventMetadata(
            SchemaVersion.Current,
            "event-create-alpha",
            ConversationEventType.ConversationCreated,
            Tenant,
            Conversation,
            "correlation-alpha",
            At,
            Actor,
            "causation-alpha"));

    private static CommandEnvelope Envelope(string commandType, byte[] payload) => new(
        MessageId: "message-alpha",
        TenantId: Tenant.Value,
        Domain: Domain,
        AggregateId: Conversation.Value,
        CommandType: commandType,
        Payload: payload,
        CorrelationId: "correlation-alpha",
        CausationId: "causation-alpha",
        UserId: "user-alpha",
        Extensions: null);

    private static AggregateReconstructionRequest ReplayRequest(ReplayEventEnvelope evt) =>
        ReplayRequest(evt.SequenceNumber, evt);

    private static AggregateReconstructionRequest ReplayRequest(long upToSequence, params ReplayEventEnvelope[] events) => new(
        TenantId: Tenant.Value,
        Domain: Domain,
        AggregateType: nameof(ConversationAggregate),
        AggregateId: Conversation.Value,
        UpToSequence: upToSequence,
        Events: events,
        IncludeTimeline: false,
        RequestId: "replay-request-1");
}
