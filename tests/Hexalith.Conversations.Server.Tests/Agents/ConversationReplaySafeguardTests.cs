// <copyright file="ConversationReplaySafeguardTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Protects authoritative replay and legacy mutations using serialized local source events.</summary>
public sealed class ConversationReplaySafeguardTests
{
    /// <summary>Accepts an exact membership retry after a later message advanced the source timestamp.</summary>
    [Fact]
    public async Task ExactMembershipRetryAfterLaterPostingRemainsNoOp()
    {
        F f = await PostedAsync();
        ConversationState state = await f.ReplayAsync();
        state.LastEventAt.ShouldBe(f.Posting.PostedAt);
        state.LastEventAt!.Value.ShouldBeGreaterThan(f.Membership.AddedAt);
        string before = JsonSerializer.Serialize(state, F.Options);

        DomainResult retry = ConversationAggregate.Handle(f.Membership, state);

        retry.IsNoOp.ShouldBeTrue();
        retry.Events.ShouldBeEmpty();
        JsonSerializer.Serialize(state, F.Options).ShouldBe(before);
        f.Persist(retry);
        JsonSerializer.Serialize(await f.ReplayAsync(), F.Options).ShouldBe(before);
    }

    /// <summary>Rejects every legacy mutation after approved deletion while preserving its source state.</summary>
    /// <param name="operation">The otherwise valid legacy mutation.</param>
    [Theory]
    [InlineData("participant")]
    [InlineData("project")]
    [InlineData("retention")]
    [InlineData("sensitivity")]
    [InlineData("redaction")]
    public async Task ApprovedDeletionRejectsEveryLegacyMutation(string operation)
    {
        F f = await PostedAsync();
        LegacyMutation(f, await f.ReplayAsync(), operation).IsSuccess.ShouldBeTrue();
        await ApproveAsync(f);
        ConversationState deleted = await f.ReplayAsync();
        deleted.IsDeleted.ShouldBeTrue();
        deleted.Lifecycle.ShouldBe(ConversationLifecycleState.Open);
        string before = JsonSerializer.Serialize(deleted, F.Options);

        DomainResult result = LegacyMutation(f, deleted, operation);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>().ReasonCode.ShouldBe("conversation_not_open");
        JsonSerializer.Serialize(deleted, F.Options).ShouldBe(before);
        f.Persist(result);
        ConversationState replayed = await f.ReplayAsync();
        replayed.SourceRevision.ShouldBe(deleted.SourceRevision + 1);
        replayed.IsDeleted.ShouldBeTrue();
        replayed.DeletionSource.ShouldBe(deleted.DeletionSource);
        replayed.Participants.ShouldBe(deleted.Participants);
        replayed.Messages.ShouldBe(deleted.Messages);
        replayed.ProjectId.ShouldBeNull();
        replayed.ActiveRetentionPolicy.ShouldBeNull();
        replayed.SensitivityMarks.ShouldBeEmpty();
        replayed.Redactions.ShouldBeEmpty();
    }

