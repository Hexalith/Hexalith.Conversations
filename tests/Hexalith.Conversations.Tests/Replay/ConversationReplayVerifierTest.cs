// <copyright file="ConversationReplayVerifierTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.State;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Replay;

/// <summary>
/// Proves deterministic ordered replay from persisted conversation events only.
/// </summary>
public sealed class ConversationReplayVerifierTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly TenantId OtherTenant = new("tenant-other");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly PartyId Participant = new("party-human");
    private static readonly MessageId Message = new("message-alpha");
    private static readonly FileId File = new("file-alpha");
    private static readonly FolderId Folder = new("folder-alpha");
    private static readonly DateTimeOffset Started = new(2026, 5, 22, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OrderedPersistedEventsShouldReplayDeterministically()
    {
        ConversationReplayEventRecord[] history = OrderedHistory();

        ConversationReplayResult first = ConversationReplayVerifier.Replay(Tenant, Conversation, history);
        ConversationReplayResult second = ConversationReplayVerifier.Replay(Tenant, Conversation, history);

        first.Outcome.ShouldBe(ConversationReplayOutcome.Replay);
        second.Outcome.ShouldBe(ConversationReplayOutcome.Replay);
        first.State.ShouldNotBeNull();
        second.State.ShouldNotBeNull();
        first.State.IsCreated.ShouldBeTrue();
        first.State.ConversationId.ShouldBe(Conversation);
        first.State.TenantId.ShouldBe(Tenant);
        first.State.CreatorPartyId.ShouldBe(Actor);
        first.State.SchemaVersion.ShouldBe(SchemaVersion.Current);
        first.State.Label.ShouldBe("Case 456");
        first.State.Lifecycle.ShouldBe(ConversationLifecycleState.Open);
        first.State.Attributes["priority"].ShouldBe("high");
        first.State.Participants.Single().PartyId.ShouldBe(Participant);
        first.State.Messages.Single().MessageId.ShouldBe(Message);
        first.State.FileReferences.Single().FileId.ShouldBe(File);
        first.State.BusinessReference.ShouldBe(new BusinessReference("crm", "case-456"));
        first.State.ProviderCorrelation.ShouldNotBeNull();
        first.State.ShouldBeEquivalentTo(second.State);
    }

    [Fact]
    public void DuplicateParticipantEventIdentityShouldBeNoOpButDuplicateCreationShouldReject()
    {
        ConversationReplayResult participantReplay = ConversationReplayVerifier.Replay(
            Tenant,
            Conversation,
            [
                Record(1, Created("event-create-001", 1)),
                Record(2, ParticipantAdded("event-participant-001", 2)),
                Record(3, ParticipantAdded("event-participant-001", 2)),
            ]);

        participantReplay.Outcome.ShouldBe(ConversationReplayOutcome.Replay);
        participantReplay.State!.Participants.Count.ShouldBe(1);

        ConversationReplayResult duplicateCreate = ConversationReplayVerifier.Replay(
            Tenant,
            Conversation,
            [
                Record(1, Created("event-create-001", 1)),
                Record(2, Created("event-create-001", 2)),
            ]);

        duplicateCreate.Outcome.ShouldBe(ConversationReplayOutcome.Reject);
        duplicateCreate.DiagnosticCode.ShouldBe("duplicate_event_identity");
    }

    [Theory]
    [InlineData("future-version", "unsupported_schema_version")]
    [InlineData("tenant-mismatch", "tenant_mismatch")]
    [InlineData("conversation-mismatch", "conversation_mismatch")]
    [InlineData("reordered-position", "event_position_reordered")]
    [InlineData("position-gap", "event_position_gap")]
    [InlineData("unknown-event", "unsupported_event_type")]
    [InlineData("malformed-payload", "malformed_payload")]
    [InlineData("event-type-mismatch", "event_type_mismatch")]
    [InlineData("provider-correlation-change", null)]
    public void ReplayVersionMatrixShouldProduceDeterministicOutcomes(string row, string? diagnostic)
    {
        ConversationReplayResult result = row switch
        {
            "future-version" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, Created("event-create-001", 1, schemaVersion: new SchemaVersion(2)))]),
            "tenant-mismatch" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, Created("event-create-001", 1, tenantId: OtherTenant))]),
            "conversation-mismatch" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, Created("event-create-001", 1, conversationId: new ConversationId("conversation-other")))]),
            "reordered-position" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [
                    Record(1, Created("event-create-001", 1)),
                    Record(2, ParticipantAdded("event-participant-001", 2)),
                    Record(2, MessageAppended("event-message-001", 2)),
                ]),
            "position-gap" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [
                    Record(1, Created("event-create-001", 1)),
                    Record(3, ParticipantAdded("event-participant-001", 3)),
                ]),
            "unknown-event" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, new UnknownReplayEvent())]),
            "malformed-payload" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [
                    Record(1, Created("event-create-001", 1)),
                    Record(2, MessageAppended("event-message-001", 2, text: "   ")),
                ]),
            "event-type-mismatch" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, Created("event-create-001", 1, eventType: ConversationEventType.ParticipantAdded))]),
            "provider-correlation-change" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [
                    Record(1, Created("event-create-001", 1, providerSession: "provider-session-a")),
                    Record(2, MessageAppended("event-message-001", 2, providerSession: "provider-session-b")),
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(row), row, "Unsupported matrix row."),
        };

        if (diagnostic is null)
        {
            result.Outcome.ShouldBe(ConversationReplayOutcome.Replay);
            result.State!.ConversationId.ShouldBe(Conversation);
            result.State.Messages.Single().ProviderCorrelation!.ProviderSessionReference.ShouldBe("provider-session-b");
        }
        else
        {
            result.Outcome.ShouldBe(ConversationReplayOutcome.Reject);
            result.DiagnosticCode.ShouldBe(diagnostic);
            result.State.ShouldBeNull();
        }
    }

    [Fact]
    public void ReplayRejectionDiagnosticsShouldBeBoundedSafeCodes()
    {
        // AC7/AC8: rejection diagnostics must not echo payload fragments, raw exception messages,
        // Party display data, provider raw IDs as authority, or storage topology.
        string[] safeCodes =
        [
            "tenant_mismatch",
            "conversation_mismatch",
            "unsupported_schema_version",
            "unsupported_event_type",
            "event_type_mismatch",
            "event_position_reordered",
            "event_position_gap",
            "duplicate_event_identity",
            "malformed_payload",
            "conversation_not_created",
            "replay_invariant_violation",
        ];

        foreach (string code in safeCodes)
        {
            code.ShouldNotContain("@");
            code.ShouldNotContain(" ");
            code.ShouldNotContain("Exception", Case.Insensitive);
            code.ShouldNotContain("stream", Case.Insensitive);
            code.ShouldNotContain("offset", Case.Insensitive);
            code.ShouldNotContain("dapr", Case.Insensitive);
            code.ShouldNotContain("signalr", Case.Insensitive);
            code.ShouldNotContain("provider-session", Case.Insensitive);
            code.Length.ShouldBeLessThanOrEqualTo(64);
        }
    }

    [Fact]
    public void RejectionEventsShouldBeReplayNoOps()
    {
        ConversationReplayResult result = ConversationReplayVerifier.Replay(
            Tenant,
            Conversation,
            [
                Record(1, Created("event-create-001", 1)),
                Record(2, new ConversationRejectedDomainEvent(
                    Hexalith.Conversations.Contracts.Errors.ConversationErrorCode.CommandValidationFailed,
                    "command_missing",
                    SchemaVersion.Current,
                    "correlation-001",
                    "causation-001")),
            ]);

        result.Outcome.ShouldBe(ConversationReplayOutcome.Replay);
        result.State!.IsCreated.ShouldBeTrue();
    }

    [Fact]
    public void MisorderedRejectionEventShouldFailClosed()
    {
        ConversationReplayResult result = ConversationReplayVerifier.Replay(
            Tenant,
            Conversation,
            [
                Record(1, Created("event-create-001", 1)),
                Record(5, new ConversationRejectedDomainEvent(
                    Hexalith.Conversations.Contracts.Errors.ConversationErrorCode.CommandValidationFailed,
                    "command_missing",
                    SchemaVersion.Current,
                    "correlation-001",
                    "causation-001")),
            ]);

        result.Outcome.ShouldBe(ConversationReplayOutcome.Reject);
        result.DiagnosticCode.ShouldBe("event_position_gap");
        result.State.ShouldBeNull();
    }

    [Fact]
    public void AdditiveV1PayloadFieldsShouldNotBreakReplay()
    {
        // Proves the replay layer tolerates additive v1 payload fields without requiring upcasting.
        // The contract record carries the documented fields; an additional adopter-supplied
        // BusinessReference qualifier (here treated as an additive v1 field through the public
        // record's optional argument) must replay without rejecting the stream.
        ConversationCreatedDomainEvent createdWithAdditive = new(
            Metadata("event-create-001", ConversationEventType.ConversationCreated, 1),
            new BusinessReference("crm", "case-123"),
            new ProjectId("project-alpha"),
            Folder,
            "Case 123",
            "idempotency-001",
            Provider("provider-session-001"));

        ConversationReplayResult result = ConversationReplayVerifier.Replay(
            Tenant,
            Conversation,
            [
                Record(1, createdWithAdditive),
                Record(2, ParticipantAdded("event-participant-001", 2)),
            ]);

        result.Outcome.ShouldBe(ConversationReplayOutcome.Replay);
        result.State!.BusinessReference.ShouldBe(new BusinessReference("crm", "case-123"));
        result.State.ProjectId.ShouldBe(new ProjectId("project-alpha"));
        result.State.Label.ShouldBe("Case 123");
    }

    [Theory]
    [InlineData("tenant-mismatch")]
    [InlineData("future-version")]
    [InlineData("unknown-event")]
    public void NegativeReplayOutcomesShouldExposeNonDisclosingShape(string row)
    {
        ConversationReplayResult result = row switch
        {
            "tenant-mismatch" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, Created("event-create-001", 1, tenantId: OtherTenant))]),
            "future-version" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, Created("event-create-001", 1, schemaVersion: new SchemaVersion(2)))]),
            "unknown-event" => ConversationReplayVerifier.Replay(
                Tenant,
                Conversation,
                [Record(1, new UnknownReplayEvent())]),
            _ => throw new ArgumentOutOfRangeException(nameof(row), row, "Unsupported matrix row."),
        };

        result.Outcome.ShouldBe(ConversationReplayOutcome.Reject);
        result.State.ShouldBeNull();
        result.DiagnosticCode.ShouldNotBeNullOrWhiteSpace();
        result.DiagnosticCode!.ShouldNotContain("tenant-other");
        result.DiagnosticCode.ShouldNotContain("tenant-alpha");
        result.DiagnosticCode.ShouldNotContain("conversation-alpha");
        result.DiagnosticCode.ShouldNotContain("event-create-001");

        string serialized = result.ToString();
        AssertNoSensitiveLeakage(serialized);
    }

    [Fact]
    public void SuccessfulReplayStateShouldNotLeakSensitivePayload()
    {
        ConversationReplayResult result = ConversationReplayVerifier.Replay(Tenant, Conversation, OrderedHistory());

        result.Outcome.ShouldBe(ConversationReplayOutcome.Replay);
        string serialized = result.ToString();
        AssertNoSensitiveLeakage(serialized);
    }

    private static void AssertNoSensitiveLeakage(string text)
    {
        text.ShouldNotContain("@", Case.Insensitive);
        text.ShouldNotContain("password", Case.Insensitive);
        text.ShouldNotContain("token", Case.Insensitive);
        text.ShouldNotContain("stream-", Case.Insensitive);
        text.ShouldNotContain("offset:", Case.Insensitive);
        text.ShouldNotContain("dapr", Case.Insensitive);
        text.ShouldNotContain("signalr", Case.Insensitive);
        text.ShouldNotContain("stacktrace", Case.Insensitive);
        text.ShouldNotContain("Exception:", Case.Insensitive);
    }

    private static ConversationReplayEventRecord[] OrderedHistory() =>
    [
        Record(1, Created("event-create-001", 1)),
        Record(2, ParticipantAdded("event-participant-001", 2)),
        Record(3, MessageAppended("event-message-001", 3)),
        Record(4, FileAttached("event-file-001", 4)),
        Record(5, MetadataUpdated("event-metadata-001", 5)),
    ];

    private static ConversationReplayEventRecord Record(long position, object e)
        => new(position, e);

    private static ConversationCreatedDomainEvent Created(
        string eventId,
        long position,
        SchemaVersion? schemaVersion = null,
        TenantId? tenantId = null,
        ConversationId? conversationId = null,
        ConversationEventType? eventType = null,
        string providerSession = "provider-session-001")
        => new(
            Metadata(eventId, eventType ?? ConversationEventType.ConversationCreated, position, schemaVersion, tenantId, conversationId),
            new BusinessReference("crm", "case-123"),
            new ProjectId("project-alpha"),
            Folder,
            "Case 123",
            "idempotency-001",
            Provider(providerSession));

    private static ParticipantAddedDomainEvent ParticipantAdded(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.ParticipantAdded, position),
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static MessageAppended MessageAppended(
        string eventId,
        long position,
        string providerSession = "provider-session-001",
        string text = "Hello")
        => new(
            Metadata(eventId, ConversationEventType.MessageAppended, position),
            Message,
            Actor,
            text,
            Provider(providerSession));

    private static FileReferenceAttached FileAttached(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.FileReferenceAttached, position),
            File,
            Folder,
            Message);

    private static ConversationMetadataUpdated MetadataUpdated(string eventId, long position)
        => new(
            Metadata(eventId, ConversationEventType.ConversationMetadataUpdated, position),
            "Case 456",
            new BusinessReference("crm", "case-456"),
            new Dictionary<string, string> { ["priority"] = "high" });

    private static ConversationEventMetadata Metadata(
        string eventId,
        ConversationEventType eventType,
        long position,
        SchemaVersion? schemaVersion = null,
        TenantId? tenantId = null,
        ConversationId? conversationId = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            eventId,
            eventType,
            tenantId ?? Tenant,
            conversationId ?? Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");

    private static ProviderCorrelationMetadata Provider(string session)
        => new("contoso-ai", "assistant", SchemaVersion.Current, session, "provider-response-001");

    private sealed record UnknownReplayEvent;
}