    /// <summary>Rejects malformed retained edit text before state mutation and refuses serialized source reads.</summary>
    /// <param name="text">The malformed retained message text.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public async Task BlankRetainedEditCannotMutateOrBecomeAuthoritative(string? text)
    {
        F f = await PostedAsync();
        ConversationState state = await f.ReplayAsync();
        string before = JsonSerializer.Serialize(state, F.Options);
        var edited = new MessageEditedDomainEvent(
            f.Metadata(ConversationEventType.MessageEdited, F.Human, F.At.AddMinutes(3)),
            f.Posting.PublicCommand.MessageId, text!, F.Human);

        Should.Throw<ArgumentException>(() => state.Apply(edited));

        JsonSerializer.Serialize(state, F.Options).ShouldBe(before);
        f.PersistedEvents.Add(edited);
        ConversationReplayResult replay = ConversationReplayVerifier.Replay(F.Tenant, F.Conversation,
            f.PersistedEvents.Select((value, index) => new ConversationReplayEventRecord(index + 1, value)));
        replay.State.ShouldBeNull();
        replay.DiagnosticCode.ShouldBe("malformed_payload");
        (await new ConversationAgentSourceReader(f).ReadAsync(F.Tenant, F.Conversation,
            TestContext.Current.CancellationToken)).ShouldBeNull();
        ConversationAgentReadResult read = await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation),
            TestContext.Current.CancellationToken);
        read.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable);
        read.Messages.ShouldBeNull();
    }

    /// <summary>Retains valid human edits and their provenance without changing the original posting intent.</summary>
    [Fact]
    public async Task ValidRetainedEditPreservesOriginalIntentAndCurrentProvenance()
    {
        F f = await PostedAsync();
        ConversationState before = await f.ReplayAsync();
        ConversationMessage original = before.Messages.Single();
        var edit = new EditConversationMessageCommand(f.CommandMetadata(F.Human, "human-edit"), F.Conversation,
            original.MessageId, "Retained human edit", F.At.AddMinutes(3));

        f.Persist(ConversationAggregate.Handle(edit, before));

        ConversationState replayed = await f.ReplayAsync();
        ConversationMessage current = replayed.Messages.Single();
        current.Text.ShouldBe(edit.Text);
        current.EditedAt.ShouldBe(edit.OperationTimestamp);
        current.Provenance!.HumanEdited.ShouldBeTrue();
        current.Provenance.EditedByPartyId.ShouldBe(F.Human);
        current.Provenance.AgentCallTraceReference.ShouldBe(original.Provenance!.AgentCallTraceReference);
        current.OriginalText.ShouldBe(original.OriginalText);
        current.OriginalProvenance.ShouldBe(original.OriginalProvenance);
        current.OriginalIntentFingerprint.ShouldBe(original.OriginalIntentFingerprint);
        replayed.SourceRevision.ShouldBe(before.SourceRevision + 1);
        ConversationAggregate.Handle(f.Posting, replayed).IsNoOp.ShouldBeTrue();
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation),
            TestContext.Current.CancellationToken)).Messages!.Single().Text.ShouldBe(edit.Text);
    }

    /// <summary>Refuses rebinding the current or any older delivery attempt in retained attempts and acknowledgements.</summary>
    /// <param name="olderAttempt">Whether a newer independent attempt follows the immutable target binding.</param>
    /// <param name="action">The otherwise valid retained delivery transition.</param>
    [Theory]
    [InlineData(false, ConversationDeletionDeliveryAction.Attempt)]
    [InlineData(true, ConversationDeletionDeliveryAction.Attempt)]
    [InlineData(false, ConversationDeletionDeliveryAction.Acknowledge)]
    [InlineData(true, ConversationDeletionDeliveryAction.Acknowledge)]
    public async Task RetainedDeliveryCannotRebindAnyAttempt(bool olderAttempt, ConversationDeletionDeliveryAction action)
    {
        F f = await DeliveryHistoryAsync(olderAttempt);
        ConversationState state = await f.ReplayAsync();
        ConversationDeletionSignal signal = state.DeletionSource.Signal!;
        string before = JsonSerializer.Serialize(state, F.Options);
        ConversationDeletionAcknowledgement? receipt = action == ConversationDeletionDeliveryAction.Acknowledge
            ? new(signal.ConversationDeletionSignalId, signal.SourceRevision, 7, "conflicting-target", "local-receipt")
            : null;
        var conflicting = new ConversationDeletionDeliveryRecordedDomainEvent(
            f.Metadata(ConversationEventType.ConversationDeletionDeliveryRecorded, F.Human, F.At.AddMinutes(4)),
            signal, action, "original-attempt", "conflicting-target", state.DeletionSource.DeliveryRevision, receipt);

        Should.Throw<InvalidOperationException>(() => state.Apply(conflicting));

        JsonSerializer.Serialize(state, F.Options).ShouldBe(before);
        state.DeliveryAttemptTarget("original-attempt").ShouldBe("original-target");
        state.DeletionSource.Acknowledgement.ShouldBeNull();
        state.DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
        f.PersistedEvents.Add(conflicting);
        ConversationDeletionSourceResult read = await f.Queries.DeletionSourceAsync("source-worker",
            new(F.Tenant, F.Conversation), TestContext.Current.CancellationToken);
        read.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable);
        read.Acknowledgement.ShouldBeNull();
        read.AcknowledgedSourceRevision.ShouldBe(0);
    }

    /// <summary>Allows an older attempt to retry its original target and preserves exact current retries as no-ops.</summary>
    [Fact]
    public async Task SameTargetRetriesPreserveEveryImmutableAttemptBinding()
    {
        F f = await DeliveryHistoryAsync(olderAttempt: true);
        ConversationState before = await f.ReplayAsync();
        RecordConversationDeletionDelivery retry = DeliveryCommand(f, before, "original-attempt", "original-target");
        DomainResult result = ConversationAggregate.Handle(retry, before);
        result.IsSuccess.ShouldBeTrue();

        f.Persist(result);

        ConversationState replayed = await f.ReplayAsync();
        replayed.DeliveryAttemptTarget("original-attempt").ShouldBe("original-target");
        replayed.DeliveryAttemptTarget("newer-attempt").ShouldBe("newer-target");
        replayed.DeletionSource.TargetVersion.ShouldBe("original-target");
        replayed.DeletionSource.DeliveryRevision.ShouldBe(before.DeletionSource.DeliveryRevision + 1);
        replayed.DeletionSource.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        ConversationAggregate.Handle(retry, replayed).IsNoOp.ShouldBeTrue();
    }

    /// <summary>Accepts an older attempt's matching receipt after a newer attempt without losing its original target.</summary>
    [Fact]
    public async Task OlderAttemptAcknowledgementPreservesReceiptAndCheckpointOnReplay()
    {
        F f = await DeliveryHistoryAsync(olderAttempt: true);
        ConversationState before = await f.ReplayAsync();
        before.DeletionSource.LastAttemptId.ShouldBe("newer-attempt");
        ConversationDeletionSignal signal = before.DeletionSource.Signal!;
        var receipt = new ConversationDeletionAcknowledgement(signal.ConversationDeletionSignalId, signal.SourceRevision,
            7, "original-target", "authenticated-local-receipt");
        var acknowledgement = new RecordConversationDeletionDelivery(
            new(f.CommandMetadata(F.Human, "older-attempt-ack"), F.Conversation, signal,
                ConversationDeletionDeliveryAction.Acknowledge, "original-attempt", "original-target",
                before.DeletionSource.DeliveryRevision, receipt), "older-attempt-ack-event");
        DomainResult result = ConversationAggregate.Handle(acknowledgement, before);
        result.IsSuccess.ShouldBeTrue();

        f.Persist(result);

        ConversationState replayed = await f.ReplayAsync();
        replayed.DeliveryAttemptTarget("original-attempt").ShouldBe("original-target");
        replayed.DeliveryAttemptTarget("newer-attempt").ShouldBe("newer-target");
        replayed.DeletionSource.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        replayed.DeletionSource.LastAttemptId.ShouldBe("original-attempt");
        replayed.DeletionSource.TargetVersion.ShouldBe("original-target");
        replayed.DeletionSource.DeliveryRevision.ShouldBe(before.DeletionSource.DeliveryRevision + 1);
        replayed.DeletionSource.AcknowledgedSourceRevision.ShouldBe(signal.SourceRevision);
        replayed.DeletionSource.Acknowledgement.ShouldBe(receipt);
        ConversationAggregate.Handle(acknowledgement, replayed).IsNoOp.ShouldBeTrue();
        ConversationDeletionSourceResult read = await f.Queries.DeletionSourceAsync("source-worker",
            new(F.Tenant, F.Conversation), TestContext.Current.CancellationToken);
        read.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        read.Acknowledgement.ShouldBe(receipt);
        read.AcknowledgedSourceRevision.ShouldBe(signal.SourceRevision);
    }

    /// <summary>Replays command-generated conflict quarantine while preserving the original attempt target.</summary>
    /// <param name="olderAttempt">Whether the conflicting attempt predates the latest attempt.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommandGeneratedTargetConflictQuarantineRemainsReplayable(bool olderAttempt)
    {
        F f = await DeliveryHistoryAsync(olderAttempt);
        ConversationState before = await f.ReplayAsync();
        var command = DeliveryCommand(f, before, "original-attempt", "conflicting-target");
        DomainResult result = ConversationAggregate.Handle(command, before);
        var quarantine = result.Events.Single().ShouldBeOfType<ConversationDeletionDeliveryRecordedDomainEvent>();
        quarantine.Action.ShouldBe(ConversationDeletionDeliveryAction.Quarantine);
        quarantine.PoisonCode.ShouldBe("attempt-target-conflict");

        f.Persist(result);

        ConversationState replayed = await f.ReplayAsync();
        replayed.DeletionSource.Outcome.ShouldBe(ConversationAgentsOutcome.Quarantined);
        replayed.DeletionSource.Signal.ShouldBe(before.DeletionSource.Signal);
        replayed.DeletionSource.Acknowledgement.ShouldBeNull();
        replayed.DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
        replayed.DeliveryAttemptTarget("original-attempt").ShouldBe("original-target");
        (await f.Queries.DeletionSourceAsync("source-worker", new(F.Tenant, F.Conversation),
            TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.Quarantined);
    }

    /// <summary>Creates a serialized complete source with an Agent membership and original posting.</summary>
    /// <returns>The source fixture ready for replay.</returns>
    private static async Task<F> PostedAsync()
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        f.Persist(ConversationAggregate.Handle(f.Posting, await f.ReplayAsync()));
        return f;
    }

    /// <summary>Persists an independently approved deletion against the exact source revision.</summary>
    /// <param name="f">The serialized source fixture.</param>
    /// <returns>The asynchronous completion.</returns>
    private static async Task ApproveAsync(F f)
    {
        ConversationState before = await f.ReplayAsync();
        var approval = new ApproveConversationDeletion(new(f.CommandMetadata(F.Human, "approval"), F.Conversation,
            "independent-approval", before.SourceRevision, F.At.AddMinutes(3),
            F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(3))), "approval-event");
        DomainResult result = ConversationAggregate.Handle(approval, before);
        result.IsSuccess.ShouldBeTrue();
        f.Persist(result);
    }

    /// <summary>Creates delivery history with an immutable original attempt and an optional newer target.</summary>
    /// <param name="olderAttempt">Whether to record the second independent attempt.</param>
    /// <returns>The serialized delivery source.</returns>
    private static async Task<F> DeliveryHistoryAsync(bool olderAttempt)
    {
        var f = new F();
        await ApproveAsync(f);
        f.Persist(ConversationAggregate.Handle(DeliveryCommand(f, await f.ReplayAsync(), "original-attempt", "original-target"),
            await f.ReplayAsync()));
        if (olderAttempt)
        {
            f.Persist(ConversationAggregate.Handle(DeliveryCommand(f, await f.ReplayAsync(), "newer-attempt", "newer-target"),
                await f.ReplayAsync()));
        }
        return f;
    }

    /// <summary>Builds an exact delivery attempt against the replayed delivery revision.</summary>
    /// <param name="f">The source fixture.</param>
    /// <param name="state">The exact replayed source state.</param>
    /// <param name="attempt">The independent attempt identity.</param>
    /// <param name="target">The receiver target.</param>
    /// <returns>The pure domain delivery command.</returns>
    private static RecordConversationDeletionDelivery DeliveryCommand(F f, ConversationState state, string attempt, string target)
        => new(new(f.CommandMetadata(F.Human, $"delivery-{f.PersistedEvents.Count}"), F.Conversation,
            state.DeletionSource.Signal!, ConversationDeletionDeliveryAction.Attempt, attempt, target,
            state.DeletionSource.DeliveryRevision), $"delivery-event-{f.PersistedEvents.Count}");

    /// <summary>Runs one otherwise valid legacy mutation against the supplied source state.</summary>
    /// <param name="f">The source fixture.</param>
    /// <param name="state">The source state supplied to the pure aggregate.</param>
    /// <param name="operation">The legacy mutation to execute.</param>
    /// <returns>The aggregate mutation result.</returns>
    private static DomainResult LegacyMutation(F f, ConversationState state, string operation)
    {
        ConversationCommandMetadata metadata = f.CommandMetadata(F.Human, $"legacy-{operation}");
        DateTimeOffset occurrence = F.At.AddMinutes(10);
        var target = new GovernanceTarget(GovernedTargetKind.Message, MessageId: f.Posting.PublicCommand.MessageId);
        var audit = new GovernanceAuditEvidenceReference(new AuditEvidenceHandle("local-audit"), "local-policy", occurrence);
        return operation switch
        {
            "participant" => ConversationAggregate.Handle(new AddParticipant(new(metadata, F.Conversation,
                new PartyId("legacy-human-member"), ParticipantType.Human, ParticipantRole.Member), occurrence, "legacy-participant-event"), state),
            "project" => ConversationAggregate.Handle(new ReassignConversationProject(new(metadata, F.Conversation,
                new(ConversationProjectAssignmentOperation.Assign, new ProjectId("legacy-project"))), occurrence, "legacy-project-event"), state),
            "retention" => ConversationAggregate.Handle(new SetConversationRetentionPolicy(new(metadata, F.Conversation,
                "local-policy", "local-rationale", occurrence), audit, "legacy-retention-event"), state),
            "sensitivity" => ConversationAggregate.Handle(new MarkConversationContentSensitive(new(metadata, F.Conversation,
                target, SensitivityCategory.Restricted, "local-policy", "local-rationale", occurrence), audit, "legacy-sensitive-event"), state),
            "redaction" => ConversationAggregate.Handle(new RedactMessageContent(new(metadata, F.Conversation,
                target, RedactionCategory.ContentSuppression, "local-policy", "local-rationale", occurrence), audit, "legacy-redacted-event"), state),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown legacy mutation."),
        };
    }
}
